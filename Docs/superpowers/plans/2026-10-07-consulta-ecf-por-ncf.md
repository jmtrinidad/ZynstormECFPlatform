# Consulta de e-CF por eNCF Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Agregar `GET v1/Ecf/by-ncf/{eNcf}`, un endpoint de solo lectura que devuelve el estado, el TrackId, el código de seguridad, la fecha de firma y el QR de un e-CF ya recibido, buscándolo por eNCF.

**Architecture:** Un servicio nuevo `IEcfLookupService` (en `ZynstormECFPlatform.Services/Production`) lee los `EcfDocument`, `EcfTransmission` y `EcfXmlDocument` del cliente dueño de la API key. Toda la lógica (normalizar el eNCF, derivar el estado, elegir entre duplicados, armar la respuesta y el QR) vive en funciones estáticas puras en `EcfLookupLogic`, que es lo que se prueba. El controlador solo valida, delega y traduce a HTTP.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10, xUnit 2.9, Moq no necesario.

Spec: `docs/superpowers/specs/2026-10-07-consulta-ecf-por-ncf-design.md`

## Global Constraints

- Ruta exacta: `GET v1/Ecf/by-ncf/{eNcf}` en `EcfController`, que hereda `[ApiKeyAuth]`.
- Filtra siempre por el `ClientId` de `HttpContext.Items["ClientId"]` (tipo `int`). Sin `ClientId` → 403. Nunca devuelve documentos de otro cliente.
- eNCF válido: `E` + 12 dígitos y tipo reconocido por `NcfHelper` (se normaliza con trim y mayúsculas). Inválido → 400.
- Solo lectura: no modifica nada y no llama a la DGII.
- Valores de `state`: `Accepted | AcceptedConditional | Pending | Rejected | Error | NotSent`, serializados como texto.
- `isUsable` es true solo para `Accepted`, `AcceptedConditional` y `Pending`.
- Prioridad al elegir entre documentos con el mismo eNCF: `Accepted` > `AcceptedConditional` > `Pending` > `Rejected` > `Error` > `NotSent`; a igual prioridad, el más reciente.
- Mapeo de `EcfStatusId`: 10 → Accepted; 7, 8, 9 → Pending; 11 → AcceptedConditional si el `estado` de la última transmisión es condicional, si no Rejected; 3 → Rejected; 12 → Error; 1, 2, 4, 5, 6, 13 → NotSent.
- Si falta el XML firmado en un documento utilizable: se devuelve el estado sin `securityCode`/`signatureDate`/`qrUrl` y con un `message`. No es un 500.
- Ambiente del QR: `EcfXmlValidation:TargetDgiiEnvironment`; si falta o no se reconoce, `Production`.
- El repo tiene un cambio sin commitear en `.github/workflows/deploy.yml` que no pertenece a esta tarea: **nunca** uses `git add .` ni `git commit -a`; agrega solo los archivos de cada tarea.
- Comandos desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`. Commits en español, terminan con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `ZynstormECFPlatform.Services/Production/EcfLookupModels.cs` (nuevo) | Enum `EcfLookupState`, records `EcfLookupCandidate`, `EcfLookupSource`, `EcfLookupResponse` |
| `ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs` (nuevo) | Funciones estáticas puras: `NormalizeNcf`, `ResolveState`, `IsUsable`, `PickBest`, `ForClient`, `ResolveEnvironment`, `BuildResponse` |
| `ZynstormECFPlatform.Services/Production/EcfLookupService.cs` (nuevo) | `IEcfLookupService` + `EcfLookupService`: acceso a datos y orquestación |
| `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (modif.) | `ExtractXmlValue` y `ExtractSecurityCode` pasan de `private` a `internal` para reutilizarlos |
| `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (modif.) | Registro del servicio |
| `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (modif.) | Acción `GetByNcf` |
| `ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs` (nuevo) | Pruebas de toda la lógica pura |

---

