# Representación Impresa (RI) desde PDF modelo — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir subir un PDF modelo por cliente (Paso 5), extraer su layout con PdfPig y generar la Representación Impresa (con QR) de los comprobantes del Paso 4 con QuestPDF, con vista previa y descarga individual/ZIP.

**Architecture:** Pipeline en `ZynstormECFPlatform.Services/Ri/`: `RiModelExtractor` (PDF→`LayoutDescriptor` JSON) al subir; en generación, `EcfRiDataMapper` (XML firmado→`RiData`) + `RiPdfRenderer` (`LayoutDescriptor`+`RiData`→bytes PDF, QR incluido). Un `CertificationRiModelService` orquesta; `CertificationController` expone endpoints; el frontend agrega el bloque `currentStep === 5`.

**Tech Stack:** .NET 10, EF Core 10, QuestPDF 2026.5.0 (ya referenciado en Services), **UglyToad.PdfPig** (nuevo), **QRCoder** (nuevo), Next.js 16 / React 19 (frontend).

## Global Constraints

- Target `net10.0`. Nullable habilitado (los proyectos ya lo usan).
- QuestPDF: fijar `QuestPDF.Settings.License = LicenseType.Community;` en un `static` ctor (patrón de `ReportPdfGenerator`).
- Coordenadas del `LayoutDescriptor` **normalizadas 0..1** relativas a la página.
- El QR se construye SIEMPRE con `EcfQrUrlBuilder.Build(...)` (nunca duplicar la lógica). Ambiente `DgiiEnvironment.CerteCF` para las RI de certificación.
- El **PDF fuente NO se persiste**; por modelo se guarda `LayoutJson` + una **RI de referencia** en `FileData`.
- Regla: para un `(ClientId, EcfTypeId)` existe **a lo sumo un** modelo; asignar un tipo a un modelo nuevo lo reasigna.
- Generar una RI requiere que el modelo del tipo esté en estado `Confirmed`.
- Commits frecuentes; TDD; reutilizar patrones existentes (servicios en `ZynstormECFPlatform.Services`, interfaces en `ZynstormECFPlatform.Abstractions/Services`, tests en `tests/EcfTest`).

---

## File Structure

**Backend**
- `ZynstormECFPlatform.Common/Ecf/EcfQrUrlBuilder.cs` — *crear*. Constructor de URL de timbre (portado de `ReceivedEcfProductionService.BuildQrUrl`).
- `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` — *modificar*. Delegar en `EcfQrUrlBuilder`.
- `ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplate.cs` — *modificar* (RI de referencia en FileData, LayoutJson, Status, warnings; quitar EcfTypeId único).
- `ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplateEcfType.cs` — *crear* (join N↔N).
- `ZynstormECFPlatform.Core/Entities/CertificationRiTemplateStatus.cs` — *crear* (enum).
- `ZynstormECFPlatform.Data/StorageContext.cs` — *modificar* (config de entidad + join) + **migración**.
- `ZynstormECFPlatform.Services/Ri/LayoutDescriptor.cs` — *crear* (modelo del LayoutJson).
- `ZynstormECFPlatform.Services/Ri/RiData.cs` — *crear* (datos para render).
- `ZynstormECFPlatform.Services/Ri/EcfRiDataMapper.cs` — *crear* (XML→RiData).
- `ZynstormECFPlatform.Services/Ri/RiModelExtractor.cs` — *crear* (PdfPig→LayoutDescriptor).
- `ZynstormECFPlatform.Services/Ri/RiPdfRenderer.cs` — *crear* (QuestPDF+QRCoder).
- `ZynstormECFPlatform.Abstractions/Services/ICertificationRiModelService.cs` — *crear*.
- `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs` — *crear* (orquestación).
- `ZynstormECFPlatform.Dtos/Ri/*.cs` — *crear* (DTOs de request/response).
- `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs` — *modificar* (endpoints).
- Registro DI (donde se registran los servicios de certificación) — *modificar*.
- `ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj` — *modificar* (paquetes).

**Tests** (`tests/EcfTest/`)
- `Ri/EcfQrUrlBuilderTests.cs`, `Ri/EcfRiDataMapperTests.cs`, `Ri/RiModelExtractorTests.cs`, `Ri/RiPdfRendererTests.cs`, `Ri/Fixtures/` (PDFs y XML de ejemplo).

**Frontend**
- `services/certification.service.ts` — *modificar* (funciones nuevas).
- `components/certification/ri-step5.tsx` — *crear* (bloque Paso 5).
- `app/certificacion/page.tsx` — *modificar* (render `currentStep === 5`).

---

## Task 1: Paquetes NuGet

**Files:**
- Modify: `ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj`

**Interfaces:**
- Produces: disponibilidad de `UglyToad.PdfPig` y `QRCoder` en el proyecto Services.

- [ ] **Step 1: Agregar PackageReferences**

En el `<ItemGroup>` de paquetes de `ZynstormECFPlatform.Services.csproj`, añadir:

```xml
<PackageReference Include="UglyToad.PdfPig" Version="0.1.11" />
<PackageReference Include="QRCoder" Version="1.6.0" />
```

- [ ] **Step 2: Restaurar y compilar**

Run: `dotnet build ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj -nologo`
Expected: `Build succeeded` (0 errores).

- [ ] **Step 3: Commit**

```bash
git add ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj
git commit -m "chore(ri): agregar PdfPig y QRCoder al proyecto Services"
```

---

## Task 2: `EcfQrUrlBuilder` compartido (refactor del QR)

