# `emit` diferido, job de transmisión y rendimiento (plataforma, parte 2) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que `POST v1/Ecf/emit?deferred=true` firme, guarde y responda de inmediato con el QR, el código de seguridad y la fecha de firma, mientras un job de Hangfire transmite a la DGII en segundo plano con reintentos ante fallos de transporte; además, menos consultas a la base de datos y una cola propia para transmitir.

**Architecture:** La parte de `ContinueProcessingAsync` que transmite y sigue el estado se extrae a `TransmitAndTrackAsync`, que usan tanto el modo síncrono (como hoy) como el job. En modo diferido `emit` firma, deja el documento en estado 7 (`SendPending`), encola `EcfTransmitJob` y responde. El job recarga todo desde la base de datos (incluido el certificado, nunca como argumento de Hangfire), pasa el documento de 7 a 8, transmite, y ante un fallo de transporte se reprograma con esperas crecientes. La política de reintentos es una función estática pura.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core 10 + Npgsql, Hangfire (PostgreSQL), xUnit 2.9.

Spec: `Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md` (secciones 5, 5b, 6 y «Rendimiento»). Requiere el Plan 1 (`2026-10-07-emit-idempotente.md`) ya aplicado: usa `EcfEmitTimings`, `BeginTransmissionAsync` y el reclamo idempotente.

## Global Constraints

- Opt-in: `deferred` vale `false` por defecto. Sin el parámetro, `emit` se comporta como hoy (síncrono, ya idempotente por el Plan 1).
- Modo diferido: HTTP 200, `Success = false`, `IsPending = true`, `TrackId` vacío, con `SecurityCode`, `SignatureDate` y `QrUrl` completos. Mensaje: «Firmado; la transmisión a la DGII está en curso.». El documento queda en estado 7.
- **No** se guarda `HangfireJobId` después de encolar: `UpdateAsync` reescribe el documento completo y podría pisar el estado 8 que el job ya escribió.
- `EcfTransmitJob`: cola `ecf-transmit`, `[AutomaticRetry(Attempts = 0)]`, argumentos solo `(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)`. El certificado y su contraseña se recargan desde la base de datos.
- El job solo transmite si el documento está en estado 7; lo pasa a 8 antes de enviar. Si no está en 7, termina sin hacer nada.
- Reintentos de transporte: solo cuando `DgiiTransmissionResult.TransportFailure` es true (timeout o red caída hacia la DGII) o hay una excepción inesperada. Esperas `EcfTransmit:RetryDelaysSeconds`, por defecto `[5, 30, 120]` (3 reintentos). Nunca se reintenta un rechazo de la DGII ni «En Proceso» (eso lo sigue `EcfTrackingJob`). Entre intentos el documento vuelve a estado 7; agotados, queda en estado 12.
- Cada intento agrega una fila `EcfTransmission` con `AttemptNumber` = número de intento.
- Un 1209/75 en un reintento propio (intento mayor a 1, sin TrackId en intentos previos) **no** es secuencia quemada: la respuesta no lleva `Status`/`DgiiResponse` rechazados, el documento queda en estado 9 con aviso, y no se pide un NCF nuevo.
- Caché de referencia: solo moneda, tipo de e-CF y sucursal principal, TTL 5 min, sin invalidación. **No** se cachea el cliente (su `ClientInactive` cambia por varios caminos), ni la API key, ni el certificado.
- Servidor de Hangfire propio para `ecf-transmit`: `EcfTransmit:WorkerCount`, 10 por defecto, acotado entre 1 y 50. El servidor por defecto queda solo con la cola `default`.
- Sección 6 del spec (QR en toda respuesta posterior a la firma): ya se cumple. `ApplyQrMetadata` llena el resultado al firmar, y toda salida posterior devuelve ese mismo objeto. No requiere código.
- Comandos desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, en la rama `feature/emit-idempotente`. Commits en español, terminan con `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`.
- El repo tiene un cambio sin commitear en `.github/workflows/deploy.yml` que no pertenece a esta tarea: **nunca** uses `git add .` ni `git commit -a`. Git registra la carpeta de docs como `Docs/` (con mayúscula).

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `ZynstormECFPlatform.Services/Production/EcfTransmitRetryPolicy.cs` (nuevo) | Funciones puras: esperas, siguiente espera, duplicado propio |
| `ZynstormECFPlatform.Services/Production/EcfReferenceCache.cs` (nuevo) | Caché de datos de referencia |
| `ZynstormECFPlatform.Services/Jobs/EcfTransmitJob.cs` (nuevo) | Job de transmisión diferida |
| `ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs` (modif.) | `TransportFailure` en `DgiiTransmissionResult` |
| `ZynstormECFPlatform.Services/DgiiTransmissionService.cs` (modif.) | Marca fallos de transporte (incluye timeout) |
| `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs` (modif.) | `deferred`, `TransmitDeferredAsync`, `DeferredTransmitOutcome` |
| `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (modif.) | Extracción de `TransmitAndTrackAsync`, modo diferido, caché, un solo guardado en `MarkDocumentAsync` |
| `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (modif.) | Registro de la caché y del job |
| `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (modif.) | Parámetro `deferred` |
| `ZynstormECFPlatform.Web.Api/Program.cs` (modif.) | Dos servidores de Hangfire |
| `ZynstormECFPlatform.Tests/Production/EcfTransmitRetryPolicyTests.cs`, `EcfReferenceCacheTests.cs`, `DgiiTransmissionServiceTests.cs` (nuevos) | Pruebas |
| `Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md` (modif.) | La caché ya no incluye cliente ni API key |

---

### Task 1: Política de reintentos (funciones puras)

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfTransmitRetryPolicy.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfTransmitRetryPolicyTests.cs`

