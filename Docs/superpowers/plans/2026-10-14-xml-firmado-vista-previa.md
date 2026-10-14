# XML firmado por comprobante y por rango, y vista previa del PDF Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Descargar el XML firmado de los comprobantes emitidos por comprobante y por rango de fechas (ZIP si son varios) desde el panel y la API con API key, exponerlo en la librería con sus guías, y agregar la vista previa del PDF de recibidos en el panel.

**Architecture:** Un servicio único (`SignedXmlExportService`) con la lógica pura aparte (`SignedXmlExportLogic`) sirve a `EcfController` (API key) y a `EcfDocumentController` (sesión). El panel descarga blobs con `fetch` y muestra el PDF de recibidos en un `<iframe>` sobre un blob. La librería renombra sus tipos de archivo a nombres neutros y agrega dos métodos.

**Tech Stack:** .NET 10, EF Core, `System.IO.Compression`, xUnit; Next.js + TypeScript + shadcn/ui; librería C# `net452;netstandard2.0;net8.0`.

Spec: `Docs/superpowers/specs/2026-10-14-xml-firmado-vista-previa-design.md`. Continúa `2026-10-14-recibidos-pdf.md` (ya implementado).

## Global Constraints

- Solo documentos emitidos: `EcfDocument` + `EcfXmlDocument.XmlSigned`. Los recibidos no entran.
- «Válido» = `EcfLookupLogic.IsUsable` (Aceptado, Aceptado condicional, En proceso). Un eNCF reintentado aporta un solo documento (`PickBest`).
- Rango = fecha de registro (`RegisteredAt`) en días de RD, ambos extremos inclusivos, con `fromDate.Date.ConvertDominicanLocalToUtc()` y `toDate.Date.AddDays(1).ConvertDominicanLocalToUtc()` (exclusivo), igual que `EcfDocumentController.ApplyCommonFilters`.
- Tope `SignedXmlExportLogic.MaxItems = 2000`; al pasarse, respuesta 400 `{ found:false, tooMany:true, total, message }`.
- Los bytes del XML firmado se entregan exactos: UTF-8 sin BOM, sin reformatear.
- Un comprobante → `{eNcf}.xml` (`application/xml`); más de uno → `XML_firmados_{rnc}_{yyyyMMdd}_{yyyyMMdd}.zip` (`application/zip`), una entrada `{eNcf}.xml` por comprobante, orden por eNCF; cero → 404. Header `X-Ecf-Count` en la API.
- API: fechas `yyyy-MM-dd` estrictas; 403 sin API key; 400 eNCF o fechas inválidas o `to < from`.
- Panel: mismos permisos que la lista (no SA solo ve clientes propios vía `Client.UserClients`); `clientId` obligatorio en el rango.
- Librería: nunca lanza por red, timeout o respuesta del servidor (sí `ArgumentException` por eNCF vacío); `[JsonProperty]` explícito en cada propiedad que Newtonsoft lee; C# compatible con `net452`; entra en la 0.3.0 (no se distribuyó); comentarios sin acentos.
- Todo local, **sin push**. Antes de editar cada repo, `git branch --show-current`. Backend `Staging` y luego `feature/emit-idempotente`; panel `Staging`; librería `feature/emit-idempotente`.
- Rutas base: backend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, panel `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`, librería `C:\Projects\Zynstorm.DGII.ECF`.

## File Structure

Backend:
- Create `ZynstormECFPlatform.Services/Production/SignedXmlExport.cs`: tipos de resultado, `SignedXmlExportLogic` (pura), `ISignedXmlExportService` y `SignedXmlExportService`.
- Modify `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`: registro.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` y `EcfDocumentController.cs`.
- Test `ZynstormECFPlatform.Tests/Production/SignedXmlExportLogicTests.cs`.

Panel: modify `services/invoice.service.ts`, `app/facturas/page.tsx`.

Librería: modify `Client/EcfEndpoints.cs`, `IEcfClient.cs`, `EcfClient.cs`, `EcfReceivedPayloads.cs`, `EcfReceivedResults.cs`; rename `EcfReceivedStatus.cs` → `EcfQueryStatus.cs`; tests `ReceivedTests.cs` y nuevo `SignedXmlTests.cs`; docs y README.

---

## Parte A: Backend (rama `Staging`)

### Task 1: Lógica pura del export

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/SignedXmlExport.cs`
- Test: `ZynstormECFPlatform.Tests/Production/SignedXmlExportLogicTests.cs`

**Interfaces:**
- Produces: `SignedXmlCandidate(int EcfDocumentId, string ENcf, EcfLookupState State, DateTime RegisteredAt)`, `SignedXmlItem(string ENcf, string Xml)`, `SignedXmlFile(byte[] Content, string FileName, string ContentType, int Count)`, `SignedXmlExportKind { Ok, NotFound, TooMany }`, `SignedXmlExportResult(Kind, File, Total, Message)` con fábricas `Success(file)`, `Missing(message)`, `Excess(total, message)`, y `SignedXmlExportLogic.{MaxItems, TryParseDate, ToUtcRange, SelectBest, BuildFile, Single, Digits}`.