**Files:**
- Create: `ZynstormECFPlatform.Common/Ecf/EcfQrUrlBuilder.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (método `BuildQrUrl` privado → delegar)
- Test: `tests/EcfTest/Ri/EcfQrUrlBuilderTests.cs`

**Interfaces:**
- Produces:
  - `EcfQrUrlBuilder.Build(DgiiEnvironment environment, int ecfType, string rncEmisorRaw, string rncCompradorRaw, string encf, string fechaEmision, decimal montoTotal, string fechaFirma, string securityCode) : string`
  - `EcfQrUrlBuilder.OnlyDigits(string?) : string`

- [ ] **Step 1: Escribir el test que falla**

`tests/EcfTest/Ri/EcfQrUrlBuilderTests.cs`:

```csharp
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Ecf;
using Xunit;

public class EcfQrUrlBuilderTests
{
    [Fact]
    public void E32_Under250k_UsesFcPortal_MontoSinCerosSobrantes()
    {
        var url = EcfQrUrlBuilder.Build(DgiiEnvironment.CerteCF, 32, "1-32-29389-4", "", "E320000000028",
            "30-06-2026", 1180.00m, "30-06-2026 18:34:41", "N4J8CY");
        Assert.Equal(
            "https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000028&MontoTotal=1180&CodigoSeguridad=N4J8CY",
            url);
    }

    [Fact]
    public void E31_UsesRegularPortal_ConComprador()
    {
        var url = EcfQrUrlBuilder.Build(DgiiEnvironment.CerteCF, 31, "132293894", "131880681", "E310000000001",
            "30-06-2026", 6029.5m, "30-06-2026 10:00:00", "AbC123");
        Assert.StartsWith("https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=132293894&RncComprador=131880681", url);
        Assert.Contains("MontoTotal=6029.5", url);
        Assert.Contains("FechaFirma=30-06-2026%2010:00:00", url);
    }
}
```

- [ ] **Step 2: Verificar que falla**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter EcfQrUrlBuilderTests`
Expected: FAIL de compilación ("EcfQrUrlBuilder does not exist").

- [ ] **Step 3: Crear `EcfQrUrlBuilder`**

`ZynstormECFPlatform.Common/Ecf/EcfQrUrlBuilder.cs` — copiar EXACTAMENTE la lógica actual de `ReceivedEcfProductionService.BuildQrUrl` (líneas ~831-885) a un método `Build` público estático, y `OnlyDigits`:

```csharp
using System.Globalization;

namespace ZynstormECFPlatform.Common.Ecf;

public static class EcfQrUrlBuilder
{
    public static string Build(
        DgiiEnvironment environment, int ecfType, string rncEmisorRaw, string rncCompradorRaw,
        string encf, string fechaEmision, decimal montoTotal, string fechaFirma, string securityCode)
    {
        var rncEmisor = OnlyDigits(rncEmisorRaw);
        var rncComprador = OnlyDigits(rncCompradorRaw);
        var fechaEmisionUrl = !string.IsNullOrWhiteSpace(fechaEmision)
            ? fechaEmision.Split(' ')[0].Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy");
        var fechaFirmaUrl = (!string.IsNullOrWhiteSpace(fechaFirma)
            ? fechaFirma.Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")).Replace(" ", "%20");
        var montoTotalUrl = montoTotal.ToString("0.##", CultureInfo.InvariantCulture);

        if (ecfType == 32 && montoTotal < 250000m)
        {
            string fcBase = environment == DgiiEnvironment.Production
                ? "https://fc.dgii.gov.do/ecf"
                : environment == DgiiEnvironment.Test
                    ? "https://fc.dgii.gov.do/testecf"
                    : "https://fc.dgii.gov.do/CerteCF";
            return $"{fcBase}/ConsultaTimbreFC?RncEmisor={rncEmisor}&ENCF={encf}&MontoTotal={montoTotalUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
        }

        string baseUrl = environment == DgiiEnvironment.Production
            ? "https://ecf.dgii.gov.do/ecf"
            : environment == DgiiEnvironment.Test
                ? "https://ecf.dgii.gov.do/TesteCF"
                : "https://ecf.dgii.gov.do/CerteCF";

        if (string.IsNullOrEmpty(rncComprador))
            return $"{baseUrl}/ConsultaTimbre?RncEmisor={rncEmisor}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";

        return $"{baseUrl}/ConsultaTimbre?RncEmisor={rncEmisor}&RncComprador={rncComprador}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
    }

    public static string OnlyDigits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
}
```

> Nota: `DgiiEnvironment` vive en `ZynstormECFPlatform.Common`. Verificar el `using` correcto (buscar `enum DgiiEnvironment`).

- [ ] **Step 4: Delegar en `ReceivedEcfProductionService`**

En `ReceivedEcfProductionService.cs`, reemplazar el cuerpo del `BuildQrUrl` privado por:

```csharp
private static string BuildQrUrl(
    DgiiEnvironment environment, int ecfType, string rncEmisorRaw, string rncCompradorRaw,
    string encf, string fechaEmision, decimal montoTotal, string fechaFirma, string securityCode)
    => ZynstormECFPlatform.Common.Ecf.EcfQrUrlBuilder.Build(
        environment, ecfType, rncEmisorRaw, rncCompradorRaw, encf, fechaEmision, montoTotal, fechaFirma, securityCode);
```

(Si `OnlyDigits` local queda sin uso tras esto, dejarlo; otros métodos lo usan.)

- [ ] **Step 5: Verificar que pasa y no rompe producción**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter EcfQrUrlBuilderTests`
Expected: PASS.
Run: `dotnet build ZynstormECFPlatform.slnx -nologo`
Expected: `Build succeeded`.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Common/Ecf/EcfQrUrlBuilder.cs ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs tests/EcfTest/Ri/EcfQrUrlBuilderTests.cs
git commit -m "refactor(qr): extraer BuildQrUrl a EcfQrUrlBuilder compartido + tests"
```

---