**Interfaces:**
- Produces:
  - `static IReadOnlyList<TimeSpan> EcfTransmitRetryPolicy.Normalize(int[]? configuredSeconds)`
  - `static TimeSpan? EcfTransmitRetryPolicy.NextDelay(int attemptsMade, IReadOnlyList<TimeSpan> delays)`
  - `static bool EcfTransmitRetryPolicy.IsOwnDuplicate(DgiiStatusResponse status, bool earlierAttemptsWithoutTrackId)`

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfTransmitRetryPolicyTests.cs`:

```csharp
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfTransmitRetryPolicyTests
{
    // ─── Normalize ─────────────────────────────────────────────────

    [Fact]
    public void Normalize_WithoutConfiguration_UsesTheDefaults()
    {
        var delays = EcfTransmitRetryPolicy.Normalize(null);

        Assert.Equal([TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)], delays);
    }

    [Fact]
    public void Normalize_EmptyConfiguration_UsesTheDefaults()
    {
        Assert.Equal(3, EcfTransmitRetryPolicy.Normalize([]).Count);
    }

    [Fact]
    public void Normalize_KeepsTheConfiguredValuesInOrder()
    {
        var delays = EcfTransmitRetryPolicy.Normalize([2, 10]);

        Assert.Equal([TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)], delays);
    }

    [Fact]
    public void Normalize_DropsNonPositiveValuesAndCapsTheMaximum()
    {
        var delays = EcfTransmitRetryPolicy.Normalize([0, -5, 7200]);

        Assert.Equal([TimeSpan.FromSeconds(3600)], delays);
    }

    [Fact]
    public void Normalize_OnlyNonPositiveValues_MeansNoRetries()
    {
        Assert.Empty(EcfTransmitRetryPolicy.Normalize([0]));
    }

    // ─── NextDelay ─────────────────────────────────────────────────

    private static readonly IReadOnlyList<TimeSpan> Delays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)];

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 30)]
    [InlineData(3, 120)]
    public void NextDelay_AfterEachFailedAttempt_IsTheNextConfiguredDelay(int attemptsMade, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), EcfTransmitRetryPolicy.NextDelay(attemptsMade, Delays));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(10)]
    public void NextDelay_OutOfRange_IsNull(int attemptsMade)
    {
        Assert.Null(EcfTransmitRetryPolicy.NextDelay(attemptsMade, Delays));
    }

    // ─── IsOwnDuplicate ────────────────────────────────────────────

    private static DgiiStatusResponse Rejected(object code, string value = "") => new()
    {
        Estado = "Rechazado",
        Codigo = "2",
        Mensajes = [new DgiiStatusMensaje { Codigo = code, Valor = value }]
    };

    [Theory]
    [InlineData(1209)]
    [InlineData(75)]
    [InlineData("1209")]
    [InlineData("75")]
    public void IsOwnDuplicate_SequenceAlreadyUsedCodes_AfterAnUnconfirmedAttempt(object code)
    {
        Assert.True(EcfTransmitRetryPolicy.IsOwnDuplicate(Rejected(code), earlierAttemptsWithoutTrackId: true));
    }

    [Fact]
    public void IsOwnDuplicate_DetectsTheTextEvenWithoutAKnownCode()
    {
        var status = Rejected(999, "El e-NCF de este Resumen de Factura de Consumo ya ha sido utilizado previamente.");

        Assert.True(EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId: true));
    }

    [Fact]
    public void IsOwnDuplicate_WithoutAnUnconfirmedEarlierAttempt_IsAGenuineBurnedSequence()
    {
        Assert.False(EcfTransmitRetryPolicy.IsOwnDuplicate(Rejected(1209), earlierAttemptsWithoutTrackId: false));
    }

    [Fact]
    public void IsOwnDuplicate_OtherRejections_AreNotDuplicates()
    {
        var status = Rejected(1940, "Monto gravado inválido.");

        Assert.False(EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId: true));
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfTransmitRetryPolicyTests"`
Expected: error de compilación (`EcfTransmitRetryPolicy` no existe).

- [ ] **Step 3: Implementar**

Crear `ZynstormECFPlatform.Services/Production/EcfTransmitRetryPolicy.cs`:

```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Reglas del reintento de la transmisión diferida. Solo se reintentan los fallos de
/// transporte (timeout, red caída): un rechazo de la DGII no se reintenta, porque reenviar el
/// mismo XML volvería a ser rechazado.
/// </summary>
public static class EcfTransmitRetryPolicy
{
    public static readonly int[] DefaultDelaysSeconds = [5, 30, 120];

    private const int MaxDelaySeconds = 3600;

    /// <summary>
    /// Esperas entre intentos a partir de la configuración. Sin configuración (o vacía) se usan
    /// las de por defecto; los valores no positivos se descartan y el máximo es una hora.
    /// Una configuración con solo ceros significa "sin reintentos".
    /// </summary>
    public static IReadOnlyList<TimeSpan> Normalize(int[]? configuredSeconds)
    {
        var source = configuredSeconds is { Length: > 0 } ? configuredSeconds : DefaultDelaysSeconds;

        return source
            .Where(seconds => seconds > 0)
            .Select(seconds => TimeSpan.FromSeconds(Math.Min(seconds, MaxDelaySeconds)))
            .ToList();
    }

    /// <summary>
    /// Espera antes del próximo intento, dado cuántos intentos ya se hicieron (el primero es 1).
    /// Null cuando ya no quedan reintentos.
    /// </summary>
    public static TimeSpan? NextDelay(int attemptsMade, IReadOnlyList<TimeSpan> delays) =>
        attemptsMade >= 1 && attemptsMade <= delays.Count ? delays[attemptsMade - 1] : null;

    /// <summary>
    /// La DGII dijo "secuencia ya utilizada" (1209 o 75) en un reintento de un documento cuyos
    /// intentos anteriores nunca recibieron TrackId. Lo más probable es que el primer envío sí
    /// llegara y la respuesta se perdiera: no es un número quemado por otro comprobante. Sin
    /// intentos previos sin confirmar, el mismo mensaje sí es una secuencia quemada.
    /// </summary>
    public static bool IsOwnDuplicate(DgiiStatusResponse status, bool earlierAttemptsWithoutTrackId)
    {
        if (!earlierAttemptsWithoutTrackId)
            return false;

        if (status.Mensajes.Any(message => IsSequenceUsedCode(message.Codigo) || SaysAlreadyUsed(message.Valor)))
            return true;

        return SaysAlreadyUsed(status.Error) || SaysAlreadyUsed(status.Mensaje);
    }

    private static bool IsSequenceUsedCode(object? code) =>
        Convert.ToString(code, System.Globalization.CultureInfo.InvariantCulture) is "1209" or "75";

    private static bool SaysAlreadyUsed(string? text) =>
        text?.Contains("ya ha sido utilizado", StringComparison.OrdinalIgnoreCase) ?? false;
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfTransmitRetryPolicyTests"`
Expected: `Passed!`, todas en verde.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfTransmitRetryPolicy.cs ZynstormECFPlatform.Tests/Production/EcfTransmitRetryPolicyTests.cs
git commit -m "feat(ecf): política de reintentos de la transmisión diferida

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 2: Marcar los fallos de transporte hacia la DGII

**Files:**
- Modify: `ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs`
- Modify: `ZynstormECFPlatform.Services/DgiiTransmissionService.cs`
- Create: `ZynstormECFPlatform.Tests/Production/DgiiTransmissionServiceTests.cs`

**Interfaces:**
- Produces: `bool DgiiTransmissionResult.TransportFailure` (true cuando no hubo respuesta de la DGII: timeout o red caída).

