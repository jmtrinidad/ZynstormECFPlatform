# Comprobantes recibidos en PDF Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Descargar los e-CF recibidos como PDF desde el panel, exponer en la API con API key «hay recibidos / PDF / XML» y agregarlo a la librería `Zynstorm.DGII.ECF` con sus guías.

**Architecture:** Un renderizador QuestPDF nuevo (`ReceivedEcfPdf`) alimentado por `ReceivedEcfMapper`, que reutiliza `EcfRiDataMapper` y los helpers de `EcfRiTemplateMapper`. Un servicio de consulta (`ReceivedEcfService` + lógica pura `ReceivedEcfLogic`) sirve a `EcfController` (API key) y `FeController` (panel). La librería agrega tres métodos a `IEcfClient` con resultados tipados que no lanzan por fallos de red.

**Tech Stack:** .NET 10 / ASP.NET / QuestPDF 2026.5.0 / QRCoder 1.6.0 / PdfPig (pruebas) / xUnit; Next.js + TypeScript (panel); librería C# `net452;netstandard2.0;net8.0` con Newtonsoft.Json 13.0.3; Python + reportlab para las guías PDF.

Spec: `Docs/superpowers/specs/2026-10-14-recibidos-pdf-design.md`.

## Global Constraints

- Solo `MessageType.Ecf` se lista y descarga; las aprobaciones comerciales no.
- `ReceivedB2BMessagesCleanupJob` borra recibidos con más de 30 días: la lista solo ve lo que se conserve y las guías lo dicen.
- Lista: `ReceivedAtUtc > since` (exclusivo), orden ascendente, `limit` por defecto 100, máximo 200. Respuesta `{ count, hasMore, items:[{ eNcf, rncEmisor, ecfType, total, receivedAtUtc }] }`.
- Códigos: sin API key de cliente 403; eNCF o `since` inválido 400; no existe 404 `{ found:false, message }`; ambiguo 409 `{ found:true, message, emisores:[...] }`; XML ilegible al pedir PDF 422 `{ message }`.
- Paleta del PDF: azul marino `#0A2540`, acento `#0D9488`, texto `#0F172A`, apagado `#64748B`, líneas `#E2E8F0`, fondo suave `#F1F5F9`. Página A4.
- Ambiente del QR: `EcfXmlValidation:TargetDgiiEnvironment`; si falta o no es válido, `Production`.
- Librería: no lanza excepciones por red, timeout ni respuesta del servidor (sí `ArgumentException` por eNCF vacío, igual que `GetByNcf`). Cada propiedad de los payloads internos lleva `[JsonProperty("...")]`. Código compatible con `net452`: solo construcciones clásicas de C# (sin `Span`, sin `async` streams, sin paquetes nuevos). Versión 0.3.0.
- Textos para el usuario en español, sin acentos en los comentarios de la librería (convención del repo) y con acentos en el backend y el panel.
- Todo local, **sin push**. Backend en `Staging` y replicado a `feature/emit-idempotente`. Antes de editar cada repo: `git branch --show-current`.
- Rutas base: backend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, panel `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`, librería `C:\Projects\Zynstorm.DGII.ECF`.

## File Structure

Backend (`ZynstormECFPlatform`):

- Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfModel.cs`: modelo de vista del PDF.
- Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfMapper.cs`: XML → modelo.
- Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfPdf.cs`: documento QuestPDF + `Render(xml, env)`.
- Modify `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs`: tres helpers pasan de `private` a `internal`.
- Create `ZynstormECFPlatform.Services/Production/ReceivedEcfLookup.cs`: DTOs, `ReceivedEcfLogic`, `IReceivedEcfService`, `ReceivedEcfService`.
- Modify `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`: registrar el servicio.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs`: tres endpoints.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/FeController.cs`: endpoint PDF del panel.
- Tests: `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfMapperTests.cs`, `ReceivedEcfPdfTests.cs`, `ReceivedEcfPreviewTests.cs`, `ZynstormECFPlatform.Tests/Production/ReceivedEcfLogicTests.cs`.

Panel (`ZynstormECFPlatform-FrontEnd`): modify `services/fe.service.ts`, `app/facturas/page.tsx`.

Librería (`Zynstorm.DGII.ECF`): create `Contracts/EcfReceivedItem.cs`, `Client/EcfReceivedStatus.cs`, `Client/EcfReceivedResults.cs`, `Client/EcfReceivedPayloads.cs`; modify `Client/EcfEndpoints.cs`, `Client/IEcfClient.cs`, `Client/EcfClient.cs`, `Directory.Build.props`, `docs/build-package.py`, `docs/CHANGELOG.md`, `docs/GUIA-DE-USO.md`, `docs/GUIA-RAPIDA.md`, `docs/GUIA-TECNICA.md`; tests `Fakes/ScriptedHandler.cs`, `ReceivedTests.cs`.

---

## Parte A: Backend (rama `Staging`)

### Task 1: Modelo y mapper del recibido

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfModel.cs`
- Create: `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfMapper.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs` (visibilidad de `NcfTypeName`, `ItbisRateFor`, `PaymentCondition`, `ModificationCodeLabel`)
- Test: `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfMapperTests.cs`

**Interfaces:**
- Produces: `ReceivedEcfMapper.Map(string signedXml, DgiiEnvironment environment) : ReceivedEcfModel`. `ReceivedEcfModel` y `ReceivedEcfItem` con las propiedades del código de abajo; `Party` viene de `ZynstormECFPlatform.Services.Ri` (`Name`, `Document`, `Address`, `Phone`, `Email`, `Country`).

- [ ] **Step 1: Confirmar rama y los helpers que se abren**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && git branch --show-current
grep -n "static.*NcfTypeName\|static.*ItbisRateFor\|static.*PaymentCondition\|static.*ModificationCodeLabel" ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs
```
Expected: `Staging`, y las cuatro líneas con `private static`.

- [ ] **Step 2: Escribir la prueba que falla**

Create `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfMapperTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfMapperTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Fact]
    public void Map_Type31_FillsIssuerBuyerAndTotals()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.Production);

        Assert.Equal(31, model.EcfType);
        Assert.Equal("E310000000402", model.ENcf);
        Assert.Equal("FACTURA DE CRÉDITO FISCAL ELECTRÓNICA", model.TypeName);
        Assert.Equal("133009889", model.Issuer.Document);
        Assert.Equal("102620717", model.Buyer.Document);
        Assert.Equal(6001.00m, model.Total);
        Assert.NotEmpty(model.Items);
        Assert.Contains("ConsultaTimbre", model.Qr);
        Assert.False(string.IsNullOrWhiteSpace(model.SecurityCode));
    }

    [Fact]
    public void Map_Type34_FillsReferenceAndTaxRate()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production);

        Assert.Equal(34, model.EcfType);
        Assert.Equal("NOTA DE CRÉDITO ELECTRÓNICA", model.TypeName);
        Assert.Equal("E310000000034", model.AffectedNcf);
        Assert.Equal("3 - Corrige montos del NCF modificado", model.ModificationCode);
        Assert.Equal("Error en monto", model.ModificationReason);
        Assert.Equal(10, model.Items.Count);
        Assert.Equal(18m, model.ItbisRate);
        Assert.Equal(86445.00m, model.Itbis);
        Assert.Equal(566695.00m, model.Total);
        Assert.Equal("Lb", model.Items[0].Unit);
    }

    [Fact]
    public void Map_Type43_WithoutBuyer_LeavesBuyerEmpty()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E430000000008.xml"), DgiiEnvironment.Production);

        Assert.Equal(43, model.EcfType);
        Assert.Equal(string.Empty, model.Buyer.Name);
        Assert.Equal(string.Empty, model.Buyer.Document);
    }

    [Fact]
    public void Map_InvalidXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ReceivedEcfMapper.Map("no es xml", DgiiEnvironment.Production));
    }
}
```

- [ ] **Step 3: Ejecutar para verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfMapperTests" 2>&1 | tail -15`
Expected: error de compilación `ReceivedEcfMapper` / `ZynstormECFPlatform.Services.Ri.Received` no existe.

- [ ] **Step 4: Abrir los helpers del mapper existente**

En `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs` cambiar `private static` por `internal static` en estas cuatro declaraciones (solo la visibilidad):

```csharp
internal static decimal ItbisRateFor(int indicadorFacturacion, RiTotals totals) => indicadorFacturacion switch
internal static string NcfTypeName(int ecfType) => ecfType switch
internal static string PaymentCondition(RiData data)
internal static string ModificationCodeLabel(string code) => code switch
```

- [ ] **Step 5: Escribir el modelo**

Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfModel.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// View-model del PDF de un e-CF RECIBIDO de un tercero. A diferencia de los RI propios, el
/// emisor sale del XML (no del cliente) y el comprador es normalmente nuestro cliente.
/// </summary>
public class ReceivedEcfModel
{
    public int EcfType { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public string ENcf { get; set; } = string.Empty;

    public Party Issuer { get; set; } = new();

    public Party Buyer { get; set; } = new();

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    /// <summary>FechaVencimientoSecuencia como dd/MM/yyyy; vacío si el XML no la trae.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    public string PaymentType { get; set; } = string.Empty;

    public string PaymentCondition { get; set; } = string.Empty;

    public string InternalInvoiceNumber { get; set; } = string.Empty;

    /// <summary>InformacionReferencia (notas 33/34).</summary>
    public string AffectedNcf { get; set; } = string.Empty;

    public string ModificationCode { get; set; } = string.Empty;

    public string ModificationReason { get; set; } = string.Empty;

    public List<ReceivedEcfItem> Items { get; set; } = [];

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Exento { get; set; }

    public decimal Itbis { get; set; }

    /// <summary>Tasa ITBIS principal de Totales (18, 16...); 0 si no hay.</summary>
    public decimal ItbisRate { get; set; }

    public decimal ItbisRetenido { get; set; }

    public decimal IsrRetencion { get; set; }

    public decimal Total { get; set; }

    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}

public class ReceivedEcfItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Abreviatura de la unidad de medida; vacío si el XML no la trae.</summary>
    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Discount { get; set; }

    public decimal Itbis { get; set; }

    public decimal Amount { get; set; }
}
```

- [ ] **Step 6: Escribir el mapper**

Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfMapper.cs`:

```csharp
using System.Globalization;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// Convierte el XML firmado de un e-CF recibido en el <see cref="ReceivedEcfModel"/> del PDF.
/// Reutiliza <see cref="EcfRiDataMapper"/> (emisor, comprador, ítems, totales, QR) y los helpers
/// de <see cref="EcfRiTemplateMapper"/> para que las etiquetas coincidan con las del resto de RI.
/// </summary>
public static class ReceivedEcfMapper
{
    public static ReceivedEcfModel Map(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        var ecfType = int.TryParse(data.TipoeCF, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        return new ReceivedEcfModel
        {
            EcfType = ecfType,
            TypeName = EcfRiTemplateMapper.NcfTypeName(ecfType),
            ENcf = data.ENcf,
            Issuer = data.Issuer,
            Buyer = data.Buyer,
            FechaEmision = data.FechaEmision.Replace('-', '/'),
            FechaFirma = data.FechaFirma,
            ValidUntil = EcfRiTemplateMapper.FormatDate(data.FechaVencimientoSecuencia),
            PaymentType = EcfRiTemplateMapper.PaymentTypeLabel(data.TipoPago),
            PaymentCondition = EcfRiTemplateMapper.PaymentCondition(data),
            InternalInvoiceNumber = data.NumeroFacturaInterna,
            AffectedNcf = data.NcfModificado,
            ModificationCode = EcfRiTemplateMapper.ModificationCodeLabel(data.CodigoModificacion),
            ModificationReason = data.RazonModificacion,
            Items = data.Items.ConvertAll(item => new ReceivedEcfItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Unit = EcfRiTemplateMapper.UnitAbbreviation(item.UnidadMedida),
                Price = item.Price,
                Discount = item.Discount,
                // Los XML de la DGII no siempre traen MontoITBIS por ítem: se deriva de la tasa.
                Itbis = item.Itbis > 0
                    ? item.Itbis
                    : Math.Round(item.Amount * EcfRiTemplateMapper.ItbisRateFor(item.IndicadorFacturacion, data.Totals) / 100m, 2),
                Amount = item.Amount
            }),
            SubTotal = data.Totals.SubTotal,
            Discount = data.Items.Sum(item => item.Discount),
            Exento = data.Totals.Exento,
            Itbis = data.Totals.Itbis,
            ItbisRate = data.Totals.Itbis1Rate,
            ItbisRetenido = data.Totals.ItbisRetenido,
            IsrRetencion = data.Totals.IsrRetencion,
            Total = data.Totals.Total,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }
}
```

Nota: `UnitAbbreviation(0)` devuelve `""` y un código desconocido devuelve `"Und"`. El fixture 34 usa la unidad 23, que el catálogo de `EcfRiTemplateMapper.UnitAbbreviation` abrevia «Lb». Si el assert falla, leer ese catálogo y ajustar el assert (no el mapper).

- [ ] **Step 7: Ejecutar las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfMapperTests" 2>&1 | tail -15`
Expected: 4 passed. Si algún valor del fixture (`133009889`, `102620717`, `6001.00`) no coincide, abrir `Ri/Fixtures/Paso_1_E310000000402.xml` y corregir el assert al valor del archivo.

- [ ] **Step 8: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
git add ZynstormECFPlatform.Services/Ri ZynstormECFPlatform.Tests/Ri/Received
git commit -m "feat(recibidos): modelo y mapper del e-CF recibido para el PDF"
```
(con las líneas de atribución del sistema al final del mensaje).

---

### Task 2: Documento PDF del recibido

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfPdf.cs`
- Test: `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfPdfTests.cs`
- Test: `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfPreviewTests.cs`

**Interfaces:**
- Consumes: `ReceivedEcfModel`, `ReceivedEcfMapper.Map` (Task 1).
- Produces: `new ReceivedEcfPdf(ReceivedEcfModel).GeneratePdf() : byte[]` y `ReceivedEcfPdf.Render(string signedXml, DgiiEnvironment environment) : byte[]` (lanza si el XML no se puede interpretar).

- [ ] **Step 1: Escribir las pruebas que fallan**