## Task 3: Modelo de datos (entidad + join + enum)

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplate.cs`
- Create: `ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplateEcfType.cs`
- Create: `ZynstormECFPlatform.Core/Entities/CertificationRiTemplateStatus.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/EcfType.cs` (nav al join)

**Interfaces:**
- Produces: entidades `CertificationInvoicePrintTemplate` (con `LayoutJson`, `Status`, `ExtractionWarnings`, `FileData`, colección `EcfTypes`), `CertificationInvoicePrintTemplateEcfType`, enum `CertificationRiTemplateStatus`.

- [ ] **Step 1: Enum de estado**

`CertificationRiTemplateStatus.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public enum CertificationRiTemplateStatus
{
    PendingExtraction = 0,
    Extracted = 1,
    Confirmed = 2,
    Failed = 3
}
```

- [ ] **Step 2: Join entity**

`CertificationInvoicePrintTemplateEcfType.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public class CertificationInvoicePrintTemplateEcfType
{
    public int CertificationInvoicePrintTemplateEcfTypeId { get; set; }
    public int CertificationInvoicePrintTemplateId { get; set; }
    public int EcfTypeId { get; set; }

    public virtual CertificationInvoicePrintTemplate Template { get; set; } = null!;
    public virtual EcfType EcfType { get; set; } = null!;
}
```

- [ ] **Step 3: Refactor de la entidad principal**

Reemplazar `CertificationInvoicePrintTemplate.cs` por:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public class CertificationInvoicePrintTemplate : BaseEntity
{
    public int CertificationInvoicePrintTemplateId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public int ClientId { get; set; }

    // RI de referencia generada (PDF). El PDF fuente NO se persiste.
    public byte[]? FileData { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/pdf";

    // Descriptor de layout extraído (JSON) — se usa para renderizar cada RI.
    public string? LayoutJson { get; set; }

    public CertificationRiTemplateStatus Status { get; set; } = CertificationRiTemplateStatus.PendingExtraction;

    // Lista JSON de anclas no encontradas / avisos de extracción.
    public string? ExtractionWarnings { get; set; }

    public virtual Client Client { get; set; } = null!;
    public virtual ICollection<CertificationInvoicePrintTemplateEcfType> EcfTypes { get; set; } = [];
}
```

- [ ] **Step 4: Nav en EcfType**

En `EcfType.cs`, cambiar la colección existente por el join:

```csharp
public virtual ICollection<CertificationInvoicePrintTemplateEcfType> CertificationInvoicePrintTemplates { get; set; } = [];
```

- [ ] **Step 5: Compilar Core**

Run: `dotnet build ZynstormECFPlatform.Core/ZynstormECFPlatform.Core.csproj -nologo`
Expected: `Build succeeded` (puede fallar `StorageContext`/`Client` hasta Task 4 — solo compilar Core aquí).

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplate.cs ZynstormECFPlatform.Core/Entities/CertificationInvoicePrintTemplateEcfType.cs ZynstormECFPlatform.Core/Entities/CertificationRiTemplateStatus.cs ZynstormECFPlatform.Core/Entities/EcfType.cs
git commit -m "feat(ri): modelo de datos de plantilla RI (LayoutJson + RI referencia + join tipos)"
```

---

## Task 4: EF config + migración

**Files:**
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs`

**Interfaces:**
- Consumes: entidades de Task 3.
- Produces: `DbSet<CertificationInvoicePrintTemplateEcfType>`; esquema migrado.

- [ ] **Step 1: Reemplazar la config de la entidad**

En `StorageContext.cs`, sustituir el bloque `modelBuilder.Entity<CertificationInvoicePrintTemplate>(...)` (líneas ~1595-1658) por uno que: quite `FileUrl` y la relación única con `EcfType`; agregue `FileData`, `LayoutJson`, `ExtractionWarnings` (nvarchar max), `Status` (int); y configure el join:

```csharp
modelBuilder.Entity<CertificationInvoicePrintTemplate>(entity =>
{
    entity.HasKey(c => c.CertificationInvoicePrintTemplateId);
    entity.Property(e => e.Name).HasMaxLength(100).IsUnicode(false).IsRequired();
    entity.Property(e => e.Description).HasMaxLength(250).IsUnicode(false);
    entity.Property(e => e.FileName).HasMaxLength(100).IsUnicode(false).IsRequired();
    entity.Property(e => e.ContentType).HasMaxLength(50).IsUnicode(false).HasDefaultValue("application/pdf").IsRequired();
    entity.Property(e => e.LayoutJson);            // nvarchar(max)
    entity.Property(e => e.ExtractionWarnings);    // nvarchar(max)
    entity.Property(e => e.Status).HasConversion<int>().HasDefaultValue(CertificationRiTemplateStatus.PendingExtraction);

    entity.Property(c => c.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
    entity.Property(c => c.LastUpdateUtc).HasColumnType(DateTimeColumnType);
    entity.Property(c => c.DeletedTimeUtc).HasColumnType(DateTimeColumnType);
    entity.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
    entity.Property(e => e.GuidId).IsRequired().HasMaxLength(450).IsUnicode(false).HasDefaultValueSql(DefaultGUIDSqlValue);
    entity.HasQueryFilter(c => !c.IsDeleted);

    entity.HasOne(d => d.Client)
          .WithMany(p => p.CertificationInvoicePrintTemplates)
          .HasForeignKey(d => d.ClientId)
          .OnDelete(DeleteBehavior.ClientSetNull)
          .HasConstraintName("FK_CertificationInvoicePrintTemplate_Client");
});

modelBuilder.Entity<CertificationInvoicePrintTemplateEcfType>(entity =>
{
    entity.HasKey(c => c.CertificationInvoicePrintTemplateEcfTypeId);
    entity.HasIndex(c => new { c.CertificationInvoicePrintTemplateId, c.EcfTypeId }).IsUnique();
    entity.HasOne(d => d.Template)
          .WithMany(p => p.EcfTypes)
          .HasForeignKey(d => d.CertificationInvoicePrintTemplateId)
          .OnDelete(DeleteBehavior.Cascade);
    entity.HasOne(d => d.EcfType)
          .WithMany(p => p.CertificationInvoicePrintTemplates)
          .HasForeignKey(d => d.EcfTypeId)
          .OnDelete(DeleteBehavior.Restrict);
});
```

