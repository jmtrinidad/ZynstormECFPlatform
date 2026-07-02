# Diseño — RI por "clon posicionado" (reconstrucción fiel del PDF modelo)

- **Fecha:** 2026-07-01
- **Estado:** Aprobado (usuario delegó ejecución; revisión al final)
- **Reemplaza el motor de render de:** `2026-06-30-representacion-impresa-modelo-pdf-design.md` (el resto de ese diseño sigue vigente)
- **Repos:** `ZynstormECFPlatform` (backend). Frontend sin cambios.

## 1. Contexto y problema

La primera versión "extraer y reconstruir" generaba una **plantilla genérica** (estructura propia) que no se parecía al PDF modelo subido. El usuario quiere que la RI **replique el diseño del modelo**. Se descartó el overlay-con-enmascarado (frágil sobre facturas llenas). Decisión: **repintar la página desde cero** ("clon posicionado"): extraer TODO el contenido posicionado del modelo, clasificar qué es estático (se conserva) vs. valor dinámico (se reemplaza con datos del e-CF), y dibujar una página nueva con esas posiciones + el QR que exige la DGII.

### Decisiones
- **Motor de render: PDFsharp** (MIT) — dibuja texto/líneas/imágenes en coordenadas absolutas. QuestPDF (de flujo) no sirve para esto y **se retira solo de la RI** (sigue en `ReportPdfGenerator`). QRCoder se mantiene (imagen del QR).
- **El PDF fuente NO se persiste**: el `PageModel` extraído contiene todo lo necesario para render; el fuente solo se usa una vez al subir. `FileData` sigue siendo la RI de referencia; `LayoutJson` ahora guarda el `PageModel`.
- **Fuentes aproximadas**: se dibuja con una tipografía base embebida (TTF permisiva, ej. Liberation/DejaVu Sans) vía un `IFontResolver` (necesario headless/WSL) al tamaño/posición extraídos. Texto, estructura y posiciones se respetan; la fuente exacta no.
- **Se reutiliza sin cambios:** `EcfQrUrlBuilder`, `EcfRiDataMapper` (→ `RiData`), la entidad `CertificationInvoicePrintTemplate` + join, `CertificationRiModelService` (orquestación), los 8 endpoints y el frontend Paso 5.
- **Se reemplaza:** `RiModelExtractor` (→ `PageModel`), `RiPdfRenderer` (→ PDFsharp). `LayoutDescriptor` → `PageModel`.

## 2. `PageModel` (contenido extraído del modelo)

Coordenadas en **puntos, origen arriba-izquierda** (se convierte desde el origen abajo-izquierda de PdfPig al extraer), para dibujar directo con PDFsharp.

```csharp
PageModel {
  double WidthPt, HeightPt;
  List<TextRun> StaticRuns;      // texto que se conserva verbatim (etiquetas, encabezado, cabeceras de tabla, pie, etc.)
  List<LineSeg> Lines;           // líneas/recuadros (bordes) {X1,Y1,X2,Y2,Thickness}
  List<ImageEl> Images;          // logo, etc. {X,Y,W,H, base64 PNG}
  List<FieldSlot> Fields;        // valores dinámicos {FieldKey, X, Y, FontSize, Align, MaxWidth}
  ItemsRegion Items;             // {TopY, RowHeight, List<ItemColumn>{Field,X,Width,Align}}
  QrSlot Qr;                     // {X,Y,Size}
  List<string> Warnings;
}
TextRun { string Text; double X,Y; double FontSize; string? ColorHex; bool Bold; }
```

`FieldKey` ∈ { eNCF, fechaEmision, fechaFirma, rncComprador, razonSocialComprador, direccionComprador, telefonoComprador, subtotal, itbis, total, exento, codigoSeguridad, … }.

## 3. Extractor (`RiModelExtractor` → `PageModel`, con PdfPig)

1. Abrir con PdfPig; si no hay texto (`GetWords()` vacío) → `Failed` + warning ("PDF sin texto extraíble").
2. Agrupar palabras en corridas por línea/proximidad; capturar posición, tamaño de fuente, color, bold. Convertir Y a origen-arriba.
3. **Clasificar** anclando en el diccionario de etiquetas conocidas (case/acento-insensible):
   - **FieldSlots (dinámicos):** para cada etiqueta de campo (eNCF/NCF, FECHA, CLIENTE/RAZÓN SOCIAL, RNC/CÉD, totales SUB-TOTAL/ITBIS/TOTAL, etc.), la(s) corrida(s) de valor a la derecha o debajo se convierten en un `FieldSlot` (posición del valor, `FieldKey` mapeado). Esas corridas de valor se **excluyen** del conjunto estático.
   - **ItemsRegion:** banda entre la fila de cabecera de la tabla (DESCRIPCIÓN/ITBIS/TOTAL/…) y la primera etiqueta de totales; columnas desde las X de las cabeceras (incluye sinónimos: **TOTAL, VALOR, IMPORTE → columna importe**). Las filas de ítems de muestra se excluyen del estático.
   - **QrSlot:** si el modelo no trae QR (caso típico) → posición por defecto (inferior). Si detecta una región tipo QR, la usa.
   - **StaticRuns = todas las corridas − (valores + filas de ítems de muestra).** Se conservan verbatim (etiquetas, encabezado, logo, líneas, cabeceras, pie).