### Task 1: Modelos, normalización, estado y selección

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfLookupModels.cs`
- Create: `ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs`
- Modify: `docs/superpowers/specs/2026-10-07-consulta-ecf-por-ncf-design.md` (una frase del `trackId`)

**Interfaces:**
- Produces:
  - `enum EcfLookupState { Accepted, AcceptedConditional, Pending, Rejected, Error, NotSent }`
  - `sealed record EcfLookupCandidate(int EcfDocumentId, EcfLookupState State, DateTime RegisteredAt)`
  - `sealed record EcfLookupSource(int EcfDocumentId, string ENcf, decimal Total, DateTime IssueDateUtc, DateTime? SignatureDateTime, string IssuerRnc, string CustomerRnc, string? TrackId, string? SignedXml)`
  - `sealed record EcfLookupResponse` (propiedades init; ver código)
  - `static string? EcfLookupLogic.NormalizeNcf(string? raw)`
  - `static EcfLookupState EcfLookupLogic.ResolveState(int ecfStatusId, string? lastResponsePayload)`
  - `static bool EcfLookupLogic.IsUsable(EcfLookupState state)`
  - `static EcfLookupCandidate? EcfLookupLogic.PickBest(IEnumerable<EcfLookupCandidate> candidates)`

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfLookupLogicTests
{
    // ─── NormalizeNcf ──────────────────────────────────────────────

    [Theory]
    [InlineData("E320000000098", "E320000000098")]
    [InlineData("  e320000000098 ", "E320000000098")]
    public void NormalizeNcf_AcceptsValidAndNormalizes(string raw, string expected)
    {
        Assert.Equal(expected, EcfLookupLogic.NormalizeNcf(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B0100000001")]          // NCF tradicional
    [InlineData("E32000000009")]         // 12 caracteres
    [InlineData("E3200000000988")]       // 14 caracteres
    [InlineData("E32000000009A")]        // letra en el consecutivo
    [InlineData("E990000000001")]        // tipo de e-CF que no existe
    public void NormalizeNcf_RejectsInvalid(string? raw)
    {
        Assert.Null(EcfLookupLogic.NormalizeNcf(raw));
    }

    // ─── ResolveState ──────────────────────────────────────────────

    private const string ConditionalPayload =
        "{\"transmission\":{\"TrackId\":\"T1\"},\"status\":{\"TrackId\":\"T1\",\"Codigo\":\"4\",\"Estado\":\"Aceptado Condicional\"}}";

    private const string RejectedPayload =
        "{\"transmission\":{},\"status\":{\"Codigo\":\"2\",\"Estado\":\"Rechazado\"}}";

    [Theory]
    [InlineData(10, EcfLookupState.Accepted)]
    [InlineData(7, EcfLookupState.Pending)]
    [InlineData(8, EcfLookupState.Pending)]
    [InlineData(9, EcfLookupState.Pending)]
    [InlineData(3, EcfLookupState.Rejected)]
    [InlineData(12, EcfLookupState.Error)]
    [InlineData(1, EcfLookupState.NotSent)]
    [InlineData(2, EcfLookupState.NotSent)]
    [InlineData(4, EcfLookupState.NotSent)]
    [InlineData(5, EcfLookupState.NotSent)]
    [InlineData(6, EcfLookupState.NotSent)]
    [InlineData(13, EcfLookupState.NotSent)]
    public void ResolveState_MapsStatusIds(int statusId, EcfLookupState expected)
    {
        Assert.Equal(expected, EcfLookupLogic.ResolveState(statusId, null));
    }

    [Fact]
    public void ResolveState_Id11WithConditionalPayload_IsAcceptedConditional()
    {
        Assert.Equal(EcfLookupState.AcceptedConditional, EcfLookupLogic.ResolveState(11, ConditionalPayload));
    }

    [Theory]
    [InlineData(RejectedPayload)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("esto no es json")]
    [InlineData("{\"transmission\":{},\"status\":null}")]
    public void ResolveState_Id11WithoutConditionalPayload_IsRejected(string? payload)
    {
        Assert.Equal(EcfLookupState.Rejected, EcfLookupLogic.ResolveState(11, payload));
    }

    [Theory]
    [InlineData(EcfLookupState.Accepted, true)]
    [InlineData(EcfLookupState.AcceptedConditional, true)]
    [InlineData(EcfLookupState.Pending, true)]
    [InlineData(EcfLookupState.Rejected, false)]
    [InlineData(EcfLookupState.Error, false)]
    [InlineData(EcfLookupState.NotSent, false)]
    public void IsUsable_OnlyForAcceptedAcceptedConditionalAndPending(EcfLookupState state, bool expected)
    {
        Assert.Equal(expected, EcfLookupLogic.IsUsable(state));
    }

    // ─── PickBest ──────────────────────────────────────────────────

    private static EcfLookupCandidate Candidate(int id, EcfLookupState state, int minutes) =>
        new(id, state, new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc).AddMinutes(minutes));

    [Fact]
    public void PickBest_PrefersAcceptedOverALaterRejection()
    {
        // La factura 118: el primer envío fue aceptado y el reenvío fue rechazado con 75.
        var accepted = Candidate(1, EcfLookupState.Accepted, 0);
        var rejected = Candidate(2, EcfLookupState.Rejected, 1);

        Assert.Equal(1, EcfLookupLogic.PickBest([rejected, accepted])!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_FollowsThePriorityOrder()
    {
        var all = new[]
        {
            Candidate(1, EcfLookupState.NotSent, 5),
            Candidate(2, EcfLookupState.Error, 4),
            Candidate(3, EcfLookupState.Rejected, 3),
            Candidate(4, EcfLookupState.Pending, 2),
            Candidate(5, EcfLookupState.AcceptedConditional, 1),
        };

        Assert.Equal(5, EcfLookupLogic.PickBest(all)!.EcfDocumentId);
        Assert.Equal(4, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId != 5))!.EcfDocumentId);
        Assert.Equal(3, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId < 4))!.EcfDocumentId);
        Assert.Equal(2, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId < 3))!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_BreaksTiesWithTheMostRecent()
    {
        var older = Candidate(1, EcfLookupState.Rejected, 0);
        var newer = Candidate(2, EcfLookupState.Rejected, 10);

        Assert.Equal(2, EcfLookupLogic.PickBest([older, newer])!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_WithNoCandidates_ReturnsNull()
    {
        Assert.Null(EcfLookupLogic.PickBest([]));
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfLookupLogicTests"`
Expected: error de compilación (`EcfLookupLogic`/`EcfLookupState` no existen).

