# RI Fidelidad EasyInvoice + Client.Address — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Llevar las plantillas RI (Representación Impresa) a paridad con los PDFs reales de EasyInvoice (campos y variantes por tipo de e-CF), corregir el mapeo del tipo 41, agregar la plantilla de gastos (43) y la columna `Address` al Client.

**Architecture:** Los XMLs firmados se mapean con `EcfRiDataMapper` (XML→`RiData`) y `EcfRiTemplateMapper` (`RiData`→view-models); las plantillas QuestPDF (`RiInvoicePdf`, `RiPurchasePdf`, nueva `RiExpensePdf`) solo leen view-models. `RiPdfRenderer` rutea por tipo. El encabezado de empresa se sobreescribe con `RiCompanyHeader` construido desde el `Client`.

**Tech Stack:** .NET 10 (correr `dotnet` vía WSL: `wsl -e bash -lc "..."`), QuestPDF, QRCoder, xUnit + PdfPig (tests), EF Core (migraciones en `ZynstormECFPlatform.Data`), Next.js/TypeScript (frontend).

**Spec:** `Docs/superpowers/specs/2026-07-02-ri-fidelidad-easyinvoice-design.md`

## Global Constraints

- Repo backend: `/home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform` (rama `feature/ri-modelo-pdf`). Frontend: `/home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd`.
- Todos los comandos dotnet se ejecutan desde Windows con `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && <cmd>"`.
- DGII: tipo **33 = Nota de Débito**, **34 = Nota de Crédito** (EasyInvoice usa E33/E34 así; el código actual los tiene invertidos y se corrige).
- CAJERO / ATENDIDO POR / USUARIO en las RI: constante `"PEDRO"`.
- TOTAL RECIBIDO (solo contado): total si es entero, si no `Math.Ceiling(total)`; SU CAMBIO = recibido − total.
- Campos opcionales ausentes en el XML → la fila/bloque se omite (no imprimir "N/D" salvo RNC/CED del cliente, que ya lo hace).
- Test suite: `wsl -e bash -lc "cd ... && dotnet test ZynstormECFPlatform.Tests -v q"` debe quedar verde al final de cada task.

---

### Task 1: Columna Address en Client (backend + migración + encabezado RI)

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs` (bloque `modelBuilder.Entity<Client>` ~línea 381)
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs` (`BuildCompanyHeaderAsync`, ~línea 91)
- Create (generada): `ZynstormECFPlatform.Data/Migrations/*_AddClientAddress.cs`

**Interfaces:**
- Produces: `Client.Address` (`string?`), `ClientCreateDto.Address` (`string?`, hereda a Update/View). AutoMapper mapea por convención (no tocar `MappingProfiles`).

- [ ] **Step 1: Agregar la propiedad a la entidad**

En `Client.cs`, después de `public string Rnc { get; set; } = null!;`:

```csharp
    public string? Address { get; set; }
```

- [ ] **Step 2: Configurar longitud en StorageContext**

Dentro de `modelBuilder.Entity<Client>(entity => { ... })`, después del bloque de `e.Rnc`:

```csharp
            entity.Property(e => e.Address)
                  .HasMaxLength(300)
                  .IsUnicode(false);
```

- [ ] **Step 3: Agregar al DTO**

En `ClientDtos.cs`, dentro de `ClientCreateDto` después de `Phone`:

```csharp
    [StringLength(300)]
    public string? Address { get; set; }
```

- [ ] **Step 4: Priorizar la dirección del cliente en el encabezado de la RI**

En `CertificationRiModelService.BuildCompanyHeaderAsync`, reemplazar:

```csharp
            Address: branch?.Address ?? string.Empty,
```

por:

```csharp
            Address: !string.IsNullOrWhiteSpace(client.Address) ? client.Address! : branch?.Address ?? string.Empty,
```

Y actualizar el doc-comment del método: la dirección sale de `Client.Address` con fallback a la sucursal principal.

- [ ] **Step 5: Generar la migración**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet ef migrations add AddClientAddress --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api"`
Expected: `Done.` y un nuevo archivo `*_AddClientAddress.cs` en `ZynstormECFPlatform.Data/Migrations` cuyo `Up` hace `AddColumn<string>(name: "Address", table: "Clients", maxLength: 300, nullable: true, ...)`. Verificar el contenido del archivo generado (solo debe tocar la columna Address).

- [ ] **Step 6: Build**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet build ZynstormECFPlatform.slnx -v q"`
Expected: Build succeeded, 0 errores.

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Core/Entities/Client.cs ZynstormECFPlatform.Data/ ZynstormECFPlatform.Dtos/ClientDtos.cs ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs
git commit -m "feat(client): columna Address + prioridad en encabezado RI"
```

---

### Task 2: Campo Address en el frontend de clientes

**Files:**
- Modify: `../ZynstormECFPlatform-FrontEnd/types/client.type.ts`
- Modify: `../ZynstormECFPlatform-FrontEnd/app/clientes/page.tsx`

**Interfaces:**
- Consumes: el backend acepta/retorna `address` en los DTOs de cliente (Task 1).

- [ ] **Step 1: Tipos**

En `client.type.ts`, agregar a `Client` (después de `phone`):

```ts
  address?: string | null
```

y a `ClientCreate` (después de `phone`):

```ts
  address?: string | null
```

- [ ] **Step 2: Formulario**

En `app/clientes/page.tsx`:

1. En el objeto de formulario vacío (~línea 70, donde están `email: "", phone: ""`), agregar `address: "",`.
2. Donde se puebla el form al editar (~línea 244, `email: client.email ?? "", phone: client.phone ?? "",`), agregar `address: client.address ?? "",`.
3. Donde se arma el payload (~línea 258, `email: ..., phone: ...`), agregar `address: formData.address?.trim() || null,`.
4. Después del `Field` de Teléfono (~línea 451-461), agregar un campo siguiendo exactamente el mismo patrón:

```tsx
                    <Field>
                      <FieldLabel htmlFor="address">Dirección</FieldLabel>
                      <Input
                        id="address"
                        value={formData.address ?? ""}
                        onChange={(event) =>
                          setFormData((current) => ({ ...current, address: event.target.value }))
                        }
                        placeholder="Ej: Av. Estrella Sadhalá #45, Santiago"
                        maxLength={300}
                      />
                    </Field>
```

(Sin validación extra: es texto libre opcional.)

- [ ] **Step 3: Verificar compilación TS**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd && npx tsc --noEmit"`
Expected: sin errores (o solo errores preexistentes no relacionados; verificar con `git stash && npx tsc --noEmit` si hay duda).

- [ ] **Step 4: Commit (repo frontend)**

```bash
cd ../ZynstormECFPlatform-FrontEnd
git add types/client.type.ts app/clientes/page.tsx
git commit -m "feat(clientes): campo direccion en formulario de cliente"
```

---

### Task 3: EcfRiDataMapper — campos nuevos del XML + fix de subtotal exento

**Files:**
- Modify: `ZynstormECFPlatform.Services/Ri/RiData.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/EcfRiDataMapper.cs`
- Modify: `ZynstormECFPlatform.Tests/Ri/EcfRiDataMapperTests.cs`
- Create: fixtures `ZynstormECFPlatform.Tests/Ri/Fixtures/Paso_E410000000007.xml`, `Paso_E430000000008.xml`, `Paso_E340000000002.xml`

**Interfaces:**
- Produces (consumido por Tasks 4-7): en `RiData`: `string FechaVencimientoSecuencia`, `int TipoPago`, `string FechaLimitePago`, `string TerminoPago`, `string NumeroFacturaInterna`, `string NcfModificado`, `string CodigoModificacion`, `string RazonModificacion`. En `RiItem`: `int UnidadMedida`, `int IndicadorFacturacion`, `decimal Discount`. En `RiTotals`: `decimal ItbisRetenido`, `decimal IsrRetencion`, `decimal Itbis1Rate`, `decimal Itbis2Rate`, `decimal Itbis3Rate`. Y `Totals.SubTotal` pasa a ser `MontoGravadoTotal + MontoExento`.

- [ ] **Step 1: Copiar fixtures reales de certificación**

```bash
cd ZynstormECFPlatform
cp ZynstormECFPlatform.Web.Api/wwwroot/certification_files/0d126d84/132293894E410000000007.xml ZynstormECFPlatform.Tests/Ri/Fixtures/Paso_E410000000007.xml
cp ZynstormECFPlatform.Web.Api/wwwroot/certification_files/ea34701f/132878191E430000000008.xml ZynstormECFPlatform.Tests/Ri/Fixtures/Paso_E430000000008.xml
cp ZynstormECFPlatform.Web.Api/wwwroot/certification_files/ea34701f/132878191E340000000002.xml ZynstormECFPlatform.Tests/Ri/Fixtures/Paso_E340000000002.xml
```