Agregar el `DbSet`:

```csharp
public DbSet<CertificationInvoicePrintTemplateEcfType> CertificationInvoicePrintTemplateEcfTypes { get; set; }
```

- [ ] **Step 2: Compilar solución**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo`
Expected: `Build succeeded`.

- [ ] **Step 3: Crear la migración**

Run (desde la raíz del repo, ajustar proyecto de arranque/DbContext según config existente):
`dotnet ef migrations add RiPrintTemplateRefactor --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: genera `..._RiPrintTemplateRefactor.cs`. Revisar que dropee `FileUrl`/`EcfTypeId` y cree la tabla del join.

- [ ] **Step 4: Aplicar y verificar**

Run: `dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: aplica sin error.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Data/StorageContext.cs ZynstormECFPlatform.Data/Migrations/*RiPrintTemplateRefactor*
git commit -m "feat(ri): EF config + migracion de plantilla RI y join de tipos"
```

---

## Task 5: `LayoutDescriptor` y `RiData`

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/LayoutDescriptor.cs`
- Create: `ZynstormECFPlatform.Services/Ri/RiData.cs`

**Interfaces:**
- Produces:
  - `LayoutDescriptor` con: `PageInfo Page`, `Dictionary<string,string> Palette`, `LogoSlot? Logo`, `Dictionary<string,FieldSlot> FieldSlots`, `ItemsTable Items`, `List<TotalRow> Totals`, `QrSlot Qr`, `List<FixedText> FixedTexts`, `Dictionary<string,string> Images`.
  - Tipos anidados: `PageInfo{double WidthPt,HeightPt,Margin}`, `FieldSlot{double X,Y; string? Label}`, `ItemsTable{double TopY; List<ItemColumn> Columns}`, `ItemColumn{string Field; double X,W; string Align}`, `TotalRow{string Field,Label}`, `QrSlot{double X,Y,Size}`, `FixedText{string Text; double X,Y; string Align; double? FontSize}`, `LogoSlot{double X,Y,W,H; string ImageRef}`.
  - `RiData` con: `Party Issuer`, `Party Buyer`, `string ENcf, TipoeCF, FechaEmision, FechaFirma, SecurityCode, QrUrl`, `List<RiItem> Items`, `RiTotals Totals`. `Party{Name,Document,Address,Phone,Email?,Country?}`, `RiItem{Description; decimal Quantity,Price,Itbis,Amount}`, `RiTotals{decimal SubTotal,Itbis,Exento,Gravado,Total}`.

- [ ] **Step 1: Crear `LayoutDescriptor.cs`** con las clases anteriores (POCOs con getters/setters, defaults `= new()`/`= []` donde aplique). Serializable con `System.Text.Json`.

- [ ] **Step 2: Crear `RiData.cs`** con las clases anteriores.

- [ ] **Step 3: Compilar**

Run: `dotnet build ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj -nologo`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/LayoutDescriptor.cs ZynstormECFPlatform.Services/Ri/RiData.cs
git commit -m "feat(ri): modelos LayoutDescriptor y RiData"
```

---

## Task 6: `EcfRiDataMapper` (XML firmado → RiData)

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/EcfRiDataMapper.cs`
- Test: `tests/EcfTest/Ri/EcfRiDataMapperTests.cs`, `tests/EcfTest/Ri/Fixtures/*.xml`

**Interfaces:**
- Consumes: `RiData` (Task 5), `EcfQrUrlBuilder` (Task 2).
- Produces: `EcfRiDataMapper.Map(string signedXml, DgiiEnvironment environment) : RiData`.

**Porting note:** portar la lógica de `MechanicalServ_Api/MechanicalServ.Web.Api/Controllers/PreviewController.cs` — métodos `BuildPreviewModel`, `First/Value/DecimalValue`, `BuildItems`, `GetSecurityCode` — adaptando a `RiData`. La URL del QR se obtiene con `EcfQrUrlBuilder.Build(...)` (NO portar el `BuildQrUrl` de Mechanic). El `securityCode` = `CodigoSeguridadeCF ?? SignatureValue[..6]` (igual que `GetSecurityCode` de Mechanic).

- [ ] **Step 1: Fixtures** — copiar a `tests/EcfTest/Ri/Fixtures/` los XML reales existentes: uno E32<250k (root `<ECF>` sin CodigoSeguridadeCF, ej. `SUBIR_DGII_Paso_29_E320000000028.xml`), uno E32<250k RFCE (con `<CodigoSeguridadeCF>`, ej. `Paso_25_E320000000344.xml`) y uno E31 (`Paso_1_E310000000402.xml`) desde `ZynstormECFPlatform.Schemas/Xml/`.

- [ ] **Step 2: Test que falla**

`EcfRiDataMapperTests.cs`:

```csharp
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

public class EcfRiDataMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void E32_FullEcf_UsesSignatureValuePrefix_AndFcPortal()
    {
        var data = EcfRiDataMapper.Map(Load("E320000000028.xml"), DgiiEnvironment.CerteCF);
        Assert.Equal("E320000000028", data.ENcf);
        Assert.Equal(6, data.SecurityCode.Length);
        Assert.Contains("fc.dgii.gov.do/CerteCF/ConsultaTimbreFC", data.QrUrl);
        Assert.Contains($"CodigoSeguridad={data.SecurityCode}", data.QrUrl);
    }

    [Fact]
    public void Rfce_UsesCodigoSeguridadeCF()
    {
        var data = EcfRiDataMapper.Map(Load("E320000000344RFCE.xml"), DgiiEnvironment.CerteCF);
        Assert.Equal("QuGoOA", data.SecurityCode); // <CodigoSeguridadeCF> del fixture
    }
}
```

(Ajustar los nombres de fixture y el valor esperado `QuGoOA` al contenido real del XML copiado.)

- [ ] **Step 3: Verificar que falla**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter EcfRiDataMapperTests`
Expected: FAIL (no compila / mapper inexistente).

- [ ] **Step 4: Implementar `EcfRiDataMapper`** portando de `PreviewController`. Firma:

```csharp
using System.Xml.Linq;
using System.Globalization;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Ecf;

namespace ZynstormECFPlatform.Services.Ri;

public static class EcfRiDataMapper
{
    public static RiData Map(string signedXml, DgiiEnvironment environment)
    {
        var doc = XDocument.Parse(signedXml);
        // ... portar BuildPreviewModel: emisor, comprador, items, totales, fechas ...
        var securityCode = GetSecurityCode(doc);
        var tipoeCF = Value(First(doc, "IdDoc"), "TipoeCF");
        var montoTotal = DecimalValue(First(doc, "Totales"), "MontoTotal");
        var qrUrl = EcfQrUrlBuilder.Build(environment, int.Parse(tipoeCF),
            /*rncEmisor*/..., /*rncComprador*/..., /*eNCF*/..., /*fechaEmision*/..., montoTotal, /*fechaFirma*/..., securityCode);
        // return new RiData { ... };
    }

    private static string GetSecurityCode(XDocument doc)
    {
        var cod = First(doc, "CodigoSeguridadeCF")?.Value?.Trim();
        if (!string.IsNullOrEmpty(cod)) return cod;
        var sig = First(doc, "SignatureValue")?.Value?.Replace("\n","").Replace("\r","").Replace(" ","").Trim() ?? "";
        return sig.Length >= 6 ? sig[..6] : sig;
    }
    // helpers First/Value/DecimalValue/BuildItems portados de PreviewController
}
```

- [ ] **Step 5: Verificar que pasa**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter EcfRiDataMapperTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/EcfRiDataMapper.cs tests/EcfTest/Ri/EcfRiDataMapperTests.cs tests/EcfTest/Ri/Fixtures/
git commit -m "feat(ri): EcfRiDataMapper (XML firmado -> RiData con QR canonico) + tests"
```