- [ ] **Step 3: Crear los modelos**

Crear `ZynstormECFPlatform.Services/Production/EcfLookupModels.cs`:

```csharp
using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfLookupState
{
    Accepted,
    AcceptedConditional,
    Pending,
    Rejected,
    Error,
    NotSent
}

/// <summary>Un documento de la plataforma con ese eNCF, reducido a lo que hace falta para elegir.</summary>
public sealed record EcfLookupCandidate(int EcfDocumentId, EcfLookupState State, DateTime RegisteredAt);

/// <summary>Los datos ya leídos de la base con los que se arma la respuesta.</summary>
public sealed record EcfLookupSource(
    int EcfDocumentId,
    string ENcf,
    decimal Total,
    DateTime IssueDateUtc,
    DateTime? SignatureDateTime,
    string IssuerRnc,
    string CustomerRnc,
    string? TrackId,
    string? SignedXml);

public sealed record EcfLookupResponse
{
    public bool Found { get; init; } = true;

    public string ENcf { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EcfLookupState State { get; init; }

    public bool IsUsable { get; init; }

    public string? TrackId { get; init; }

    public string? SecurityCode { get; init; }

    public string? SignatureDate { get; init; }

    public string? QrUrl { get; init; }

    public int EcfDocumentId { get; init; }

    public decimal Total { get; init; }

    public DateTime IssueDate { get; init; }

    /// <summary>Cantidad de documentos de la plataforma con ese eNCF (envíos y reenvíos).</summary>
    public int Attempts { get; init; }

    public string Message { get; init; } = string.Empty;
}
```

- [ ] **Step 4: Crear la lógica mínima**

Crear `ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs`:

```csharp
using System.Text.Json;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

public static class EcfLookupLogic
{
    /// <summary>eNCF en mayúsculas y sin espacios, o null si no es un eNCF válido.</summary>
    public static string? NormalizeNcf(string? raw)
    {
        var value = raw?.Trim().ToUpperInvariant();

        if (value is null || value.Length != 13 || value[0] != 'E')
            return null;

        if (!value.Skip(1).All(char.IsAsciiDigit))
            return null;

        return NcfHelper.TryExtractEcfType(value, out _) ? value : null;
    }

    /// <summary>
    /// Estado a partir del EcfStatusId de la plataforma. El id 11 se usa tanto para Rejected
    /// como para Aceptado Condicional (MapDgiiStatusToEcfStatus), así que ahí se lee el
    /// estado de la última transmisión.
    /// </summary>
    public static EcfLookupState ResolveState(int ecfStatusId, string? lastResponsePayload) => ecfStatusId switch
    {
        10 => EcfLookupState.Accepted,
        7 or 8 or 9 => EcfLookupState.Pending,
        11 => IsConditional(lastResponsePayload) ? EcfLookupState.AcceptedConditional : EcfLookupState.Rejected,
        3 => EcfLookupState.Rejected,
        12 => EcfLookupState.Error,
        _ => EcfLookupState.NotSent
    };

    public static bool IsUsable(EcfLookupState state) =>
        state is EcfLookupState.Accepted or EcfLookupState.AcceptedConditional or EcfLookupState.Pending;

    public static EcfLookupCandidate? PickBest(IEnumerable<EcfLookupCandidate> candidates) =>
        candidates
            .OrderBy(candidate => Priority(candidate.State))
            .ThenByDescending(candidate => candidate.RegisteredAt)
            .ThenByDescending(candidate => candidate.EcfDocumentId)
            .FirstOrDefault();

    private static int Priority(EcfLookupState state) => state switch
    {
        EcfLookupState.Accepted => 0,
        EcfLookupState.AcceptedConditional => 1,
        EcfLookupState.Pending => 2,
        EcfLookupState.Rejected => 3,
        EcfLookupState.Error => 4,
        _ => 5
    };

    /// <summary>
    /// ResponsePayload se guarda como { transmission, status }. Solo importa si el estado de
    /// la DGII fue "Aceptado Condicional".
    /// </summary>
    private static bool IsConditional(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
            return false;

        try
        {
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("status", out var statusElement)
                || statusElement.ValueKind != JsonValueKind.Object)
                return false;

            var status = statusElement.Deserialize<DgiiStatusResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return status is not null && ReceivedEcfProductionService.IsAcceptedConditionalDgiiStatus(status);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfLookupLogicTests"`
