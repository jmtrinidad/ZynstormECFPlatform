# `emit` idempotente (plataforma, parte 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que `POST v1/Ecf/emit` deje de transmitir dos veces el mismo eNCF: si el documento ya está aceptado o en proceso devuelve lo guardado (`replayed`), si falló permite reenviar; guardar el TrackId apenas la DGII lo devuelve; y medir el tiempo de cada fase.

**Architecture:** Una función pura `EcfEmitDecision.Decide` elige entre `Create`, `Replay` y `Retry` a partir de los documentos existentes con ese `(ClientId, eNCF)`. `ReceivedEcfProductionService.ProcessAsync` ejecuta esa decisión dentro de una transacción `READ COMMITTED` protegida por `pg_advisory_xact_lock`. Los datos de los documentos existentes y la respuesta de un `Replay` salen de `EcfLookupService` (ya existente). El modo diferido, el job de transmisión y los reintentos de transporte son la parte 2 y no están aquí.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10 + Npgsql, Dapper, xUnit 2.9.

Spec: `Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md` (secciones 1 a 4 y 5b, primer punto, y «Rendimiento», fase 0).

## Global Constraints

- Identidad de un documento: `(ClientId, eNCF)`, derivada del cuerpo del `emit`. Sin header de idempotencia.
- Reglas: aceptado o aceptado condicional → `Replay` sin transmitir; en vuelo (estados 1, 2, 4, 5, 6, 7, 8, 9) con actividad hace menos de `staleAfter`, o con TrackId → `Replay` (`Pending`); rechazado, error, `ValidationFailed` (3), cancelado (13) o en vuelo vencido sin TrackId → `Retry`; ninguno → `Create`.
- `staleAfter` = `EcfIdempotency:StaleInFlightMinutes`, 5 por defecto, acotado entre 1 y 120.
- `Retry` y `Create` crean un `EcfDocument` nuevo (los anteriores son historial). `attempt` = cantidad de documentos con ese eNCF.
- La transacción de decisión es `READ COMMITTED` (no la `Serializable` que usa `IUnitOfWork` por defecto): con `Serializable`, el snapshot se toma antes de obtener el candado y la petición que espera no vería el documento recién confirmado.
- Un `Replay` responde HTTP 200 siempre, también si está `Pending` (la librería solo trata 2xx como éxito). El `504` actual de un pendiente no replay se conserva.
- Cada `Replay` deja una fila en `SystemLog` y otra en `EcfStatusHistory`.
- El TrackId se persiste (transmisión en estado 9, `Success = false`) apenas `SendEcfAsync` lo devuelve; el estado final solo actualiza esa fila.
- Comandos desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`. Commits en español, terminan con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- El repo tiene un cambio sin commitear en `.github/workflows/deploy.yml` que no pertenece a esta tarea: **nunca** uses `git add .` ni `git commit -a`. Git registra la carpeta de docs como `Docs/` (con mayúscula).

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `ZynstormECFPlatform.Services/Production/EcfEmitDecision.cs` (nuevo) | Tipos `EcfEmitAction`, `EcfEmitExisting`, `EcfEmitDecisionResult` y la función pura `Decide` + `LockKey` |
| `ZynstormECFPlatform.Services/Production/EcfEmitReplay.cs` (nuevo) | Traduce un `EcfLookupResponse` a la respuesta de `emit` con `Replayed = true` |
| `ZynstormECFPlatform.Services/Production/EcfEmitTimings.cs` (nuevo) | Cronómetro por fase |
| `ZynstormECFPlatform.Services/Production/EcfLookupService.cs` (modif.) | Dos métodos nuevos: `ListExistingAsync`, `DescribeDocumentAsync` |
| `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs` (modif.) | `Replayed` y `Attempt` en la respuesta |
| `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (modif.) | Reclamo del documento bajo candado, TrackId inmediato, cronómetro |
| `ZynstormECFPlatform.Abstractions/Data/IUnitOfWork.cs`, `ZynstormECFPlatform.Data/UnitOfWork.cs` (modif.) | Transacción con nivel de aislamiento elegible |
| `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (modif.) | Un replay pendiente responde 200 |
| `ZynstormECFPlatform.Tests/Production/EcfEmitDecisionTests.cs`, `EcfEmitReplayTests.cs`, `EcfEmitTimingsTests.cs` (nuevos) | Pruebas de las funciones puras |

---

### Task 1: Decisión pura y clave del candado

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfEmitDecision.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfEmitDecisionTests.cs`

