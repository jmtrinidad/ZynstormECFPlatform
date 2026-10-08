# Simulación (Paso 4): reenviar tipo o comprobante — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Desde el Paso 4 poder reenviar todos los comprobantes de una tarjeta (grupo) o un solo comprobante del historial, sin reiniciar la simulación completa.

**Architecture:** `OldCertificationSimulationService.ProcessSimulacionEcfJobInternalAsync` recibe una selección opcional (`resendGroup` / `resendDocumentGuid`); con selección filtra la matriz, reemplaza solo los `CertificationDocument` del grupo y reconstruye desde la base las facturas 31 aceptadas que necesitan las notas 33/34. La lógica pura (grupos, filtro, parseo) vive en una clase estática testeable `SimulationResendSelection`. Un endpoint nuevo `POST simulation/resend` encola el job; el frontend agrega botones y recarga el historial al terminar.

**Tech Stack:** .NET 10, EF Core (Npgsql), Hangfire, SignalR, xUnit; Next.js/React (TypeScript).

Spec: `Docs/superpowers/specs/2026-10-08-simulacion-reenvio-design.md`

## Global Constraints

- **No se modifica nada fuera de certificación.** Archivos permitidos, y solo estos:
  - `ZynstormECFPlatform.Services/Certification/OldSimulation/*`
  - `ZynstormECFPlatform.Dtos/CertificationJobStatusDto.cs`
  - `ZynstormECFPlatform.Dtos/BusinessSimulationDtos.cs`
  - `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs`
  - `ZynstormECFPlatform.Tests/Certification/*` (nuevos)
  - `ZynstormECFPlatform-FrontEnd/app/certificacion/page.tsx`
  - `ZynstormECFPlatform-FrontEnd/services/certification.service.ts`
- No tocar `DgiiTransmissionService`, `DgiiTransmissionResult`, `EcfRequestDtos`, generadores de producción, entidades ni migraciones.
- La simulación completa ("Iniciar Simulación de Negocio") debe comportarse **exactamente igual** que hoy: todo cambio en el motor va detrás de `isResend`.
- Grupos (strings exactos): `31`, `33`, `34`, `32-250K`, `41`, `43`, `44`, `45`, `46`, `47`, `32-RFCE`, `32-MANUAL`. `32-MANUAL` no es reenviable por sí solo.
- Mensajes al usuario en español.
- Ramas: backend `feature/simulacion-reenvio` (ya creada desde `main`); frontend: crear `feature/simulacion-reenvio` desde `Staging` en `ZynstormECFPlatform-FrontEnd`.

