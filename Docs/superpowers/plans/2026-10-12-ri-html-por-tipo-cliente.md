# RI con formato HTML por tipo de cliente — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que un tipo de cliente (ej. "Royaltrix Software") tenga un formato HTML de Representación Impresa con marcadores `{{...}}` que se rellenan con el XML firmado, sin alterar el PDF QuestPDF actual para clientes sin tipo.

**Architecture:** Camino aditivo. Dos entidades nuevas (`ClientType`, `RiHtmlTemplate`) y dos columnas nulables en `Client`. `RiHtmlTemplateValidator` sanea y valida el HTML al subirlo; `RiHtmlRenderer` (AngleSharp) rellena marcadores, repite filas de ítems, elimina líneas vacías e inyecta los bloques eNCF/QR faltantes. `CertificationRiModelService` resuelve cliente → tipo → defecto y solo usa el renderer HTML cuando hay formato; si no, el código actual no se toca. Un `RiFormatController` nuevo expone el CRUD; el frontend agrega una tarjeta "Formato de factura" en el Paso 5.

**Tech Stack:** .NET 10, EF Core (Npgsql), QuestPDF/QRCoder (existentes), AngleSharp (nuevo), xunit; Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-10-12-ri-html-por-tipo-cliente-design.md`

## Global Constraints
- **No romper lo existente:** cliente sin `ClientTypeId` ni `RiHtmlTemplateId` ⇒ salida PDF idéntica a la actual (mismos endpoints, mismo ZIP de `.pdf`). `RiPdfRenderer`, `RiInvoicePdf`, `RiPurchasePdf`, `RiExpensePdf`, `EcfRiTemplateMapper` NO se modifican.
- Trabajar **directo en la rama `Staging`** (backend y frontend); luego replicar (Task 9). No hacer push sin que el usuario lo pida.
- Resolución del formato: `Client.RiHtmlTemplateId` → `Client.ClientType.RiHtmlTemplateId` → PDF por defecto.
- Marcadores planos PascalCase, sensibles a mayúsculas. Ítems: `Descripcion`, `Monto`, `Cantidad`, `Precio`, `Unidad`, `ItemItbis`. Totales: `Subtotal`, `Itbis`, `Total`, `TotalDescuento`.
- `FechaVencimiento` queda vacío en tipos **32 y 34** (igual que `RiInvoicePdf`).
- Mínimo para subir: una fila con `{{Descripcion}}` o `{{Monto}}`, y `{{Total}}`. El resto avisa pero no bloquea. Máximo 2 MB.
- Todo valor se escapa como HTML; solo `EmisorLogo` (si empieza con `data:image/`) y `QR` salen como `<img src="data:...">`.
- Bloques que el sistema agrega si faltan: `eNCF` + `VALIDO HASTA` debajo del título, y bloque QR al final (QR, "Codigo seguridad", "Fecha firma digital", "REPRESENTACION IMPRESA DEL e-CF"). El bloque QR se agrega solo si el HTML no contiene `{{QR}}`.
- Base de datos PostgreSQL: columnas con `RegisteredAt`/`GuidId`/`IsDeleted` como las demás entidades (`BaseEntity`).
- Build: `dotnet build ZynstormECFPlatform.slnx -nologo`. Tests: `dotnet test ZynstormECFPlatform.Tests -nologo`. Frontend: `npx tsc --noEmit`.
- Rutas absolutas: backend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, frontend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`. Cada repo tiene su propio `.git`.

## Decisiones tomadas al planificar (ajustes menores a la spec)
1. El marcador `Itbis` de totales y el de ítems chocaban: los ítems usan **`ItemItbis`**.
2. El renderer usa `EcfRiTemplateMapper.MapInvoice` para **todos** los tipos (incluidos 41 y 43; el mapper es genérico y `NcfTypeName` ya cubre esos tipos).
3. Endpoints en un controller nuevo `RiFormatController` (`v1/RiFormat/...`), no dentro de `CertificationController`. Autorización `[Authorize]` (usuario autenticado).
4. Borrar un formato en uso se **rechaza** con mensaje (en vez de flujo de reasignación).
5. La subida del HTML va como JSON `{name, fileName, html}` (el frontend lee el archivo con `file.text()`), sin multipart.

## File Structure

Backend (`ZynstormECFPlatform`):
- Create `ZynstormECFPlatform.Core/Entities/ClientType.cs`, `RiHtmlTemplate.cs`; modify `Client.cs`.
- Modify `ZynstormECFPlatform.Data/StorageContext.cs`; create migración `AddRiHtmlFormats`.
- Create `ZynstormECFPlatform.Services/Ri/Html/RiHtmlMarkers.cs`, `RiHtmlTemplateValidator.cs`, `RiHtmlRenderer.cs`.
- Create `ZynstormECFPlatform.Services/Ri/RiFormatResolver.cs`, `RiFormatService.cs`; create `ZynstormECFPlatform.Abstractions/Services/IRiFormatService.cs`; create `ZynstormECFPlatform.Dtos/RiFormatDtos.cs`.
- Modify `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs`, `ZynstormECFPlatform.Abstractions/Services/ICertificationRiModelService.cs`, `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, `ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj`.
- Create `ZynstormECFPlatform.Web.Api/Controllers/RiFormatController.cs`; modify `CertificationController.cs` (preview/download).
- Tests: `ZynstormECFPlatform.Tests/Ri/Html/RiHtmlTemplateValidatorTests.cs`, `RiHtmlRendererTests.cs`, `RiHtmlTestTemplates.cs`, `ZynstormECFPlatform.Tests/Ri/RiFormatResolverTests.cs`.

Frontend (`ZynstormECFPlatform-FrontEnd`):
- Create `types/riFormat.type.ts`, `services/riFormat.service.ts`, `components/certification/ri-format-card.tsx`.
- Modify `components/certification/ri-step5.tsx`, `services/certification.service.ts` (no cambia URLs; solo se lee el `Content-Type`).

---

## Task 1: Entidades, configuración EF y migración

**Files:**
- Create: `ZynstormECFPlatform.Core/Entities/ClientType.cs`, `ZynstormECFPlatform.Core/Entities/RiHtmlTemplate.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`, `ZynstormECFPlatform.Data/StorageContext.cs`
- Create (generada): `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddRiHtmlFormats.cs` + `.Designer.cs`; modifica `StorageContextModelSnapshot.cs`

**Interfaces:**
- Produces: `ClientType { int ClientTypeId; string Name; string? Description; int? RiHtmlTemplateId; string? SampleDataJson; RiHtmlTemplate? RiHtmlTemplate; ICollection<Client> Clients }`, `RiHtmlTemplate { int RiHtmlTemplateId; string Name; string FileName; string Html; ICollection<ClientType> ClientTypes; ICollection<Client> Clients }`, `Client.ClientTypeId?`, `Client.ClientType?`, `Client.RiHtmlTemplateId?`, `Client.RiHtmlTemplate?`, `DbSet<ClientType> ClientTypes`, `DbSet<RiHtmlTemplate> RiHtmlTemplates` (se accede con `context.Set<T>()` como el resto).

- [ ] **Step 1: Crear las entidades**

`ZynstormECFPlatform.Core/Entities/ClientType.cs`:
```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Tipo/grupo de cliente (ej. "Royaltrix Software"). Define el formato HTML de RI que usan
/// todos sus clientes y los valores de muestra de los campos que el XML no trae.
/// </summary>
public class ClientType : BaseEntity
{
    public int ClientTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? RiHtmlTemplateId { get; set; }

    /// <summary>JSON plano {"Cedula":"...","Medico":"...","EmisorLogo":"data:image/..."}.</summary>
    public string? SampleDataJson { get; set; }

    public virtual RiHtmlTemplate? RiHtmlTemplate { get; set; }

    public virtual ICollection<Client> Clients { get; set; } = [];
}
```
`ZynstormECFPlatform.Core/Entities/RiHtmlTemplate.cs`:
```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Formato HTML de Representación Impresa (biblioteca reutilizable). Html ya saneado.</summary>
public class RiHtmlTemplate : BaseEntity
{
    public int RiHtmlTemplateId { get; set; }