**Interfaces:**
- Consumes: `EcfLookupState`, `EcfLookupLogic.ResolveState` (existentes).
- Produces:
  - `enum EcfEmitAction { Create, Replay, Retry }`
  - `sealed record EcfEmitExisting(int EcfDocumentId, int EcfStatusId, EcfLookupState State, DateTime LastActivityUtc, bool HasTrackId)`
  - `sealed record EcfEmitDecisionResult(EcfEmitAction Action, int? ReplayDocumentId, EcfLookupState? ReplayState)`
  - `static EcfEmitDecisionResult EcfEmitDecision.Decide(IReadOnlyCollection<EcfEmitExisting> existing, DateTime nowUtc, TimeSpan staleAfter)`
  - `static long EcfEmitDecision.LockKey(int clientId, string eNcf)`

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfEmitDecisionTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitDecisionTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(5);

    private static EcfEmitExisting Doc(
        int id, int statusId, int minutesAgo = 0, bool track = false, EcfLookupState? state = null) =>
        new(id, statusId, state ?? EcfLookupLogic.ResolveState(statusId, null), Now.AddMinutes(-minutesAgo), track);

    private static EcfEmitDecisionResult Decide(params EcfEmitExisting[] existing) =>
        EcfEmitDecision.Decide(existing, Now, Stale);

    [Fact]
    public void NoDocuments_Creates()
    {
        var result = Decide();

        Assert.Equal(EcfEmitAction.Create, result.Action);
        Assert.Null(result.ReplayDocumentId);
    }

    [Fact]
    public void Accepted_Replays()
    {
        var result = Decide(Doc(1, 10));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(1, result.ReplayDocumentId);
        Assert.Equal(EcfLookupState.Accepted, result.ReplayState);
    }

    [Fact]
    public void AcceptedConditional_Replays()
    {
        var result = Decide(Doc(1, 11, state: EcfLookupState.AcceptedConditional));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.AcceptedConditional, result.ReplayState);
    }

    [Fact]
    public void Invoice118_AcceptedFirstThenRejectedDuplicate_ReplaysTheAcceptedOne()
    {
        var result = Decide(Doc(1, 10, minutesAgo: 1), Doc(2, 11, minutesAgo: 0));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(1, result.ReplayDocumentId);
    }

    [Fact]
    public void Accepted_BeatsAnyInFlightDocument()
    {
        var result = Decide(Doc(1, 7, minutesAgo: 1), Doc(2, 10, minutesAgo: 30));

        Assert.Equal(2, result.ReplayDocumentId);
        Assert.Equal(EcfLookupState.Accepted, result.ReplayState);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void RecentInFlight_ReplaysAsPending(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 1));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.Pending, result.ReplayState);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(8)]
    public void StaleInFlightWithoutTrackId_Retries(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 6));

        Assert.Equal(EcfEmitAction.Retry, result.Action);
    }

    [Fact]
    public void StaleThreshold_IsExclusive()
    {
        Assert.Equal(EcfEmitAction.Replay, Decide(Doc(1, 8, minutesAgo: 4)).Action);
        Assert.Equal(EcfEmitAction.Retry, Decide(Doc(1, 8, minutesAgo: 5)).Action);
    }

    [Fact]
    public void StalePendingWithTrackId_StillReplays()
    {
        // La DGII ya tiene el documento: el job de seguimiento lo resolverá.
        var result = Decide(Doc(1, 9, minutesAgo: 120, track: true));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(EcfLookupState.Pending, result.ReplayState);
    }

    [Theory]
    [InlineData(3)]    // ValidationFailed
    [InlineData(11)]   // Rejected
    [InlineData(12)]   // Error
    [InlineData(13)]   // Cancelled
    public void FailedDocuments_Retry(int statusId)
    {
        var result = Decide(Doc(1, statusId, minutesAgo: 0));

        Assert.Equal(EcfEmitAction.Retry, result.Action);
        Assert.Null(result.ReplayDocumentId);
    }

    [Fact]
    public void InFlight_BeatsFailedDocuments()
    {
        var result = Decide(Doc(1, 11, minutesAgo: 10), Doc(2, 7, minutesAgo: 1));

        Assert.Equal(EcfEmitAction.Replay, result.Action);
        Assert.Equal(2, result.ReplayDocumentId);
    }

    [Fact]
    public void SeveralInFlight_ReplaysTheMostRecent()
    {
        var result = Decide(Doc(1, 8, minutesAgo: 3), Doc(2, 8, minutesAgo: 1));

        Assert.Equal(2, result.ReplayDocumentId);
    }

    // ─── LockKey ───────────────────────────────────────────────────

    [Fact]
    public void LockKey_IsStableForTheSameClientAndNcf()
    {
        Assert.Equal(
            EcfEmitDecision.LockKey(7, "E320000000098"),
            EcfEmitDecision.LockKey(7, "E320000000098"));
    }

    [Fact]
    public void LockKey_DiffersByNcfAndByClient()
    {
        var baseKey = EcfEmitDecision.LockKey(7, "E320000000098");

        Assert.NotEqual(baseKey, EcfEmitDecision.LockKey(7, "E320000000099"));
        Assert.NotEqual(baseKey, EcfEmitDecision.LockKey(8, "E320000000098"));
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitDecisionTests"`
Expected: error de compilación (`EcfEmitDecision`, `EcfEmitExisting` no existen).

- [ ] **Step 3: Implementar**

Crear `ZynstormECFPlatform.Services/Production/EcfEmitDecision.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfEmitAction
{
    /// <summary>No hay documento con ese eNCF: se crea uno y se emite.</summary>
    Create,

    /// <summary>Ya existe un documento aceptado o en proceso: no se transmite, se devuelve lo guardado.</summary>
    Replay,

    /// <summary>Los documentos existentes fallaron o quedaron colgados: se crea uno nuevo y se emite.</summary>
    Retry
}

/// <summary>Un documento ya existente con ese (cliente, eNCF), reducido a lo que decide.</summary>
public sealed record EcfEmitExisting(
    int EcfDocumentId,
    int EcfStatusId,
    EcfLookupState State,
    DateTime LastActivityUtc,
    bool HasTrackId);

public sealed record EcfEmitDecisionResult(
    EcfEmitAction Action,
    int? ReplayDocumentId,
    EcfLookupState? ReplayState);

public static class EcfEmitDecision
{
    public static EcfEmitDecisionResult Decide(
        IReadOnlyCollection<EcfEmitExisting> existing,
        DateTime nowUtc,
        TimeSpan staleAfter)
    {
        if (existing.Count == 0)
            return new EcfEmitDecisionResult(EcfEmitAction.Create, null, null);

        var accepted = existing
            .Where(document => document.State is EcfLookupState.Accepted or EcfLookupState.AcceptedConditional)
            .OrderBy(document => document.State == EcfLookupState.Accepted ? 0 : 1)
            .ThenByDescending(document => document.LastActivityUtc)
            .ThenByDescending(document => document.EcfDocumentId)
            .FirstOrDefault();

        if (accepted is not null)
            return new EcfEmitDecisionResult(EcfEmitAction.Replay, accepted.EcfDocumentId, accepted.State);

        var live = existing
            .Where(document => IsLive(document, nowUtc, staleAfter))
            .OrderByDescending(document => document.LastActivityUtc)
            .ThenByDescending(document => document.EcfDocumentId)
            .FirstOrDefault();

        if (live is not null)
            return new EcfEmitDecisionResult(EcfEmitAction.Replay, live.EcfDocumentId, EcfLookupState.Pending);

        return new EcfEmitDecisionResult(EcfEmitAction.Retry, null, null);
    }

    /// <summary>
    /// Clave de 64 bits para pg_advisory_xact_lock, estable para el mismo (cliente, eNCF) y
    /// distinta entre clientes y entre eNCF.
    /// </summary>
    public static long LockKey(int clientId, string eNcf)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"ecf-emit|{clientId}|{eNcf}"));
        return BitConverter.ToInt64(hash, 0);
    }

    /// <summary>
    /// En vuelo: creado, validando, listo para firmar, firmando, firmado, pendiente de envío,
    /// enviando o enviado (ids 1, 2, 4, 5, 6, 7, 8, 9). Sigue vivo mientras su última actividad
    /// sea reciente o la DGII ya lo tenga (TrackId): ese lo resuelve el job de seguimiento.
    /// </summary>
    private static bool IsLive(EcfEmitExisting document, DateTime nowUtc, TimeSpan staleAfter)
    {
        var inFlight = document.EcfStatusId is 1 or 2 or 4 or 5 or 6 or 7 or 8 or 9;

        if (!inFlight)
            return false;

        return document.HasTrackId || nowUtc - document.LastActivityUtc < staleAfter;
    }
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitDecisionTests"`
Expected: `Passed!`, todas en verde.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfEmitDecision.cs ZynstormECFPlatform.Tests/Production/EcfEmitDecisionTests.cs
git commit -m "feat(ecf): decisión idempotente de emit (crear, repetir o reintentar)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Respuesta de un replay

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs` (DTO `ReceivedEcfEmissionResultDto`)
- Create: `ZynstormECFPlatform.Services/Production/EcfEmitReplay.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfEmitReplayTests.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`

**Interfaces:**
- Consumes: `EcfLookupResponse`, `EcfLookupState` (existentes).
- Produces:
  - Propiedades nuevas en `ReceivedEcfEmissionResultDto`: `bool Replayed`, `int Attempt`.
  - `static ReceivedEcfEmissionResultDto EcfEmitReplay.ToResult(EcfLookupResponse lookup, int attempt)`

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfEmitReplayTests.cs`:

```csharp
using System.Text.Json;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitReplayTests
{
    private static EcfLookupResponse Lookup(EcfLookupState state) => new()
    {
        ENcf = "E320000000098",
        State = state,
        IsUsable = true,
        TrackId = "TRACK-1",
        SecurityCode = "ABCDEF",
        SignatureDate = "07-10-2026 09:01:23",
        QrUrl = "https://fc.dgii.gov.do/ecf/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000098&MontoTotal=140&CodigoSeguridad=ABCDEF",
        EcfDocumentId = 123
    };

    [Fact]
    public void Accepted_IsASuccessfulReplayWithTheStoredData()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.Accepted), attempt: 2);

        Assert.True(result.Replayed);
        Assert.Equal(2, result.Attempt);
        Assert.True(result.Success);
        Assert.False(result.IsPending);
        Assert.False(result.IsAcceptedConditional);
        Assert.Equal("TRACK-1", result.TrackId);
        Assert.Equal("ABCDEF", result.SecurityCode);
        Assert.Equal("07-10-2026 09:01:23", result.SignatureDate);
        Assert.Contains("ConsultaTimbreFC", result.QrUrl);
        Assert.Equal(123, result.EcfDocumentId);
        Assert.Equal("E320000000098", result.ENcf);
        Assert.Equal(32, result.EcfType);
    }

    [Fact]
    public void AcceptedConditional_IsFlaggedAndNotASuccess()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.AcceptedConditional), attempt: 1);

        Assert.False(result.Success);
        Assert.True(result.IsAcceptedConditional);
        Assert.False(result.IsPending);
    }

    [Fact]
    public void Pending_IsPendingAndNotASuccess()
    {
        var result = EcfEmitReplay.ToResult(Lookup(EcfLookupState.Pending), attempt: 1);

        Assert.False(result.Success);
        Assert.True(result.IsPending);
        Assert.True(result.Replayed);
    }

    [Theory]
    [InlineData(EcfLookupState.Accepted)]
    [InlineData(EcfLookupState.AcceptedConditional)]
    [InlineData(EcfLookupState.Pending)]
    public void Message_NeverContainsRejectionWords(EcfLookupState state)
    {
        // La librería de e-CF busca "Rechaz" en el mensaje para clasificar un rechazo.
        var message = EcfEmitReplay.ToResult(Lookup(state), attempt: 1).Message;

        Assert.False(string.IsNullOrWhiteSpace(message));
        Assert.DoesNotContain("Rechaz", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MissingSecurityData_BecomesEmptyStrings()
    {
        var lookup = Lookup(EcfLookupState.Accepted) with { SecurityCode = null, SignatureDate = null, QrUrl = null, TrackId = null };

        var result = EcfEmitReplay.ToResult(lookup, attempt: 1);

        Assert.Equal(string.Empty, result.SecurityCode);
        Assert.Equal(string.Empty, result.SignatureDate);
        Assert.Equal(string.Empty, result.QrUrl);
        Assert.Equal(string.Empty, result.TrackId);
    }

    [Fact]
    public void Serializes_ReplayedAndAttempt()
    {
        var json = JsonSerializer.Serialize(
            EcfEmitReplay.ToResult(Lookup(EcfLookupState.Accepted), attempt: 3),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"replayed\":true", json);
        Assert.Contains("\"attempt\":3", json);
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitReplayTests"`
Expected: error de compilación (`EcfEmitReplay`, `Replayed` no existen).

- [ ] **Step 3: Agregar los campos al DTO de respuesta**

En `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs`, dentro de `ReceivedEcfEmissionResultDto`, después de la línea `public string QrUrl { get; set; } = string.Empty;` agregar:

```csharp

    /// <summary>La plataforma no transmitió: devolvió lo que ya tenía guardado para ese eNCF.</summary>
    public bool Replayed { get; set; }

    /// <summary>Cantidad de documentos de la plataforma con este eNCF, contando este.</summary>
    public int Attempt { get; set; }
```

- [ ] **Step 4: Implementar la traducción**

Crear `ZynstormECFPlatform.Services/Production/EcfEmitReplay.cs`:

```csharp
using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Services.Production;

public static class EcfEmitReplay
{
    /// <summary>
    /// Respuesta de <c>emit</c> cuando el eNCF ya tenía un documento aceptado o en proceso.
    /// Tiene la misma forma que una emisión normal para que la librería la clasifique igual:
    /// Success solo si está aceptado, IsPending si sigue en proceso. El mensaje no usa la
    /// palabra "Rechaz": la librería la toma como señal de rechazo.
    /// </summary>
    public static ReceivedEcfEmissionResultDto ToResult(EcfLookupResponse lookup, int attempt)
    {
        var result = new ReceivedEcfEmissionResultDto
        {
            Replayed = true,
            Attempt = attempt,
            Success = lookup.State == EcfLookupState.Accepted,
            IsPending = lookup.State == EcfLookupState.Pending,
            IsAcceptedConditional = lookup.State == EcfLookupState.AcceptedConditional,
            EcfDocumentId = lookup.EcfDocumentId,
            ENcf = lookup.ENcf,
            EcfType = NcfHelper.TryExtractEcfType(lookup.ENcf, out var ecfType) ? ecfType : 0,
            TrackId = lookup.TrackId ?? string.Empty,
            SecurityCode = lookup.SecurityCode ?? string.Empty,
            SignatureDate = lookup.SignatureDate ?? string.Empty,
            QrUrl = lookup.QrUrl ?? string.Empty,
            Message = Message(lookup)
        };

        return result;
    }

    private static string Message(EcfLookupResponse lookup)
    {
        var track = string.IsNullOrWhiteSpace(lookup.TrackId) ? string.Empty : $" TrackId: {lookup.TrackId}";

        return lookup.State switch
        {
            EcfLookupState.Accepted =>
                $"El comprobante ya estaba aceptado; se devolvió el resultado guardado, sin volver a enviarlo.{track}",
            EcfLookupState.AcceptedConditional =>
                $"El comprobante ya estaba aceptado con observaciones; se devolvió el resultado guardado, sin volver a enviarlo.{track}",
            _ =>
                $"El comprobante ya fue recibido y sigue en proceso; se devolvió su estado actual, sin volver a enviarlo.{track}"
        };
    }
}
```

- [ ] **Step 5: Que un replay pendiente responda 200**

En `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`, dentro de `EmitEcf`, reemplazar:

```csharp
                if (result.IsPending)
                    return StatusCode(StatusCodes.Status504GatewayTimeout, result);
```

por:

```csharp
                // Un replay pendiente no es un fallo de la plataforma: devolvió lo guardado.
                if (result.IsPending && !result.Replayed)
                    return StatusCode(StatusCodes.Status504GatewayTimeout, result);
```

- [ ] **Step 6: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitReplayTests"`
Expected: `Passed!`, todas en verde.

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs ZynstormECFPlatform.Services/Production/EcfEmitReplay.cs ZynstormECFPlatform.Tests/Production/EcfEmitReplayTests.cs ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs
git commit -m "feat(ecf): respuesta de emit cuando el eNCF ya existe (replayed)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Acceso a datos del replay y transacción READ COMMITTED

**Files:**
- Modify: `ZynstormECFPlatform.Abstractions/Data/IUnitOfWork.cs`
- Modify: `ZynstormECFPlatform.Data/UnitOfWork.cs`
- Modify: `ZynstormECFPlatform.Services/Production/EcfLookupService.cs`

**Interfaces:**
- Consumes: `EcfEmitExisting` (Task 1), `EcfLookupLogic.ForClient / ResolveState / BuildResponse / ResolveEnvironment` (existentes).
- Produces:
  - `Task<TResult> IUnitOfWork.ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken = default)`
  - `Task<IReadOnlyList<EcfEmitExisting>> IEcfLookupService.ListExistingAsync(int clientId, string eNcf, CancellationToken cancellationToken = default)`
  - `Task<EcfLookupResponse> IEcfLookupService.DescribeDocumentAsync(int clientId, int ecfDocumentId, EcfLookupState state, int attempts, CancellationToken cancellationToken = default)`

Sin pruebas xUnit: es acceso a datos (el repo no tiene base de datos de prueba). Se verifica con el build; el comportamiento real se prueba en staging en la Task 7.

- [ ] **Step 1: Ampliar `IUnitOfWork`**

En `ZynstormECFPlatform.Abstractions/Data/IUnitOfWork.cs`, después del método `ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);` agregar:

```csharp

    /// <summary>
    /// Igual que la sobrecarga sin nivel, que usa Serializable. Con Serializable (y
    /// RepeatableRead) Postgres toma el snapshot en la primera sentencia, antes de que un
    /// pg_advisory_xact_lock termine de esperar; con ReadCommitted cada sentencia ve lo que ya
    /// se confirmó.
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implementarlo en `UnitOfWork`**

En `ZynstormECFPlatform.Data/UnitOfWork.cs`, reemplazar el método

```csharp
    public async Task BeginAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return;
        }

        await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken).ConfigureAwait(false);
    }