(El csproj ya copia `Ri\Fixtures\**\*.xml` al output.)

Contenido relevante de cada fixture (para las aserciones):
- **E41** (`Paso_E410000000007.xml`): Emisor=DOCUMENTOS ELECTRONICOS DE 02 (RNC 132293894), Comprador=DOCUMENTOS ELECTRONICOS DE 11 (533445861), `MontoGravadoTotal=16064.05`, `TotalITBIS=2891.53`, `MontoTotal=18955.58`, `TotalITBISRetenido=2846.53`, `TotalISRRetencion=1606.41`, `ITBIS1=18`, 5 ítems con `IndicadorFacturacion=1`.
- **E43** (`Paso_E430000000008.xml`): sin `Comprador`, sin `TipoPago`; `FechaVencimientoSecuencia=31-12-2028`; Totales solo `MontoExento=4950.00`, `MontoTotal=4950.00`; 1 ítem `NombreItem="Gasto personal en comida (kiosko)"`, `UnidadMedida=43`, `IndicadorFacturacion=4`.
- **E34** (`Paso_E340000000002.xml`): `TipoPago=1`, sin `FechaVencimientoSecuencia`, `NumeroFacturaInterna=123456789016`, `InformacionReferencia` con `NCFModificado=E310000000034`, `CodigoModificacion=3`, `RazonModificacion=Error en monto`.
- **E31 existente** (`Paso_1_E310000000402.xml`): `FechaVencimientoSecuencia=31-12-2028`, `TipoPago=2`, `FechaLimitePago=02-05-2026`, `TerminoPago=15 DIAS`, sin `NumeroFacturaInterna`, `MontoExento=6001.00` (sin `MontoGravadoTotal`), ítem con `UnidadMedida=43`.

- [ ] **Step 2: Tests que fallan**

Agregar a `EcfRiDataMapperTests.cs`:

```csharp
    [Fact]
    public void E31_Exento_SubTotalIncludesMontoExento_AndExtractsIdDocFields()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(6001.00m, data.Totals.SubTotal); // MontoGravadoTotal(0) + MontoExento(6001)
        Assert.Equal("31-12-2028", data.FechaVencimientoSecuencia);
        Assert.Equal(2, data.TipoPago);
        Assert.Equal("02-05-2026", data.FechaLimitePago);
        Assert.Equal("15 DIAS", data.TerminoPago);
        Assert.Equal(string.Empty, data.NumeroFacturaInterna);
        Assert.Equal(43, data.Items[0].UnidadMedida);
    }

    [Fact]
    public void E41_ExtractsRetentions_AndItbisRates()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(16064.05m, data.Totals.SubTotal);
        Assert.Equal(2846.53m, data.Totals.ItbisRetenido);
        Assert.Equal(1606.41m, data.Totals.IsrRetencion);
        Assert.Equal(18m, data.Totals.Itbis1Rate);
        Assert.Equal(5, data.Items.Count);
        Assert.Equal(1, data.Items[0].IndicadorFacturacion);
    }

    [Fact]
    public void E34_ExtractsInformacionReferencia()
    {
        var data = EcfRiDataMapper.Map(Load("Paso_E340000000002.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("E310000000034", data.NcfModificado);
        Assert.Equal("3", data.CodigoModificacion);
        Assert.Equal("Error en monto", data.RazonModificacion);
        Assert.Equal("123456789016", data.NumeroFacturaInterna);
        Assert.Equal(string.Empty, data.FechaVencimientoSecuencia);
    }
```

- [ ] **Step 3: Verificar que fallan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter EcfRiDataMapperTests -v q"`
Expected: FAIL por compilación (los campos no existen en `RiData`).

- [ ] **Step 4: Implementación**

En `RiData.cs` — agregar a `RiData` (después de `TipoeCF`):

```csharp
    public string FechaVencimientoSecuencia { get; set; } = string.Empty;

    /// <summary>1=Contado, 2=Crédito, 0=ausente.</summary>
    public int TipoPago { get; set; }

    public string FechaLimitePago { get; set; } = string.Empty;

    public string TerminoPago { get; set; } = string.Empty;

    public string NumeroFacturaInterna { get; set; } = string.Empty;

    public string NcfModificado { get; set; } = string.Empty;

    public string CodigoModificacion { get; set; } = string.Empty;

    public string RazonModificacion { get; set; } = string.Empty;
```

A `RiItem`:

```csharp
    /// <summary>Código DGII de unidad de medida (1-62); 0 si el XML no lo trae.</summary>
    public int UnidadMedida { get; set; }

    public int IndicadorFacturacion { get; set; }

    public decimal Discount { get; set; }
```

A `RiTotals`:

```csharp
    public decimal ItbisRetenido { get; set; }

    public decimal IsrRetencion { get; set; }

    /// <summary>Tasas ITBIS de Totales (ej. 18, 16, 0).</summary>
    public decimal Itbis1Rate { get; set; }

    public decimal Itbis2Rate { get; set; }

    public decimal Itbis3Rate { get; set; }
```

En `EcfRiDataMapper.Map`, después de `var rncComprador = ...` agregar:

```csharp
        var infoReferencia = First(doc, "InformacionReferencia");
```

y en el `return new RiData { ... }` agregar (junto a `TipoeCF`):

```csharp
            FechaVencimientoSecuencia = Value(idDoc, "FechaVencimientoSecuencia"),
            TipoPago = (int)DecimalValue(idDoc, "TipoPago"),
            FechaLimitePago = Value(idDoc, "FechaLimitePago"),
            TerminoPago = Value(idDoc, "TerminoPago"),
            NumeroFacturaInterna = Value(emisor, "NumeroFacturaInterna"),
            NcfModificado = Value(infoReferencia, "NCFModificado"),
            CodigoModificacion = Value(infoReferencia, "CodigoModificacion"),
            RazonModificacion = Value(infoReferencia, "RazonModificacion"),
```

En el mismo `return`, en `Totals`, reemplazar `SubTotal = DecimalValue(totales, "MontoGravadoTotal"),` por:

```csharp
                SubTotal = DecimalValue(totales, "MontoGravadoTotal") + DecimalValue(totales, "MontoExento"),
```

y agregar:

```csharp
                ItbisRetenido = DecimalValue(totales, "TotalITBISRetenido"),
                IsrRetencion = DecimalValue(totales, "TotalISRRetencion"),
                Itbis1Rate = DecimalValue(totales, "ITBIS1"),
                Itbis2Rate = DecimalValue(totales, "ITBIS2"),
                Itbis3Rate = DecimalValue(totales, "ITBIS3"),
```

En `BuildItems`, dentro del `Select`, agregar:

```csharp
            UnidadMedida = (int)DecimalValue(item, "UnidadMedida"),
            IndicadorFacturacion = (int)DecimalValue(item, "IndicadorFacturacion"),
            Discount = DecimalValue(item, "DescuentoMonto")
```

