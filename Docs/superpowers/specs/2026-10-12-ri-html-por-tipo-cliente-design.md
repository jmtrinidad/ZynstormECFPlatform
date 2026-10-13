# RI con formato HTML por tipo de cliente — Diseño

Fecha: 2026-10-12 · Paso 5 de certificación ("Prueba Simulación Representación Impresa")

> **PENDIENTE DE REPLICAR (no olvidar, no requiere que el usuario lo repita):** al terminar y verificar esta funcionalidad, los mismos cambios deben replicarse en las ramas **`Staging`** y **`feature/emit-idempotente`** del backend (`ZynstormECFPlatform`) y en **`Staging`** del frontend (`ZynstormECFPlatform-FrontEnd`, que no tiene rama emit-idempotente). Incluye la migración EF: debe aplicarse sin romper el historial de migraciones de cada rama. Antes de dar el trabajo por cerrado, confirmar con el usuario que ramas ya recibieron los cambios.

## 1. Objetivo

Hoy la RI (Representación Impresa) de cada comprobante del Paso 5 sale siempre de plantillas QuestPDF fijas por tipo de e-CF (`RiPdfRenderer`). Se agrega la posibilidad de que un **tipo de cliente** (ej. "Royaltrix Software") tenga su propio **formato HTML**, con marcadores `{{...}}` que el sistema reemplaza con los datos del XML firmado del comprobante.

## 2. Restricción principal: no romper lo existente

- Si el cliente **no tiene tipo de cliente ni formato propio**, el comportamiento es **idéntico al actual**: PDF QuestPDF por defecto, mismos endpoints, mismo contenido y mismo ZIP de PDFs.
- `RiPdfRenderer`, `RiInvoicePdf`, `RiPurchasePdf`, `RiExpensePdf` y `EcfRiTemplateMapper` no se modifican en su comportamiento. El camino HTML es **aditivo**.
- Las columnas nuevas de `Client` son nulables; los clientes existentes quedan sin cambios tras la migración.
- Los tests existentes (`RiInvoicePdfTests`, etc.) deben seguir pasando sin edición.

## 3. Modelo de datos

| Entidad | Campos | Notas |
|---|---|---|
| `ClientType` (nueva) | `ClientTypeId`, `Name` (único), `Description?`, `RiHtmlTemplateId?`, `SampleDataJson?`, auditoría (`BaseEntity`) | `SampleDataJson`: valores de muestra para campos que el XML no trae (§5). |
| `RiHtmlTemplate` (nueva) | `RiHtmlTemplateId`, `Name`, `Html`, `FileName`, auditoría | Biblioteca de formatos. Un formato lo pueden usar varios tipos y clientes. |
| `Client` (se amplía) | `ClientTypeId?`, `RiHtmlTemplateId?` | `RiHtmlTemplateId` = formato propio del cliente (excepción). |

No se reutiliza `CertificationInvoicePrintTemplate` (heredada del modelo PDF anterior: `LayoutJson`, `FileData`, estados de extracción; sin uso).

**Resolución del formato** para un comprobante de un cliente:
1. `Client.RiHtmlTemplateId` (formato propio) →
2. `Client.ClientType.RiHtmlTemplateId` →
3. PDF QuestPDF por defecto (comportamiento actual).

Migración EF nueva y aditiva: crea las dos tablas y agrega las dos columnas nulables a `Client` con sus FK (`ON DELETE SET NULL` hacia el formato y hacia el tipo, para no impedir borrar un formato o un tipo).

## 4. Marcadores

Planos, PascalCase, tal como en el HTML de referencia del usuario:

- **Emisor** (del `Client` seleccionado, no del XML; igual que `BuildCompanyHeaderAsync`): `EmisorLogo`, `EmisorDireccion`, `EmisorRNC`, `EmisorTelefono`.
- **Comprobante** (del XML): `TipoComprobante` (nombre, ej. "FACTURA DE CONSUMO ELECTRÓNICA"), `eNCF`, `FechaVencimiento`, `NoFactura` (NumeroFacturaInterna), `Fecha`, `FormaPago`, `Nombre` (comprador), `QR`, `CodigoSeguridad`, `FechaFirma`.
- **Totales** (del XML): `Total`, `TotalDescuento`, más `Subtotal` e `Itbis`.
- **De muestra** (del tipo de cliente, §5): `Cedula`, `Sexo`, `FechaNac`, `Edad`, `SegMed`, `NSS`, `Devolucion`, `Medico`, `MontoPagado`, `Restante`.
- **Ítems** (repetibles): `Descripcion`, `Monto`, `Cantidad`, `Precio`, `ItemItbis`, `Unidad` (el ITBIS por ítem usa `ItemItbis` para no chocar con el `Itbis` de totales).

Datos de comprobante, totales e ítems salen de `RiInvoiceModel` vía `EcfRiTemplateMapper.MapInvoice` para todos los tipos (el mapper es genérico) (se reutiliza el mapeo existente, sin duplicarlo). Un marcador desconocido se deja tal cual y se reporta como aviso en la subida.

## 5. Datos de muestra por tipo de cliente

`ClientType.SampleDataJson` es un objeto plano `{ "Cedula": "...", "Sexo": "...", ..., "EmisorLogo": "data:image/png;base64,..." }`. Lo edita el usuario junto al formato. Es simulación de certificación, por lo que los valores son ficticios y se usan en todas las RI de los clientes de ese tipo. Una clave ausente equivale a valor vacío. Si el XML trae un dato real que coincide con un marcador (ej. nombre del comprador), el XML tiene prioridad sobre la muestra. `Monto Pagado` y `Restante` salen de la muestra si existen; si no, quedan vacíos.

## 6. Reglas del render (`RiHtmlRenderer`)

1. **Ítems**: se localiza el menor `<tr>` o elemento de bloque que contenga un marcador de ítem (`Descripcion`, `Monto`, `Cantidad`, `Precio`, `Itbis`, `Unidad`) y se repite por cada ítem del XML. Sin bucles en el HTML. Debe haber al menos una fila de ítems.
2. **Elemento vacío se elimina**: si todos los marcadores de un elemento contenedor (fila, `div`, `p`, `span` de línea) resuelven a vacío, se elimina ese elemento. Con esto, en los tipos **32 y 34** desaparece la línea `VALIDO HASTA` (no tiene `FechaVencimiento`) sin modificar el HTML; un `EmisorLogo` vacío elimina el recuadro.
3. **Bloques obligatorios**: si el HTML trae `{{eNCF}}` / `{{FechaVencimiento}}`, `{{QR}}`, `{{CodigoSeguridad}}` y `{{FechaFirma}}`, se usan donde estén. Si falta `{{eNCF}}`, el sistema agrega el bloque `eNCF` + `VALIDO HASTA` justo debajo del título del comprobante (el elemento con `{{TipoComprobante}}` o, en su defecto, el primer encabezado), y si falta `{{QR}}` agrega el bloque del QR (imagen QR, código de seguridad, fecha de firma digital, "REPRESENTACION IMPRESA DEL e-CF") antes de `</body>`.
4. **QR**: imagen generada con QRCoder a partir de la URL de `EcfQrUrlBuilder` (la misma ya usada por `RiInvoicePdf`), inline como `data:image/png;base64`.
5. **Escapado**: todo valor se escapa como HTML. Solo `EmisorLogo` y `QR` se emiten como `<img src="data:...">`, y `EmisorLogo` solo acepta un `data:image/...`.
6. **Regla de fecha de vencimiento**: `FechaVencimiento` solo se llena para los tipos que la tienen; los consumo (32) y la nota de crédito (34) nunca, igual que `RiInvoicePdf`.

## 7. Subida y seguridad del HTML

`RiHtmlTemplateValidator` en el servicio:
- Limpia `<script>`, atributos `on*`, `javascript:` en `href`/`src`, `<iframe>`, `<object>`, `<embed>` y `<base>`.
- **Rechaza** si no hay fila de ítems (`{{Descripcion}}` o `{{Monto}}`) o falta `{{Total}}`.
- **Avisa** (no bloquea) por marcadores desconocidos y por bloques que el sistema agregará (eNCF, QR).
- Límite de tamaño (2 MB). El HTML limpio es lo que se guarda.