```

por:

```csharp
    public Task BeginAsync(CancellationToken cancellationToken = default)
        => BeginAsync(IsolationLevel.Serializable, cancellationToken);

    public async Task BeginAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction is not null)
        {
            return;
        }

        await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
    }
```

y, justo antes de `public async Task BeginAsync...` (ahora `public Task BeginAsync`), agregar:

```csharp
    public Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            await BeginAsync(isolationLevel, cancellationToken).ConfigureAwait(false);

            try
            {
                var result = await operation(cancellationToken).ConfigureAwait(false);
                await CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        });
    }

```

- [ ] **Step 3: Reescribir `EcfLookupService`**

Reemplazar todo el contenido de `ZynstormECFPlatform.Services/Production/EcfLookupService.cs` por:

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

    /// <summary>
    /// Los documentos de ese cliente con ese eNCF, reducidos a lo que necesita
    /// <see cref="EcfEmitDecision"/>. Lista vacía si no hay ninguno.
    /// </summary>
    Task<IReadOnlyList<EcfEmitExisting>> ListExistingAsync(int clientId, string eNcf, CancellationToken cancellationToken = default);

    /// <summary>
    /// La respuesta (estado, TrackId, código de seguridad, fecha de firma y QR) de un
    /// documento concreto, con el estado que decidió quien llama.
    /// </summary>
    Task<EcfLookupResponse> DescribeDocumentAsync(
        int clientId,
        int ecfDocumentId,
        EcfLookupState state,
        int attempts,
        CancellationToken cancellationToken = default);
}

public class EcfLookupService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients,
    IConfiguration configuration) : IEcfLookupService
{
    private sealed record TransmissionRow(int EcfDocumentId, string TrackId, string? ResponsePayload, DateTime SentAtUtc);

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

        var byDocument = await LoadTransmissionsAsync(found.Select(document => document.EcfDocumentId).ToList(), cancellationToken);

        var candidates = found
            .Select(document => new EcfLookupCandidate(
                document.EcfDocumentId,
                EcfLookupLogic.ResolveState(document.EcfStatusId, LastPayload(byDocument, document.EcfDocumentId)),
                document.RegisteredAt))
            .ToList();

        var best = EcfLookupLogic.PickBest(candidates)!;

        return await DescribeDocumentAsync(clientId, best.EcfDocumentId, best.State, found.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<EcfEmitExisting>> ListExistingAsync(
        int clientId,
        string eNcf,
        CancellationToken cancellationToken = default)
    {
        var found = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, eNcf))
            .Select(document => new
            {
                document.EcfDocumentId,
                document.EcfStatusId,
                document.RegisteredAt,
                document.LastUpdateUtc
            })
            .ToListAsync(cancellationToken);

        if (found.Count == 0)
            return [];

        var byDocument = await LoadTransmissionsAsync(found.Select(document => document.EcfDocumentId).ToList(), cancellationToken);

        return found
            .Select(document => new EcfEmitExisting(
                document.EcfDocumentId,
                document.EcfStatusId,
                EcfLookupLogic.ResolveState(document.EcfStatusId, LastPayload(byDocument, document.EcfDocumentId)),
                document.LastUpdateUtc ?? document.RegisteredAt,
                byDocument.TryGetValue(document.EcfDocumentId, out var list)
                    && list.Any(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))))
            .ToList();
    }

    public async Task<EcfLookupResponse> DescribeDocumentAsync(
        int clientId,
        int ecfDocumentId,
        EcfLookupState state,
        int attempts,
        CancellationToken cancellationToken = default)
    {
        var detail = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == ecfDocumentId && document.ClientId == clientId)
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
            .Where(xml => xml.EcfDocumentId == ecfDocumentId)
            .OrderByDescending(xml => xml.EcfXmlDocumentId)
            .Select(xml => xml.XmlSigned)
            .FirstOrDefaultAsync(cancellationToken);

        var byDocument = await LoadTransmissionsAsync([ecfDocumentId], cancellationToken);

        var trackId = byDocument.TryGetValue(ecfDocumentId, out var list)
            ? list.FirstOrDefault(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))?.TrackId
            : null;

        var source = new EcfLookupSource(
            ecfDocumentId,
            detail.Ncf,
            detail.Total,
            detail.IssueDateUtc,
            detail.SignatureDateTime,
            issuerRnc,
            detail.CustomerRnc,
            trackId,
            signedXml);

        var environment = EcfLookupLogic.ResolveEnvironment(configuration["EcfXmlValidation:TargetDgiiEnvironment"]);

        return EcfLookupLogic.BuildResponse(source, state, attempts, environment);
    }

    /// <summary>Transmisiones de esos documentos, la más reciente primero dentro de cada uno.</summary>
    private async Task<Dictionary<int, List<TransmissionRow>>> LoadTransmissionsAsync(
        List<int> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await transmissions.Table
            .AsNoTracking()
            .Where(transmission => documentIds.Contains(transmission.EcfDocumentId))
            .Select(transmission => new TransmissionRow(
                transmission.EcfDocumentId,
                transmission.TrackId,
                transmission.ResponsePayload,
                transmission.SentAtUtc))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.EcfDocumentId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.SentAtUtc).ToList());
    }

    private static string? LastPayload(Dictionary<int, List<TransmissionRow>> byDocument, int documentId) =>
        byDocument.TryGetValue(documentId, out var list) ? list[0].ResponsePayload : null;
}
```

