# RI "clon posicionado" (PDFsharp) — Plan de Implementación

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development. Steps use `- [ ]`.

**Goal:** Reemplazar el motor de RI para que repinte fielmente el layout del PDF modelo desde cero (PDFsharp), sustituyendo los valores por los del e-CF y estampando el QR.

**Architecture:** El extractor produce un `PageModel` (texto posicionado clasificado estático/dinámico + líneas + imágenes + slots de campos + región de ítems + slot QR); el renderer dibuja una página nueva con PDFsharp. Se reusan mapper, QR builder, entidad, servicio, endpoints y frontend.

**Tech Stack:** .NET 10, PdfPig (extracción), **PdfSharp 6.x** (render por coordenadas, nuevo), QRCoder (QR), xUnit.

## Global Constraints
- El QR se genera con `EcfQrUrlBuilder`/`EcfRiDataMapper` existentes (no reimplementar).
- El PDF fuente NO se persiste; `LayoutJson` guarda el `PageModel`; `FileData` = RI de referencia. Sin migración.
- Coordenadas del `PageModel` en puntos, origen arriba-izquierda.
- PDFsharp debe funcionar headless (WSL/Linux) → `IFontResolver` con TTF embebida.
- QuestPDF se retira SOLO de la RI (sigue en `ReportPdfGenerator`).
- Tests en `ZynstormECFPlatform.Tests` (xUnit net10, `global using Xunit`, refs Common/Core/Services). Build: `dotnet build ZynstormECFPlatform.slnx -nologo`.

---

## Task R1: PdfSharp + IFontResolver headless (de-risk)

**Files:** Modify `ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj`; Create `ZynstormECFPlatform.Services/Ri/RiFontResolver.cs` + embed a TTF; Test `ZynstormECFPlatform.Tests/Ri/RiFontResolverTests.cs`.

**Interfaces:** Produces `RiFontResolver.EnsureRegistered()` (idempotente, registra `GlobalFontSettings.FontResolver` una vez) y una familia base `RiFontResolver.BaseFamily` ("RiBase").

- [ ] **Step 1:** Agregar `<PackageReference Include="PdfSharp" Version="6.2.0" />` al csproj de Services. Descargar/embeber una TTF permisiva (ej. `DejaVuSans.ttf` y su bold) como `EmbeddedResource` en `ZynstormECFPlatform.Services/Ri/Fonts/`. (Si no hay red para la fuente, usar una TTF ya presente en el repo/SO y anotarlo.)
- [ ] **Step 2 (RED):** Test que registra el resolver y renderiza un PDF mínimo con PdfSharp dibujando "Hola" con `new XFont(RiFontResolver.BaseFamily, 10)`, y asierta que produce bytes con cabecera `%PDF`. Correr → falla (resolver/clase no existe).
- [ ] **Step 3:** Implementar `RiFontResolver : IFontResolver` que sirva la TTF embebida para BaseFamily (regular + bold); `EnsureRegistered()` setea `GlobalFontSettings.FontResolver` una sola vez (lock/flag).
- [ ] **Step 4 (GREEN):** Correr el test → pasa. `dotnet build ZynstormECFPlatform.slnx -nologo` → succeeds.
- [ ] **Step 5:** Commit `feat(ri): PdfSharp + IFontResolver headless con fuente embebida`.

## Task R2: `PageModel`

**Files:** Create `ZynstormECFPlatform.Services/Ri/PageModel.cs`; delete/replace `ZynstormECFPlatform.Services/Ri/LayoutDescriptor.cs`.

**Interfaces:** Produce `PageModel` y tipos anidados exactamente como en la §2 del spec: `PageModel{double WidthPt,HeightPt; List<TextRun> StaticRuns; List<LineSeg> Lines; List<ImageEl> Images; List<FieldSlot> Fields; ItemsRegion Items; QrSlot Qr; List<string> Warnings}`; `TextRun{string Text; double X,Y; double FontSize; string? ColorHex; bool Bold}`; `LineSeg{double X1,Y1,X2,Y2,Thickness}`; `ImageEl{double X,Y,W,H; string Base64}`; `FieldSlot{string FieldKey; double X,Y; double FontSize; string Align; double MaxWidth}`; `ItemsRegion{double TopY,RowHeight; List<ItemColumn> Columns}`; `ItemColumn{string Field; double X,Width; string Align}`; `QrSlot{double X,Y,Size}`.

- [ ] **Step 1:** Crear `PageModel.cs` con esos POCOs (System.Text.Json-serializable, colecciones `= []`).
- [ ] **Step 2:** Eliminar `LayoutDescriptor.cs`. (Sus consumidores se ajustan en R3/R4/R5.)
- [ ] **Step 3:** `dotnet build ZynstormECFPlatform.Services/ZynstormECFPlatform.Services.csproj` — puede romper hasta R3/R4/R5 (extractor/renderer/servicio referencian LayoutDescriptor); es esperado dentro de esta secuencia. Si prefieres, combina R2–R5 en un build final verde.
- [ ] **Step 4:** Commit `feat(ri): PageModel (contenido posicionado clasificado)`.

## Task R3: `RiModelExtractor` → `PageModel` (PdfPig, clasificación estático/dinámico)

**Files:** Rewrite `ZynstormECFPlatform.Services/Ri/RiModelExtractor.cs`; rewrite `ZynstormECFPlatform.Tests/Ri/RiModelExtractorTests.cs`.

**Interfaces:** `RiModelExtractor.Extract(byte[] pdfBytes) : RiExtractionResult` con `RiExtractionResult(PageModel? Page, List<string> Warnings, bool Success)`.