---

## Task 7: `RiModelExtractor` (PdfPig → LayoutDescriptor)

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/RiModelExtractor.cs`
- Test: `tests/EcfTest/Ri/RiModelExtractorTests.cs`, `tests/EcfTest/Ri/Fixtures/modelo_ok.pdf`, `modelo_sin_texto.pdf`

**Interfaces:**
- Consumes: `LayoutDescriptor` (Task 5).
- Produces: `RiModelExtractor.Extract(byte[] pdfBytes) : RiExtractionResult` donde `RiExtractionResult{LayoutDescriptor? Layout; List<string> Warnings; bool Success}`.

**Anchor dictionary (normalizado, case/acento-insensible):** emisor `RNC`; documento `NCF`/`E-NCF`/`NCF ELECTRONICO`; `FECHA`; comprador `CLIENTE`/`RAZON SOCIAL`/`RNC/CEDULA`; tabla `DESCRIPCION`,`CANTIDAD`,`PRECIO`,`ITBIS`,`VALOR`/`IMPORTE`; totales `SUB-TOTAL`/`SUBTOTAL`,`ITBIS`,`TOTAL`; `CODIGO DE SEGURIDAD`; `FECHA DE FIRMA`.

- [ ] **Step 1: Fixtures** — generar `modelo_ok.pdf` (una factura simple con las etiquetas del diccionario y una tabla; puede generarse con QuestPDF en un pequeño script o incluir un PDF real de ejemplo) y `modelo_sin_texto.pdf` (imagen escaneada sin texto extraíble).

- [ ] **Step 2: Test que falla**

`RiModelExtractorTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Ri;
using Xunit;

public class RiModelExtractorTests
{
    private static byte[] Load(string name) =>
        File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void KnownFormat_DetectsColumnsAndFieldSlots()
    {
        var r = RiModelExtractor.Extract(Load("modelo_ok.pdf"));
        Assert.True(r.Success);
        Assert.NotNull(r.Layout);
        Assert.Contains("eNCF", r.Layout!.FieldSlots.Keys);
        Assert.True(r.Layout.Items.Columns.Count >= 3);
    }

    [Fact]
    public void PdfSinTexto_MarcaFailed_ConWarning()
    {
        var r = RiModelExtractor.Extract(Load("modelo_sin_texto.pdf"));
        Assert.False(r.Success);
        Assert.NotEmpty(r.Warnings);
    }
}
```

- [ ] **Step 3: Verificar que falla**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter RiModelExtractorTests`
Expected: FAIL.

- [ ] **Step 4: Implementar `RiModelExtractor`** con PdfPig:

```csharp
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ZynstormECFPlatform.Services.Ri;

public record RiExtractionResult(LayoutDescriptor? Layout, List<string> Warnings, bool Success);

public static class RiModelExtractor
{
    public static RiExtractionResult Extract(byte[] pdfBytes)
    {
        var warnings = new List<string>();
        using var pdf = PdfDocument.Open(pdfBytes);
        var page = pdf.GetPage(1);
        var words = page.GetWords().ToList();
        if (words.Count == 0)
            return new RiExtractionResult(null, new() { "El PDF no contiene texto extraible (posible escaneo/imagen). Usa un PDF con texto seleccionable." }, false);

        double W = page.Width, H = page.Height;
        // PdfPig: origen abajo-izq; normalizar Y como (H - top)/H para que 0=arriba.
        double NX(double x) => x / W;
        double NY(double yTop) => (H - yTop) / H;

        var layout = new LayoutDescriptor { Page = new PageInfo { WidthPt = W, HeightPt = H, Margin = 0.04 } };
        // 1) localizar anclas por texto (normalizando acentos/mayusculas)
        // 2) FieldSlots: para cada ancla de campo, slot a la derecha/debajo
        // 3) Items.Columns: X de cada cabecera detectada -> columnas ordenadas por X
        // 4) Logo: page.GetImages() mayor en zona superior -> base64 en Images["logo1"]
        // 5) Palette/fuentes: color/tamaño dominante de words
        // 6) FixedTexts: words no asociadas a anclas/campos (opcional en v1)
        // 7) Qr: si no se detecta region, default {X=0.08,Y=0.80,Size=0.15}
        // Cada ancla faltante -> warnings.Add(...)
        return new RiExtractionResult(layout, warnings, true);
    }
}
```