- [ ] **Step 5: Verificar que pasan (los 3 nuevos + los 3 existentes)**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter EcfRiDataMapperTests -v q"`
Expected: 6 passed.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiData.cs ZynstormECFPlatform.Services/Ri/EcfRiDataMapper.cs ZynstormECFPlatform.Tests/Ri/
git commit -m "feat(ri): extraer campos IdDoc/retenciones/referencia del XML y fix subtotal exento"
```

---

### Task 4: MapInvoice + RiInvoiceModel — campos y reglas nuevas (incluye fix 33/34)

**Files:**
- Modify: `ZynstormECFPlatform.Services/Ri/RiInvoiceModel.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs`
- Modify: `ZynstormECFPlatform.Tests/Ri/EcfRiTemplateMapperTests.cs`

**Interfaces:**
- Consumes: campos nuevos de `RiData` (Task 3).
- Produces (consumido por Task 5): en `RiInvoiceModel`: `string ValidUntil`, `string InternalInvoiceNumber`, `string PaymentCondition`, `bool IsCredit`, `string Cashier`, `decimal ReceivedAmount`, `decimal ChangeAmount`, `string AffectedNcf`, `string ModificationCode`, `string ModificationReason`; en `RiInvoiceItem`: `string Unit`. Helpers `internal static` en `EcfRiTemplateMapper` (consumidos por Tasks 6/7): `PaymentTypeLabel(int)`, `UnitAbbreviation(int)`, `FormatDate(string)`.

- [ ] **Step 1: Tests que fallan**

Agregar a `EcfRiTemplateMapperTests.cs`:

```csharp
    [Fact]
    public void MapInvoice_E31_Credito_PopulatesNewFields()
    {
        // Paso_1_E310000000402.xml: TipoPago=2, TerminoPago="15 DIAS",
        // FechaVencimientoSecuencia=31-12-2028, sin NumeroFacturaInterna,
        // MontoExento=6001 (exento), item con UnidadMedida=43.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_1_E310000000402.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("31/12/2028", model.ValidUntil);
        Assert.Equal(string.Empty, model.InternalInvoiceNumber);
        Assert.Equal("CRÉDITO", model.PaymentType);
        Assert.True(model.IsCredit);
        Assert.Equal("15 DIAS", model.PaymentCondition);
        Assert.Equal("PEDRO", model.Cashier);
        Assert.Equal(6001.00m, model.SubTotal);
        Assert.Equal("Und", model.Items[0].Unit);
        Assert.Equal(6001.00m, model.ReceivedAmount); // entero -> igual al total
        Assert.Equal(0m, model.ChangeAmount);
    }

    [Fact]
    public void MapInvoice_E34_IsCreditNote_WithReferenceInfo()
    {
        // Paso_E340000000002.xml: TipoeCF=34 (Nota de CRÉDITO según DGII),
        // NCFModificado=E310000000034, CodigoModificacion=3, RazonModificacion="Error en monto".
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_E340000000002.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(34, model.EcfType);
        Assert.Equal("NOTA DE CRÉDITO ELECTRÓNICA", model.NcfTypeName);
        Assert.Equal("E310000000034", model.AffectedNcf);
        Assert.StartsWith("3 - ", model.ModificationCode);
        Assert.Equal("Error en monto", model.ModificationReason);
        Assert.Equal("123456789016", model.InternalInvoiceNumber);
        Assert.Equal(string.Empty, model.ValidUntil);
        Assert.Equal(566695.00m, model.ReceivedAmount);
    }

    [Fact]
    public void MapInvoice_ReceivedAmount_RoundsUpDecimals()
    {
        // E41 fixture reutilizado solo por sus totales: MontoTotal=18955.58 -> recibido 18956, cambio 0.42.
        var model = EcfRiTemplateMapper.MapInvoice(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal(18956m, model.ReceivedAmount);
        Assert.Equal(0.42m, model.ChangeAmount);
    }
```

- [ ] **Step 2: Verificar que fallan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter EcfRiTemplateMapperTests -v q"`
Expected: FAIL por compilación (campos no existen).

- [ ] **Step 3: Extender RiInvoiceModel**

En `RiInvoiceModel.cs`, agregar a `RiInvoiceModel` (después de `PaymentType`):

```csharp
    /// <summary>FechaVencimientoSecuencia formateada dd/MM/yyyy; vacío si el XML no la trae.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    /// <summary>NumeroFacturaInterna del emisor; vacío si el XML no lo trae (la fila se omite).</summary>
    public string InternalInvoiceNumber { get; set; } = string.Empty;

    /// <summary>"CONTADO" / "N DÍAS" (TerminoPago del XML si viene); vacío si no aplica.</summary>
    public string PaymentCondition { get; set; } = string.Empty;

    /// <summary>true cuando TipoPago=2 (crédito): footer CRÉDITO + FIRMA REQUERIDA, sin recibido/cambio.</summary>
    public bool IsCredit { get; set; }

    public string Cashier { get; set; } = string.Empty;

    public decimal ReceivedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    /// <summary>InformacionReferencia/NCFModificado (solo notas 33/34).</summary>
    public string AffectedNcf { get; set; } = string.Empty;

    public string ModificationCode { get; set; } = string.Empty;

    public string ModificationReason { get; set; } = string.Empty;
```

y a `RiInvoiceItem`:

```csharp
    /// <summary>Abreviatura de la unidad de medida DGII ("Und", "Caja", ...); vacío si el XML no la trae.</summary>
    public string Unit { get; set; } = string.Empty;
```

- [ ] **Step 4: Extender MapInvoice y helpers**

En `EcfRiTemplateMapper.cs`:

1. Corregir `NcfTypeName` (33/34 invertidos):

```csharp
        33 => "NOTA DE DÉBITO ELECTRÓNICA",
        34 => "NOTA DE CRÉDITO ELECTRÓNICA",
```

2. En `MapInvoice`, antes del `return`:

```csharp
        var received = data.Totals.Total == decimal.Truncate(data.Totals.Total)
            ? data.Totals.Total
            : Math.Ceiling(data.Totals.Total);
```

3. En el `return new RiInvoiceModel { ... }` agregar:

```csharp
            ValidUntil = FormatDate(data.FechaVencimientoSecuencia),
            InternalInvoiceNumber = data.NumeroFacturaInterna,
            PaymentType = PaymentTypeLabel(data.TipoPago),
            PaymentCondition = PaymentCondition(data),
            IsCredit = data.TipoPago == 2,
            Cashier = CertificationCashier,
            ReceivedAmount = received,
            ChangeAmount = received - data.Totals.Total,
            AffectedNcf = data.NcfModificado,
            ModificationCode = ModificationCodeLabel(data.CodigoModificacion),
            ModificationReason = data.RazonModificacion,
            Discount = data.Items.Sum(item => item.Discount),
```

y en el `ConvertAll` de ítems agregar:

```csharp
                Unit = UnitAbbreviation(item.UnidadMedida),
                Discount = item.Discount
```

4. Agregar los helpers al final de la clase:

```csharp
    /// <summary>Nombre fijo usado en las RI de certificación (decisión de producto).</summary>
    internal const string CertificationCashier = "PEDRO";

    internal static string PaymentTypeLabel(int tipoPago) => tipoPago switch
    {
        1 => "CONTADO",
        2 => "CRÉDITO",
        _ => string.Empty
    };

    /// <summary>"dd-MM-yyyy" del XML -> "dd/MM/yyyy" para mostrar; vacío se preserva.</summary>
    internal static string FormatDate(string xmlDate) => xmlDate.Replace('-', '/');

    private static string PaymentCondition(RiData data)
    {
        if (!string.IsNullOrEmpty(data.TerminoPago))
        {
            return data.TerminoPago;
        }

        if (data.TipoPago == 1)
        {
            return "CONTADO";
        }

        if (data.TipoPago == 2)
        {
            var days = DaysBetween(data.FechaEmision, data.FechaLimitePago);
            if (days <= 0)
            {
                days = 30;
            }
            return $"{days} DÍAS";
        }

        return string.Empty;
    }

    private static int DaysBetween(string fromDdMmYyyy, string toDdMmYyyy) =>
        DateTime.TryParseExact(fromDdMmYyyy, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from)
        && DateTime.TryParseExact(toDdMmYyyy, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to)
            ? (int)(to.Date - from.Date).TotalDays
            : 0;

    /// <summary>Códigos DGII de unidad de medida (enum UnitOfMeasure) -> abreviatura corta del recibo.</summary>
    internal static string UnitAbbreviation(int code) => code switch
    {
        0 => string.Empty,
        2 => "Bolsa",
        5 => "Bot",
        6 => "Caja",
        12 => "Día",
        13 => "Doc",
        15 => "Gal",
        17 => "g",
        19 => "Hora",
        21 => "Kg",
        23 => "Lb",
        24 => "L",
        26 => "m",
        27 => "m²",
        28 => "m³",
        30 => "Min",
        31 => "Paq",
        32 => "Par",
        34 => "Pza",
        39 => "Ton",
        45 => "Millar",
        46 => "Saco",
        47 => "Lata",
        59 => "ml",
        60 => "mg",
        61 => "Oz",
        _ => "Und"
    };

    /// <summary>Catálogo DGII de códigos de modificación (InformacionReferencia).</summary>
    private static string ModificationCodeLabel(string code) => code switch
    {
        "1" => "1 - Anula el NCF modificado",
        "2" => "2 - Corrige texto del NCF modificado",
        "3" => "3 - Corrige montos del NCF modificado",
        "4" => "4 - Reemplazo NCF emitido en contingencia",
        "5" => "5 - Referencia Factura de Consumo Electrónica",
        "" => string.Empty,
        _ => code
    };
```

(`using System.Globalization;` ya está en el archivo.)

- [ ] **Step 5: Verificar que pasan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter EcfRiTemplateMapperTests -v q"`
Expected: 5 passed (2 existentes + 3 nuevos).

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiInvoiceModel.cs ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs ZynstormECFPlatform.Tests/Ri/EcfRiTemplateMapperTests.cs
git commit -m "feat(ri): MapInvoice completo (valido hasta, pago, cajero, referencia NC/ND) y fix 33/34"
```

---

### Task 5: RiInvoicePdf — paridad total con InvoicePdf de EasyInvoice

**Files:**
- Modify: `ZynstormECFPlatform.Services/Ri/RiInvoicePdf.cs` (se reescribe `Compose`)
- Modify: `ZynstormECFPlatform.Tests/Ri/RiInvoicePdfTests.cs`

**Interfaces:**
- Consumes: `RiInvoiceModel` con los campos de Task 4.

- [ ] **Step 1: Tests que fallan**

Agregar a `RiInvoicePdfTests.cs` (reutiliza el `using UglyToad.PdfPig;` existente). Helper local y dos tests:

```csharp
    private static string PdfText(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
    }

    [Fact]
    public void GeneratePdf_Contado_RendersFullHeaderAndFooter()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "MULTI SERVICE ICAAYSI SRL", Rnc = "132293894", Address = "C/Cristino Zeno & Duarte", Phone = "(809) 725 4440", Whatsapp = "(809) 725 4440" },
            Client = new RiInvoiceClient { Name = "TRANSPORTE NJ,SRL", Rnc = "133009889" },
            NcfNumber = "E310000000019",
            NcfTypeName = "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
            EcfType = 31,
            ValidUntil = "31/12/2028",
            InternalInvoiceNumber = "0019",
            PaymentType = "CONTADO",
            PaymentCondition = "CONTADO",
            IsCredit = false,
            Cashier = "PEDRO",
            FechaEmision = "02-07-2026",
            Items = [new RiInvoiceItem { Description = "BOLAZUL GRANDE 10/5", Quantity = 2, Price = 187.50m, Itbis = 67.50m, Amount = 442.50m, Unit = "Und" }],
            SubTotal = 375.00m,
            Itbis = 67.50m,
            Total = 442.50m,
            ReceivedAmount = 443.00m,
            ChangeAmount = 0.50m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=132293894&ENCF=E310000000019&CodigoSeguridad=AbC123",
            SecurityCode = "AbC123"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.Contains("VALIDO HASTA:", text);
        Assert.Contains("31/12/2028", text);
        Assert.Contains("FACTURA:", text);
        Assert.Contains("0019", text);
        Assert.Contains("TIPO DE PAGO:", text);
        Assert.Contains("COND. PAGO:", text);
        Assert.Contains("CAJERO:", text);
        Assert.Contains("PEDRO", text);
        Assert.Contains("Und", text);
        Assert.Contains("ATENDIDO POR:", text);
        Assert.Contains("ARTÍCULOS:", text);
        Assert.Contains("TOTAL RECIBIDO:", text);
        Assert.Contains("SU CAMBIO:", text);
        Assert.DoesNotContain("FIRMA REQUERIDA", text);
    }

    [Fact]
    public void GeneratePdf_CreditNote34_HidesValidUntil_ShowsReferenceBlocks()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "EMPRESA X", Rnc = "132878191" },
            Client = new RiInvoiceClient { Name = "CLIENTE Y", Rnc = "131880681" },
            NcfNumber = "E340000000002",
            NcfTypeName = "NOTA DE CRÉDITO ELECTRÓNICA",
            EcfType = 34,
            ValidUntil = "31/12/2028", // aunque venga, para 34 no se muestra
            InternalInvoiceNumber = "123456789016",
            PaymentType = "CONTADO",
            IsCredit = false,
            Cashier = "PEDRO",
            FechaEmision = "02-04-2020",
            AffectedNcf = "E310000000034",
            ModificationCode = "3 - Corrige montos del NCF modificado",
            ModificationReason = "Error en monto",
            Items = [new RiInvoiceItem { Description = "BLOCK", Quantity = 1, Price = 480250.00m, Itbis = 86445.00m, Amount = 480250.00m }],
            SubTotal = 480250.00m,
            Itbis = 86445.00m,
            Total = 566695.00m,
            ReceivedAmount = 566695.00m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E340000000002&CodigoSeguridad=XyZ789",
            SecurityCode = "XyZ789"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.DoesNotContain("VALIDO HASTA:", text);
        Assert.Contains("NOTA CRÉDITO:", text);
        Assert.Contains("DATOS FACTURA AFECTADA", text);
        Assert.Contains("NCF MODIFICADO:", text);
        Assert.Contains("E310000000034", text);
        Assert.Contains("INFORMACIÓN DE MODIFICACIÓN", text);
        Assert.Contains("Error en monto", text);
        // Las notas no llevan TIPO/COND PAGO en la cabecera (paridad EasyInvoice).
        Assert.DoesNotContain("COND. PAGO:", text);
    }

    [Fact]
    public void GeneratePdf_Credito_ShowsFirmaRequerida_NoReceivedChange()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "TRANSPORTE NJ, SRL", Rnc = "133009889" },
            Client = new RiInvoiceClient { Name = "MORTEROS DE EUROPA", Rnc = "102620717" },
            NcfNumber = "E310000000402",
            NcfTypeName = "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
            EcfType = 31,
            ValidUntil = "31/12/2028",
            PaymentType = "CRÉDITO",
            PaymentCondition = "15 DIAS",
            IsCredit = true,
            Cashier = "PEDRO",
            FechaEmision = "17-04-2026",
            Items = [new RiInvoiceItem { Description = "Servicio de Transporte", Quantity = 1, Price = 6001.00m, Amount = 6001.00m }],
            SubTotal = 6001.00m,
            Total = 6001.00m,
            ReceivedAmount = 6001.00m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E310000000402&CodigoSeguridad=N4J8CY",
            SecurityCode = "N4J8CY"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.Contains("FIRMA REQUERIDA", text);
        Assert.Contains("CRÉDITO:", text);
        Assert.DoesNotContain("TOTAL RECIBIDO:", text);
        Assert.DoesNotContain("SU CAMBIO:", text);
    }