- [ ] **Step 1: Rama y línea base**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && git branch --show-current && git status --short | head -3
```
Expected: `Staging`, árbol limpio.

- [ ] **Step 2: Escribir las pruebas que fallan**

Create `ZynstormECFPlatform.Tests/Production/SignedXmlExportLogicTests.cs`:

```csharp
using System.IO.Compression;
using System.Text;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class SignedXmlExportLogicTests
{
    private static SignedXmlCandidate Candidate(int id, string encf, EcfLookupState state, int day = 1) =>
        new(id, encf, state, new DateTime(2026, 10, day, 12, 0, 0, DateTimeKind.Utc));

    [Theory]
    [InlineData("2026-10-14", true)]
    [InlineData("2026-1-4", false)]
    [InlineData("14/10/2026", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseDate_IsStrict(string? raw, bool expected)
    {
        Assert.Equal(expected, SignedXmlExportLogic.TryParseDate(raw, out _));
    }

    [Fact]
    public void ToUtcRange_UsesDominicanDays_InclusiveOfTheLastDay()
    {
        var (from, toExclusive) = SignedXmlExportLogic.ToUtcRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        // RD es UTC-4 todo el año: el día local empieza a las 04:00 UTC.
        Assert.Equal(new DateTime(2026, 10, 1, 4, 0, 0), from);
        Assert.Equal(new DateTime(2026, 10, 3, 4, 0, 0), toExclusive);
    }

    [Fact]
    public void SelectBest_KeepsOnlyUsableOnes_AndOnePerEncf()
    {
        var picked = SignedXmlExportLogic.SelectBest(
        [
            Candidate(1, "E310000000001", EcfLookupState.Rejected),
            Candidate(2, "E310000000001", EcfLookupState.Accepted, day: 2),
            Candidate(3, "E310000000002", EcfLookupState.Rejected),
            Candidate(4, "E310000000003", EcfLookupState.Pending),
            Candidate(5, "E310000000003", EcfLookupState.Accepted, day: 3),
            Candidate(6, "E310000000004", EcfLookupState.Error),
            Candidate(7, "E310000000005", EcfLookupState.NotSent)
        ]);

        Assert.Equal(["E310000000001", "E310000000003"], picked.Select(c => c.ENcf).ToArray());
        Assert.Equal([2, 5], picked.Select(c => c.EcfDocumentId).ToArray());
    }

    [Fact]
    public void SelectBest_OrdersByEncf()
    {
        var picked = SignedXmlExportLogic.SelectBest(
        [
            Candidate(1, "E310000000009", EcfLookupState.Accepted),
            Candidate(2, "E310000000003", EcfLookupState.Accepted)
        ]);

        Assert.Equal(["E310000000003", "E310000000009"], picked.Select(c => c.ENcf).ToArray());
    }

    [Fact]
    public void BuildFile_OneItem_IsThePlainXml_WithExactBytes()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ECF><eNCF>E310000000001</eNCF></ECF>";

        var file = SignedXmlExportLogic.BuildFile(
            [new SignedXmlItem("E310000000001", xml)], "132293894", new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        Assert.Equal("E310000000001.xml", file.FileName);
        Assert.Equal("application/xml", file.ContentType);
        Assert.Equal(1, file.Count);
        Assert.Equal(Encoding.UTF8.GetBytes(xml), file.Content);
        Assert.NotEqual(0xEF, file.Content[0]);
    }

    [Fact]
    public void BuildFile_SeveralItems_IsAZipWithOneEntryEach()
    {
        var items = new[]
        {
            new SignedXmlItem("E310000000001", "<ECF>uno ñ</ECF>"),
            new SignedXmlItem("E310000000002", "<ECF>dos</ECF>")
        };

        var file = SignedXmlExportLogic.BuildFile(items, "1-32-29389-4", new DateTime(2026, 10, 1), new DateTime(2026, 10, 14));

        Assert.Equal("XML_firmados_132293894_20261001_20261014.zip", file.FileName);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal(2, file.Count);

        using var archive = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);
        Assert.Equal(["E310000000001.xml", "E310000000002.xml"], archive.Entries.Select(e => e.FullName).ToArray());

        using var reader = new StreamReader(archive.GetEntry("E310000000001.xml")!.Open(), new UTF8Encoding(false));
        Assert.Equal("<ECF>uno ñ</ECF>", reader.ReadToEnd());
    }

    [Fact]
    public void Digits_KeepsOnlyDigits()
    {
        Assert.Equal("132293894", SignedXmlExportLogic.Digits("1-32-29389-4"));
        Assert.Equal(string.Empty, SignedXmlExportLogic.Digits(null));
    }
}
```

- [ ] **Step 3: Ejecutar para verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~SignedXmlExportLogicTests" 2>&1 | grep -E "error|Passed!|Failed!" | sort -u | head -5`
Expected: error de compilación, `SignedXmlExportLogic` no existe.

- [ ] **Step 4: Implementar los tipos y la lógica**

Create `ZynstormECFPlatform.Services/Production/SignedXmlExport.cs` con esta primera parte (el servicio se agrega en la Task 2, en el mismo archivo):

```csharp
using System.Globalization;
using System.IO.Compression;
using System.Text;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>Un documento emitido reducido a lo que hace falta para elegir cuál se entrega.</summary>
public sealed record SignedXmlCandidate(int EcfDocumentId, string ENcf, EcfLookupState State, DateTime RegisteredAt);

public sealed record SignedXmlItem(string ENcf, string Xml);

/// <summary>El archivo a entregar: un XML o un ZIP de XML.</summary>
public sealed record SignedXmlFile(byte[] Content, string FileName, string ContentType, int Count);

public enum SignedXmlExportKind
{
    Ok,
    NotFound,
    TooMany
}

public sealed record SignedXmlExportResult(SignedXmlExportKind Kind, SignedXmlFile? File, int Total, string Message)
{
    public static SignedXmlExportResult Success(SignedXmlFile file) =>
        new(SignedXmlExportKind.Ok, file, file.Count, string.Empty);

    public static SignedXmlExportResult Missing(string message) =>
        new(SignedXmlExportKind.NotFound, null, 0, message);

    public static SignedXmlExportResult Excess(int total, string message) =>
        new(SignedXmlExportKind.TooMany, null, total, message);
}

/// <summary>Reglas puras del export de XML firmados. Sin base de datos, para poder probarlas.</summary>
public static class SignedXmlExportLogic
{
    public const int MaxItems = 2000;

    // Sin BOM: el XML firmado se entrega tal cual, y un BOM no forma parte de lo firmado.
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static bool TryParseDate(string? raw, out DateTime date) =>
        DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// Días de RD (ambos inclusivos) a un intervalo UTC [desde, hasta). Es la misma conversión que
    /// usa la lista de facturas del panel, para que lo que se ve sea lo que se descarga.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) ToUtcRange(DateTime fromDate, DateTime toDate) =>
        (fromDate.Date.ConvertDominicanLocalToUtc(), toDate.Date.AddDays(1).ConvertDominicanLocalToUtc());

    /// <summary>Solo documentos válidos, uno por eNCF (el mejor), ordenados por eNCF.</summary>
    public static IReadOnlyList<SignedXmlCandidate> SelectBest(IEnumerable<SignedXmlCandidate> candidates) =>
        candidates
            .Where(candidate => EcfLookupLogic.IsUsable(candidate.State))
            .GroupBy(candidate => candidate.ENcf)
            .Select(group =>
            {
                var best = EcfLookupLogic.PickBest(
                    group.Select(candidate => new EcfLookupCandidate(candidate.EcfDocumentId, candidate.State, candidate.RegisteredAt)))!;

                return group.First(candidate => candidate.EcfDocumentId == best.EcfDocumentId);
            })
            .OrderBy(candidate => candidate.ENcf, StringComparer.Ordinal)
            .ToList();

    public static SignedXmlFile Single(SignedXmlItem item) =>
        new(Utf8NoBom.GetBytes(item.Xml), $"{item.ENcf}.xml", "application/xml", 1);

    /// <summary>Un solo comprobante es el XML; más de uno, un ZIP con una entrada por eNCF.</summary>
    public static SignedXmlFile BuildFile(IReadOnlyList<SignedXmlItem> items, string issuerRnc, DateTime fromDate, DateTime toDate)
    {
        if (items.Count == 1)
        {
            return Single(items[0]);
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var item in items)
            {
                var entry = archive.CreateEntry($"{item.ENcf}.xml", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                var bytes = Utf8NoBom.GetBytes(item.Xml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        var name = $"XML_firmados_{Digits(issuerRnc)}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.zip";
        return new SignedXmlFile(stream.ToArray(), name, "application/zip", items.Count);
    }

    public static string Digits(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : new string(value.Where(char.IsAsciiDigit).ToArray());
}
```

- [ ] **Step 5: Ejecutar las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~SignedXmlExportLogicTests" 2>&1 | grep -E " error |Passed!|Failed!|\[FAIL\]|Expected|Actual" | sort -u | head -10`
Expected: todas pasan. Si `ToUtcRange` falla por la zona horaria del equipo, el cálculo debe seguir siendo el de `ConvertDominicanLocalToUtc` (no tocar su lógica): ajustar solo el assert si el entorno devolviera otro desfase, y decirlo en el reporte.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Production/SignedXmlExport.cs ZynstormECFPlatform.Tests/Production/SignedXmlExportLogicTests.cs Docs
git commit -m "feat(xml-firmado): lógica del export de XML firmados (selección, rango y ZIP)"
```
(con las líneas de atribución del sistema; `Docs` incluye el spec y este plan.)

---

### Task 2: Servicio de export

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/SignedXmlExport.cs` (agregar al final)
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `IEcfDocumentService.Table`, `IEcfTransmissionService.Table`, `IEcfXmlDocumentService.Table`, `IClientService.Table`, `EcfLookupLogic.{ForClient, ResolveState}`.
- Produces: `ISignedXmlExportService.GetByNcfAsync(int clientId, string normalizedEncf, CancellationToken)`, `.GetForDocumentAsync(int ecfDocumentId, CancellationToken)`, `.GetRangeAsync(int clientId, DateTime fromDate, DateTime toDate, CancellationToken)`, todos `Task<SignedXmlExportResult>`.

- [ ] **Step 1: Confirmar los servicios de datos**

Run: `grep -n "Table" ZynstormECFPlatform.Abstractions/Data/IRepository.cs; grep -n "interface IClientService\|interface IEcfXmlDocumentService\|interface IEcfTransmissionService\|interface IEcfDocumentService" -r ZynstormECFPlatform.Abstractions | head`
Expected: `IQueryable<TModel> Table` y las cuatro interfaces en `ZynstormECFPlatform.Abstractions.DataServices`.

- [ ] **Step 2: Agregar el servicio**

Al principio de `SignedXmlExport.cs` agregar los usings que faltan (`using Microsoft.EntityFrameworkCore;` y `using ZynstormECFPlatform.Abstractions.DataServices;`) y, al final del archivo:

```csharp
public interface ISignedXmlExportService
{
    /// <summary>XML firmado del mejor documento válido del cliente con ese eNCF (ya normalizado).</summary>
    Task<SignedXmlExportResult> GetByNcfAsync(int clientId, string normalizedEncf, CancellationToken cancellationToken = default);

    /// <summary>XML firmado de ese documento exacto, en cualquier estado.</summary>
    Task<SignedXmlExportResult> GetForDocumentAsync(int ecfDocumentId, CancellationToken cancellationToken = default);

    /// <summary>XML firmados de los documentos válidos del cliente registrados en el rango de días de RD.</summary>
    Task<SignedXmlExportResult> GetRangeAsync(int clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

public class SignedXmlExportService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients) : ISignedXmlExportService
{
    /// <summary>
    /// EcfStatusId que la plataforma comparte entre Rechazado y Aceptado condicional: para
    /// distinguirlos hay que leer la última transmisión (ver EcfLookupLogic.ResolveState).
    /// </summary>
    private const int RejectedOrConditionalStatusId = 11;

    private sealed record Row(int Id, string ENcf, int EcfStatusId, DateTime RegisteredAt);

    public async Task<SignedXmlExportResult> GetByNcfAsync(
        int clientId,
        string normalizedEncf,
        CancellationToken cancellationToken = default)
    {
        var rows = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, normalizedEncf))
            .Select(document => new Row(document.EcfDocumentId, document.Ncf, document.EcfStatusId, document.RegisteredAt))
            .ToListAsync(cancellationToken);

        var selected = SignedXmlExportLogic.SelectBest(await ToCandidatesAsync(rows, cancellationToken));
        var items = await LoadItemsAsync(selected, cancellationToken);

        return items.Count == 0
            ? SignedXmlExportResult.Missing("No hay un comprobante emitido y válido con ese eNCF que tenga XML firmado.")
            : SignedXmlExportResult.Success(SignedXmlExportLogic.Single(items[0]));
    }

    public async Task<SignedXmlExportResult> GetForDocumentAsync(
        int ecfDocumentId,
        CancellationToken cancellationToken = default)
    {
        var encf = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == ecfDocumentId)
            .Select(document => document.Ncf)
            .FirstOrDefaultAsync(cancellationToken);

        var xml = encf is null
            ? null
            : (await LoadSignedXmlAsync([ecfDocumentId], cancellationToken)).GetValueOrDefault(ecfDocumentId);

        return encf is null || xml is null
            ? SignedXmlExportResult.Missing("El comprobante no tiene XML firmado.")
            : SignedXmlExportResult.Success(SignedXmlExportLogic.Single(new SignedXmlItem(encf, xml)));
    }

    public async Task<SignedXmlExportResult> GetRangeAsync(
        int clientId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtcExclusive) = SignedXmlExportLogic.ToUtcRange(fromDate, toDate);

        var rows = await documents.Table
            .AsNoTracking()
            .Where(document => document.ClientId == clientId
                               && !document.IsDeleted
                               && document.RegisteredAt >= fromUtc
                               && document.RegisteredAt < toUtcExclusive)
            .Select(document => new Row(document.EcfDocumentId, document.Ncf, document.EcfStatusId, document.RegisteredAt))
            .ToListAsync(cancellationToken);

        var selected = SignedXmlExportLogic.SelectBest(await ToCandidatesAsync(rows, cancellationToken));

        if (selected.Count > SignedXmlExportLogic.MaxItems)
        {
            return SignedXmlExportResult.Excess(
                selected.Count,
                $"El rango tiene {selected.Count} comprobantes y el máximo por descarga es {SignedXmlExportLogic.MaxItems}. Reduzca el rango de fechas.");
        }

        var items = await LoadItemsAsync(selected, cancellationToken);

        if (items.Count == 0)
        {
            return SignedXmlExportResult.Missing("No hay comprobantes emitidos y válidos con XML firmado en ese rango de fechas.");
        }

        var rnc = await clients.Table
            .AsNoTracking()
            .Where(client => client.ClientId == clientId)
            .Select(client => client.Rnc)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return SignedXmlExportResult.Success(SignedXmlExportLogic.BuildFile(items, rnc, fromDate, toDate));
    }

    /// <summary>Estado de cada documento; la última transmisión solo se lee donde el estado la necesita.</summary>
    private async Task<List<SignedXmlCandidate>> ToCandidatesAsync(List<Row> rows, CancellationToken cancellationToken)
    {
        var ambiguousIds = rows
            .Where(row => row.EcfStatusId == RejectedOrConditionalStatusId)
            .Select(row => row.Id)
            .ToList();

        var payloads = new Dictionary<int, string?>();

        if (ambiguousIds.Count > 0)
        {
            var sent = await transmissions.Table
                .AsNoTracking()
                .Where(transmission => ambiguousIds.Contains(transmission.EcfDocumentId))
                .Select(transmission => new { transmission.EcfDocumentId, transmission.ResponsePayload, transmission.SentAtUtc })
                .ToListAsync(cancellationToken);

            payloads = sent
                .GroupBy(transmission => transmission.EcfDocumentId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(transmission => transmission.SentAtUtc).First().ResponsePayload);
        }

        return rows
            .Select(row => new SignedXmlCandidate(
                row.Id,
                row.ENcf,
                EcfLookupLogic.ResolveState(row.EcfStatusId, payloads.GetValueOrDefault(row.Id)),
                row.RegisteredAt))
            .ToList();
    }

    /// <summary>XML firmado de cada documento elegido; los que no lo tienen se omiten.</summary>
    private async Task<List<SignedXmlItem>> LoadItemsAsync(
        IReadOnlyList<SignedXmlCandidate> selected,
        CancellationToken cancellationToken)
    {
        if (selected.Count == 0)
        {
            return [];
        }

        var signed = await LoadSignedXmlAsync(selected.Select(candidate => candidate.EcfDocumentId).ToList(), cancellationToken);

        return selected
            .Where(candidate => signed.ContainsKey(candidate.EcfDocumentId))
            .Select(candidate => new SignedXmlItem(candidate.ENcf, signed[candidate.EcfDocumentId]))
            .ToList();
    }

    /// <summary>El XmlSigned con texto de mayor id, por documento.</summary>
    private async Task<Dictionary<int, string>> LoadSignedXmlAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await xmlDocuments.Table
            .AsNoTracking()
            .Where(xml => documentIds.Contains(xml.EcfDocumentId) && xml.XmlSigned != null && xml.XmlSigned != "")
            .Select(xml => new { xml.EcfDocumentId, xml.EcfXmlDocumentId, xml.XmlSigned })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.EcfDocumentId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(row => row.EcfXmlDocumentId).First().XmlSigned);
    }
}
```

- [ ] **Step 3: Registrar el servicio**

En `ServiceCollectionExtensions.cs`, debajo de `services.AddTransient<Production.IReceivedEcfService, Production.ReceivedEcfService>();` agregar:

```csharp
        services.AddTransient<Production.ISignedXmlExportService, Production.SignedXmlExportService>();
```

- [ ] **Step 4: Compilar y ejecutar toda la suite**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded|Error\(s\)" | sort -u; dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!" | sort -u`
Expected: `Build succeeded` y todo en verde. Si `GetValueOrDefault` sobre `Dictionary` no resuelve, agregar `using System.Collections.Generic;` o usar `TryGetValue`.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services
git commit -m "feat(xml-firmado): servicio que entrega el XML firmado por eNCF, documento o rango"
```

---

### Task 3: Endpoints (API key y panel)

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfDocumentController.cs`

**Interfaces:**
- Consumes: `ISignedXmlExportService` (Task 2), `SignedXmlExportLogic.TryParseDate`, `EcfLookupLogic.NormalizeNcf`, `IClientService.Table`.
- Produces (HTTP): los cuatro endpoints de las Global Constraints.

- [ ] **Step 1: `EcfController`**

1. Usings: agregar `using System.Globalization;` junto a `using System.Text;`.
2. Constructor: agregar `ISignedXmlExportService signedXmlExportService,` después de `IReceivedEcfService receivedEcfService,` y el campo `private readonly ISignedXmlExportService _signedXmlExport = signedXmlExportService;` después de `_receivedEcfService`.
3. Antes de la acción `GetSample` (`/// Endpoint útil para ver un ejemplo vacío`), agregar:

```csharp
        /// <summary>
        /// Descarga el XML firmado de un comprobante emitido por el cliente de la API key,
        /// identificado por su eNCF. Si el eNCF se reintentó, entrega el documento válido.
        /// </summary>
        [HttpGet("signed-xml/{eNcf}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/xml")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSignedXml(string eNcf, CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return ReceivedRequiresApiKey();
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

            return SignedXmlResponse(await _signedXmlExport.GetByNcfAsync(clientId, normalized, cancellationToken));
        }

        /// <summary>
        /// Descarga los XML firmados de los comprobantes emitidos en un rango de fechas (días de RD,
        /// ambos inclusivos). Un comprobante llega como XML; varios, como un ZIP con un XML por eNCF.
        /// Máximo 2000 comprobantes por descarga.
        /// </summary>
        [HttpGet("signed-xml")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSignedXmlRange(
            [FromQuery] string? from,
            [FromQuery] string? to,
            CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return ReceivedRequiresApiKey();
            }

            if (!SignedXmlExportLogic.TryParseDate(from, out var fromDate) || !SignedXmlExportLogic.TryParseDate(to, out var toDate))
            {
                return BadRequest(new
                {
                    found = false,
                    message = "Los parámetros 'from' y 'to' son obligatorios y deben tener el formato yyyy-MM-dd."
                });
            }

            if (toDate < fromDate)
            {
                return BadRequest(new { found = false, message = "'to' no puede ser anterior a 'from'." });
            }

            return SignedXmlResponse(await _signedXmlExport.GetRangeAsync(clientId, fromDate, toDate, cancellationToken));
        }

        private IActionResult SignedXmlResponse(SignedXmlExportResult result)
        {
            switch (result.Kind)
            {
                case SignedXmlExportKind.Ok:
                    Response.Headers["X-Ecf-Count"] = result.File!.Count.ToString(CultureInfo.InvariantCulture);
                    return File(result.File.Content, result.File.ContentType, result.File.FileName);

                case SignedXmlExportKind.TooMany:
                    return BadRequest(new { found = false, tooMany = true, total = result.Total, message = result.Message });

                default:
                    return NotFound(new { found = false, message = result.Message });
            }
        }

```

- [ ] **Step 2: `EcfDocumentController`**

1. Usings: agregar `using System.Globalization;` y `using ZynstormECFPlatform.Services.Production;`.
2. Constructor y campos:

```csharp
    public class EcfDocumentController(
        IEcfDocumentService ecfDocumentService,
        ISignedXmlExportService signedXmlExportService,
        IClientService clientService,
        ILogger<EcfDocumentController> logger) : ControllerBase
    {
        private readonly IEcfDocumentService _ecfDocumentService = ecfDocumentService;
        private readonly ISignedXmlExportService _signedXmlExport = signedXmlExportService;
        private readonly IClientService _clientService = clientService;
        private readonly ILogger<EcfDocumentController> _logger = logger;
```
(`IClientService` está en `ZynstormECFPlatform.Abstractions.DataServices`, ya importado.)

3. Antes de `[HttpGet("stats")]` agregar:

```csharp
        /// <summary>
        /// Descarga el XML firmado de un comprobante (cualquier estado). 404 si no existe, no es del
        /// usuario o no tiene XML firmado.
        /// </summary>
        [HttpGet("guid/{guid}/signed-xml")]
        public async Task<IActionResult> DownloadSignedXml(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _ecfDocumentService.Table
                    .AsNoTracking()
                    .Where(e => e.GuidId == guid && !e.IsDeleted);

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(e => e.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                var documentId = await query
                    .Select(e => (int?)e.EcfDocumentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (documentId is null)
                {
                    return NotFound(new { message = "No se encontró la factura o no tiene permisos para verla." });
                }

                return SignedXmlResult(await _signedXmlExport.GetForDocumentAsync(documentId.Value, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando el XML firmado de {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al descargar el XML firmado." });
            }
        }

        /// <summary>
        /// Descarga los XML firmados del cliente en un rango de fechas (días de RD, inclusivos):
        /// un XML si hay uno, un ZIP si hay varios. El cliente es obligatorio.
        /// </summary>
        [HttpGet("signed-xml")]
        public async Task<IActionResult> DownloadSignedXmlRange(
            [FromQuery] int? clientId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            if (clientId is null or <= 0 || fromDate is null || toDate is null)
            {
                return BadRequest(new { message = "Indique el cliente y el rango de fechas (fromDate y toDate)." });
            }

            if (toDate.Value.Date < fromDate.Value.Date)
            {
                return BadRequest(new { message = "La fecha final no puede ser anterior a la inicial." });
            }

            try
            {
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    var owns = await _clientService.Table
                        .AsNoTracking()
                        .AnyAsync(c => c.ClientId == clientId.Value && c.UserClients.Any(uc => uc.UserId == userId), cancellationToken);

                    if (!owns)
                    {
                        return NotFound(new { message = "No se encontró el cliente o no tiene permisos para verlo." });
                    }
                }

                return SignedXmlResult(await _signedXmlExport.GetRangeAsync(clientId.Value, fromDate.Value, toDate.Value, cancellationToken));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error descargando los XML firmados del cliente {ClientId}", clientId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al descargar los XML firmados." });
            }
        }

        private IActionResult SignedXmlResult(SignedXmlExportResult result)
        {
            switch (result.Kind)
            {
                case SignedXmlExportKind.Ok:
                    Response.Headers["X-Ecf-Count"] = result.File!.Count.ToString(CultureInfo.InvariantCulture);
                    return File(result.File.Content, result.File.ContentType, result.File.FileName);

                case SignedXmlExportKind.TooMany:
                    return BadRequest(new { tooMany = true, total = result.Total, message = result.Message });

                default:
                    return NotFound(new { message = result.Message });
            }
        }

```

- [ ] **Step 3: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded|Error\(s\)" | sort -u; dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!" | sort -u`
Expected: `Build succeeded`, suite en verde. Si `Client.UserClients` no es accesible desde `IClientService.Table` o `c.UserClients` no existe, mirar cómo la usa `e.Client.UserClients` en `ApplyCommonFilters` y ajustar la navegación sin cambiar la regla de permisos.

- [ ] **Step 4: Commit**

```bash
git add ZynstormECFPlatform.Web.Api
git commit -m "feat(xml-firmado): endpoints del XML firmado por eNCF, por documento y por rango"
```

---

### Task 4: Replicar a `feature/emit-idempotente`

- [ ] **Step 1: Hashes y cherry-pick**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && git log --oneline -3 | cat
git checkout feature/emit-idempotente && git cherry-pick H1 H2 H3
```
(`H1..H3` = los tres commits de las Tasks 1 a 3, del más antiguo al más nuevo.) Resolver cualquier conflicto conservando ambos lados (los registros de servicios y los constructores).

- [ ] **Step 2: Compilar y probar en esa rama**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u; dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!" | sort -u; git checkout Staging; git branch --show-current`
Expected: verde y vuelta a `Staging`.

---

## Parte B: Panel (rama `Staging`)

### Task 5: XML firmado y vista previa del PDF

**Files:**
- Modify: `services/invoice.service.ts`
- Modify: `app/facturas/page.tsx`

**Interfaces:**
- Consumes: `GET EcfDocument/guid/{guid}/signed-xml`, `GET EcfDocument/signed-xml?clientId&fromDate&toDate` (Task 3), `downloadReceivedFilePdf` (existente en `fe.service.ts`).
- Produces: `downloadSignedXml(guid, ncf)` y `downloadSignedXmlRange({clientId, fromDate, toDate})`, ambos `Promise<{ blob: Blob; fileName: string }>`.

- [ ] **Step 1: Rama y constantes**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd && git branch --show-current && git status --short | head -3; grep -n "INVOICE_URL" services/invoice.service.ts | head -3
```
Expected: `Staging`, árbol limpio y la definición de `INVOICE_URL`.

- [ ] **Step 2: Funciones de servicio**

Al final de `services/invoice.service.ts` agregar:

```ts
export interface DownloadedFile {
  blob: Blob
  fileName: string
}

/** Descarga un archivo del API con la sesión actual y lee su nombre de Content-Disposition. */
const downloadFile = async (url: string, fallbackName: string): Promise<DownloadedFile> => {
  const response = await fetch(url, { credentials: "include" })

  if (!response.ok) {
    let detail = response.statusText
    try {
      const body = await response.json()
      if (body?.message) detail = body.message
    } catch {
      // El cuerpo no era JSON: se queda con el statusText.
    }
    throw new Error(detail)
  }

  const blob = await response.blob()

  let fileName = fallbackName
  const contentDisposition = response.headers.get("content-disposition")
  if (contentDisposition) {
    const match = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;]+)/i)
    if (match && match[1]) {
      fileName = decodeURIComponent(match[1].replace(/"/g, ""))
    }
  }

  return { blob, fileName }
}