Implementar los pasos 1-7 con heurísticas de anclaje (comparación `RemoveDiacritics().ToUpperInvariant()`), poblando `layout`. Mantener el método por debajo de ~200 líneas; extraer helpers privados (`FindAnchor`, `NormalizeText`, `BuildColumns`).

- [ ] **Step 5: Verificar que pasa**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter RiModelExtractorTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiModelExtractor.cs tests/EcfTest/Ri/RiModelExtractorTests.cs tests/EcfTest/Ri/Fixtures/modelo_ok.pdf tests/EcfTest/Ri/Fixtures/modelo_sin_texto.pdf
git commit -m "feat(ri): RiModelExtractor con PdfPig (anclas -> LayoutDescriptor) + tests"
```

---

## Task 8: `RiPdfRenderer` (LayoutDescriptor + RiData → PDF con QR)

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/RiPdfRenderer.cs`
- Test: `tests/EcfTest/Ri/RiPdfRendererTests.cs`

**Interfaces:**
- Consumes: `LayoutDescriptor`, `RiData`.
- Produces: `RiPdfRenderer.Render(LayoutDescriptor layout, RiData data) : byte[]`.

**Porting note:** usar `MechanicalServ.PdfCreate/Reports/ElectronicInvoicePdf.cs` como base de composición QuestPDF (header, info doc, comprador, items, totales, QR). Adaptar para que **respete `layout`** (paleta, tamaño de página, columnas de la tabla, posición del QR) y consuma `RiData`. Generar la imagen del QR con QRCoder.

- [ ] **Step 1: Test que falla**

`RiPdfRendererTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Ri;
using Xunit;

public class RiPdfRendererTests
{
    [Fact]
    public void Render_ProducesNonEmptyPdf_WithQr()
    {
        var layout = new LayoutDescriptor(); // defaults
        var data = new RiData {
            ENcf = "E320000000028", TipoeCF = "32", FechaEmision = "30-06-2026",
            SecurityCode = "N4J8CY", QrUrl = "https://fc.dgii.gov.do/CerteCF/ConsultaTimbreFC?...",
            Issuer = new Party { Name = "MULTI SERVICE ICAAYSI SRL", Document = "132293894" },
            Buyer = new Party { Name = "CONSUMIDOR FINAL" },
            Items = new() { new RiItem { Description = "Servicio", Quantity = 1, Price = 1180, Amount = 1180 } },
            Totals = new RiTotals { Total = 1180 }
        };
        var bytes = RiPdfRenderer.Render(layout, data);
        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }
}
```

- [ ] **Step 2: Verificar que falla**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter RiPdfRendererTests`
Expected: FAIL.

- [ ] **Step 3: Implementar `RiPdfRenderer`**

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

namespace ZynstormECFPlatform.Services.Ri;

public static class RiPdfRenderer
{
    static RiPdfRenderer() { QuestPDF.Settings.License = LicenseType.Community; }

    public static byte[] Render(LayoutDescriptor layout, RiData data)
    {
        var qrPng = BuildQrPng(data.QrUrl);
        return Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(PageSizes.Letter); // o desde layout.Page si aplica
                page.Margin((float)(layout.Page.Margin <= 1 ? layout.Page.Margin * 100 : layout.Page.Margin), Unit.Point);
                page.Content().Column(col =>
                {
                    // header (logo/emisor) -> info doc (eNCF, fecha) -> comprador
                    // -> tabla items usando layout.Items.Columns -> totales
                    // -> QR (Image(qrPng)) + Codigo de Seguridad -> textos fijos
                });
            });
        }).GeneratePdf();
    }

    private static byte[] BuildQrPng(string url)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(url ?? string.Empty, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(10);
    }
}
```

Completar la composición portando de `ElectronicInvoicePdf`, respetando `layout` (columnas/paleta/QR). Slots/columnas ausentes → defaults.

- [ ] **Step 4: Verificar que pasa**

Run: `dotnet test tests/EcfTest/EcfTest.csproj --filter RiPdfRendererTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiPdfRenderer.cs tests/EcfTest/Ri/RiPdfRendererTests.cs
git commit -m "feat(ri): RiPdfRenderer con QuestPDF + QR (QRCoder) + test de humo"
```

---

## Task 9: DTOs + `ICertificationRiModelService` + `CertificationRiModelService`

**Files:**
- Create: `ZynstormECFPlatform.Dtos/Ri/RiModelDto.cs`, `RiModelListItemDto.cs`, `SaveRiModelRequestDto.cs`
- Create: `ZynstormECFPlatform.Abstractions/Services/ICertificationRiModelService.cs`
- Create: `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs`

**Interfaces:**
- Consumes: `RiModelExtractor`, `EcfRiDataMapper`, `RiPdfRenderer`, `StorageContext`, `IEcfTypeService`/`IClientService` (según patrón existente), `CertificationDocument` (para `XmlSent` del Paso 4).
- Produces (interfaz):
  - `Task<RiModelDto> UploadAsync(string clientGuidId, string name, IReadOnlyList<string> ecfTypeCodes, byte[] sourcePdf, string fileName)`
  - `Task<RiModelDto> UpdateAsync(string templateGuidId, string? name, IReadOnlyList<string>? ecfTypeCodes, bool? confirm, byte[]? newSourcePdf, string? fileName)`
  - `Task<List<RiModelListItemDto>> ListByClientAsync(string clientGuidId)`
  - `Task<byte[]?> GetReferenceRiAsync(string templateGuidId)`
  - `Task DeleteAsync(string templateGuidId)`
  - `Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf)` (usa el `XmlSent` del `CertificationDocument` + el modelo `Confirmed` del tipo)
  - `Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath)`