```

Nota: el test existente `GeneratePdf_ProducesPdf_WithNcfAndSecurityCode` sigue igual (no asume filas nuevas).

- [ ] **Step 2: Verificar que fallan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter RiInvoicePdfTests -v q"`
Expected: FAIL (aserciones de texto: VALIDO HASTA/CAJERO/etc. no aparecen).

- [ ] **Step 3: Reescribir Compose**

Reemplazar el cuerpo de `Compose` en `RiInvoicePdf.cs` por (se conservan `Culture`, ctor estático, `GetMetadata` y `GetQueryParam`):

```csharp
    public void Compose(IDocumentContainer container)
    {
        var company = _model.Company;
        var client = _model.Client;

        // DGII: 33 = Nota de Débito, 34 = Nota de Crédito (paridad EasyInvoice E33/E34).
        var isCreditNote = _model.EcfType == 34;
        var isDebitNote = _model.EcfType == 33;
        var isNote = isCreditNote || isDebitNote;
        var docTypeLabel = isCreditNote ? "NOTA CRÉDITO:" : (isDebitNote ? "NOTA DÉBITO:" : "FACTURA:");

        container.Page(page =>
        {
            page.ContinuousSize(227); // 80mm paper width
            page.MarginHorizontal(12);
            page.MarginVertical(7);
            page.Content().Column(col =>
            {
                col.Spacing(7);
                col.Item().Column(c =>
                {
                    c.Item().AlignCenter().Text($"{company.Name.ToUpper()}").Bold().FontSize(12);
                    c.Item().AlignCenter().Text($"RNC.: {company.Rnc}").FontSize(9.5f);

                    if (!string.IsNullOrEmpty(company.Address))
                    {
                        c.Item().AlignCenter().Text($"{company.Address}").FontSize(9.5f);
                    }

                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        c.Item().AlignCenter().Text($"Tel.: {company.Phone}").FontSize(9.5f);
                    }

                    if (!string.IsNullOrEmpty(company.Whatsapp))
                    {
                        c.Item().AlignCenter().Text($"WA: {company.Whatsapp}").FontSize(9.5f);
                    }

                    c.Item().LineHorizontal(0.5f);

                    c.Item().PaddingVertical(3).AlignCenter()
                             .Text($"{_model.NcfTypeName.ToUpper()}")
                             .Bold()
                             .FontSize(8.5f);

                    c.Item().LineHorizontal(0.5f);
                });

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("eNCF:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.NcfNumber).FontSize(8.5f);

                    // Oculto para 32 y 34, como el isEcfNote del InvoicePdf original.
                    var hideValidUntil = _model.EcfType == 32 || _model.EcfType == 34;
                    if (!hideValidUntil && !string.IsNullOrEmpty(_model.ValidUntil))
                    {
                        tb.Cell().Text("VALIDO HASTA:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.ValidUntil).FontSize(8.5f);
                    }

                    if (!string.IsNullOrEmpty(_model.InternalInvoiceNumber))
                    {
                        tb.Cell().Text(docTypeLabel).SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.InternalInvoiceNumber).FontSize(8.5f);
                    }

                    if (!isNote && !string.IsNullOrEmpty(_model.PaymentType))
                    {
                        tb.Cell().Text("TIPO DE PAGO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.PaymentType.ToUpper()).FontSize(8.5f);

                        if (!string.IsNullOrEmpty(_model.PaymentCondition))
                        {
                            tb.Cell().Text("COND. PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentCondition.ToUpper()).FontSize(8.5f);
                        }
                    }

                    tb.Cell().Text("FECHA:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.FechaEmision).FontSize(8.5f);

                    if (!string.IsNullOrEmpty(_model.Cashier))
                    {
                        tb.Cell().Text("CAJERO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.Cashier).FontSize(8.5f);
                    }
                });

                col.Item().LineHorizontal(0.5f);

                if (!string.IsNullOrEmpty(client.Name) || !string.IsNullOrEmpty(client.Rnc))
                {
                    col.Item().Table(tb =>
                    {
                        tb.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        tb.Cell().Text("CLIENTE:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text($"{client.Name}").FontSize(8.5f);

                        tb.Cell().Text("RNC/CED:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(string.IsNullOrEmpty(client.Rnc) ? "N/D" : client.Rnc).FontSize(8.5f);
                    });

                    col.Item().LineHorizontal(0.5f);
                }

                if (isNote && !string.IsNullOrEmpty(_model.AffectedNcf))
                {
                    col.Item().Table(tb =>
                    {
                        tb.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        tb.Cell().ColumnSpan(2).PaddingBottom(2).Text("DATOS FACTURA AFECTADA").Bold().FontSize(8.5f);

                        tb.Cell().Text("NCF MODIFICADO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.AffectedNcf).FontSize(8.5f);

                        if (!string.IsNullOrEmpty(_model.PaymentType))
                        {
                            tb.Cell().Text("TIPO DE PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentType.ToUpper()).FontSize(8.5f);
                        }

                        if (!string.IsNullOrEmpty(_model.PaymentCondition))
                        {
                            tb.Cell().Text("COND. PAGO:").SemiBold().FontSize(8.5f);
                            tb.Cell().AlignRight().Text(_model.PaymentCondition.ToUpper()).FontSize(8.5f);
                        }
                    });

                    col.Item().LineHorizontal(0.5f);
                }

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(60);
                    });

                    tb.Header(header =>
                    {
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("DESCRIPCIÓN").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).Text("ITBIS").Bold().FontSize(8);
                        header.Cell().ShowOnce().BorderColor("#D9D9D9").Padding(2).AlignRight().Text("TOTAL").Bold().FontSize(8);
                    });

                    foreach (var item in _model.Items)
                    {
                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Column(productCol =>
                        {
                            productCol.Item().Text(item.Description).FontSize(8);
                            var quantity = item.Quantity.ToString("0.##", CultureInfo.InvariantCulture);
                            var qtyLabel = string.IsNullOrEmpty(item.Unit)
                                ? $"{quantity}  x  {item.Price:F2}"
                                : $"{quantity} {item.Unit}  x  {item.Price:F2}";
                            productCol.Item().Text(qtyLabel).FontSize(7.5f).FontColor("#7F7F7F");
                        });
                        tb.Cell().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Text(string.Format(Culture, "{0:C2}", item.Itbis)).FontSize(8);
                        tb.Cell().AlignRight().BorderBottom(0.5f).BorderColor("#D9D9D9").Padding(2).Text(string.Format(Culture, "{0:C2}", item.Amount)).FontSize(8);
                    }
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("SUB-TOTAL:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.SubTotal)).Bold().FontSize(9);

                    if (_model.Discount > 0)
                    {
                        tb.Cell().Text("DESC:").Bold().FontSize(9);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "-{0:C2}", _model.Discount)).Bold().FontSize(9);
                    }

                    tb.Cell().Text("ITBIS:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Itbis)).Bold().FontSize(9);

                    tb.Cell().Text("TOTAL RD$:").Bold().FontSize(11);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).Bold().FontSize(11);
                });

                if (isNote && (!string.IsNullOrEmpty(_model.ModificationCode) || !string.IsNullOrEmpty(_model.ModificationReason)))
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingVertical(2).Column(noteCol =>
                    {
                        noteCol.Item().Text("INFORMACIÓN DE MODIFICACIÓN").Bold().FontSize(8);
                        if (!string.IsNullOrEmpty(_model.ModificationCode))
                        {
                            noteCol.Item().Text(txt =>
                            {
                                txt.Span("Código Mod.: ").Bold().FontSize(7.5f);
                                txt.Span(_model.ModificationCode).FontSize(7.5f);
                            });
                        }
                        if (!string.IsNullOrEmpty(_model.ModificationReason))
                        {
                            noteCol.Item().Text(txt =>
                            {
                                txt.Span("Razón / Concepto: ").Bold().FontSize(7.5f);
                                txt.Span(_model.ModificationReason).FontSize(7.5f);
                            });
                        }
                    });
                }

                if (!string.IsNullOrEmpty(_model.Note))
                {
                    col.Item().LineHorizontal(0.5f);
                    col.Item().PaddingVertical(2).Column(noteCol =>
                    {
                        noteCol.Item().Text("NOTA").Bold().FontSize(8);
                        noteCol.Item().Text(_model.Note).FontSize(7.5f);
                    });
                }

                col.Item().Text("");

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    tb.Cell().Text("ATENDIDO POR:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.Cashier).FontSize(8.5f);

                    tb.Cell().Text("ARTÍCULOS:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text($"{_model.Items.Count}").FontSize(8.5f);

                    tb.Cell().Text(_model.IsCredit ? "CRÉDITO:" : "CONTADO:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).FontSize(8.5f);

                    if (!_model.IsCredit)
                    {
                        tb.Cell().Text("TOTAL RECIBIDO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.ReceivedAmount)).FontSize(8.5f);

                        tb.Cell().Text("SU CAMBIO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.ChangeAmount)).FontSize(8.5f);
                    }
                });

                if (_model.IsCredit)
                {
                    col.Item().Text("");
                    col.Item().LineHorizontal(0.5f);
                    col.Item().AlignCenter().Text("FIRMA REQUERIDA").SemiBold().FontSize(8.5f);
                }

                col.Item().LineHorizontal(0.5f);
                col.Item().Text("");

                if (!string.IsNullOrEmpty(_model.Qr))
                {
                    byte[]? qrCodeBytes = null;
                    try
                    {
                        using var qrGenerator = new QRCoder.QRCodeGenerator();
                        using var qrCodeData = qrGenerator.CreateQrCode(_model.Qr, QRCoder.QRCodeGenerator.ECCLevel.M);
                        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                        qrCodeBytes = qrCode.GetGraphic(5);
                    }
                    catch
                    {
                        // Fallback: skip the QR image if it can't be generated.
                    }

                    if (qrCodeBytes != null)
                    {
                        col.Item().AlignCenter().Width(110).Image(qrCodeBytes);
                    }

                    var securityCode = GetQueryParam(_model.Qr, "CodigoSeguridad");
                    if (string.IsNullOrEmpty(securityCode) || securityCode == "N/D")
                    {
                        securityCode = string.IsNullOrEmpty(_model.SecurityCode) ? "N/D" : _model.SecurityCode;
                    }

                    var signatureDate = GetQueryParam(_model.Qr, "FechaFirma");
                    if (string.IsNullOrEmpty(signatureDate) || signatureDate == "N/D")
                    {
                        signatureDate = string.IsNullOrEmpty(_model.FechaFirma) ? "N/D" : _model.FechaFirma;
                    }

                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Codigo seguridad: ").Bold().FontSize(8);
                        txt.Span(securityCode).FontSize(8);
                    });

                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Fecha firma digital: ").Bold().FontSize(8);
                        txt.Span(signatureDate).FontSize(8);
                    });

                    col.Item().AlignCenter().Text("REPRESENTACION IMPRESA DEL e-CF")
                              .Bold()
                              .FontSize(8);

                    col.Item().Text("");
                }

                col.Item().AlignCenter().Text("¡Gracias por preferirnos!").Italic().FontSize(8.5f);
            });
        });
    }
```