Create `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfPdfTests.cs`:

```csharp
using System.Text;
using QuestPDF.Fluent;
using UglyToad.PdfPig;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfPdfTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    private static string PdfText(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
    }

    private static int PageCount(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return pdf.NumberOfPages;
    }

    [Theory]
    [InlineData("Paso_1_E310000000402.xml", "E310000000402")]
    [InlineData("Paso_25_E320000000344.xml", "E320000000344")]
    [InlineData("Paso_E340000000002.xml", "E340000000002")]
    [InlineData("Paso_E410000000007.xml", "E410000000007")]
    [InlineData("Paso_E430000000008.xml", "E430000000008")]
    public void Render_AllTypes_ProducesPdfWithNcf(string fixture, string encf)
    {
        var bytes = ReceivedEcfPdf.Render(Load(fixture), DgiiEnvironment.Production);

        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
        var text = PdfText(bytes);
        Assert.Contains(encf, text);
        Assert.Contains("COMPROBANTE", text);
        Assert.Contains("RECIBIDO", text);
    }

    [Fact]
    public void Render_Type34_ShowsReferenceAndTotals()
    {
        var text = PdfText(ReceivedEcfPdf.Render(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production));

        Assert.Contains("DOCUMENTOS ELECTRONICOS DE 02", text);
        Assert.Contains("DOCUMENTOS ELECTRONICOS DE 03", text);
        Assert.Contains("E310000000034", text);
        Assert.Contains("Error en monto", text);
        Assert.Contains("566,695.00", text);
        Assert.Contains("ITBIS", text);
    }

    [Fact]
    public void GeneratePdf_BuyerEmpty_ShowsConsumidorFinal()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E430000000008.xml"), DgiiEnvironment.Production);

        Assert.Contains("Consumidor", PdfText(new ReceivedEcfPdf(model).GeneratePdf()));
    }

    [Fact]
    public void GeneratePdf_ManyItems_SpansPagesAndRepeatsTableHeader()
    {
        var model = ReceivedEcfMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.Production);
        model.Items = Enumerable.Range(1, 80).Select(i => new ReceivedEcfItem
        {
            Description = $"Artículo número {i}",
            Quantity = 1,
            Unit = "Und",
            Price = 100m,
            Itbis = 18m,
            Amount = 100m
        }).ToList();

        var bytes = new ReceivedEcfPdf(model).GeneratePdf();

        Assert.True(PageCount(bytes) >= 2);
        var occurrences = PdfText(bytes).Split("Descripción").Length - 1;
        Assert.True(occurrences >= 2, "la cabecera de la tabla debe repetirse en cada página");
    }

    [Fact]
    public void Render_InvalidXml_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ReceivedEcfPdf.Render("no es xml", DgiiEnvironment.Production));
    }
}
```

Create `ZynstormECFPlatform.Tests/Ri/Received/ReceivedEcfPreviewTests.cs` (revisión visual; no hace nada si no hay variable de entorno):

```csharp
using QuestPDF.Fluent;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Ri.Received;

namespace ZynstormECFPlatform.Tests.Ri.Received;

public class ReceivedEcfPreviewTests
{
    /// <summary>
    /// Con RI_PREVIEW_DIR definida deja PDF y PNG de cada fixture en esa carpeta, para revisar el
    /// diseño a ojo. Sin la variable no hace nada.
    /// </summary>
    [Fact]
    public void WritePreviews_WhenRequested()
    {
        var dir = Environment.GetEnvironmentVariable("RI_PREVIEW_DIR");
        if (string.IsNullOrWhiteSpace(dir))
        {
            return;
        }

        Directory.CreateDirectory(dir);
        foreach (var fixture in new[] { "Paso_1_E310000000402", "Paso_E340000000002", "Paso_E430000000008" })
        {
            var xml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", fixture + ".xml"));
            var model = ReceivedEcfMapper.Map(xml, DgiiEnvironment.Production);
            var document = new ReceivedEcfPdf(model);

            File.WriteAllBytes(Path.Combine(dir, fixture + ".pdf"), document.GeneratePdf());
            var page = 1;
            foreach (var png in document.GenerateImages())
            {
                File.WriteAllBytes(Path.Combine(dir, $"{fixture}-p{page++}.png"), png);
            }
        }
    }
}
```

- [ ] **Step 2: Ejecutar para verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfPdfTests" 2>&1 | tail -15`
Expected: error de compilación, `ReceivedEcfPdf` no existe.

- [ ] **Step 3: Implementar el documento**

Create `ZynstormECFPlatform.Services/Ri/Received/ReceivedEcfPdf.cs`:

```csharp
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// PDF A4 de un e-CF RECIBIDO de un tercero: cabecera azul marino con el eNCF, tarjetas de
/// emisor y comprador, tabla de ítems con cabecera repetida, caja de totales y pie con el QR de
/// la DGII. Sirve a todos los tipos de e-CF (31 a 47).
/// </summary>
public class ReceivedEcfPdf(ReceivedEcfModel model) : IDocument
{
    private const string Navy = "#0A2540";
    private const string Accent = "#0D9488";
    private const string Ink = "#0F172A";
    private const string Muted = "#64748B";
    private const string Line = "#E2E8F0";
    private const string Soft = "#F1F5F9";
    private const float Gutter = 36;

    private static readonly CultureInfo Culture = new("es-DO");

    private readonly ReceivedEcfModel _model = model;

    static ReceivedEcfPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>Mapea el XML firmado y genera el PDF. Lanza si el XML no se puede interpretar.</summary>
    public static byte[] Render(string signedXml, DgiiEnvironment environment) =>
        new ReceivedEcfPdf(ReceivedEcfMapper.Map(signedXml, environment)).GeneratePdf();

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"{_model.TypeName} {_model.ENcf}",
        Author = _model.Issuer.Name,
        Subject = "Representación impresa de un e-CF recibido"
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(0);
            page.DefaultTextStyle(style => style.FontFamily(Fonts.Lato).FontSize(9).FontColor(Ink));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingHorizontal(Gutter).PaddingTop(18).PaddingBottom(12).Column(column =>
            {
                column.Spacing(14);
                column.Item().Element(ComposeParties);
                column.Item().Element(ComposeMeta);
                column.Item().Element(ComposeReference);
                column.Item().Element(ComposeItems);
                column.Item().Element(ComposeTotals);
            });
            page.Footer().Element(ComposeFooter);
        });
    }

    // ------------------------------------------------------------------ header

    private void ComposeHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Background(Navy).PaddingHorizontal(Gutter).PaddingVertical(22).Row(row =>
            {
                row.RelativeItem().Column(left =>
                {
                    left.Item().Text("COMPROBANTE RECIBIDO")
                        .FontSize(8).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                    left.Item().PaddingTop(5).Text(_model.TypeName)
                        .FontSize(15).Bold().FontColor(Colors.White);
                    left.Item().PaddingTop(3).Text("Documento electrónico fiscal de un tercero")
                        .FontSize(8).FontColor("#94A3B8");
                });

                row.ConstantItem(190).AlignRight().Column(right =>
                {
                    right.Item().AlignRight().Text("e-NCF").FontSize(8).FontColor("#94A3B8").LetterSpacing(0.12f);
                    right.Item().AlignRight().Text(_model.ENcf).FontSize(20).Bold().FontColor(Colors.White);
                });
            });
            column.Item().Height(3).Background(Accent);
        });
    }

    // ------------------------------------------------------------------ parties

    private void ComposeParties(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Element(c => PartyCard(c, "EMISOR", _model.Issuer, "No indicado"));
            row.ConstantItem(12);
            row.RelativeItem().Element(c => PartyCard(c, "COMPRADOR", _model.Buyer, "Consumidor final"));
        });
    }

    private static void PartyCard(IContainer container, string label, Party party, string emptyName)
    {
        container
            .Background(Soft)
            .BorderLeft(3).BorderColor(Accent)
            .Padding(10)
            .Column(column =>
            {
                column.Spacing(2);
                column.Item().Text(label).FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.12f);
                column.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(party.Name) ? emptyName : party.Name)
                    .FontSize(11).Bold();

                if (!string.IsNullOrWhiteSpace(party.Document))
                {
                    column.Item().Text(text =>
                    {
                        text.Span("RNC / ID  ").FontColor(Muted);
                        text.Span(party.Document).SemiBold();
                    });
                }

                if (!string.IsNullOrWhiteSpace(party.Address))
                {
                    column.Item().Text(party.Address).FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(party.Phone))
                {
                    column.Item().Text($"Tel. {party.Phone}").FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(party.Email))
                {
                    column.Item().Text(party.Email).FontColor(Muted);
                }
            });
    }

    // ------------------------------------------------------------------ meta strip

    private void ComposeMeta(IContainer container)
    {
        var cells = new List<(string Label, string Value)>
        {
            ("FECHA DE EMISIÓN", _model.FechaEmision),
            ("VÁLIDO HASTA", _model.ValidUntil),
            ("FORMA DE PAGO", string.IsNullOrWhiteSpace(_model.PaymentCondition) ? _model.PaymentType : _model.PaymentCondition),
            ("FACTURA INTERNA", _model.InternalInvoiceNumber)
        }.Where(cell => !string.IsNullOrWhiteSpace(cell.Value)).ToList();

        if (cells.Count == 0)
        {
            return;
        }

        container.BorderTop(0.75f).BorderBottom(0.75f).BorderColor(Line).PaddingVertical(8).Row(row =>
        {
            foreach (var cell in cells)
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text(cell.Label).FontSize(7).Bold().FontColor(Muted).LetterSpacing(0.1f);
                    column.Item().PaddingTop(2).Text(cell.Value).FontSize(10).SemiBold();
                });
            }
        });
    }

    private void ComposeReference(IContainer container)
    {
        if (string.IsNullOrWhiteSpace(_model.AffectedNcf))
        {
            return;
        }

        container.Border(0.75f).BorderColor(Line).Padding(10).Column(column =>
        {
            column.Item().Text("DOCUMENTO AFECTADO").FontSize(7.5f).Bold().FontColor(Accent).LetterSpacing(0.12f);
            column.Item().PaddingTop(3).Text(text =>
            {
                text.Span("NCF modificado  ").FontColor(Muted);
                text.Span(_model.AffectedNcf).Bold();
            });

            if (!string.IsNullOrWhiteSpace(_model.ModificationCode))
            {
                column.Item().Text(text =>
                {
                    text.Span("Modificación  ").FontColor(Muted);
                    text.Span(_model.ModificationCode);
                });
            }

            if (!string.IsNullOrWhiteSpace(_model.ModificationReason))
            {
                column.Item().Text(text =>
                {
                    text.Span("Motivo  ").FontColor(Muted);
                    text.Span(_model.ModificationReason);
                });
            }
        });
    }

    // ------------------------------------------------------------------ items

    private void ComposeItems(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(22);
                columns.RelativeColumn(5);
                columns.ConstantColumn(44);
                columns.ConstantColumn(66);
                columns.ConstantColumn(54);
                columns.ConstantColumn(58);
                columns.ConstantColumn(72);
            });

            table.Header(header =>
            {
                HeaderCell(header.Cell(), "#", false);
                HeaderCell(header.Cell(), "Descripción", false);
                HeaderCell(header.Cell(), "Cant.", true);
                HeaderCell(header.Cell(), "Precio", true);
                HeaderCell(header.Cell(), "Desc.", true);
                HeaderCell(header.Cell(), "ITBIS", true);
                HeaderCell(header.Cell(), "Importe", true);
            });

            var index = 0;
            foreach (var item in _model.Items)
            {
                var background = index % 2 == 1 ? Soft : Colors.White;
                index++;

                BodyCell(table.Cell(), background, index.ToString(Culture), false, Muted);
                table.Cell().Background(background).BorderBottom(0.5f).BorderColor(Line)
                    .PaddingVertical(5).PaddingHorizontal(4)
                    .Text(text =>
                    {
                        text.Span(item.Description);
                        if (!string.IsNullOrWhiteSpace(item.Unit))
                        {
                            text.Span($"  ·  {item.Unit}").FontColor(Muted).FontSize(7.5f);
                        }
                    });
                BodyCell(table.Cell(), background, item.Quantity.ToString("#,##0.##", Culture), true);
                BodyCell(table.Cell(), background, Money(item.Price), true);
                BodyCell(table.Cell(), background, item.Discount > 0 ? Money(item.Discount) : "—", true,
                    item.Discount > 0 ? Ink : Muted);
                BodyCell(table.Cell(), background, item.Itbis > 0 ? Money(item.Itbis) : "—", true,
                    item.Itbis > 0 ? Ink : Muted);
                BodyCell(table.Cell(), background, Money(item.Amount), true, Ink, bold: true);
            }
        });
    }

    private static void HeaderCell(IContainer container, string text, bool alignRight)
    {
        var cell = container.Background(Navy).PaddingVertical(6).PaddingHorizontal(4);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        cell.Text(text).FontSize(8).Bold().FontColor(Colors.White);
    }

    private static void BodyCell(IContainer container, string background, string text, bool alignRight,
        string color = Ink, bool bold = false)
    {
        var cell = container.Background(background).BorderBottom(0.5f).BorderColor(Line)
            .PaddingVertical(5).PaddingHorizontal(4);
        if (alignRight)
        {
            cell = cell.AlignRight();
        }

        var span = cell.Text(text).FontColor(color);
        if (bold)
        {
            span.SemiBold();
        }
    }

    // ------------------------------------------------------------------ totals

    private void ComposeTotals(IContainer container)
    {
        container.AlignRight().Width(250).ShowEntire().Column(column =>
        {
            column.Item().Element(c => TotalRow(c, "Subtotal", Money(_model.SubTotal)));

            if (_model.Discount > 0)
            {
                column.Item().Element(c => TotalRow(c, "Descuentos", $"-{Money(_model.Discount)}"));
            }

            if (_model.Exento > 0)
            {
                column.Item().Element(c => TotalRow(c, "Monto exento", Money(_model.Exento)));
            }

            var itbisLabel = _model.ItbisRate > 0
                ? $"ITBIS ({_model.ItbisRate.ToString("0.##", Culture)}%)"
                : "ITBIS";
            column.Item().Element(c => TotalRow(c, itbisLabel, Money(_model.Itbis)));

            if (_model.ItbisRetenido > 0)
            {
                column.Item().Element(c => TotalRow(c, "ITBIS retenido", $"-{Money(_model.ItbisRetenido)}"));
            }

            if (_model.IsrRetencion > 0)
            {
                column.Item().Element(c => TotalRow(c, "ISR retenido", $"-{Money(_model.IsrRetencion)}"));
            }

            column.Item().PaddingTop(6).Background(Navy).PaddingVertical(9).PaddingHorizontal(10).Row(row =>
            {
                row.RelativeItem().Text("TOTAL").FontSize(10).Bold().FontColor("#5EEAD4").LetterSpacing(0.12f);
                row.AutoItem().Text(Money(_model.Total)).FontSize(14).Bold().FontColor(Colors.White);
            });
        });
    }

    private static void TotalRow(IContainer container, string label, string value)
    {
        container.BorderBottom(0.5f).BorderColor(Line).PaddingVertical(4).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(Muted);
            row.AutoItem().Text(value).SemiBold();
        });
    }

    // ------------------------------------------------------------------ footer

    private void ComposeFooter(IContainer container)
    {
        container.PaddingHorizontal(Gutter).PaddingBottom(16).Column(column =>
        {
            column.Item().BorderTop(0.75f).BorderColor(Line).PaddingTop(10).Row(row =>
            {
                var qr = TryBuildQr(_model.Qr);
                if (qr is not null)
                {
                    row.ConstantItem(70).Height(70).Image(qr).FitArea();
                    row.ConstantItem(12);
                }

                row.RelativeItem().Column(info =>
                {
                    info.Spacing(2);
                    if (!string.IsNullOrWhiteSpace(_model.SecurityCode))
                    {
                        info.Item().Text(text =>
                        {
                            text.Span("Código de seguridad  ").FontColor(Muted);
                            text.Span(_model.SecurityCode).Bold();
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_model.FechaFirma))
                    {
                        info.Item().Text(text =>
                        {
                            text.Span("Fecha de firma  ").FontColor(Muted);
                            text.Span(_model.FechaFirma).SemiBold();
                        });
                    }

                    if (qr is not null)
                    {
                        info.Item().PaddingTop(2).Text("Escanee el código para verificar este comprobante en la DGII.")
                            .FontSize(7.5f).FontColor(Muted);
                    }
                });

                row.ConstantItem(140).AlignRight().AlignBottom().Column(right =>
                {
                    right.Item().AlignRight().Text("Representación impresa de un e-CF recibido")
                        .FontSize(7.5f).FontColor(Muted);
                    right.Item().AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            });
        });
    }

    private static byte[]? TryBuildQr(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        try
        {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var code = new QRCoder.PngByteQRCode(data);
            return code.GetGraphic(5);
        }
        catch
        {
            // Sin QR el PDF sigue siendo válido; solo se omite el bloque.
            return null;
        }
    }

    private static string Money(decimal value) => string.Format(Culture, "{0:C2}", value);
}
```