- [ ] **Step 4: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx`
Expected: `Build succeeded`, 0 errores. Si algún mock de `IUnitOfWork` en las pruebas deja de compilar, es porque implementa la interfaz a mano: agregar el método nuevo lanzando `NotImplementedException`.

- [ ] **Step 5: Ejecutar las pruebas existentes**

Run: `dotnet test ZynstormECFPlatform.Tests`
Expected: `Passed!` sin fallos (incluidas las `EcfLookupLogicTests`).

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Abstractions/Data/IUnitOfWork.cs ZynstormECFPlatform.Data/UnitOfWork.cs ZynstormECFPlatform.Services/Production/EcfLookupService.cs
git commit -m "feat(ecf): lectura de documentos existentes y transacción READ COMMITTED

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: `emit` idempotente

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

**Interfaces:**
- Consumes: `IEcfLookupService.ListExistingAsync / DescribeDocumentAsync` (Task 3), `EcfEmitDecision.Decide / LockKey` (Task 1), `EcfEmitReplay.ToResult` (Task 2), `IUnitOfWork.ExecuteInTransactionAsync(..., IsolationLevel, ...)` (Task 3).
- Produces: `ProcessAsync` que respeta la regla de idempotencia y llena `Attempt` / `Replayed`.

Sin pruebas xUnit (orquestación con base de datos): la lógica que decide ya está probada en las Tasks 1-2. Se verifica con el build y en staging (Task 7).

- [ ] **Step 1: Inyectar `IEcfLookupService`**

En `ReceivedEcfProductionService.cs`:

1. Agregar `using System.Data;` junto a los demás `using` de arriba.
2. Después de la línea `private readonly IClientUsageService _clientUsageService;` agregar:

```csharp
    private readonly IEcfLookupService _lookupService;