- [ ] **Step 4: Verificar que pasan (los 3 nuevos + el existente)**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter RiInvoicePdfTests -v q"`
Expected: 4 passed.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiInvoicePdf.cs ZynstormECFPlatform.Tests/Ri/RiInvoicePdfTests.cs
git commit -m "feat(ri): RiInvoicePdf a paridad con InvoicePdf de EasyInvoice (variantes por tipo)"
```

---

### Task 6: Compras 41 — roles correctos, retenciones e ITBIS% por línea

**Files:**
- Modify: `ZynstormECFPlatform.Services/Ri/RiPurchaseModel.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs` (`MapPurchase`)
- Modify: `ZynstormECFPlatform.Services/Ri/RiPurchasePdf.cs`
- Modify: `ZynstormECFPlatform.Tests/Ri/EcfRiTemplateMapperTests.cs`, `ZynstormECFPlatform.Tests/Ri/RiPurchasePdfTests.cs`

**Interfaces:**
- Consumes: `RiData` (Task 3), helper `EcfRiTemplateMapper.PaymentTypeLabel` (Task 4).
- Produces: `RiPurchaseModel.ItbisRetentionAmount` (`decimal`), `RiPurchaseItem.ItbisRate` (`decimal`, ej. 18).

- [ ] **Step 1: Tests que fallan**

Agregar a `EcfRiTemplateMapperTests.cs`:

```csharp
    [Fact]
    public void MapPurchase_E41_CompanyIsEmisor_SupplierIsComprador_WithRetentions()
    {
        // Paso_E410000000007.xml: Emisor=DOCUMENTOS ELECTRONICOS DE 02 (la empresa),
        // Comprador=DOCUMENTOS ELECTRONICOS DE 11 (suplidor informal),
        // TotalITBISRetenido=2846.53, TotalISRRetencion=1606.41, ITBIS1=18.
        var model = EcfRiTemplateMapper.MapPurchase(Load("Paso_E410000000007.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 02", model.Company.Name);
        Assert.Equal("132293894", model.Company.Rnc);
        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 11", model.Supplier.Name);
        Assert.Equal("533445861", model.Supplier.Rnc);
        Assert.Equal(2846.53m, model.ItbisRetentionAmount);
        Assert.Equal(1606.41m, model.IsrRetentionAmount);
        Assert.Equal(10.0m, Math.Round(model.IsrRetentionRate, 1)); // 1606.41 / 16064.05 * 100
        Assert.Equal(16064.05m, model.SubTotal);
        Assert.Equal(18m, model.Items[0].ItbisRate);
        Assert.Equal(Math.Round(model.Items[0].Amount * 0.18m, 2), model.Items[0].Itbis);
    }
```

Y a `RiPurchasePdfTests.cs` un test de texto (usar el mismo patrón PdfPig que `RiInvoicePdfTests`; si el archivo no tiene `using UglyToad.PdfPig;`, agregarlo):

```csharp
    [Fact]
    public void GeneratePdf_WithRetentions_ShowsRetentionRows_AndNetTotal()
    {
        var model = new RiPurchaseModel
        {
            Company = new RiPurchaseCompany { Name = "EMPRESA COMPRADORA", Rnc = "132293894", Address = "AVE. ISABEL AGUIAR NO. 269" },
            Supplier = new RiPurchaseSupplier { Name = "SUPLIDOR INFORMAL", Rnc = "533445861" },
            NcfNumber = "E410000000007",
            FechaEmision = "01-04-2020",
            Items = [new RiPurchaseItem { Description = "Servicio Profesional", Quantity = 15, Price = 385.00m, Itbis = 1049.90m, Amount = 5832.75m, ItbisRate = 18m }],
            SubTotal = 16064.05m,
            Itbis = 2891.53m,
            Total = 18955.58m,
            IsrRetentionAmount = 1606.41m,
            IsrRetentionRate = 10.0m,
            ItbisRetentionAmount = 2846.53m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E410000000007&CodigoSeguridad=lu69Mx",
            SecurityCode = "lu69Mx"
        };

        using var pdf = UglyToad.PdfPig.PdfDocument.Open(new RiPurchasePdf(model).GeneratePdf());
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));

        Assert.Contains("Retención ISR", text);
        Assert.Contains("Retención ITBIS", text);
        Assert.Contains("18%", text);         // ITBIS% de la línea
        Assert.Contains("14,502.64", text);   // Total Neto = 18955.58 - 2846.53 - 1606.41
    }