- [ ] **Step 4: Ejecutar las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfPdfTests" 2>&1 | tail -20`
Expected: todas pasan. Si algún método de QuestPDF no compila (`LetterSpacing`, `SemiBold`, `ShowEntire`, `FitArea`), leer el error y usar el equivalente de la versión 2026.5.0 sin cambiar el diseño: quitar `LetterSpacing` es aceptable, el resto tiene que quedar igual. Si el assert `"566,695.00"` falla por el formato de moneda, imprimir el texto del PDF y asertar contra el formato real de `es-DO`.

- [ ] **Step 5: Revisión visual**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
export RI_PREVIEW_DIR="C:/Users/JOSETR~2/AppData/Local/Temp/claude/C--Projects-ZynstormECF-WorkSpace/b2761a8b-05ed-4499-8a85-8fad9df7a35f/scratchpad/recibidos"
dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfPreviewTests" 2>&1 | tail -5
ls "$RI_PREVIEW_DIR"
```
Expected: PDF y PNG de los tres fixtures. Abrir los PNG con la herramienta Read y revisar: cabecera, tarjetas, tabla, totales, pie, que nada se corte ni se solape. Ajustar tamaños, espacios o colores en `ReceivedEcfPdf.cs` y repetir hasta que se vea profesional y limpio. El fixture 34 (10 ítems, nota de crédito) y el 43 (sin comprador) son los casos exigentes.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/Received ZynstormECFPlatform.Tests/Ri/Received
git commit -m "feat(recibidos): PDF A4 del e-CF recibido con diseño propio"
```

---

### Task 3: Consulta de recibidos (lógica y servicio)