Expected: `Passed!` con todas las pruebas de `EcfLookupLogicTests` en verde.

- [ ] **Step 6: Ajustar la frase del TrackId en el spec**

En `docs/superpowers/specs/2026-10-07-consulta-ecf-por-ncf-design.md`, reemplazar:

```
- `trackId`: de la última transmisión con `Success` del documento elegido.
```

por:

```
- `trackId`: de la última transmisión del documento elegido que tenga `TrackId` no vacío (un
  documento `Pending` aún no tiene `Success`, pero sí `TrackId`).
```

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfLookupModels.cs ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs docs/superpowers/specs/2026-10-07-consulta-ecf-por-ncf-design.md
git commit -m "feat(ecf): lógica de consulta por eNCF (normalizar, estado y selección)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Filtro por cliente, ambiente y armado de la respuesta

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (visibilidad de dos helpers, líneas ~1010 y ~1024)
- Modify: `ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs`
- Modify: `ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs`

**Interfaces:**
- Consumes: `EcfLookupSource`, `EcfLookupResponse`, `EcfLookupState`, `EcfLookupLogic.IsUsable` (Task 1).
- Produces:
  - `static Expression<Func<EcfDocument, bool>> EcfLookupLogic.ForClient(int clientId, string eNcf)`
  - `static DgiiEnvironment EcfLookupLogic.ResolveEnvironment(string? configured)`
  - `static EcfLookupResponse EcfLookupLogic.BuildResponse(EcfLookupSource source, EcfLookupState state, int attempts, DgiiEnvironment environment)`

- [ ] **Step 1: Escribir las pruebas que fallan**