```

- [ ] **Step 2: Verificar que fallan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter \"MapPurchase_E41|GeneratePdf_WithRetentions\" -v q"`
Expected: FAIL por compilación (`ItbisRetentionAmount`/`ItbisRate` no existen).

- [ ] **Step 3: Modelo**

En `RiPurchaseModel.cs`, después de `IsrRetentionAmount`:

```csharp
    /// <summary>ITBIS retenido (Totales/TotalITBISRetenido). Defaults to 0.</summary>
    public decimal ItbisRetentionAmount { get; set; }
```

y a `RiPurchaseItem`:

```csharp
    /// <summary>Tasa ITBIS de la línea según IndicadorFacturacion (ej. 18); 0 = exento.</summary>
    public decimal ItbisRate { get; set; }
```

- [ ] **Step 4: MapPurchase corregido**

Reemplazar `MapPurchase` en `EcfRiTemplateMapper.cs` por:

```csharp
    /// <summary>
    /// Maps a signed e-CF type 41 (Comprobante de Compras) XML into the
    /// <see cref="RiPurchaseModel"/> consumed by <see cref="RiPurchasePdf"/>. En el 41 el
    /// EMISOR del XML es la empresa que registra la compra (ella emite el comprobante) y el
    /// nodo COMPRADOR contiene al suplidor informal al que se le compró
    /// (dgii_ecf_requirements.md §41): Company ← Emisor, Supplier ← Comprador.
    /// </summary>
    public static RiPurchaseModel MapPurchase(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        return new RiPurchaseModel
        {
            Company = new RiPurchaseCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            Supplier = new RiPurchaseSupplier
            {
                Name = data.Buyer.Name,
                Rnc = data.Buyer.Document,
                Address = string.IsNullOrWhiteSpace(data.Buyer.Address) ? null : data.Buyer.Address
            },
            NcfNumber = data.ENcf,
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            Items = data.Items.ConvertAll(item =>
            {
                var rate = ItbisRateFor(item.IndicadorFacturacion, data.Totals);
                return new RiPurchaseItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    ItbisRate = rate,
                    Itbis = item.Itbis > 0 ? item.Itbis : Math.Round(item.Amount * rate / 100m, 2),
                    Amount = item.Amount
                };
            }),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            IsrRetentionAmount = data.Totals.IsrRetencion,
            IsrRetentionRate = data.Totals.SubTotal > 0
                ? data.Totals.IsrRetencion / data.Totals.SubTotal * 100m
                : 0m,
            ItbisRetentionAmount = data.Totals.ItbisRetenido,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>IndicadorFacturacion del ítem → tasa ITBIS de Totales (1→ITBIS1, 2→ITBIS2, 3→ITBIS3; 4/0→exento).</summary>
    private static decimal ItbisRateFor(int indicadorFacturacion, RiTotals totals) => indicadorFacturacion switch
    {
        1 => totals.Itbis1Rate,
        2 => totals.Itbis2Rate,
        3 => totals.Itbis3Rate,
        _ => 0m
    };
```

- [ ] **Step 5: RiPurchasePdf — ITBIS% de la línea, fila Retención ITBIS y Total Neto**

En `RiPurchasePdf.cs`:

1. En el `foreach (var item in _model.Items)`, reemplazar:

```csharp
                        var itbisPct = item.Amount != 0 ? (item.Itbis / item.Amount) * 100m : 0m;
```

por:

```csharp
                        var itbisPct = item.ItbisRate;
```

2. Después del bloque `// ISR Retention` (el `if (_model.IsrRetentionAmount > 0) { ... }`), agregar:

```csharp
                        // ITBIS Retention
                        if (_model.ItbisRetentionAmount > 0)
                        {
                            totalsCol.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Retención ITBIS:").FontSize(7.5f);
                                r.RelativeItem().AlignRight().Text($"-{string.Format(Culture, "{0:C2}", _model.ItbisRetentionAmount)}").FontSize(7.5f);
                            });
                        }
```

3. Reemplazar:

```csharp
                        var finalTotal = _model.Total - _model.IsrRetentionAmount;
```

por:

```csharp
                        var finalTotal = _model.Total - _model.IsrRetentionAmount - _model.ItbisRetentionAmount;
```

