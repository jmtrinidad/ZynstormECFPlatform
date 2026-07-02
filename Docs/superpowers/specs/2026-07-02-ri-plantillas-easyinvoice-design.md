# Diseño — RI con plantillas QuestPDF portadas de EasyInvoice (built-in, sin subida)

- **Fecha:** 2026-07-02
- **Estado:** Aprobado (usuario delegó ejecución; revisión al final)
- **Reemplaza el motor de render de:** `2026-07-01-ri-clon-posicionado-design.md` (clon posicionado / PdfSharp / subida) — se retira.
- **Repos:** `ZynstormECFPlatform` (backend) + `ZynstormECFPlatform-FrontEnd` (quitar UI de subida).

## 1. Contexto y decisión

En vez de que el usuario suba un PDF modelo por cliente y extraerlo, se usan **plantillas QuestPDF predefinidas portadas de EasyInvoice** (el mismo diseño que el usuario ya usa para facturar). Se generan las RI de los comprobantes del Paso 4 con esas plantillas + los XML ya enviados a la DGII. **No hay subida ni extracción.**

### Mapeo tipo e-CF → plantilla (verificado en EasyInvoice)
- **`InvoicePdf`** (`EasyInvoice.Reports/Invoices/InvoicePdf.cs`, recibo 80mm `ContinuousSize(227)`, **ya incluye QR** y se adapta a Factura/NC/ND por tipo): **31, 32, 33, 34, 43, 44, 45, 46, 47**.
- **`PurchasePdf`** (`EasyInvoice.Reports/Purchases/PurchasePdf.cs`, **hoja completa** `PageSizes.Letter`, sin QR): **41 (Compras)** — se le agrega el bloque QR.

### Decisiones
- Motor: **QuestPDF** (ya referenciado; EasyInvoice usa QuestPDF 2026.2.3, Zynstorm 2026.5.0). QRCoder ya está.
- El QR se pasa como string URL construido con `EcfQrUrlBuilder` (canónico: FC para 32<250k, código, monto `0.##`). `InvoicePdf` ya renderiza el QR desde esa URL y extrae CódigoSeguridad/FechaFirma de sus query params.
- Se **retira** todo el flujo de subida/extracción: `RiModelExtractor` (PdfPig), `RiPdfRenderer` (PdfSharp), `PageModel`, `RiFontResolver` + fuente embebida, el paquete `PdfSharp`, la entidad/endpoints de subida de modelos, y la UI de subir en el frontend.
- Se **reutiliza**: `EcfRiDataMapper` (extendido para poblar el view-model), `EcfQrUrlBuilder`, y el flujo de generar/preview/descargar/ZIP de los comprobantes del Paso 4.

## 2. Arquitectura

Backend, carpeta `ZynstormECFPlatform.Services/Ri/`:
1. **View-models** (`RiInvoiceModel`, `RiPurchaseModel`) — POCOs con solo los campos que usan las plantillas portadas (Company{Name,Rnc,Address,Phone,...}, Client/Supplier, NCF/tipo, ítems, subtotales/itbis/total/descuento, nota, `Qr` string, `SecurityCode`, fechas). Se evita arrastrar el grafo de entidades de EasyInvoice.
2. **`RiInvoicePdf`** y **`RiPurchasePdf`** (`IDocument`) — plantillas QuestPDF portadas de EasyInvoice, adaptadas a los view-models de Zynstorm; `RiPurchasePdf` con bloque QR agregado.
3. **`EcfRiTemplateMapper`** — XML firmado del Paso 4 → view-model correspondiente (extiende/usa `EcfRiDataMapper`); setea `Qr` con `EcfQrUrlBuilder.Build(...)` (ambiente `CerteCF`).
4. **`RiPdfRenderer`** (reescrito) — `Render(int ecfType, string signedXml) : byte[]`: elige plantilla por tipo (41→Purchase, resto→Invoice), mapea, y `Document.Create(...).GeneratePdf()`.
5. **`CertificationRiModelService`** (simplificado) — quita subir/extraer/confirmar/reasignar; deja `RenderRiForDocumentAsync(clientGuidId, ncf)` y `RenderAllZipAsync(clientGuidId, webRootPath)` usando el nuevo `RiPdfRenderer`. La resolución del comprobante (XmlSent del `CertificationDocument`) se mantiene.