**Files:**
- Create: `ZynstormECFPlatform.Services/Production/ReceivedEcfLookup.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (línea ~59, junto a `IEcfLookupService`)
- Test: `ZynstormECFPlatform.Tests/Production/ReceivedEcfLogicTests.cs`

**Interfaces:**
- Consumes: `EcfLookupLogic.NormalizeNcf(string?) : string?`, `NcfHelper.TryExtractEcfType(string, out int)`, `ReceivedEcfMapper`-independiente (el total se lee con `XDocument`), `IReceivedB2BMessageService.Table`.
- Produces:
  - `ReceivedEcfListItem(string ENcf, string RncEmisor, int EcfType, decimal? Total, DateTime ReceivedAtUtc)` (record).
  - `ReceivedEcfListResponse(int Count, bool HasMore, IReadOnlyList<ReceivedEcfListItem> Items)` (record).
  - `ReceivedEcfPickKind { Found, NotFound, Ambiguous }`, `ReceivedEcfPick(ReceivedEcfPickKind Kind, ReceivedB2BMessage? Message, IReadOnlyList<string> Emisores)` (record).
  - `ReceivedEcfLogic.NormalizeRnc(string?) : string`, `.ClampLimit(int?) : int`, `.Pick(IEnumerable<ReceivedB2BMessage>, string? rncEmisor) : ReceivedEcfPick`, `.ReadTotal(string rawXml) : decimal?`, `.ToListItem(ReceivedB2BMessage) : ReceivedEcfListItem`, `.ResolveEnvironment(string? configured) : DgiiEnvironment`, `.TryParseSince(string? raw, out DateTime? sinceUtc) : bool`.
  - `IReceivedEcfService.ListAsync(int clientId, DateTime? sinceUtc, int limit, CancellationToken ct) : Task<ReceivedEcfListResponse>` y `.FindAsync(int clientId, string normalizedEncf, string? rncEmisor, CancellationToken ct) : Task<ReceivedEcfPick>`.

- [ ] **Step 1: Escribir las pruebas que fallan**

Create `ZynstormECFPlatform.Tests/Production/ReceivedEcfLogicTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class ReceivedEcfLogicTests
{
    private static ReceivedB2BMessage Message(string rnc, DateTime receivedAt, string encf = "E310000000402", string xml = "") =>
        new()
        {
            ReceivedB2BMessageId = 1,
            ClientId = 7,
            MessageType = MessageType.Ecf,
            RncEmisor = rnc,
            RncComprador = "101010101",
            ENcf = encf,
            RawXml = xml,
            ReceivedAtUtc = receivedAt
        };

    [Theory]
    [InlineData("1-33-00988-9", "133009889")]
    [InlineData(" 133009889 ", "133009889")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeRnc_KeepsOnlyDigits(string? raw, string expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.NormalizeRnc(raw));
    }

    [Theory]
    [InlineData(null, 100)]
    [InlineData(0, 100)]
    [InlineData(-5, 100)]
    [InlineData(25, 25)]
    [InlineData(200, 200)]
    [InlineData(5000, 200)]
    public void ClampLimit_AppliesDefaultAndMaximum(int? raw, int expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.ClampLimit(raw));
    }

    [Fact]
    public void Pick_NoCandidates_IsNotFound()
    {
        Assert.Equal(ReceivedEcfPickKind.NotFound, ReceivedEcfLogic.Pick([], null).Kind);
    }

    [Fact]
    public void Pick_SingleEmisor_ReturnsLatestResend()
    {
        var older = Message("133009889", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Message("133009889", new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc));

        var pick = ReceivedEcfLogic.Pick([older, newer], null);

        Assert.Equal(ReceivedEcfPickKind.Found, pick.Kind);
        Assert.Same(newer, pick.Message);
    }

    [Fact]
    public void Pick_TwoEmisores_WithoutRnc_IsAmbiguous()
    {
        var a = Message("133009889", DateTime.UtcNow);
        var b = Message("102620717", DateTime.UtcNow);

        var pick = ReceivedEcfLogic.Pick([a, b], null);

        Assert.Equal(ReceivedEcfPickKind.Ambiguous, pick.Kind);
        Assert.Equal(["102620717", "133009889"], pick.Emisores);
    }

    [Fact]
    public void Pick_TwoEmisores_WithRnc_NarrowsToThatEmisor()
    {
        var a = Message("133009889", DateTime.UtcNow);
        var b = Message("102620717", DateTime.UtcNow);

        var pick = ReceivedEcfLogic.Pick([a, b], "1-02-62071-7");

        Assert.Equal(ReceivedEcfPickKind.Found, pick.Kind);
        Assert.Same(b, pick.Message);
    }

    [Fact]
    public void Pick_RncThatDoesNotMatch_IsNotFound()
    {
        var a = Message("133009889", DateTime.UtcNow);

        Assert.Equal(ReceivedEcfPickKind.NotFound, ReceivedEcfLogic.Pick([a], "999999999").Kind);
    }

    [Fact]
    public void ReadTotal_ReadsMontoTotal_AndIsNullWhenUnreadable()
    {
        Assert.Equal(1180.50m,
            ReceivedEcfLogic.ReadTotal("<ECF><Encabezado><Totales><MontoTotal>1180.50</MontoTotal></Totales></Encabezado></ECF>"));
        Assert.Null(ReceivedEcfLogic.ReadTotal("no es xml"));
        Assert.Null(ReceivedEcfLogic.ReadTotal("<ECF/>"));
    }

    [Fact]
    public void ToListItem_TakesTypeFromNcf()
    {
        var item = ReceivedEcfLogic.ToListItem(Message("133009889", new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), "E340000000002"));

        Assert.Equal("E340000000002", item.ENcf);
        Assert.Equal(34, item.EcfType);
        Assert.Equal("133009889", item.RncEmisor);
        Assert.Null(item.Total);
    }

    [Theory]
    [InlineData(null, DgiiEnvironment.Production)]
    [InlineData("", DgiiEnvironment.Production)]
    [InlineData("basura", DgiiEnvironment.Production)]
    [InlineData("Test", DgiiEnvironment.Test)]
    [InlineData("certecf", DgiiEnvironment.CerteCF)]
    public void ResolveEnvironment_FallsBackToProduction(string? configured, DgiiEnvironment expected)
    {
        Assert.Equal(expected, ReceivedEcfLogic.ResolveEnvironment(configured));
    }

    [Theory]
    [InlineData(null, true, null)]
    [InlineData("", true, null)]
    [InlineData("2026-10-14T12:30:00Z", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("2026-10-14T08:30:00-04:00", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("2026-10-14T12:30:00", true, "2026-10-14T12:30:00.0000000Z")]
    [InlineData("ayer", false, null)]
    public void TryParseSince_ReadsIsoAsUtc(string? raw, bool ok, string? expected)
    {
        var result = ReceivedEcfLogic.TryParseSince(raw, out var since);

        Assert.Equal(ok, result);
        Assert.Equal(expected, since?.ToString("O"));
        if (since is not null)
        {
            Assert.Equal(DateTimeKind.Utc, since.Value.Kind);
        }
    }
}
```

- [ ] **Step 2: Ejecutar para verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfLogicTests" 2>&1 | tail -10`
Expected: error de compilación, `ReceivedEcfLogic` no existe.

- [ ] **Step 3: Implementar la lógica y el servicio**

Create `ZynstormECFPlatform.Services/Production/ReceivedEcfLookup.cs`:

```csharp
using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Production;

public sealed record ReceivedEcfListItem(string ENcf, string RncEmisor, int EcfType, decimal? Total, DateTime ReceivedAtUtc);

public sealed record ReceivedEcfListResponse(int Count, bool HasMore, IReadOnlyList<ReceivedEcfListItem> Items);

public enum ReceivedEcfPickKind
{
    Found,
    NotFound,
    Ambiguous
}

public sealed record ReceivedEcfPick(ReceivedEcfPickKind Kind, ReceivedB2BMessage? Message, IReadOnlyList<string> Emisores);

/// <summary>Reglas puras de la consulta de e-CF recibidos. Sin base de datos, para poder probarlas.</summary>
public static class ReceivedEcfLogic
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 200;

    /// <summary>Solo los dígitos del RNC; vacío si no hay.</summary>
    public static string NormalizeRnc(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? string.Empty : new string(raw.Where(char.IsAsciiDigit).ToArray());

    public static int ClampLimit(int? raw) =>
        raw is null or <= 0 ? DefaultLimit : Math.Min(raw.Value, MaxLimit);

    /// <summary>
    /// Elige el recibido para un eNCF. El eNCF solo es único por emisor, así que con candidatos de
    /// más de un RNC sin <paramref name="rncEmisor"/> el resultado es ambiguo. Dentro de un mismo
    /// emisor (reenvíos) gana el más reciente.
    /// </summary>
    public static ReceivedEcfPick Pick(IEnumerable<ReceivedB2BMessage> candidates, string? rncEmisor)
    {
        var list = candidates.ToList();
        var wanted = NormalizeRnc(rncEmisor);

        if (wanted.Length > 0)
        {
            list = list.Where(m => NormalizeRnc(m.RncEmisor) == wanted).ToList();
        }

        if (list.Count == 0)
        {
            return new ReceivedEcfPick(ReceivedEcfPickKind.NotFound, null, []);
        }

        var emisores = list
            .Select(m => NormalizeRnc(m.RncEmisor))
            .Distinct()
            .OrderBy(rnc => rnc, StringComparer.Ordinal)
            .ToList();

        if (emisores.Count > 1)
        {
            return new ReceivedEcfPick(ReceivedEcfPickKind.Ambiguous, null, emisores);
        }

        var latest = list.OrderByDescending(m => m.ReceivedAtUtc).First();
        return new ReceivedEcfPick(ReceivedEcfPickKind.Found, latest, emisores);
    }

    /// <summary>MontoTotal del XML, o null si el XML no se puede leer o no lo trae.</summary>
    public static decimal? ReadTotal(string rawXml)
    {
        try
        {
            var total = XDocument.Parse(rawXml).Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "MontoTotal")?.Value.Trim();

            return decimal.TryParse(total, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static ReceivedEcfListItem ToListItem(ReceivedB2BMessage message) =>
        new(
            message.ENcf,
            NormalizeRnc(message.RncEmisor),
            NcfHelper.TryExtractEcfType(message.ENcf, out var type) ? type : 0,
            ReadTotal(message.RawXml),
            DateTime.SpecifyKind(message.ReceivedAtUtc, DateTimeKind.Utc));

    /// <summary>Ambiente del QR del PDF: el configurado, o Production.</summary>
    public static DgiiEnvironment ResolveEnvironment(string? configured) =>
        !string.IsNullOrWhiteSpace(configured) && Enum.TryParse<DgiiEnvironment>(configured, true, out var parsed)
            ? parsed
            : DgiiEnvironment.Production;

    /// <summary>
    /// Lee el parámetro <c>since</c>. Vacío es válido (sin filtro). Un valor sin zona horaria se
    /// toma como UTC.
    /// </summary>
    public static bool TryParseSince(string? raw, out DateTime? sinceUtc)
    {
        sinceUtc = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return false;
        }

        sinceUtc = DateTime.SpecifyKind(parsed.UtcDateTime, DateTimeKind.Utc);
        return true;
    }
}

public interface IReceivedEcfService
{
    /// <summary>e-CF recibidos de un cliente, del más antiguo al más nuevo, posteriores a <paramref name="sinceUtc"/>.</summary>
    Task<ReceivedEcfListResponse> ListAsync(int clientId, DateTime? sinceUtc, int limit, CancellationToken cancellationToken = default);

    /// <summary>Recibido por eNCF ya normalizado, acotado a un cliente y opcionalmente a un emisor.</summary>
    Task<ReceivedEcfPick> FindAsync(int clientId, string normalizedEncf, string? rncEmisor, CancellationToken cancellationToken = default);
}

public class ReceivedEcfService(IReceivedB2BMessageService messages) : IReceivedEcfService
{
    public async Task<ReceivedEcfListResponse> ListAsync(
        int clientId,
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = messages.Table
            .AsNoTracking()
            .Where(m => m.ClientId == clientId && m.MessageType == MessageType.Ecf);

        if (sinceUtc is not null)
        {
            query = query.Where(m => m.ReceivedAtUtc > sinceUtc.Value);
        }

        // Se pide uno de más para saber si hay más sin hacer un COUNT aparte.
        var page = await query
            .OrderBy(m => m.ReceivedAtUtc)
            .ThenBy(m => m.ReceivedB2BMessageId)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var items = page.Take(limit).Select(ReceivedEcfLogic.ToListItem).ToList();

        return new ReceivedEcfListResponse(items.Count, page.Count > limit, items);
    }

    public async Task<ReceivedEcfPick> FindAsync(
        int clientId,
        string normalizedEncf,
        string? rncEmisor,
        CancellationToken cancellationToken = default)
    {
        var candidates = await messages.Table
            .AsNoTracking()
            .Where(m => m.ClientId == clientId && m.MessageType == MessageType.Ecf && m.ENcf == normalizedEncf)
            .ToListAsync(cancellationToken);

        return ReceivedEcfLogic.Pick(candidates, rncEmisor);
    }
}
```

- [ ] **Step 4: Registrar el servicio**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, debajo de la línea `services.AddTransient<Production.IEcfLookupService, Production.EcfLookupService>();` agregar:

```csharp
        services.AddTransient<Production.IReceivedEcfService, Production.ReceivedEcfService>();
```

- [ ] **Step 5: Ejecutar las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ReceivedEcfLogicTests" 2>&1 | tail -15`
Expected: todas pasan. Si `NcfHelper.TryExtractEcfType` no existe con esa firma, abrir `ZynstormECFPlatform.Common/Utilities/NcfHelper.cs` y usar la firma real (`EcfLookupLogic.NormalizeNcf` la usa como `NcfHelper.TryExtractEcfType(value, out _)`). Si el `Assert.Equal(["102620717", "133009889"], pick.Emisores)` falla por tipos de colección, comparar con `.ToArray()`.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests/Production/ReceivedEcfLogicTests.cs
git commit -m "feat(recibidos): consulta de e-CF recibidos por cliente, eNCF y emisor"
```

---

### Task 4: Endpoints de la API con API key y del panel

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (constructor y tres acciones nuevas antes de `GetSample`)
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/FeController.cs` (constructor y acción PDF después de `DownloadReceivedFile`)

**Interfaces:**
- Consumes: `IReceivedEcfService`, `ReceivedEcfLogic.*`, `EcfLookupLogic.NormalizeNcf`, `ReceivedEcfPdf.Render` (Tasks 1–3).
- Produces (HTTP): los tres endpoints de las Global Constraints y `GET v1/Fe/client/{clientId}/received-files/{id}/pdf`.

- [ ] **Step 1: Controlador de la API con API key**

En `EcfController.cs`:

1. Usings: agregar `using System.Text;`, `using Microsoft.Extensions.Configuration;`, `using ZynstormECFPlatform.Services.Ri.Received;`.
2. Constructor y campos: agregar los parámetros `IReceivedEcfService receivedEcfService, IConfiguration configuration,` antes de `ILogger<EcfController> logger` y los campos:

```csharp
        private readonly IReceivedEcfService _receivedEcfService = receivedEcfService;
        private readonly IConfiguration _configuration = configuration;
```

3. Antes de la acción `GetSample` (`[HttpGet("sample")]`) agregar:

```csharp
        /// <summary>
        /// Lista los e-CF que terceros le enviaron al cliente dueño de la API key. Pensado para
        /// consultarlo cada cierto tiempo: guarde el <c>receivedAtUtc</c> del último ítem y úselo
        /// como <c>since</c> en la próxima consulta. Se conservan 30 días.
        /// </summary>
        [HttpGet("received")]
        [ProducesResponseType(typeof(ReceivedEcfListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetReceived(
            [FromQuery] string? since,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return ReceivedRequiresApiKey();
            }

            if (!ReceivedEcfLogic.TryParseSince(since, out var sinceUtc))
            {
                return BadRequest(new
                {
                    found = false,
                    message = "El parámetro 'since' no es una fecha válida. Use el formato ISO 8601, por ejemplo 2026-10-14T12:30:00Z."
                });
            }

            var result = await _receivedEcfService.ListAsync(
                clientId, sinceUtc, ReceivedEcfLogic.ClampLimit(limit), cancellationToken);

            return Ok(result);
        }

        /// <summary>Descarga el PDF de un e-CF recibido, identificado por su eNCF.</summary>
        [HttpGet("received/{eNcf}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetReceivedPdf(
            string eNcf,
            [FromQuery] string? rncEmisor,
            CancellationToken cancellationToken)
        {
            var (error, message) = await ResolveReceivedAsync(eNcf, rncEmisor, cancellationToken);

            if (error is not null)
            {
                return error;
            }

            try
            {
                var environment = ReceivedEcfLogic.ResolveEnvironment(_configuration["EcfXmlValidation:TargetDgiiEnvironment"]);
                var pdf = ReceivedEcfPdf.Render(message!.RawXml, environment);

                return File(pdf, "application/pdf", $"ECF_{message.ENcf}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo generar el PDF del e-CF recibido {ENcf}.", message!.ENcf);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                {
                    found = true,
                    message = "El XML recibido no se pudo interpretar para generar el PDF. Descargue el XML original."
                });
            }
        }

        /// <summary>Descarga el XML original de un e-CF recibido, identificado por su eNCF.</summary>
        [HttpGet("received/{eNcf}/xml")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/xml")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetReceivedXml(
            string eNcf,
            [FromQuery] string? rncEmisor,
            CancellationToken cancellationToken)
        {
            var (error, message) = await ResolveReceivedAsync(eNcf, rncEmisor, cancellationToken);

            return error ?? File(Encoding.UTF8.GetBytes(message!.RawXml), "application/xml", $"ECF_{message.ENcf}.xml");
        }

        private IActionResult ReceivedRequiresApiKey() =>
            StatusCode(StatusCodes.Status403Forbidden, new
            {
                found = false,
                message = "Esta consulta requiere una API key válida en el header 'X-Api-Key'."
            });

        /// <summary>
        /// Valida el eNCF y busca el recibido del cliente. Devuelve la respuesta de error lista
        /// (403, 400, 404 o 409) o el mensaje encontrado.
        /// </summary>
        private async Task<(IActionResult? Error, ZynstormECFPlatform.Core.Entities.ReceivedB2BMessage? Message)> ResolveReceivedAsync(
            string eNcf,
            string? rncEmisor,
            CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return (ReceivedRequiresApiKey(), null);
            }

            var normalized = EcfLookupLogic.NormalizeNcf(eNcf);

            if (normalized is null)
            {
                return (BadRequest(new
                {
                    found = false,
                    message = "El eNCF no es válido: debe ser 'E' seguido de 12 dígitos (por ejemplo E310000000001)."
                }), null);
            }

            var pick = await _receivedEcfService.FindAsync(clientId, normalized, rncEmisor, cancellationToken);

            return pick.Kind switch
            {
                ReceivedEcfPickKind.NotFound => (NotFound(new
                {
                    found = false,
                    message = "No hay un e-CF recibido con ese eNCF. Los recibidos se conservan 30 días."
                }), null),
                ReceivedEcfPickKind.Ambiguous => (Conflict(new
                {
                    found = true,
                    message = "Hay recibidos con ese eNCF de más de un emisor. Indique el RNC del emisor con el parámetro 'rncEmisor'.",
                    emisores = pick.Emisores
                }), null),
                _ => (null, pick.Message)
            };
        }
```

(`EcfLookupLogic` ya está disponible por el `using ZynstormECFPlatform.Services.Production;` existente.)

- [ ] **Step 2: Endpoint del panel**

En `FeController.cs`:

1. Usings: agregar `using Microsoft.Extensions.Configuration;`, `using ZynstormECFPlatform.Services.Production;`, `using ZynstormECFPlatform.Services.Ri.Received;`.
2. Campo `private readonly IConfiguration _configuration;`, parámetro `IConfiguration configuration` al final del constructor y `_configuration = configuration;` en su cuerpo.
3. Después de `DownloadReceivedFile` (termina en la línea ~672) agregar:

```csharp
    /// <summary>
    /// Descarga como PDF un e-CF recibido, generado a partir del XML guardado. Solo e-CF: las
    /// aprobaciones comerciales no tienen representación impresa.
    /// </summary>
    [Authorize]
    [HttpGet("client/{clientId}/received-files/{id:int}/pdf")]
    public async Task<IActionResult> DownloadReceivedFilePdf(int clientId, int id)
    {
        try
        {
            var message = await _receivedB2BMessageService.GetByAsync(m => m.ReceivedB2BMessageId == id && m.ClientId == clientId);

            if (message == null)
            {
                return NotFound(new { error = "El comprobante solicitado no existe." });
            }

            if (message.MessageType != MessageType.Ecf)
            {
                return BadRequest(new { error = "Solo los e-CF recibidos se pueden descargar como PDF." });
            }

            var environment = ReceivedEcfLogic.ResolveEnvironment(_configuration["EcfXmlValidation:TargetDgiiEnvironment"]);

            byte[] pdf;
            try
            {
                pdf = ReceivedEcfPdf.Render(message.RawXml, environment);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo generar el PDF del comprobante recibido {Id}.", id);
                return UnprocessableEntity(new { error = "El XML recibido no se pudo interpretar para generar el PDF. Descargue el XML original." });
            }

            return File(pdf, "application/pdf", $"ECF_{message.ENcf}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for B2B message {Id} for client {ClientId}", id, clientId);
            return StatusCode(500, new { error = "Error interno al generar el PDF del comprobante." });
        }
    }
```

Nota para quien ejecute: `FeController` es `[AllowAnonymous]` completo (la DGII llama a `RecepcionEcf` sin sesión), y los endpoints de recibidos del panel que ya existían (`received-files`, descarga y envío por correo) heredan eso y no exigen sesión. Este endpoint nuevo sí lleva `[Authorize]`, porque el panel llama con `credentials: "include"` como el resto de controladores protegidos. No tocar los endpoints existentes en este plan; queda anotado como pendiente aparte.

- [ ] **Step 3: Compilar y ejecutar toda la suite**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
dotnet build ZynstormECFPlatform.sln 2>&1 | tail -8
dotnet test ZynstormECFPlatform.Tests 2>&1 | tail -8
```
Expected: `Build succeeded` sin errores y la suite completa en verde (antes de este trabajo: ~321 pruebas en `Staging`, más las nuevas). Si el nombre de la solución es otro, usar `ls *.sln*`. Si algo en `Program.cs` o un test construye `EcfController`/`FeController` a mano, ajustar los argumentos del constructor.

- [ ] **Step 4: Verificar que `[Authorize]` no rompe el esquema de autenticación**

Run: `grep -n "AddAuthentication\|AddJwtBearer\|OnMessageReceived\|Cookies" ZynstormECFPlatform.Web.Api/Program.cs | head`
Expected: confirmar que el JWT se lee de la cookie (algo como `context.Token = context.Request.Cookies[...]`) o del header. Si solo se lee del header `Authorization` y el panel no lo manda en este fetch, el PDF devolvería 401: en ese caso quitar `[Authorize]` de la acción, dejarla igual que sus vecinas y decirlo en el reporte final. Si el panel autentica por cookie (como el resto de sus llamadas), dejar `[Authorize]`.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Web.Api
git commit -m "feat(recibidos): endpoints de consulta, PDF y XML por API key y PDF para el panel"
```

---

### Task 5: Replicar el backend a `feature/emit-idempotente`

**Files:** ninguno nuevo (cherry-pick).

- [ ] **Step 1: Listar los commits a replicar**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
git status --short | head -3
git log --oneline -6
```
Expected: árbol limpio y los 4 commits de las Tasks 1 a 4 al tope (más el del spec/plan si se commiteó aparte).

- [ ] **Step 2: Cherry-pick en la otra rama**

Run (sustituir los hashes `H1..H4` por los de las Tasks 1 a 4, del más antiguo al más nuevo; incluir el commit del spec y el plan):
```bash
git checkout feature/emit-idempotente
git cherry-pick H_SPEC H1 H2 H3 H4
```
Expected: sin conflictos. Si hay conflicto en `EcfController.cs` (esa rama tiene `emit`, `by-ncf` y el re-encolado), resolver conservando ambos bloques: los endpoints existentes y los tres nuevos, con los dos servicios inyectados.

- [ ] **Step 3: Compilar y probar en esa rama**

Run: `dotnet test ZynstormECFPlatform.Tests 2>&1 | tail -6`
Expected: verde (370 de antes más las nuevas).

- [ ] **Step 4: Volver a `Staging`**

Run: `git checkout Staging && git branch --show-current`
Expected: `Staging`.

---

## Parte B: Panel (frontend)

### Task 6: Botón «Descargar PDF» en Recibidas

**Files:**
- Modify: `ZynstormECFPlatform-FrontEnd/services/fe.service.ts` (después de `downloadReceivedFileRaw`)
- Modify: `ZynstormECFPlatform-FrontEnd/app/facturas/page.tsx` (import ~línea 70, estado ~línea 471, handler ~línea 550, botón ~línea 1212)

**Interfaces:**
- Consumes: `GET {API}/v1/Fe/client/{clientId}/received-files/{id}/pdf` (Task 4).
- Produces: `downloadReceivedFilePdf(clientId: number, id: number): Promise<{ blob: Blob; fileName: string }>`.

- [ ] **Step 1: Rama del panel**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd && git branch --show-current && git status --short | head -5 && git log --oneline -3
```
Expected: la rama actual es `main` y `main` ya contiene `Staging`. Trabajar en `Staging` (ahí se desarrolla): `git checkout Staging && git merge --ff-only main` si hace falta para partir de lo último, y confirmar con `git branch --show-current`. Si el árbol tiene cambios sin commitear que no son de esta tarea, parar y avisar.

- [ ] **Step 2: Función de servicio**

En `services/fe.service.ts`, después de `downloadReceivedFileRaw`, agregar:

```ts
export const downloadReceivedFilePdf = async (
  clientId: number,
  id: number
): Promise<{ blob: Blob; fileName: string }> => {
  const response = await fetch(`${FE_URL}/client/${clientId}/received-files/${id}/pdf`, {
    credentials: "include",
  })

  if (!response.ok) {
    let detail = response.statusText
    try {
      const body = await response.json()
      if (body?.error) detail = body.error
    } catch {
      // El cuerpo no era JSON: se queda con el statusText.
    }
    throw new Error(detail)
  }

  const blob = await response.blob()

  let fileName = `ECF_${id}.pdf`
  const contentDisposition = response.headers.get("content-disposition")
  if (contentDisposition) {
    const match = contentDisposition.match(/filename\*?=(?:UTF-8'')?([^;]+)/i)
    if (match && match[1]) {
      fileName = decodeURIComponent(match[1].replace(/"/g, ""))
    }
  }

  return { blob, fileName }
}
```

- [ ] **Step 3: Import, estado y handler en la página**

En `app/facturas/page.tsx`:

1. Cambiar el import de la línea ~70 a:

```tsx
import { getReceivedFiles, downloadReceivedFileRaw, downloadReceivedFilePdf, sendReceivedFileEmail, ReceivedB2BFile } from "@/services/fe.service"
```

2. Junto a `const [downloadingXmlId, setDownloadingXmlId] = useState<number | null>(null)` (línea ~471) agregar:

```tsx
  const [downloadingPdfId, setDownloadingPdfId] = useState<number | null>(null)
```

3. Después de `handleDownloadXmlB2B` (termina ~línea 551) agregar:

```tsx
  const handleDownloadPdfB2B = async (file: ReceivedB2BFile) => {
    if (!selectedClientId) return
    setDownloadingPdfId(file.receivedB2BMessageId)
    setEmailStatusMessage(null)
    try {
      const { blob, fileName } = await downloadReceivedFilePdf(Number(selectedClientId), file.receivedB2BMessageId)
      const url = URL.createObjectURL(blob)
      const link = document.createElement("a")
      link.href = url
      link.download = fileName
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      URL.revokeObjectURL(url)
    } catch (err) {
      console.error("Error downloading PDF B2B:", err)
      alert(err instanceof Error && err.message ? `Error al descargar el PDF: ${err.message}` : "Error al descargar el PDF.")
    } finally {
      setDownloadingPdfId(null)
    }
  }
```

- [ ] **Step 4: Botón en la columna Acciones**

En la fila de recibidos, entre el botón «Descargar XML» (`title="Descargar XML"`) y el de correo, agregar (solo para e-CF; `FileText` ya está importado):

```tsx
                                {file.messageType === "Ecf" && (
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-7 w-7 p-0"
                                    onClick={() => handleDownloadPdfB2B(file)}
                                    disabled={downloadingPdfId !== null}
                                    title="Descargar PDF"
                                  >
                                    {downloadingPdfId === file.receivedB2BMessageId ? (
                                      <RefreshCw className="h-4 w-4 animate-spin" />
                                    ) : (
                                      <FileText className="h-4 w-4" />
                                    )}
                                  </Button>
                                )}
```

- [ ] **Step 5: Tipos y lint**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
npx tsc --noEmit 2>&1 | tail -10
npx eslint app/facturas/page.tsx services/fe.service.ts 2>&1 | tail -10
```
Expected: sin errores nuevos (si hay errores previos ajenos a estos archivos, anotarlos y no tocarlos).

- [ ] **Step 6: Verificación manual**

Si la API local no se puede levantar (la de desarrollo apunta a una BD remota), probar el PDF sin UI contra el renderizador: ya cubierto por las pruebas de la Task 2. Dejar anotado en el reporte final que el botón queda pendiente de probar a mano en Staging con un recibido real.

- [ ] **Step 7: Commit**

```bash
git add services/fe.service.ts app/facturas/page.tsx
git commit -m "feat(facturas): descargar en PDF los e-CF recibidos"
```

---

## Parte C: Librería (`C:\Projects\Zynstorm.DGII.ECF`, rama `feature/emit-idempotente`)

### Task 7: Contratos, rutas y cliente

**Files:**
- Create: `src/Zynstorm.DGII.ECF/Contracts/EcfReceivedItem.cs`
- Create: `src/Zynstorm.DGII.ECF/Client/EcfReceivedStatus.cs`
- Create: `src/Zynstorm.DGII.ECF/Client/EcfReceivedResults.cs`
- Create: `src/Zynstorm.DGII.ECF/Client/EcfReceivedPayloads.cs`
- Modify: `src/Zynstorm.DGII.ECF/Client/EcfEndpoints.cs`, `Client/IEcfClient.cs`, `Client/EcfClient.cs`
- Modify: `tests/Zynstorm.DGII.ECF.Tests/Fakes/ScriptedHandler.cs`
- Test: `tests/Zynstorm.DGII.ECF.Tests/ReceivedTests.cs`

**Interfaces:**
- Consumes: endpoints de la Task 4; `EcfEndpoints.EnsureAllowed`, `EcfClient.IsTransportException`, `EcfClient.Transport`-style helpers (privados, mismo archivo), `EcfJson.Deserialize<T>`.
- Produces (público):
  - `EcfReceivedStatus { Ok = 0, NotFound = 1, Ambiguous = 2, InvalidRequest = 3, Unauthorized = 4, TransportError = 5, ContractError = 6 }`.
  - `EcfReceivedItem { string ENcf; string RncEmisor; int EcfType; decimal? Total; DateTime ReceivedAtUtc }`.
  - `EcfReceivedList { EcfReceivedStatus Status; string Message; string Detail; bool IsTimeout; IReadOnlyList<EcfReceivedItem> Items; int Count; bool HasMore; DateTime? NextSince }`.
  - `EcfReceivedFile { EcfReceivedStatus Status; string Message; string Detail; bool IsTimeout; byte[] Content; string FileName; string ContentType; string Text; IReadOnlyList<string> Candidates }`.
  - `IEcfClient`/`EcfClient`: `GetReceivedAsync(DateTime? sinceUtc = null, int limit = 100, CancellationToken)`, `GetReceived(DateTime? sinceUtc = null, int limit = 100)`, `GetReceivedPdfAsync(string eNcf, string rncEmisor = null, CancellationToken)`, `GetReceivedPdf(string eNcf, string rncEmisor = null)`, `GetReceivedXmlAsync(...)`, `GetReceivedXml(...)`.

- [ ] **Step 1: Rama y línea base**

Run:
```bash
cd /c/Projects/Zynstorm.DGII.ECF && git branch --show-current && git status --short | head -5
dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | tail -5
```
Expected: `feature/emit-idempotente`, árbol limpio y la suite en verde (anotar cuántas pruebas hay).

- [ ] **Step 2: Soporte de cuerpos binarios y cabeceras en el handler de pruebas**

En `tests/Zynstorm.DGII.ECF.Tests/Fakes/ScriptedHandler.cs`, después del método `Responds` agregar:

```csharp
    /// <summary>Respuesta binaria (un PDF, por ejemplo) con su tipo y, si se indica, su nombre de archivo.</summary>
    internal ScriptedHandler RespondsBytes(HttpStatusCode statusCode, byte[] body, string contentType, string fileName = null)
    {
        _script.Enqueue(() =>
        {
            var response = new HttpResponseMessage(statusCode) { Content = new ByteArrayContent(body) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

            if (fileName != null)
                response.Content.Headers.ContentDisposition =
                    new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = fileName };

            return response;
        });
        return this;
    }
```

- [ ] **Step 3: Escribir las pruebas que fallan**

Create `tests/Zynstorm.DGII.ECF.Tests/ReceivedTests.cs`:

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
/// Consulta y descarga de los e-CF que terceros le enviaron al cliente: lista, PDF y XML.
/// </summary>
public class ReceivedTests
{
    private const string Staging = "https://ecfstaging.zynstorm.com/api/v1";

    private static EcfClient Client(ScriptedHandler handler) =>
        new(new EcfOptions(Staging, "key-de-prueba"), handler, ownsHttpClient: false);

    private const string ListBody = """
        {
          "count": 2,
          "hasMore": true,
          "items": [
            { "eNcf": "E310000000402", "rncEmisor": "133009889", "ecfType": 31, "total": 6001.00, "receivedAtUtc": "2026-10-14T12:00:00Z" },
            { "eNcf": "E340000000002", "rncEmisor": "132878191", "ecfType": 34, "total": null,    "receivedAtUtc": "2026-10-14T13:30:00Z" }
          ]
        }
        """;

    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7 prueba");

    // ---------------------------------------------------------------- lista

    [Fact]
    public async Task La_Lista_Se_Lee_Con_Sus_Datos_Y_NextSince()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, ListBody);

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(EcfReceivedStatus.Ok, list.Status);
        Assert.Equal(2, list.Count);
        Assert.True(list.HasMore);
        Assert.Equal("E310000000402", list.Items[0].ENcf);
        Assert.Equal("133009889", list.Items[0].RncEmisor);
        Assert.Equal(31, list.Items[0].EcfType);
        Assert.Equal(6001.00m, list.Items[0].Total);
        Assert.Null(list.Items[1].Total);
        Assert.Equal(DateTimeKind.Utc, list.Items[1].ReceivedAtUtc.Kind);
        Assert.Equal(new DateTime(2026, 10, 14, 13, 30, 0, DateTimeKind.Utc), list.NextSince);
    }

    [Fact]
    public async Task Una_Lista_Vacia_No_Tiene_NextSince()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "{\"count\":0,\"hasMore\":false,\"items\":[]}");

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(EcfReceivedStatus.Ok, list.Status);
        Assert.Empty(list.Items);
        Assert.Null(list.NextSince);
    }

    [Fact]
    public async Task La_Lista_Va_A_La_Ruta_Received_Con_Since_En_UTC_Y_Limit()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "{\"count\":0,\"hasMore\":false,\"items\":[]}");
        var since = new DateTime(2026, 10, 14, 12, 30, 0, DateTimeKind.Utc);

        await Client(handler).GetReceivedAsync(since, 50);

        var url = handler.Urls.Single();
        Assert.StartsWith(Staging + "/Ecf/received?", url);
        Assert.Contains("since=2026-10-14T12%3A30%3A00.0000000Z", url);
        Assert.Contains("limit=50", url);
    }

    [Fact]
    public async Task Sin_Since_No_Se_Envia_El_Parametro()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "{\"count\":0,\"hasMore\":false,\"items\":[]}");

        await Client(handler).GetReceivedAsync();

        var url = handler.Urls.Single();
        Assert.DoesNotContain("since=", url);
        Assert.Contains("limit=100", url);
    }

    [Fact]
    public async Task Un_Since_Local_Se_Convierte_A_UTC()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "{\"count\":0,\"hasMore\":false,\"items\":[]}");
        var local = new DateTime(2026, 10, 14, 8, 30, 0, DateTimeKind.Local);

        await Client(handler).GetReceivedAsync(local);

        var expected = Uri.EscapeDataString(local.ToUniversalTime().ToString("o"));
        Assert.Contains("since=" + expected, handler.Urls.Single());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, EcfReceivedStatus.InvalidRequest)]
    [InlineData(HttpStatusCode.Unauthorized, EcfReceivedStatus.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, EcfReceivedStatus.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, EcfReceivedStatus.TransportError)]
    [InlineData(HttpStatusCode.BadGateway, EcfReceivedStatus.TransportError)]
    public async Task La_Lista_Traduce_Los_Codigos_De_Error(HttpStatusCode code, EcfReceivedStatus expected)
    {
        var handler = new ScriptedHandler().Responds(code, "{\"message\":\"motivo del servidor\"}");

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(expected, list.Status);
        Assert.Empty(list.Items);
        Assert.False(string.IsNullOrWhiteSpace(list.Message));
    }

    [Fact]
    public async Task Un_Cuerpo_Que_No_Es_Json_Es_ContractError()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "<html>no es json</html>");

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(EcfReceivedStatus.ContractError, list.Status);
    }

    [Fact]
    public async Task Una_Caida_De_Red_Es_TransportError_Sin_Excepcion()
    {
        var handler = new ScriptedHandler().Throws(new HttpRequestException("sin red"));

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(EcfReceivedStatus.TransportError, list.Status);
        Assert.False(list.IsTimeout);
    }

    [Fact]
    public async Task Un_Timeout_Se_Marca_Como_Tal()
    {
        var handler = new ScriptedHandler().Throws(new TaskCanceledException("timeout"));

        var list = await Client(handler).GetReceivedAsync();

        Assert.Equal(EcfReceivedStatus.TransportError, list.Status);
        Assert.True(list.IsTimeout);
    }

    [Fact]
    public void La_Version_Sincrona_Devuelve_Lo_Mismo()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, ListBody);

        var list = Client(handler).GetReceived();

        Assert.Equal(EcfReceivedStatus.Ok, list.Status);
        Assert.Equal(2, list.Items.Count);
    }

    // ---------------------------------------------------------------- PDF y XML

    [Fact]
    public async Task El_Pdf_Se_Descarga_Con_Su_Nombre_Y_Tipo()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Pdf, "application/pdf", "ECF_E310000000402.pdf");

        var file = await Client(handler).GetReceivedPdfAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.Ok, file.Status);
        Assert.Equal(Pdf, file.Content);
        Assert.Equal("ECF_E310000000402.pdf", file.FileName);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(Staging + "/Ecf/received/E310000000402/pdf", handler.Urls.Single());
    }

    [Fact]
    public async Task El_Pdf_Sin_Nombre_En_La_Respuesta_Usa_Uno_Por_Defecto()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Pdf, "application/pdf");

        var file = await Client(handler).GetReceivedPdfAsync("E310000000402");

        Assert.Equal("ECF_E310000000402.pdf", file.FileName);
    }

    [Fact]
    public async Task El_Xml_Se_Descarga_Y_Se_Puede_Leer_Como_Texto()
    {
        var xml = "<?xml version=\"1.0\"?><ECF><eNCF>E310000000402</eNCF></ECF>";
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Encoding.UTF8.GetBytes(xml), "application/xml", "ECF_E310000000402.xml");

        var file = await Client(handler).GetReceivedXmlAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.Ok, file.Status);
        Assert.Equal(xml, file.Text);
        Assert.Equal("ECF_E310000000402.xml", file.FileName);
        Assert.Equal(Staging + "/Ecf/received/E310000000402/xml", handler.Urls.Single());
    }

    [Fact]
    public async Task El_RncEmisor_Va_En_La_Consulta_Solo_Con_Digitos()
    {
        var handler = new ScriptedHandler().RespondsBytes(HttpStatusCode.OK, Pdf, "application/pdf");

        await Client(handler).GetReceivedPdfAsync("E310000000402", "1-33-00988-9");

        Assert.EndsWith("/Ecf/received/E310000000402/pdf?rncEmisor=133009889", handler.Urls.Single());
    }

    [Fact]
    public async Task Un_404_Es_NotFound_Y_Lleva_El_Mensaje_De_La_Plataforma()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.NotFound,
            "{\"found\":false,\"message\":\"No hay un e-CF recibido con ese eNCF.\"}");

        var file = await Client(handler).GetReceivedPdfAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.NotFound, file.Status);
        Assert.Null(file.Content);
        Assert.Contains("No hay un e-CF recibido", file.Message);
    }

    [Fact]
    public async Task Un_409_Es_Ambiguous_Y_Trae_Los_Emisores()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.Conflict,
            "{\"found\":true,\"message\":\"Indique el RNC del emisor.\",\"emisores\":[\"102620717\",\"133009889\"]}");

        var file = await Client(handler).GetReceivedXmlAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.Ambiguous, file.Status);
        Assert.Equal(new[] { "102620717", "133009889" }, file.Candidates.ToArray());
    }

    [Fact]
    public async Task Un_422_Del_Pdf_Es_ContractError_Con_El_Motivo()
    {
        var handler = new ScriptedHandler().Responds((HttpStatusCode)422,
            "{\"found\":true,\"message\":\"El XML recibido no se pudo interpretar.\"}");

        var file = await Client(handler).GetReceivedPdfAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.ContractError, file.Status);
        Assert.Contains("no se pudo interpretar", file.Message);
    }

    [Fact]
    public async Task Un_Error_De_Red_En_La_Descarga_No_Lanza()
    {
        var handler = new ScriptedHandler().Throws(new HttpRequestException("sin red"));

        var file = await Client(handler).GetReceivedPdfAsync("E310000000402");

        Assert.Equal(EcfReceivedStatus.TransportError, file.Status);
        Assert.Null(file.Content);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Un_ENcf_Vacio_Lanza_ArgumentException(string eNcf)
    {
        var handler = new ScriptedHandler();

        await Assert.ThrowsAsync<ArgumentException>(() => Client(handler).GetReceivedPdfAsync(eNcf));
        await Assert.ThrowsAsync<ArgumentException>(() => Client(handler).GetReceivedXmlAsync(eNcf));
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task Un_Limit_Fuera_De_Rango_Se_Ajusta_Antes_De_Enviar()
    {
        var handler = new ScriptedHandler().Responds(HttpStatusCode.OK, "{\"count\":0,\"hasMore\":false,\"items\":[]}");

        await Client(handler).GetReceivedAsync(null, 0);

        Assert.Contains("limit=100", handler.Urls.Single());
    }
}
```

- [ ] **Step 4: Ejecutar para verificar que falla**

Run: `dotnet test tests/Zynstorm.DGII.ECF.Tests --filter "FullyQualifiedName~ReceivedTests" 2>&1 | tail -10`
Expected: error de compilación (`EcfReceivedStatus`, `GetReceivedAsync`... no existen).

- [ ] **Step 5: Contratos y tipos públicos**

Create `src/Zynstorm.DGII.ECF/Contracts/EcfReceivedItem.cs`:

```csharp
using System;
using Newtonsoft.Json;

namespace Zynstorm.DGII.ECF.Contracts
{
    /// <summary>
    /// Un e-CF que un tercero le envio al cliente. Es el renglon de <c>GetReceived</c>: con el
    /// <see cref="ENcf"/> (y el <see cref="RncEmisor"/> si hace falta) se pide el PDF o el XML.
    /// </summary>
    public class EcfReceivedItem
    {
        [JsonProperty("eNcf")]
        public string ENcf { get; set; }

        /// <summary>RNC del emisor, solo digitos. El eNCF solo es unico por emisor.</summary>
        [JsonProperty("rncEmisor")]
        public string RncEmisor { get; set; }

        /// <summary>Tipo de e-CF: 31, 32, 33, 34, 41, 43, 44, 45, 46 o 47. Cero si no se pudo determinar.</summary>
        [JsonProperty("ecfType")]
        public int EcfType { get; set; }

        /// <summary>Monto total del comprobante; <c>null</c> si el XML no se pudo leer.</summary>
        [JsonProperty("total")]
        public decimal? Total { get; set; }

        /// <summary>Cuando lo recibio la plataforma, en UTC.</summary>
        [JsonProperty("receivedAtUtc")]
        public DateTime ReceivedAtUtc { get; set; }
    }
}
```

Create `src/Zynstorm.DGII.ECF/Client/EcfReceivedStatus.cs`:

```csharp
namespace Zynstorm.DGII.ECF.Client
{
    /// <summary>Resultado de consultar o descargar un comprobante recibido.</summary>
    public enum EcfReceivedStatus
    {
        /// <summary>Todo bien: la lista o el archivo vienen en el resultado.</summary>
        Ok = 0,

        /// <summary>La plataforma no tiene ese comprobante recibido. Los recibidos se conservan 30 dias.</summary>
        NotFound = 1,

        /// <summary>
        /// Hay recibidos con ese eNCF de mas de un emisor. Vuelva a pedirlo indicando el RNC del
        /// emisor; los candidatos vienen en <see cref="EcfReceivedFile.Candidates"/>.
        /// </summary>
        Ambiguous = 2,

        /// <summary>La plataforma rechazo la peticion: eNCF o fecha con formato invalido.</summary>
        InvalidRequest = 3,

        /// <summary>La API key no es valida o no esta activa.</summary>
        Unauthorized = 4,

        /// <summary>No hubo respuesta (red, timeout) o la plataforma fallo. Reintente mas tarde.</summary>
        TransportError = 5,

        /// <summary>La plataforma respondio algo que esta version no entiende, o no pudo generar el PDF.</summary>
        ContractError = 6
    }
}
```

Create `src/Zynstorm.DGII.ECF/Client/EcfReceivedResults.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using Zynstorm.DGII.ECF.Contracts;

namespace Zynstorm.DGII.ECF.Client
{
    /// <summary>Lo que devuelve <c>GetReceived</c>: los e-CF recibidos, del mas antiguo al mas nuevo.</summary>
    public sealed class EcfReceivedList
    {
        internal EcfReceivedList(
            EcfReceivedStatus status,
            string message,
            string detail,
            IReadOnlyList<EcfReceivedItem> items,
            bool hasMore,
            bool timedOut)
        {
            Status = status;
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
            Items = items ?? new List<EcfReceivedItem>();
            HasMore = hasMore;
            IsTimeout = timedOut;
        }

        public EcfReceivedStatus Status { get; }

        /// <summary>Una linea, apta para mostrarle al usuario.</summary>
        public string Message { get; }

        /// <summary>Detalle tecnico para el log.</summary>
        public string Detail { get; }

        /// <summary>La plataforma no respondio dentro del limite de tiempo.</summary>
        public bool IsTimeout { get; }

        public IReadOnlyList<EcfReceivedItem> Items { get; }

        public int Count { get { return Items.Count; } }

        /// <summary>Hay mas comprobantes despues del ultimo: vuelva a consultar con <see cref="NextSince"/>.</summary>
        public bool HasMore { get; }

        /// <summary>
        /// Valor para el <c>sinceUtc</c> de la proxima consulta: la fecha de recepcion del ultimo
        /// comprobante de esta lista. <c>null</c> si la lista llego vacia (conserve el valor anterior).
        /// </summary>
        public DateTime? NextSince
        {
            get { return Items.Count == 0 ? (DateTime?)null : Items[Items.Count - 1].ReceivedAtUtc; }
        }
    }

    /// <summary>Lo que devuelve <c>GetReceivedPdf</c> y <c>GetReceivedXml</c>.</summary>
    public sealed class EcfReceivedFile
    {
        internal EcfReceivedFile(
            EcfReceivedStatus status,
            string message,
            string detail,
            byte[] content,
            string fileName,
            string contentType,
            IReadOnlyList<string> candidates,
            bool timedOut)
        {
            Status = status;
            Message = message ?? string.Empty;
            Detail = detail ?? string.Empty;
            Content = content;
            FileName = fileName;
            ContentType = contentType;
            Candidates = candidates ?? new List<string>();
            IsTimeout = timedOut;
        }

        public EcfReceivedStatus Status { get; }

        /// <summary>Una linea, apta para mostrarle al usuario.</summary>
        public string Message { get; }

        /// <summary>Detalle tecnico para el log.</summary>
        public string Detail { get; }

        /// <summary>La plataforma no respondio dentro del limite de tiempo.</summary>
        public bool IsTimeout { get; }

        /// <summary>Los bytes del archivo; <c>null</c> si <see cref="Status"/> no es <see cref="EcfReceivedStatus.Ok"/>.</summary>
        public byte[] Content { get; }

        /// <summary>Nombre sugerido, por ejemplo <c>ECF_E310000000402.pdf</c>.</summary>
        public string FileName { get; }

        public string ContentType { get; }

        /// <summary>Los bytes decodificados como UTF-8: el XML, cuando se pidio el XML. <c>null</c> sin contenido.</summary>
        public string Text
        {
            get { return Content == null ? null : Encoding.UTF8.GetString(Content); }
        }

        /// <summary>RNC de los emisores cuando <see cref="Status"/> es <see cref="EcfReceivedStatus.Ambiguous"/>.</summary>
        public IReadOnlyList<string> Candidates { get; }
    }
}
```

Create `src/Zynstorm.DGII.ECF/Client/EcfReceivedPayloads.cs`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using Zynstorm.DGII.ECF.Contracts;

namespace Zynstorm.DGII.ECF.Client
{
    /// <summary>
    /// La respuesta de <c>GET Ecf/received</c>. Es interna: quien integra recibe un
    /// <see cref="EcfReceivedList"/>. Cada propiedad lleva su nombre JSON explicito porque el
    /// ofuscador renombra los miembros de las clases internas (ver EcfLookupPayload).
    /// </summary>
    internal sealed class EcfReceivedListPayload
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("hasMore")]
        public bool HasMore { get; set; }

        [JsonProperty("items")]
        public List<EcfReceivedItem> Items { get; set; }
    }

    /// <summary>El cuerpo de error de los endpoints de recibidos (404, 409, 400, 422).</summary>
    internal sealed class EcfReceivedErrorPayload
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("emisores")]
        public List<string> Emisores { get; set; }
    }
}
```

- [ ] **Step 6: Rutas**

En `Client/EcfEndpoints.cs`, junto a `ByNcf`, agregar:

```csharp
        /// <summary>GET de la lista de e-CF recibidos. <paramref name="sinceUtc"/> es opcional.</summary>
        internal static string Received(DateTime? sinceUtc, int limit)
        {
            var query = new System.Text.StringBuilder("Ecf/received?");

            if (sinceUtc.HasValue)
            {
                var utc = sinceUtc.Value.Kind == DateTimeKind.Local
                    ? sinceUtc.Value.ToUniversalTime()
                    : DateTime.SpecifyKind(sinceUtc.Value, DateTimeKind.Utc);

                query.Append("since=").Append(Uri.EscapeDataString(utc.ToString("o"))).Append('&');
            }

            return query.Append("limit=").Append(limit).ToString();
        }

        /// <summary>GET del PDF o del XML de un recibido. El RNC va solo con digitos.</summary>
        internal static string ReceivedFile(string eNcf, string kind, string rncEmisor)
        {
            var path = "Ecf/received/" + Uri.EscapeDataString(eNcf) + "/" + kind;
            var digits = OnlyDigits(rncEmisor);

            return digits.Length == 0 ? path : path + "?rncEmisor=" + digits;
        }

        private static string OnlyDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var builder = new System.Text.StringBuilder();

            foreach (var character in value)
                if (character >= '0' && character <= '9')
                    builder.Append(character);

            return builder.ToString();
        }