- [ ] **Step 6: Verificar que pasan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter \"EcfRiTemplateMapperTests|RiPurchasePdfTests\" -v q"`
Expected: todos passed (incluye los tests previos de ambos archivos).

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/RiPurchaseModel.cs ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs ZynstormECFPlatform.Services/Ri/RiPurchasePdf.cs ZynstormECFPlatform.Tests/Ri/
git commit -m "fix(ri): compras 41 con roles emisor/suplidor correctos, retenciones y total neto"
```

---

### Task 7: RiExpensePdf — plantilla de gastos menores (43) + ruteo

**Files:**
- Create: `ZynstormECFPlatform.Services/Ri/RiExpenseModel.cs`
- Create: `ZynstormECFPlatform.Services/Ri/RiExpensePdf.cs`
- Modify: `ZynstormECFPlatform.Services/Ri/EcfRiTemplateMapper.cs` (agregar `MapExpense`)
- Modify: `ZynstormECFPlatform.Services/Ri/RiPdfRenderer.cs`
- Modify: `ZynstormECFPlatform.Tests/Ri/EcfRiTemplateMapperTests.cs`, `ZynstormECFPlatform.Tests/Ri/RiPdfRendererTests.cs`

**Interfaces:**
- Consumes: `RiData` (Task 3); helpers `PaymentTypeLabel`, `FormatDate`, `CertificationCashier` (Task 4); `RiInvoiceCompany` (existente, se reutiliza para el encabezado).
- Produces: `RiExpenseModel`, `EcfRiTemplateMapper.MapExpense(string signedXml, DgiiEnvironment environment) : RiExpenseModel`, `RiExpensePdf(RiExpenseModel) : IDocument`; `RiPdfRenderer.Render` rutea 43 → `RiExpensePdf`.

- [ ] **Step 1: Tests que fallan**

Agregar a `EcfRiTemplateMapperTests.cs`:

```csharp
    [Fact]
    public void MapExpense_E43_PopulatesModel()
    {
        // Paso_E430000000008.xml: sin Comprador ni TipoPago; MontoExento=4950;
        // 1 ítem "Gasto personal en comida (kiosko)"; FechaVencimientoSecuencia=31-12-2028.
        var model = EcfRiTemplateMapper.MapExpense(Load("Paso_E430000000008.xml"), DgiiEnvironment.CerteCF);

        Assert.Equal("DOCUMENTOS ELECTRONICOS DE 02", model.Company.Name);
        Assert.Equal("E430000000008", model.NcfNumber);
        Assert.Equal("31/12/2028", model.ValidUntil);
        Assert.Equal(string.Empty, model.PaymentMethod);
        Assert.Equal("PEDRO", model.UserName);
        Assert.Equal("Gasto personal en comida (kiosko)", model.Concept);
        Assert.Equal(4950.00m, model.SubTotal);
        Assert.Equal(0m, model.Itbis);
        Assert.Equal("EXENTO:", model.ItbisLabel);
        Assert.Equal(4950.00m, model.Total);
        Assert.NotEmpty(model.Qr);
        Assert.NotEmpty(model.SecurityCode);
    }
```

Y a `RiPdfRendererTests.cs`:

```csharp
    [Fact]
    public void Render_Type43_DispatchesToExpenseTemplate_AndProducesPdf()
    {
        // Paso_E430000000008.xml: TipoeCF=43 -> RiExpensePdf (recibo de gastos menores).
        var bytes = RiPdfRenderer.Render(43, Load("Paso_E430000000008.xml"));

        Assert.True(bytes.Length > 500);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

        using var pdf = UglyToad.PdfPig.PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
        Assert.Contains("GASTOS MENORES ELECTRÓNICO", text);
        Assert.Contains("CONCEPTO:", text);
        Assert.Contains("EXENTO", text);
        Assert.Contains("Gastos Menores E43", text);
    }
```

- [ ] **Step 2: Verificar que fallan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter \"MapExpense_E43|Render_Type43\" -v q"`
Expected: FAIL por compilación (`RiExpenseModel`/`MapExpense` no existen).

- [ ] **Step 3: Crear RiExpenseModel.cs**

```csharp
namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// View-model consumed by <see cref="RiExpensePdf"/> (e-CF tipo 43, Gastos Menores).
/// Contains only the fields the ported EasyInvoice <c>ExpensePdf</c> (rama IsInformal)
/// renders y que existen en el XML del 43, populated by
/// <see cref="EcfRiTemplateMapper.MapExpense"/>. ACREEDOR/CATEGORÍA/GASTO NO del diseño
/// original se omiten: no viajan en el e-CF 43 (que tampoco lleva comprador).
/// </summary>
public class RiExpenseModel
{
    public RiInvoiceCompany Company { get; set; } = new();

    public string NcfNumber { get; set; } = string.Empty;