Comandos (desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`):
- Tests: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~Certification"`
- Build API: `dotnet build ZynstormECFPlatform.Web.Api -nologo -v q`

---

### Task 1: `SimulationResendSelection` (lógica pura de grupos)

**Files:**
- Create: `ZynstormECFPlatform.Services/Certification/OldSimulation/SimulationResendSelection.cs`
- Test: `ZynstormECFPlatform.Tests/Certification/SimulationResendSelectionTests.cs`

**Interfaces:**
- Produces:
  - `const string SimulationResendSelection.Over250k32 = "32-250K"`, `Rfce32 = "32-RFCE"`, `Manual32 = "32-MANUAL"`
  - `bool IsResendableGroup(string? group)`
  - `string GroupOfRow(int type, bool isSummary, bool isManual)`
  - `string GroupOfDocument(string? ecfTypeCode, string? trackId, string? xmlSent)`
  - `IReadOnlySet<string> GroupsReplacedBy(string group)`
  - `bool RunsRow(string? selectedGroup, string rowGroup)` (`null` = simulación completa)
  - `void AddCount(SimulationStatsDto stats, string group)`
  - `Accepted31Reference? ParseAccepted31(string? xmlSent)`
  - `sealed record Accepted31Reference(string Ncf, DateTime IssueDate, string? CustomerRnc, string? CustomerName, decimal? FirstItemUnitPrice)`

- [ ] **Step 1: Write the failing tests**

`ZynstormECFPlatform.Tests/Certification/SimulationResendSelectionTests.cs`:

```csharp
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Tests.Certification;

public class SimulationResendSelectionTests
{
    [Theory]
    [InlineData(31, false, false, "31")]
    [InlineData(32, false, false, "32-250K")]
    [InlineData(32, true, false, "32-RFCE")]
    [InlineData(32, false, true, "32-MANUAL")]
    [InlineData(47, false, false, "47")]
    public void GroupOfRow_MapsMatrixRows(int type, bool isSummary, bool isManual, string expected)
    {
        Assert.Equal(expected, SimulationResendSelection.GroupOfRow(type, isSummary, isManual));
    }

    [Fact]
    public void GroupOfDocument_ClassifiesThe32Variants()
    {
        Assert.Equal("32-MANUAL", SimulationResendSelection.GroupOfDocument("32", "MANUAL", "<ECF/>"));
        Assert.Equal("32-RFCE", SimulationResendSelection.GroupOfDocument("32", "abc", "<RFCE><Encabezado/></RFCE>"));
        Assert.Equal("32-250K", SimulationResendSelection.GroupOfDocument("32", "abc", "<ECF><Encabezado/></ECF>"));
        Assert.Equal("34", SimulationResendSelection.GroupOfDocument("34", "abc", "<ECF/>"));
    }

    [Fact]
    public void RunsRow_FullSimulationRunsEverything_RfceAlsoRunsManual()
    {
        Assert.True(SimulationResendSelection.RunsRow(null, "41"));
        Assert.True(SimulationResendSelection.RunsRow("34", "34"));
        Assert.False(SimulationResendSelection.RunsRow("34", "31"));
        Assert.True(SimulationResendSelection.RunsRow("32-RFCE", "32-MANUAL"));
        Assert.False(SimulationResendSelection.RunsRow("32-250K", "32-RFCE"));
    }

    [Fact]
    public void IsResendableGroup_ExcludesManualAndUnknown()
    {
        Assert.True(SimulationResendSelection.IsResendableGroup("32-RFCE"));
        Assert.True(SimulationResendSelection.IsResendableGroup("45"));
        Assert.False(SimulationResendSelection.IsResendableGroup("32-MANUAL"));
        Assert.False(SimulationResendSelection.IsResendableGroup("99"));
        Assert.False(SimulationResendSelection.IsResendableGroup(null));
    }

    [Fact]
    public void AddCount_IncrementsTheMatchingCounter()
    {
        var stats = new SimulationStatsDto();
        SimulationResendSelection.AddCount(stats, "31");
        SimulationResendSelection.AddCount(stats, "31");
        SimulationResendSelection.AddCount(stats, "32-RFCE");
        SimulationResendSelection.AddCount(stats, "32-MANUAL");
        SimulationResendSelection.AddCount(stats, "32-250K");

        Assert.Equal(2, stats.Type31);
        Assert.Equal(1, stats.Type32Rfce);
        Assert.Equal(1, stats.Type32Manual);
        Assert.Equal(1, stats.Type32Greater250k);
    }

    [Fact]
    public void ParseAccepted31_ReadsReferenceFieldsFromSignedXml()
    {
        const string xml = """
            <ECF>
              <Encabezado>
                <IdDoc><TipoeCF>31</TipoeCF><eNCF>E310000000007</eNCF></IdDoc>
                <Emisor><FechaEmision>08-10-2026</FechaEmision></Emisor>
                <Comprador><RNCComprador>131880681</RNCComprador><RazonSocialComprador>DOCUMENTOS ELECTRONICOS DE 03</RazonSocialComprador></Comprador>
              </Encabezado>
              <DetallesItems>
                <Item><NumeroLinea>1</NumeroLinea><PrecioUnitarioItem>245.0000</PrecioUnitarioItem></Item>
                <Item><NumeroLinea>2</NumeroLinea><PrecioUnitarioItem>10.00</PrecioUnitarioItem></Item>
              </DetallesItems>
            </ECF>
            """;

        var r = SimulationResendSelection.ParseAccepted31(xml);

        Assert.NotNull(r);
        Assert.Equal("E310000000007", r!.Ncf);
        Assert.Equal(new DateTime(2026, 10, 8), r.IssueDate);
        Assert.Equal("131880681", r.CustomerRnc);
        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 03", r.CustomerName);
        Assert.Equal(245.0000m, r.FirstItemUnitPrice);
    }

    [Fact]
    public void ParseAccepted31_ReturnsNullForInvalidXml()
    {
        Assert.Null(SimulationResendSelection.ParseAccepted31("no es xml"));
        Assert.Null(SimulationResendSelection.ParseAccepted31(null));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~SimulationResendSelection"`
Expected: build error `The name 'SimulationResendSelection' does not exist`.

- [ ] **Step 3: Implement**

`ZynstormECFPlatform.Services/Certification/OldSimulation/SimulationResendSelection.cs`:

```csharp
using System.Globalization;
using System.Xml.Linq;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

/// <summary>
/// Grupos de la simulación (una tarjeta del Paso 4 = un grupo) y reglas puras para reenviar
/// un grupo o un comprobante sin reiniciar la simulación completa.
/// </summary>
public static class SimulationResendSelection
{
    public const string Over250k32 = "32-250K";
    public const string Rfce32 = "32-RFCE";
    public const string Manual32 = "32-MANUAL";

    private static readonly HashSet<string> ResendableGroups = new(StringComparer.Ordinal)
    {
        "31", "33", "34", Over250k32, "41", "43", "44", "45", "46", "47", Rfce32
    };

    public static bool IsResendableGroup(string? group) => group != null && ResendableGroups.Contains(group);

    public static string GroupOfRow(int type, bool isSummary, bool isManual) =>
        type == 32 ? (isManual ? Manual32 : isSummary ? Rfce32 : Over250k32) : type.ToString();

    public static string GroupOfDocument(string? ecfTypeCode, string? trackId, string? xmlSent)
    {
        if (ecfTypeCode != "32") return ecfTypeCode ?? string.Empty;
        if (trackId == "MANUAL") return Manual32;
        return RootName(xmlSent) == "RFCE" ? Rfce32 : Over250k32;
    }

    // Reenviar Consumo (Resumen) arrastra a los manuales: se generan con los RFCE aceptados.
    public static IReadOnlySet<string> GroupsReplacedBy(string group) =>
        group == Rfce32 ? new HashSet<string> { Rfce32, Manual32 } : new HashSet<string> { group };

    /// <summary>null = simulación completa: corre todos los renglones de la matriz.</summary>
    public static bool RunsRow(string? selectedGroup, string rowGroup) =>
        selectedGroup == null || GroupsReplacedBy(selectedGroup).Contains(rowGroup);

    public static void AddCount(SimulationStatsDto stats, string group)
    {
        switch (group)
        {
            case "31": stats.Type31++; break;
            case "33": stats.Type33++; break;
            case "34": stats.Type34++; break;
            case Over250k32: stats.Type32Greater250k++; break;
            case Rfce32: stats.Type32Rfce++; break;
            case Manual32: stats.Type32Manual++; break;
            case "41": stats.Type41++; break;
            case "43": stats.Type43++; break;
            case "44": stats.Type44++; break;
            case "45": stats.Type45++; break;
            case "46": stats.Type46++; break;
            case "47": stats.Type47++; break;
        }
    }

    /// <summary>Datos de una factura 31 aceptada que necesita una nota 33/34 para referenciarla.</summary>
    public static Accepted31Reference? ParseAccepted31(string? xmlSent)
    {
        if (string.IsNullOrWhiteSpace(xmlSent)) return null;
        try
        {
            var doc = XDocument.Parse(xmlSent);
            string? Value(string name) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value;

            var ncf = Value("eNCF");
            if (string.IsNullOrWhiteSpace(ncf)) return null;
            if (!DateTime.TryParseExact(Value("FechaEmision"), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate))
                return null;

            var firstPrice = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Item")?
                .Elements().FirstOrDefault(e => e.Name.LocalName == "PrecioUnitarioItem")?.Value;
            decimal? price = decimal.TryParse(firstPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) ? p : null;

            return new Accepted31Reference(ncf, issueDate, Value("RNCComprador"), Value("RazonSocialComprador"), price);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? RootName(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try { return XDocument.Parse(xml).Root?.Name.LocalName; }
        catch (System.Xml.XmlException) { return null; }
    }
}

public sealed record Accepted31Reference(string Ncf, DateTime IssueDate, string? CustomerRnc, string? CustomerName, decimal? FirstItemUnitPrice);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~SimulationResendSelection"`
Expected: `Passed!  - Failed: 0, Passed: 11`

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Certification/OldSimulation/SimulationResendSelection.cs ZynstormECFPlatform.Tests/Certification/SimulationResendSelectionTests.cs
git commit -m "feat(certificacion): grupos y reglas puras para reenviar en la simulación"
```

---

### Task 2: DTOs (`DocumentGuidId` y request de reenvío)

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/CertificationJobStatusDto.cs` (clase `CertificationStepResultDto`)
- Modify: `ZynstormECFPlatform.Dtos/BusinessSimulationDtos.cs`
- Modify: `ZynstormECFPlatform.Services/Certification/OldSimulation/OldCertificationSimulationService.cs` (`GetLastSimulationResultsByClientAsync`)

**Interfaces:**
- Produces: `CertificationStepResultDto.DocumentGuidId : string?`; `ResendSimulationRequestDto { BusinessTypeGuidId, ClientGuidId, Group?, DocumentGuidId? }`.

- [ ] **Step 1: Add the field to `CertificationStepResultDto`** (after `BuyerRnc`):

```csharp
    public string BuyerRnc { get; set; } = string.Empty;
    /// <summary>GuidId del CertificationDocument (simulación): permite reenviar ese comprobante.</summary>
    public string? DocumentGuidId { get; set; }
}
```

- [ ] **Step 2: Add the request DTO** at the end of `BusinessSimulationDtos.cs`:

```csharp
public class ResendSimulationRequestDto
{
    public string BusinessTypeGuidId { get; set; } = string.Empty;
    public string ClientGuidId { get; set; } = string.Empty;
    /// <summary>Grupo a reenviar (p. ej. "34", "32-RFCE"). Excluyente con DocumentGuidId.</summary>
    public string? Group { get; set; }
    /// <summary>GuidId del CertificationDocument a reenviar. Excluyente con Group.</summary>
    public string? DocumentGuidId { get; set; }
}
```

- [ ] **Step 3: Return the GuidId in last results.** In `GetLastSimulationResultsByClientAsync`, inside `completedSteps.Add(new CertificationStepResultDto { ... })`, after `BuyerRnc = buyerRnc`:

```csharp
                    BuyerRnc = buyerRnc,
                    DocumentGuidId = d.GuidId
```

- [ ] **Step 4: Build**

Run: `dotnet build ZynstormECFPlatform.Web.Api -nologo -v q`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Dtos/CertificationJobStatusDto.cs ZynstormECFPlatform.Dtos/BusinessSimulationDtos.cs ZynstormECFPlatform.Services/Certification/OldSimulation/OldCertificationSimulationService.cs
git commit -m "feat(certificacion): DocumentGuidId en resultados de simulación y request de reenvío"
```

---

### Task 3: Motor de simulación con selección

**Files:**
- Modify: `ZynstormECFPlatform.Services/Certification/OldSimulation/OldCertificationSimulationService.cs`
- Modify: `ZynstormECFPlatform.Services/Certification/OldSimulation/IOldCertificationSimulationService.cs`

**Interfaces:**
- Consumes: todo lo de `SimulationResendSelection` (Task 1); `CertificationStepResultDto.DocumentGuidId` (Task 2).
- Produces (interfaz):
  - `Task<string> EnqueueResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string webRootPath)` — lanza `ArgumentException` (datos inválidos) o `InvalidOperationException` (ya hay simulación en curso).
  - `[AutomaticRetry(Attempts = 0)] Task ProcessBusinessResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string jobId, string webRootPath)`

Este archivo no tiene tests de integración (depende de DGII, Hangfire y la base). La lógica decidible ya está cubierta en Task 1; aquí se verifica con build + tests existentes y en staging (Task 6). Cada cambio del motor va detrás de `isResend` para que la simulación completa no cambie.

- [ ] **Step 1: Interface.** In `IOldCertificationSimulationService.cs`, after `ProcessBusinessSimulationJobAsync`:

```csharp
    Task<string> EnqueueResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string webRootPath);

    [AutomaticRetry(Attempts = 0)]
    Task ProcessBusinessResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string jobId, string webRootPath);