```

- [ ] **Step 7: Interfaz**

En `Client/IEcfClient.cs`, antes del cierre de la interfaz, agregar (con `using System;` ya presente o agregarlo):

```csharp
        /// <summary>
        /// Lista los e-CF que terceros le enviaron al cliente, del mas antiguo al mas nuevo. Pensado
        /// para consultarlo cada cierto tiempo: guarde <see cref="EcfReceivedList.NextSince"/> y
        /// pasela como <paramref name="sinceUtc"/> en la proxima consulta. La plataforma conserva los
        /// recibidos 30 dias. <paramref name="limit"/> va de 1 a 200 (100 si es invalido).
        /// No lanza excepcion por red, timeout ni respuesta del servidor.
        /// </summary>
        Task<EcfReceivedList> GetReceivedAsync(System.DateTime? sinceUtc = null, int limit = 100, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Version sincrona de <see cref="GetReceivedAsync"/>.</summary>
        EcfReceivedList GetReceived(System.DateTime? sinceUtc = null, int limit = 100);

        /// <summary>
        /// Descarga el PDF de un e-CF recibido. <paramref name="rncEmisor"/> solo hace falta si hay
        /// recibidos con ese eNCF de mas de un emisor (<see cref="EcfReceivedStatus.Ambiguous"/>).
        /// </summary>
        /// <exception cref="System.ArgumentException">Si el eNCF viene vacio.</exception>
        Task<EcfReceivedFile> GetReceivedPdfAsync(string eNcf, string rncEmisor = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Version sincrona de <see cref="GetReceivedPdfAsync"/>.</summary>
        EcfReceivedFile GetReceivedPdf(string eNcf, string rncEmisor = null);

        /// <summary>Descarga el XML original de un e-CF recibido. Mismas reglas que el PDF.</summary>
        Task<EcfReceivedFile> GetReceivedXmlAsync(string eNcf, string rncEmisor = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>Version sincrona de <see cref="GetReceivedXmlAsync"/>.</summary>
        EcfReceivedFile GetReceivedXml(string eNcf, string rncEmisor = null);
```

- [ ] **Step 8: Implementación en `EcfClient`**

En `Client/EcfClient.cs`, justo después de `GetByNcf(string eNcf)` (línea ~257), agregar:

```csharp
        // -----------------------------------------------------------------------------------
        // Comprobantes recibidos
        // -----------------------------------------------------------------------------------

        public async Task<EcfReceivedList> GetReceivedAsync(DateTime? sinceUtc = null, int limit = 100, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (limit < 1 || limit > 200)
                limit = 100;

            try
            {
                EcfEndpoints.EnsureAllowed(_http.BaseAddress);

                using (var response = await _http.GetAsync(EcfEndpoints.Received(sinceUtc, limit), cancellationToken).ConfigureAwait(false))
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return InterpretReceivedList(response.StatusCode, body);
                }
            }
            catch (Exception exception) when (IsTransportException(exception))
            {
                var transport = Transport(exception, cancellationToken);

                return new EcfReceivedList(EcfReceivedStatus.TransportError, transport.Message, transport.Detail, null, false, transport.IsTimeout);
            }
        }

        public EcfReceivedList GetReceived(DateTime? sinceUtc = null, int limit = 100) =>
            Task.Run(() => GetReceivedAsync(sinceUtc, limit)).GetAwaiter().GetResult();

        public Task<EcfReceivedFile> GetReceivedPdfAsync(string eNcf, string rncEmisor = null, CancellationToken cancellationToken = default(CancellationToken)) =>
            DownloadReceivedAsync(eNcf, "pdf", rncEmisor, "application/pdf", cancellationToken);

        public EcfReceivedFile GetReceivedPdf(string eNcf, string rncEmisor = null) =>
            Task.Run(() => GetReceivedPdfAsync(eNcf, rncEmisor)).GetAwaiter().GetResult();

        public Task<EcfReceivedFile> GetReceivedXmlAsync(string eNcf, string rncEmisor = null, CancellationToken cancellationToken = default(CancellationToken)) =>
            DownloadReceivedAsync(eNcf, "xml", rncEmisor, "application/xml", cancellationToken);

        public EcfReceivedFile GetReceivedXml(string eNcf, string rncEmisor = null) =>
            Task.Run(() => GetReceivedXmlAsync(eNcf, rncEmisor)).GetAwaiter().GetResult();

        private async Task<EcfReceivedFile> DownloadReceivedAsync(string eNcf, string kind, string rncEmisor, string defaultContentType, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(eNcf))
                throw new ArgumentException("Falta el eNCF.", nameof(eNcf));

            var ncf = eNcf.Trim();

            try
            {
                EcfEndpoints.EnsureAllowed(_http.BaseAddress);

                using (var response = await _http.GetAsync(EcfEndpoints.ReceivedFile(ncf, kind, rncEmisor), cancellationToken).ConfigureAwait(false))
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

                        return new EcfReceivedFile(
                            EcfReceivedStatus.Ok,
                            "Descargado.",
                            string.Empty,
                            bytes,
                            string.IsNullOrWhiteSpace(name) ? "ECF_" + ncf.ToUpperInvariant() + "." + kind : name.Trim('"'),
                            string.IsNullOrWhiteSpace(type) ? defaultContentType : type,
                            null,
                            false);
                    }

                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return InterpretReceivedError(response.StatusCode, body);
                }
            }
            catch (Exception exception) when (IsTransportException(exception))
            {
                var transport = Transport(exception, cancellationToken);

                return new EcfReceivedFile(EcfReceivedStatus.TransportError, transport.Message, transport.Detail, null, null, null, null, transport.IsTimeout);
            }
        }

        private static EcfReceivedList InterpretReceivedList(HttpStatusCode statusCode, string body)
        {
            var code = (int)statusCode;

            if (code < 200 || code >= 300)
            {
                var failure = InterpretReceivedError(statusCode, body);

                return new EcfReceivedList(failure.Status, failure.Message, failure.Detail, null, false, false);
            }

            EcfReceivedListPayload payload = null;

            try
            {
                payload = EcfJson.Deserialize<EcfReceivedListPayload>(body);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // El cuerpo no es JSON del contrato.
            }

            if (payload == null || payload.Items == null)
                return new EcfReceivedList(
                    EcfReceivedStatus.ContractError,
                    "La plataforma respondio algo que esta version no entiende al listar los recibidos.",
                    body,
                    null,
                    false,
                    false);

            foreach (var item in payload.Items)
                item.ReceivedAtUtc = DateTime.SpecifyKind(item.ReceivedAtUtc, DateTimeKind.Utc);

            return new EcfReceivedList(
                EcfReceivedStatus.Ok,
                payload.Items.Count == 0 ? "No hay comprobantes recibidos nuevos." : payload.Items.Count + " comprobante(s) recibido(s).",
                string.Empty,
                payload.Items,
                payload.HasMore,
                false);
        }

        /// <summary>Traduce un codigo de error de los endpoints de recibidos, sin lanzar nunca.</summary>
        private static EcfReceivedFile InterpretReceivedError(HttpStatusCode statusCode, string body)
        {
            var code = (int)statusCode;

            EcfReceivedErrorPayload payload = null;

            try
            {
                payload = EcfJson.Deserialize<EcfReceivedErrorPayload>(body);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // Cuerpo que no es JSON: se usa el mensaje por defecto.
            }

            var serverMessage = payload != null && !string.IsNullOrWhiteSpace(payload.Message) ? payload.Message : null;

            EcfReceivedStatus status;
            string message;
            IReadOnlyList<string> candidates = null;

            if (statusCode == HttpStatusCode.NotFound)
            {
                status = EcfReceivedStatus.NotFound;
                message = serverMessage ?? "La plataforma no tiene ese comprobante recibido. Se conservan 30 dias.";
            }
            else if (statusCode == HttpStatusCode.Conflict)
            {
                status = EcfReceivedStatus.Ambiguous;
                message = serverMessage ?? "Hay recibidos con ese eNCF de mas de un emisor. Indique el RNC del emisor.";
                candidates = payload != null && payload.Emisores != null ? payload.Emisores : new List<string>();
            }
            else if (statusCode == HttpStatusCode.BadRequest)
            {
                status = EcfReceivedStatus.InvalidRequest;
                message = serverMessage ?? "La plataforma rechazo la consulta por un parametro invalido.";
            }
            else if (statusCode == HttpStatusCode.Unauthorized || statusCode == HttpStatusCode.Forbidden)
            {
                status = EcfReceivedStatus.Unauthorized;
                message = serverMessage ?? "La API key no es valida o no esta activa.";
            }
            else if (code >= 500)
            {
                status = EcfReceivedStatus.TransportError;
                message = "La plataforma respondio " + code + ". Reintente mas tarde.";
            }
            else
            {
                // 422 incluido: la plataforma no pudo generar el PDF de ese XML.
                status = EcfReceivedStatus.ContractError;
                message = serverMessage ?? "La plataforma respondio " + code + " a la consulta.";
            }

            return new EcfReceivedFile(status, message, body, null, null, null, candidates, false);
        }