    /// <summary>FechaVencimientoSecuencia formateada dd/MM/yyyy; vacío si no viene.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    /// <summary>"CONTADO"/"CRÉDITO" según TipoPago; vacío si no viene (fila omitida).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>Descripciones de los ítems unidas por "; ".</summary>
    public string Concept { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    /// <summary>"ITBIS 18%:" / "ITBIS 16%:" / "EXENTO:" según la tasa efectiva.</summary>
    public string ItbisLabel { get; set; } = string.Empty;

    /// <summary>DGII ConsultaTimbre URL, also used to render the QR image.</summary>
    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Agregar MapExpense al EcfRiTemplateMapper**

```csharp
    /// <summary>
    /// Maps a signed e-CF type 43 (Gastos Menores) XML into the <see cref="RiExpenseModel"/>
    /// consumed by <see cref="RiExpensePdf"/> (plantilla portada del ExpensePdf informal de
    /// EasyInvoice). El 43 no lleva comprador: solo emisor, totales e ítems.
    /// </summary>
    public static RiExpenseModel MapExpense(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        var itbisLabel = data.Totals.Itbis <= 0
            ? "EXENTO:"
            : (data.Totals.Itbis1Rate == 16m ? "ITBIS 16%:" : "ITBIS 18%:");

        return new RiExpenseModel
        {
            Company = new RiInvoiceCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            NcfNumber = data.ENcf,
            ValidUntil = FormatDate(data.FechaVencimientoSecuencia),
            PaymentMethod = PaymentTypeLabel(data.TipoPago),
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            UserName = CertificationCashier,
            Concept = string.Join("; ", data.Items
                .Select(item => item.Description)
                .Where(description => !string.IsNullOrWhiteSpace(description))),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            ItbisLabel = itbisLabel,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }
```

- [ ] **Step 5: Crear RiExpensePdf.cs**

```csharp
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// QuestPDF Ri template ported from EasyInvoice's <c>EasyInvoice.Reports/Expenses/ExpensePdf.cs</c>
/// (rama IsInformal, "GASTOS MENORES ELECTRÓNICO"), adaptada a <see cref="RiExpenseModel"/> y
/// extendida con el bloque QR/código de seguridad de las RI. Renders as an 80mm continuous
/// receipt and covers e-CF type 43.
/// </summary>
public class RiExpensePdf(RiExpenseModel model) : IDocument
{
    private static readonly CultureInfo Culture = new("es-DO");

    private readonly RiExpenseModel _model = model;

    static RiExpensePdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        var company = _model.Company;

        container.Page(page =>
        {
            page.ContinuousSize(227); // 80mm
            page.MarginHorizontal(12);
            page.MarginVertical(7);

            page.Content().Column(col =>
            {
                col.Spacing(6);

                col.Item().Column(c =>
                {
                    c.Item().AlignCenter().Text(company.Name.ToUpper()).Bold().FontSize(12);
                    c.Item().AlignCenter().Text($"RNC.: {company.Rnc}").FontSize(8.5f);
                    if (!string.IsNullOrEmpty(company.Address))
                    {
                        c.Item().AlignCenter().Text(company.Address).FontSize(8.5f);
                    }
                    if (!string.IsNullOrEmpty(company.Phone))
                    {
                        c.Item().AlignCenter().Text($"Tel.: {company.Phone}").FontSize(8.5f);
                    }
                    if (!string.IsNullOrEmpty(company.Whatsapp))
                    {
                        c.Item().AlignCenter().Text($"WA: {company.Whatsapp}").FontSize(8.5f);
                    }

                    c.Item().LineHorizontal(0.5f);

                    c.Item().PaddingVertical(3)
                            .AlignCenter()
                            .Text("GASTOS MENORES ELECTRÓNICO")
                            .Bold()
                            .FontSize(8.5f);

                    c.Item().LineHorizontal(0.5f);
                });

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    tb.Cell().Text("eNCF:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.NcfNumber).FontSize(8.5f);

                    if (!string.IsNullOrEmpty(_model.ValidUntil))
                    {
                        tb.Cell().Text("VÁLIDO HASTA:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.ValidUntil).FontSize(8.5f);
                    }

                    if (!string.IsNullOrEmpty(_model.PaymentMethod))
                    {
                        tb.Cell().Text("MÉTODO PAGO:").SemiBold().FontSize(8.5f);
                        tb.Cell().AlignRight().Text(_model.PaymentMethod.ToUpper()).FontSize(8.5f);
                    }

                    tb.Cell().Text("FECHA:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.FechaEmision).FontSize(8.5f);

                    tb.Cell().Text("USUARIO:").SemiBold().FontSize(8.5f);
                    tb.Cell().AlignRight().Text(_model.UserName).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().PaddingVertical(2).Column(c =>
                {
                    c.Item().Text("CONCEPTO:").SemiBold().FontSize(8.5f);
                    c.Item().Text(_model.Concept).FontSize(8.5f);
                });

                col.Item().LineHorizontal(0.5f);

                col.Item().Table(tb =>
                {
                    tb.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn();
                        cols.RelativeColumn();
                    });

                    tb.Cell().Text("SUB-TOTAL:").Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.SubTotal)).Bold().FontSize(9);

                    tb.Cell().Text(_model.ItbisLabel).Bold().FontSize(9);
                    tb.Cell().AlignRight().Text(_model.Itbis > 0
                        ? string.Format(Culture, "{0:C2}", _model.Itbis)
                        : "EXENTO").Bold().FontSize(9);

                    tb.Cell().Text("TOTAL RD$:").Bold().FontSize(11);
                    tb.Cell().AlignRight().Text(string.Format(Culture, "{0:C2}", _model.Total)).Bold().FontSize(11);
                });

                col.Item().Text("");

                if (!string.IsNullOrEmpty(_model.Qr))
                {
                    byte[]? qrCodeBytes = null;
                    try
                    {
                        using var qrGenerator = new QRCoder.QRCodeGenerator();
                        using var qrCodeData = qrGenerator.CreateQrCode(_model.Qr, QRCoder.QRCodeGenerator.ECCLevel.M);
                        using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                        qrCodeBytes = qrCode.GetGraphic(5);
                    }
                    catch
                    {
                        // Fallback: skip the QR image if it can't be generated.
                    }

                    if (qrCodeBytes != null)
                    {
                        col.Item().AlignCenter().Width(110).Image(qrCodeBytes);
                    }

                    var securityCode = string.IsNullOrEmpty(_model.SecurityCode) ? "N/D" : _model.SecurityCode;
                    col.Item().AlignCenter().Text(txt =>
                    {
                        txt.Span("Codigo seguridad: ").Bold().FontSize(8);
                        txt.Span(securityCode).FontSize(8);
                    });

                    if (!string.IsNullOrEmpty(_model.FechaFirma))
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span("Fecha firma digital: ").Bold().FontSize(8);
                            txt.Span(_model.FechaFirma).FontSize(8);
                        });
                    }
                }

                col.Item().AlignCenter().Text("REPRESENTACIÓN IMPRESA DEL e-CF").Bold().FontSize(7.5f);
                col.Item().AlignCenter().Text("Comprobante Electrónico Gastos Menores E43").Italic().FontSize(7.5f);

                col.Item().Text("");
                col.Item().AlignCenter().Text("¡Gracias!").Italic().FontSize(8.5f);
            });
        });
    }
}
```

- [ ] **Step 6: Ruteo en RiPdfRenderer**

En `RiPdfRenderer.Render`, después del bloque `if (ecfType == 41) { ... }`, agregar:

```csharp
        if (ecfType == 43)
        {
            var expense = EcfRiTemplateMapper.MapExpense(signedXml, DgiiEnvironment.CerteCF);
            if (company is not null)
            {
                expense.Company = new RiInvoiceCompany
                {
                    Name = company.Name,
                    Rnc = company.Rnc,
                    Address = company.Address,
                    Phone = company.Phone,
                    Whatsapp = company.Whatsapp,
                };
            }
            return new RiExpensePdf(expense).GeneratePdf();
        }
```

Actualizar el doc-comment de la clase: 41→Purchase, 43→Expense, resto→Invoice.

- [ ] **Step 7: Verificar que pasan**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter \"EcfRiTemplateMapperTests|RiPdfRendererTests\" -v q"`
Expected: todos passed.

- [ ] **Step 8: Commit**

```bash
git add ZynstormECFPlatform.Services/Ri/ ZynstormECFPlatform.Tests/Ri/
git commit -m "feat(ri): plantilla RiExpensePdf para gastos menores (43) + ruteo"
```

---

### Task 8: Corrección de docs, suite completa y verificación visual

**Files:**
- Modify: `dgii_ecf_requirements.md` (líneas 23-24)
- Verificación: render de PDFs E31/E34/E41/E43 con fixtures

- [ ] **Step 1: Corregir la tabla de tipos en dgii_ecf_requirements.md**

Líneas 23-24, intercambiar los nombres (los códigos quedan igual):

```markdown
| **33** | 📈 **Nota de Débito** | `Inf. Referencia` | 🔸 `NCFModificado`<br>🔸 `FechaNCFModificado`<br>🔸 `CodigoModificacion` |
| **34** | 📉 **Nota de Crédito** | `Inf. Referencia` | 🔸 `NCFModificado`<br>🔸 `FechaNCFModificado`<br>🔸 `CodigoModificacion` |
```

- [ ] **Step 2: Suite completa**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests -v q"`
Expected: todos los tests passed (0 failed). Si algún test previo del repo falla por los cambios, investigar y corregir antes de seguir.

- [ ] **Step 3: Generar PDFs de muestra para revisión visual**

Crear TEMPORALMENTE `ZynstormECFPlatform.Tests/Ri/RiVisualDumpTests.cs` (no se commitea):

```csharp
using ZynstormECFPlatform.Services.Ri;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiVisualDumpTests
{
    private static string Load(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Ri", "Fixtures", name));

    [Theory]
    [InlineData(31, "Paso_1_E310000000402.xml")]
    [InlineData(34, "Paso_E340000000002.xml")]
    [InlineData(41, "Paso_E410000000007.xml")]
    [InlineData(43, "Paso_E430000000008.xml")]
    public void Dump(int type, string fixture)
    {
        var company = new RiCompanyHeader("MULTI SERVICE ICAAYSI SRL", "132293894", "C/Cristino Zeno & Duarte", "(809) 725 4440", "(809) 725 4440");
        var bytes = RiPdfRenderer.Render(type, Load(fixture), company);
        File.WriteAllBytes($"/tmp/ri_visual_{type}.pdf", bytes);
    }
}
```

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet test ZynstormECFPlatform.Tests --filter RiVisualDumpTests -v q && ls -la /tmp/ri_visual_*.pdf"`
Expected: 4 passed y 4 PDFs en `/tmp`.

- [ ] **Step 4: Revisar visualmente los 4 PDFs**

Abrir con la herramienta Read (soporta PDF): `\\wsl.localhost\Ubuntu\tmp\ri_visual_31.pdf`, `ri_visual_34.pdf`, `ri_visual_41.pdf`, `ri_visual_43.pdf`. Verificar contra el diseño EasyInvoice:
- 31: encabezado con dirección, VALIDO HASTA, TIPO/COND PAGO (CRÉDITO/15 DIAS), CAJERO PEDRO, unidad "Und", footer con ARTÍCULOS y FIRMA REQUERIDA (es crédito), QR.
- 34: título NOTA DE CRÉDITO, sin VALIDO HASTA, DATOS FACTURA AFECTADA e INFORMACIÓN DE MODIFICACIÓN.
- 41: empresa (header) = MULTI SERVICE (override) y SUPLIDOR = DOCUMENTOS ELECTRONICOS DE 11; filas Retención ISR/ITBIS y Total Neto 14,502.64; ITBIS% 18%.
- 43: plantilla de gasto con CONCEPTO y EXENTO.

- [ ] **Step 5: Borrar el dump temporal y commit final**

```bash
rm ZynstormECFPlatform.Tests/Ri/RiVisualDumpTests.cs
git add dgii_ecf_requirements.md
git commit -m "docs(dgii): corregir 33=Nota de Débito / 34=Nota de Crédito"
```

- [ ] **Step 6: Suite completa final + build solución**

Run: `wsl -e bash -lc "cd /home/dev/projects/ZynstormECF-WorkSpace/ZynstormECFPlatform && dotnet build ZynstormECFPlatform.slnx -v q && dotnet test ZynstormECFPlatform.Tests -v q"`
Expected: build OK, todos los tests passed.