Agregar al final de la clase `EcfLookupLogicTests` (antes de la llave de cierre de la clase) y estos `using` arriba del archivo:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
```

```csharp
    // ─── ForClient ─────────────────────────────────────────────────

    [Fact]
    public void ForClient_ReturnsOnlyTheClientsOwnNonDeletedDocuments()
    {
        var documents = new[]
        {
            new EcfDocument { EcfDocumentId = 1, ClientId = 7, Ncf = "E320000000098" },
            new EcfDocument { EcfDocumentId = 2, ClientId = 8, Ncf = "E320000000098" },   // otro cliente
            new EcfDocument { EcfDocumentId = 3, ClientId = 7, Ncf = "E320000000099" },   // otro eNCF
            new EcfDocument { EcfDocumentId = 4, ClientId = 7, Ncf = "E320000000098", IsDeleted = true },
        }.AsQueryable();

        var ids = documents
            .Where(EcfLookupLogic.ForClient(7, "E320000000098"))
            .Select(document => document.EcfDocumentId)
            .ToList();

        Assert.Equal([1], ids);
    }

    // ─── ResolveEnvironment ────────────────────────────────────────

    [Theory]
    [InlineData("Production", DgiiEnvironment.Production)]
    [InlineData("test", DgiiEnvironment.Test)]
    [InlineData("CerteCF", DgiiEnvironment.CerteCF)]
    [InlineData(null, DgiiEnvironment.Production)]
    [InlineData("", DgiiEnvironment.Production)]
    [InlineData("algo raro", DgiiEnvironment.Production)]
    public void ResolveEnvironment_ParsesOrDefaultsToProduction(string? configured, DgiiEnvironment expected)
    {
        Assert.Equal(expected, EcfLookupLogic.ResolveEnvironment(configured));
    }

    // ─── BuildResponse ─────────────────────────────────────────────

    private const string SignedXml =
        "<ECF><FechaHoraFirma>07-10-2026 09:01:23</FechaHoraFirma>" +
        "<Signature><SignatureValue>ABCDEF123456</SignatureValue></Signature></ECF>";

    private static EcfLookupSource Source(string? signedXml, DateTime? signatureDateTime = null) => new(
        EcfDocumentId: 123,
        ENcf: "E320000000098",
        Total: 140m,
        IssueDateUtc: new DateTime(2026, 10, 7),
        SignatureDateTime: signatureDateTime,
        IssuerRnc: "132293894",
        CustomerRnc: "",
        TrackId: "TRACK-1",
        SignedXml: signedXml);

    [Fact]
    public void BuildResponse_Accepted_CarriesTheDataNeededToPrintTheInvoice()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), EcfLookupState.Accepted, attempts: 2, DgiiEnvironment.Production);

        Assert.True(response.Found);
        Assert.Equal(EcfLookupState.Accepted, response.State);
        Assert.True(response.IsUsable);
        Assert.Equal("TRACK-1", response.TrackId);
        Assert.Equal("ABCDEF", response.SecurityCode);
        Assert.Equal("07-10-2026 09:01:23", response.SignatureDate);
        Assert.Equal(
            "https://fc.dgii.gov.do/ecf/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000098&MontoTotal=140&CodigoSeguridad=ABCDEF",
            response.QrUrl);
        Assert.Equal(123, response.EcfDocumentId);
        Assert.Equal(2, response.Attempts);
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void BuildResponse_UsableWithoutSignedXml_ReturnsStateWithoutSecurityData()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(signedXml: null), EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.True(response.IsUsable);
        Assert.Null(response.SecurityCode);
        Assert.Null(response.SignatureDate);
        Assert.Null(response.QrUrl);
        Assert.Contains("XML firmado", response.Message);
    }

    [Fact]
    public void BuildResponse_UnparseableSignedXml_IsTreatedAsMissing()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source("esto no es xml"), EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.Null(response.SecurityCode);
        Assert.Null(response.QrUrl);
        Assert.Contains("XML firmado", response.Message);
    }

    [Fact]
    public void BuildResponse_SignatureDateFallsBackToTheStoredOne()
    {
        const string xmlWithoutDate = "<ECF><Signature><SignatureValue>ABCDEF123456</SignatureValue></Signature></ECF>";

        var response = EcfLookupLogic.BuildResponse(
            Source(xmlWithoutDate, new DateTime(2026, 10, 7, 9, 1, 23)),
            EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.Equal("07-10-2026 09:01:23", response.SignatureDate);
        Assert.NotNull(response.QrUrl);
    }

    [Theory]
    [InlineData(EcfLookupState.Rejected)]
    [InlineData(EcfLookupState.Error)]
    [InlineData(EcfLookupState.NotSent)]
    public void BuildResponse_NotUsable_DoesNotExposeSecurityData(EcfLookupState state)
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), state, attempts: 1, DgiiEnvironment.Production);

        Assert.False(response.IsUsable);
        Assert.Null(response.SecurityCode);
        Assert.Null(response.QrUrl);
        Assert.False(string.IsNullOrWhiteSpace(response.Message));
    }

    [Fact]
    public void BuildResponse_Pending_IsUsableAndKeepsTheTrackId()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), EcfLookupState.Pending, attempts: 1, DgiiEnvironment.Production);

        Assert.True(response.IsUsable);
        Assert.Equal("TRACK-1", response.TrackId);
    }
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfLookupLogicTests"`
Expected: error de compilación (`ForClient`, `ResolveEnvironment`, `BuildResponse` no existen).

- [ ] **Step 3: Hacer reutilizables los dos extractores de XML**

En `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`, cambiar la visibilidad (solo la palabra `private` → `internal`):

```csharp
    internal static string ExtractXmlValue(string xml, string localName)
```

```csharp
    internal static string ExtractSecurityCode(string xml)
```

- [ ] **Step 4: Agregar las tres funciones a `EcfLookupLogic`**

En `EcfLookupLogic.cs`, agregar estos `using` arriba:

```csharp
using System.Linq.Expressions;
using ZynstormECFPlatform.Core.Ecf;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
```

y estos miembros dentro de la clase, antes de `Priority`:

```csharp
    /// <summary>Documentos de ESE cliente con ese eNCF. Es el límite de seguridad del endpoint.</summary>
    public static Expression<Func<EcfDocument, bool>> ForClient(int clientId, string eNcf) =>
        document => document.ClientId == clientId && document.Ncf == eNcf && !document.IsDeleted;

    /// <summary>Ambiente del QR: el configurado o, si falta o no se reconoce, Production.</summary>
    public static DgiiEnvironment ResolveEnvironment(string? configured) =>
        Enum.TryParse<DgiiEnvironment>(configured, ignoreCase: true, out var environment)
            ? environment
            : DgiiEnvironment.Production;

    public static EcfLookupResponse BuildResponse(
        EcfLookupSource source,
        EcfLookupState state,
        int attempts,
        DgiiEnvironment environment)
    {
        var usable = IsUsable(state);
        var message = StateMessage(state);
        string? securityCode = null;
        string? signatureDate = null;
        string? qrUrl = null;

        if (usable)
        {
            var code = string.IsNullOrWhiteSpace(source.SignedXml)
                ? string.Empty
                : ReceivedEcfProductionService.ExtractSecurityCode(source.SignedXml);

            if (string.IsNullOrWhiteSpace(code))
            {
                message = "El comprobante existe en la plataforma, pero no se pudo leer su XML firmado: no hay código de seguridad ni QR.";
            }
            else
            {
                securityCode = code;

                var date = ReceivedEcfProductionService.ExtractXmlValue(source.SignedXml!, "FechaHoraFirma");
                if (string.IsNullOrWhiteSpace(date))
                    date = source.SignatureDateTime?.ToString("dd-MM-yyyy HH:mm:ss") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(date))
                {
                    message = "El comprobante existe en la plataforma, pero no tiene fecha de firma: no se pudo armar el QR.";
                }
                else
                {
                    signatureDate = date;

                    if (NcfHelper.TryExtractEcfType(source.ENcf, out var ecfType))
                    {
                        qrUrl = EcfQrUrlBuilder.Build(
                            environment,
                            ecfType,
                            source.IssuerRnc,
                            source.CustomerRnc,
                            source.ENcf,
                            source.IssueDateUtc.ToString("dd-MM-yyyy"),
                            source.Total,
                            date,
                            code);
                    }
                }
            }
        }

        return new EcfLookupResponse
        {
            Found = true,
            ENcf = source.ENcf,
            State = state,
            IsUsable = usable,
            TrackId = string.IsNullOrWhiteSpace(source.TrackId) ? null : source.TrackId,
            SecurityCode = securityCode,
            SignatureDate = signatureDate,
            QrUrl = qrUrl,
            EcfDocumentId = source.EcfDocumentId,
            Total = source.Total,
            IssueDate = source.IssueDateUtc,
            Attempts = attempts,
            Message = message
        };
    }

    private static string StateMessage(EcfLookupState state) => state switch
    {
        EcfLookupState.Rejected => "La plataforma tiene este eNCF rechazado.",
        EcfLookupState.Error => "La plataforma tiene este eNCF con error de envío.",
        EcfLookupState.NotSent => "El documento no llegó a salir hacia la DGII; se puede reenviar.",
        _ => string.Empty
    };
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfLookupLogicTests"`
Expected: `Passed!`, todas en verde (las de la Task 1 y las nuevas).

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs ZynstormECFPlatform.Services/Production/EcfLookupLogic.cs ZynstormECFPlatform.Tests/Production/EcfLookupLogicTests.cs
git commit -m "feat(ecf): filtro por cliente, ambiente y armado de la respuesta de consulta

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Servicio de consulta y registro

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfLookupService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs:56`

**Interfaces:**
- Consumes: `EcfLookupLogic.ForClient / ResolveState / PickBest / ResolveEnvironment / BuildResponse`, `EcfLookupCandidate`, `EcfLookupSource`, `EcfLookupResponse` (Tasks 1-2).
- Produces: `interface IEcfLookupService { Task<EcfLookupResponse?> FindByNcfAsync(int clientId, string eNcf, CancellationToken cancellationToken = default); }`. Devuelve `null` si la plataforma no tiene ese eNCF para ese cliente. El `eNcf` ya viene normalizado.

El acceso a datos de este servicio no se prueba con xUnit (no hay base de datos de prueba en el repo); todo lo que decide está en `EcfLookupLogic`, ya probado. Se verifica con el build.

- [ ] **Step 1: Crear el servicio**

Crear `ZynstormECFPlatform.Services/Production/EcfLookupService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Services.Production;

public interface IEcfLookupService
{
    /// <summary>
    /// Estado de un e-CF ya recibido, por eNCF y dentro de un solo cliente. Null si la
    /// plataforma nunca recibió ese eNCF. Solo lee: no modifica nada ni llama a la DGII.
    /// </summary>
    Task<EcfLookupResponse?> FindByNcfAsync(int clientId, string eNcf, CancellationToken cancellationToken = default);
}