- [ ] **Step 1 (RED):** Test que genera in-memory con QuestPDF un PDF de formato conocido (encabezado "MI EMPRESA", etiquetas `NCF:`,`Fecha:`,`Cliente:`,`RNC:`; cabeceras de tabla `Descripción` `ITBIS` `Total`; una fila de ítem de muestra `Producto X ... 118.00`; totales `Sub-Total`,`ITBIS`,`Total` con montos de muestra). Extrae y asierta: `Success`; `Page.StaticRuns` contiene "SUB-TOTAL"/"Descripción" (etiquetas/cabeceras); `Page.Fields` contiene `eNCF` y `total`; `Page.Items.Columns.Count >= 2` (incluye la columna `Total`→importe); y que el texto de valor de muestra (p.ej. el NCF de muestra o "Producto X") NO está en `StaticRuns`. Correr → falla.
- [ ] **Step 2:** Implementar el extractor per §3 del spec: agrupar palabras en corridas; diccionario de anclas (case/acento-insensible) que además reconozca `TOTAL/VALOR/IMPORTE`→columna `importe`; construir `Fields` (valor junto a etiqueta), `Items` (banda header→totales, columnas por X de cabeceras), `QrSlot` default inferior; `StaticRuns = corridas − valores − filas de ítems`; `Lines` via `ExperimentalAccess.GetPaths()`, `Images` via `GetImages()` (try/catch). `words.Count==0` → `Success=false` + warning. Métodos helper (`NormalizeText`, `FindAnchor`, `BuildColumns`, `IsValueRun`).
- [ ] **Step 3 (GREEN):** `dotnet test ... --filter RiModelExtractorTests` → pasa.
- [ ] **Step 4:** Commit `feat(ri): extractor a PageModel con clasificacion estatico/dinamico`.

## Task R4: `RiPdfRenderer` → PDFsharp

**Files:** Rewrite `ZynstormECFPlatform.Services/Ri/RiPdfRenderer.cs`; rewrite `ZynstormECFPlatform.Tests/Ri/RiPdfRendererTests.cs`.

**Interfaces:** `RiPdfRenderer.Render(PageModel page, RiData data) : byte[]`.

- [ ] **Step 1 (RED):** Test que construye un `PageModel` (WidthPt/HeightPt Letter; una `StaticRun` "SUB-TOTAL:"; un `FieldSlot{FieldKey="total",...}`; una `ItemsRegion` con columnas `descripcion`,`importe` y `TopY/RowHeight`; `QrSlot`) y un `RiData` (item Description "Servicio ABC" Amount 1180; Totals.Total 1180; QrUrl no vacío; SecurityCode "N4J8CY"), renderiza, lee el texto con PdfPig y asierta que contiene "SUB-TOTAL", "Servicio ABC" y "1,180". Correr → falla (renderer nuevo no existe / firma cambió).
- [ ] **Step 2:** Implementar con PDFsharp per §4: `RiFontResolver.EnsureRegistered()`; `PdfDocument`+página `WidthPt×HeightPt`; `XGraphics`: dibujar `StaticRuns` (XFont base, bold, color), `Lines`, `Images` (XImage desde base64), `Fields` (valor de `RiData` por `FieldKey`, un helper `FieldValue(data,key)`), `Items` (por cada `RiData.Items[i]` dibujar cada columna en `col.X, TopY+i*RowHeight`, con `ItemColumnValue`), y el QR (QRCoder PNG → `XImage`) + "Código de Seguridad: {SecurityCode}". Guardar a `MemoryStream` → bytes. Mapear `FieldKey`/columnas con las MISMAS claves minúscula-español del extractor (`eNCF,fechaEmision,rncComprador,subtotal,itbis,total,exento,codigoSeguridad`; columnas `descripcion,cantidad,precio,itbis,valor,importe`).
- [ ] **Step 3 (GREEN):** `dotnet test ... --filter RiPdfRendererTests` → pasa.
- [ ] **Step 4:** Commit `feat(ri): renderer PDFsharp desde PageModel + QR`.

## Task R5: Cablear `CertificationRiModelService` + build verde total

**Files:** Modify `ZynstormECFPlatform.Services/Ri/CertificationRiModelService.cs`.

- [ ] **Step 1:** Cambiar el tipo (de)serializado de `LayoutJson` de `LayoutDescriptor` a `PageModel`; ajustar las llamadas: `RiModelExtractor.Extract` → `result.Page`; render de la RI de referencia y de cada comprobante con `RiPdfRenderer.Render(pageModel, riData)`. Mantener `SampleRiData`, reasignación de tipos, ZIP y confirm.
- [ ] **Step 2:** `dotnet build ZynstormECFPlatform.slnx -nologo` → succeeds (0 errores). `dotnet test ZynstormECFPlatform.Tests/ZynstormECFPlatform.Tests.csproj` → todos pasan.
- [ ] **Step 3:** Commit `refactor(ri): servicio usa PageModel + renderer PDFsharp`.

## Self-Review
- Cobertura spec: §2→R2, §3→R3, §4→R4/R1, §5→R5, §7→R3/R4. QR/mapper reusados sin tocar.
- Consistencia de claves: extractor (R3) y renderer (R4) usan el MISMO vocabulario minúscula-español (evita el bug C1 previo). `RiExtractionResult.Page` (R3) consumido en R5.
- Sin placeholders de acción vaga; el riesgo headless se ataca primero (R1).

## Riesgo
- PDFsharp headless/fuentes: mitigado por R1 (se prueba aislado antes de todo).
- Calidad de clasificación estático/dinámico: es lo más incierto; los tests de R3 la acotan y la vista previa de confirmación es el resguardo.