/** XML firmado de un comprobante (cualquier estado). */
export const downloadSignedXml = async (guid: string, ncf: string): Promise<DownloadedFile> => {
  return await downloadFile(`${INVOICE_URL}/guid/${guid}/signed-xml`, `${ncf}.xml`)
}

/** XML firmados de un cliente en un rango de fechas: un XML si hay uno, un ZIP si hay varios. */
export const downloadSignedXmlRange = async (filters: {
  clientId: string
  fromDate: string
  toDate: string
}): Promise<DownloadedFile> => {
  const params = new URLSearchParams({
    clientId: filters.clientId,
    fromDate: filters.fromDate,
    toDate: filters.toDate,
  })
  return await downloadFile(`${INVOICE_URL}/signed-xml?${params.toString()}`, "XML_firmados.zip")
}
```

- [ ] **Step 3: Imports, estado y helper en la página**

En `app/facturas/page.tsx`:

1. Import de servicios (línea ~68) → `import { getInvoices, getInvoiceByGuid, getInvoiceStats, requeueInvoice, downloadSignedXml, downloadSignedXmlRange, Invoice, InvoiceStats } from "@/services/invoice.service"`.
2. En el import de `lucide-react`, después de `  FileCode2,` agregar `  FileArchive,` y `  FileSearch,`.
3. Junto a `const [downloadingPdfId, setDownloadingPdfId] = ...` agregar:

```tsx
  const [downloadingRange, setDownloadingRange] = useState(false)
  const [loadingPreviewId, setLoadingPreviewId] = useState<number | null>(null)
  const [pdfPreview, setPdfPreview] = useState<{ blob: Blob; url: string; fileName: string; eNcf: string } | null>(null)