- [ ] **Step 1: DTOs** — crear los 3 DTOs (campos: Guid, Name, Description, EcfTypeCodes[], Status, Warnings[], AssignedTypeCodes[], HasReferenceRi).

- [ ] **Step 2: Interfaz** `ICertificationRiModelService` con las firmas de arriba.

- [ ] **Step 3: Servicio** `CertificationRiModelService`:
  - `UploadAsync`: `RiModelExtractor.Extract` → si `!Success`, guarda entidad `Failed` + warnings (sin FileData); si `Success`, serializa `LayoutJson`, genera **RI de referencia** con `RiPdfRenderer.Render(layout, SampleRiData())` → `FileData`, estado `Extracted`; crea filas del join reasignando tipos (quita el tipo de otros modelos del mismo cliente). **No** persiste el PDF fuente.
  - `SampleRiData()`: `RiData` representativo (emisor del cliente, 1-2 ítems ficticios, QR de ejemplo) para la RI de referencia.
  - `UpdateAsync`: renombrar / reasignar tipos / `confirm` (Status=`Confirmed`) y/o re-subir fuente (re-extraer, regenerar LayoutJson + RI referencia).
  - `RenderRiForDocumentAsync`: carga `CertificationDocument` por `ncf` del cliente; resuelve el modelo `Confirmed` del `EcfType`; si no hay → `InvalidOperationException("No hay modelo confirmado para el tipo X")`; `EcfRiDataMapper.Map(xmlSent, CerteCF)` + `RiPdfRenderer.Render(layout, data)`.
  - `RenderAllZipAsync`: itera los `CertificationDocument` del cliente; por cada uno con modelo confirmado, genera RI; arma ZIP en `wwwroot` (patrón de `CreateSimulationZipAsync`); los tipos sin modelo se omiten y se listan en un `_faltantes.txt` dentro del ZIP.

- [ ] **Step 4: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Dtos/Ri/ ZynstormECFPlatform.Abstractions/Services/ICertificationRiModelService.cs ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs
git commit -m "feat(ri): servicio de orquestacion de modelos RI + DTOs"
```

---

## Task 10: Registro DI

**Files:**
- Modify: registro de servicios (buscar dónde se hace `AddScoped<ICertificationSimulationService, ...>` — mismo archivo/extensión).

- [ ] **Step 1: Registrar**

Agregar junto a los demás servicios de certificación:

```csharp
services.AddScoped<ICertificationRiModelService, CertificationRiModelService>();
```

- [ ] **Step 2: Compilar**

Run: `dotnet build ZynstormECFPlatform.Web.Api/ZynstormECFPlatform.Web.Api.csproj -nologo`
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "chore(ri): registrar CertificationRiModelService en DI"
```

---

## Task 11: Endpoints en `CertificationController`

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs`

**Interfaces:**
- Consumes: `ICertificationRiModelService` (Task 9), patrón `IFormFile`/`IWebHostEnvironment` ya usado en el controlador.

- [ ] **Step 1: Inyectar** `ICertificationRiModelService riModelService` en el ctor primario del controlador (junto a los existentes).

- [ ] **Step 2: Agregar endpoints** (usar rutas relativas del controlador; content-type validado a `application/pdf`):

```csharp
[HttpPost("print-templates")]
public async Task<ActionResult> UploadRiModel([FromForm] IFormFile pdfFile, [FromForm] string clientGuidId,
    [FromForm] string name, [FromForm] List<string> ecfTypeCodes)
{
    if (pdfFile.ContentType != "application/pdf") return BadRequest("El archivo debe ser PDF.");
    using var ms = new MemoryStream();
    await pdfFile.CopyToAsync(ms);
    var dto = await riModelService.UploadAsync(clientGuidId, name, ecfTypeCodes, ms.ToArray(), pdfFile.FileName);
    return Ok(dto);
}

[HttpGet("print-templates/{clientGuidId}")]
public async Task<ActionResult> ListRiModels(string clientGuidId)
    => Ok(await riModelService.ListByClientAsync(clientGuidId));

[HttpGet("print-templates/{templateGuidId}/preview")]
public async Task<ActionResult> PreviewRiModel(string templateGuidId)
{
    var bytes = await riModelService.GetReferenceRiAsync(templateGuidId);
    return bytes is null ? NotFound() : File(bytes, "application/pdf");
}

[HttpPut("print-templates/{templateGuidId}")]
public async Task<ActionResult> UpdateRiModel(string templateGuidId, [FromForm] string? name,
    [FromForm] List<string>? ecfTypeCodes, [FromForm] bool? confirm, [FromForm] IFormFile? pdfFile)
{
    byte[]? src = null; string? fn = null;
    if (pdfFile is not null) { using var ms = new MemoryStream(); await pdfFile.CopyToAsync(ms); src = ms.ToArray(); fn = pdfFile.FileName; }
    return Ok(await riModelService.UpdateAsync(templateGuidId, name, ecfTypeCodes, confirm, src, fn));
}

[HttpDelete("print-templates/{templateGuidId}")]
public async Task<ActionResult> DeleteRiModel(string templateGuidId)
{ await riModelService.DeleteAsync(templateGuidId); return NoContent(); }

[HttpGet("ri/{clientGuidId}/{ncf}/preview")]
public async Task<ActionResult> PreviewRi(string clientGuidId, string ncf)
    => File(await riModelService.RenderRiForDocumentAsync(clientGuidId, ncf), "application/pdf");