```

Notas: el archivo ya importa `System.Net`, `System.Net.Http`, `System.Collections.Generic`, `System.Threading.Tasks` y `Zynstorm.DGII.ECF.Contracts` (los usa `GetByNcfAsync`); si falta alguno, agregarlo. `Transport(...)` devuelve un `EcfOutcome` con `Message`, `Detail` e `IsTimeout`, que es lo que se reutiliza aquí. `EcfJson.Deserialize<T>` lanza `JsonException` con cuerpos inválidos, igual que en `GetByNcf`.

- [ ] **Step 9: Ejecutar las pruebas**

Run: `dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | tail -15`
Expected: toda la suite en verde (las de antes más ~28 nuevas) en el target de pruebas. Si `Un_Since_Local_Se_Convierte_A_UTC` falla por la zona horaria de la máquina, verificar que `Received()` use `ToUniversalTime()` para `Kind == Local` (así está escrito). Si `EcfJson.Deserialize` usa `CamelCasePropertyNamesContractResolver`, los `[JsonProperty]` explícitos mandan y no hay conflicto.

- [ ] **Step 10: Compilar los tres targets**

Run: `dotnet build src/Zynstorm.DGII.ECF -c Debug 2>&1 | tail -8`
Expected: `Build succeeded` para `net452`, `netstandard2.0` y `net8.0`. Si `net452` falla por sintaxis (por ejemplo `?.` o expresiones que no soporta), reescribir con construcciones clásicas; no cambiar el comportamiento.

- [ ] **Step 11: Commit**

```bash
git add src tests
git commit -m "feat: consultar y descargar los e-CF recibidos (lista, PDF y XML)"
```

---

### Task 8: Versión 0.3.0, guías y paquete

**Files:**
- Modify: `Directory.Build.props` (`<Version>`), `docs/build-package.py` (`VERSION`)
- Modify: `docs/CHANGELOG.md`, `docs/GUIA-DE-USO.md`, `docs/GUIA-RAPIDA.md`, `docs/GUIA-TECNICA.md`
- Regenerate: `docs/html/*.html`, `dist/*` (según el flujo del repo)

**Interfaces:**
- Consumes: la API pública de la Task 7.
- Produces: guías `.md` y regeneradas (`.html`, `.pdf`), `dist/` y el zip 0.3.0.

- [ ] **Step 1: Leer cómo están organizadas las guías**

Run:
```bash
cd /c/Projects/Zynstorm.DGII.ECF
grep -n "^## \|^### " docs/GUIA-DE-USO.md | head -80
grep -n "^## \|^### " docs/GUIA-RAPIDA.md | head -40
grep -n "^## " docs/GUIA-TECNICA.md
grep -n "GetByNcf" docs/GUIA-DE-USO.md | head -20
```
Expected: el índice de secciones. Identificar dónde está la sección de `GetByNcf`/`WaitForFinal` en la guía de uso (la nueva sección va a continuación), el índice o tabla de contenido si existe (agregar la entrada) y la lista de métodos de la guía rápida.

- [ ] **Step 2: Subir la versión**

- `Directory.Build.props`: `<Version>0.2.1</Version>` → `<Version>0.3.0</Version>`.
- `docs/build-package.py`: `VERSION = "0.2.1"` → `VERSION = "0.3.0"`.
- Buscar otras apariciones: `grep -rn "0\.2\.1" --include=*.md --include=*.py --include=*.props --include=*.cmd --include=*.csproj . | grep -v "/obj/\|/dist/\|CHANGELOG"`; actualizar solo las que son la versión actual (no los textos históricos del CHANGELOG ni de VERSIONADO que hablan de 0.2.1 como historia).

- [ ] **Step 3: CHANGELOG**

En `docs/CHANGELOG.md`, encima de la sección `## Proxima version (solo documentacion)` (que se fusiona en esta versión: su contenido pasa a formar parte de 0.3.0) crear:

```markdown
## 0.3.0

Nuevo: consultar y descargar los comprobantes **recibidos**. **Reemplace la DLL** (la de `net452` o la de `netstandard2.0`, segun su proyecto) y recompile.

- **`GetReceived` / `GetReceivedAsync`**: lista los e-CF que terceros le enviaron a su cliente, del mas antiguo al mas nuevo. Esta pensado para consultarlo cada cierto tiempo: guarde `NextSince` y pasela como `sinceUtc` en la proxima consulta; si `HasMore` es `true`, consulte otra vez de inmediato. La plataforma conserva los recibidos **30 dias**.
- **`GetReceivedPdf` / `GetReceivedPdfAsync`**: descarga el PDF del recibido (diseno propio, A4, con el QR de la DGII).
- **`GetReceivedXml` / `GetReceivedXmlAsync`**: descarga el XML original. `Text` lo devuelve ya como texto.
- Como el resto de la libreria, **no lanzan excepcion** por red, timeout ni respuesta del servidor: devuelven un resultado con `Status` (`Ok`, `NotFound`, `Ambiguous`, `InvalidRequest`, `Unauthorized`, `TransportError`, `ContractError`). Un eNCF vacio si lanza `ArgumentException`, igual que `GetByNcf`.
- Si hay recibidos con el mismo eNCF de dos emisores distintos, el resultado es `Ambiguous` y `Candidates` trae los RNC: vuelva a pedirlo pasando `rncEmisor`.
- **`IEcfClient` gana seis miembros.** Quien use `EcfClient` no cambia nada. Quien **implemente o simule** `IEcfClient` en sus pruebas tiene que agregarlos.
- **Requiere la plataforma actualizada**: los endpoints `Ecf/received` son nuevos. Contra una plataforma anterior, las llamadas devuelven `NotFound`.
```

Y a continuación, **conservar** el contenido de «Proxima version (solo documentacion)» pero cambiando su título por `### Tambien incluido: documentacion` dentro de 0.3.0 (sin repetir la frase «no cambia la DLL»): mover sus viñetas debajo del bloque anterior, tal como están.

- [ ] **Step 4: Guía de uso**

En `docs/GUIA-DE-USO.md` agregar, a continuación de la sección de `GetByNcf`/`WaitForFinal` (y su entrada en el índice si lo hay), una sección nueva con este contenido (ajustar el número de sección al que corresponda en el documento):

````markdown
## Comprobantes recibidos: consultar y descargar

Cuando un proveedor le emite un e-CF a su cliente, la plataforma lo recibe de la DGII y lo guarda. La librería permite saber si llegó algo nuevo y descargarlo, en PDF o en XML.

> **Se conservan 30 días.** Pasado ese plazo la plataforma borra el comprobante y ya no se puede descargar. Consulte con frecuencia y guarde en su sistema lo que le interese.

### Cómo se usa

La idea es consultar cada cierto tiempo (por ejemplo cada 5 o 10 minutos) y descargar solo lo nuevo:

```csharp
// 'since' es lo último que vio: guárdelo en su base de datos o archivo de configuración.
DateTime? since = LeerUltimaConsulta();

var recibidos = await client.GetReceivedAsync(since);

if (recibidos.Status != EcfReceivedStatus.Ok)
{
    Log(recibidos.Message, recibidos.Detail);   // no hay excepción: revise Status
    return;
}

foreach (var item in recibidos.Items)
{
    var pdf = await client.GetReceivedPdfAsync(item.ENcf, item.RncEmisor);

    if (pdf.Status == EcfReceivedStatus.Ok)
        File.WriteAllBytes(Path.Combine(carpeta, pdf.FileName), pdf.Content);
    else
        Log(pdf.Message, pdf.Detail);
}

// Solo avance el marcador si la lista llegó bien. Si HasMore es true, consulte otra vez ya.
if (recibidos.NextSince.HasValue)
    GuardarUltimaConsulta(recibidos.NextSince.Value);
```

### Qué devuelve cada método

| Método | Devuelve | Contenido |
|---|---|---|
| `GetReceived(sinceUtc, limit)` | `EcfReceivedList` | `Items` (eNCF, RNC del emisor, tipo, total, fecha de recepción), `Count`, `HasMore`, `NextSince` |
| `GetReceivedPdf(eNcf, rncEmisor)` | `EcfReceivedFile` | `Content` (bytes del PDF), `FileName`, `ContentType` |
| `GetReceivedXml(eNcf, rncEmisor)` | `EcfReceivedFile` | `Content`, `Text` (el XML como texto), `FileName` |

Todos tienen versión `Async` y versión síncrona. `limit` va de 1 a 200 (100 por omisión). `sinceUtc` es exclusivo: solo trae lo recibido **después** de esa fecha.

### Estados

| `Status` | Qué significa | Qué hacer |
|---|---|---|
| `Ok` | Todo bien | Usar `Items` o `Content` |
| `NotFound` | La plataforma no tiene ese recibido (nunca llegó o pasaron 30 días) | No reintentar |
| `Ambiguous` | Hay recibidos con ese eNCF de más de un emisor | Pedirlo de nuevo con `rncEmisor`; los candidatos están en `Candidates` |
| `InvalidRequest` | eNCF o fecha con formato inválido | Corregir el dato |
| `Unauthorized` | La API key no es válida o no está activa | Revisar la configuración |
| `TransportError` | Sin red, timeout o fallo de la plataforma | Reintentar más tarde; mire `IsTimeout` |
| `ContractError` | La plataforma respondió algo que esta versión no entiende, o no pudo armar el PDF de ese XML | Descargar el XML original y avisar a soporte |

### Qué trae el PDF

El PDF es la representación impresa del comprobante recibido: tipo de comprobante y eNCF, datos del emisor y del comprador, fechas y forma de pago, el detalle de ítems, los totales (con ITBIS y retenciones si las hay), el QR de verificación de la DGII, el código de seguridad y la fecha de firma. Sirve para todos los tipos de e-CF (31 a 47). Para notas de crédito y débito incluye el documento afectado.

### Dos detalles

- **El eNCF solo es único por emisor.** `item.RncEmisor` viene en la lista; páselo siempre que lo tenga y no recibirá `Ambiguous`.
- **Solo e-CF.** Las aprobaciones comerciales que llegan por el mismo canal no aparecen en esta lista.
````

Agregar también al checklist de puesta en marcha (si la guía lo tiene) un ítem: `- [ ] Si recibe comprobantes de proveedores: consulta con GetReceived cada pocos minutos, guarda NextSince y descarga lo nuevo antes de 30 días.`

- [ ] **Step 5: Guía rápida**

En `docs/GUIA-RAPIDA.md`, en la lista de lo que se puede hacer y/o junto a los ejemplos de consulta, agregar:

````markdown
### Recibir comprobantes de proveedores

```csharp
var recibidos = client.GetReceived(ultimaConsulta);          // lista lo nuevo
foreach (var item in recibidos.Items)
{
    var pdf = client.GetReceivedPdf(item.ENcf, item.RncEmisor);
    if (pdf.Status == EcfReceivedStatus.Ok)
        File.WriteAllBytes(pdf.FileName, pdf.Content);       // también GetReceivedXml
}
// Guarde recibidos.NextSince para la próxima consulta. Se conservan 30 días.
```
````

- [ ] **Step 6: Guía técnica**

En `docs/GUIA-TECNICA.md`, a continuación de la sección de `GetByNcfAsync` (5c), agregar:

````markdown
## 5d. Comprobantes recibidos

Tres consultas de lectura sobre lo que la plataforma guardó de los e-CF que terceros le enviaron al cliente de la API key. No modifican nada ni llaman a la DGII.

| Método | Ruta | Respuesta |
|---|---|---|
| `GetReceivedAsync(sinceUtc, limit)` | `GET Ecf/received?since=<ISO UTC>&limit=N` | `{ count, hasMore, items:[{ eNcf, rncEmisor, ecfType, total, receivedAtUtc }] }` |
| `GetReceivedPdfAsync(eNcf, rncEmisor)` | `GET Ecf/received/{eNcf}/pdf[?rncEmisor=]` | `application/pdf` |
| `GetReceivedXmlAsync(eNcf, rncEmisor)` | `GET Ecf/received/{eNcf}/xml[?rncEmisor=]` | `application/xml` |

**Lista.** `ReceivedAtUtc > since` (exclusivo), orden ascendente, `limit` de 1 a 200 (la librería corrige un valor fuera de rango a 100 antes de enviar). `since` viaja en UTC, ISO 8601 (`o`); un `DateTime` local se convierte y uno sin zona se toma como UTC. `NextSince` es la `ReceivedAtUtc` del último ítem, y es `null` si la lista llegó vacía: el integrador conserva el valor anterior. Solo e-CF; las aprobaciones comerciales no se listan.

**Traducción de códigos.** `200` → `Ok`; `404` → `NotFound`; `409` → `Ambiguous` (con `emisores` en `Candidates`); `400` → `InvalidRequest`; `401`/`403` → `Unauthorized`; `5xx` y fallos de red o timeout → `TransportError` (con `IsTimeout`); cualquier otro código, incluido `422` (la plataforma no pudo generar el PDF de ese XML) o un cuerpo ilegible → `ContractError`. Un eNCF vacío lanza `ArgumentException` antes de enviar, igual que `GetByNcfAsync`.

**Descarga.** `FileName` sale de `Content-Disposition`; si falta, la librería arma `ECF_{eNcf}.pdf` o `.xml`. `Text` decodifica `Content` como UTF-8. El RNC emisor viaja solo con dígitos.

**Ofuscación.** Los payloads internos (`EcfReceivedListPayload`, `EcfReceivedErrorPayload`) llevan `[JsonProperty]` explícito en cada propiedad, por la misma razón que `EcfLookupPayload` (0.2.1). `EcfReceivedItem` es público y también lleva los atributos.

**Retención.** La plataforma borra los recibidos con más de 30 días; después de ese plazo `NotFound` es la respuesta correcta y no un error.

**`IEcfClient`.** Gana seis miembros (tres `Async` y tres síncronos) en 0.3.0. Es un cambio de versión menor: quien implemente la interfaz debe agregarlos.
````

Y en la lista de pruebas/checklist de la guía técnica (si existe, cerca de la línea 467), agregar: `- [ ] GetReceived, GetReceivedPdf y GetReceivedXml contra staging devuelven Ok con un recibido real y NotFound con un eNCF inexistente.`

- [ ] **Step 7: Regenerar HTML, PDF y paquete**

Averiguar el flujo exacto sin adivinar:

```bash
grep -n "build-html\|build-pdf\|build-package" -r *.cmd src/Zynstorm.DGII.ECF/*.csproj build/*.targets docs/*.py README.md 2>/dev/null | head
```

Luego ejecutar, en este orden, lo que corresponda al flujo documentado (según el resultado anterior; por defecto):

```bash
cd /c/Projects/Zynstorm.DGII.ECF
python docs/build-html.py
python docs/build-pdf.py
./generar-dll-ofuscada.cmd --no-pause     # DLL net452 y netstandard2.0 ofuscadas y verificadas
python docs/build-package.py
```
Expected: `docs/html/*.html` y `dist/GUIA-*.pdf` regenerados; el script de ofuscado termina con las verificaciones de API pública (`verify-public-api.ps1`) y de protección en verde; `build-package.py` imprime el zip `dist/Zynstorm.DGII.ECF-0.3.0.zip` sin avisos de contenido prohibido. Si falta una dependencia de Python (`reportlab`) instalarla con `pip install reportlab`. Si el ofuscado falla en la verificación de API pública porque los tipos nuevos aparecen como diferencia, revisar que sean `public` y que `build/obfuscar.xml` no los oculte; no desactivar la verificación.

- [ ] **Step 8: Revisar los PDF generados**

Abrir `dist/GUIA-DE-USO.pdf` y `dist/GUIA-RAPIDA.pdf` con la herramienta Read (por páginas, `pages: "1-3"` y las del nuevo apartado) y comprobar que la sección nueva se ve bien: títulos, tablas, bloques de código sin cortarse. Corregir el Markdown si algo se desfigura y regenerar.

- [ ] **Step 9: Probar la DLL ofuscada contra los recibidos**

Hay una prueba que corre contra la DLL ofuscada (la regresión de 0.2.1: `CamelCaseParityTests` o la que se indica en `docs/VERSIONADO.md`). Buscarla y correrla:

```bash
grep -n "ofuscad\|Obfuscat" docs/VERSIONADO.md tests/Zynstorm.DGII.ECF.Tests/*.cs | head
```
Si existe una forma de probar los payloads contra la DLL de `dist/`, agregar el caso de `EcfReceivedListPayload` y `EcfReceivedErrorPayload` siguiendo el patrón de la prueba de `EcfLookupPayload`, y ejecutarla. Si no existe tal mecanismo, dejar anotado en el reporte que la prueba contra la DLL ofuscada queda pendiente de hacerse a mano.

- [ ] **Step 10: Commit**

```bash
git add Directory.Build.props docs dist src tests
git status --short | head -20
git commit -m "docs: version 0.3.0 con la consulta y descarga de recibidos en las guias"
```
(Revisar `git status` antes: no incluir `dist/*.rar`, `__pycache__` ni binarios que el repo no versiona; seguir lo que ya hace el historial con `git show --stat HEAD~1`.)

---

## Parte D: Cierre

### Task 9: Verificación final y memoria

- [ ] **Step 1: Verificar los tres repos**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && git branch --show-current && dotnet test ZynstormECFPlatform.Tests 2>&1 | tail -4
git checkout feature/emit-idempotente && dotnet test ZynstormECFPlatform.Tests 2>&1 | tail -4 && git checkout Staging
cd ../ZynstormECFPlatform-FrontEnd && git branch --show-current && npx tsc --noEmit 2>&1 | tail -4
cd /c/Projects/Zynstorm.DGII.ECF && git branch --show-current && dotnet test tests/Zynstorm.DGII.ECF.Tests 2>&1 | tail -4
```
Expected: todo en verde, cada repo en la rama prevista y sin cambios sin commitear (`git status --short`).

- [ ] **Step 2: Guardar la memoria del proyecto**

Crear `C:\Users\JoseTrinidad\.claude\projects\C--Projects-ZynstormECF-WorkSpace\memory\recibidos-pdf-api-libreria.md` con el estado final (commits por repo, que es solo local sin push, que la prueba manual en Staging del botón y de los endpoints está pendiente, que los recibidos se borran a los 30 días, que `FeController` es `[AllowAnonymous]` completo y que sus endpoints de recibidos existentes no exigen sesión) y agregar su línea al índice `MEMORY.md`.

- [ ] **Step 3: Reportar**

Resumir al usuario: qué se hizo en cada repo, hashes, resultados de pruebas, lo que quedó sin verificar (probar el botón y los endpoints en Staging con un recibido real, probar la DLL ofuscada si no hubo prueba automática) y el hallazgo de seguridad de `FeController`. No hacer push.