Hoy `SendEcfAsync` solo captura `HttpRequestException`; un timeout (`TaskCanceledException`) se escapa al `catch` general de `ProcessAsync`.

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/DgiiTransmissionServiceTests.cs`:

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTransmissionServiceTests
{
    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw exception;
    }

    private static DgiiTransmissionService ServiceThatThrows(Exception exception)
    {
        // El validador interno usa PlatformUrl y no necesita las URLs de la DGII.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EcfXmlValidation:UseInternalValidator"] = "true",
                ["AppSettings:PlatformUrl"] = "https://ecfstaging.zynstorm.com/api"
            })
            .Build();

        return new DgiiTransmissionService(
            new HttpClient(new ThrowingHandler(exception)),
            configuration,
            NullLogger<DgiiTransmissionService>.Instance);
    }

    [Fact]
    public async Task Timeout_IsReportedAsATransportFailure()
    {
        var service = ServiceThatThrows(new TaskCanceledException("timeout"));

        var result = await service.SendEcfAsync(DgiiEnvironment.Test, "token", "<x/>", 32, 10m, "132293894", "E320000000098");

        Assert.True(result.TransportFailure);
        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public async Task ConnectionFailure_IsReportedAsATransportFailure()
    {
        var service = ServiceThatThrows(new HttpRequestException("no route"));

        var result = await service.SendEcfAsync(DgiiEnvironment.Test, "token", "<x/>", 32, 10m, "132293894", "E320000000098");

        Assert.True(result.TransportFailure);
        Assert.False(result.Success);
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~DgiiTransmissionServiceTests"`
Expected: error de compilación (`TransportFailure` no existe). Si falta `Microsoft.Extensions.Configuration` o `Microsoft.Extensions.Logging.Abstractions` en el proyecto de pruebas, agregar a `ZynstormECFPlatform.Tests.csproj` `<PackageReference Include="Microsoft.Extensions.Configuration" Version="10.0.5" />` y `<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.5" />`.

- [ ] **Step 3: Agregar el campo**

En `ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs`, dentro de `DgiiTransmissionResult`, después de `public string? SignedXml { get; set; }` agregar:

```csharp

    /// <summary>
    /// No hubo respuesta de la DGII: timeout o red caída. Es lo único que se reintenta de forma
    /// automática, porque no se sabe si el documento llegó.
    /// </summary>
    public bool TransportFailure { get; set; }
```

- [ ] **Step 4: Marcar el fallo en `SendEcfAsync`**

En `ZynstormECFPlatform.Services/DgiiTransmissionService.cs`, en `SendEcfAsync`, reemplazar:

```csharp
                Mensaje = ex.InnerException?.Message ?? ex.Message
            };
        }

        var responseString = await response.Content.ReadAsStringAsync();
        LogDgiiRawResponse("SendEcf",
```

por:

```csharp
                Mensaje = ex.InnerException?.Message ?? ex.Message,
                TransportFailure = true
            };
        }
        catch (TaskCanceledException ex)
        {
            // HttpClient reporta su timeout como cancelación: la DGII no respondió a tiempo.
            _logger.LogError(
                ex,
                "DGII SendEcf timed out. Environment={Environment} Endpoint={Endpoint} EcfType={EcfType} ENcf={ENcf} IsRfce={IsRfce}",
                environment,
                endpointUrl,
                ecfType,
                eNcf,
                isSummary);

            return new DgiiTransmissionResult
            {
                Error = $"La DGII no respondió a tiempo para {(isSummary ? "Resumen B2C/RFCE" : "e-CF")}. Endpoint: {endpointUrl}.",
                Mensaje = ex.Message,
                TransportFailure = true
            };
        }

        var responseString = await response.Content.ReadAsStringAsync();
        LogDgiiRawResponse("SendEcf",
```

- [ ] **Step 5: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~DgiiTransmissionServiceTests"`
Expected: `Passed!`, 2 pruebas en verde.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Abstractions/Services/IDgiiTransmissionService.cs ZynstormECFPlatform.Services/DgiiTransmissionService.cs ZynstormECFPlatform.Tests/Production/DgiiTransmissionServiceTests.cs
git commit -m "fix(ecf): marcar timeout y red caída hacia la DGII como fallo de transporte

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Caché de datos de referencia

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/EcfReferenceCache.cs`
- Create: `ZynstormECFPlatform.Tests/Production/EcfReferenceCacheTests.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Modify: `Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md`