```

3. En el constructor, reemplazar `IClientUsageService clientUsageService)` por:

```csharp
        IClientUsageService clientUsageService,
        IEcfLookupService lookupService)
```

4. Y después de `_clientUsageService = clientUsageService;` agregar:

```csharp
        _lookupService = lookupService;
```

- [ ] **Step 2: Reclamar el documento bajo candado**

En `ProcessAsync`, reemplazar:

```csharp
        var ecfDocument = await CreateEcfDocumentAsync(dto, client, clientBranch, apiKey, currency, ecfTypeEntity);
        resultDto.EcfDocumentId = ecfDocument.EcfDocumentId;
```

por:

```csharp
        var claim = await ClaimDocumentAsync(
            client.ClientId, eNcf, dto, client, clientBranch, apiKey, currency, ecfTypeEntity, cancellationToken);

        // El eNCF ya tenía un documento aceptado o en proceso: no se transmite de nuevo.
        if (claim.Replay is not null)
            return claim.Replay;

        var ecfDocument = claim.Document!;
        resultDto.Attempt = claim.Attempt;
        resultDto.EcfDocumentId = ecfDocument.EcfDocumentId;
```

- [ ] **Step 3: Agregar `ClaimDocumentAsync`**

Dentro de la clase, justo antes de `public const string ClientInactiveMessage`, agregar:

```csharp
    private sealed record EmitClaim(EcfDocument? Document, ReceivedEcfEmissionResultDto? Replay, int Attempt);

    /// <summary>
    /// Decide y reclama el documento en una transacción corta READ COMMITTED protegida por un
    /// candado de Postgres por (cliente, eNCF). La petición que llegue mientras otra decide
    /// espera el candado y, cuando lo obtiene, ya ve el documento que la otra creó: así dos
    /// emisiones simultáneas del mismo eNCF no crean dos envíos. No hay índice único porque
    /// producción ya tiene eNCF repetidos.
    /// </summary>
    private async Task<EmitClaim> ClaimDocumentAsync(
        int clientId,
        string eNcf,
        EcfInvoiceRequestDto dto,
        Client client,
        ClientBranche? clientBranch,
        ApiKey apiKey,
        Currency currency,
        Core.Entities.EcfType ecfType,
        CancellationToken cancellationToken)
    {
        var staleAfter = TimeSpan.FromMinutes(Math.Clamp(
            _configuration.GetValue<int?>("EcfIdempotency:StaleInFlightMinutes") ?? 5,
            1,
            120));

        return await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _ecfDocumentService.ExecuteAsync(
                "SELECT pg_advisory_xact_lock(@LockKey)",
                new { LockKey = EcfEmitDecision.LockKey(clientId, eNcf) },
                token);

            var existing = await _lookupService.ListExistingAsync(clientId, eNcf, token);
            var decision = EcfEmitDecision.Decide(existing, DateTime.UtcNow, staleAfter);

            if (decision.Action == EcfEmitAction.Replay)
            {
                var documentId = decision.ReplayDocumentId!.Value;
                var state = decision.ReplayState!.Value;

                var lookup = await _lookupService.DescribeDocumentAsync(clientId, documentId, state, existing.Count, token);
                var replay = EcfEmitReplay.ToResult(lookup, existing.Count);

                var current = existing.First(document => document.EcfDocumentId == documentId);
                var note = $"Reenvío ignorado: el eNCF {eNcf} ya tiene un documento en estado {state}; no se transmitió de nuevo.";

                await _systemLogService.InsertAsync(new SystemLog
                {
                    ClientId = clientId,
                    EcfDocumentId = documentId,
                    LogLevel = "Information",
                    Message = note,
                    CreateAtUtc = DateTime.UtcNow
                });

                await _ecfStatusHistoryService.InsertAsync(new EcfStatusHistory
                {
                    EcfDocumentId = documentId,
                    EcfStatusId = current.EcfStatusId,
                    Message = note
                });

                return new EmitClaim(null, replay, existing.Count);
            }

            var document = await CreateEcfDocumentAsync(dto, client, clientBranch, apiKey, currency, ecfType);

            return new EmitClaim(document, null, existing.Count + 1);
        }, IsolationLevel.ReadCommitted, cancellationToken);
    }