```

4. Reemplazar la función `handleDownloadXml` completa (desde `const handleDownloadXml = async (invoice: Invoice | null) => {` hasta su `}` de cierre, justo antes de `const getPageNumbers`) por:

```tsx
  const saveFile = (blob: Blob, fileName: string) => {
    const url = URL.createObjectURL(blob)
    const link = document.createElement("a")
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
  }

  const handleDownloadXml = async (invoice: Invoice | null) => {
    if (!invoice) return
    setDownloadingRowId(invoice.id)
    try {
      const { blob, fileName } = await downloadSignedXml(invoice.id, invoice.ncf)
      saveFile(blob, fileName)
    } catch (err) {
      console.error("Error downloading signed XML:", err)
      alert(err instanceof Error && err.message ? `Error al descargar el XML firmado: ${err.message}` : "Error al descargar el XML firmado.")
    } finally {
      setDownloadingRowId(null)
    }
  }

  const handleDownloadSignedXmlRange = async () => {
    if (!invoiceClientId) return
    setDownloadingRange(true)
    try {
      const { blob, fileName } = await downloadSignedXmlRange({ clientId: invoiceClientId, fromDate, toDate })
      saveFile(blob, fileName)
    } catch (err) {
      console.error("Error downloading signed XML range:", err)
      alert(err instanceof Error && err.message ? err.message : "Error al descargar los XML firmados.")
    } finally {
      setDownloadingRange(false)
    }
  }

  const closePdfPreview = () => {
    if (pdfPreview) URL.revokeObjectURL(pdfPreview.url)
    setPdfPreview(null)
  }

  const handlePreviewPdfB2B = async (file: ReceivedB2BFile) => {
    if (!selectedClientId) return
    setLoadingPreviewId(file.receivedB2BMessageId)
    try {
      const { blob, fileName } = await downloadReceivedFilePdf(Number(selectedClientId), file.receivedB2BMessageId)
      if (pdfPreview) URL.revokeObjectURL(pdfPreview.url)
      setPdfPreview({ blob, url: URL.createObjectURL(blob), fileName, eNcf: file.eNcf })
    } catch (err) {
      console.error("Error previewing PDF B2B:", err)
      alert(err instanceof Error && err.message ? `Error al generar la vista previa: ${err.message}` : "Error al generar la vista previa.")
    } finally {
      setLoadingPreviewId(null)
    }
  }