**Interfaces:**
- Produces: `Task<T?> EcfReferenceCache.GetOrLoadAsync<T>(string key, Func<Task<T?>> load) where T : class`. No cachea la ausencia (null).

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Production/EcfReferenceCacheTests.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfReferenceCacheTests
{
    private sealed class FakeCache : ICacheService
    {
        public Dictionary<string, object> Items { get; } = [];

        public TimeSpan? LastExpiration { get; private set; }

        public T? Get<T>(string key) => Items.TryGetValue(key, out var value) ? (T)value : default;

        public void Set<T>(string key, T value, TimeSpan expiration)
        {
            Items[key] = value!;
            LastExpiration = expiration;
        }

        public void Remove(string key) => Items.Remove(key);
    }

    private sealed record Currency(string Code);

    [Fact]
    public async Task SecondCall_ComesFromTheCache()
    {
        var cache = new EcfReferenceCache(new FakeCache());
        var loads = 0;

        Task<Currency?> Load() { loads++; return Task.FromResult<Currency?>(new Currency("DOP")); }

        var first = await cache.GetOrLoadAsync("ecf-ref:currency", Load);
        var second = await cache.GetOrLoadAsync("ecf-ref:currency", Load);

        Assert.Equal(1, loads);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Absence_IsNotCached()
    {
        var cache = new EcfReferenceCache(new FakeCache());
        var loads = 0;

        Task<Currency?> Load() { loads++; return Task.FromResult<Currency?>(null); }

        Assert.Null(await cache.GetOrLoadAsync("ecf-ref:currency", Load));
        Assert.Null(await cache.GetOrLoadAsync("ecf-ref:currency", Load));

        Assert.Equal(2, loads);
    }

    [Fact]
    public async Task DifferentKeys_AreIndependent()
    {
        var cache = new EcfReferenceCache(new FakeCache());

        var one = await cache.GetOrLoadAsync("ecf-ref:type:31", () => Task.FromResult<Currency?>(new Currency("31")));
        var two = await cache.GetOrLoadAsync("ecf-ref:type:32", () => Task.FromResult<Currency?>(new Currency("32")));

        Assert.Equal("31", one!.Code);
        Assert.Equal("32", two!.Code);
    }

    [Fact]
    public async Task Entries_ExpireAfterFiveMinutes()
    {
        var fake = new FakeCache();
        var cache = new EcfReferenceCache(fake);

        await cache.GetOrLoadAsync("ecf-ref:currency", () => Task.FromResult<Currency?>(new Currency("DOP")));

        Assert.Equal(TimeSpan.FromMinutes(5), fake.LastExpiration);
    }
}
```

- [ ] **Step 2: Ejecutar y comprobar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfReferenceCacheTests"`
Expected: error de compilación (`EcfReferenceCache` no existe).

- [ ] **Step 3: Implementar**

Crear `ZynstormECFPlatform.Services/Production/EcfReferenceCache.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Caché de datos que casi nunca cambian y que cada emisión consultaba a la base de datos:
/// moneda, tipo de e-CF y sucursal principal. TTL de 5 minutos y sin invalidación.
///
/// Nunca se guardan aquí el cliente (su ClientInactive cambia por pagos, recordatorios y
/// ediciones, y un cliente suspendido no debe seguir emitiendo), la API key ni el
/// certificado.
/// </summary>
public sealed class EcfReferenceCache(ICacheService cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<T?> GetOrLoadAsync<T>(string key, Func<Task<T?>> load) where T : class
    {
        var cached = cache.Get<T>(key);

        if (cached is not null)
            return cached;

        var loaded = await load();

        // No se cachea la ausencia: un dato que todavía no existe debe poder aparecer.
        if (loaded is not null)
            cache.Set(key, loaded, Ttl);

        return loaded;
    }
}
```

- [ ] **Step 4: Ejecutar y comprobar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~EcfReferenceCacheTests"`
Expected: `Passed!`, 4 pruebas en verde.

- [ ] **Step 5: Registrar la caché**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, después de la línea `services.AddTransient<Production.IEcfLookupService, Production.EcfLookupService>();` agregar:

```csharp
        services.AddSingleton<Production.EcfReferenceCache>();
```

- [ ] **Step 6: Usarla en `ProcessAsync`**

En `ReceivedEcfProductionService.cs`:

1. Después de `private readonly IEcfLookupService _lookupService;` agregar:

```csharp
    private readonly EcfReferenceCache _referenceCache;
```

2. En el constructor, reemplazar `IEcfLookupService lookupService)` por:

```csharp
        IEcfLookupService lookupService,
        EcfReferenceCache referenceCache)
```

y después de `_lookupService = lookupService;` agregar:

```csharp
        _referenceCache = referenceCache;
```

3. En `ProcessAsync`, reemplazar:

```csharp
        var clientBranch = await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
            ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId);
        var currency = await _currencyService.GetByAsync(x => x.Code == "DOP")
            ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0);
```

por:

```csharp
        var clientBranch = await _referenceCache.GetOrLoadAsync(
            $"ecf-ref:branch:{client.ClientId}",
            async () => await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
                ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId));
        var currency = await _referenceCache.GetOrLoadAsync(
            "ecf-ref:currency",
            async () => await _currencyService.GetByAsync(x => x.Code == "DOP")
                ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0));
```

y reemplazar:

```csharp
        var ecfTypeEntity = await _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString());
```

por:

```csharp
        var ecfTypeEntity = await _referenceCache.GetOrLoadAsync(
            $"ecf-ref:ecftype:{ecfType}",
            () => _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString()));
```

- [ ] **Step 7: Corregir el spec**

En `Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md`, reemplazar el punto 2 de «Rendimiento»:

```
2. **Menos viajes a la base de datos antes de firmar.** Moneda DOP, tipo de e-CF y los datos
   del cliente (cliente, API key, sucursal principal) casi nunca cambian: se cachean en
   memoria con `ICacheService` y TTL corto (5 min) y se invalidan al editar el cliente.
   **No** se cachea el certificado ni su contraseña descifrada: esos se leen y descifran en
   cada firma.
```

por:

```
2. **Menos viajes a la base de datos antes de firmar.** Moneda DOP, tipo de e-CF y sucursal
   principal casi nunca cambian: se cachean en memoria con `ICacheService` y TTL de 5 min, sin
   invalidación. **No** se cachea el cliente (su `ClientInactive` cambia por pagos,
   recordatorios y ediciones, y un cliente suspendido no debe seguir emitiendo), ni la API key,
   ni el certificado ni su contraseña descifrada: esos se leen en cada emisión.
```

- [ ] **Step 8: Compilar y probar todo**

Run: `dotnet build ZynstormECFPlatform.slnx` y `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos.

- [ ] **Step 9: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/EcfReferenceCache.cs ZynstormECFPlatform.Tests/Production/EcfReferenceCacheTests.cs ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs Docs/superpowers/specs/2026-10-07-emit-idempotente-diferido-design.md
git commit -m "perf(ecf): caché de moneda, tipo de e-CF y sucursal principal en emit

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 4: Extraer la transmisión y el seguimiento (sin cambiar el comportamiento)

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

**Interfaces:**
- Produces (privados):
  - `enum TransmitStep { Completed, TransportFailure }`
  - `Task<TransmitStep> TransmitAndTrackAsync(ReceivedEcfEmissionResultDto resultDto, EcfDocument ecfDocument, int clientId, string signedXml, int ecfType, decimal total, string issuerRnc, string eNcf, bool isSummary, DgiiEnvironment targetEnvironment, string certBase64, string certPass, Task<string>? tokenTask, int statusDelayMilliseconds, EcfEmitTimings? timings, CancellationToken cancellationToken)`
  - `Task<(string CertBase64, string CertPass)?> LoadSigningMaterialAsync(int clientId, ApiKey apiKey)`

Es una extracción pura: no hay prueba nueva; el comportamiento del modo síncrono no cambia. Se verifica con el build, las pruebas existentes y la prueba manual de la Task 7 (sin `deferred`).

- [ ] **Step 1: Agregar `LoadSigningMaterialAsync`**

En `ReceivedEcfProductionService.cs`, justo antes de `private DgiiEnvironment ResolveTargetEnvironment(`, agregar:

```csharp
    /// <summary>
    /// Certificado del cliente y su contraseña, descifrados con la clave de su API key. Null si el
    /// cliente no tiene un certificado activo. Se lee de la base de datos en cada uso: nunca se
    /// cachea ni se pasa como argumento de un job.
    /// </summary>
    private async Task<(string CertBase64, string CertPass)?> LoadSigningMaterialAsync(int clientId, ApiKey apiKey)
    {
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey ?? string.Empty);
        var certificate = await _clientCertificateService.GetActiveCertificateAsync(x => x.ClientId == clientId);

        if (certificate == null)
            return null;

        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

        return (Convert.ToBase64String(certificateBytes), Encoding.UTF8.GetString(passwordBytes));
    }

```

- [ ] **Step 2: Usarlo en `ContinueProcessingAsync`**

Reemplazar:

```csharp
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey ?? string.Empty);
        var certificate = await _clientCertificateService.GetActiveCertificateAsync(x => x.ClientId == client.ClientId);
        if (certificate == null)
        {
            FailConfiguration(resultDto, "Certificado no encontrado.");
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message);
            return resultDto;
        }
        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
        var certBase64 = Convert.ToBase64String(certificateBytes);
        var certPass = Encoding.UTF8.GetString(passwordBytes);
```

por:

```csharp
        var material = await LoadSigningMaterialAsync(client.ClientId, apiKey);
        if (material is null)
        {
            FailConfiguration(resultDto, "Certificado no encontrado.");
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message);
            return resultDto;
        }
        var (certBase64, certPass) = material.Value;