Defensa en profundidad: la vista previa se muestra en un `iframe` con `sandbox` (sin scripts), y la respuesta lleva `Content-Security-Policy: default-src 'none'; img-src data:; style-src 'unsafe-inline'`.

## 8. API (`CertificationController`)

Los endpoints nuevos viven en un `RiFormatController` (`v1/RiFormat/...`, `[Authorize]`), no en `CertificationController`; las rutas exactas están en el plan. Existentes, **misma ruta y contrato**: `GET ri/{clientGuidId}/{ncf}/preview|download`, `GET ri/{clientGuidId}/zip`. Con formato HTML resuelto devuelven `text/html` (descarga como `RI_{ncf}.html`); sin formato, PDF como hoy. El ZIP contiene `.html` o `.pdf` según corresponda.

Nuevos:
- `GET/POST/PUT/DELETE client-types` — CRUD de tipos de cliente, incluido `SampleDataJson`.
- `GET/POST/PUT/DELETE ri/html-templates` — biblioteca de formatos (la subida valida según §7); no se permite borrar un formato en uso (se rechaza con mensaje).
- `PUT clients/{clientGuidId}/ri-format` — asigna `ClientTypeId` y/o `RiHtmlTemplateId` propio (o los quita con null).
- `GET ri/html-templates/sample` — HTML de ejemplo descargable (el de referencia con todos los marcadores).
- `GET ri/{clientGuidId}/format` — formato resuelto (propio / tipo / defecto) para mostrarlo en la UI.

## 9. Frontend (Paso 5, `ri-step5.tsx`)

Tarjeta **"Formato de factura"** sobre la lista de comprobantes:
- Muestra qué formato rige: *Propio del cliente*, *Del tipo de cliente "X"* o *Por defecto*.
- Selector de **tipo de cliente** (con opción "Ninguno" = por defecto) y alta de un tipo nuevo.
- Subir/reemplazar el HTML del tipo (con el resultado de la validación: errores y avisos), editar los valores de muestra, ver la lista de marcadores y descargar el HTML de ejemplo.
- Opción "Usar un formato propio para este cliente" (excepción).
- **Vista previa**: HTML en `iframe` con `sandbox`; botón **Imprimir / Guardar PDF** que invoca `print()` del iframe. Si el formato es el por defecto, todo funciona exactamente como hoy (PDF).
- Servicios en `services/certification.service.ts` y tipos en `types/`.

## 10. Pruebas

- **Renderer**: con el HTML de referencia, tipos 31, 32, 34 y 44 sobre XML reales: 32 y 34 sin `VALIDO HASTA`; 31 y 44 con él; ítems repetidos N veces; QR, código de seguridad y fecha de firma presentes; marcadores faltantes (eNCF/QR) inyectados en el lugar correcto; valores escapados (`<`, `&`); logo vacío elimina el recuadro.
- **Validador**: rechazo sin ítems o sin `Total`; limpieza de `script`/`on*`/`javascript:`; avisos por marcadores desconocidos.
- **Resolución**: cliente propio > tipo > defecto; **cliente sin tipo ni formato produce el mismo PDF que antes** (regresión).
- **Servicio**: ZIP mixto; tipo sin formato asignado cae al defecto.
- Build `dotnet build ZynstormECFPlatform.slnx -nologo`, `dotnet test`, `npx tsc --noEmit` sin errores nuevos.

## 11. Fuera de alcance

- Generar PDF desde HTML en el servidor (Chromium): se imprime desde el navegador.
- Editor visual de formatos; se sube un HTML.
- Mapeo de campos adicionales desde etiquetas del XML (se usan datos de muestra).
- Cambios al formato por defecto QuestPDF.

## 12. Replicación a otras ramas

Ver la nota al inicio. Plan: implementar y verificar en una rama de trabajo, luego llevar los commits a `Staging` y `feature/emit-idempotente` (backend) y a `Staging` (frontend). Revisar conflictos en `StorageContext.cs`, `StorageContextModelSnapshot.cs`, `CertificationController.cs` y la migración, que son los puntos que divergen entre ramas.