```

- [ ] **Step 4: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx`
Expected: `Build succeeded`, 0 errores. Si alguna prueba construye `ReceivedEcfProductionService` con `new`, agregarle el argumento `Mock.Of<IEcfLookupService>()`.

- [ ] **Step 5: Ejecutar las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests`
Expected: `Passed!` sin fallos.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs
git commit -m "feat(ecf): emit idempotente por (cliente, eNCF) con candado de Postgres

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Guardar el TrackId apenas la DGII responde

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

**Interfaces:**
- Consumes: `IEcfTransmissionService.InsertAsync / UpdateAsync`, `MarkDocumentAsync`, `BuildTransmissionResponseMessage`, `TrimTo` (existentes en el servicio).
- Produces: `BeginTransmissionAsync(EcfDocument, DgiiTransmissionResult, string signedXml) → EcfTransmission` y `CompleteTransmissionAsync(EcfTransmission, DgiiTransmissionResult, int statusId, DgiiStatusResponse)`.

- [ ] **Step 1: Agregar los dos métodos**

En `ReceivedEcfProductionService.cs`, justo después del método `SaveTransmissionAsync` y antes de `SaveValidationTransmissionAsync`, agregar:

```csharp
    /// <summary>
    /// La DGII ya recibió el documento y devolvió su TrackId: se guarda ahora, antes de esperar
    /// el estado final (hasta 60 s). Si la petición se corta en esa espera, el TrackId ya está
    /// en la base de datos y el job de seguimiento y la consulta por eNCF lo encuentran.
    /// </summary>
    private async Task<EcfTransmission> BeginTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        string signedXml)
    {
        var record = new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = 1,
            RequestPayload = signedXml,
            ResponsePayload = JsonSerializer.Serialize(new { transmission, status = (DgiiStatusResponse?)null }),
            EcfStatusId = 9,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = TrimTo(transmission.Codigo?.ToString() ?? string.Empty, 50),
            ResponseMessage = BuildTransmissionResponseMessage(transmission, null),
            Success = false
        };

        await _ecfTransmissionService.InsertAsync(record);
        await MarkDocumentAsync(ecfDocument, 9, $"Recibido por la DGII. TrackId: {transmission.TrackId}");

        return record;
    }

    /// <summary>Actualiza con el estado final la transmisión que <see cref="BeginTransmissionAsync"/> guardó.</summary>
    private async Task CompleteTransmissionAsync(
        EcfTransmission record,
        DgiiTransmissionResult transmission,
        int statusId,
        DgiiStatusResponse status)
    {
        record.EcfStatusId = statusId;
        record.ResponsePayload = JsonSerializer.Serialize(new { transmission, status });
        record.ResponseCode = TrimTo(status.Codigo ?? transmission.Codigo?.ToString() ?? string.Empty, 50);
        record.ResponseMessage = BuildTransmissionResponseMessage(transmission, status);
        record.Success = statusId == 10;

        await _ecfTransmissionService.UpdateAsync(record);
    }