    public string Name { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string Html { get; set; } = null!;

    public virtual ICollection<ClientType> ClientTypes { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}
```

- [ ] **Step 2: Ampliar `Client`**

En `Client.cs`, debajo de `public int? PlanId { get; set; }` agregar:
```csharp
    /// <summary>Tipo de cliente (define el formato HTML de RI y datos de muestra). Null = formato por defecto.</summary>
    public int? ClientTypeId { get; set; }

    /// <summary>Formato HTML de RI propio de este cliente; tiene prioridad sobre el de su tipo.</summary>
    public int? RiHtmlTemplateId { get; set; }

    public virtual ClientType? ClientType { get; set; }

    public virtual RiHtmlTemplate? RiHtmlTemplate { get; set; }
```

- [ ] **Step 3: Configurar EF en `StorageContext`**

Agregar los `DbSet` junto a los existentes (línea ~173):
```csharp
    public DbSet<ClientType> ClientTypes { get; set; }
    public DbSet<RiHtmlTemplate> RiHtmlTemplates { get; set; }
```
Y en `OnModelCreating`, justo antes de `modelBuilder.Entity<CertificationInvoicePrintTemplateEcfType>(...)`:
```csharp
        modelBuilder.Entity<RiHtmlTemplate>(entity =>
        {
            entity.HasKey(c => c.RiHtmlTemplateId);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.FileName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Html).IsRequired();

            entity.Property(c => c.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(c => c.LastUpdateUtc).HasColumnType(DateTimeColumnType);
            entity.Property(c => c.DeletedTimeUtc).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
            entity.Property(e => e.GuidId).IsRequired().HasMaxLength(450).IsUnicode(false).HasDefaultValueSql(DefaultGUIDSqlValue);
            entity.HasQueryFilter(c => !c.IsDeleted);
        });

        modelBuilder.Entity<ClientType>(entity =>
        {
            entity.HasKey(c => c.ClientTypeId);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(c => c.RegisteredAt).HasColumnType(DateTimeColumnType).HasDefaultValueSql(DefaultDateTimeSqlValue);
            entity.Property(c => c.LastUpdateUtc).HasColumnType(DateTimeColumnType);
            entity.Property(c => c.DeletedTimeUtc).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.IsDeleted).HasDefaultValue(false).IsRequired();
            entity.Property(e => e.GuidId).IsRequired().HasMaxLength(450).IsUnicode(false).HasDefaultValueSql(DefaultGUIDSqlValue);
            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.RiHtmlTemplate)
                  .WithMany(p => p.ClientTypes)
                  .HasForeignKey(d => d.RiHtmlTemplateId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_ClientType_RiHtmlTemplate");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasOne(d => d.ClientType)
                  .WithMany(p => p.Clients)
                  .HasForeignKey(d => d.ClientTypeId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_Client_ClientType");

            entity.HasOne(d => d.RiHtmlTemplate)
                  .WithMany(p => p.Clients)
                  .HasForeignKey(d => d.RiHtmlTemplateId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_Client_RiHtmlTemplate");
        });
```
Nota: `StorageContext` ya configura `Client` en otro `modelBuilder.Entity<Client>(...)`; EF acumula configuraciones de la misma entidad, así que este bloque adicional es válido. Si el archivo ya define estas relaciones con otro estilo, ajustar al estilo existente sin duplicar nombres de FK.

- [ ] **Step 4: Compilar**

Run: `cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet build ZynstormECFPlatform.slnx -nologo -v q`
Expected: `Build succeeded` sin errores.

- [ ] **Step 5: Generar la migración**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
dotnet ef migrations add AddRiHtmlFormats -p ZynstormECFPlatform.Data -s ZynstormECFPlatform.Web.Api
```
Expected: `Done.` y archivos nuevos en `ZynstormECFPlatform.Data/Migrations/`.

- [ ] **Step 6: Revisar que la migración sea solo aditiva**

Abrir `*_AddRiHtmlFormats.cs`. Debe contener únicamente: `CreateTable("RiHtmlTemplates")`, `CreateTable("ClientTypes")`, `AddColumn<int>("ClientTypeId")` y `AddColumn<int>("RiHtmlTemplateId")` en `Clients` (nullable), índices y FKs `SetNull`. Si aparece cualquier `DropColumn`/`DropTable`/`AlterColumn` ajeno, **detenerse**: el snapshot estaba desincronizado; borrar la migración con `dotnet ef migrations remove -p ZynstormECFPlatform.Data -s ZynstormECFPlatform.Web.Api` y reportarlo al usuario.

- [ ] **Step 7: Commit**
```bash
git add ZynstormECFPlatform.Core ZynstormECFPlatform.Data
git commit -m "feat(ri): entidades ClientType y RiHtmlTemplate con columnas nulables en Client

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 2: Marcadores y validador/sanitizador del HTML

**Files:**
- Modify: `ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj`
- Create: `ZynstormECFPlatform.Services/Ri/Html/RiHtmlMarkers.cs`, `ZynstormECFPlatform.Services/Ri/Html/RiHtmlTemplateValidator.cs`
- Test: `ZynstormECFPlatform.Tests/Ri/Html/RiHtmlTestTemplates.cs`, `ZynstormECFPlatform.Tests/Ri/Html/RiHtmlTemplateValidatorTests.cs`

**Interfaces:**
- Produces:
  - `RiHtmlMarkers.Pattern : Regex` (grupo 1 = nombre), `RiHtmlMarkers.ItemMarkers`, `RiHtmlMarkers.SampleMarkers`, `RiHtmlMarkers.Known : IReadOnlySet<string>`, `RiHtmlMarkers.Extract(string text) : IEnumerable<string>`.
  - `sealed record RiHtmlValidationResult(string SanitizedHtml, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings) { bool IsValid }`.
  - `RiHtmlTemplateValidator.Validate(string? html) : RiHtmlValidationResult`, `RiHtmlTemplateValidator.MaxBytes = 2*1024*1024`.
  - `RiHtmlTestTemplates.Reference : string` (HTML de referencia del usuario, usado por los tests de Task 2 y 3).

- [ ] **Step 1: Agregar AngleSharp**

Run:
```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
dotnet add ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj package AngleSharp --version 1.3.0
```
Expected: `PackageReference for package 'AngleSharp' version '1.3.0' added`. Si `1.3.0` no existe en el feed, usar la última 1.x estable que ofrezca `dotnet add package AngleSharp`.

- [ ] **Step 2: Crear el HTML de referencia para tests**

`ZynstormECFPlatform.Tests/Ri/Html/RiHtmlTestTemplates.cs`:
```csharp
namespace ZynstormECFPlatform.Tests.Ri.Html;

/// <summary>Reproduce el formato de referencia entregado por el usuario (Royaltrix Software).</summary>
public static class RiHtmlTestTemplates
{
    public const string Reference = """
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Factura</title></head>
<body style="font-family:Arial;width:300px">
  <div id="logo" style="border:1px dashed #000;height:60px;text-align:center">{{EmisorLogo}}</div>
  <div style="text-align:center">{{EmisorDireccion}}</div>
  <div style="text-align:center"><b>RNC: {{EmisorRNC}}</b></div>
  <div style="text-align:center"><b>Tel.: {{EmisorTelefono}}</b></div>
  <h2><u>FACTURA</u></h2>
  <div style="text-align:center"><b>{{TipoComprobante}}</b></div>
  <div class="row"><span>eNCF:</span><span>{{eNCF}}</span></div>
  <div class="row"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
  <div class="row"><span>No. Factura:</span><b>{{NoFactura}}</b></div>
  <div class="row"><span>Fecha:</span><span>{{Fecha}}</span></div>
  <div class="row"><span>Cédula:</span><span>{{Cedula}}</span></div>
  <div class="row"><span>Nombre:</span><b>{{Nombre}}</b></div>
  <div class="row"><span>Sexo:</span><span>{{Sexo}}</span></div>
  <div class="row"><span>Fecha Nac.:</span><span>{{FechaNac}}</span></div>
  <div class="row"><span>Edad:</span><span>{{Edad}}</span></div>
  <div class="row"><span>Seg. Méd.:</span><span>{{SegMed}}</span></div>
  <div class="row"><span>NSS:</span><span>{{NSS}}</span></div>
  <div class="row"><span>Forma Pago:</span><b>{{FormaPago}}</b></div>
  <div class="row"><span>Devolución:</span><span>{{Devolucion}}</span></div>
  <div class="row"><span>Médico:</span><span>{{Medico}}</span></div>
  <table id="items" style="width:100%">
    <thead><tr><th>Descripción</th><th>Monto</th></tr></thead>
    <tbody><tr><td>{{Descripcion}}</td><td>{{Monto}}</td></tr></tbody>
  </table>
  <div class="row"><b>Total:</b><b>{{Total}}</b></div>
  <div class="row"><b>Total Descuento:</b><b>{{TotalDescuento}}</b></div>
  <div class="row"><b>Monto Pagado:</b><b>{{MontoPagado}}</b></div>
  <div class="row"><b>Restante:</b><b>{{Restante}}</b></div>
  <div id="qr" style="border:1px dashed #000;width:110px;height:110px;margin:auto">{{QR}}</div>
  <div style="text-align:center"><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
  <div style="text-align:center"><b>Fecha firma digital:</b> {{FechaFirma}}</div>
  <div style="text-align:center"><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</body></html>
""";
}
```

- [ ] **Step 3: Escribir los tests del validador (fallan)**

`ZynstormECFPlatform.Tests/Ri/Html/RiHtmlTemplateValidatorTests.cs`:
```csharp
using ZynstormECFPlatform.Services.Ri.Html;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri.Html;

public class RiHtmlTemplateValidatorTests
{
    [Fact]
    public void Validate_Reference_IsValid_AndKeepsMarkers()
    {
        var result = RiHtmlTemplateValidator.Validate(RiHtmlTestTemplates.Reference);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("{{Descripcion}}", result.SanitizedHtml);
        Assert.Contains("{{Total}}", result.SanitizedHtml);
    }

    [Fact]
    public void Validate_WithoutItemsRow_IsRejected()
    {
        var result = RiHtmlTemplateValidator.Validate("<html><body><p>{{Total}}</p></body></html>");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("{{Descripcion}}"));
    }

    [Fact]
    public void Validate_WithoutTotal_IsRejected()
    {
        var result = RiHtmlTemplateValidator.Validate("<html><body><table><tr><td>{{Descripcion}}</td></tr></table></body></html>");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("{{Total}}"));
    }

    [Fact]
    public void Validate_Empty_IsRejected()
    {
        Assert.False(RiHtmlTemplateValidator.Validate("   ").IsValid);
        Assert.False(RiHtmlTemplateValidator.Validate(null).IsValid);
    }

    [Fact]
    public void Validate_TooLarge_IsRejected()
    {
        var html = "<p>" + new string('a', RiHtmlTemplateValidator.MaxBytes + 1) + "</p>";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("2 MB"));
    }