```

- [ ] **Step 4: Botón «XML firmados» junto a las fechas**

Después del botón `Hoy` (`<Button type="button" variant="outline" size="sm" className="h-9" onClick={setToday}> Hoy </Button>`) agregar:

```tsx
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="h-9"
                  onClick={handleDownloadSignedXmlRange}
                  disabled={!invoiceClientId || downloadingRange}
                  title={
                    invoiceClientId
                      ? "Descargar los XML firmados del rango (un ZIP si son varios)"
                      : "Seleccione un cliente para descargar sus XML firmados"
                  }
                >
                  {downloadingRange ? (
                    <RefreshCw className="h-4 w-4 animate-spin" />
                  ) : (
                    <FileArchive className="h-4 w-4" />
                  )}
                  <span className="ml-1.5 hidden xl:inline">XML firmados</span>
                </Button>
```

- [ ] **Step 5: Botón de vista previa y diálogo en Recibidas**

1. Antes de la línea `{file.messageType === "Ecf" && (` del botón «Descargar PDF» (la única de la fila), insertar:

```tsx
                                {file.messageType === "Ecf" && (
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-7 w-7 p-0"
                                    onClick={() => handlePreviewPdfB2B(file)}
                                    disabled={loadingPreviewId !== null}
                                    title="Vista previa del PDF"
                                  >
                                    {loadingPreviewId === file.receivedB2BMessageId ? (
                                      <RefreshCw className="h-4 w-4 animate-spin" />
                                    ) : (
                                      <FileSearch className="h-4 w-4" />
                                    )}
                                  </Button>
                                )}
```

2. Antes del comentario `{/* XML B2B View Dialog */}` agregar:

```tsx
            {/* Vista previa del PDF de un e-CF recibido */}
            <Dialog open={pdfPreview !== null} onOpenChange={(open) => { if (!open) closePdfPreview() }}>
              <DialogContent className="sm:max-w-4xl h-[85vh] flex flex-col gap-3">
                <DialogHeader>
                  <DialogTitle className="flex items-center gap-2">
                    <FileText className="h-5 w-5" />
                    {pdfPreview?.eNcf || pdfPreview?.fileName}
                  </DialogTitle>
                </DialogHeader>
                {pdfPreview && (
                  <iframe
                    src={pdfPreview.url}
                    title={`Vista previa ${pdfPreview.fileName}`}
                    className="min-h-0 w-full flex-1 rounded-md border border-border bg-white"
                  />
                )}
                <div className="flex justify-end gap-2">
                  <Button variant="outline" size="sm" onClick={closePdfPreview}>
                    Cerrar
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => pdfPreview && saveFile(pdfPreview.blob, pdfPreview.fileName)}
                  >
                    <Download className="mr-1.5 h-4 w-4" />
                    Descargar PDF
                  </Button>
                </div>
              </DialogContent>
            </Dialog>

```

- [ ] **Step 6: Tipos y lint**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
npx tsc --noEmit 2>&1 | grep "error TS"
npx eslint services/invoice.service.ts app/facturas/page.tsx 2>&1 | tail -6
git checkout tsconfig.tsbuildinfo
```
Expected: solo el error previo de `app/configuraciones/cuenta/page.tsx`; eslint sin errores (los avisos previos de `useEffect` no son de este cambio).

- [ ] **Step 7: Revisar que el iframe pueda mostrar un blob**

Run: `grep -n "Content-Security-Policy\|frame-src\|X-Frame-Options" next.config.* middleware.* 2>/dev/null | head`
Expected: sin salida. Si hay una CSP, permitir `frame-src blob:` y `object-src blob:` y decirlo en el reporte.

- [ ] **Step 8: Commit**

```bash
git add services/invoice.service.ts app/facturas/page.tsx
git commit -m "feat(facturas): XML firmados por comprobante y rango, y vista previa del PDF de recibidos"
```

---

## Parte C: Librería (`C:\Projects\Zynstorm.DGII.ECF`, rama `feature/emit-idempotente`)

### Task 6: Renombrar los tipos de archivo y agregar `GetSignedXml*`

**Files:**
- Modify: `src/Zynstorm.DGII.ECF/Client/EcfEndpoints.cs`, `IEcfClient.cs`, `EcfClient.cs`, `EcfReceivedPayloads.cs`, `EcfReceivedResults.cs`
- Rename: `Client/EcfReceivedStatus.cs` → `Client/EcfQueryStatus.cs`
- Modify (renombre mecánico): `tests/Zynstorm.DGII.ECF.Tests/ReceivedTests.cs`
- Test: `tests/Zynstorm.DGII.ECF.Tests/SignedXmlTests.cs`

**Interfaces:**
- Produces: `EcfQueryStatus` (los valores de `EcfReceivedStatus` más `TooManyResults = 7`), `EcfFile` (lo de `EcfReceivedFile` más `int Count` y `bool IsZip`), `IEcfClient/EcfClient.GetSignedXmlAsync(string eNcf, CancellationToken)`, `GetSignedXml(string eNcf)`, `GetSignedXmlRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken)`, `GetSignedXmlRange(DateTime fromDate, DateTime toDate)`.

- [ ] **Step 1: Rama y línea base**

Run:
```bash
cd /c/Projects/Zynstorm.DGII.ECF && git branch --show-current && git status --short | head -3 && dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | grep -E " error |Passed!|Failed!"
```
Expected: `feature/emit-idempotente`, limpio y 380 pruebas en verde.

- [ ] **Step 2: Renombrar con `git mv` y sed**

Run:
```bash
git mv src/Zynstorm.DGII.ECF/Client/EcfReceivedStatus.cs src/Zynstorm.DGII.ECF/Client/EcfQueryStatus.cs
grep -rl "EcfReceivedStatus\|EcfReceivedFile" src tests docs README.md --include=*.cs --include=*.md | xargs sed -i 's/EcfReceivedStatus/EcfQueryStatus/g; s/EcfReceivedFile/EcfFile/g'
grep -rn "EcfReceivedStatus\|EcfReceivedFile" src tests docs README.md | grep -v "docs/html" | head
dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | grep -E " error |Passed!|Failed!"
```
Expected: sin coincidencias restantes y 380 pruebas en verde (el renombre es mecánico). Los `docs/html` se regeneran en la Task 7.

- [ ] **Step 3: Escribir las pruebas que fallan**

Create `tests/Zynstorm.DGII.ECF.Tests/SignedXmlTests.cs`:

```csharp
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Zynstorm.DGII.ECF.Client;
using Zynstorm.DGII.ECF.Tests.Fakes;

namespace Zynstorm.DGII.ECF.Tests;

/// <summary>
/// XML firmado de los comprobantes emitidos: por eNCF, y por rango de fechas (un XML o un ZIP).
/// </summary>
public class SignedXmlTests
{
    private const string Staging = "https://ecfstaging.zynstorm.com/api/v1";

    private static EcfClient Client(ScriptedHandler handler) =>
        new(new EcfOptions(Staging, "key-de-prueba"), handler, ownsHttpClient: false);

    private static readonly byte[] Xml = Encoding.UTF8.GetBytes("<ECF><eNCF>E310000000001</eNCF></ECF>");
    private static readonly byte[] Zip = { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };

    [Fact]
    public async Task Por_ENcf_Va_A_La_Ruta_Signed_Xml_Y_Devuelve_El_Xml()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Xml, "application/xml", "E310000000001.xml");

        var file = await Client(handler).GetSignedXmlAsync("e310000000001 ");

        Assert.Equal(EcfQueryStatus.Ok, file.Status);
        Assert.Equal(Xml, file.Content);
        Assert.Equal("E310000000001.xml", file.FileName);
        Assert.False(file.IsZip);
        Assert.Equal(1, file.Count);
        Assert.Equal(Staging + "/Ecf/signed-xml/e310000000001", handler.Urls.Single());
    }

    [Fact]
    public async Task El_Rango_Va_Con_Las_Fechas_Como_Dia()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Xml, "application/xml");

        await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 10, 1, 18, 30, 0), new DateTime(2026, 10, 14, 3, 0, 0));

        Assert.Equal(Staging + "/Ecf/signed-xml?from=2026-10-01&to=2026-10-14", handler.Urls.Single());
    }

    [Fact]
    public async Task Varios_Comprobantes_Llegan_Como_Zip_Con_Su_Cantidad()
    {
        var handler = new ScriptedHandler().RespondsBytes(
            HttpStatusCode.OK, Zip, "application/zip", "XML_firmados_132293894_20261001_20261014.zip", count: 7);

        var file = await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 14));

        Assert.Equal(EcfQueryStatus.Ok, file.Status);
        Assert.True(file.IsZip);
        Assert.Equal(7, file.Count);
        Assert.Equal("XML_firmados_132293894_20261001_20261014.zip", file.FileName);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal(Zip, file.Content);
    }

    [Fact]
    public async Task Un_Solo_Comprobante_En_El_Rango_Es_Xml_Y_Cuenta_Uno()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Xml, "application/xml", "E310000000001.xml", count: 1);

        var file = await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 10, 14), new DateTime(2026, 10, 14));

        Assert.False(file.IsZip);
        Assert.Equal(1, file.Count);
    }

    [Fact]
    public async Task Un_404_Es_NotFound_Con_El_Mensaje_De_La_Plataforma()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.NotFound,
            "{\"found\":false,\"message\":\"No hay comprobantes emitidos en ese rango.\"}");

        var file = await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        Assert.Equal(EcfQueryStatus.NotFound, file.Status);
        Assert.Null(file.Content);
        Assert.Contains("No hay comprobantes", file.Message);
    }

    [Fact]
    public async Task Un_400_Con_TooMany_Es_TooManyResults()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.BadRequest,
            "{\"found\":false,\"tooMany\":true,\"total\":2500,\"message\":\"El rango tiene 2500 comprobantes. Reduzca el rango.\"}");

        var file = await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 1, 1), new DateTime(2026, 10, 14));

        Assert.Equal(EcfQueryStatus.TooManyResults, file.Status);
        Assert.Contains("Reduzca el rango", file.Message);
    }

    [Fact]
    public async Task Un_400_Normal_Sigue_Siendo_InvalidRequest()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.BadRequest, "{\"found\":false,\"message\":\"eNCF invalido.\"}");

        var file = await Client(handler).GetSignedXmlAsync("E31");

        Assert.Equal(EcfQueryStatus.InvalidRequest, file.Status);
    }

    [Fact]
    public async Task Una_Caida_De_Red_Es_TransportError_Sin_Excepcion()
    {
        var handler = new ScriptedHandler().Throws(new HttpRequestException("sin red"));

        var file = await Client(handler).GetSignedXmlRangeAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        Assert.Equal(EcfQueryStatus.TransportError, file.Status);
        Assert.Null(file.Content);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Un_ENcf_Vacio_Lanza_ArgumentException(string eNcf)
    {
        var handler = new ScriptedHandler();

        await Assert.ThrowsAsync<ArgumentException>(() => Client(handler).GetSignedXmlAsync(eNcf));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public void Las_Versiones_Sincronas_Devuelven_Lo_Mismo()
    {
        var handler = new ScriptedHandler()
            .RespondsBytes(HttpStatusCode.OK, Xml, "application/xml", "E310000000001.xml")
            .RespondsBytes(HttpStatusCode.OK, Xml, "application/xml", "E310000000001.xml");

        var client = Client(handler);

        Assert.Equal(EcfQueryStatus.Ok, client.GetSignedXml("E310000000001").Status);
        Assert.Equal(EcfQueryStatus.Ok, client.GetSignedXmlRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2)).Status);
    }
}
```

Y en `Fakes/ScriptedHandler.cs`, cambiar `RespondsBytes` para aceptar la cabecera de cantidad (leer el archivo antes de editarlo):

```csharp
    /// <summary>Respuesta binaria (un PDF, por ejemplo) con su tipo y, si se indica, su nombre de archivo y su X-Ecf-Count.</summary>
    internal ScriptedHandler RespondsBytes(HttpStatusCode statusCode, byte[] body, string contentType, string fileName = null, int? count = null)
    {
        _script.Enqueue(() =>
        {
            var response = new HttpResponseMessage(statusCode) { Content = new ByteArrayContent(body) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            if (fileName != null)
                response.Content.Headers.ContentDisposition =
                    new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = fileName };

            if (count.HasValue)
                response.Headers.Add("X-Ecf-Count", count.Value.ToString());

            return response;
        });
        return this;
    }
```

- [ ] **Step 4: Ejecutar para verificar que falla**

Run: `dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | grep -E "error" | sort -u | head -5`
Expected: errores de compilación (`GetSignedXmlAsync`, `IsZip`, `Count`, `TooManyResults` no existen).

- [ ] **Step 5: Estado y resultado**

En `Client/EcfQueryStatus.cs`, cambiar el comentario del enum a `/// <summary>Resultado de consultar o descargar comprobantes: recibidos, o el XML firmado de los emitidos.</summary>` y agregar al final del enum (después de `ContractError = 6`):

```csharp
        ,

        /// <summary>
        /// El rango pedido trae mas comprobantes de los que se pueden descargar juntos (2000).
        /// Pida un rango mas corto.
        /// </summary>
        TooManyResults = 7
```
(es decir, `ContractError = 6,` seguido del miembro nuevo con su comentario).

En `Client/EcfReceivedResults.cs`, en la clase `EcfFile`:
1. Agregar el parametro al constructor interno, al final: `bool timedOut,` pasa a `bool timedOut,\n            int count = 0)` y en el cuerpo `Count = count;`.
2. Agregar las propiedades:

```csharp
        /// <summary>Cuantos comprobantes trae el archivo: 1 para un XML o un PDF, o los que contiene el ZIP.</summary>
        public int Count { get; }

        /// <summary>El archivo es un ZIP (varios comprobantes) y no un XML suelto.</summary>
        public bool IsZip
        {
            get
            {
                return string.Equals(ContentType, "application/zip", StringComparison.OrdinalIgnoreCase)
                    || (FileName != null && FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            }
        }
```

- [ ] **Step 6: Payload de error con `tooMany`**

En `Client/EcfReceivedPayloads.cs`, clase `EcfReceivedErrorPayload`, agregar:

```csharp
        [JsonProperty("tooMany")]
        public bool TooMany { get; set; }
```

- [ ] **Step 7: Rutas**

En `Client/EcfEndpoints.cs`, junto a `ReceivedFile`, agregar:

```csharp
        /// <summary>GET del XML firmado de un comprobante emitido.</summary>
        internal static string SignedXml(string eNcf) => "Ecf/signed-xml/" + Uri.EscapeDataString(eNcf);

        /// <summary>GET de los XML firmados de un rango de dias (de RD, inclusivos).</summary>
        internal static string SignedXmlRange(DateTime fromDate, DateTime toDate) =>
            "Ecf/signed-xml?from=" + fromDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            + "&to=" + toDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
```

- [ ] **Step 8: Interfaz**

En `Client/IEcfClient.cs`, antes del cierre de la interfaz, agregar:

```csharp
        /// <summary>
        /// Descarga el XML firmado de un comprobante emitido por su cliente, por su eNCF. Si el eNCF
        /// se reintento, entrega el documento valido. No lanza excepcion por red, timeout ni
        /// respuesta del servidor.
        /// </summary>
        /// <exception cref="System.ArgumentException">Si el eNCF viene vacio.</exception>
        Task<EcfFile> GetSignedXmlAsync(string eNcf, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Version sincrona de <see cref="GetSignedXmlAsync"/>.</summary>
        EcfFile GetSignedXml(string eNcf);

        /// <summary>
        /// Descarga los XML firmados de los comprobantes emitidos entre dos dias (hora de RD, ambos
        /// inclusivos; solo cuenta la fecha). Un comprobante llega como XML y varios como un ZIP con
        /// un XML por eNCF (<see cref="EcfFile.IsZip"/>). Maximo 2000 por descarga
        /// (<see cref="EcfQueryStatus.TooManyResults"/>). Solo entran comprobantes validos.
        /// </summary>
        Task<EcfFile> GetSignedXmlRangeAsync(System.DateTime fromDate, System.DateTime toDate, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Version sincrona de <see cref="GetSignedXmlRangeAsync"/>.</summary>
        EcfFile GetSignedXmlRange(System.DateTime fromDate, System.DateTime toDate);
```

- [ ] **Step 9: `EcfClient`**

En `Client/EcfClient.cs` (leer antes de editar):

1. Reemplazar `DownloadReceivedAsync` por una versión general que lea la cantidad, y dejar los envoltorios de recibidos. La firma nueva y su cuerpo:

```csharp
        private Task<EcfFile> DownloadReceivedAsync(string eNcf, string kind, string rncEmisor, string defaultContentType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(eNcf))
                throw new ArgumentException("Falta el eNCF.", nameof(eNcf));

            var ncf = eNcf.Trim();

            return DownloadAsync(
                EcfEndpoints.ReceivedFile(ncf, kind, rncEmisor),
                "ECF_" + ncf.ToUpperInvariant() + "." + kind,
                defaultContentType,
                cancellationToken);
        }

        /// <summary>
        /// GET de un archivo (PDF, XML o ZIP). Lee el nombre de Content-Disposition y la cantidad de
        /// X-Ecf-Count; cualquier otro codigo se traduce sin lanzar.
        /// </summary>
        private async Task<EcfFile> DownloadAsync(string path, string fallbackFileName, string defaultContentType, CancellationToken cancellationToken)
        {
            try
            {
                EcfEndpoints.EnsureAllowed(_http.BaseAddress);

                using (var response = await _http.GetAsync(path, cancellationToken).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                        var name = response.Content.Headers.ContentDisposition != null
                            ? response.Content.Headers.ContentDisposition.FileName
                            : null;
                        var type = response.Content.Headers.ContentType != null
                            ? response.Content.Headers.ContentType.MediaType
                            : null;

                        var count = 1;
                        IEnumerable<string> values;
                        int parsed;

                        if (response.Headers.TryGetValues("X-Ecf-Count", out values)
                            && int.TryParse(values.FirstOrDefault(), out parsed)
                            && parsed > 0)
                            count = parsed;

                        return new EcfFile(
                            EcfQueryStatus.Ok,
                            "Descargado.",
                            string.Empty,
                            bytes,
                            string.IsNullOrWhiteSpace(name) ? fallbackFileName : name.Trim('"'),
                            string.IsNullOrWhiteSpace(type) ? defaultContentType : type,
                            null,
                            false,
                            count);
                    }

                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return InterpretReceivedError(response.StatusCode, body);
                }
            }
            catch (Exception exception) when (IsTransportException(exception))
            {
                var transport = Transport(exception, cancellationToken);

                return new EcfFile(EcfQueryStatus.TransportError, transport.Message, transport.Detail, null, null, null, null, transport.IsTimeout);
            }
        }

        public async Task<EcfFile> GetSignedXmlAsync(string eNcf, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(eNcf))
                throw new ArgumentException("Falta el eNCF.", nameof(eNcf));

            var ncf = eNcf.Trim();

            return await DownloadAsync(
                EcfEndpoints.SignedXml(ncf),
                ncf.ToUpperInvariant() + ".xml",
                "application/xml",
                cancellationToken).ConfigureAwait(false);
        }

        public EcfFile GetSignedXml(string eNcf) =>
            Task.Run(() => GetSignedXmlAsync(eNcf)).GetAwaiter().GetResult();

        public Task<EcfFile> GetSignedXmlRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default(CancellationToken)) =>
            DownloadAsync(
                EcfEndpoints.SignedXmlRange(fromDate, toDate),
                "XML_firmados_" + fromDate.ToString("yyyyMMdd") + "_" + toDate.ToString("yyyyMMdd") + ".zip",
                "application/zip",
                cancellationToken);

        public EcfFile GetSignedXmlRange(DateTime fromDate, DateTime toDate) =>
            Task.Run(() => GetSignedXmlRangeAsync(fromDate, toDate)).GetAwaiter().GetResult();
```

(El cuerpo anterior de `DownloadReceivedAsync` se elimina: queda reemplazado por lo anterior.)

2. En `InterpretReceivedError`, antes de la rama `else if (statusCode == HttpStatusCode.BadRequest)`, agregar:

```csharp
            else if (statusCode == HttpStatusCode.BadRequest && payload != null && payload.TooMany)
            {
                status = EcfQueryStatus.TooManyResults;
                message = serverMessage ?? "El rango trae demasiados comprobantes. Pida un rango mas corto.";
            }
```
(debe quedar antes de la rama genérica de `BadRequest` para que gane.)

- [ ] **Step 10: Ejecutar pruebas y compilar los tres targets**

Run:
```bash
dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | grep -E " error |Passed!|Failed!|\[FAIL\]" | sort -u
dotnet build src/Zynstorm.DGII.ECF -c Debug 2>&1 | grep -E " error |Build succeeded" | sort -u
```
Expected: todo en verde y los tres targets compilan. Si `FirstOrDefault` falta en `net452`, el archivo ya importa `System.Linq`.

- [ ] **Step 11: Agregar los payloads a la prueba de reflexión**

En `ReceivedTests.cs` la prueba `Cada_Propiedad_Leida_Del_Json_Lleva_Su_Nombre_Explicito` ya cubre `EcfReceivedErrorPayload` (ahora con `TooMany`): ejecutarla otra vez para confirmarlo.

Run: `dotnet test tests/Zynstorm.DGII.ECF.Tests --filter "FullyQualifiedName~Cada_Propiedad" 2>&1 | grep -E "Passed!|Failed!"`
Expected: `Passed`.

- [ ] **Step 12: Commit**

```bash
git add -A src tests docs README.md
git commit -m "feat: XML firmado por eNCF y por rango (XML o ZIP) y tipos de archivo neutros"
```

---

### Task 7: Guías, CHANGELOG y paquete

**Files:**
- Modify: `docs/CHANGELOG.md`, `docs/GUIA-DE-USO.md`, `docs/GUIA-RAPIDA.md`, `docs/GUIA-TECNICA.md`, `README.md`
- Regenerate: `docs/html/*.html`, `dist/*`

- [ ] **Step 1: CHANGELOG (dentro de 0.3.0)**

En `docs/CHANGELOG.md`, en la sección `## 0.3.0`, después de la viñeta de `GetReceivedXml` y antes de «Como el resto de la libreria, no lanzan excepcion», agregar:

```markdown
- **`GetSignedXml` / `GetSignedXmlAsync`**: descarga el **XML firmado de un comprobante que usted emitio**,
  por su eNCF. Si el eNCF se reintento, entrega el documento valido.
- **`GetSignedXmlRange` / `GetSignedXmlRangeAsync`**: descarga los XML firmados de los comprobantes
  emitidos entre dos dias (hora de RD, ambos inclusivos). Con un solo comprobante llega el XML;
  **con varios, un ZIP** con un XML por eNCF (`IsZip`, `Count`). Solo entran los validos (aceptados,
  aceptados con reparo o en proceso), maximo **2000** por descarga (`TooManyResults`).
- Los tipos de resultado se llaman **`EcfFile`** y **`EcfQueryStatus`** (sirven para recibidos y para
  el XML firmado). `EcfFile` trae `Count` e `IsZip`; el estado gana `TooManyResults`.
```

Y en el párrafo de estados de esa viñeta, nombrar `TooManyResults` entre los estados.

- [ ] **Step 2: Guía de uso**

En `docs/GUIA-DE-USO.md`:
1. Antes de la sección `## 11. Sin inyección de dependencias` (después de la 10b), agregar una sección nueva:

````markdown
---

## 10c. XML firmado de lo que usted emitió

Cada comprobante que la plataforma firma queda guardado. La librería permite pedir ese **XML firmado**,
por comprobante o por rango de fechas, para entregárselo a su cliente o guardarlo como respaldo.

### Por comprobante

```csharp
var xml = await _ecf.GetSignedXmlAsync("E310000000402");

if (xml.Status == EcfQueryStatus.Ok)
    File.WriteAllBytes(xml.FileName, xml.Content);   // xml.Text lo trae ya como texto
```

Si el eNCF se envió más de una vez, la plataforma entrega el documento válido (el aceptado).

### Por rango de fechas

```csharp
var rango = await _ecf.GetSignedXmlRangeAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 14));

if (rango.Status == EcfQueryStatus.Ok)
{
    // Un comprobante llega como .xml; varios, como un .zip con un XML por eNCF.
    File.WriteAllBytes(rango.FileName, rango.Content);
    Console.WriteLine(rango.Count + " comprobante(s)" + (rango.IsZip ? " en un ZIP" : ""));
}
else if (rango.Status == EcfQueryStatus.TooManyResults)
{
    // Más de 2000: pida el rango por partes.
}
```

- Las fechas son **días** en hora de República Dominicana, **ambos inclusivos**: solo cuenta la parte de fecha.
- Solo entran los comprobantes **válidos** (aceptados, aceptados con reparo o en proceso). Los rechazados y los que fallaron no.
- Un mismo eNCF aparece **una sola vez**, aunque se haya reenviado.
- El máximo es **2000 comprobantes** por descarga.
- El XML se entrega **exactamente como se firmó**: no lo abra y lo vuelva a guardar con otro formato, porque rompería la firma.
- `Status` es el mismo de los recibidos (sección 10b): `Ok`, `NotFound` (nada en el rango), `InvalidRequest`, `Unauthorized`, `TransportError`, `ContractError` y `TooManyResults`.
- Un ZIP grande tarda: si le da `TransportError` con `IsTimeout`, suba `Timeout` en la configuración o pida rangos más cortos.
- **Requiere la plataforma actualizada.**
````

2. En el checklist de la sección 15 agregar: `- [ ] \`GetSignedXml\` de un eNCF aceptado devuelve su XML firmado, y \`GetSignedXmlRange\` devuelve un ZIP cuando el rango tiene más de un comprobante.`
3. En la sección 1, en «Lo hace la librería», agregar: `- **Descarga el XML firmado** de lo que usted emitió, por eNCF o por rango de fechas.`

- [ ] **Step 3: Guía rápida y técnica**

`docs/GUIA-RAPIDA.md`: al final de la sección «7. Comprobantes recibidos de proveedores» (antes del `---`), agregar:

````markdown
## 8. XML firmado de lo que usted emitió

```csharp
var uno   = client.GetSignedXml("E310000000402");                                       // un XML
var rango = client.GetSignedXmlRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 14));
// rango.IsZip == true cuando hay más de un comprobante: un ZIP con un XML por eNCF.

if (rango.Status == EcfQueryStatus.Ok)
    File.WriteAllBytes(rango.FileName, rango.Content);
```

- Solo comprobantes válidos; máximo 2000 por descarga (`TooManyResults`).
- Las fechas son días en hora de RD, ambos inclusivos.
- No reformatee el XML: rompería la firma. Detalle en `GUIA-DE-USO.pdf`, sección 10c.
````

`docs/GUIA-TECNICA.md`: después de la sección `## 5d. Comprobantes recibidos` agregar `## 5e. XML firmado de lo emitido`:

````markdown
## 5e. XML firmado de lo emitido

| Método | Ruta | Respuesta |
|---|---|---|
| `GetSignedXmlAsync(eNcf)` | `GET Ecf/signed-xml/{eNcf}` | `application/xml`, `X-Ecf-Count: 1` |
| `GetSignedXmlRangeAsync(from, to)` | `GET Ecf/signed-xml?from=yyyy-MM-dd&to=yyyy-MM-dd` | `application/xml` si hay uno, `application/zip` si hay varios, con `X-Ecf-Count` |

**Qué entra.** Documentos del cliente de la API key con `RegisteredAt` dentro del rango de días de RD
(ambos inclusivos, la misma regla que la lista de facturas del panel), en estado válido
(Aceptado, Aceptado condicional o En proceso), uno por eNCF (el mejor, con la regla de `by-ncf`) y
con `XmlSigned` no vacío. Orden de las entradas del ZIP: por eNCF. Los bytes del XML son los de
`XmlSigned` en UTF-8 sin BOM, sin reformatear.

**Traducción de códigos.** Igual que en 5d, más `400` con `tooMany: true` → `TooManyResults` (el
tope es 2000 por descarga). Las fechas viajan como `yyyy-MM-dd` y la librería usa solo la parte de
fecha de los `DateTime`. `Count` sale de `X-Ecf-Count` (1 si falta) e `IsZip` del tipo de contenido
o de la extensión `.zip`.

**Tipos.** `EcfFile` y `EcfQueryStatus` sirven para recibidos y para el XML firmado (en 0.3.0 se
renombraron antes de distribuirse). `EcfReceivedErrorPayload` gana `tooMany` con su
`[JsonProperty]`.
````

Y en la lista de verificación de `GUIA-TECNICA.md` (sección 10): `- [ ] \`GetSignedXml\` y \`GetSignedXmlRange\` contra staging devuelven el XML firmado de un aceptado, un ZIP con más de uno y \`NotFound\` en un rango vacío (con las DLL entregadas).` Actualizar la cifra «Las 380 pruebas pasan» a la cantidad real tras la Task 6.

- [ ] **Step 4: README**

En `README.md`, al final del párrafo «Comprobantes recibidos (desde 0.3.0)» agregar: `Tambien descarga el **XML firmado de lo que usted emitio**, por eNCF o por rango de fechas (un XML, o un ZIP si son varios): \`GetSignedXml\` y \`GetSignedXmlRange\` (seccion 10c).`

- [ ] **Step 5: Regenerar y empaquetar**

Run:
```bash
cd /c/Projects/Zynstorm.DGII.ECF
dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | grep -E "Passed!|Failed!"
```
y en PowerShell: `cmd /c ".\generar-dll-ofuscada.cmd --no-pause"`; luego:
```bash
python docs/build-pdf.py && python docs/build-html.py && python docs/build-package.py
```
Expected: «API publica y runtime ... verificados» para las dos DLL, PDF y HTML regenerados y `dist/Zynstorm.DGII.ECF-0.3.0.zip` firmado sin avisos.

- [ ] **Step 6: Comprobar la DLL ofuscada**

Con PowerShell, como en la entrega anterior, cargar `dist\netstandard2.0\Zynstorm.DGII.ECF.dll` por reflexión y comprobar que existen `EcfFile`, `EcfQueryStatus` y los cuatro métodos `GetSignedXml*` en `IEcfClient`, y que el atributo `tooMany` sobrevive en una clase interna renombrada.

- [ ] **Step 7: Revisar las páginas nuevas de los PDF**

Con PyMuPDF (`import fitz`) renderizar a PNG las páginas que contienen «GetSignedXml» de `dist/GUIA-DE-USO.pdf` y `dist/GUIA-RAPIDA.pdf` y mirarlas con `Read`; corregir el Markdown si algo se desfigura y regenerar.

- [ ] **Step 8: Commit**

```bash
git add -A README.md docs
git commit -m "docs: XML firmado por eNCF y por rango en las guias y el CHANGELOG de la 0.3.0"
```

---

## Parte D: Cierre

### Task 8: Verificación final y memoria

- [ ] **Step 1: Verificar los tres repos**

Backend en `Staging` y en `feature/emit-idempotente` (`dotnet test`), panel (`npx tsc --noEmit`, `git status`, `git checkout tsconfig.tsbuildinfo`), librería (`dotnet test`). Expected: todo en verde, ramas correctas, sin cambios sin commitear.

- [ ] **Step 2: Memoria**

Actualizar `C:\Users\JoseTrinidad\.claude\projects\C--Projects-ZynstormECF-WorkSpace\memory\recibidos-pdf-api-libreria.md` con los hashes nuevos, los endpoints `Ecf/signed-xml*` y `EcfDocument/.../signed-xml`, la vista previa, el renombre a `EcfFile`/`EcfQueryStatus` y lo pendiente de probar en Staging (ZIP real, vista previa, límite de 2000). Actualizar su línea en `MEMORY.md`.

- [ ] **Step 3: Reportar**

Resumir por repo, con hashes, pruebas y lo no verificado (Staging, la DLL ofuscada contra la plataforma real). No hacer push.