```

- [ ] **Step 3: Mover la transmisión a `TransmitAndTrackAsync`**

En `ContinueProcessingAsync`, reemplazar **todo** desde la línea `if (useStagingValidation)` (la que sigue a `timings.Mark("guardado_previo");`) hasta el `return resultDto;` final del método (el que sigue a `if (resultDto.Success) { ... RegisterAcceptedAsync ... }`) por:

```csharp
        await TransmitAndTrackAsync(
            resultDto,
            ecfDocument,
            client.ClientId,
            signedXml,
            ecfType,
            total,
            issuerRnc,
            eNcf,
            isSummary,
            targetEnvironment,
            certBase64,
            certPass,
            tokenTask,
            statusDelayMilliseconds,
            timings,
            cancellationToken);

        return resultDto;
    }

    private enum TransmitStep { Completed, TransportFailure }

    /// <summary>
    /// Transmite el documento ya firmado a la DGII y sigue su estado hasta donde se pueda.
    /// Lo usan el modo síncrono de <c>emit</c> y, más adelante, el job de transmisión diferida.
    /// </summary>
    private async Task<TransmitStep> TransmitAndTrackAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        string signedXml,
        int ecfType,
        decimal total,
        string issuerRnc,
        string eNcf,
        bool isSummary,
        DgiiEnvironment targetEnvironment,
        string certBase64,
        string certPass,
        Task<string>? tokenTask,
        int statusDelayMilliseconds,
        EcfEmitTimings? timings,
        CancellationToken cancellationToken)
    {
        if (ShouldUseStagingXmlValidation())
        {
            await ProcessWithStagingValidationAsync(
                resultDto,
                ecfDocument,
                clientId,
                signedXml,
                statusDelayMilliseconds,
                issuerRnc,
                targetEnvironment,
                certBase64,
                certPass);

            return TransmitStep.Completed;
        }

        var token = await tokenTask!;

        var transmission = await _transmissionService.SendEcfAsync(targetEnvironment, token, signedXml, ecfType, total, issuerRnc, eNcf, isSummary);
        await AddDgiiResponseLogAsync(ecfDocument, clientId, "recepcion", targetEnvironment, transmission);
        timings?.Mark("recepcion_dgii");

        resultDto.Transmission = transmission;
        resultDto.TrackId = transmission.TrackId;

        if (!transmission.Success)
        {
            resultDto.Message = BuildDgiiTransmissionError(transmission);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 12, signedXml);
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, clientId, "Error", resultDto.Message, JsonSerializer.Serialize(transmission));
            return TransmitStep.Completed;
        }

        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml);

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

            return TransmitStep.Completed;
        }

        resultDto.Success = string.Equals(transmission.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || transmission.Codigo is 0 or 1;
        resultDto.Message = resultDto.Success ? $"DGII: {transmission.Estado ?? "Aceptado"}" : BuildDgiiTransmissionError(transmission);
        var finalStatusId = resultDto.Success ? 10 : 12;

        await SaveTransmissionAsync(ecfDocument, transmission, finalStatusId, signedXml);
        await MarkDocumentAsync(ecfDocument, finalStatusId, resultDto.Message);
        await AddLogAsync(ecfDocument, clientId, resultDto.Success ? "Information" : "Error", resultDto.Message, JsonSerializer.Serialize(transmission));

        if (resultDto.Success)
        {
            await AddLogAsync(ecfDocument, clientId, "Information", $"e-CF aprobado. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");
            await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId, cancellationToken);
        }

        return TransmitStep.Completed;
    }
```

- [ ] **Step 4: Ajustar la variable `useStagingValidation`**

En `ContinueProcessingAsync`, la variable `useStagingValidation` sigue usándose para decidir `tokenTask`; no se toca. Si el compilador marca otra referencia a `useStagingValidation` que ya no exista, es porque quedó dentro del bloque movido: no debería quedar ninguna fuera de la línea `var useStagingValidation = ShouldUseStagingXmlValidation();` y de `tokenTask`.

- [ ] **Step 5: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx` y `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos. Revisar `git diff --stat`: solo `ReceivedEcfProductionService.cs`, con un diff pequeño en líneas netas (el bloque se movió, no se reescribió).

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs
git commit -m "refactor(ecf): extraer la transmisión y el seguimiento de ContinueProcessingAsync

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Modo diferido, job de transmisión y servidores de Hangfire

**Files:**
- Create: `ZynstormECFPlatform.Services/Jobs/EcfTransmitJob.cs`
- Modify: `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Program.cs`

**Interfaces:**
- Consumes: `EcfTransmitRetryPolicy.Normalize / NextDelay / IsOwnDuplicate` (Task 1), `DgiiTransmissionResult.TransportFailure` (Task 2), `TransmitAndTrackAsync`, `LoadSigningMaterialAsync` (Task 4).
- Produces:
  - `ProcessAsync(..., bool deferred = false)` (parámetro nuevo al final).
  - `Task<DeferredTransmitOutcome> IReceivedEcfProductionService.TransmitDeferredAsync(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment, CancellationToken cancellationToken = default)`
  - `sealed record DeferredTransmitOutcome(TimeSpan? RetryAfter)`; `RetryAfter` null significa que no hay más que hacer.
  - `EcfTransmitJob.Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)`.

Sin pruebas xUnit en esta tarea (orquestación con base de datos y Hangfire): las decisiones que importan (esperas, duplicado propio, fallo de transporte) ya están probadas en las Tasks 1 y 2. Se verifica con el build y con la Task 7.

- [ ] **Step 1: Ampliar la interfaz del servicio**

En `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs`, reemplazar:

```csharp
    Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750,
        CancellationToken cancellationToken = default);
}
```

por:

```csharp
    Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750,
        CancellationToken cancellationToken = default,
        bool deferred = false);

    /// <summary>
    /// Transmite a la DGII un documento que <c>emit</c> dejó firmado y en cola (estado 7), y sigue
    /// su estado. Lo ejecuta <c>EcfTransmitJob</c>. Devuelve cuánto esperar para reintentar si
    /// hubo un fallo de transporte; sin espera, no queda nada por hacer.
    /// </summary>
    Task<DeferredTransmitOutcome> TransmitDeferredAsync(
        int ecfDocumentId,
        int attemptNumber,
        DgiiEnvironment environment,
        CancellationToken cancellationToken = default);
}