```

- [ ] **Step 2: Usarlos en la rama de producción**

En `ContinueProcessingAsync`, reemplazar:

```csharp
        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            var status = await WaitForFinalDgiiStatusAsync(
                targetEnvironment, token, transmission.TrackId, cancellationToken);
```

por:

```csharp
        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml);

            var status = await WaitForFinalDgiiStatusAsync(
                targetEnvironment, token, transmission.TrackId, cancellationToken);
```

y, más abajo en el mismo bloque, reemplazar:

```csharp
            await SaveTransmissionAsync(ecfDocument, transmission, statusId, signedXml, status);
```

por:

```csharp
            await CompleteTransmissionAsync(transmissionRecord, transmission, statusId, status);
```

- [ ] **Step 3: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx` y luego `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos. Si el compilador marca `SaveTransmissionAsync` con un parámetro `status` sin uso, no es un error: la otra rama (`!transmission.Success` y la rama sin TrackId) todavía lo usa.

- [ ] **Step 4: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs
git commit -m "fix(ecf): guardar el TrackId apenas la DGII responde, antes de esperar el estado final

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Cronómetro por fase

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfEmitTimings.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfEmitTimingsTests.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

**Interfaces:**
- Produces:
  - `new EcfEmitTimings(Func<long>? elapsedMilliseconds = null)`
  - `void Mark(string phase)`: registra lo transcurrido desde la marca anterior.
  - `string ToString()`: `fase=Nms, fase=Nms, total=Nms`.

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfEmitTimingsTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfEmitTimingsTests
{
    [Fact]
    public void Mark_RecordsTheTimeSinceThePreviousMark()
    {
        var elapsed = 0L;
        var timings = new EcfEmitTimings(() => elapsed);

        elapsed = 40;
        timings.Mark("consultas_previas");
        elapsed = 100;
        timings.Mark("firma");

        Assert.Equal("consultas_previas=40ms, firma=60ms, total=100ms", timings.ToString());
    }

    [Fact]
    public void WithoutMarks_OnlyTheTotalIsReported()
    {
        var timings = new EcfEmitTimings(() => 7);

        Assert.Equal("total=7ms", timings.ToString());
    }

    [Fact]
    public void RealClock_NeverGoesNegative()
    {
        var timings = new EcfEmitTimings();
        timings.Mark("fase");

        Assert.DoesNotContain("-", timings.ToString());
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitTimingsTests"`
Expected: error de compilación (`EcfEmitTimings` no existe).

- [ ] **Step 3: Implementar**

Crear `ZynstormECFPlatform.Services/Production/EcfEmitTimings.cs`:

```csharp
using System.Diagnostics;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Cronómetro por fase de una emisión. Cada <see cref="Mark"/> guarda lo transcurrido desde
/// la marca anterior; el resultado se escribe en el log del documento para medir antes de
/// optimizar.
/// </summary>
public sealed class EcfEmitTimings
{
    private readonly Func<long> _elapsedMilliseconds;
    private readonly List<(string Phase, long Milliseconds)> _laps = [];
    private long _last;

    public EcfEmitTimings(Func<long>? elapsedMilliseconds = null)
    {
        if (elapsedMilliseconds is null)
        {
            var watch = Stopwatch.StartNew();
            elapsedMilliseconds = () => watch.ElapsedMilliseconds;
        }

        _elapsedMilliseconds = elapsedMilliseconds;
    }

    public void Mark(string phase)
    {
        var now = _elapsedMilliseconds();
        _laps.Add((phase, now - _last));
        _last = now;
    }

    public override string ToString()
    {
        var total = _elapsedMilliseconds();
        var phases = _laps.Select(lap => $"{lap.Phase}={lap.Milliseconds}ms");

        return string.Join(", ", phases.Append($"total={total}ms"));
    }
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfEmitTimingsTests"`
Expected: `Passed!`, 3 pruebas en verde.

- [ ] **Step 5: Conectarlo a `emit`**

En `ReceivedEcfProductionService.cs`:

1. En `ProcessAsync`, justo después de `var resultDto = new ReceivedEcfEmissionResultDto();` agregar:

```csharp
        var timings = new EcfEmitTimings();
```