    [Fact]
    public void Validate_StripsScriptsHandlersAndJavascriptUrls()
    {
        var html = """
<html><head><script>alert(1)</script><base href="http://evil"><link rel="stylesheet" href="http://evil/x.css"></head>
<body onload="x()"><a href="javascript:alert(1)" onclick="y()">x</a><iframe src="http://evil"></iframe>
<object data="x"></object><embed src="x">
<style>@import url(http://evil/a.css); p{color:red}</style>
<table><tr><td>{{Descripcion}}</td></tr></table>{{Total}}</body></html>
""";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.True(result.IsValid);
        var s = result.SanitizedHtml;
        Assert.DoesNotContain("<script", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("javascript:", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<object", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<embed", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<base", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<link", s, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import", s, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("color:red", s);
    }

    [Fact]
    public void Validate_WarnsUnknownMarkers_AndMissingBlocks()
    {
        var html = "<html><body><table><tr><td>{{Descripcion}}</td></tr></table>{{Total}}{{Inventado}}</body></html>";

        var result = RiHtmlTemplateValidator.Validate(html);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("{{Inventado}}"));
        Assert.Contains(result.Warnings, w => w.Contains("{{eNCF}}"));
        Assert.Contains(result.Warnings, w => w.Contains("{{QR}}"));
    }

    [Fact]
    public void Validate_Reference_HasNoWarnings()
    {
        var result = RiHtmlTemplateValidator.Validate(RiHtmlTestTemplates.Reference);

        Assert.Empty(result.Warnings);
    }
}
```

- [ ] **Step 4: Verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiHtmlTemplateValidatorTests"`
Expected: error de compilación (`RiHtmlTemplateValidator` no existe).

- [ ] **Step 5: Implementar `RiHtmlMarkers`**

`ZynstormECFPlatform.Services/Ri/Html/RiHtmlMarkers.cs`:
```csharp
using System.Text.RegularExpressions;

namespace ZynstormECFPlatform.Services.Ri.Html;

/// <summary>Marcadores {{Nombre}} soportados por los formatos HTML de RI.</summary>
public static class RiHtmlMarkers
{
    public static readonly Regex Pattern = new(@"\{\{\s*([A-Za-z][A-Za-z0-9_]*)\s*\}\}", RegexOptions.Compiled);

    /// <summary>Marcadores que viven dentro de la fila repetible de ítems.</summary>
    public static readonly IReadOnlySet<string> ItemMarkers = new HashSet<string>(StringComparer.Ordinal)
    {
        "Descripcion", "Monto", "Cantidad", "Precio", "Unidad", "ItemItbis"
    };

    /// <summary>Marcadores que el XML no trae: salen de los datos de muestra del tipo de cliente.</summary>
    public static readonly IReadOnlySet<string> SampleMarkers = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cedula", "Sexo", "FechaNac", "Edad", "SegMed", "NSS", "Devolucion", "Medico", "MontoPagado", "Restante"
    };

    private static readonly string[] DocumentMarkers =
    [
        "EmisorLogo", "EmisorNombre", "EmisorDireccion", "EmisorRNC", "EmisorTelefono",
        "TipoComprobante", "eNCF", "FechaVencimiento", "NoFactura", "Fecha", "FormaPago", "Nombre",
        "QR", "CodigoSeguridad", "FechaFirma", "Subtotal", "Itbis", "Total", "TotalDescuento"
    ];

    public static readonly IReadOnlySet<string> Known =
        new HashSet<string>(DocumentMarkers.Concat(ItemMarkers).Concat(SampleMarkers), StringComparer.Ordinal);

    /// <summary>Nombres de marcadores presentes en <paramref name="text"/>, en orden y con repeticiones.</summary>
    public static IEnumerable<string> Extract(string text) =>
        Pattern.Matches(text).Select(m => m.Groups[1].Value);
}
```

- [ ] **Step 6: Implementar el validador**

`ZynstormECFPlatform.Services/Ri/Html/RiHtmlTemplateValidator.cs`:
```csharp
using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace ZynstormECFPlatform.Services.Ri.Html;

public sealed record RiHtmlValidationResult(
    string SanitizedHtml,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Sanea el HTML subido (sin scripts ni recursos externos) y valida los marcadores mínimos.</summary>
public static class RiHtmlTemplateValidator
{
    public const int MaxBytes = 2 * 1024 * 1024;

    private static readonly string[] RemovedTags = ["script", "iframe", "object", "embed", "base", "link", "frame", "frameset"];
    private static readonly string[] UrlAttributes = ["href", "src", "action", "formaction", "xlink:href", "data"];
    private static readonly Regex CssImport = new(@"@import[^;]*;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static RiHtmlValidationResult Validate(string? html)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(html))
        {
            errors.Add("El HTML está vacío.");
            return new RiHtmlValidationResult(string.Empty, errors, warnings);
        }

        if (Encoding.UTF8.GetByteCount(html) > MaxBytes)
        {
            errors.Add("El HTML supera el tamaño máximo de 2 MB.");
            return new RiHtmlValidationResult(string.Empty, errors, warnings);
        }

        var document = new HtmlParser().ParseDocument(html);
        Sanitize(document);
        var sanitized = "<!DOCTYPE html>" + document.DocumentElement.OuterHtml;

        var markers = RiHtmlMarkers.Extract(sanitized).ToHashSet(StringComparer.Ordinal);

        if (!markers.Contains("Descripcion") && !markers.Contains("Monto"))
        {
            errors.Add("El formato debe incluir una fila de ítems con {{Descripcion}} o {{Monto}}.");
        }

        if (!markers.Contains("Total"))
        {
            errors.Add("El formato debe incluir {{Total}}.");
        }

        foreach (var unknown in markers.Where(m => !RiHtmlMarkers.Known.Contains(m)).OrderBy(m => m, StringComparer.Ordinal))
        {
            warnings.Add($"Marcador desconocido: {{{{{unknown}}}}}; se dejará tal cual.");
        }

        if (!markers.Contains("eNCF"))
        {
            warnings.Add("No incluye {{eNCF}}: el sistema agregará el bloque eNCF / VALIDO HASTA debajo del título.");
        }

        if (!markers.Contains("QR"))
        {
            warnings.Add("No incluye {{QR}}: el sistema agregará el bloque del QR al final.");
        }
        else
        {
            if (!markers.Contains("CodigoSeguridad"))
            {
                warnings.Add("Incluye {{QR}} pero no {{CodigoSeguridad}}.");
            }

            if (!markers.Contains("FechaFirma"))
            {
                warnings.Add("Incluye {{QR}} pero no {{FechaFirma}}.");
            }
        }

        return new RiHtmlValidationResult(sanitized, errors, warnings);
    }

    private static void Sanitize(IDocument document)
    {
        foreach (var tag in RemovedTags)
        {
            foreach (var element in document.QuerySelectorAll(tag).ToList())
            {
                element.Remove();
            }
        }

        foreach (var meta in document.QuerySelectorAll("meta[http-equiv]").ToList())
        {
            meta.Remove();
        }

        foreach (var element in document.All.ToList())
        {
            foreach (var attribute in element.Attributes.ToList())
            {
                var name = attribute.Name;
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
                {
                    element.RemoveAttribute(name);
                    continue;
                }

                if (UrlAttributes.Contains(name, StringComparer.OrdinalIgnoreCase) &&
                    attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    element.RemoveAttribute(name);
                }
            }
        }

        foreach (var style in document.QuerySelectorAll("style").ToList())
        {
            style.TextContent = CssImport.Replace(style.TextContent, string.Empty);
        }
    }
}
```

- [ ] **Step 7: Verificar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiHtmlTemplateValidatorTests"`
Expected: 8 passed. Si `Validate_Reference_HasNoWarnings` falla por un marcador, revisar `RiHtmlMarkers.Known` contra `Reference` (deben coincidir exactamente).

- [ ] **Step 8: Commit**
```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests
git commit -m "feat(ri): validador y sanitizador del HTML de formato de RI

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 3: `RiHtmlRenderer` (marcadores, ítems, líneas vacías, bloques inyectados)

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/Html/RiHtmlRenderer.cs`
- Test: `ZynstormECFPlatform.Tests/Ri/Html/RiHtmlRendererTests.cs`

**Interfaces:**
- Consumes: `EcfRiTemplateMapper.MapInvoice(string, DgiiEnvironment) : RiInvoiceModel` (existente); `RiCompanyHeader(Name, Rnc, Address, Phone, Whatsapp)` y `RiInvoiceCompany` (existentes en `RiPdfRenderer.cs`/`RiInvoiceModel.cs`); `RiHtmlMarkers` (Task 2).
- Produces: `RiHtmlRenderer.Render(string templateHtml, string signedXml, RiCompanyHeader? company, string? sampleDataJson) : string` (HTML completo).

- [ ] **Step 1: Escribir los tests (fallan)**

`ZynstormECFPlatform.Tests/Ri/Html/RiHtmlRendererTests.cs`:
```csharp
using ZynstormECFPlatform.Services.Ri;
using ZynstormECFPlatform.Services.Ri.Html;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri.Html;

public class RiHtmlRendererTests
{
    private static readonly RiCompanyHeader Company =
        new("ROYALTRIX SRL", "133009889", "Calle 1, Santo Domingo", "809-555-1234", "809-555-1234");

    private const string Samples = """
{"Cedula":"001-0000000-1","Sexo":"M","FechaNac":"01/01/1990","Edad":"36","SegMed":"ARS Demo",
 "NSS":"123456789","Devolucion":"0.00","Medico":"Dr. Ejemplo","MontoPagado":"RD$100.00","Restante":"RD$0.00"}
""";

    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    private static string Render31(string template = RiHtmlTestTemplates.Reference, string? samples = Samples) =>
        RiHtmlRenderer.Render(template, Load("Paso_1_E310000000402.xml"), Company, samples);

    [Fact]
    public void Render_Type31_FillsHeaderBuyerAndNcf()
    {
        var html = Render31();

        Assert.Contains("Calle 1, Santo Domingo", html);
        Assert.Contains("RNC: 133009889", html);
        Assert.Contains("E310000000402", html);
        Assert.Contains("MORTEROS DE EUROPA", html);
        Assert.Contains("FACTURA DE CRÉDITO FISCAL ELECTRÓNICA", html);
        Assert.DoesNotContain("{{", html);
    }

    [Fact]
    public void Render_Type31_ShowsValidoHasta()
    {
        var html = Render31();

        Assert.Contains("VALIDO HASTA:", html);
        Assert.Contains("31/12/2028", html);
    }

    [Theory]
    [InlineData("Paso_25_E320000000344.xml")]
    [InlineData("Paso_E340000000002.xml")]
    public void Render_Type32And34_RemovesValidoHastaLine(string fixture)
    {
        var html = RiHtmlRenderer.Render(RiHtmlTestTemplates.Reference, Load(fixture), Company, Samples);

        Assert.DoesNotContain("VALIDO HASTA", html);
        Assert.Contains("eNCF:", html);
    }

    [Fact]
    public void Render_RepeatsItemRow_OncePerXmlItem()
    {
        var html = Render31();

        // Cada ítem produce una <tr> en el tbody; la del encabezado vive en thead.
        var document = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var rows = document.QuerySelectorAll("#items tbody tr");
        Assert.True(rows.Length >= 1);
        Assert.All(rows, r => Assert.DoesNotContain("{{", r.TextContent));
        Assert.All(rows, r => Assert.Contains("RD$", r.TextContent));
    }

    [Fact]
    public void Render_UsesSampleDataForFieldsMissingInXml()
    {
        var html = Render31();

        Assert.Contains("Dr. Ejemplo", html);
        Assert.Contains("ARS Demo", html);
        Assert.Contains("123456789", html);
    }

    [Fact]
    public void Render_RemovesLinesWhoseValueIsEmpty()
    {
        var html = Render31(samples: null);

        Assert.DoesNotContain("Médico:", html);
        Assert.DoesNotContain("NSS:", html);
        // El logo vacío elimina el recuadro.
        Assert.DoesNotContain("id=\"logo\"", html);
    }

    [Fact]
    public void Render_EmbedsQrImage_AndSecurityBlock()
    {
        var html = Render31();

        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("Codigo seguridad", html);
        Assert.Contains("REPRESENTACION IMPRESA DEL e-CF", html);
    }

    [Fact]
    public void Render_UsesDataImageLogoOnlyWhenValid()
    {
        var good = Render31(samples: "{\"EmisorLogo\":\"data:image/png;base64,AAAA\"}");
        var bad = Render31(samples: "{\"EmisorLogo\":\"http://evil/x.png\"}");

        Assert.Contains("data:image/png;base64,AAAA", good);
        Assert.DoesNotContain("evil", bad);
    }

    [Fact]
    public void Render_EscapesValuesFromXmlAndCompany()
    {
        var company = new RiCompanyHeader("<b>X</b> & Co", "1", "<script>alert(1)</script>", "1", "1");

        var html = RiHtmlRenderer.Render(RiHtmlTestTemplates.Reference, Load("Paso_1_E310000000402.xml"), company, Samples);

        Assert.DoesNotContain("<script>alert(1)</script>", html);
        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
    }

    [Fact]
    public void Render_InjectsMissingEncfAndQrBlocks()
    {
        const string minimal = """
<html><body>
<h1>FACTURA</h1>
<table><tr><td>{{Descripcion}}</td><td>{{Monto}}</td></tr></table>
<div>Total: {{Total}}</div>
</body></html>
""";

        var html = Render31(minimal);

        Assert.Contains("E310000000402", html);
        Assert.Contains("VALIDO HASTA:", html);
        Assert.Contains("data:image/png;base64,", html);
        Assert.Contains("REPRESENTACION IMPRESA DEL e-CF", html);
        // El bloque eNCF queda debajo del título y antes de la tabla de ítems.
        Assert.True(html.IndexOf("FACTURA", StringComparison.Ordinal) < html.IndexOf("E310000000402", StringComparison.Ordinal));
        Assert.True(html.IndexOf("E310000000402", StringComparison.Ordinal) < html.IndexOf("<table", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_KeepsUnknownMarkersAsIs()
    {
        const string t = "<html><body><table><tr><td>{{Descripcion}}</td></tr></table>{{Total}} {{Inventado}}</body></html>";

        var html = Render31(t);

        Assert.Contains("{{Inventado}}", html);
    }
}
```
- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiHtmlRendererTests"`
Expected: error de compilación (`RiHtmlRenderer` no existe).

- [ ] **Step 3: Implementar el renderer**

`ZynstormECFPlatform.Services/Ri/Html/RiHtmlRenderer.cs`:
```csharp
using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Html;

/// <summary>
/// Rellena un formato HTML de RI (marcadores {{...}}) con los datos de un e-CF firmado.
/// Camino aditivo: no toca las plantillas QuestPDF.
/// </summary>
public static class RiHtmlRenderer
{
    private static readonly CultureInfo Culture = new("es-DO");

    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "span", "b", "strong", "i", "em", "u", "a", "small", "font", "sup", "sub", "label"
    };

    private const string EncfBlock = """
<div class="ri-encf" style="display:flex;justify-content:space-between"><span>eNCF:</span><span>{{eNCF}}</span></div>
<div class="ri-valido" style="display:flex;justify-content:space-between"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
""";

    private const string QrBlock = """
<div class="ri-qr" style="text-align:center;margin-top:12px">
<div style="width:110px;height:110px;margin:0 auto">{{QR}}</div>
<div><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
<div><b>Fecha firma digital:</b> {{FechaFirma}}</div>
<div><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</div>
""";

    public static string Render(string templateHtml, string signedXml, RiCompanyHeader? company, string? sampleDataJson)
    {
        var model = EcfRiTemplateMapper.MapInvoice(signedXml, DgiiEnvironment.CerteCF);
        if (company is not null)
        {
            model.Company = new RiInvoiceCompany
            {
                Name = company.Name,
                Rnc = company.Rnc,
                Address = company.Address,
                Phone = company.Phone,
                Whatsapp = company.Whatsapp,
            };
        }

        var values = BuildValues(model, ParseSamples(sampleDataJson));

        var document = new HtmlParser().ParseDocument(templateHtml);
        var body = document.Body ?? throw new InvalidOperationException("El formato HTML no tiene <body>.");

        EnsureCharset(document);
        InjectMissingBlocks(document, body);
        ExpandItemRows(document, body, model.Items);
        RemoveEmptyLines(body, values);
        SubstituteGlobalMarkers(document, body, values);

        return "<!DOCTYPE html>" + document.DocumentElement.OuterHtml;
    }

    // ---------------------------------------------------------------- values

    private static Dictionary<string, string> ParseSamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static Dictionary<string, string> BuildValues(RiInvoiceModel model, Dictionary<string, string> samples)
    {
        string Sample(string key) => samples.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;

        var hideValidUntil = model.EcfType is 32 or 34;
        var logo = Sample("EmisorLogo");

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EmisorLogo"] = logo.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ? logo : string.Empty,
            ["EmisorNombre"] = model.Company.Name,
            ["EmisorDireccion"] = model.Company.Address,
            ["EmisorRNC"] = model.Company.Rnc,
            ["EmisorTelefono"] = model.Company.Phone,
            ["TipoComprobante"] = model.NcfTypeName,
            ["eNCF"] = model.NcfNumber,
            ["FechaVencimiento"] = hideValidUntil ? string.Empty : model.ValidUntil,
            ["NoFactura"] = model.InternalInvoiceNumber,
            ["Fecha"] = model.FechaEmision,
            ["FormaPago"] = model.PaymentType,
            ["Nombre"] = model.Client.Name,
            ["QR"] = QrDataUri(model.Qr),
            ["CodigoSeguridad"] = string.IsNullOrEmpty(model.SecurityCode) ? "N/D" : model.SecurityCode,
            ["FechaFirma"] = string.IsNullOrEmpty(model.FechaFirma) ? "N/D" : model.FechaFirma,
            ["Subtotal"] = Money(model.SubTotal),
            ["Itbis"] = Money(model.Itbis),
            ["Total"] = Money(model.Total),
            ["TotalDescuento"] = Money(model.Discount),
            // El XML tiene prioridad sobre la muestra (la cédula del comprador).
            ["Cedula"] = string.IsNullOrEmpty(model.Client.Rnc) ? Sample("Cedula") : model.Client.Rnc,
        };

        foreach (var key in RiHtmlMarkers.SampleMarkers.Where(k => k != "Cedula"))
        {
            values[key] = Sample(key);
        }

        return values;
    }

    private static Dictionary<string, string> ItemValues(RiInvoiceItem item) => new(StringComparer.Ordinal)
    {
        ["Descripcion"] = item.Description,
        ["Cantidad"] = item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
        ["Unidad"] = item.Unit,
        ["Precio"] = Money(item.Price),
        ["ItemItbis"] = Money(item.Itbis),
        ["Monto"] = Money(item.Amount),
    };

    private static string Money(decimal value) => string.Format(Culture, "{0:C2}", value);

    private static string QrDataUri(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        try
        {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var png = new QRCoder.PngByteQRCode(data);
            return "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(5));
        }
        catch
        {
            return string.Empty;
        }
    }

    // ------------------------------------------------------------- DOM utils

    private static void EnsureCharset(IDocument document)
    {
        if (document.QuerySelector("meta[charset]") is not null || document.Head is null)
        {
            return;
        }

        var meta = document.CreateElement("meta");
        meta.SetAttribute("charset", "utf-8");
        document.Head.InsertBefore(meta, document.Head.FirstChild);
    }

    private static List<IText> TextNodesWithMarkers(IElement root, Func<string, bool> markerFilter) =>
        root.Descendants<IText>()
            .Where(t => RiHtmlMarkers.Extract(t.Data).Any(markerFilter))
            .ToList();

    private static bool HasStaticText(IElement element) =>
        element.Descendants<IText>()
            .Any(t => !string.IsNullOrWhiteSpace(RiHtmlMarkers.Pattern.Replace(t.Data, string.Empty)));

    private static IElement LowestBlock(IElement element, IElement body)
    {
        var current = element;
        while (current != body && InlineTags.Contains(current.LocalName) && current.ParentElement is not null)
        {
            current = current.ParentElement;
        }

        return current;
    }

    private static void InsertHtml(IDocument document, string html, IElement parent, INode? before)
    {
        var holder = document.CreateElement("div");
        holder.InnerHtml = html;
        foreach (var child in holder.ChildNodes.ToList())
        {
            parent.InsertBefore(child, before);
        }
    }

    // -------------------------------------------------------- injected blocks

    private static void InjectMissingBlocks(IDocument document, IElement body)
    {
        var markers = RiHtmlMarkers.Extract(document.DocumentElement.OuterHtml).ToHashSet(StringComparer.Ordinal);

        if (!markers.Contains("eNCF"))
        {
            var anchor = TextNodesWithMarkers(body, m => m == "TipoComprobante").FirstOrDefault()?.ParentElement
                         ?? body.QuerySelector("h1, h2, h3");
            anchor = anchor is null ? null : LowestBlock(anchor, body);

            if (anchor is null || anchor == body)
            {
                InsertHtml(document, EncfBlock, body, body.FirstChild);
            }
            else
            {
                InsertHtml(document, EncfBlock, anchor.ParentElement!, anchor.NextSibling);
            }
        }

        if (!markers.Contains("QR"))
        {
            InsertHtml(document, QrBlock, body, null);
        }
    }

    // ------------------------------------------------------------- item rows

    private static void ExpandItemRows(IDocument document, IElement body, IReadOnlyList<RiInvoiceItem> items)
    {
        var itemNodes = TextNodesWithMarkers(body, RiHtmlMarkers.ItemMarkers.Contains);
        if (itemNodes.Count == 0)
        {
            return;
        }

        var row = FindItemRow(itemNodes, body);

        foreach (var item in items)
        {
            var clone = (IElement)row.Clone(true);
            var values = ItemValues(item);
            foreach (var node in TextNodesWithMarkers(clone, values.ContainsKey))
            {
                ReplaceTextNode(document, node, name => values.TryGetValue(name, out var v) ? WebUtility.HtmlEncode(v) : null);
            }

            row.ParentElement!.InsertBefore(clone, row);
        }

        row.Remove();
    }

    private static IElement FindItemRow(List<IText> itemNodes, IElement body)
    {
        var common = itemNodes[0].ParentElement!;
        foreach (var node in itemNodes.Skip(1))
        {
            var current = node.ParentElement;
            while (current is not null && !common.Contains(current))
            {
                current = current.ParentElement;
            }

            common = current ?? body;
        }

        var row = LowestBlock(common, body);
        if (row.Closest("tr") is { } tr && body.Contains(tr))
        {
            row = tr;
        }

        // Si los marcadores están dispersos y el ancestro común es el body, repetir solo el bloque del primero.
        return row == body ? LowestBlock(itemNodes[0].ParentElement!, body) : row;
    }

    // ------------------------------------------------------------ empty lines

    private static void RemoveEmptyLines(IElement body, Dictionary<string, string> values)
    {
        bool IsEmpty(string marker) => values.TryGetValue(marker, out var v) && string.IsNullOrEmpty(v);

        bool AllKnownMarkersEmpty(IElement element)
        {
            var known = element.Descendants<IText>()
                .SelectMany(t => RiHtmlMarkers.Extract(t.Data))
                .Where(values.ContainsKey)
                .ToList();
            return known.Count > 0 && known.All(IsEmpty);
        }

        var targets = new HashSet<IElement>();

        foreach (var node in TextNodesWithMarkers(body, values.ContainsKey))
        {
            var nodeMarkers = RiHtmlMarkers.Extract(node.Data).Where(values.ContainsKey).ToList();
            if (nodeMarkers.Count == 0 || !nodeMarkers.All(IsEmpty))
            {
                continue;
            }

            var block = LowestBlock(node.ParentElement!, body);
            var candidate = block;
            while (candidate != body && !HasStaticText(candidate) && candidate.ParentElement is not null)
            {
                candidate = candidate.ParentElement;
            }

            if (candidate != body && AllKnownMarkersEmpty(candidate))
            {
                targets.Add(candidate);                       // fila con etiqueta y todos los valores vacíos
            }
            else if (block != body && !HasStaticText(block) && AllKnownMarkersEmpty(block))
            {
                targets.Add(block);                           // caja que solo contiene el marcador (logo, QR)
            }
        }

        foreach (var target in targets)
        {
            target.Remove();
        }
    }

    // ------------------------------------------------------- global markers

    private static void SubstituteGlobalMarkers(IDocument document, IElement body, Dictionary<string, string> values)
    {
        foreach (var node in TextNodesWithMarkers(body, values.ContainsKey))
        {
            ReplaceTextNode(document, node, name => values.TryGetValue(name, out var v) ? RenderTextValue(name, v) : null);
        }

        foreach (var element in document.All.ToList())
        {
            foreach (var attribute in element.Attributes.ToList())
            {
                if (!attribute.Value.Contains("{{", StringComparison.Ordinal))
                {
                    continue;
                }

                element.SetAttribute(attribute.Name, RiHtmlMarkers.Pattern.Replace(attribute.Value, m =>
                    values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value));
            }
        }
    }

    private static string RenderTextValue(string name, string value) => name switch
    {
        "EmisorLogo" when value.Length > 0 =>
            $"<img src=\"{WebUtility.HtmlEncode(value)}\" alt=\"\" style=\"max-width:100%;max-height:100%;object-fit:contain\">",
        "QR" when value.Length > 0 =>
            $"<img src=\"{WebUtility.HtmlEncode(value)}\" alt=\"QR\" style=\"width:110px;height:110px\">",
        _ => WebUtility.HtmlEncode(value)
    };

    /// <summary>
    /// Reemplaza los marcadores de un nodo de texto por HTML ya escapado. Si <paramref name="resolve"/>
    /// devuelve null el marcador se conserva literalmente (marcador desconocido).
    /// </summary>
    private static void ReplaceTextNode(IDocument document, IText node, Func<string, string?> resolve)
    {
        var parent = node.ParentElement;
        if (parent is null)
        {
            return;
        }

        var html = RiHtmlMarkers.Pattern.Replace(WebUtility.HtmlEncode(node.Data), m =>
            resolve(m.Groups[1].Value) ?? m.Value);

        InsertHtml(document, html, parent, node);
        parent.RemoveChild(node);
    }
}
```
Notas de implementación que el engineer debe respetar:
- `ReplaceTextNode` codifica **todo** el texto del nodo antes de aplicar el patrón. Como `{{`/`}}` no cambian al codificarse, los marcadores siguen coincidiendo, y el texto estático (con `&`, `<`) queda correctamente escapado al reinsertarse como HTML.
- Para ítems, `resolve` ya devuelve el valor escapado; para globales, `RenderTextValue` devuelve texto escapado o un `<img>` (único HTML crudo permitido).
- Los valores de `values` se usan crudos en atributos (AngleSharp los escapa al serializar).

- [ ] **Step 4: Ejecutar los tests**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiHtmlRendererTests"`
Expected: todos pasan. Diagnóstico si fallan:
- `Render_RemovesLinesWhoseValueIsEmpty` falla por `id="logo"`: revisar que el `div#logo` (solo `{{EmisorLogo}}`) caiga en la rama `else if (block != body && !HasStaticText(block) ...)`; su ancestro con texto estático es `body` (candidate == body) → debe usarse la segunda rama.
- `Render_Type32And34_...` falla porque queda "VALIDO HASTA": la fila `.row` con `<span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span>` debe resolverse subiendo del `span` (inline) a su `div.row` (con texto estático) y eliminarse completo.
- Conteo de filas de ítems: confirmar que `Descripcion`/`Monto` están en un `tr` del `tbody` y que el `thead` no contiene marcadores.

- [ ] **Step 5: Regresión del PDF**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~Ri"`
Expected: todos los tests `Ri*` (incluidos `RiInvoicePdfTests`, `RiPdfRendererTests`) pasan sin cambios.

- [ ] **Step 6: Commit**
```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests
git commit -m "feat(ri): renderer HTML con ítems repetidos, líneas vacías y bloques eNCF/QR

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 4: DTOs, resolución de formato y `RiFormatService`

**Files:**
- Create: `ZynstormECFPlatform.Dtos/RiFormatDtos.cs`, `ZynstormECFPlatform.Abstractions/Services/IRiFormatService.cs`, `ZynstormECFPlatform.Services/Ri/RiFormatResolver.cs`, `ZynstormECFPlatform.Services/Ri/RiFormatService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`
- Test: `ZynstormECFPlatform.Tests/Ri/RiFormatResolverTests.cs`

**Interfaces:**
- Consumes: entidades de Task 1; `RiHtmlTemplateValidator` (Task 2).
- Produces:
  - `enum RiFormatSource { Default, ClientType, Client }` y `record ResolvedRiFormat(RiFormatSource Source, string? ClientTypeName, RiHtmlTemplate? Template, string? SampleDataJson)` (en `RiFormatResolver.cs`, namespace `ZynstormECFPlatform.Services.Ri`).
  - `RiFormatResolver.Resolve(Client client) : ResolvedRiFormat` (requiere `ClientType` y ambos `RiHtmlTemplate` cargados).
  - DTOs: `ClientTypeDto`, `ClientTypeSaveDto`, `RiHtmlTemplateDto`, `RiHtmlTemplateUploadDto`, `RiHtmlTemplateUploadResultDto`, `ClientRiFormatAssignDto`, `ResolvedRiFormatDto`.
  - `IRiFormatService`: `ListClientTypesAsync()`, `CreateClientTypeAsync(ClientTypeSaveDto)`, `UpdateClientTypeAsync(string guidId, ClientTypeSaveDto)`, `DeleteClientTypeAsync(string guidId)`, `ListTemplatesAsync()`, `UploadTemplateAsync(RiHtmlTemplateUploadDto)`, `DeleteTemplateAsync(string guidId)`, `AssignToClientAsync(string clientGuidId, ClientRiFormatAssignDto)`, `GetResolvedAsync(string clientGuidId) : ResolvedRiFormatDto`, `ResolveForClientAsync(int clientId) : ResolvedRiFormat`.

- [ ] **Step 1: Escribir los tests del resolver (fallan)**

`ZynstormECFPlatform.Tests/Ri/RiFormatResolverTests.cs`:
```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiFormatResolverTests
{
    private static RiHtmlTemplate Template(string name) => new() { Name = name, FileName = name + ".html", Html = "<p>" + name + "</p>" };

    [Fact]
    public void Resolve_ClientWithoutTypeNorTemplate_IsDefault()
    {
        var result = RiFormatResolver.Resolve(new Client());

        Assert.Equal(RiFormatSource.Default, result.Source);
        Assert.Null(result.Template);
    }

    [Fact]
    public void Resolve_TypeWithoutTemplate_IsDefault()
    {
        var client = new Client { ClientType = new ClientType { Name = "Otro" } };

        Assert.Equal(RiFormatSource.Default, RiFormatResolver.Resolve(client).Source);
    }

    [Fact]
    public void Resolve_TypeWithTemplate_UsesTypeTemplateAndSamples()
    {
        var client = new Client
        {
            ClientType = new ClientType
            {
                Name = "Royaltrix Software",
                SampleDataJson = "{\"Medico\":\"Dr. X\"}",
                RiHtmlTemplate = Template("royaltrix")
            }
        };

        var result = RiFormatResolver.Resolve(client);

        Assert.Equal(RiFormatSource.ClientType, result.Source);
        Assert.Equal("royaltrix", result.Template!.Name);
        Assert.Equal("Royaltrix Software", result.ClientTypeName);
        Assert.Equal("{\"Medico\":\"Dr. X\"}", result.SampleDataJson);
    }

    [Fact]
    public void Resolve_ClientTemplate_WinsOverTypeTemplate_ButKeepsTypeSamples()
    {
        var client = new Client
        {
            RiHtmlTemplate = Template("propio"),
            ClientType = new ClientType
            {
                Name = "Royaltrix Software",
                SampleDataJson = "{\"Medico\":\"Dr. X\"}",
                RiHtmlTemplate = Template("royaltrix")
            }
        };

        var result = RiFormatResolver.Resolve(client);

        Assert.Equal(RiFormatSource.Client, result.Source);
        Assert.Equal("propio", result.Template!.Name);
        Assert.Equal("{\"Medico\":\"Dr. X\"}", result.SampleDataJson);
    }
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiFormatResolverTests"`
Expected: error de compilación (`RiFormatResolver` no existe).

- [ ] **Step 3: Implementar el resolver**

`ZynstormECFPlatform.Services/Ri/RiFormatResolver.cs`:
```csharp
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Ri;

public enum RiFormatSource
{
    Default,
    ClientType,
    Client
}

/// <param name="Template">Null cuando <paramref name="Source"/> es Default.</param>
public sealed record ResolvedRiFormat(
    RiFormatSource Source,
    string? ClientTypeName,
    RiHtmlTemplate? Template,
    string? SampleDataJson);

/// <summary>Cliente (formato propio) → tipo de cliente → PDF por defecto.</summary>
public static class RiFormatResolver
{
    public static ResolvedRiFormat Resolve(Client client)
    {
        var type = client.ClientType;

        if (client.RiHtmlTemplate is not null)
        {
            return new ResolvedRiFormat(RiFormatSource.Client, type?.Name, client.RiHtmlTemplate, type?.SampleDataJson);
        }

        if (type?.RiHtmlTemplate is not null)
        {
            return new ResolvedRiFormat(RiFormatSource.ClientType, type.Name, type.RiHtmlTemplate, type.SampleDataJson);
        }

        return new ResolvedRiFormat(RiFormatSource.Default, type?.Name, null, null);
    }
}
```

- [ ] **Step 4: DTOs**

`ZynstormECFPlatform.Dtos/RiFormatDtos.cs`:
```csharp
namespace ZynstormECFPlatform.Dtos;

public record ClientTypeDto(
    string GuidId,
    string Name,
    string? Description,
    string? RiHtmlTemplateGuidId,
    string? RiHtmlTemplateName,
    string? SampleDataJson,
    int ClientCount);

public record ClientTypeSaveDto(
    string Name,
    string? Description,
    string? RiHtmlTemplateGuidId,
    string? SampleDataJson);

public record RiHtmlTemplateDto(
    string GuidId,
    string Name,
    string FileName,
    DateTime RegisteredAt,
    int InUseCount);

public record RiHtmlTemplateUploadDto(string Name, string FileName, string Html);

public record RiHtmlTemplateUploadResultDto(
    RiHtmlTemplateDto? Template,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public record ClientRiFormatAssignDto(string? ClientTypeGuidId, string? RiHtmlTemplateGuidId);

/// <param name="Source">"Default" | "ClientType" | "Client".</param>
public record ResolvedRiFormatDto(
    string Source,
    string? ClientTypeName,
    string? TemplateGuidId,
    string? TemplateName);
```

- [ ] **Step 5: Interfaz del servicio**

`ZynstormECFPlatform.Abstractions/Services/IRiFormatService.cs`:
```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>Biblioteca de formatos HTML de RI, tipos de cliente y su asignación a clientes.</summary>
public interface IRiFormatService
{
    Task<IReadOnlyList<ClientTypeDto>> ListClientTypesAsync();

    Task<ClientTypeDto> CreateClientTypeAsync(ClientTypeSaveDto dto);

    Task<ClientTypeDto> UpdateClientTypeAsync(string guidId, ClientTypeSaveDto dto);

    Task DeleteClientTypeAsync(string guidId);

    Task<IReadOnlyList<RiHtmlTemplateDto>> ListTemplatesAsync();

    Task<RiHtmlTemplateUploadResultDto> UploadTemplateAsync(RiHtmlTemplateUploadDto dto);

    Task DeleteTemplateAsync(string guidId);

    Task AssignToClientAsync(string clientGuidId, ClientRiFormatAssignDto dto);

    Task<ResolvedRiFormatDto> GetResolvedAsync(string clientGuidId);
}
```
(`ResolveForClientAsync` es interno al servicio de render y vive en `RiFormatService` como método público de la clase, no de la interfaz — ver Task 5.)

- [ ] **Step 6: Implementar `RiFormatService`**

`ZynstormECFPlatform.Services/Ri/RiFormatService.cs`:
```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Ri.Html;

namespace ZynstormECFPlatform.Services.Ri;

public class RiFormatService(StorageContext context) : IRiFormatService
{
    public async Task<IReadOnlyList<ClientTypeDto>> ListClientTypesAsync()
    {
        var types = await context.Set<ClientType>()
            .Include(t => t.RiHtmlTemplate)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var counts = await context.Set<Client>()
            .Where(c => c.ClientTypeId != null)
            .GroupBy(c => c.ClientTypeId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return types.ConvertAll(t => ToDto(t, counts.GetValueOrDefault(t.ClientTypeId)));
    }

    public async Task<ClientTypeDto> CreateClientTypeAsync(ClientTypeSaveDto dto)
    {
        var name = ValidateTypeDto(dto);
        if (await context.Set<ClientType>().AnyAsync(t => t.Name == name))
        {
            throw new InvalidOperationException($"Ya existe un tipo de cliente llamado '{name}'.");
        }

        var entity = new ClientType
        {
            GuidId = Guid.NewGuid().ToString(),
            RegisteredAt = DateTime.UtcNow,
            Name = name,
            Description = dto.Description,
            SampleDataJson = NormalizeSamples(dto.SampleDataJson),
            RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId),
        };

        context.Set<ClientType>().Add(entity);
        await context.SaveChangesAsync();

        return await GetTypeDtoAsync(entity.GuidId);
    }

    public async Task<ClientTypeDto> UpdateClientTypeAsync(string guidId, ClientTypeSaveDto dto)
    {
        var name = ValidateTypeDto(dto);
        var entity = await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Tipo de cliente no encontrado.");

        if (await context.Set<ClientType>().AnyAsync(t => t.Name == name && t.ClientTypeId != entity.ClientTypeId))
        {
            throw new InvalidOperationException($"Ya existe un tipo de cliente llamado '{name}'.");
        }

        entity.Name = name;
        entity.Description = dto.Description;
        entity.SampleDataJson = NormalizeSamples(dto.SampleDataJson);
        entity.RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId);
        entity.LastUpdateUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return await GetTypeDtoAsync(guidId);
    }

    public async Task DeleteClientTypeAsync(string guidId)
    {
        var entity = await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Tipo de cliente no encontrado.");

        var inUse = await context.Set<Client>().CountAsync(c => c.ClientTypeId == entity.ClientTypeId);
        if (inUse > 0)
        {
            throw new InvalidOperationException($"El tipo '{entity.Name}' lo usan {inUse} cliente(s); reasígnelos antes de borrarlo.");
        }

        context.Set<ClientType>().Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RiHtmlTemplateDto>> ListTemplatesAsync()
    {
        var templates = await context.Set<RiHtmlTemplate>().OrderBy(t => t.Name).ToListAsync();
        var result = new List<RiHtmlTemplateDto>();
        foreach (var template in templates)
        {
            result.Add(await ToDtoAsync(template));
        }

        return result;
    }

    public async Task<RiHtmlTemplateUploadResultDto> UploadTemplateAsync(RiHtmlTemplateUploadDto dto)
    {
        var validation = RiHtmlTemplateValidator.Validate(dto.Html);
        if (!validation.IsValid)
        {
            return new RiHtmlTemplateUploadResultDto(null, validation.Errors, validation.Warnings);
        }

        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return new RiHtmlTemplateUploadResultDto(null, ["El nombre del formato es obligatorio."], validation.Warnings);
        }

        var entity = new RiHtmlTemplate
        {
            GuidId = Guid.NewGuid().ToString(),
            RegisteredAt = DateTime.UtcNow,
            Name = name,
            FileName = string.IsNullOrWhiteSpace(dto.FileName) ? $"{name}.html" : dto.FileName.Trim(),
            Html = validation.SanitizedHtml,
        };

        context.Set<RiHtmlTemplate>().Add(entity);
        await context.SaveChangesAsync();

        return new RiHtmlTemplateUploadResultDto(await ToDtoAsync(entity), [], validation.Warnings);
    }

    public async Task DeleteTemplateAsync(string guidId)
    {
        var entity = await context.Set<RiHtmlTemplate>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Formato no encontrado.");

        var inUse = await UsageCountAsync(entity.RiHtmlTemplateId);
        if (inUse > 0)
        {
            throw new InvalidOperationException($"El formato '{entity.Name}' está en uso por {inUse} tipo(s)/cliente(s); reasígnelos antes de borrarlo.");
        }

        context.Set<RiHtmlTemplate>().Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task AssignToClientAsync(string clientGuidId, ClientRiFormatAssignDto dto)
    {
        var client = await context.Set<Client>().FirstOrDefaultAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        client.ClientTypeId = string.IsNullOrWhiteSpace(dto.ClientTypeGuidId)
            ? null
            : (await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == dto.ClientTypeGuidId)
                ?? throw new InvalidOperationException("Tipo de cliente no encontrado.")).ClientTypeId;

        client.RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId);
        client.LastUpdateUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<ResolvedRiFormatDto> GetResolvedAsync(string clientGuidId)
    {
        var client = await LoadClientAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var resolved = RiFormatResolver.Resolve(client);
        return new ResolvedRiFormatDto(
            resolved.Source.ToString(),
            resolved.ClientTypeName,
            resolved.Template?.GuidId,
            resolved.Template?.Name);
    }

    /// <summary>Formato efectivo de un cliente (usado por <see cref="CertificationRiModelService"/>).</summary>
    public async Task<ResolvedRiFormat> ResolveForClientAsync(int clientId)
    {
        var client = await LoadClientAsync(c => c.ClientId == clientId)
            ?? throw new InvalidOperationException("Cliente no encontrado.");
        return RiFormatResolver.Resolve(client);
    }

    // ---------------------------------------------------------------- helpers

    private Task<Client?> LoadClientAsync(System.Linq.Expressions.Expression<Func<Client, bool>> predicate) =>
        context.Set<Client>()
            .Include(c => c.RiHtmlTemplate)
            .Include(c => c.ClientType).ThenInclude(t => t!.RiHtmlTemplate)
            .FirstOrDefaultAsync(predicate);

    private async Task<int?> ResolveTemplateIdAsync(string? templateGuidId)
    {
        if (string.IsNullOrWhiteSpace(templateGuidId))
        {
            return null;
        }

        var template = await context.Set<RiHtmlTemplate>().FirstOrDefaultAsync(t => t.GuidId == templateGuidId)
            ?? throw new InvalidOperationException("Formato no encontrado.");
        return template.RiHtmlTemplateId;
    }

    private Task<int> UsageCountAsync(int templateId) =>
        context.Set<ClientType>().CountAsync(t => t.RiHtmlTemplateId == templateId)
            .ContinueWith(async types => types.Result + await context.Set<Client>().CountAsync(c => c.RiHtmlTemplateId == templateId))
            .Unwrap();

    private async Task<RiHtmlTemplateDto> ToDtoAsync(RiHtmlTemplate t) =>
        new(t.GuidId, t.Name, t.FileName, t.RegisteredAt, await UsageCountAsync(t.RiHtmlTemplateId));

    private async Task<ClientTypeDto> GetTypeDtoAsync(string guidId)
    {
        var type = await context.Set<ClientType>().Include(t => t.RiHtmlTemplate).FirstAsync(t => t.GuidId == guidId);
        var count = await context.Set<Client>().CountAsync(c => c.ClientTypeId == type.ClientTypeId);
        return ToDto(type, count);
    }

    private static ClientTypeDto ToDto(ClientType t, int clientCount) =>
        new(t.GuidId, t.Name, t.Description, t.RiHtmlTemplate?.GuidId, t.RiHtmlTemplate?.Name, t.SampleDataJson, clientCount);

    private static string ValidateTypeDto(ClientTypeSaveDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("El nombre del tipo de cliente es obligatorio.");
        }

        return name;
    }

    /// <summary>Acepta null/vacío; si trae contenido debe ser un objeto JSON plano de strings.</summary>
    private static string? NormalizeSamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                         ?? throw new InvalidOperationException("Los datos de muestra deben ser un objeto JSON.");
            return JsonSerializer.Serialize(parsed);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Los datos de muestra deben ser un objeto JSON plano de texto, ej. {\"Medico\":\"Dr. X\"}.");
        }
    }
}
```

- [ ] **Step 7: Registrar en DI**

En `ServiceCollectionExtensions.cs`, bajo `services.AddTransient<ICertificationRiModelService, Ri.CertificationRiModelService>();` agregar:
```csharp
        services.AddTransient<Ri.RiFormatService>();
        services.AddTransient<IRiFormatService>(sp => sp.GetRequiredService<Ri.RiFormatService>());
```

- [ ] **Step 8: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `Build succeeded`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~RiFormatResolverTests"` → 4 passed.

- [ ] **Step 9: Commit**
```bash
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Abstractions ZynstormECFPlatform.Services ZynstormECFPlatform.Tests
git commit -m "feat(ri): resolución de formato y servicio de tipos de cliente y formatos HTML

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 5: Integrar en `CertificationRiModelService` (preview, descarga y ZIP)

**Files:**
- Modify: `ZynstormECFPlatform.Abstractions/Services/ICertificationRiModelService.cs`, `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs`

**Interfaces:**
- Consumes: `RiFormatService.ResolveForClientAsync(int clientId) : ResolvedRiFormat`, `RiHtmlRenderer.Render(...)`, `RiPdfRenderer.Render(int, string, RiCompanyHeader?)`.
- Produces: `record RiRenderResult(byte[] Content, string ContentType, string FileName)` y `ICertificationRiModelService.RenderRiAsync(string clientGuidId, string ncf) : Task<RiRenderResult>`. `RenderRiForDocumentAsync` se conserva sin cambios de comportamiento. `RenderAllZipAsync` mantiene su firma.

- [ ] **Step 1: Ampliar la interfaz**

Reemplazar el contenido de `ICertificationRiModelService.cs`:
```csharp
namespace ZynstormECFPlatform.Abstractions.Services;

/// <param name="ContentType">"application/pdf" (formato por defecto) o "text/html" (formato HTML del cliente).</param>
public record RiRenderResult(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Renders the Ri (Representación Impresa) for certification documents, either
/// individually or in bulk as a ZIP, dispatching by e-CF type and by the client's format.
/// </summary>
public interface ICertificationRiModelService
{
    Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf);

    /// <summary>PDF por defecto, o HTML si el cliente (o su tipo) tiene un formato HTML.</summary>
    Task<RiRenderResult> RenderRiAsync(string clientGuidId, string ncf);

    Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath);
}
```

- [ ] **Step 2: Modificar el servicio**

En `CertificationRiModelService.cs`:
1. Constructor: `public class CertificationRiModelService(IClientService clientService, StorageContext context, RiFormatService formatService) : ICertificationRiModelService`.
2. Agregar `using System.Text;` ya existe; agregar `using ZynstormECFPlatform.Services.Ri.Html;`.
3. Agregar el método (junto a `RenderRiForDocumentAsync`, sin modificar éste):
```csharp
    public async Task<RiRenderResult> RenderRiAsync(string clientGuidId, string ncf)
    {
        var client = await clientService.GetByAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var format = await formatService.ResolveForClientAsync(client.ClientId);
        if (format.Template is null)
        {
            // Sin formato HTML: comportamiento original (PDF QuestPDF).
            return new RiRenderResult(await RenderRiForDocumentAsync(clientGuidId, ncf), "application/pdf", $"RI_{ncf}.pdf");
        }

        var doc = await context.Set<CertificationDocument>()
            .Include(d => d.CertificationProcess)
            .FirstOrDefaultAsync(d => d.CertificationProcess.ClientId == client.ClientId && d.ENcfSecuence == ncf)
            ?? throw new InvalidOperationException($"No se encontró el comprobante {ncf} para el cliente.");

        var company = await BuildCompanyHeaderAsync(client);
        var html = RiHtmlRenderer.Render(format.Template.Html, doc.XmlSent, company, format.SampleDataJson);
        return new RiRenderResult(Encoding.UTF8.GetBytes(html), "text/html", $"RI_{ncf}.html");
    }
```
4. En `RenderAllZipAsync`: justo después de `var company = await BuildCompanyHeaderAsync(client);` agregar `var format = await formatService.ResolveForClientAsync(client.ClientId);` y reemplazar el cuerpo del `try` dentro del `foreach`:
```csharp
                try
                {
                    byte[] bytes;
                    string extension;
                    if (format.Template is null)
                    {
                        bytes = RiPdfRenderer.Render(ParseEcfType(doc.EcfType.Code), doc.XmlSent, company);
                        extension = "pdf";
                    }
                    else
                    {
                        bytes = Encoding.UTF8.GetBytes(RiHtmlRenderer.Render(format.Template.Html, doc.XmlSent, company, format.SampleDataJson));
                        extension = "html";
                    }

                    var entry = archive.CreateEntry($"{doc.ENcfSecuence}.{extension}", CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(bytes);
                }
```
(el `catch` existente queda igual). Con `format.Template is null` el resultado es byte a byte el de antes.

- [ ] **Step 3: Compilar y regresión**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `Build succeeded`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo --filter "FullyQualifiedName~Ri"` → todo en verde.

- [ ] **Step 4: Commit**
```bash
git add ZynstormECFPlatform.Abstractions ZynstormECFPlatform.Services
git commit -m "feat(ri): usar el formato HTML del cliente en preview, descarga y ZIP

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 6: Controllers (nuevo `RiFormatController` y preview/download)

**Files:**
- Create: `ZynstormECFPlatform.Web.Api/Controllers/RiFormatController.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs` (acciones `PreviewRi` y `DownloadRi`, líneas ~496-530)

**Interfaces:**
- Consumes: `IRiFormatService` (Task 4), `ICertificationRiModelService.RenderRiAsync` (Task 5).
- Produces (HTTP, base `v1/RiFormat`): `GET client-types`, `POST client-types`, `PUT client-types/{guidId}`, `DELETE client-types/{guidId}`, `GET templates`, `POST templates` (JSON `RiHtmlTemplateUploadDto`), `DELETE templates/{guidId}`, `GET templates/sample`, `PUT clients/{clientGuidId}/assignment` (`ClientRiFormatAssignDto`), `GET clients/{clientGuidId}/resolved`. Preview/download existentes devuelven `text/html` si hay formato HTML.

- [ ] **Step 1: Crear el controller**

`ZynstormECFPlatform.Web.Api/Controllers/RiFormatController.cs`:
```csharp
using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>Tipos de cliente y biblioteca de formatos HTML de Representación Impresa.</summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class RiFormatController(IRiFormatService formatService) : ControllerBase
{
    [HttpGet("client-types")]
    public async Task<ActionResult<IReadOnlyList<ClientTypeDto>>> ListClientTypes() =>
        Ok(await formatService.ListClientTypesAsync());

    [HttpPost("client-types")]
    public Task<ActionResult> CreateClientType([FromBody] ClientTypeSaveDto dto) =>
        Run(async () => Ok(await formatService.CreateClientTypeAsync(dto)));

    [HttpPut("client-types/{guidId}")]
    public Task<ActionResult> UpdateClientType(string guidId, [FromBody] ClientTypeSaveDto dto) =>
        Run(async () => Ok(await formatService.UpdateClientTypeAsync(guidId, dto)));

    [HttpDelete("client-types/{guidId}")]
    public Task<ActionResult> DeleteClientType(string guidId) =>
        Run(async () =>
        {
            await formatService.DeleteClientTypeAsync(guidId);
            return NoContent();
        });

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<RiHtmlTemplateDto>>> ListTemplates() =>
        Ok(await formatService.ListTemplatesAsync());

    [HttpPost("templates")]
    public Task<ActionResult> UploadTemplate([FromBody] RiHtmlTemplateUploadDto dto) =>
        Run(async () =>
        {
            var result = await formatService.UploadTemplateAsync(dto);
            return result.Errors.Count > 0 ? UnprocessableEntity(result) : Ok(result);
        });

    [HttpDelete("templates/{guidId}")]
    public Task<ActionResult> DeleteTemplate(string guidId) =>
        Run(async () =>
        {
            await formatService.DeleteTemplateAsync(guidId);
            return NoContent();
        });

    [HttpGet("templates/sample")]
    public ActionResult DownloadSample() =>
        File(Encoding.UTF8.GetBytes(SampleHtml), "text/html", "formato-ri-ejemplo.html");

    [HttpPut("clients/{clientGuidId}/assignment")]
    public Task<ActionResult> Assign(string clientGuidId, [FromBody] ClientRiFormatAssignDto dto) =>
        Run(async () =>
        {
            await formatService.AssignToClientAsync(clientGuidId, dto);
            return NoContent();
        });

    [HttpGet("clients/{clientGuidId}/resolved")]
    public Task<ActionResult> Resolved(string clientGuidId) =>
        Run(async () => Ok(await formatService.GetResolvedAsync(clientGuidId)));

    private static async Task<ActionResult> Run(Func<Task<ActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    /// <summary>HTML de ejemplo con todos los marcadores soportados.</summary>
    private const string SampleHtml = """
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Factura</title>
<style>
 body{font-family:Arial,sans-serif;width:300px;margin:0 auto}
 .row{display:flex;justify-content:space-between}
 @media print{body{width:auto}}
</style></head>
<body>
 <div style="border:1px dashed #000;height:60px;text-align:center">{{EmisorLogo}}</div>
 <div style="text-align:center">{{EmisorDireccion}}</div>
 <div style="text-align:center"><b>RNC: {{EmisorRNC}}</b></div>
 <div style="text-align:center"><b>Tel.: {{EmisorTelefono}}</b></div>
 <h2><u>FACTURA</u></h2>
 <div style="text-align:center"><b>{{TipoComprobante}}</b></div>
 <div class="row"><span>eNCF:</span><span>{{eNCF}}</span></div>
 <div class="row"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
 <div class="row"><span>No. Factura:</span><b>{{NoFactura}}</b></div>
 <div class="row"><span>Fecha:</span><span>{{Fecha}}</span></div>
 <div class="row"><span>Nombre:</span><b>{{Nombre}}</b></div>
 <div class="row"><span>Forma Pago:</span><b>{{FormaPago}}</b></div>
 <table style="width:100%">
  <thead><tr><th align="left">Descripción</th><th align="right">Monto</th></tr></thead>
  <tbody><tr><td>{{Descripcion}}</td><td align="right">{{Monto}}</td></tr></tbody>
 </table>
 <div class="row"><b>Total:</b><b>{{Total}}</b></div>
 <div class="row"><b>Total Descuento:</b><b>{{TotalDescuento}}</b></div>
 <div style="border:1px dashed #000;width:110px;height:110px;margin:12px auto">{{QR}}</div>
 <div style="text-align:center"><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
 <div style="text-align:center"><b>Fecha firma digital:</b> {{FechaFirma}}</div>
 <div style="text-align:center"><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</body></html>
""";
}
```

- [ ] **Step 2: Actualizar preview/download en `CertificationController`**

Reemplazar los cuerpos de `PreviewRi` y `DownloadRi`:
```csharp
    [HttpGet("ri/{clientGuidId}/{ncf}/preview")]
    public async Task<ActionResult> PreviewRi(string clientGuidId, string ncf)
    {
        try
        {
            var result = await riModelService.RenderRiAsync(clientGuidId, ncf);
            ApplyHtmlSecurityHeaders(result);
            return File(result.Content, result.ContentType);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ri/{clientGuidId}/{ncf}/download")]
    public async Task<ActionResult> DownloadRi(string clientGuidId, string ncf)
    {
        try
        {
            var result = await riModelService.RenderRiAsync(clientGuidId, ncf);
            ApplyHtmlSecurityHeaders(result);
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>El HTML de un formato propio no debe poder ejecutar scripts ni cargar recursos externos.</summary>
    private void ApplyHtmlSecurityHeaders(RiRenderResult result)
    {
        if (result.ContentType == "text/html")
        {
            Response.Headers["Content-Security-Policy"] = "default-src 'none'; img-src data:; style-src 'unsafe-inline'";
        }
    }
```
(`RiRenderResult` ya está en `ZynstormECFPlatform.Abstractions.Services`, importado en este archivo.) Para PDF el resultado es el mismo que antes: `application/pdf`, nombre `RI_{ncf}.pdf`.

- [ ] **Step 3: Compilar y regresión**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` → `Build succeeded`.
Run: `dotnet test ZynstormECFPlatform.Tests -nologo` → todo en verde.

- [ ] **Step 4: Verificación manual del endpoint de ejemplo y del flujo (con la API levantada)**

`scratch/start-api.sh` (en `C:\Projects\ZynstormECF-WorkSpace\scratch`) arranca la API. Con un token válido:
```bash
curl -s -H "Authorization: Bearer $TOKEN" http://localhost:5000/v1/RiFormat/templates/sample | head -5   # devuelve el HTML de ejemplo
curl -s -X POST -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Royaltrix","fileName":"royaltrix.html","html":"<html><body><table><tr><td>{{Descripcion}}</td></tr></table>{{Total}}</body></html>"}' \
  http://localhost:5000/v1/RiFormat/templates                                                        # 200 con Template + Warnings
```
Expected: 200 con `template.guidId` y advertencias de `{{eNCF}}`/`{{QR}}`. Un HTML sin `{{Total}}` devuelve 422 con `errors`. Si no hay entorno local con BD, anotar en el commit que esta verificación quedó pendiente para el despliegue en Staging.

- [ ] **Step 5: Commit**
```bash
git add ZynstormECFPlatform.Web.Api
git commit -m "feat(ri): endpoints de tipos de cliente y formatos HTML; preview/descarga con formato propio

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 7: Frontend — tipos y servicio

**Files:**
- Create: `ZynstormECFPlatform-FrontEnd/types/riFormat.type.ts`, `ZynstormECFPlatform-FrontEnd/services/riFormat.service.ts`

**Interfaces:**
- Consumes: endpoints de Task 6; helpers `get/post/put/destroy` de `services/fetchHandler`.
- Produces: tipos `ClientType`, `ClientTypeSave`, `RiHtmlTemplate`, `RiHtmlTemplateUploadResult`, `ResolvedRiFormat`; funciones `listClientTypes`, `createClientType`, `updateClientType`, `deleteClientType`, `listRiTemplates`, `uploadRiTemplate`, `deleteRiTemplate`, `assignRiFormat`, `getResolvedRiFormat`, `riSampleHtmlUrl`.

- [ ] **Step 1: Tipos**

`types/riFormat.type.ts`:
```ts
export interface ClientType {
  guidId: string
  name: string
  description?: string | null
  riHtmlTemplateGuidId?: string | null
  riHtmlTemplateName?: string | null
  sampleDataJson?: string | null
  clientCount: number
}

export interface ClientTypeSave {
  name: string
  description?: string | null
  riHtmlTemplateGuidId?: string | null
  sampleDataJson?: string | null
}

export interface RiHtmlTemplate {
  guidId: string
  name: string
  fileName: string
  registeredAt: string
  inUseCount: number
}

export interface RiHtmlTemplateUploadResult {
  template?: RiHtmlTemplate | null
  errors: string[]
  warnings: string[]
}

export interface ResolvedRiFormat {
  source: "Default" | "ClientType" | "Client"
  clientTypeName?: string | null
  templateGuidId?: string | null
  templateName?: string | null
}
```

- [ ] **Step 2: Servicio**

`services/riFormat.service.ts`:
```ts
import { destroy, get, post, put } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import {
  ClientType,
  ClientTypeSave,
  ResolvedRiFormat,
  RiHtmlTemplate,
  RiHtmlTemplateUploadResult,
} from "@/types/riFormat.type"

const RI_FORMAT_URL = `${API_BASE_URL}/v1/RiFormat`

export const listClientTypes = () => get<ClientType[]>(`${RI_FORMAT_URL}/client-types`)

export const createClientType = (dto: ClientTypeSave) => post<ClientType>(`${RI_FORMAT_URL}/client-types`, dto)

export const updateClientType = (guidId: string, dto: ClientTypeSave) =>
  put<ClientType>(`${RI_FORMAT_URL}/client-types/${guidId}`, dto)

export const deleteClientType = async (guidId: string): Promise<void> => {
  await destroy<void>(`${RI_FORMAT_URL}/client-types/${guidId}`)
}

export const listRiTemplates = () => get<RiHtmlTemplate[]>(`${RI_FORMAT_URL}/templates`)

/**
 * Sube un HTML (texto) como formato. El backend responde 422 con { errors, warnings } si no es válido;
 * en ese caso se devuelve el resultado en lugar de lanzar para poder mostrar los errores.
 */
export const uploadRiTemplate = async (
  name: string,
  fileName: string,
  html: string,
): Promise<RiHtmlTemplateUploadResult> => {
  const response = await fetch(`${RI_FORMAT_URL}/templates`, {
    method: "POST",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ name, fileName, html }),
  })
  if (response.ok || response.status === 422) {
    return (await response.json()) as RiHtmlTemplateUploadResult
  }
  throw new Error((await response.text()) || `No se pudo subir el formato (HTTP ${response.status}).`)
}

export const deleteRiTemplate = async (guidId: string): Promise<void> => {
  await destroy<void>(`${RI_FORMAT_URL}/templates/${guidId}`)
}

export const assignRiFormat = async (
  clientGuidId: string,
  assignment: { clientTypeGuidId: string | null; riHtmlTemplateGuidId: string | null },
): Promise<void> => {
  await put<void>(`${RI_FORMAT_URL}/clients/${clientGuidId}/assignment`, assignment)
}

export const getResolvedRiFormat = (clientGuidId: string) =>
  get<ResolvedRiFormat>(`${RI_FORMAT_URL}/clients/${clientGuidId}/resolved`)

export const riSampleHtmlUrl = `${RI_FORMAT_URL}/templates/sample`
```
Verificar en `services/fetchHandler.ts` que `put`/`post` toleran respuestas `204 No Content` (para `assignRiFormat`/`destroy`); si `put<void>` intenta `response.json()` sobre un 204, ajustar el helper para devolver `undefined` cuando `response.status === 204`.

- [ ] **Step 3: Verificar tipos**

Run: `cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd && npx tsc --noEmit`
Expected: sin errores nuevos respecto a la línea base (anotar los errores previos antes de empezar con `npx tsc --noEmit 2>&1 | tail -5`).

- [ ] **Step 4: Commit**
```bash
git add types/riFormat.type.ts services/riFormat.service.ts services/fetchHandler.ts
git commit -m "feat(ri): servicio y tipos de formatos HTML de RI

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 8: Frontend — tarjeta "Formato de factura" y vista previa HTML

**Files:**
- Create: `ZynstormECFPlatform-FrontEnd/components/certification/ri-format-card.tsx`
- Modify: `ZynstormECFPlatform-FrontEnd/components/certification/ri-step5.tsx`

**Interfaces:**
- Consumes: `services/riFormat.service.ts` y `types/riFormat.type.ts` (Task 7); componentes shadcn existentes (`Card`, `Button`, `Select`, `Input`, `Textarea`, `Dialog`, `Label`, `Badge`, `Alert`).
- Produces: `<RiFormatCard clientGuidId={string} onFormatChanged={() => void} />`; `RiStep5` muestra la tarjeta, y su vista previa soporta HTML (iframe `sandbox`) además de PDF, con botón Imprimir.

- [ ] **Step 1: Verificar que existen los componentes UI**

Run: `ls components/ui | grep -E "^(select|textarea|input|label|badge|alert|dialog)\.tsx$"`
Expected: los 7 archivos. Si falta alguno (p. ej. `textarea.tsx`), agregarlo con `npx shadcn@latest add textarea`.

- [ ] **Step 2: Crear `ri-format-card.tsx`**

```tsx
"use client"

import { useCallback, useEffect, useRef, useState } from "react"
import { AlertCircle, CheckCircle2, Download, Loader2, Trash2, Upload } from "lucide-react"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Textarea } from "@/components/ui/textarea"
import {
  assignRiFormat,
  createClientType,
  deleteRiTemplate,
  getResolvedRiFormat,
  listClientTypes,
  listRiTemplates,
  riSampleHtmlUrl,
  updateClientType,
  uploadRiTemplate,
} from "@/services/riFormat.service"
import type { ClientType, ResolvedRiFormat, RiHtmlTemplate } from "@/types/riFormat.type"

const NONE = "__none__"

const SOURCE_LABEL: Record<ResolvedRiFormat["source"], string> = {
  Default: "Por defecto (PDF del sistema)",
  ClientType: "Del tipo de cliente",
  Client: "Propio de este cliente",
}

interface RiFormatCardProps {
  clientGuidId: string
  onFormatChanged: () => void
}

export function RiFormatCard({ clientGuidId, onFormatChanged }: RiFormatCardProps) {
  const [types, setTypes] = useState<ClientType[]>([])
  const [templates, setTemplates] = useState<RiHtmlTemplate[]>([])
  const [resolved, setResolved] = useState<ResolvedRiFormat | null>(null)
  const [selectedType, setSelectedType] = useState<string>(NONE)
  const [ownTemplate, setOwnTemplate] = useState<string>(NONE)
  const [newTypeName, setNewTypeName] = useState("")
  const [samples, setSamples] = useState("")
  const [uploadErrors, setUploadErrors] = useState<string[]>([])
  const [uploadWarnings, setUploadWarnings] = useState<string[]>([])
  const [message, setMessage] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  const currentType = types.find((t) => t.guidId === selectedType)

  const refresh = useCallback(async () => {
    if (!clientGuidId) return
    const [typeList, templateList, current] = await Promise.all([
      listClientTypes(),
      listRiTemplates(),
      getResolvedRiFormat(clientGuidId),
    ])
    setTypes(typeList)
    setTemplates(templateList)
    setResolved(current)
    return { typeList, current }
  }, [clientGuidId])

  useEffect(() => {
    void refresh()
      .then((r) => {
        if (!r) return
        // Cuál tipo/propio tiene asignado el cliente: se infiere del nombre resuelto.
        const assigned = r.typeList.find((t) => t.name === r.current.clientTypeName)
        setSelectedType(assigned?.guidId ?? NONE)
        setOwnTemplate(r.current.source === "Client" ? (r.current.templateGuidId ?? NONE) : NONE)
      })
      .catch((e) => setMessage(e instanceof Error ? e.message : "No se pudo cargar el formato."))
  }, [refresh])

  useEffect(() => {
    setSamples(currentType?.sampleDataJson ?? "")
  }, [currentType])

  const run = async (action: () => Promise<void>) => {
    setBusy(true)
    setMessage(null)
    try {
      await action()
    } catch (e) {
      setMessage(e instanceof Error ? e.message : "La operación falló.")
    } finally {
      setBusy(false)
    }
  }

  const saveAssignment = () =>
    run(async () => {
      await assignRiFormat(clientGuidId, {
        clientTypeGuidId: selectedType === NONE ? null : selectedType,
        riHtmlTemplateGuidId: ownTemplate === NONE ? null : ownTemplate,
      })
      await refresh()
      onFormatChanged()
      setMessage("Asignación guardada.")
    })

  const createType = () =>
    run(async () => {
      const created = await createClientType({ name: newTypeName.trim() })
      setNewTypeName("")
      await refresh()
      setSelectedType(created.guidId)
    })

  const saveSamples = () =>
    run(async () => {
      if (!currentType) return
      await updateClientType(currentType.guidId, {
        name: currentType.name,
        description: currentType.description,
        riHtmlTemplateGuidId: currentType.riHtmlTemplateGuidId,
        sampleDataJson: samples.trim() || null,
      })
      await refresh()
      onFormatChanged()
      setMessage("Datos de muestra guardados.")
    })

  const onFileChosen = (file: File | undefined, target: "type" | "client") => {
    if (!file) return
    void run(async () => {
      const html = await file.text()
      const name = file.name.replace(/\.html?$/i, "")
      const result = await uploadRiTemplate(name, file.name, html)
      setUploadErrors(result.errors)
      setUploadWarnings(result.warnings)
      if (!result.template) return
      if (target === "type" && currentType) {
        await updateClientType(currentType.guidId, {
          name: currentType.name,
          description: currentType.description,
          riHtmlTemplateGuidId: result.template.guidId,
          sampleDataJson: currentType.sampleDataJson,
        })
      } else if (target === "client") {
        setOwnTemplate(result.template.guidId)
      }
      await refresh()
      setMessage(`Formato "${result.template.name}" subido.`)
    })
    if (fileRef.current) fileRef.current.value = ""
  }

  const removeTemplate = (guidId: string) =>
    run(async () => {
      await deleteRiTemplate(guidId)
      await refresh()
    })

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-lg">Formato de factura</CardTitle>
        <CardDescription>
          Sin tipo de cliente ni formato propio se usa el PDF del sistema. Con un formato HTML se rellenan sus
          marcadores {"{{...}}"} con los datos del XML.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-5">
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <span className="text-muted-foreground">Formato en uso:</span>
          {resolved ? (
            <Badge variant={resolved.source === "Default" ? "secondary" : "default"}>
              {SOURCE_LABEL[resolved.source]}
              {resolved.templateName ? ` — ${resolved.templateName}` : ""}
            </Badge>
          ) : (
            <Loader2 className="h-4 w-4 animate-spin" />
          )}
        </div>

        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label>Tipo de cliente</Label>
            <Select value={selectedType} onValueChange={setSelectedType}>
              <SelectTrigger>
                <SelectValue placeholder="Ninguno" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NONE}>Ninguno (formato por defecto)</SelectItem>
                {types.map((t) => (
                  <SelectItem key={t.guidId} value={t.guidId}>
                    {t.name} {t.riHtmlTemplateName ? `· ${t.riHtmlTemplateName}` : "· sin formato"}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <div className="flex gap-2">
              <Input
                placeholder="Nuevo tipo (ej. Royaltrix Software)"
                value={newTypeName}
                onChange={(e) => setNewTypeName(e.target.value)}
              />
              <Button variant="outline" disabled={busy || !newTypeName.trim()} onClick={createType}>
                Crear
              </Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label>Formato propio de este cliente (excepción)</Label>
            <Select value={ownTemplate} onValueChange={setOwnTemplate}>
              <SelectTrigger>
                <SelectValue placeholder="Usar el del tipo" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={NONE}>Usar el del tipo de cliente</SelectItem>
                {templates.map((t) => (
                  <SelectItem key={t.guidId} value={t.guidId}>
                    {t.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Button variant="outline" disabled={busy} onClick={() => fileRef.current?.click()}>
              <Upload className="h-4 w-4" /> Subir otro HTML para este cliente
            </Button>
          </div>
        </div>

        <input
          ref={fileRef}
          type="file"
          accept=".html,.htm,text/html"
          className="hidden"
          onChange={(e) => onFileChosen(e.target.files?.[0], ownTemplate === NONE && currentType ? "type" : "client")}
        />

        {currentType && (
          <div className="space-y-2 rounded-lg border border-border p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <span className="text-sm font-medium">
                {currentType.name} · {currentType.clientCount} cliente(s)
              </span>
              <Button variant="outline" size="sm" disabled={busy} onClick={() => fileRef.current?.click()}>
                <Upload className="h-4 w-4" /> Subir/reemplazar HTML del tipo
              </Button>
            </div>
            <Label className="text-xs text-muted-foreground">
              Datos de muestra (JSON): Cedula, Sexo, FechaNac, Edad, SegMed, NSS, Devolucion, Medico, MontoPagado,
              Restante, EmisorLogo (data:image/...)
            </Label>
            <Textarea
              rows={5}
              className="font-mono text-xs"
              placeholder='{"Cedula":"001-0000000-1","Medico":"Dr. Ejemplo"}'
              value={samples}
              onChange={(e) => setSamples(e.target.value)}
            />
            <Button variant="outline" size="sm" disabled={busy} onClick={saveSamples}>
              Guardar datos de muestra
            </Button>
          </div>
        )}

        {uploadErrors.length > 0 && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              <ul className="list-disc pl-4">{uploadErrors.map((e) => <li key={e}>{e}</li>)}</ul>
            </AlertDescription>
          </Alert>
        )}
        {uploadWarnings.length > 0 && (
          <Alert>
            <AlertCircle className="h-4 w-4" />
            <AlertDescription>
              <ul className="list-disc pl-4">{uploadWarnings.map((w) => <li key={w}>{w}</li>)}</ul>
            </AlertDescription>
          </Alert>
        )}

        {templates.length > 0 && (
          <div className="space-y-1 text-sm">
            <span className="text-muted-foreground">Biblioteca de formatos</span>
            {templates.map((t) => (
              <div key={t.guidId} className="flex items-center justify-between rounded-md border border-border px-3 py-1.5">
                <span>
                  {t.name} <span className="text-xs text-muted-foreground">({t.inUseCount} en uso)</span>
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7 text-destructive"
                  disabled={busy || t.inUseCount > 0}
                  title={t.inUseCount > 0 ? "En uso: reasígnelo antes de borrar" : "Eliminar formato"}
                  onClick={() => removeTemplate(t.guidId)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}

        <div className="flex flex-wrap items-center justify-between gap-2">
          <Button variant="ghost" size="sm" onClick={() => window.open(riSampleHtmlUrl, "_blank")}>
            <Download className="h-4 w-4" /> Descargar HTML de ejemplo con los marcadores
          </Button>
          <div className="flex items-center gap-3">
            {message && (
              <span className="flex items-center gap-1 text-xs text-muted-foreground">
                <CheckCircle2 className="h-3.5 w-3.5" /> {message}
              </span>
            )}
            <Button disabled={busy} onClick={saveAssignment}>
              {busy ? <Loader2 className="h-4 w-4 animate-spin" /> : null} Guardar asignación
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

export default RiFormatCard
```
Nota: el `<input type="file">` se comparte; `onFileChosen` decide el destino (`type` si hay tipo seleccionado y no se eligió formato propio; `client` en caso contrario). El botón "Subir otro HTML para este cliente" solo sube el archivo a la biblioteca y lo deja seleccionado como formato propio hasta pulsar "Guardar asignación".

- [ ] **Step 3: Integrar en `ri-step5.tsx` (tarjeta + vista previa HTML + imprimir)**

Cambios en `components/certification/ri-step5.tsx`:
1. Imports: `import { useRef, useState } from "react"` (agregar `useRef`), `Printer` en el import de `lucide-react`, y `import { RiFormatCard } from "@/components/certification/ri-format-card"`.
2. Estado nuevo junto a los demás: 
```tsx
  const [previewKind, setPreviewKind] = useState<"pdf" | "html">("pdf")
  const iframeRef = useRef<HTMLIFrameElement>(null)
```
3. En `openPreview`, reemplazar el bloque desde `const buf = await res.arrayBuffer()` hasta `setPreviewUrl(URL.createObjectURL(blob))` por:
```tsx
      const contentType = res.headers.get("content-type") ?? ""
      const buf = await res.arrayBuffer()
      if (contentType.includes("text/html")) {
        // Formato HTML propio: se muestra en un iframe con sandbox (sin scripts).
        setPreviewKind("html")
        setPreviewUrl(URL.createObjectURL(new Blob([buf], { type: "text/html;charset=utf-8" })))
      } else {
        const bytes = new Uint8Array(buf)
        // Verifica que sea realmente un PDF (%PDF); si no, el endpoint devolvió otra cosa (error/HTML).
        const isPdf =
          bytes.length > 4 && bytes[0] === 0x25 && bytes[1] === 0x50 && bytes[2] === 0x44 && bytes[3] === 0x46
        if (!isPdf) {
          const snippet = new TextDecoder().decode(bytes.slice(0, 300)).trim()
          throw new Error(`El servidor no devolvió un PDF. Respuesta: ${snippet || "(vacía)"}`)
        }
        // Se fuerza el MIME a application/pdf (el proxy puede no propagar el Content-Type),
        // de lo contrario el iframe no lo renderiza como PDF.
        setPreviewKind("pdf")
        setPreviewUrl(URL.createObjectURL(new Blob([buf], { type: "application/pdf" })))
      }
```
(conservar los comentarios existentes sobre cookie httpOnly y blob).
4. Al inicio del JSX retornado, antes del primer `<Card>`: 
```tsx
      <RiFormatCard clientGuidId={clientGuidId} onFormatChanged={() => setFormatVersion((v) => v + 1)} />
```
y el estado `const [formatVersion, setFormatVersion] = useState(0)` (solo fuerza a re-renderizar; la lista de comprobantes no cambia). Si no se necesita el valor, usar `() => undefined` en su lugar y omitir el estado.
5. Cambiar el texto informativo: "Las representaciones impresas se generan automáticamente con las plantillas del sistema según el tipo de comprobante. No es necesario cargar un modelo." por "Si el cliente no tiene un formato HTML asignado, se usan las plantillas PDF del sistema según el tipo de comprobante."
6. En el `DialogContent`, reemplazar el `<iframe ...>` por:
```tsx
          ) : previewUrl ? (
            <>
              <iframe
                ref={iframeRef}
                src={previewUrl}
                className="h-[70vh] w-full rounded-md border border-border"
                title={previewTitle}
                {...(previewKind === "html" ? { sandbox: "allow-same-origin allow-modals" } : {})}
              />
              {previewKind === "html" && (
                <div className="flex justify-end">
                  <Button variant="outline" onClick={() => iframeRef.current?.contentWindow?.print()}>
                    <Printer className="h-4 w-4" /> Imprimir / Guardar PDF
                  </Button>
                </div>
              )}
            </>
          ) : null}
```
`sandbox="allow-same-origin allow-modals"` (sin `allow-scripts`) impide ejecutar scripts del HTML pero permite que la página padre llame a `print()` sobre el iframe del mismo origen (blob).
7. El botón "Descargar" por comprobante sigue usando `riDownloadUrl` (el backend devuelve `.html` o `.pdf` según el formato).

- [ ] **Step 4: Verificar tipos y lint**

Run: `npx tsc --noEmit` y `npx eslint components/certification/ri-format-card.tsx components/certification/ri-step5.tsx`
Expected: sin errores nuevos.

- [ ] **Step 5: Verificación visual**

Levantar con `scratch/start-fe.sh` y `scratch/start-api.sh`, ir a Certificación → Paso 5 con un cliente: (a) sin tipo ⇒ badge "Por defecto" y vista previa PDF idéntica a antes; (b) crear tipo "Royaltrix Software", subir el HTML de referencia, guardar datos de muestra, asignar ⇒ badge "Del tipo de cliente", vista previa HTML con ítems, QR y sin "VALIDO HASTA" en tipos 32/34; (c) botón Imprimir abre el diálogo del navegador. Si no se puede levantar el entorno, indicarlo explícitamente en el reporte (no declarar verificado).

- [ ] **Step 6: Commit**
```bash
git add components/certification
git commit -m "feat(ri): tarjeta de formato de factura y vista previa HTML en el Paso 5

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```

---

## Task 9: Replicar a `feature/emit-idempotente` (backend) y confirmar ramas

> Pendiente fijo del usuario: replicar en **`Staging`** (donde se trabajó, ya hecho) y **`feature/emit-idempotente`** (backend). El frontend solo tiene `Staging` (además de `Dev`/`main`, que no se tocan).

**Files:** ninguno nuevo; cherry-pick de los commits de Tasks 1–6 en `feature/emit-idempotente`.

- [ ] **Step 1: Listar los commits a replicar**

Run (en el backend): `git log --oneline Staging -12` — anotar los hashes de Tasks 1–6 (excluir los commits de documentación ya existentes si la otra rama ya los tiene).

- [ ] **Step 2: Cherry-pick de todo salvo la migración**

```bash
git checkout feature/emit-idempotente
git status --short                      # debe estar limpio; si no, detenerse y avisar
git cherry-pick <hash-task1> <hash-task2> <hash-task3> <hash-task4> <hash-task5> <hash-task6>
```
El commit de la Task 1 incluye la migración y el snapshot, que **divergen** entre ramas. Si el cherry-pick de Task 1 entra en conflicto en `StorageContext.cs` o `StorageContextModelSnapshot.cs`:
```bash
git cherry-pick --abort
git cherry-pick -n <hash-task1>
git restore --staged --worktree ZynstormECFPlatform.Data/Migrations      # descartar migración y snapshot de Staging
git cherry-pick --continue --no-edit
dotnet ef migrations add AddRiHtmlFormats -p ZynstormECFPlatform.Data -s ZynstormECFPlatform.Web.Api
git add ZynstormECFPlatform.Data/Migrations
git commit -m "feat(ri): migración AddRiHtmlFormats para emit-idempotente

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>"
```
(Es decir: se conservan las entidades/configuración y se **regenera** la migración sobre el snapshot de esta rama.) Revisar la migración regenerada igual que en Task 1 Step 6 (solo tablas/columnas nuevas).

- [ ] **Step 3: Compilar y probar en `feature/emit-idempotente`**

Run: `dotnet build ZynstormECFPlatform.slnx -nologo -v q` y `dotnet test ZynstormECFPlatform.Tests -nologo`
Expected: build y tests en verde. Resolver cualquier conflicto de compilación (por ejemplo cambios de firma en `CertificationController` o `ServiceCollectionExtensions` propios de esa rama) sin alterar el comportamiento por defecto del PDF.

- [ ] **Step 4: Volver a `Staging` y reportar**

```bash
git checkout Staging
git log --oneline -3
git log --oneline feature/emit-idempotente -3
```
Reportar al usuario: commits aplicados en cada rama, resultado de build/tests en ambas, que no se hizo push, y qué verificación manual (Task 6 Step 4, Task 8 Step 5) quedó pendiente si no hubo entorno. **Preguntar** si desea hacer push de `Staging` y `feature/emit-idempotente` (y del frontend `Staging`). Actualizar la memoria `ri-html-replicate-branches` marcando qué ramas ya recibieron los cambios.

---

## Self-Review

**Cobertura de la spec**
- §2 no romper lo existente → Global Constraints; Task 3 Step 5 y Task 5 Step 3 (regresión `Ri*`); Task 5 conserva `RenderRiForDocumentAsync` y la rama `Template is null` es el código original; Task 4 `RiFormatResolverTests.Resolve_ClientWithoutTypeNorTemplate_IsDefault`.
- §3 modelo + resolución + migración → Task 1 y Task 4 (resolver).
- §4 marcadores → `RiHtmlMarkers` (Task 2) y `BuildValues`/`ItemValues` (Task 3); ajuste `ItemItbis` documentado.
- §5 datos de muestra → `ParseSamples`/`Sample`, `ClientType.SampleDataJson`, `NormalizeSamples` (Tasks 3, 4) y UI (Task 8).
- §6 reglas de render (ítems, líneas vacías, bloques inyectados, QR, escapado, FechaVencimiento 32/34) → Task 3 (cada regla tiene test).
- §7 validación/seguridad → Task 2 (sanitizador + validador) y Task 6 (CSP) y Task 8 (`sandbox`).
- §8 API → Task 6 (ruta distinta a la spec, documentada en Decisiones).
- §9 frontend → Tasks 7–8.
- §10 pruebas → incluidas en Tasks 2–4; DB no se prueba con tests (no hay proveedor en memoria en el proyecto de tests): la lógica de resolución es pura y está cubierta, y el CRUD se verifica manualmente (Task 6 Step 4).
- §12 replicación → Task 9.

**Placeholders:** ninguno ("sanity" del test 1 está resuelto con instrucción explícita de borrarlo).

**Consistencia de tipos:** `RiHtmlRenderer.Render(string templateHtml, string signedXml, RiCompanyHeader? company, string? sampleDataJson)` coincide entre Task 3 (definición/tests) y Task 5 (uso). `ResolvedRiFormat`/`RiFormatSource` (Task 4) coinciden con su uso en Task 5. `RiRenderResult(byte[] Content, string ContentType, string FileName)` coincide en Tasks 5 y 6. DTOs (Task 4) coinciden con tipos TS (Task 7): `clientCount`, `inUseCount`, `riHtmlTemplateGuidId`, `source` como string. `RiFormatService` se registra como clase concreta y como `IRiFormatService` (Task 4 Step 7) porque `CertificationRiModelService` inyecta la clase concreta (Task 5).

**Riesgos conocidos**
- El motor de líneas vacías depende de heurística de DOM (fila = ancestro con texto estático). Si el HTML real del usuario tiene otra estructura, ajustar `RemoveEmptyLines`; hay tests con el HTML de referencia.
- Si la versión de AngleSharp difiere de 1.3.0, la API usada (`Descendants<T>()`, `Closest`, `InsertBefore`, `Clone`) es estable en 1.x.
