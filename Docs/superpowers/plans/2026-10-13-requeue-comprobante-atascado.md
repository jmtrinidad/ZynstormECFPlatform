# Re-encolar un comprobante atascado — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que un usuario pueda re-encolar desde el listado de facturas un comprobante atascado (reenviarlo o reconsultar su estado a la DGII), y que la plataforma recupere un TrackId perdido con la «Consulta de TrackId» de la DGII.

**Architecture:** Lógica pura y probada (`DgiiTrackIdPicker`, `DgiiTrackIdParser`, `EcfRequeuePolicy`, `EcfRetrackSchedule`); una llamada nueva a la DGII (`GetTrackIdsAsync`); la recuperación del TrackId se engancha en `TransmitDeferredAsync` y en la rama del 1209/75 (se extrae `TrackReceivedAsync` sin cambiar su comportamiento); un job nuevo `EcfRetrackJob` que carga el certificado desde la base; un servicio `EcfRequeueService` con un endpoint `POST …/requeue`; y un botón en la lista de facturas.

**Tech Stack:** .NET 10, EF Core (Npgsql), Hangfire, xunit; Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-10-13-requeue-comprobante-atascado-design.md`

## Global Constraints
- **Backend: solo en la rama `feature/emit-idempotente`.** Depende de `EcfTransmitJob` y del envío diferido. **No** replicar a `Staging` ni `main`. Antes de cada tarea ejecutar `git branch --show-current` y confirmar que es `feature/emit-idempotente` (el usuario a veces deja el repo en otra rama).
- **Frontend: rama `Staging`** (repo `ZynstormECFPlatform-FrontEnd`).
- **Sin migración.** Todo se calcula con lo que ya se guarda.
- **No cambia** el comportamiento de `emit`, de la idempotencia ni del modo síncrono: la extracción de `TrackReceivedAsync` es un movimiento de código sin cambios de lógica (salvo las dos adiciones de recuperación de TrackId descritas).
- Umbrales: cooldown 60 s; `EcfIdempotency:StaleInFlightMinutes` (5 por defecto) para estados 7/8/9 sin TrackId; 2 min para reconsultar con TrackId. Reconsulta: espera 5 s (intentos 1-3), 30 s (4-6), 2 min (7-9), máximo 10 intentos.
- Estados: 3 `ValidationFailed`, 7 `SendPending`, 8 `Sending`, 9 `Sent`, 10 `Accepted`, 11 `Rejected`, 12 `Error`, 13 `Cancelled`.
- Un manual `Transmit` usa `attemptNumber = max(2, transmisiones + 1)`.
- La DGII no publica la consulta de TrackId en `CerteCF` ni existe con `UseInternalValidator`: en esos casos `Available = false` y todo sigue como hoy.
- Comandos (desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`): build `dotnet build ZynstormECFPlatform.slnx -nologo -v q`; tests `dotnet test ZynstormECFPlatform.Tests -nologo`. Frontend (desde `…\ZynstormECFPlatform-FrontEnd`): `npx tsc --noEmit`.
- Los commits terminan con:
  ```
  Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
  Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN
  ```

## File Structure

Backend (`ZynstormECFPlatform`):
- Modify `ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs` (modelos + `GetTrackIdsAsync`).
- Create `ZynstormECFPlatform.Services/Production/DgiiTrackIdPicker.cs`, `DgiiTrackIdParser.cs`, `EcfRequeuePolicy.cs`, `EcfRetrackSchedule.cs`, `EcfRequeueService.cs`.
- Modify `ZynstormECFPlatform.Services/DgiiTransmissionService.cs`, `Production/ReceivedEcfProductionService.cs`, `Production/IReceivedEcfProductionService.cs`, `Jobs/EcfTrackingJob.cs`, `Extensions/ServiceCollectionExtensions.cs`.
- Create `ZynstormECFPlatform.Services/Jobs/EcfRetrackJob.cs`.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/EcfDocumentController.cs`, `ZynstormECFPlatform.Web.Api/appsettings.json`; `ZynstormECFPlatform.Dtos/EcfDocumentDtos.cs`.
- Tests (`ZynstormECFPlatform.Tests/Production/`): `DgiiTrackIdPickerTests.cs`, `DgiiTrackIdParserTests.cs`, `DgiiTrackIdsServiceTests.cs`, `EcfRequeuePolicyTests.cs`, `EcfRetrackScheduleTests.cs`.

Frontend (`ZynstormECFPlatform-FrontEnd`): modify `services/invoice.service.ts`, `app/facturas/page.tsx`.

---

## Task 1: Modelos, selector y lector de la «Consulta de TrackId» (lógica pura)

**Files:**
- Modify: `ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs`
- Create: `ZynstormECFPlatform.Services/Production/DgiiTrackIdPicker.cs`, `ZynstormECFPlatform.Services/Production/DgiiTrackIdParser.cs`
- Test: `ZynstormECFPlatform.Tests/Production/DgiiTrackIdPickerTests.cs`, `ZynstormECFPlatform.Tests/Production/DgiiTrackIdParserTests.cs`

**Interfaces:**
- Produces: `DgiiTrackIdEntry { string TrackId; string? Estado; string? FechaRecepcion }`, `DgiiTrackIdsResult { bool Available; string? Error; List<DgiiTrackIdEntry> Entries }`, `DgiiTrackIdPicker.Pick(IEnumerable<DgiiTrackIdEntry> entries, string? excludeTrackId = null) : DgiiTrackIdEntry?`, `DgiiTrackIdParser.Parse(string? body) : List<DgiiTrackIdEntry>`.

- [ ] **Step 1: Agregar los modelos a la interfaz**

En `IDgiiTransmissionService.cs`, justo antes de `public class RfceMensaje`, agregar:
```csharp
/// <summary>Un TrackId que la DGII devolvió para un eNCF recibido (servicio «Consulta de TrackId e-CF»).</summary>
public class DgiiTrackIdEntry
{
    public string TrackId { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string? FechaRecepcion { get; set; }
}

public class DgiiTrackIdsResult
{
    /// <summary>false cuando la consulta no existe en ese ambiente (CerteCF, validador interno): no es un error.</summary>
    public bool Available { get; set; } = true;

    /// <summary>Texto del fallo (HTTP, red) o del motivo de no estar disponible; null si la consulta se hizo bien.</summary>
    public string? Error { get; set; }