2. Justo después del bloque que asigna `resultDto.Attempt = claim.Attempt;` y `resultDto.EcfDocumentId = ...` agregar:

```csharp
        timings.Mark("consultas_previas");
```

3. Reemplazar:

```csharp
        try
        {
            return await ContinueProcessingAsync(
                resultDto, dto, client, apiKey, ecfDocument, ecfType, targetEnvironment,
                issuerRnc, eNcf, statusDelayMilliseconds, cancellationToken);
        }
```

por:

```csharp
        try
        {
            var processed = await ContinueProcessingAsync(
                resultDto, dto, client, apiKey, ecfDocument, ecfType, targetEnvironment,
                issuerRnc, eNcf, statusDelayMilliseconds, timings, cancellationToken);

            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"Tiempos de emit [{eNcf}]: {timings}");

            return processed;
        }
```

4. En la firma de `ContinueProcessingAsync`, reemplazar `int statusDelayMilliseconds,\n        CancellationToken cancellationToken)` (el que pertenece a `ContinueProcessingAsync`, el primero que aparece con `string eNcf,` antes) por:

```csharp
        int statusDelayMilliseconds,
        EcfEmitTimings timings,
        CancellationToken cancellationToken)
```

5. Dentro de `ContinueProcessingAsync`, agregar estas marcas (una línea cada una, en el punto indicado):
   - Después de `resultDto.XsdErrors = xsdErrors;` y su `if (xsdErrors.Count > 0) { ... }`, antes del cálculo de `decryptedSecretKey`: `timings.Mark("generacion_xsd");`
   - Después de `resultDto.XmlValidation = BuildAcceptedValidationResult(...)`: `timings.Mark("firma");`
   - Después de la línea `await _unitOfWork.SaveChangesAsync();` que sigue a `Enviando e-CF a DGII` (la segunda del método): `timings.Mark("guardado_previo");`
   - Después de `await AddDgiiResponseLogAsync(ecfDocument, client.ClientId, "recepcion", targetEnvironment, transmission);`: `timings.Mark("recepcion_dgii");`
   - Después de `_cacheService.Set($"EcfStatus_{transmission.TrackId}", status, TimeSpan.FromHours(1));`: `timings.Mark("espera_estado_final");`

- [ ] **Step 6: Compilar y probar todo**

Run: `dotnet build ZynstormECFPlatform.slnx` y luego `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos.

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfEmitTimings.cs ZynstormECFPlatform.Tests/Production/EcfEmitTimingsTests.cs ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs
git commit -m "feat(ecf): tiempos por fase de emit en el log del documento

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Verificación final y prueba en staging

**Files:** ninguno nuevo.

- [ ] **Step 1: Build y pruebas completas**

Run: `dotnet build ZynstormECFPlatform.slnx` y `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos.

- [ ] **Step 2: Comprobar que no se coló nada ajeno**

Run: `git status --short`
Expected: solo ` M .github/workflows/deploy.yml` (el cambio previo, sin tocar).

- [ ] **Step 3: Prueba manual en staging (después de desplegar)**

Con la API key de un cliente de pruebas de `ecfstaging.zynstorm.com` y un eNCF de prueba nuevo:

1. `POST v1/Ecf/emit` con el comprobante: debe aceptarse. En la tabla `EcfDocument` hay una fila con ese eNCF y en `SystemLog` un registro «Tiempos de emit».
2. Repetir **exactamente** el mismo `POST`: la respuesta trae `replayed: true`, `attempt: 1`, el mismo `trackId`, `securityCode` y `qrUrl`, y **no** aparece una fila nueva en `EcfDocument`. En `SystemLog` hay «Reenvío ignorado».
3. Lanzar dos `POST` simultáneos con otro eNCF nuevo: queda **una sola** fila en `EcfDocument` y la segunda respuesta es `replayed: true`.
4. Con un eNCF cuyo documento quedó rechazado (provocar un rechazo con un dato inválido), corregir y reenviar: se crea una fila nueva, `attempt: 2`.
5. `GET v1/Ecf/by-ncf/{eNcf}` devuelve el documento aceptado con `attempts` igual al número de filas.

Si el paso 3 deja dos filas, el candado no está sirviendo: revisar que la transacción sea `READ COMMITTED` y que `ExecuteAsync` use la transacción activa (`ActiveDbTransaction`).

- [ ] **Step 4: Reportar**

Informar el resultado de cada comprobación del paso 3. No hacer push hasta que el usuario lo pida.

---

## Self-Review

**Cobertura del spec**
- Sección 1 (decisión): Task 1, con la tabla completa y el umbral `staleAfter` (`StaleThreshold_IsExclusive`).
- Sección 2 (candado, READ COMMITTED): Tasks 3 y 4.
- Sección 3 (replay, rastro, HTTP 200): Tasks 2 y 4.
- Sección 4 (retry con documento nuevo, `attempt` = cantidad de documentos): Task 4 (`existing.Count + 1`).
- Sección 5b, primer punto (TrackId al recibir): Task 5.
- Rendimiento, fase 0 (cronómetro): Task 6.
- **Fuera de este plan (parte 2):** modo diferido (sección 5), `EcfTransmitJob`, reintentos de transporte (5b), 1209/75 en reintento propio, QR en toda respuesta (sección 6), cachés, cola propia, `deferred=true`. **Parte 3:** librería.

**Placeholders:** ninguno; cada paso con código trae el código completo.

**Consistencia de tipos:** `EcfEmitExisting`, `EcfEmitDecisionResult`, `EcfEmitAction`, `EcfEmitTimings`, `EcfLookupState`, `EcfLookupResponse` y las firmas de `IEcfLookupService` son las mismas en todas las tareas. `EmitClaim(EcfDocument?, ReceivedEcfEmissionResultDto?, int)` se define y se usa solo en la Task 4. `ExecuteAsync(string, object, CancellationToken)` es la sobrecarga de `IRepository` que usa la transacción activa.