[HttpGet("ri/{clientGuidId}/{ncf}/download")]
public async Task<ActionResult> DownloadRi(string clientGuidId, string ncf)
    => File(await riModelService.RenderRiForDocumentAsync(clientGuidId, ncf), "application/pdf", $"RI_{ncf}.pdf");

[HttpGet("ri/{clientGuidId}/zip")]
public async Task<ActionResult> DownloadRiZip(string clientGuidId)
{
    var bytes = await riModelService.RenderAllZipAsync(clientGuidId, env.WebRootPath);
    return File(bytes, "application/zip", $"RI_{clientGuidId}.zip");
}
```

(Envolver en try/catch coherente con el controlador; `InvalidOperationException` de "sin modelo" → `BadRequest`.)

- [ ] **Step 3: Compilar y smoke manual**

Run: `dotnet build ZynstormECFPlatform.Web.Api/ZynstormECFPlatform.Web.Api.csproj -nologo`
Expected: `Build succeeded`. Levantar la API y probar `POST /print-templates` con un PDF y `GET /print-templates/{clientGuidId}/preview`.

- [ ] **Step 4: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs
git commit -m "feat(ri): endpoints de modelos RI y generacion de RI (preview/download/zip)"
```

---

## Task 12: Frontend — servicio

**Files:**
- Modify: `ZynstormECFPlatform-FrontEnd/services/certification.service.ts`

**Interfaces:**
- Produces: `uploadRiModel`, `listRiModels`, `updateRiModel`, `deleteRiModel`, `riModelPreviewUrl(templateGuidId)`, `riPreviewUrl(clientGuidId, ncf)`, `riDownloadUrl(clientGuidId, ncf)`, `riZipUrl(clientGuidId)`, y el tipo `RiModelListItem`.

- [ ] **Step 1: Agregar tipos + funciones** siguiendo el patrón de `getLastSimulationResults` (fetch a `${CERTIFICATION_URL}/...`, `credentials:"include"`). Para preview/download que devuelven binario, exponer **URLs** (`${CERTIFICATION_URL}/ri/${clientGuidId}/${ncf}/preview`) para usarlas en `<iframe>`/`window.open`. `uploadRiModel`/`updateRiModel` usan `FormData` (multipart).

- [ ] **Step 2: Typecheck**

Run (en WSL con node 22): `npx tsc --noEmit -p tsconfig.json`
Expected: sin errores en `certification.service.ts`.

- [ ] **Step 3: Commit**

```bash
git add services/certification.service.ts
git commit -m "feat(ri): cliente de servicio para modelos RI y generacion"
```

---

## Task 13: Frontend — bloque Paso 5

**Files:**
- Create: `ZynstormECFPlatform-FrontEnd/components/certification/ri-step5.tsx`
- Modify: `ZynstormECFPlatform-FrontEnd/app/certificacion/page.tsx` (render `currentStep === 5`)

**Interfaces:**
- Consumes: funciones de Task 12; `simulationTests` (comprobantes del Paso 4) y `getEcfTypeFromNcf` ya en `page.tsx`.

- [ ] **Step 1: Componente `RiStep5`** con props `{ clientGuidId, selectedClient, simulationTests }`:
  - **Modelos**: form (input file PDF, nombre, multi-select de tipos presentes en `simulationTests`) → `uploadRiModel`; lista de modelos (`listRiModels`) con tipos, badge de `Status`, warnings, botón "Ver preview" (abre `riModelPreviewUrl` en modal `<iframe>`), reasignar tipos / confirmar (`updateRiModel`), eliminar (`deleteRiModel`).
  - **Generación**: tabla de `simulationTests` (NCF, tipo via `getEcfTypeFromNcf`, si el tipo tiene modelo `Confirmed`); por fila "Vista previa" (`<iframe>` a `riPreviewUrl`) y "Descargar" (`window.open(riDownloadUrl)`); botón "Descargar todas (ZIP)" (`riZipUrl`). Filas cuyo tipo no tiene modelo confirmado se marcan y se deshabilitan.

- [ ] **Step 2: Integrar en `page.tsx`** — agregar rama `currentStep === 5` (junto a la de `=== 4`) que renderiza `<RiStep5 clientGuidId=... selectedClient=... simulationTests={simulationTests} />`.

- [ ] **Step 3: Typecheck + prueba manual**

Run: `npx tsc --noEmit -p tsconfig.json`
Expected: sin errores. Levantar Next dev, ir al Paso 5, subir un PDF modelo, confirmar preview, generar RI de un comprobante y validar el QR en la DGII.

- [ ] **Step 4: Commit**

```bash
git add components/certification/ri-step5.tsx app/certificacion/page.tsx
git commit -m "feat(ri): UI del Paso 5 (subir modelo, preview, generar RI individual/ZIP)"
```

---

## Self-Review (cobertura del spec)

- §1 objetivos → Tasks 1-13. §2 componentes → Tasks 2,5,6,7,8,9. §3 datos → Tasks 3,4. §4 extractor/LayoutJson → Tasks 5,7. §5 mapper/renderer/QR → Tasks 2,6,8. §6 endpoints/frontend → Tasks 11,12,13. §7 errores → Tasks 7 (PDF sin texto), 9 (tipo sin modelo), 11 (content-type). §8 testing → Tasks 2,6,7,8.
- Consistencia de tipos: `LayoutDescriptor`/`RiData` (Task 5) usados igual en 6/7/8/9; `EcfQrUrlBuilder.Build` firma única (Task 2) consumida en 6; `ICertificationRiModelService` (Task 9) consumido en 11.
- Sin placeholders de acción vaga; el porting referencia archivos/métodos exactos de Mechanic-Service.

## Notas de riesgo
- La extracción por anclas (Task 7) es la parte más incierta; sus tests con `modelo_ok.pdf` acotan el comportamiento. La RI de referencia + preview (Tasks 9/11/13) son el resguardo visual acordado.
- QuestPDF es de flujo: el renderer honra columnas/paleta/QR; el eje vertical fluye (acordado en el spec).