    public List<DgiiTrackIdEntry> Entries { get; set; } = [];
}
```
Y dentro de `IDgiiTransmissionService`, tras `GetStatusAsync`:
```csharp
    /// <summary>
    /// Consulta de TrackId e-CF de la DGII: los TrackId que recibió para un eNCF del emisor.
    /// Sirve para recuperar el TrackId cuando la respuesta de la recepción se perdió.
    /// </summary>
    Task<DgiiTrackIdsResult> GetTrackIdsAsync(
        DgiiEnvironment environment,
        string token,
        string rncEmisor,
        string eNcf,
        CancellationToken cancellationToken = default);
```
(No compila hasta el Task 2: `DgiiTransmissionService` debe implementarlo. Se corrige allí; el build completo se verifica al final del Task 2.)

- [ ] **Step 2: Escribir los tests (fallan)**

`DgiiTrackIdPickerTests.cs`:
```csharp
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdPickerTests
{
    private static DgiiTrackIdEntry Entry(string trackId, string? estado, string? fecha = null) =>
        new() { TrackId = trackId, Estado = estado, FechaRecepcion = fecha };

    [Fact]
    public void Pick_Empty_ReturnsNull() => Assert.Null(DgiiTrackIdPicker.Pick([]));

    [Fact]
    public void Pick_IgnoresNotFoundAndEntriesWithoutTrackId()
    {
        var picked = DgiiTrackIdPicker.Pick([Entry("", "Aceptado"), Entry("T1", "No encontrado")]);

        Assert.Null(picked);
    }

    [Fact]
    public void Pick_PrefersAcceptedOverInProcessOverRejected()
    {
        var picked = DgiiTrackIdPicker.Pick([
            Entry("R", "Rechazado", "2026-10-13 12:00:00"),
            Entry("P", "En proceso", "2026-10-13 11:00:00"),
            Entry("A", "Aceptado", "2026-10-13 09:00:00")]);

        Assert.Equal("A", picked!.TrackId);
    }

    [Fact]
    public void Pick_AcceptedConditionalCountsAsAccepted()
    {
        var picked = DgiiTrackIdPicker.Pick([Entry("P", "En proceso"), Entry("C", "Aceptado Condicional")]);

        Assert.Equal("C", picked!.TrackId);
    }

    [Fact]
    public void Pick_SameRank_PicksTheMostRecentReception()
    {
        var picked = DgiiTrackIdPicker.Pick([
            Entry("OLD", "En proceso", "2026-10-13 09:00:00"),
            Entry("NEW", "En proceso", "2026-10-13 10:00:00")]);

        Assert.Equal("NEW", picked!.TrackId);
    }

    [Fact]
    public void Pick_ExcludesTheGivenTrackId()
    {
        var picked = DgiiTrackIdPicker.Pick(
            [Entry("DUP", "Rechazado"), Entry("ORIG", "En proceso")],
            excludeTrackId: "DUP");

        Assert.Equal("ORIG", picked!.TrackId);

        Assert.Null(DgiiTrackIdPicker.Pick([Entry("DUP", "Rechazado")], excludeTrackId: "dup"));
    }
}
```
`DgiiTrackIdParserTests.cs`:
```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdParserTests
{
    [Fact]
    public void Parse_List_ReadsEveryEntry()
    {
        var entries = DgiiTrackIdParser.Parse("""
            [{"trackId":"A","estado":"Aceptado","fechaRecepcion":"13/10/2026 10:00:00"},
             {"trackId":"B","estado":"Rechazado","fechaRecepcion":"13/10/2026 09:00:00"}]
            """);

        Assert.Equal(2, entries.Count);
        Assert.Equal("A", entries[0].TrackId);
        Assert.Equal("Aceptado", entries[0].Estado);
        Assert.Equal("13/10/2026 10:00:00", entries[0].FechaRecepcion);
    }

    [Fact]
    public void Parse_SingleObject_ReadsOneEntry()
    {
        var entries = DgiiTrackIdParser.Parse("""{"trackId":"A","estado":"En proceso","fechaRecepcion":"x"}""");

        Assert.Single(entries);
        Assert.Equal("A", entries[0].TrackId);
    }

    [Fact]
    public void Parse_PropertyNamesAreCaseInsensitive()
    {
        var entries = DgiiTrackIdParser.Parse("""{"TrackId":"A","Estado":"Aceptado"}""");

        Assert.Equal("A", Assert.Single(entries).TrackId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("""{"estado":"No encontrado"}""")]
    public void Parse_EmptyOrWithoutTrackId_ReturnsNoEntries(string? body) =>
        Assert.Empty(DgiiTrackIdParser.Parse(body));
}
```

- [ ] **Step 3: Implementar el selector y el lector**

`ZynstormECFPlatform.Services/Production/DgiiTrackIdPicker.cs`:
```csharp
using System.Globalization;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Elige, entre los TrackId que la DGII tiene para un eNCF, el que debe seguirse: prefiere el
/// aceptado (también el condicional), luego el que sigue en proceso y por último el rechazado; a
/// igual estado, el recibido más recientemente. Descarta «No encontrado» y los sin TrackId.
/// </summary>
public static class DgiiTrackIdPicker
{
    public static DgiiTrackIdEntry? Pick(IEnumerable<DgiiTrackIdEntry> entries, string? excludeTrackId = null) =>
        entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.TrackId))
            .Where(entry => !string.Equals(entry.TrackId, excludeTrackId, StringComparison.OrdinalIgnoreCase))
            .Where(entry => Normalize(entry.Estado) != "noencontrado")
            .Select((entry, index) => (Entry: entry, Index: index, Rank: Rank(entry.Estado), Date: ParseDate(entry.FechaRecepcion)))
            .OrderBy(item => item.Rank)
            .ThenByDescending(item => item.Date ?? DateTime.MinValue)
            .ThenByDescending(item => item.Index)
            .Select(item => item.Entry)
            .FirstOrDefault();

    private static int Rank(string? estado) => Normalize(estado) switch
    {
        "aceptado" or "aceptadocondicional" => 0,
        "enproceso" => 1,
        "rechazado" => 2,
        _ => 3
    };

    private static string Normalize(string? value) =>
        new string((value ?? string.Empty).Where(char.IsLetter).ToArray()).ToLowerInvariant();

    private static DateTime? ParseDate(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out var local))
            return local;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant)
            ? invariant
            : null;
    }
}
```
`ZynstormECFPlatform.Services/Production/DgiiTrackIdParser.cs`:
```csharp
using System.Text.Json;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>Lee la respuesta de la Consulta de TrackId: una lista, o un solo objeto (así lo muestra la guía de la DGII).</summary>
public static class DgiiTrackIdParser
{
    public static List<DgiiTrackIdEntry> Parse(string? body)
    {
        var entries = new List<DgiiTrackIdEntry>();
        if (string.IsNullOrWhiteSpace(body))
            return entries;

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                AddEntry(item, entries);
        }
        else
        {
            AddEntry(root, entries);
        }