```

- [ ] **Step 2: Running-job guard field.** Next to the existing `_jobStatuses` static field in `OldCertificationSimulationService`, add:

```csharp
    // Simulación en curso por cliente (GuidId -> jobId): un reenvío no puede correr junto a otra simulación.
    private static readonly ConcurrentDictionary<string, string> _runningByClient = new();
```

- [ ] **Step 3: Extract the business context and active-process lookup.** Replace the whole `ProcessBusinessSimulationJobAsync` method (and register the full run in the guard inside `EnqueueBusinessSimulationJobAsync`) with:

```csharp
    public async Task<string> EnqueueBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        _runningByClient[clientGuidId] = jobId;
        Console.WriteLine($"[Simulation] Enqueueing job {jobId} for businessType {businessTypeGuidId}");
        BackgroundJob.Enqueue<IOldCertificationSimulationService>(x => x.ProcessBusinessSimulationJobAsync(businessTypeGuidId, clientGuidId, jobId, webRootPath));
        return jobId;
    }
```

```csharp
    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath)
    {
        try
        {
            var ctx = await LoadBusinessContextAsync(businessTypeGuidId, clientGuidId);
            await ProcessSimulacionEcfJobInternalAsync(ctx.Dto, jobId, webRootPath, ctx.Samples, ctx.SampleIds, ctx.BusinessTypeId);
        }
        finally
        {
            _runningByClient.TryRemove(new KeyValuePair<string, string>(clientGuidId, jobId));
        }
    }

    public async Task<string> EnqueueResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(group) == string.IsNullOrWhiteSpace(documentGuidId))
            throw new ArgumentException("Debe indicar un grupo o un comprobante a reenviar (solo uno).");
        if (!string.IsNullOrWhiteSpace(group) && !SimulationResendSelection.IsResendableGroup(group))
            throw new ArgumentException($"Grupo de simulación inválido: {group}.");

        var client = await _clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new ArgumentException("Cliente no encontrado.");
        var process = await FindActiveProcessAsync(client.ClientId)
            ?? throw new ArgumentException("Primero ejecute la simulación completa.");

        if (!string.IsNullOrWhiteSpace(documentGuidId))
        {
            var doc = await _context.Set<CertificationDocument>()
                .Include(d => d.EcfType)
                .FirstOrDefaultAsync(d => d.GuidId == documentGuidId && d.CertificationProcessId == process.CertificationProcessId)
                ?? throw new ArgumentException("Comprobante no encontrado en la simulación vigente.");
            if (SimulationResendSelection.GroupOfDocument(doc.EcfType?.Code, doc.TrackId, doc.XmlSent) == SimulationResendSelection.Manual32)
                throw new ArgumentException("Los comprobantes manuales se regeneran reenviando Consumo (Resumen).");
        }

        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        if (!_runningByClient.TryAdd(clientGuidId, jobId))
            throw new InvalidOperationException("Ya hay una simulación en curso para este cliente.");

        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        try
        {
            Console.WriteLine($"[Simulation] Enqueueing resend job {jobId} (group={group ?? "-"}, document={documentGuidId ?? "-"})");
            BackgroundJob.Enqueue<IOldCertificationSimulationService>(x =>
                x.ProcessBusinessResendJobAsync(businessTypeGuidId, clientGuidId, group, documentGuidId, jobId, webRootPath));
        }
        catch
        {
            _runningByClient.TryRemove(new KeyValuePair<string, string>(clientGuidId, jobId));
            throw;
        }
        return jobId;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBusinessResendJobAsync(string businessTypeGuidId, string clientGuidId, string? group, string? documentGuidId, string jobId, string webRootPath)
    {
        try
        {
            var ctx = await LoadBusinessContextAsync(businessTypeGuidId, clientGuidId);
            await ProcessSimulacionEcfJobInternalAsync(ctx.Dto, jobId, webRootPath, ctx.Samples, ctx.SampleIds, ctx.BusinessTypeId,
                resendGroup: string.IsNullOrWhiteSpace(group) ? null : group,
                resendDocumentGuid: string.IsNullOrWhiteSpace(documentGuidId) ? null : documentGuidId);
        }
        finally
        {
            _runningByClient.TryRemove(new KeyValuePair<string, string>(clientGuidId, jobId));
        }
    }

    private async Task<(OldEcfInvoiceRequestDto Dto, Dictionary<string, string> Samples, Dictionary<string, int> SampleIds, int BusinessTypeId)> LoadBusinessContextAsync(string businessTypeGuidId, string clientGuidId)
    {
        var client = await _clientService.GetByAsync(c => c.GuidId == clientGuidId) ?? throw new Exception("Cliente no encontrado.");
        var businessType = await _context.Set<BusinessType>().FirstOrDefaultAsync(b => b.GuidId == businessTypeGuidId) ?? throw new Exception("Tipo de negocio no encontrado.");

        var samples = await _context.Set<BusinessSimulationSample>()
            .Where(s => s.BusinessTypeId == businessType.BusinessTypeId)
            .ToDictionaryAsync(s => s.EcfType.ToString(), s => s.JsonData);

        var sampleIds = await _context.Set<BusinessSimulationSample>()
            .Where(s => s.BusinessTypeId == businessType.BusinessTypeId)
            .ToDictionaryAsync(s => s.EcfType.ToString(), s => s.BusinessSimulationSampleId);

        var mainBranch = await _context.Set<ClientBranche>().FirstOrDefaultAsync(b => b.ClientId == client.ClientId && b.IsMain);

        var initialDto = new OldEcfInvoiceRequestDto
        {
            IssuerRnc = client.Rnc,
            IssuerName = client.Name,
            IssuerAddress = mainBranch?.Address ?? "CALLE PRINCIPAL #1",
            ClientId = client.ClientId
        };

        return (initialDto, samples, sampleIds, businessType.BusinessTypeId);
    }

    private Task<CertificationProcess?> FindActiveProcessAsync(int clientId) =>
        _context.Set<CertificationProcess>()
            .OrderByDescending(p => p.RegisteredAt)
            .FirstOrDefaultAsync(p => p.ClientId == clientId &&
                                      (p.Status == CertificationStatus.Pending || p.Status == CertificationStatus.InProgress));

    // Facturas 31 aceptadas de la simulación vigente, para que una nota 33/34 reenviada sola las referencie.
    private async Task<List<(string Ncf, DateTime IssueDate, string? CustomerRnc, OldEcfInvoiceRequestDto Dto)>> LoadAccepted31PoolAsync(int processId, Dictionary<string, string>? samples)
    {
        var pool = new List<(string Ncf, DateTime IssueDate, string? CustomerRnc, OldEcfInvoiceRequestDto Dto)>();
        if (samples == null || !samples.TryGetValue("31", out var sampleJson)) return pool;

        var docs = await _context.Set<CertificationDocument>()
            .Include(d => d.EcfType)
            .Where(d => d.CertificationProcessId == processId && d.Status == DocumentStatus.Accepted && d.EcfType.Code == "31")
            .OrderBy(d => d.ENcfSecuence)
            .ToListAsync();

        foreach (var d in docs)
        {
            var reference = SimulationResendSelection.ParseAccepted31(d.XmlSent);
            if (reference == null) continue;

            var dto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(sampleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (dto == null) continue;

            dto.Ncf = reference.Ncf;
            dto.IssueDate = reference.IssueDate;
            dto.CustomerRnc = reference.CustomerRnc!;
            dto.CustomerName = reference.CustomerName!;
            if (reference.FirstItemUnitPrice.HasValue && dto.Items.Count > 0)
                dto.Items[0].UnitPrice = reference.FirstItemUnitPrice.Value;

            pool.Add((reference.Ncf, reference.IssueDate, reference.CustomerRnc, dto));
        }
        return pool;
    }
```

- [ ] **Step 4: Selection parameters and process handling in `ProcessSimulacionEcfJobInternalAsync`.**

4a. Change the signature:

```csharp
    private async Task ProcessSimulacionEcfJobInternalAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath, Dictionary<string, string>? samples = null, Dictionary<string, int>? sampleIds = null, int? businessTypeId = null, string? resendGroup = null, string? resendDocumentGuid = null)
    {
        bool isResend = resendGroup != null || resendDocumentGuid != null;
        CertificationDocument? docToReplace = null;
        var preservedDocs = new List<CertificationDocument>();
```

(keep the existing `accepted31Pool` / `rfcePool` declarations right after.)

4b. Replace the block from `var process = await _context.Set<CertificationProcess>()` through the closing `}` of its `else { process = new CertificationProcess ... }` with:

```csharp
            var process = await FindActiveProcessAsync(client.ClientId);

            if (isResend)
            {
                if (process == null) throw new Exception("Primero ejecute la simulación completa.");

                var processDocs = await _context.Set<CertificationDocument>()
                    .Include(d => d.EcfType)
                    .Where(d => d.CertificationProcessId == process.CertificationProcessId)
                    .ToListAsync();

                string GroupOf(CertificationDocument d) => SimulationResendSelection.GroupOfDocument(d.EcfType?.Code, d.TrackId, d.XmlSent);

                if (resendDocumentGuid != null)
                {
                    // Un comprobante: el viejo (y su manual, si es RFCE) se reemplaza solo si el nuevo es aceptado.
                    docToReplace = processDocs.FirstOrDefault(d => d.GuidId == resendDocumentGuid)
                        ?? throw new Exception("Comprobante no encontrado en la simulación vigente.");
                    resendGroup = GroupOf(docToReplace);
                    var oldNcf = docToReplace.ENcfSecuence;
                    preservedDocs = processDocs
                        .Where(d => d != docToReplace && !(resendGroup == SimulationResendSelection.Rfce32 && d.TrackId == "MANUAL" && d.ENcfSecuence == oldNcf))
                        .ToList();
                }
                else
                {
                    // Un grupo: se borran sus registros vigentes y se envían de nuevo.
                    var replaced = SimulationResendSelection.GroupsReplacedBy(resendGroup!);
                    var toDelete = processDocs.Where(d => replaced.Contains(GroupOf(d))).ToList();
                    preservedDocs = processDocs.Except(toDelete).ToList();
                    Console.WriteLine($"[Simulation] Resend group {resendGroup}: deleting {toDelete.Count} documents.");
                    if (toDelete.Any())
                    {
                        _context.Set<CertificationDocument>().RemoveRange(toDelete);
                        await _context.SaveChangesAsync();
                    }
                }

                // Las tarjetas que no se reenvían conservan sus contadores.
                foreach (var d in preservedDocs)
                    SimulationResendSelection.AddCount(status.SimulationStats, GroupOf(d));
            }
            else if (process != null)
            {
                var docsToDelete = await _context.Set<CertificationDocument>()
                    .Where(d => d.CertificationProcessId == process.CertificationProcessId)
                    .ToListAsync();

                Console.WriteLine($"[Simulation] Deleting {docsToDelete.Count} old documents for process {process.CertificationProcessId}");
                if (docsToDelete.Any())
                {
                    _context.Set<CertificationDocument>().RemoveRange(docsToDelete);
                    await _context.SaveChangesAsync();
                }
                Console.WriteLine($"[Simulation] Old documents deleted. Next eNCF will be reserved from ENcf.Sequence.");
            }
            else
            {
                process = new CertificationProcess
                {
                    ClientId = client.ClientId,
                    Environment = DgiiEnvironment.CerteCF,
                    Status = CertificationStatus.InProgress,
                    StartDate = DateTime.Now,
                    CurrentStepId = step4.CertificationStepId,
                    RegisteredAt = DateTime.Now
                };
                _context.Set<CertificationProcess>().Add(process);
                await _context.SaveChangesAsync();
            }