public class EcfLookupService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients,
    IConfiguration configuration) : IEcfLookupService
{
    public async Task<EcfLookupResponse?> FindByNcfAsync(
        int clientId,
        string eNcf,
        CancellationToken cancellationToken = default)
    {
        var found = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, eNcf))
            .Select(document => new { document.EcfDocumentId, document.EcfStatusId, document.RegisteredAt })
            .ToListAsync(cancellationToken);

        if (found.Count == 0)
            return null;

        var ids = found.Select(document => document.EcfDocumentId).ToList();

        var sent = await transmissions.Table
            .AsNoTracking()
            .Where(transmission => ids.Contains(transmission.EcfDocumentId))
            .Select(transmission => new
            {
                transmission.EcfDocumentId,
                transmission.TrackId,
                transmission.ResponsePayload,
                transmission.SentAtUtc
            })
            .ToListAsync(cancellationToken);

        // Más reciente primero: el primer elemento es la última transmisión del documento.
        var transmissionsByDocument = sent
            .GroupBy(transmission => transmission.EcfDocumentId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(transmission => transmission.SentAtUtc).ToList());

        var candidates = found
            .Select(document => new EcfLookupCandidate(
                document.EcfDocumentId,
                EcfLookupLogic.ResolveState(
                    document.EcfStatusId,
                    transmissionsByDocument.TryGetValue(document.EcfDocumentId, out var list)
                        ? list[0].ResponsePayload
                        : null),
                document.RegisteredAt))
            .ToList();

        var best = EcfLookupLogic.PickBest(candidates)!;

        var detail = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == best.EcfDocumentId)
            .Select(document => new
            {
                document.Ncf,
                document.Total,
                document.IssueDateUtc,
                document.SignatureDateTime,
                document.CustomerRnc
            })
            .FirstAsync(cancellationToken);

        var issuerRnc = await clients.Table
            .AsNoTracking()
            .Where(client => client.ClientId == clientId)
            .Select(client => client.Rnc)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var signedXml = await xmlDocuments.Table
            .AsNoTracking()
            .Where(xml => xml.EcfDocumentId == best.EcfDocumentId)
            .OrderByDescending(xml => xml.EcfXmlDocumentId)
            .Select(xml => xml.XmlSigned)
            .FirstOrDefaultAsync(cancellationToken);

        var trackId = transmissionsByDocument.TryGetValue(best.EcfDocumentId, out var bestList)
            ? bestList.FirstOrDefault(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))?.TrackId
            : null;

        var source = new EcfLookupSource(
            best.EcfDocumentId,
            detail.Ncf,
            detail.Total,
            detail.IssueDateUtc,
            detail.SignatureDateTime,
            issuerRnc,
            detail.CustomerRnc,
            trackId,
            signedXml);

        var environment = EcfLookupLogic.ResolveEnvironment(configuration["EcfXmlValidation:TargetDgiiEnvironment"]);

        return EcfLookupLogic.BuildResponse(source, best.State, found.Count, environment);
    }
}
```

- [ ] **Step 2: Registrar el servicio**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, justo después de la línea

```csharp
        services.AddTransient<Production.IReceivedEcfProductionService, Production.ReceivedEcfProductionService>();
```

agregar:

```csharp
        services.AddTransient<Production.IEcfLookupService, Production.EcfLookupService>();