        return entries;
    }

    private static void AddEntry(JsonElement element, List<DgiiTrackIdEntry> entries)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        var trackId = Read(element, "trackId");
        if (string.IsNullOrWhiteSpace(trackId))
            return;

        entries.Add(new DgiiTrackIdEntry
        {
            TrackId = trackId,
            Estado = Read(element, "estado"),
            FechaRecepcion = Read(element, "fechaRecepcion")
        });
    }

    private static string? Read(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Null => null,
                _ => property.Value.ToString()
            };
        }

        return null;
    }
}
```

- [ ] **Step 4: Commit (el build completo se verifica al terminar el Task 2)**

Los tests de este task se ejecutan junto con el Task 2 (la interfaz aún no compila hasta implementar `GetTrackIdsAsync`). No hacer commit todavía: continuar con el Task 2.

---

## Task 2: `GetTrackIdsAsync` en `DgiiTransmissionService` y configuración

**Files:**
- Modify: `ZynstormECFPlatform.Services/DgiiTransmissionService.cs`, `ZynstormECFPlatform.Web.Api/appsettings.json`
- Test: `ZynstormECFPlatform.Tests/Production/DgiiTrackIdsServiceTests.cs`

**Interfaces:**
- Consumes: modelos y `DgiiTrackIdParser` del Task 1; el campo privado `_httpClient`, `_configuration`, `_logger` y `LogDgiiRawResponse("GetStatus", environment, url, ecfType: null, eNcf: trackId, isRfce: false, response, responseString)` ya existentes.
- Produces: `DgiiTransmissionService.GetTrackIdsAsync(DgiiEnvironment, string token, string rncEmisor, string eNcf, CancellationToken) : Task<DgiiTrackIdsResult>`.

- [ ] **Step 1: Escribir los tests (fallan)**

`DgiiTrackIdsServiceTests.cs`:
```csharp
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdsServiceTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Last { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(respond(request));
        }
    }

    private static DgiiTransmissionService Service(HttpMessageHandler handler, Dictionary<string, string?>? settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "false",
                ["DgiiUrls:Test:ConsultaTrackIds"] = "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds"
            })
            .Build();

        return new DgiiTransmissionService(new HttpClient(handler), configuration, NullLogger<DgiiTransmissionService>.Instance);
    }

    [Fact]
    public async Task BuildsTheRequestAndParsesTheEntries()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""[{"trackId":"T1","estado":"Aceptado","fechaRecepcion":"x"}]""")
        });

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.True(result.Available);
        Assert.Null(result.Error);
        Assert.Equal("T1", Assert.Single(result.Entries).TrackId);
        Assert.Equal(
            "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds/api/trackids/consulta?rncemisor=132293894&encf=E320000000098",
            handler.Last!.RequestUri!.ToString());
        Assert.Equal("Bearer", handler.Last.Headers.Authorization!.Scheme);
        Assert.Equal("tok", handler.Last.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task WithoutConfiguredUrl_IsNotAvailable()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no debe llamar a la DGII"));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.CerteCF, "tok", "132293894", "E320000000098");

        Assert.False(result.Available);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task WithInternalValidator_IsNotAvailable()
    {
        var handler = new StubHandler(_ => throw new InvalidOperationException("no debe llamar a la DGII"));
        var settings = new Dictionary<string, string?>
        {
            ["EcfXmlValidation:UseInternalValidator"] = "true",
            ["DgiiUrls:Test:ConsultaTrackIds"] = "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds"
        };

        var result = await Service(handler, settings).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.False(result.Available);
    }

    [Fact]
    public async Task HttpFailure_IsReportedAsErrorButStillAvailable()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.True(result.Available);
        Assert.NotNull(result.Error);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task NetworkFailure_IsReportedAsError()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException("sin ruta"));

        var result = await Service(handler).GetTrackIdsAsync(DgiiEnvironment.Test, "tok", "132293894", "E320000000098");

        Assert.NotNull(result.Error);
        Assert.Empty(result.Entries);
    }
}
```

- [ ] **Step 2: Verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~DgiiTrackIds"`
Expected: error de compilación (`DgiiTransmissionService` no implementa `GetTrackIdsAsync`).

- [ ] **Step 3: Implementar**

En `DgiiTransmissionService.cs`, justo antes de `public async Task<DgiiTransmissionResult> SendArecfAsync(...)`:
```csharp
    public async Task<DgiiTrackIdsResult> GetTrackIdsAsync(
        DgiiEnvironment environment,
        string token,
        string rncEmisor,
        string eNcf,
        CancellationToken cancellationToken = default)
    {
        // El validador interno de staging no ofrece esta consulta.
        if (_configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator"))
        {
            return new DgiiTrackIdsResult { Available = false, Error = "El validador interno no ofrece la consulta de TrackId." };
        }

        // La DGII no la publica en certificación (CerteCF): sin URL configurada no existe.
        var baseUrl = _configuration[$"DgiiUrls:{environment}:ConsultaTrackIds"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new DgiiTrackIdsResult { Available = false, Error = $"No hay URL de consulta de TrackId para {environment}." };
        }

        var url = $"{baseUrl.TrimEnd('/')}/api/trackids/consulta?rncemisor={Uri.EscapeDataString(rncEmisor)}&encf={Uri.EscapeDataString(eNcf)}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            LogDgiiRawResponse("GetTrackIds", environment, url, ecfType: null, eNcf, isRfce: false, response, responseString);

            if (!response.IsSuccessStatusCode)
            {
                return new DgiiTrackIdsResult { Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}" };
            }

            return new DgiiTrackIdsResult { Entries = Production.DgiiTrackIdParser.Parse(responseString) };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            return new DgiiTrackIdsResult { Error = ex.Message };
        }
    }
```
(`Production.DgiiTrackIdParser` está en el namespace `ZynstormECFPlatform.Services.Production`; si el archivo no tiene `using`, la referencia calificada funciona porque `DgiiTransmissionService` está en `ZynstormECFPlatform.Services`.)

- [ ] **Step 4: Configurar las URLs**

En `ZynstormECFPlatform.Web.Api/appsettings.json`, dentro de `DgiiUrls`, agregar a `Test` y a `Production` (no a `CerteCF`):
```json
"Test": {
  ...,
  "ConsultaTrackIds": "https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds"
},
"Production": {
  ...,
  "ConsultaTrackIds": "https://ecf.dgii.gov.do/ecf/ConsultaTrackIds"
}
```
(`appsettings.Staging.json` solo redefine `Test` parcialmente; la configuración se combina, así que hereda la clave nueva.)

- [ ] **Step 5: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `0 Error(s)`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~DgiiTrackId"`
Expected: todos pasan (Picker 6, Parser 8 con las filas del `Theory`, Service 5).

- [ ] **Step 6: Commit**
```bash
git add ZynstormECFPlatform.Abstractions ZynstormECFPlatform.Services ZynstormECFPlatform.Web.Api/appsettings.json ZynstormECFPlatform.Tests
git commit -m "feat(ecf): consulta de TrackId de la DGII por eNCF (selector, lector y servicio)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```

---

## Task 3: Recuperar el TrackId perdido en la transmisión (extraer `TrackReceivedAsync`)

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

**Interfaces:**
- Consumes: `IDgiiTransmissionService.GetTrackIdsAsync`, `DgiiTrackIdPicker.Pick` (Tasks 1-2) y los métodos privados existentes `BeginTransmissionAsync`, `CompleteTransmissionAsync`, `MarkDocumentAsync`, `AddLogAsync`, `WaitForFinalDgiiStatusAsync`, `AddDgiiStatusLogAsync`, `EcfTransmitRetryPolicy.IsOwnDuplicate`.
- Produces (privados): `TrackReceivedAsync(...)` (bloque existente movido) y `TryAdoptExistingTrackIdAsync(...) : Task<bool>`.

> Este task **mueve** código existente sin cambiar su lógica (paso 1) y luego agrega dos usos de la recuperación (pasos 2-3). Se verifica con build y tests existentes; el comportamiento con la DGII real se prueba a mano en Staging (no hay base de datos de prueba en el repo).

- [ ] **Step 1: Extraer `TrackReceivedAsync` (movimiento sin cambios de lógica)**

En `TransmitAndTrackAsync`, el bloque
```csharp
        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml, attemptNumber);
            ... (todo el contenido) ...
            return TransmitStep.Completed;
        }
```
se reemplaza por:
```csharp
        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            await TrackReceivedAsync(
                resultDto, ecfDocument, clientId, transmission, signedXml, eNcf, issuerRnc,
                targetEnvironment, certBase64, certPass, token, attemptNumber,
                earlierAttemptsWithoutTrackId, timings, cancellationToken);

            return TransmitStep.Completed;
        }
```
y el contenido original (desde `var transmissionRecord = …` hasta el último `}` previo al `return TransmitStep.Completed;`) pasa a este método nuevo, justo debajo de `TransmitAndTrackAsync`, **idéntico salvo** (a) los dos `return TransmitStep.Completed;` internos, que no existen en el bloque original (el bloque solo termina con ese return final, ya reemplazado), y (b) la rama del 1209/75 que se amplía en el Step 2:
```csharp
    /// <summary>
    /// La DGII recibió el documento y devolvió un TrackId: se guarda, se espera el estado final y,
    /// si sigue en proceso, se programa el seguimiento. Lo usan el envío normal y la recuperación
    /// de un TrackId perdido.
    /// </summary>
    private async Task TrackReceivedAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        DgiiTransmissionResult transmission,
        string signedXml,
        string eNcf,
        string issuerRnc,
        DgiiEnvironment targetEnvironment,
        string certBase64,
        string certPass,
        string token,
        int attemptNumber,
        bool earlierAttemptsWithoutTrackId,
        EcfEmitTimings? timings,
        CancellationToken cancellationToken)
    {
        var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml, attemptNumber);

        var status = await WaitForFinalDgiiStatusAsync(
            targetEnvironment, token, transmission.TrackId, cancellationToken);
        _cacheService.Set($"EcfStatus_{transmission.TrackId}", status, TimeSpan.FromHours(1));
        timings?.Mark("espera_estado_final");
        await AddDgiiStatusLogAsync(ecfDocument, clientId, targetEnvironment, transmission.TrackId, status);
        resultDto.Status = status;
        resultDto.DgiiResponse = status;
        resultDto.IsAcceptedConditional = IsAcceptedConditionalDgiiStatus(status);
        resultDto.RequiresCorrection = RequiresCorrectionDgiiStatus(status);
        resultDto.Success = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase);
        var statusId = MapDgiiStatusToEcfStatus(status);
        resultDto.Message = resultDto.Success ? $"TrackId: {transmission.TrackId}" : BuildDgiiStatusError(status);

        // 1209/75 en un reintento propio: el primer envío probablemente sí llegó y la respuesta se
        // perdió. (cuerpo de la rama: ver Step 2)
        if (EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId))
        {
            // <Step 2 agrega aquí la recuperación del TrackId original>

            statusId = 9;
            resultDto.Success = false;
            resultDto.IsAcceptedConditional = false;
            resultDto.RequiresCorrection = false;
            resultDto.IsPending = true;
            resultDto.Status = null;
            resultDto.DgiiResponse = null;
            resultDto.Message = "La DGII ya recibió este comprobante en un envío anterior; falta confirmar su TrackId.";

            await AddLogAsync(
                ecfDocument,
                clientId,
                "Warning",
                $"Reintento {attemptNumber}: la DGII ya tenía el eNCF {eNcf} de un envío anterior sin TrackId. No se trata como secuencia quemada; conciliar con la DGII.",
                JsonSerializer.Serialize(status));
        }

        if (resultDto.Success)
            await AddLogAsync(ecfDocument, clientId, "Information", $"e-CF aprobado. TrackId: {transmission.TrackId}. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");

        await MarkDocumentAsync(ecfDocument, statusId, resultDto.Message);
        await CompleteTransmissionAsync(transmissionRecord, transmission, statusId, status);

        if (resultDto.Success || resultDto.IsAcceptedConditional)
            await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId, cancellationToken);

        if (IsPendingDgiiStatus(status))
        {
            resultDto.IsPending = true;
            resultDto.Success = false;
            resultDto.Message = string.IsNullOrWhiteSpace(status.Error)
                ? $"DGII aun procesa el e-CF. TrackId: {transmission.TrackId}"
                : status.Error;

            var trackingDelaySeconds = Math.Max(
                1,
                _configuration.GetValue<int?>("EcfPerformance:TrackingInitialDelaySeconds") ?? 1);
            var jobId = BackgroundJob.Schedule<EcfTrackingJob>(
                j => j.Execute(transmission.TrackId, targetEnvironment, issuerRnc, certBase64, certPass, ecfDocument.EcfDocumentId, 1),
                TimeSpan.FromSeconds(trackingDelaySeconds));

            ecfDocument.HangfireJobId = jobId;
            await _ecfDocumentService.UpdateAsync(ecfDocument);
            resultDto.HangfireJobId = jobId;
            await AddLogAsync(ecfDocument, clientId, "Information", $"DGII no retorno aceptacion inmediata. Job de seguimiento programado: {jobId}.");
        }
    }
```
> Importante: copiar el cuerpo **desde el archivo actual**, no solo desde este plan, y verificar con `git diff` que lo único distinto del bloque original es la firma del método, el `return;` implícito y el comentario `<Step 2 …>` (que se reemplaza enseguida).

- [ ] **Step 2: Agregar `TryAdoptExistingTrackIdAsync` y usarlo en la rama del 1209/75**

Debajo de `TrackReceivedAsync` agregar:
```csharp
    /// <summary>
    /// Pregunta a la DGII si ya tiene recibido el eNCF (Consulta de TrackId). Si lo tiene, adopta
    /// su TrackId y sigue su estado sin retransmitir. False cuando la consulta no está disponible,
    /// falla o la DGII no lo tiene: quien llama sigue con su camino normal.
    /// </summary>
    private async Task<bool> TryAdoptExistingTrackIdAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        string signedXml,
        string issuerRnc,
        string eNcf,
        DgiiEnvironment targetEnvironment,
        string certBase64,
        string certPass,
        string token,
        int attemptNumber,
        string? excludeTrackId,
        CancellationToken cancellationToken)
    {
        DgiiTrackIdsResult found;

        try
        {
            found = await _transmissionService.GetTrackIdsAsync(targetEnvironment, token, issuerRnc, eNcf, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await AddLogAsync(ecfDocument, clientId, "Warning", $"No se pudo consultar el TrackId de {eNcf} a la DGII: {ex.Message}");
            return false;
        }

        if (!found.Available || found.Error is not null)
        {
            await AddLogAsync(
                ecfDocument,
                clientId,
                "Information",
                found.Available
                    ? $"No se pudo consultar el TrackId de {eNcf} a la DGII: {found.Error}"
                    : $"Consulta de TrackId no disponible: {found.Error}");
            return false;
        }

        var entry = DgiiTrackIdPicker.Pick(found.Entries, excludeTrackId);
        if (entry is null)
        {
            await AddLogAsync(ecfDocument, clientId, "Information", $"La DGII no tiene recibido el eNCF {eNcf}; se transmitirá.");
            return false;
        }

        await AddLogAsync(
            ecfDocument,
            clientId,
            "Information",
            $"La DGII ya había recibido el eNCF {eNcf}: TrackId {entry.TrackId} ({entry.Estado}). Se recuperó sin retransmitir.");

        var recovered = new DgiiTransmissionResult { TrackId = entry.TrackId, Estado = entry.Estado };

        await TrackReceivedAsync(
            resultDto, ecfDocument, clientId, recovered, signedXml, eNcf, issuerRnc,
            targetEnvironment, certBase64, certPass, token, attemptNumber,
            earlierAttemptsWithoutTrackId: false, timings: null, cancellationToken);

        return true;
    }
```
En `TrackReceivedAsync`, reemplazar el comentario `// <Step 2 agrega aquí …>` por:
```csharp
            // La DGII respondió con un TrackId NUEVO y rechazado (el del intento duplicado). El del
            // envío original sigue sin conocerse: se le pregunta a la DGII (se excluye el duplicado).
            if (await TryAdoptExistingTrackIdAsync(
                    resultDto, ecfDocument, clientId, signedXml, issuerRnc, eNcf, targetEnvironment,
                    certBase64, certPass, token, attemptNumber, excludeTrackId: transmission.TrackId, cancellationToken))
            {
                await CompleteTransmissionAsync(transmissionRecord, transmission, 11, status);
                return;
            }

            // No se pudo recuperar: se vacía el TrackId guardado del duplicado. Si quedara, una
            // reconsulta posterior marcaría como rechazado un comprobante que la DGII sí aceptó
            // (la respuesta completa sigue en ResponsePayload).
            transmissionRecord.TrackId = string.Empty;
```

- [ ] **Step 3: Recuperar antes de retransmitir en `TransmitDeferredAsync`**

En `TransmitDeferredAsync`, reemplazar la asignación `step = await TransmitAndTrackAsync(` … `cancellationToken);` por:
```csharp
            // Un reintento de un documento que ya tuvo intentos sin TrackId: antes de volver a
            // enviar se le pregunta a la DGII si ya lo recibió (cubre los reintentos automáticos y
            // el re-encolado manual). Si lo tiene, se sigue su estado sin retransmitir.
            var adopted = earlierAttemptsWithoutTrackId
                && tokenTask is not null
                && await TryAdoptExistingTrackIdAsync(
                    resultDto, ecfDocument, clientId, xml.XmlSigned, issuerRnc, ecfDocument.Ncf,
                    targetEnvironment, certBase64, certPass, await tokenTask, attemptNumber,
                    excludeTrackId: null, cancellationToken);

            step = adopted
                ? TransmitStep.Completed
                : await TransmitAndTrackAsync(
                    resultDto,
                    ecfDocument,
                    clientId,
                    xml.XmlSigned,
                    ecfType,
                    total,
                    issuerRnc,
                    ecfDocument.Ncf,
                    ShouldSendAsB2cSummary(ecfType, total),
                    targetEnvironment,
                    certBase64,
                    certPass,
                    tokenTask,
                    statusDelayMilliseconds: 750,
                    timings: null,
                    attemptNumber,
                    isDeferred: true,
                    earlierAttemptsWithoutTrackId,
                    cancellationToken);
```
(Los argumentos de `TransmitAndTrackAsync` son los mismos que ya tiene la llamada; solo cambia el envoltorio.)

- [ ] **Step 4: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `0 Error(s)`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo` → todo en verde.
Run: `git diff --stat` y revisar que el cambio en `TransmitAndTrackAsync` es solo la sustitución del bloque por la llamada.

- [ ] **Step 5: Commit**
```bash
git add ZynstormECFPlatform.Services
git commit -m "feat(ecf): recuperar el TrackId perdido preguntando a la DGII antes de retransmitir

Se extrae TrackReceivedAsync sin cambiar su lógica. En un reintento sin TrackId previo y en la
rama del 1209/75 se usa la Consulta de TrackId; si no se recupera, el TrackId del intento
duplicado se vacía para que una reconsulta no marque como rechazado un comprobante aceptado.

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```

---

## Task 4: Política de elegibilidad y calendario de reconsulta (lógica pura)

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfRequeuePolicy.cs`, `ZynstormECFPlatform.Services/Production/EcfRetrackSchedule.cs`
- Test: `ZynstormECFPlatform.Tests/Production/EcfRequeuePolicyTests.cs`, `ZynstormECFPlatform.Tests/Production/EcfRetrackScheduleTests.cs`

**Interfaces:**
- Produces: `enum EcfRequeueAction { Transmit, Track }`, `record EcfRequeueSnapshot(int EcfStatusId, bool HasSignedXml, bool HasTrackId, DateTime LastActivityUtc)`, `EcfRequeuePolicy.Evaluate(EcfRequeueSnapshot, DateTime nowUtc, TimeSpan staleInFlight) : EcfRequeueAction?`, `EcfRequeuePolicy.NextAttemptNumber(int transmissions) : int`, `EcfRetrackSchedule.MaxAttempts`, `EcfRetrackSchedule.NextDelay(int attemptsMade) : TimeSpan?`.

- [ ] **Step 1: Escribir los tests (fallan)**

`EcfRequeuePolicyTests.cs`:
```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfRequeuePolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Stale = TimeSpan.FromMinutes(5);

    private static EcfRequeueAction? Evaluate(int status, bool xml = true, bool trackId = false, double idleMinutes = 10) =>
        EcfRequeuePolicy.Evaluate(new EcfRequeueSnapshot(status, xml, trackId, Now.AddMinutes(-idleMinutes)), Now, Stale);

    [Theory]
    [InlineData(12)]
    public void ErrorWithoutTrackId_IsTransmit(int status) =>
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(status));

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    public void InFlightWithoutTrackId_IsTransmitOnlyAfterTheStaleWindow(int status)
    {
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(status, idleMinutes: 5));
        Assert.Null(Evaluate(status, idleMinutes: 4.9));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(12)]
    public void WithTrackId_IsTrackAfterTwoMinutes(int status)
    {
        Assert.Equal(EcfRequeueAction.Track, Evaluate(status, trackId: true, idleMinutes: 2));
        Assert.Null(Evaluate(status, trackId: true, idleMinutes: 1.9));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(13)]
    [InlineData(6)]
    public void ExcludedStatuses_AreNeverRequeueable(int status)
    {
        Assert.Null(Evaluate(status));
        Assert.Null(Evaluate(status, trackId: true));
    }

    [Fact]
    public void WithoutSignedXml_IsNeverRequeueable() => Assert.Null(Evaluate(12, xml: false));

    [Fact]
    public void ChangedInTheLastMinute_IsNotRequeueable()
    {
        Assert.Null(Evaluate(12, idleMinutes: 0.5));
        Assert.Equal(EcfRequeueAction.Transmit, Evaluate(12, idleMinutes: 1));
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(5, 6)]
    public void NextAttemptNumber_IsAtLeastTwo(int transmissions, int expected) =>
        Assert.Equal(expected, EcfRequeuePolicy.NextAttemptNumber(transmissions));
}
```
`EcfRetrackScheduleTests.cs`:
```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfRetrackScheduleTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(3, 5)]
    [InlineData(4, 30)]
    [InlineData(6, 30)]
    [InlineData(7, 120)]
    [InlineData(9, 120)]
    public void NextDelay_GrowsWithTheAttempts(int attemptsMade, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), EcfRetrackSchedule.NextDelay(attemptsMade));

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(11)]
    public void NextDelay_IsNullOutsideTheRange(int attemptsMade) =>
        Assert.Null(EcfRetrackSchedule.NextDelay(attemptsMade));
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~EcfRequeuePolicy|FullyQualifiedName~EcfRetrackSchedule"`
Expected: error de compilación (tipos inexistentes).

- [ ] **Step 3: Implementar**

`EcfRequeuePolicy.cs`:
```csharp
namespace ZynstormECFPlatform.Services.Production;

public enum EcfRequeueAction
{
    /// <summary>Volver a enviar a la DGII (primero se le pregunta si ya lo tiene).</summary>
    Transmit,

    /// <summary>Volver a consultar el estado en la DGII: ya la tiene, no se retransmite.</summary>
    Track
}

/// <summary>Lo que decide si un comprobante se puede re-encolar.</summary>
public sealed record EcfRequeueSnapshot(int EcfStatusId, bool HasSignedXml, bool HasTrackId, DateTime LastActivityUtc);

public static class EcfRequeuePolicy
{
    /// <summary>Si cambió hace menos de esto, puede haber un job en curso (o un doble clic).</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    /// <summary>El seguimiento original dura alrededor de 1 o 2 minutos; pasado esto ya terminó.</summary>
    public static readonly TimeSpan TrackIdle = TimeSpan.FromMinutes(2);

    public static EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot, DateTime nowUtc, TimeSpan staleInFlight)
    {
        if (!snapshot.HasSignedXml)
            return null;

        var idle = nowUtc - snapshot.LastActivityUtc;
        if (idle < Cooldown)
            return null;

        if (snapshot.HasTrackId)
        {
            return snapshot.EcfStatusId is 9 or 12 && idle >= TrackIdle
                ? EcfRequeueAction.Track
                : null;
        }

        return snapshot.EcfStatusId switch
        {
            12 => EcfRequeueAction.Transmit,
            7 or 8 or 9 when idle >= staleInFlight => EcfRequeueAction.Transmit,
            _ => null
        };
    }

    /// <summary>
    /// Número de intento del reenvío manual: al menos 2, para que el job siempre le pregunte
    /// primero a la DGII si ya recibió el comprobante (la comprobación aplica desde el intento 2).
    /// </summary>
    public static int NextAttemptNumber(int transmissions) => Math.Max(2, transmissions + 1);
}
```
`EcfRetrackSchedule.cs`:
```csharp
namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Espera entre consultas de estado de la reconsulta manual: 5 s, luego 30 s y luego 2 min. Son 10
/// intentos (unos 10 minutos), más que el seguimiento original, para cubrir una DGII lenta.
/// </summary>
public static class EcfRetrackSchedule
{
    public const int MaxAttempts = 10;

    /// <summary>Espera antes del siguiente intento, dados los ya hechos (el primero es 1). Null al agotarse.</summary>
    public static TimeSpan? NextDelay(int attemptsMade) => attemptsMade switch
    {
        >= 1 and <= 3 => TimeSpan.FromSeconds(5),
        >= 4 and <= 6 => TimeSpan.FromSeconds(30),
        >= 7 and < MaxAttempts => TimeSpan.FromMinutes(2),
        _ => null
    };
}
```

- [ ] **Step 4: Probar**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~EcfRequeuePolicy|FullyQualifiedName~EcfRetrackSchedule"`
Expected: todos pasan.

- [ ] **Step 5: Commit**
```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests
git commit -m "feat(ecf): política de elegibilidad para re-encolar y calendario de reconsulta

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```

---

## Task 5: `EcfRetrackJob` (reconsulta sin certificado en los argumentos)

**Files:**
- Modify: `ZynstormECFPlatform.Services/Jobs/EcfTrackingJob.cs`, `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs`, `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`, `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Create: `ZynstormECFPlatform.Services/Jobs/EcfRetrackJob.cs`

**Interfaces:**
- Consumes: `EcfRetrackSchedule` (Task 4).
- Produces: `EcfTrackingJob.CheckAsync(...) : Task<bool>` (true = la DGII sigue procesando); `IReceivedEcfProductionService.PrepareRetrackAsync(int ecfDocumentId) : Task<RetrackMaterial?>`; `sealed record RetrackMaterial(string TrackId, string IssuerRnc, string CertBase64, string CertPass)`; `EcfRetrackJob.Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)`.

- [ ] **Step 1: Separar `CheckAsync` de `EcfTrackingJob.Execute` (sin cambiar su comportamiento)**

En `EcfTrackingJob.cs`, reemplazar el método `Execute` completo por estos dos (el cuerpo del `try`, `PersistStatusAsync` y `PersistJobErrorAsync` no cambian):
```csharp
    public async Task Execute(
        string trackId,
        DgiiEnvironment environment,
        string rncEmisor,
        string certBase64,
        string certPass,
        int ecfDocumentId = 0,
        int attemptNumber = 1)
    {
        var stillPending = await CheckAsync(trackId, environment, rncEmisor, certBase64, certPass, ecfDocumentId, attemptNumber);

        if (stillPending && attemptNumber < MaxAttempts)
        {
            BackgroundJob.Schedule<EcfTrackingJob>(
                j => j.Execute(trackId, environment, rncEmisor, certBase64, certPass, ecfDocumentId, attemptNumber + 1),
                TimeSpan.FromSeconds(3));
        }
    }

    /// <summary>
    /// Consulta una vez el estado a la DGII y lo guarda. No programa nada. True si la DGII sigue
    /// procesando el comprobante. Lo usan este job y <see cref="EcfRetrackJob"/>.
    /// </summary>
    public async Task<bool> CheckAsync(
        string trackId,
        DgiiEnvironment environment,
        string rncEmisor,
        string certBase64,
        string certPass,
        int ecfDocumentId = 0,
        int attemptNumber = 1)
    {
        _logger.LogInformation("Checking status for TrackId: {TrackId}", trackId);

        try
        {
            string token = await _authService.GetTokenAsync(rncEmisor, environment, certBase64, certPass);
            var statusResponse = await _transmissionService.GetStatusAsync(environment, token, trackId);

            string cacheKey = $"EcfStatus_{trackId}";
            _cacheService.Set(cacheKey, statusResponse, TimeSpan.FromHours(1));

            _logger.LogInformation("TrackId {TrackId} status: {Status}", trackId, statusResponse.Estado);

            if (ecfDocumentId > 0)
            {
                await PersistStatusAsync(ecfDocumentId, trackId, rncEmisor, statusResponse, attemptNumber, environment);
            }

            return ReceivedEcfProductionService.IsPendingDgiiStatus(statusResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking status for TrackId: {TrackId}", trackId);
            if (ecfDocumentId > 0)
            {
                await PersistJobErrorAsync(ecfDocumentId, trackId, ex);
            }

            return false;
        }
    }
```
> Antes de pegar, comparar con el `Execute` actual (líneas ~66-107 del archivo): los únicos cambios deben ser el reparto en dos métodos y que `CheckAsync` devuelve el bool en lugar de programar el siguiente job.

- [ ] **Step 2: `PrepareRetrackAsync` en el servicio de emisión**

En `IReceivedEcfProductionService.cs`, dentro de la interfaz agregar:
```csharp
    /// <summary>
    /// Lo que necesita la reconsulta manual: el TrackId más reciente del documento y el
    /// certificado del cliente, leídos de la base de datos (nunca viajan como argumento de un
    /// job). Null si no hay TrackId, cliente, API key o certificado.
    /// </summary>
    Task<RetrackMaterial?> PrepareRetrackAsync(int ecfDocumentId);
```
y debajo de `DeferredTransmitOutcome`:
```csharp
public sealed record RetrackMaterial(string TrackId, string IssuerRnc, string CertBase64, string CertPass);
```
En `ReceivedEcfProductionService.cs`, junto a `TransmitDeferredAsync`:
```csharp
    public async Task<RetrackMaterial?> PrepareRetrackAsync(int ecfDocumentId)
    {
        var ecfDocument = await _ecfDocumentService.GetAsync(ecfDocumentId);
        if (ecfDocument is null)
            return null;

        var trackId = await _ecfTransmissionService.Table
            .AsNoTracking()
            .Where(t => t.EcfDocumentId == ecfDocumentId && t.TrackId != string.Empty)
            .OrderByDescending(t => t.SentAtUtc)
            .Select(t => t.TrackId)
            .FirstOrDefaultAsync();

        if (string.IsNullOrEmpty(trackId))
            return null;

        var client = await _clientService.GetAsync(ecfDocument.ClientId);
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == ecfDocument.ClientId);
        if (client is null || apiKey is null)
            return null;

        var material = await LoadSigningMaterialAsync(ecfDocument.ClientId, apiKey);
        if (material is null)
            return null;

        return new RetrackMaterial(trackId, client.Rnc, material.Value.CertBase64, material.Value.CertPass);
    }
```

- [ ] **Step 3: El job**

`ZynstormECFPlatform.Services/Jobs/EcfRetrackJob.cs`:
```csharp
using Hangfire;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Reconsulta a la DGII el estado de un comprobante que se quedó «Enviado» sin resolverse (el
/// seguimiento original se agotó). No retransmite. Lo dispara el re-encolado manual.
///
/// Solo recibe identificadores: el TrackId y el certificado se cargan de la base de datos en cada
/// consulta, porque Hangfire guarda los argumentos de un job en claro. Se reprograma a sí mismo con
/// espera creciente (<see cref="EcfRetrackSchedule"/>) hasta que la DGII resuelva o se agoten los
/// intentos.
/// </summary>
[Queue("ecf-transmit")]
[AutomaticRetry(Attempts = 0)]
public class EcfRetrackJob(IReceivedEcfProductionService service, EcfTrackingJob tracking)
{
    public async Task Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)
    {
        var material = await service.PrepareRetrackAsync(ecfDocumentId);
        if (material is null)
            return;

        var stillPending = await tracking.CheckAsync(
            material.TrackId, environment, material.IssuerRnc, material.CertBase64, material.CertPass,
            ecfDocumentId, attemptNumber);

        if (stillPending && EcfRetrackSchedule.NextDelay(attemptNumber) is { } delay)
        {
            BackgroundJob.Schedule<EcfRetrackJob>(
                job => job.Execute(ecfDocumentId, attemptNumber + 1, environment),
                delay);
        }
    }
}
```
En `ServiceCollectionExtensions.cs`, junto a `services.AddTransient<Jobs.EcfTransmitJob>();` agregar:
```csharp
        services.AddTransient<Jobs.EcfRetrackJob>();
```

- [ ] **Step 4: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `0 Error(s)`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo` → todo en verde.

- [ ] **Step 5: Commit**
```bash
git add ZynstormECFPlatform.Services
git commit -m "feat(ecf): job de reconsulta del estado sin certificado en los argumentos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```

---

## Task 6: `EcfRequeueService`, endpoint y campo `requeueAction` en lista y detalle

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfRequeueService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, `ZynstormECFPlatform.Dtos/EcfDocumentDtos.cs`, `ZynstormECFPlatform.Web.Api/Controllers/EcfDocumentController.cs`

**Interfaces:**
- Consumes: `EcfRequeuePolicy`, `EcfRequeueSnapshot`, `EcfRequeueAction` (Task 4); `EcfTransmitJob.Execute(int, int, DgiiEnvironment)`; `EcfRetrackJob.Execute(int, int, DgiiEnvironment)` (Task 5).
- Produces: `IEcfRequeueService { EcfRequeueAction? Evaluate(EcfRequeueSnapshot); Task<EcfRequeueResult> RequeueAsync(string guid, string? userId, bool isSuperAdmin, string userName, CancellationToken) }`, `enum EcfRequeueOutcome { Ok, NotFound, Conflict }`, `record EcfRequeueResult(EcfRequeueOutcome Outcome, EcfRequeueAction? Action, string Message)`; `EcfDocumentViewDto.RequeueAction : string?`; `POST v1/EcfDocument/guid/{guid}/requeue`.

- [ ] **Step 1: El servicio**

`ZynstormECFPlatform.Services/Production/EcfRequeueService.cs`:
```csharp
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Services.Jobs;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfRequeueOutcome { Ok, NotFound, Conflict }

public sealed record EcfRequeueResult(EcfRequeueOutcome Outcome, EcfRequeueAction? Action, string Message);

public interface IEcfRequeueService
{
    /// <summary>Qué se puede hacer con un comprobante ahora (null si nada). Misma regla que usa <see cref="RequeueAsync"/>.</summary>
    EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot);

    Task<EcfRequeueResult> RequeueAsync(
        string guid,
        string? userId,
        bool isSuperAdmin,
        string userName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Re-encola un comprobante atascado: lo reenvía a la DGII (<see cref="EcfTransmitJob"/>) o vuelve a
/// consultar su estado (<see cref="EcfRetrackJob"/>). La transición se reclama con un UPDATE
/// condicional, así que dos peticiones simultáneas no encolan dos veces.
/// </summary>
public class EcfRequeueService(StorageContext context, IConfiguration configuration) : IEcfRequeueService
{
    private TimeSpan StaleAfter => TimeSpan.FromMinutes(Math.Clamp(
        configuration.GetValue<int?>("EcfIdempotency:StaleInFlightMinutes") ?? 5,
        1,
        120));

    public EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot) =>
        EcfRequeuePolicy.Evaluate(snapshot, DateTime.UtcNow, StaleAfter);

    public async Task<EcfRequeueResult> RequeueAsync(
        string guid,
        string? userId,
        bool isSuperAdmin,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<EcfDocument>().AsNoTracking().Where(d => d.GuidId == guid);

        if (!isSuperAdmin)
        {
            query = query.Where(d => d.Client.UserClients.Any(uc => uc.UserId == userId));
        }

        var row = await query
            .Select(d => new
            {
                d.EcfDocumentId,
                d.ClientId,
                d.EcfStatusId,
                d.RegisteredAt,
                d.LastUpdateUtc,
                HasSignedXml = d.EcfXmlDocuments.Any(x => x.XmlSigned != null && x.XmlSigned != ""),
                HasTrackId = d.EcfTransmissions.Any(t => t.TrackId != ""),
                Transmissions = d.EcfTransmissions.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.NotFound, null, "No se encontró el comprobante o no tiene permisos sobre él.");
        }

        var observed = row.LastUpdateUtc ?? row.RegisteredAt;
        var action = Evaluate(new EcfRequeueSnapshot(row.EcfStatusId, row.HasSignedXml, row.HasTrackId, observed));

        if (action is null)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.Conflict, null, "Este comprobante ya no se puede re-encolar: cambió de estado o se re-encoló hace poco.");
        }

        var now = DateTime.UtcNow;
        var newStatus = action == EcfRequeueAction.Transmit ? 7 : row.EcfStatusId;

        // Reclamo atómico: solo gana quien todavía ve el documento como lo evaluó.
        var claimed = await context.Set<EcfDocument>()
            .Where(d => d.EcfDocumentId == row.EcfDocumentId
                        && d.EcfStatusId == row.EcfStatusId
                        && (d.LastUpdateUtc ?? d.RegisteredAt) == observed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.EcfStatusId, newStatus)
                    .SetProperty(d => d.LastUpdateUtc, (DateTime?)now),
                cancellationToken);

        if (claimed == 0)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.Conflict, null, "Otro usuario ya re-encoló este comprobante.");
        }

        var note = action == EcfRequeueAction.Transmit
            ? $"Re-encolado por {userName}: reenviar a la DGII."
            : $"Re-encolado por {userName}: reconsultar el estado a la DGII.";

        context.Set<EcfStatusHistory>().Add(new EcfStatusHistory
        {
            EcfDocumentId = row.EcfDocumentId,
            EcfStatusId = newStatus,
            Message = note
        });
        context.Set<SystemLog>().Add(new SystemLog
        {
            ClientId = row.ClientId,
            EcfDocumentId = row.EcfDocumentId,
            LogLevel = "Information",
            Message = note,
            CreateAtUtc = now
        });
        await context.SaveChangesAsync(cancellationToken);

        // El ambiente de la DGII lo impone EcfXmlValidation:TargetDgiiEnvironment.
        if (action == EcfRequeueAction.Transmit)
        {
            BackgroundJob.Enqueue<EcfTransmitJob>(job => job.Execute(
                row.EcfDocumentId,
                EcfRequeuePolicy.NextAttemptNumber(row.Transmissions),
                DgiiEnvironment.Production));
        }
        else
        {
            BackgroundJob.Enqueue<EcfRetrackJob>(job => job.Execute(row.EcfDocumentId, 1, DgiiEnvironment.Production));
        }

        var message = action == EcfRequeueAction.Transmit
            ? "Comprobante en cola para reenviarse a la DGII."
            : "Comprobante en cola para volver a consultar su estado a la DGII.";

        return new EcfRequeueResult(EcfRequeueOutcome.Ok, action, message);
    }
}
```
Registrar en `ServiceCollectionExtensions.cs`:
```csharp
        services.AddTransient<Production.IEcfRequeueService, Production.EcfRequeueService>();
```

- [ ] **Step 2: DTO**

En `EcfDocumentViewDto` (`EcfDocumentDtos.cs`) agregar, tras `DgiiMessage`:
```csharp
        public string? RequeueAction { get; set; } // "transmit" | "track" | null: qué se puede hacer con un comprobante atascado
```

- [ ] **Step 3: Controller — lista, detalle y endpoint**

En `EcfDocumentController.cs`:
1. Constructor: agregar la dependencia `IEcfRequeueService requeueService` (y `using ZynstormECFPlatform.Services.Production;`):
```csharp
    public class EcfDocumentController(
        IEcfDocumentService ecfDocumentService,
        IEcfRequeueService requeueService,
        ILogger<EcfDocumentController> logger) : ControllerBase
```
2. Lista (`Get`): en la proyección `tempProjection` agregar al final del objeto anónimo:
```csharp
                    EcfStatusId = e.EcfStatusId,
                    LastActivityUtc = e.LastUpdateUtc ?? e.RegisteredAt,
                    HasSignedXml = e.EcfXmlDocuments.Any(x => x.XmlSigned != null && x.XmlSigned != ""),
                    HasTrackId = e.EcfTransmissions.Any(t => t.TrackId != "")
```
   y en el `Select` en memoria que arma `EcfDocumentViewDto`, agregar:
```csharp
                    DgiiMessage = x.DgiiMessage,
                    RequeueAction = requeueService
                        .Evaluate(new EcfRequeueSnapshot(x.EcfStatusId, x.HasSignedXml, x.HasTrackId, x.LastActivityUtc))
                        ?.ToString().ToLowerInvariant()
```
   (reemplaza la línea `DgiiMessage = x.DgiiMessage` existente).
3. Detalle (`GetByGuid`): tras construir `dto`, antes de `return Ok(dto);`:
```csharp
                dto.RequeueAction = requeueService
                    .Evaluate(new EcfRequeueSnapshot(
                        document.EcfStatusId,
                        document.EcfXmlDocuments.Any(x => !string.IsNullOrEmpty(x.XmlSigned)),
                        document.EcfTransmissions.Any(t => !string.IsNullOrEmpty(t.TrackId)),
                        document.LastUpdateUtc ?? document.RegisteredAt))
                    ?.ToString().ToLowerInvariant();
```
4. Endpoint nuevo, antes de `[HttpGet("stats")]`:
```csharp
        /// <summary>
        /// Re-encola un comprobante atascado: lo reenvía a la DGII o vuelve a consultar su estado,
        /// según lo que corresponda. Un SA puede sobre cualquier cliente; el resto, solo sobre los suyos.
        /// </summary>
        [HttpPost("guid/{guid}/requeue")]
        public async Task<IActionResult> Requeue(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var userName = User.Identity?.Name ?? CurrentUserId ?? "usuario";
                var result = await _requeueService.RequeueAsync(guid, CurrentUserId, IsSA, userName, cancellationToken);

                return result.Outcome switch
                {
                    EcfRequeueOutcome.Ok => Ok(new { action = result.Action!.Value.ToString().ToLowerInvariant(), message = result.Message }),
                    EcfRequeueOutcome.NotFound => NotFound(new { message = result.Message }),
                    _ => Conflict(new { message = result.Message })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error re-encolando la factura {Guid}: {Message}", guid, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al re-encolar la factura." });
            }
        }
```
   Con el constructor primario, usar el parámetro `requeueService` directamente (en el cuerpo del endpoint escribir `requeueService.RequeueAsync(...)` en lugar de `_requeueService`) o declarar `private readonly IEcfRequeueService _requeueService = requeueService;` junto a los otros campos y usar ese nombre en los cuatro puntos. Elegir **una** forma y aplicarla en los cuatro puntos.

- [ ] **Step 4: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `0 Error(s)` (si EF no puede traducir la proyección, simplificar `HasSignedXml`/`HasTrackId` a `Any()` sin los chequeos de cadena y revisar el mensaje de EF).
Run: `dotnet test ZynstormECFPlatform.Tests -nologo` → todo en verde.

- [ ] **Step 5: Verificación manual (Staging, con la API de la rama desplegada)**

- `GET v1/EcfDocument?...` devuelve `requeueAction` en cada fila (`null` en la mayoría).
- Con un comprobante en estado 12 sin TrackId y con más de 1 min sin cambios: `POST v1/EcfDocument/guid/{guid}/requeue` → 200 `{ action: "transmit", … }`; el historial muestra «Re-encolado por …» y el job corre en la cola `ecf-transmit`. Repetir el `POST` enseguida → 409.
- Con un usuario sin vínculo al cliente → 404.
Si no hay entorno, anotarlo en el reporte: **no** declarar verificado.

- [ ] **Step 6: Commit**
```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Dtos ZynstormECFPlatform.Web.Api
git commit -m "feat(ecf): re-encolar un comprobante atascado (servicio, endpoint y requeueAction en la lista)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```

---

## Task 7: Frontend — botón de re-encolar en la lista de facturas

**Files (repo `ZynstormECFPlatform-FrontEnd`, rama `Staging`):**
- Modify: `services/invoice.service.ts`, `app/facturas/page.tsx`

**Interfaces:**
- Consumes: `POST v1/EcfDocument/guid/{guid}/requeue` y el campo `requeueAction` (Task 6).
- Produces: `Invoice.requeueAction`, `requeueInvoice(guid)`, botón con confirmación en la fila y en el detalle.

- [ ] **Step 1: Servicio y tipo**

En `services/invoice.service.ts`: importar `post` (`import { get, post } from "@/services/fetchHandler"`), agregar a `Invoice`:
```ts
  /** Qué se puede hacer con un comprobante atascado; lo decide el servidor. */
  requeueAction?: "transmit" | "track" | null
```
y al final del archivo:
```ts
export interface RequeueResult {
  action: "transmit" | "track"
  message: string
}

export const requeueInvoice = async (guid: string): Promise<RequeueResult> => {
  return await post<RequeueResult>(`${INVOICE_URL}/guid/${guid}/requeue`)
}
```

- [ ] **Step 2: Estado, manejador y confirmación**

En `app/facturas/page.tsx`:
1. Importar `RotateCcw` en la lista de iconos de `lucide-react`, `requeueInvoice` en el import de `@/services/invoice.service`, y el `AlertDialog`:
```tsx
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
```
2. Junto a los demás `useState` de la tabla (cerca de `downloadingRowId`):
```tsx
  const [requeueTarget, setRequeueTarget] = useState<Invoice | null>(null)
  const [requeueingId, setRequeueingId] = useState<string | null>(null)
  const [requeueMessage, setRequeueMessage] = useState<{ type: "success" | "error"; text: string } | null>(null)
```
3. Manejador, junto a `handleViewInvoice`:
```tsx
  const handleRequeue = async () => {
    const invoice = requeueTarget
    if (!invoice) return
    setRequeueTarget(null)
    setRequeueingId(invoice.id)
    try {
      const result = await requeueInvoice(invoice.id)
      setRequeueMessage({ type: "success", text: `${invoice.ncf}: ${result.message}` })
      await fetchInvoices()
    } catch (err) {
      setRequeueMessage({
        type: "error",
        text: err instanceof Error ? err.message : "No se pudo re-encolar el comprobante.",
      })
    } finally {
      setRequeueingId(null)
    }
  }
```
   (`fetchInvoices` ya es el `useCallback` que recarga la lista y las estadísticas.)

- [ ] **Step 3: Botón en la fila, en el detalle, aviso y diálogo**

1. En la celda de acciones de la fila, tras el botón «Descargar XML»:
```tsx
                                  {invoice.requeueAction && (
                                    <Button
                                      variant="ghost"
                                      size="sm"
                                      className="h-7 w-7 p-0 text-amber-600 hover:text-amber-700"
                                      onClick={() => setRequeueTarget(invoice)}
                                      disabled={requeueingId !== null}
                                      title={
                                        invoice.requeueAction === "transmit"
                                          ? "Volver a enviar a la DGII"
                                          : "Volver a consultar el estado a la DGII"
                                      }
                                    >
                                      {requeueingId === invoice.id ? (
                                        <RefreshCw className="h-4 w-4 animate-spin" />
                                      ) : (
                                        <RotateCcw className="h-4 w-4" />
                                      )}
                                    </Button>
                                  )}
```
2. En el diálogo de detalle, junto al botón «Descargar XML» (dentro del `div className="flex justify-end gap-2"`):
```tsx
                    {selectedInvoice?.requeueAction && (
                      <Button
                        variant="outline"
                        className="gap-2"
                        onClick={() => {
                          setRequeueTarget(selectedInvoice)
                          setIsDetailDialogOpen(false)
                        }}
                        disabled={requeueingId !== null}
                      >
                        <RotateCcw className="h-4 w-4" />
                        {selectedInvoice.requeueAction === "transmit" ? "Reenviar a la DGII" : "Reconsultar estado"}
                      </Button>
                    )}
```
3. Aviso del resultado, justo debajo del bloque `{/* Error Alert */}`:
```tsx
            {requeueMessage && (
              <div
                className={cn(
                  "flex items-start justify-between gap-3 rounded-lg border px-4 py-2 text-sm",
                  requeueMessage.type === "success"
                    ? "border-green-300 bg-green-50 text-green-800"
                    : "border-destructive/30 bg-destructive/10 text-destructive",
                )}
              >
                <span>{requeueMessage.text}</span>
                <button type="button" className="text-xs underline" onClick={() => setRequeueMessage(null)}>
                  Cerrar
                </button>
              </div>
            )}
```
4. Diálogo de confirmación, al final del JSX de la página (junto a los demás `Dialog`):
```tsx
      <AlertDialog open={requeueTarget !== null} onOpenChange={(open) => !open && setRequeueTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {requeueTarget?.requeueAction === "transmit" ? "Volver a enviar a la DGII" : "Volver a consultar el estado"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {requeueTarget?.requeueAction === "transmit"
                ? `Se pondrá en cola el envío de ${requeueTarget?.ncf}. Antes de enviarlo se le pregunta a la DGII si ya lo recibió; si ya lo tiene, solo se sigue su estado y no se duplica.`
                : `${requeueTarget?.ncf} ya está en la DGII. Se volverá a consultar su estado; no se retransmite.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={handleRequeue}>Continuar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
```
   (Colocarlo dentro del mismo fragmento/elemento raíz que ya devuelve la página; si el JSX raíz es `MainLayout`, ponerlo como último hijo.)

- [ ] **Step 4: Tipos y lint**

Run (en el frontend): `npx tsc --noEmit 2>&1 | grep "error TS"` y comparar con la línea base (1 error previo en `app/configuraciones/cuenta/page.tsx`); `npx eslint app/facturas/page.tsx services/invoice.service.ts`
Expected: sin errores nuevos.

- [ ] **Step 5: Verificación visual**

Con la API de la rama: abrir `Facturas`, una factura en estado Error con más de 1 min sin cambios muestra el icono de reintento; el diálogo explica la acción; tras confirmar aparece el aviso verde y la fila se refresca. Contra una API sin esta función, la lista no trae `requeueAction` y el botón no aparece. Si no hay entorno, indicarlo: no declarar verificado.

- [ ] **Step 6: Commit (repo del frontend, rama `Staging`)**
```bash
git add services/invoice.service.ts app/facturas/page.tsx
git commit -m "feat(facturas): botón para re-encolar un comprobante atascado

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01WE3G9oxkAhZACwop2PcvqN"
```
(No agregar `tsconfig.tsbuildinfo`: lo modifica `tsc`; restaurarlo con `git checkout tsconfig.tsbuildinfo`.)

---

## Task 8: Cierre

- [ ] **Step 1:** Backend en `feature/emit-idempotente`: `dotnet build` y `dotnet test` completos en verde. Frontend: `tsc` sin errores nuevos.
- [ ] **Step 2:** Actualizar la memoria `ri-html-replicate-branches` con el estado (commits y que el backend **no** se replica a Staging).
- [ ] **Step 3:** Reportar al usuario: qué quedó hecho, qué verificación manual falta (Staging con la API de la rama desplegada y la imagen `:emit-idempotente` en Dokploy), y preguntar si desea push. Recordar que, cuando esto se publique, los textos de las guías de la librería («avise a soporte») pasan a «reintente desde el panel».

---

## Self-Review

**Cobertura de la spec**
- Elegibilidad (tabla, cooldown, estados excluidos, 9 sin TrackId, 9/12 con TrackId) → Task 4.
- Acción `Transmit` (reclamo condicional, `attemptNumber ≥ 2`, comprobación previa a la DGII) → Tasks 3 y 6.
- Acción `Track` (`EcfRetrackJob`, certificado desde la base, calendario creciente, `CheckAsync`) → Tasks 4 y 5.
- Recuperar TrackId perdido (`GetTrackIdsAsync`, configuración, `Pick`, usos en `TransmitDeferredAsync` y en el 1209/75, vaciar TrackId del duplicado) → Tasks 1-3.
- API (`POST …/requeue`, 404/409/200, rastro en historial y log, `requeueAction` en lista y detalle) → Task 6.
- Pantalla (botón, confirmación, refresco, ocultar sin soporte) → Task 7.
- Pruebas puras → Tasks 1, 2 y 4; lo ligado a base de datos/Hangfire se verifica a mano (Task 6 Step 5, Task 7 Step 5), como dice la spec.

**Placeholders:** el único marcador textual es `<Step 2 agrega aquí …>` dentro del código del Step 1 del Task 3, que se reemplaza explícitamente en el Step 2 de ese mismo task.

**Consistencia de tipos:** `DgiiTrackIdEntry`/`DgiiTrackIdsResult` (Task 1) los usan `GetTrackIdsAsync` (Task 2) y `TryAdoptExistingTrackIdAsync` (Task 3). `EcfRequeueSnapshot`/`EcfRequeueAction`/`EcfRequeuePolicy.NextAttemptNumber` (Task 4) los usa `EcfRequeueService` (Task 6). `RetrackMaterial`/`PrepareRetrackAsync`/`CheckAsync` (Task 5) los usa `EcfRetrackJob` (Task 5). `requeueAction` es `"transmit" | "track"` en minúsculas en el backend (`ToString().ToLowerInvariant()`) y en el frontend.

**Riesgos**
- `TrackReceivedAsync` es un movimiento de código en un método crítico del flujo de emisión: el Step 1 del Task 3 exige comparar con `git diff` y el Step 4 corre todos los tests, pero no hay pruebas de integración; se confía en la prueba manual en Staging.
- Si EF no traduce la proyección de `Get` (Task 6 Step 4), simplificar las subconsultas y reintentar.
- `ExecuteUpdateAsync` con `(d.LastUpdateUtc ?? d.RegisteredAt) == observed` depende de que PostgreSQL devuelva la misma precisión que se leyó (microsegundos): se compara el valor leído, no uno recalculado.