## 3. Datos y flujo
- Fuente de datos: el `XmlSent` de cada `CertificationDocument` del Paso 4 (ya existe). El "Company" (emisor) = datos del emisor del XML (el cliente que se certifica); "Client/Supplier" = comprador.
- Generación on-demand: preview/descarga por comprobante + ZIP de todos (patrón existente en `wwwroot`).
- Ya no se persiste ningún modelo (ni fuente ni referencia): las plantillas son código built-in.

## 4. Qué se retira / entidad
- Retirar: `RiModelExtractor.cs`, `RiPdfRenderer.cs` (PdfSharp) → se reescribe a QuestPDF, `PageModel.cs`, `RiFontResolver.cs` + `Ri/Fonts/*`, paquete `PdfSharp`. Endpoints de subida (`POST/PUT/GET print-templates*`, `DELETE`) y su parte del servicio. UI de subir modelo en el frontend Paso 5.
- La entidad `CertificationInvoicePrintTemplate` + join quedan sin uso. Se dejan en la BD (no se borra la tabla para no arriesgar) pero sin endpoints; opcionalmente marcar como obsoleta. (No hay migración de borrado en este spec.)
- Endpoints que se mantienen: `GET ri/{clientGuidId}/{ncf}/preview` y `/download`, `GET ri/{clientGuidId}/zip`.

## 5. Frontend (Paso 5)
- Quitar la sección de subir/gestionar modelos. Dejar: lista de comprobantes del Paso 4 con "Vista previa" (iframe al preview) + "Descargar" por comprobante, y "Descargar todas (ZIP)". Sin selección de modelo (la plantilla se elige por tipo automáticamente).

## 6. Errores
- XML inválido/incompleto → error por comprobante (no rompe el lote/ZIP).
- Tipo sin plantilla específica → usa `InvoicePdf` (base). 41 → `PurchasePdf`.
- Fallo de QuestPDF/QRCoder → error controlado por comprobante.

## 7. Testing
- **`EcfRiTemplateMapper`** (unit, XML reales E31/E32<250k/E41): view-model poblado correcto (emisor, comprador, ítems, totales) y `Qr` con el portal correcto (FC vs regular) vía `EcfQrUrlBuilder`.
- **`RiInvoicePdf`/`RiPurchasePdf`** (humo): `Render(tipo, xml)` produce bytes `%PDF` no vacíos; para InvoicePdf, leer el texto con PdfPig y asertar que aparecen NCF, total y el Código de Seguridad; para Purchase, `%PDF` + tamaño.
- **`EcfQrUrlBuilder`**: tests existentes siguen.
- Fidelidad visual: verificación por el controller renderizando y abriendo el PDF (Read), como en el rediseño anterior.

## 8. Notas de implementación
- Portar `InvoicePdf.cs` (426 líneas) y `PurchasePdf.cs` (259) desde `EasyInvoice-WorkSpace/EasyInvoice/EasyInvoice.Reports/`, junto con las constantes/helpers mínimos que referencian (`Culture.CultureInfo`, `PaymentType`, formato de NCF) — copiar solo lo necesario, adaptando namespaces a `ZynstormECFPlatform.Services.Ri`.
- `InvoicePdf` ya extrae `CodigoSeguridad`/`FechaFirma` de la URL del QR (query params); asegurar que la URL canónica los incluya (para 32<250k FC no hay FechaFirma — manejar ausencia).
- QuestPDF license Community en `static` ctor (patrón existente).
- Quitar `PdfSharp` del csproj de Services tras eliminar `RiFontResolver`/`RiPdfRenderer` viejo.