4. Líneas/recuadros vía `page.ExperimentalAccess.GetPaths()`; imágenes vía `page.GetImages()` (try/catch; formatos no decodificables → warning).
5. Anclas faltantes → `Warnings` (best-effort; la vista previa de confirmación lo detecta).

## 4. Renderer (`RiPdfRenderer` → bytes PDF, con PDFsharp)

- Registrar un `IFontResolver` global (una vez) que sirva la TTF base embebida (recurso del ensamblado) → funciona headless en WSL/Linux.
- Crear `PdfDocument`, una página `WidthPt × HeightPt` del `PageModel`.
- Con `XGraphics`:
  - Dibujar cada `StaticRun` en (X,Y) con `XFont` (familia base, `FontSize`, bold), color.
  - Dibujar `Lines` y `Images` (logo desde base64) en sus posiciones.
  - Dibujar cada `FieldSlot`: el valor de `RiData` para ese `FieldKey`, en (X,Y), con alineación/tamaño.
  - Dibujar `ItemsRegion`: por cada `RiData.Items[i]`, dibujar cada columna en (`col.X`, `TopY + i*RowHeight`); si hay más ítems que espacio, comprimir interlineado o continuar (v1: comprimir; overflow extremo → warning).
  - Dibujar el **QR** (QRCoder PNG → `XImage`) en el `QrSlot` + el texto "Código de Seguridad: {valor}".
- Slots/valores ausentes → se omiten (no rompe).

## 5. Datos, servicio y reuse

- **Entidad sin cambios**; `LayoutJson` (nvarchar max ya existente) ahora serializa el `PageModel` → **sin migración**.
- `CertificationRiModelService`: cambia solo las llamadas — `RiModelExtractor.Extract(pdf)` → `PageModel`; render de la RI de referencia y de cada comprobante con el nuevo `RiPdfRenderer.Render(pageModel, riData)`. `SampleRiData`, reasignación de tipos, ZIP, confirm: igual.
- Controller y frontend: **sin cambios**.

## 6. Errores
- PDF sin texto / PDFsharp no puede parsear el fuente al extraer → `Failed` + mensaje claro; no queda `Confirmed`.
- Anclas faltantes → warnings + best-effort; se confirma tras ver la vista previa.
- Ítem/valor sin slot mapeado → se omite; overflow de ítems → comprimir + warning.
- Tipo sin modelo confirmado al generar → BadRequest (sin cambios).

## 7. Testing
- **Extractor:** fixture de formato conocido generado in-memory con QuestPDF (etiquetas + valores de muestra + filas de ítems). Aserciones: `StaticRuns` contiene las etiquetas (p.ej. "SUB-TOTAL"); `Fields` contiene `eNCF`/`total`; `Items` detectada con ≥2 columnas; las corridas de valor de muestra NO están en `StaticRuns`.
- **Renderer:** construir un `PageModel` (algunas `StaticRuns` + `Fields` + `ItemsRegion`) + `RiData`, renderizar con PDFsharp, leer el texto con PdfPig y asertar que aparecen los valores del e-CF (ítem, total) y las etiquetas estáticas, y que un QR (imagen) fue incrustado.
- **Mapper/QR:** sin cambios (tests existentes siguen).
- Fidelidad end-to-end (parecido real al modelo): verificación manual con un PDF real + vista previa.

## 8. Notas de implementación
- Paquete nuevo: `PdfSharp` (6.x, net10) en `ZynstormECFPlatform.Services`. Mantener QRCoder.
- Fuente base: embeber un TTF permisivo como recurso y resolverlo con `IFontResolver`.
- Retirar QuestPDF **solo** de `RiPdfRenderer` (no de `ReportPdfGenerator`).
- Reemplaza los archivos `RiModelExtractor.cs`, `RiPdfRenderer.cs`, `LayoutDescriptor.cs`→`PageModel.cs`; ajustar sus tests.