```

- [ ] **Step 5: Selected rows and total steps.** Right after the `matrix` array declaration, add:

```csharp
            bool RowSelected((int Type, int Count, bool IsSummary, bool IsManual, decimal? MinAmount, decimal? MaxAmount) m) =>
                SimulationResendSelection.RunsRow(resendGroup, SimulationResendSelection.GroupOfRow(m.Type, m.IsSummary, m.IsManual));
            int RowCount((int Type, int Count, bool IsSummary, bool IsManual, decimal? MinAmount, decimal? MaxAmount) m) =>
                docToReplace != null ? 1 : m.Count;
```

and replace `status.TotalSteps = matrix.Sum(m => m.Count);` with:

```csharp
            status.TotalSteps = matrix.Where(RowSelected).Sum(RowCount);
```

(Keep the `TotalType*` lines on the full `matrix`: las tarjetas siempre muestran el total de su grupo.)

- [ ] **Step 6: Filter the automatic loop.** Replace:

```csharp
            foreach (var item in matrix.Where(m => !m.IsManual))
            {
                for (int i = 0; i < item.Count; i++)
```

with:

```csharp
            foreach (var item in matrix.Where(m => !m.IsManual).Where(RowSelected))
            {
                for (int i = 0; i < RowCount(item); i++)
```

- [ ] **Step 7: Notes 33/34 can use 31s from the database.** Replace:

```csharp
                        int poolIndex = (item.Type == 33) ? i : (1 + i);
                        if (accepted31Pool.Count <= poolIndex) continue;
```

with:

```csharp
                        if (isResend && accepted31Pool.Count == 0)
                        {
                            accepted31Pool.AddRange(await LoadAccepted31PoolAsync(process.CertificationProcessId, samples));
                            if (accepted31Pool.Count == 0)
                                throw new Exception("No hay facturas 31 aceptadas para referenciar. Reenvíe primero Factura de Crédito Fiscal.");
                        }
                        int poolIndex = (item.Type == 33) ? i : (1 + i);
                        if (isResend) poolIndex %= accepted31Pool.Count;
                        if (accepted31Pool.Count <= poolIndex) continue;
```

- [ ] **Step 8: Persist / replace in document mode.** Replace the persistence block of the automatic loop:

```csharp
                    // PERSISTENCE: Save to CertificationDocument table
                    var certDoc = new CertificationDocument
                    {
                        ...
                    };
                    _context.Set<CertificationDocument>().Add(certDoc);
                    await _context.SaveChangesAsync();
```

with (same initializer fields as today, shown completely):

```csharp
                    // PERSISTENCE: Save to CertificationDocument table.
                    // Reenvío de un comprobante rechazado: se conserva el registro anterior y el
                    // rechazo queda solo en el log del job.
                    CertificationDocument? certDoc = null;
                    if (docToReplace == null || isAccepted)
                    {
                        certDoc = new CertificationDocument
                        {
                            CertificationProcessId = process.CertificationProcessId,
                            ENcfSecuence = currentDto.Ncf,
                            ENcfId = encfRecord.ENcfId,
                            EcfTypeId = ecfTypeRecord.EcfTypeId,
                            XmlSent = signedXml,
                            TrackId = trackId,
                            Status = isAccepted ? DocumentStatus.Accepted : DocumentStatus.Rejected,
                            SentAt = DateTimeExtensions.DrNow,
                            RegisteredAt = DateTimeExtensions.DrNow,
                            GuidId = Guid.NewGuid().ToString()
                        };
                        _context.Set<CertificationDocument>().Add(certDoc);

                        if (docToReplace != null)
                        {
                            _context.Set<CertificationDocument>().Remove(docToReplace);
                            if (item.IsSummary)
                            {
                                var oldManual = await _context.Set<CertificationDocument>()
                                    .Where(d => d.CertificationProcessId == process.CertificationProcessId && d.TrackId == "MANUAL" && d.ENcfSecuence == docToReplace.ENcfSecuence)
                                    .ToListAsync();
                                _context.Set<CertificationDocument>().RemoveRange(oldManual);
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
```

and in the `status.CompletedSteps.Add(new CertificationStepResultDto { ... })` right below, after `XmlFileName = xmlFileName`, add:

```csharp
                        XmlFileName = xmlFileName,
                        DocumentGuidId = certDoc?.GuidId
```

- [ ] **Step 9: Filter the manual loop.** Replace:

```csharp
            foreach (var item in matrix.Where(m => m.IsManual))
            {
                if (stopRequested) break;
                for (int i = 0; i < item.Count; i++)
```

with:

```csharp
            foreach (var item in matrix.Where(m => m.IsManual).Where(RowSelected))
            {
                if (stopRequested) break;
                // Reenvío de un RFCE: solo su manual (el pool tiene un solo RFCE).
                int manualCount = docToReplace != null ? rfcePool.Count : item.Count;
                for (int i = 0; i < manualCount; i++)
```

and in the manual `status.CompletedSteps.Add(new CertificationStepResultDto { ... })`, after `XmlFileName = xmlFileName`:

```csharp
                        XmlFileName = xmlFileName,
                        DocumentGuidId = certDocManual.GuidId
```

- [ ] **Step 10: Build and run existing tests**

Run: `dotnet build ZynstormECFPlatform.Web.Api -nologo -v q`
Expected: `Build succeeded.`
Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~Certification"`
Expected: all pass (6 existing + 11 from Task 1).

- [ ] **Step 11: Review the diff for full-run regressions**

Run: `git diff ZynstormECFPlatform.Services/Certification/OldSimulation/OldCertificationSimulationService.cs`
Check: with `resendGroup == null && resendDocumentGuid == null`, `RowSelected` is always true, `RowCount` returns `item.Count`, `manualCount == item.Count`, the delete-all branch runs, notes keep `if (accepted31Pool.Count <= poolIndex) continue;`, and every document is persisted (`docToReplace == null`).

- [ ] **Step 12: Commit**

```bash
git add ZynstormECFPlatform.Services/Certification/OldSimulation/OldCertificationSimulationService.cs ZynstormECFPlatform.Services/Certification/OldSimulation/IOldCertificationSimulationService.cs
git commit -m "feat(certificacion): reenviar un grupo o un comprobante de la simulación sin reiniciarla"
```

---

### Task 4: Endpoint `POST simulation/resend`

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs` (after `StartBusinessSimulation`)

**Interfaces:**
- Consumes: `oldSimulationService.EnqueueResendJobAsync(...)` (Task 3), `ResendSimulationRequestDto` (Task 2).
- Produces: `POST v1/Certification/simulation/resend` → `200 { jobId, message }`, `400 { message }`, `409 { message }`.

- [ ] **Step 1: Add the action** right after the `StartBusinessSimulation` method:

```csharp
    [HttpPost("simulation/resend")]
    public async Task<ActionResult> ResendSimulation([FromBody] ResendSimulationRequestDto dto)
    {
        try
        {
            var jobId = await oldSimulationService.EnqueueResendJobAsync(dto.BusinessTypeGuidId, dto.ClientGuidId, dto.Group, dto.DocumentGuidId, env.WebRootPath);
            return Ok(new { JobId = jobId, Message = "Reenvío de simulación iniciado." });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
```

- [ ] **Step 2: Build**

Run: `dotnet build ZynstormECFPlatform.Web.Api -nologo -v q`
Expected: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs
git commit -m "feat(certificacion): endpoint para reenviar grupo o comprobante de la simulación"
```

---

### Task 5: Frontend — botones de reenvío

Repo: `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`. Primero: `git checkout -b feature/simulacion-reenvio Staging`.

**Files:**
- Modify: `services/certification.service.ts`
- Modify: `app/certificacion/page.tsx`

**Interfaces:**
- Consumes: `POST ${CERTIFICATION_URL}/simulation/resend` (Task 4); `documentGuidId` en cada log/resultado (Task 2/3).
- Produces: `resendSimulation(businessTypeGuidId, clientGuidId, target: { group?: string; documentGuidId?: string }, token?) => Promise<{ jobId: string; message: string }>`.

- [ ] **Step 1: Type + API client.** In `services/certification.service.ts`, add to `EcfDataTestStatus` (after `buyerRnc?: string`):

```ts
  buyerRnc?: string
  documentGuidId?: string | null
```

and after `startBusinessSimulation`, add:

```ts
export const resendSimulation = async (
  businessTypeGuidId: string,
  clientGuidId: string,
  target: { group?: string; documentGuidId?: string },
  token?: string | null,
): Promise<{ jobId: string; message: string }> => {
  const headers: Record<string, string> = { "Content-Type": "application/json" }
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${CERTIFICATION_URL}/simulation/resend`, {
    method: "POST",
    credentials: "include",
    headers,
    body: JSON.stringify({ businessTypeGuidId, clientGuidId, ...target }),
  })

  if (!response.ok) {
    let message = "No se pudo iniciar el reenvío."
    try {
      const body = await response.json()
      message = body?.message ?? body?.Message ?? message
    } catch {
      // respuesta sin JSON
    }
    throw new Error(message)
  }

  return response.json()
}
```

- [ ] **Step 2: Import and state in `page.tsx`.** Add `resendSimulation` to the import block that already imports `startBusinessSimulation`:

```ts
import { 
  getBusinessTypes, 
  startBusinessSimulation, 
  resendSimulation,
  getLastSimulationResults,
  BusinessType,
  EcfDataTestStatus as SimulationTestStatus,
} from "@/services/certification.service"
```

Add `RotateCcw` to the `lucide-react` import list (next to `Eye`). Next to `const [isStartingSimulation, setIsStartingSimulation] = useState(false)`:

```ts
  const [isResendingSimulation, setIsResendingSimulation] = useState(false)
  const resendJobRef = useRef<string | null>(null)
```

(Add `useRef` to the `react` import if it is not imported yet.)

- [ ] **Step 3: Handler.** After `handleStartBusinessSimulation`:

```ts
  const simulationRunning = isStartingSimulation || isResendingSimulation ||
    ["Pending", "Processing"].includes(simulationProgress.status)

  const handleResendSimulation = async (target: { group?: string; documentGuidId?: string }, label: string) => {
    if (!selectedClient || !selectedBusinessType || simulationRunning) return
    if (!window.confirm(`¿Reenviar ${label} a la DGII? Se reemplazarán los registros anteriores de ${target.group ? "ese tipo" : "ese comprobante"}.`)) return
    try {
      setIsResendingSimulation(true)
      const response = await resendSimulation(selectedBusinessType, selectedClient, target, token)
      resendJobRef.current = response.jobId
      setSimulationProgress((prev) => ({ ...prev, jobId: response.jobId, status: "Processing", errorMessage: "" }))
      setSimulationJobId(response.jobId)
    } catch (err) {
      setIsResendingSimulation(false)
      alert(err instanceof Error ? err.message : "No se pudo iniciar el reenvío.")
    }
  }
```

- [ ] **Step 4: Reload the history when a resend finishes.** In the `onUpdate("simulation", ...)` handler, right after the `if (payload.jobId && payload.jobId !== simulationJobId) return` line:

```ts
      const terminal = ["Completed", "CompletedWithErrors", "Failed"].includes(payload.status)
      if (terminal && resendJobRef.current === payload.jobId) {
        resendJobRef.current = null
        setIsResendingSimulation(false)
        if (payload.errorMessage) alert(payload.errorMessage)
        if (selectedClient) void loadLastSimulation(selectedClient)
        return
      }
```

and in both branches of `setSimulationTests((prev) => ...)` (update and push), after `buyerRnc: log.buyerRnc`:

```ts
                buyerRnc: log.buyerRnc,
                documentGuidId: log.documentGuidId
```

- [ ] **Step 5: Card button.** In the cards array, add a `group` to each item (Manual gets `null`):

```ts
                                { label: "31", group: "31", current: ..., desc: "Factura de Crédito Fiscal" },
                                { label: "33", group: "33", ... },
                                { label: "41", group: "41", ... },
                                { label: "44", group: "44", ... },
                                { label: "46", group: "46", ... },
                                { label: "32 RFCE", group: "32-RFCE", ... },
                                { label: "32 >= 250k", group: "32-250K", ... },
                                { label: "32 Manual", group: null, ... },
                                { label: "34", group: "34", ... },
                                { label: "43", group: "43", ... },
                                { label: "45", group: "45", ... },
                                { label: "47", group: "47", ... },
```

(keep each item's existing `current`, `total` and `desc` exactly; only insert `group`.) Then, right after the `<p ... title={item.desc}>{item.desc}</p>` element of the card:

```tsx
                                    {item.group && (simulationProgress.completedSteps?.length ?? 0) > 0 && (
                                      <Button
                                        variant="ghost"
                                        size="sm"
                                        className="mt-2 h-7 justify-start px-2 text-xs text-primary"
                                        disabled={simulationRunning}
                                        onClick={() => handleResendSimulation({ group: item.group! }, item.desc)}
                                        title={`Reenviar ${item.desc}`}
                                      >
                                        <RotateCcw className="mr-1 h-3 w-3" /> Reenviar
                                      </Button>
                                    )}
```

- [ ] **Step 6: Row button.** In the history table, inside the Acción cell's fragment, after the QR `Button` block:

```tsx
                                                {test.documentGuidId && !getSimulationStatusFlags(test.status ?? test.message).generatedManual && (
                                                  <Button
                                                    variant="ghost"
                                                    size="icon"
                                                    className="h-8 w-8 text-amber-600 hover:text-amber-700 hover:bg-amber-50"
                                                    disabled={simulationRunning}
                                                    onClick={() => handleResendSimulation({ documentGuidId: test.documentGuidId! }, test.ncf ?? "el comprobante")}
                                                    title="Reenviar este comprobante"
                                                  >
                                                    <RotateCcw className="h-4 w-4" />
                                                  </Button>
                                                )}
```

- [ ] **Step 7: Type-check and lint**

Run: `npx tsc --noEmit -p .`
Expected: no new errors in `app/certificacion/page.tsx` or `services/certification.service.ts` (compare against `git stash; npx tsc --noEmit -p .; git stash pop` if the repo already has errors).
Run: `npx eslint app/certificacion/page.tsx services/certification.service.ts`
Expected: no new errors.

- [ ] **Step 8: Commit**

```bash
git add services/certification.service.ts app/certificacion/page.tsx
git commit -m "feat(certificacion): reenviar tipo o comprobante en la simulación (paso 4)"
```

---

### Task 6: Verificación en staging (manual, con el usuario)

Requiere desplegar ambas ramas en staging (el push lo hace el usuario con su cuenta).

- [ ] **Step 1:** Simulación completa con "Iniciar Simulación de Negocio": mismo resultado que antes (29 pasos, tarjetas completas).
- [ ] **Step 2:** "Reenviar" en Notas de Crédito (34): solo se envían 2 comprobantes; las demás tarjetas no cambian; el historial muestra las 34 nuevas y no las viejas.
- [ ] **Step 3:** "Reenviar" en Consumo (Resumen): 4 RFCE + 4 manuales nuevos; los manuales viejos desaparecen.
- [ ] **Step 4:** Ícono reenviar en una fila 31: se envía 1; al aceptarse reemplaza la fila; contador 31 sigue en 4/4.
- [ ] **Step 5:** Con una simulación en curso, el botón Reenviar está deshabilitado y un `POST simulation/resend` directo devuelve `409`.