public sealed record DeferredTransmitOutcome(TimeSpan? RetryAfter);
```

- [ ] **Step 2: Crear el job**

Crear `ZynstormECFPlatform.Services/Jobs/EcfTransmitJob.cs`:

```csharp
using Hangfire;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Transmite a la DGII un e-CF que <c>emit?deferred=true</c> ya firmó y respondió. Cola propia
/// para que el envío no compita con los jobs de mantenimiento. Hangfire no reintenta: los
/// reintentos por fallo de transporte los programa este mismo job con esperas crecientes.
///
/// Solo recibe identificadores: el certificado y su contraseña se recargan de la base de
/// datos, porque Hangfire guarda los argumentos de un job en claro.
/// </summary>
[Queue("ecf-transmit")]
[AutomaticRetry(Attempts = 0)]
public class EcfTransmitJob(IReceivedEcfProductionService service)
{
    public async Task Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)
    {
        var outcome = await service.TransmitDeferredAsync(ecfDocumentId, attemptNumber, environment);

        if (outcome.RetryAfter is { } delay)
        {
            BackgroundJob.Schedule<EcfTransmitJob>(
                job => job.Execute(ecfDocumentId, attemptNumber + 1, environment),
                delay);
        }
    }
}
```

- [ ] **Step 3: Registrar el job**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, justo después de la línea `services.AddTransient<Jobs.EcfTrackingJob>();` agregar:

```csharp
        services.AddTransient<Jobs.EcfTransmitJob>();
```

- [ ] **Step 4: Parámetro `deferred` en `ProcessAsync` y en el flujo**

En `ReceivedEcfProductionService.cs`:

1. Agregar `using Microsoft.EntityFrameworkCore;` a los `using` de arriba.

2. En la firma de `ProcessAsync`, reemplazar `CancellationToken cancellationToken = default)` (el de `ProcessAsync`, el que sigue a `int statusDelayMilliseconds = 750,`) por:

```csharp
        CancellationToken cancellationToken = default,
        bool deferred = false)
```

3. En `ProcessAsync`, en la llamada a `ContinueProcessingAsync`, reemplazar `issuerRnc, eNcf, statusDelayMilliseconds, timings, cancellationToken);` por:

```csharp
                issuerRnc, eNcf, statusDelayMilliseconds, timings, deferred, cancellationToken);
```

4. En la firma de `ContinueProcessingAsync`, reemplazar:

```csharp
        EcfEmitTimings timings,
        CancellationToken cancellationToken)
    {
        _ecfStatusHistoryService.Add(new EcfStatusHistory
```

por:

```csharp
        EcfEmitTimings timings,
        bool deferred,
        CancellationToken cancellationToken)
    {
        _ecfStatusHistoryService.Add(new EcfStatusHistory
```

5. En `ContinueProcessingAsync`, reemplazar:

```csharp
        var tokenTask = useStagingValidation
            ? null
            : _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);
```

por:

```csharp
        // En modo diferido el token lo pide el job: no se gasta una llamada a la DGII en la respuesta.
        var tokenTask = useStagingValidation || deferred
            ? null
            : _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);
```

6. En `ContinueProcessingAsync`, justo después del bloque que agrega la fila de historial con `Message = "XML firmado y guardado."` y antes de `var sendingMessage = ...`, agregar:

```csharp
        if (deferred)
        {
            ecfDocument.EcfStatusId = 7;
            _ecfDocumentService.Modify(ecfDocument);
            _ecfStatusHistoryService.Add(new EcfStatusHistory
            {
                EcfDocumentId = ecfDocument.EcfDocumentId,
                EcfStatusId = 7,
                Message = "Firmado; la transmisión a la DGII quedó en cola."
            });
            _systemLogService.Add(new SystemLog
            {
                ClientId = client.ClientId,
                EcfDocumentId = ecfDocument.EcfDocumentId,
                LogLevel = "Information",
                Message = $"Modo diferido: se respondió al firmar y la transmisión corre en segundo plano ({targetEnvironment}).",
                CreateAtUtc = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            timings.Mark("guardado_previo");

            // No se guarda el id del job en el documento: UpdateAsync reescribe el documento
            // completo y podría pisar el estado 8 que el job ya escribió.
            var queuedJobId = BackgroundJob.Enqueue<EcfTransmitJob>(
                job => job.Execute(ecfDocument.EcfDocumentId, 1, targetEnvironment));

            resultDto.Success = false;
            resultDto.IsPending = true;
            resultDto.HangfireJobId = queuedJobId;
            resultDto.Message = "Firmado; la transmisión a la DGII está en curso.";

            return resultDto;
        }

```

- [ ] **Step 5: Intentos y fallos de transporte en `TransmitAndTrackAsync`**

1. Reemplazar la firma de `TransmitAndTrackAsync`:

```csharp
        EcfEmitTimings? timings,
        CancellationToken cancellationToken)
    {
        if (ShouldUseStagingXmlValidation())
```

por:

```csharp
        EcfEmitTimings? timings,
        int attemptNumber,
        bool isDeferred,
        bool earlierAttemptsWithoutTrackId,
        CancellationToken cancellationToken)
    {
        if (ShouldUseStagingXmlValidation())
```

2. En `ContinueProcessingAsync`, la llamada a `TransmitAndTrackAsync`: reemplazar

```csharp
            timings,
            cancellationToken);

        return resultDto;
```

por:

```csharp
            timings,
            attemptNumber: 1,
            isDeferred: false,
            earlierAttemptsWithoutTrackId: false,
            cancellationToken);

        return resultDto;
```

3. En `TransmitAndTrackAsync`, justo antes de `if (!transmission.Success)` agregar:

```csharp
        // Diferido: un fallo de transporte (no se sabe si llegó) no es un error del documento. Se
        // deja el rastro del intento y quien llama decide si reintenta.
        if (isDeferred && transmission.TransportFailure)
        {
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 12, signedXml, attemptNumber: attemptNumber);
            await AddLogAsync(
                ecfDocument,
                clientId,
                "Warning",
                $"Fallo de transporte hacia la DGII en el intento {attemptNumber}: {BuildDgiiTransmissionError(transmission)}",
                JsonSerializer.Serialize(transmission));

            return TransmitStep.TransportFailure;
        }

```

4. En `TransmitAndTrackAsync`, reemplazar `var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml);` por:

```csharp
            var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml, attemptNumber);