```

- [ ] **Step 3: Compilar**

Run: `dotnet build ZynstormECFPlatform.Services`
Expected: `Build succeeded` con 0 errores. Si `Client.Rnc` o `IClientService.Table` no compilan, abrir `ZynstormECFPlatform.Core/Entities/Client.cs` y `ZynstormECFPlatform.Abstractions/DataServices/IClientService.cs` y usar el nombre real de la propiedad del RNC del cliente; no cambiar nada más.

- [ ] **Step 4: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfLookupService.cs ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(ecf): servicio de consulta de e-CF por eNCF

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Endpoint y verificación final

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (constructor y una acción nueva después de `GetEmissionStatus`)

**Interfaces:**
- Consumes: `IEcfLookupService.FindByNcfAsync(int clientId, string eNcf, CancellationToken)` (Task 3), `EcfLookupLogic.NormalizeNcf(string?)` (Task 1).
- Produces: `GET v1/Ecf/by-ncf/{eNcf}` → 200 `EcfLookupResponse`; 400 `{found:false,message}`; 403 `{found:false,message}`; 404 `{found:false,message}`.

- [ ] **Step 1: Inyectar el servicio**

En `EcfController.cs`, cambiar el constructor primario y agregar el campo:

```csharp
    public class EcfController(
        IEcfProductionGeneratorService ecfGeneratorService,
        IReceivedEcfProductionService receivedEcfProductionService,
        IEcfLookupService ecfLookupService,
        ICacheService cacheService,
        ILogger<EcfController> logger) : ControllerBase
    {
        private readonly IEcfProductionGeneratorService _ecfGeneratorService = ecfGeneratorService;
        private readonly IReceivedEcfProductionService _receivedEcfProductionService = receivedEcfProductionService;
        private readonly IEcfLookupService _ecfLookupService = ecfLookupService;
```

(el resto de los campos `_cacheService` y `_logger` queda igual).

- [ ] **Step 2: Agregar la acción**

Después del método `GetEmissionStatus` y antes de `GetSample`:

```csharp
        /// <summary>
        /// Consulta un e-CF ya recibido por su eNCF, dentro del cliente dueño de la API key.
        /// Existe para el integrador que perdió la respuesta de <c>emit</c> (timeout): devuelve
        /// el estado, el TrackId, el código de seguridad, la fecha de firma y el QR, de modo que
        /// no haga falta reenviar un comprobante que la DGII ya aceptó. No modifica nada.
        /// </summary>
        [HttpGet("by-ncf/{eNcf}")]
        [ProducesResponseType(typeof(EcfLookupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByNcf(string eNcf, CancellationToken cancellationToken)
        {
            // Sin ClientId no hay forma de acotar la consulta: ocurre con una sesión JWT de la
            // web, que no pasa por la API key. Nunca se consulta "sin cliente".
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    found = false,
                    message = "Esta consulta requiere una API key válida en el header 'X-Api-Key'."
                });
            }

            var normalized = EcfLookupLogic.NormalizeNcf(eNcf);

            if (normalized is null)
            {
                return BadRequest(new
                {
                    found = false,
                    message = "El eNCF no es válido: debe ser 'E' seguido de 12 dígitos (por ejemplo E310000000001)."
                });
            }

            var result = await _ecfLookupService.FindByNcfAsync(clientId, normalized, cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    found = false,
                    message = "La plataforma no tiene registrado ese eNCF."
                });
            }

            return Ok(result);
        }
```

- [ ] **Step 3: Compilar toda la solución**

Run: `dotnet build ZynstormECFPlatform.slnx`
Expected: `Build succeeded`, 0 errores (las advertencias que ya existían no cuentan).

- [ ] **Step 4: Correr todas las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests`
Expected: `Passed!` sin fallos. Si falla alguna prueba que no sea `EcfLookupLogicTests`, comprobar con `git stash` si ya fallaba antes de estos cambios y reportarlo sin corregirla.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs
git commit -m "feat(ecf): endpoint GET v1/Ecf/by-ncf/{eNcf}

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

- [ ] **Step 6: Comprobar que no se coló nada ajeno**

Run: `git status --short`
Expected: solo ` M .github/workflows/deploy.yml` (el cambio previo, sin tocar) y nada más.

---

## Self-Review

**Cobertura del spec**
- Endpoint, ruta, `[ApiKeyAuth]`, 403 sin `ClientId`, 400 formato, 404, 200 → Task 4.
- Aislamiento por cliente → `ForClient` (Task 2, probado) + uso en el servicio (Task 3) + 403 en el controlador (Task 4).
- Normalización y validación del eNCF → Task 1.
- Tabla de estados e id 11 ambiguo → `ResolveState` (Task 1).
- Prioridad entre duplicados (caso 118) → `PickBest` (Task 1).
- `trackId`, código de seguridad, fecha de firma, QR → `BuildResponse` (Task 2) y carga de datos (Task 3).
- XML ausente o ilegible → `BuildResponse` (Task 2, probado).
- Ambiente del QR → `ResolveEnvironment` (Task 2).
- `NotSent` y `isUsable` → Tasks 1 y 2.
- Pruebas listadas en el spec: todas cubiertas en `EcfLookupLogicTests`; el aislamiento por cliente se prueba sobre el filtro, que es lo que impone el límite.
- Ajuste al spec: el `trackId` sale de la última transmisión con `TrackId` no vacío (los `Pending` no tienen `Success`); se corrige en Task 1, paso 6.

**Placeholders:** ninguno; cada paso de código trae el código completo.

**Consistencia de tipos:** `EcfLookupCandidate`, `EcfLookupSource`, `EcfLookupResponse`, `EcfLookupState` y las firmas de `EcfLookupLogic` son las mismas en todas las tareas. `FindByNcfAsync(int, string, CancellationToken)` coincide entre Task 3 y Task 4.