```

5. En `TransmitAndTrackAsync`, justo después de la línea `resultDto.Message = resultDto.Success ? $"TrackId: {transmission.TrackId}" : BuildDgiiStatusError(status);` agregar:

```csharp

            // 1209/75 en un reintento propio: el primer envío probablemente sí llegó y la
            // respuesta se perdió. No es una secuencia quemada ni un rechazo: se deja Pendiente
            // con un aviso, sin Status/DgiiResponse rechazados (la librería los clasificaría
            // como secuencia quemada) y sin pedir un NCF nuevo.
            if (EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId))
            {
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
```

6. Cambiar `SaveTransmissionAsync` y `BeginTransmissionAsync` para aceptar el número de intento. Reemplazar la firma de `SaveTransmissionAsync`:

```csharp
    private async Task SaveTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        int statusId,
        string signedXml,
        DgiiStatusResponse? status = null)
    {
        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = 1,
```

por:

```csharp
    private async Task SaveTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        int statusId,
        string signedXml,
        DgiiStatusResponse? status = null,
        int attemptNumber = 1)
    {
        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = attemptNumber,
```

y la firma y el cuerpo de `BeginTransmissionAsync`:

```csharp
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
```

por:

```csharp
    private async Task<EcfTransmission> BeginTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        string signedXml,
        int attemptNumber = 1)
    {
        var record = new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = attemptNumber,
```

- [ ] **Step 6: Implementar `TransmitDeferredAsync`**

En `ReceivedEcfProductionService.cs`, justo antes de `private DgiiEnvironment ResolveTargetEnvironment(`, agregar:

```csharp
    public async Task<DeferredTransmitOutcome> TransmitDeferredAsync(
        int ecfDocumentId,
        int attemptNumber,
        DgiiEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var ecfDocument = await _ecfDocumentService.GetAsync(ecfDocumentId);

        if (ecfDocument is null)
            return new DeferredTransmitOutcome(null);

        // Solo se transmite lo que quedó en cola (7). Si otra ejecución ya lo pasó a 8 o ya
        // está aceptado, no se hace nada: así dos ejecuciones nunca transmiten el mismo documento.
        if (ecfDocument.EcfStatusId != 7)
        {
            await AddLogAsync(
                ecfDocument,
                ecfDocument.ClientId,
                "Information",
                $"Transmisión diferida omitida (intento {attemptNumber}): el documento está en estado {ecfDocument.EcfStatusId}, no en cola.");

            return new DeferredTransmitOutcome(null);
        }

        var clientId = ecfDocument.ClientId;
        await MarkDocumentAsync(ecfDocument, 8, $"Enviando e-CF a DGII (intento {attemptNumber}).");

        TransmitStep step;

        try
        {
            var client = await _clientService.GetAsync(clientId)
                ?? throw new InvalidOperationException($"No se encontró el cliente {clientId} del documento {ecfDocumentId}.");
            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == clientId)
                ?? throw new InvalidOperationException($"No hay API key para el cliente {clientId}.");

            var material = await LoadSigningMaterialAsync(clientId, apiKey);

            if (material is null)
            {
                await MarkDocumentAsync(ecfDocument, 3, "Certificado no encontrado.");
                await AddLogAsync(ecfDocument, clientId, "Warning", "Certificado no encontrado; no se pudo transmitir.");
                return new DeferredTransmitOutcome(null);
            }

            var (certBase64, certPass) = material.Value;

            var xml = await _ecfXmlDocumentService.GetByAsync(x => x.EcfDocumentId == ecfDocumentId)
                ?? throw new InvalidOperationException($"El documento {ecfDocumentId} no tiene XML firmado.");

            var ecfType = NcfHelper.ExtractEcfType(ecfDocument.Ncf);
            var total = ecfDocument.Total;
            var issuerRnc = client.Rnc;
            var targetEnvironment = ResolveTargetEnvironment(environment);

            var earlierAttemptsWithoutTrackId = attemptNumber > 1
                && !await _ecfTransmissionService.Table.AnyAsync(
                    t => t.EcfDocumentId == ecfDocumentId && t.TrackId != string.Empty,
                    cancellationToken);

            var tokenTask = ShouldUseStagingXmlValidation()
                ? null
                : _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);

            var resultDto = new ReceivedEcfEmissionResultDto
            {
                EcfDocumentId = ecfDocumentId,
                ENcf = ecfDocument.Ncf,
                EcfType = ecfType,
                SecurityCode = string.Empty,
                SignatureDate = string.Empty,
                QrUrl = string.Empty
            };

            step = await TransmitAndTrackAsync(
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
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo inesperado (token, red, base de datos) se trata como transporte: se
            // reintenta con las mismas esperas y, agotadas, el documento queda en Error.
            await AddLogAsync(
                ecfDocument,
                clientId,
                "Error",
                $"Fallo inesperado al transmitir (intento {attemptNumber}): {ex.Message}",
                ex.ToString());

            step = TransmitStep.TransportFailure;
        }

        if (step == TransmitStep.Completed)
            return new DeferredTransmitOutcome(null);

        var delays = EcfTransmitRetryPolicy.Normalize(
            _configuration.GetSection("EcfTransmit:RetryDelaysSeconds").Get<int[]>());
        var delay = EcfTransmitRetryPolicy.NextDelay(attemptNumber, delays);

        if (delay is { } wait)
        {
            await MarkDocumentAsync(
                ecfDocument,
                7,
                $"La DGII no respondió en el intento {attemptNumber}; se reintentará en {wait.TotalSeconds:0} s.");

            return new DeferredTransmitOutcome(wait);
        }

        await MarkDocumentAsync(
            ecfDocument,
            12,
            $"Se agotaron los reintentos de transmisión a la DGII ({attemptNumber} intentos). El documento se puede reenviar con el mismo eNCF.");
        await AddLogAsync(ecfDocument, clientId, "Error", $"Se agotaron los reintentos de transmisión tras {attemptNumber} intentos.");

        return new DeferredTransmitOutcome(null);
    }

```

- [ ] **Step 7: Parámetro `deferred` en el controlador**

En `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`:

1. Reemplazar `public async Task<IActionResult> EmitEcf([FromBody] EcfInvoiceRequestDto dto, [FromQuery] DgiiEnvironment environment = DgiiEnvironment.Test)` por:

```csharp
        public async Task<IActionResult> EmitEcf(
            [FromBody] EcfInvoiceRequestDto dto,
            [FromQuery] DgiiEnvironment environment = DgiiEnvironment.Test,
            [FromQuery] bool deferred = false)
```

2. Reemplazar:

```csharp
                var result = await _receivedEcfProductionService.ProcessAsync(
                    dto,
                    environment,
                    cancellationToken: HttpContext.RequestAborted);
```

por:

```csharp
                var result = await _receivedEcfProductionService.ProcessAsync(
                    dto,
                    environment,
                    cancellationToken: HttpContext.RequestAborted,
                    deferred: deferred);
```

3. Reemplazar:

```csharp
                // Un replay pendiente no es un fallo de la plataforma: devolvió lo guardado.
                if (result.IsPending && !result.Replayed)
```

por:

```csharp
                // Un replay o una respuesta diferida pendiente no son un fallo de la plataforma:
                // devolvieron lo guardado o firmaron y dejaron la transmisión en cola.
                if (result.IsPending && !result.Replayed && !deferred)
```

- [ ] **Step 8: Servidores de Hangfire**

En `ZynstormECFPlatform.Web.Api/Program.cs`, reemplazar:

```csharp
builder.Services.AddHangfireServer();
```

por:

```csharp
// Servidor por defecto: mantenimiento, reportes, recordatorios y seguimiento de estado.
builder.Services.AddHangfireServer(options => options.Queues = ["default"]);

// Servidor propio para transmitir e-CF a la DGII: el envío en segundo plano no compite con los
// demás jobs ni espera detrás de ellos.
builder.Services.AddHangfireServer(options =>
{
    options.ServerName = $"{Environment.MachineName}-ecf-transmit";
    options.Queues = ["ecf-transmit"];
    options.WorkerCount = Math.Clamp(builder.Configuration.GetValue<int?>("EcfTransmit:WorkerCount") ?? 10, 1, 50);
});
```

- [ ] **Step 9: Compilar y probar todo**

Run: `dotnet build ZynstormECFPlatform.slnx` y `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos. Si hay un `[Queue(...)]` en otro job existente distinto de `default`, ese job quedaría sin servidor: buscar con `grep -rn "\[Queue(" --include=*.cs ZynstormECFPlatform.Services ZynstormECFPlatform.Web.Api` y agregar esa cola a la lista del servidor por defecto.

- [ ] **Step 10: Commit**

```bash
git add ZynstormECFPlatform.Services/Jobs/EcfTransmitJob.cs ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs ZynstormECFPlatform.Web.Api/Program.cs
git commit -m "feat(ecf): emit diferido con job de transmisión, reintentos de transporte y cola propia

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

### Task 6: Un solo guardado al cambiar el estado de un documento

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`

`MarkDocumentAsync` hace hoy dos viajes a la base de datos (actualiza el documento y luego inserta el historial, cada uno con su `SaveChanges`) y se llama en cada cambio de estado. Pasa a uno.

- [ ] **Step 1: Reemplazar `MarkDocumentAsync`**

Reemplazar:

```csharp
    private async Task MarkDocumentAsync(EcfDocument ecfDocument, int statusId, string message)
    {
        ecfDocument.EcfStatusId = statusId;
        await _ecfDocumentService.UpdateAsync(ecfDocument);
        await AddHistoryAsync(ecfDocument, statusId, message);
    }
```

por:

```csharp
    private async Task MarkDocumentAsync(EcfDocument ecfDocument, int statusId, string message)
    {
        ecfDocument.EcfStatusId = statusId;

        // Modify + Add y un solo SaveChanges: antes eran dos viajes a la base de datos por cada
        // cambio de estado.
        _ecfDocumentService.Modify(ecfDocument);
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = statusId,
            Message = message
        });

        await _unitOfWork.SaveChangesAsync();
    }
```

- [ ] **Step 2: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx` y `dotnet test ZynstormECFPlatform.Tests`
Expected: `Build succeeded`, 0 errores; `Passed!` sin fallos. Si `AddHistoryAsync` queda sin usar, no es un error: se puede dejar (lo usan otros métodos si los hay); si el compilador avisa de método privado sin uso, eliminarlo.

- [ ] **Step 3: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs
git commit -m "perf(ecf): un solo guardado por cada cambio de estado del documento

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

Con la API key de un cliente de pruebas de `ecfstaging.zynstorm.com`:

1. **Modo síncrono intacto:** `POST v1/Ecf/emit` **sin** `deferred` con un eNCF nuevo: se acepta como antes. En `SystemLog`, «Tiempos de emit».
2. **Diferido:** `POST v1/Ecf/emit?deferred=true` con otro eNCF nuevo: responde en menos de 1 s con HTTP 200, `isPending: true`, `success: false`, `trackId` vacío y `qrUrl`, `securityCode`, `signatureDate` completos. A los pocos segundos, `GET v1/Ecf/by-ncf/{eNcf}` devuelve `state: Accepted` con `trackId`. En `EcfDocument`, estado 10.
3. **Idempotencia sobre el diferido:** repetir el mismo `POST` diferido: `replayed: true`, sin fila nueva.
4. **Cola propia:** en el panel de Hangfire hay un servidor `<máquina>-ecf-transmit` con la cola `ecf-transmit`, y el job aparece ahí.
5. **Fallo de transporte:** apuntar temporalmente `AppSettings:PlatformUrl` (o la URL de recepción que use el ambiente) a un puerto cerrado y emitir en modo diferido con un eNCF nuevo. Esperado: respuesta inmediata como en el paso 2; después, filas en `EcfTransmission` con `AttemptNumber` 1, 2, 3 y 4 separadas por unos 5 s, 30 s y 120 s; al final el documento en estado 12 con el mensaje «Se agotaron los reintentos…». Restaurar la configuración y reemitir el mismo eNCF: crea un documento nuevo (`attempt: 2`) y se acepta.
6. **Tiempos:** comparar «Tiempos de emit» del paso 1 con los de antes de esta rama; el modo diferido no tiene `espera_estado_final`.

- [ ] **Step 4: Reportar**

Informar el resultado de cada comprobación. No hacer push hasta que el usuario lo pida.

---

## Self-Review

**Cobertura del spec**
- Sección 5 (modo diferido: firmar, estado 7, encolar, responder con QR; job que recarga certificado de la base de datos, transición 7→8 condicional, mismo método de transmisión que el síncrono, ambas ramas): Tasks 4 y 5.
- Sección 5b (reintentos ante transporte con 5 s, 30 s, 2 min; entre intentos estado 7; agotados estado 12; `AttemptNumber` por intento; 1209/75 propio como `Pending`; captura de `TaskCanceledException`): Tasks 1, 2 y 5.
- Sección 6 (QR en toda respuesta posterior a la firma): ya se cumple; documentado en las restricciones, sin código.
- Rendimiento: caché (Task 3, con la corrección de que no incluye el cliente), menos `SaveChanges` (Task 6), cola propia (Task 5, paso 8), token precalentado en el job en vez de en la respuesta (Task 5, pasos 4.5 y 6), cronómetro (Plan 1).
- Contrato `emit`: parámetro `deferred` (Task 5, paso 7); `replayed` y `attempt` ya están (Plan 1).
- Pruebas del spec: política de reintentos (Task 1), fallos de transporte (Task 2), caché (Task 3). El job, el candado y el diferido se verifican con el build y la Task 7, como indicaba el spec.

**Riesgos conocidos**
- La extracción de la Task 4 no tiene prueba automática; se compensa con que es un movimiento de bloque (diff pequeño en líneas netas) y con el paso 1 de la Task 7 (modo síncrono sin cambios).
- Un documento en estado 9 por duplicado propio queda `Pending` con TrackId (el del duplicado): `EcfEmitDecision` lo trata como vivo para siempre y no permite reenviar. Es intencional (evita un tercer envío) y exige conciliar con la DGII a mano; el aviso queda en `SystemLog`.

**Placeholders:** ninguno; cada paso de código trae el código completo o la sustitución exacta.

**Consistencia de tipos:** `TransmitStep`, `TransmitAndTrackAsync` (firma de 16 parámetros en la Task 4, 19 tras la Task 5), `DeferredTransmitOutcome(TimeSpan? RetryAfter)`, `EcfTransmitRetryPolicy.Normalize/NextDelay/IsOwnDuplicate`, `DgiiTransmissionResult.TransportFailure` y `EcfTransmitJob.Execute(int, int, DgiiEnvironment)` son los mismos en todas las tareas. `earlierAttemptsWithoutTrackId` se calcula en `TransmitDeferredAsync` y se consume en `TransmitAndTrackAsync`.
