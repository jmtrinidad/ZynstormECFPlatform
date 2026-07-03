# Diseño — Fidelidad de las plantillas RI al diseño EasyInvoice + columna Address en Client

- **Fecha:** 2026-07-02
- **Estado:** Aprobado (usuario delegó ejecución; revisión al final)
- **Complementa a:** `2026-07-02-ri-plantillas-easyinvoice-design.md` (port inicial de las plantillas). Este spec corrige omisiones y errores de ese port.
- **Repos:** `ZynstormECFPlatform` (backend) + `ZynstormECFPlatform-FrontEnd` (campo Address en el formulario de cliente).

## 1. Contexto

El port inicial de las plantillas EasyInvoice a QuestPDF (`RiInvoicePdf`/`RiPurchasePdf`) omitió campos y variantes por tipo que sí tiene el diseño original (`EasyInvoice.Reports/Invoices/InvoicePdf.cs`, `Expenses/ExpensePdf.cs`, `Purchases/PurchasePdf.cs`), y el mapeo del tipo 41 (Compras) invirtió los roles emisor/comprador. Verificado contra el código real de EasyInvoice y XMLs firmados de certificación.

### Cómo rutea EasyInvoice por tipo de e-CF (fuente de verdad)
- **`InvoicePdf`** (recibo 80mm): 31, 32, 33, 34, 44, 45, 46, 47 — una sola plantilla con variantes por tipo (ver §3).
- **`ExpensePdf`** (recibo 80mm, `IsInformal`): **43** — "GASTOS MENORES ELECTRÓNICO".
- **`PurchasePdf`** (hoja Letter): **41**.

Zynstorm hoy manda todo excepto 41 a `RiInvoicePdf`: falta la plantilla de gastos para el 43.

### Bugs confirmados en el port actual
1. `RiInvoicePdf` imprime el eNCF en las filas "eNCF:" y "FACTURA:" (duplicado).
2. `MapInvoice` nunca llena `PaymentType`, `Discount` ni `Note` (la fila TIPO DE PAGO jamás sale).
3. **Crédito/débito invertidos**: DGII/EasyInvoice: tipo **33 = Nota de Débito**, **34 = Nota de Crédito**. El port (`RiInvoicePdf.isCreditNote/isDebitNote` y `NcfTypeName`) los tiene al revés; `dgii_ecf_requirements.md` líneas 23-24 también (se corrige).
4. **SubTotal exento en RD$0.00**: `EcfRiDataMapper` usa solo `MontoGravadoTotal`; en comprobantes exentos el subtotal sale 0 aunque el total no lo sea.
5. **41 invertido**: `MapPurchase` pone Company=nodo `Comprador` y Supplier=nodo `Emisor`. En el 41 el `Emisor` del XML ES la empresa que registra la compra y el nodo `Comprador` es el suplidor informal (dgii_ecf_requirements.md §41, SamplePayloads/41_Compras.json).
6. **Retenciones del 41 no mapeadas**: `TotalISRRetencion`/`TotalITBISRetenido` existen en el XML; `IsrRetentionAmount/Rate` nunca se llenan → la fila de retención jamás se imprime y "Total Neto" = total bruto.
7. **ITBIS% por línea del 41 siempre 0%**: se calcula `Itbis/Amount` por ítem, pero en el 41 los ítems no traen `MontoITBIS`.

### Decisiones del usuario
- CAJERO / ATENDIDO POR / USUARIO: constante **"PEDRO"** (las RI son para certificación).
- TOTAL RECIBIDO (solo contado): el total si es entero; si trae decimales, `Math.Ceiling(total)`. SU CAMBIO = recibido − total.
- `Client.Address` se agrega a la tabla y además alimenta el encabezado de la RI (prioridad sobre la dirección de la sucursal en `BuildCompanyHeaderAsync`).

## 2. Ruteo por tipo (RiPdfRenderer)

| Tipo e-CF | Plantilla |
|---|---|
| 41 | `RiPurchasePdf` (existente, corregida) |
| 43 | **`RiExpensePdf` (nueva)** |
| 31, 32, 33, 34, 44, 45, 46, 47 y default | `RiInvoicePdf` (completada) |

## 3. RiInvoicePdf — completar al diseño real

Datos nuevos que el mapper extrae del XML (todos existen en el formato e-CF):

| Fila / bloque | Fuente XML | Regla |
|---|---|---|
| VALIDO HASTA | `IdDoc/FechaVencimientoSecuencia` | **Oculto para tipos 32 y 34** (paridad con `isEcfNote` del original). Formato dd/MM/yyyy. |
| FACTURA: / NOTA CRÉDITO: / NOTA DÉBITO: | `Emisor/NumeroFacturaInterna` | Fila omitida si el XML no lo trae (elimina el eNCF duplicado). Etiqueta según tipo: 34→NOTA CRÉDITO:, 33→NOTA DÉBITO:, resto→FACTURA:. |
| TIPO DE PAGO | `IdDoc/TipoPago` | 1→CONTADO, 2→CRÉDITO, otro→omitida. Solo para no-notas (ni 33 ni 34), como el original. |
| COND. PAGO | `IdDoc/FechaLimitePago` − `Emisor/FechaEmision` | Contado→"CONTADO"; crédito→"N DÍAS" (si N≤0 → 30). Solo no-notas. |
| FECHA | `Emisor/FechaEmision` | El XML no trae hora de emisión: se imprime la fecha tal cual (dd-MM-yyyy). |
| CAJERO | — | "PEDRO". |
| DATOS FACTURA AFECTADA (solo 33/34) | `InformacionReferencia/NCFModificado` | Bloque con NCF MODIFICADO + TIPO DE PAGO + COND. PAGO (paridad con original). |
| INFORMACIÓN DE MODIFICACIÓN (solo 33/34) | `InformacionReferencia/CodigoModificacion` (+ `RazonModificacion` si existe) | Código con descripción del catálogo DGII (1-5); razón/concepto si viene. |
| Línea de cantidad del ítem | `Item/UnidadMedida` | "2.00 Und x 187.50": código DGII → abreviatura (mini-tabla de códigos comunes, fallback "Und"). Precio unitario del XML tal cual (ya viene neto). |
| DESC: | Σ `Item/DescuentoMonto` | Igual que hoy: solo si > 0. |
| ATENDIDO POR | — | "PEDRO". |
| ARTÍCULOS | conteo de `Item` | |
| CONTADO:/CRÉDITO: | `TipoPago` + `MontoTotal` | Etiqueta según tipo de pago. |
| TOTAL RECIBIDO / SU CAMBIO | derivado | Solo contado: recibido = total entero ? total : ⌈total⌉; cambio = recibido − total. |
| FIRMA REQUERIDA | `TipoPago`=2 | Línea centrada tras el footer (paridad con original). |

Correcciones transversales: crédito/débito (33↔34) en `isCreditNote/isDebitNote` y `NcfTypeName`; subtotal = `MontoGravadoTotal + MontoExento` en `EcfRiDataMapper` (aplica a todas las plantillas).

## 4. RiExpensePdf — nueva plantilla para 43

Portada de `EasyInvoice.Reports/Expenses/ExpensePdf.cs` (rama `IsInformal`), leyendo un `RiExpenseModel` poblado por `EcfRiTemplateMapper.MapExpense`:

- Encabezado empresa (mismo patrón: nombre, RNC, dirección, tel, WA — con override de `RiCompanyHeader`).
- Título: "GASTOS MENORES ELECTRÓNICO".
- eNCF, VÁLIDO HASTA (`FechaVencimientoSecuencia`), MÉTODO PAGO (`TipoPago`: 1→CONTADO/2→CRÉDITO), FECHA (`FechaEmision`), USUARIO: "PEDRO".
- ACREEDOR, CATEGORÍA y GASTO NO se **omiten** (no existen en el XML del 43; el 43 no lleva comprador).
- CONCEPTO: descripciones de los ítems (unidas por "; " si hay varias).
- Totales: SUB-TOTAL, etiqueta ITBIS dinámica ("ITBIS 18%:"/"ITBIS 16%:"/"EXENTO:" — según tasa efectiva; monto "EXENTO" si TotalITBIS=0), TOTAL RD$.
- Bloque QR + "Codigo seguridad" + "Fecha firma digital" + "REPRESENTACIÓN IMPRESA DEL e-CF" + "Comprobante Electrónico Gastos Menores E43" (paridad con original + QR agregado como en las demás RI).

## 5. RiPurchasePdf / MapPurchase — correcciones del 41

- **Roles**: Company ← nodo `Emisor` (la empresa; el override de `RiCompanyHeader` sigue siendo coherente), SUPLIDOR ← nodo `Comprador` (suplidor informal). Corregir el doc-comment.
- **Retenciones**: `IsrRetentionAmount` ← `Totales/TotalISRRetencion`; nueva propiedad `ItbisRetentionAmount` ← `Totales/TotalITBISRetenido`; fila "Retención ITBIS" análoga a la de ISR; tasa ISR mostrada = monto/subtotal (si subtotal>0). **Total Neto = MontoTotal − TotalITBISRetenido − TotalISRRetencion**.
- **ITBIS% por línea**: derivado de `Item/IndicadorFacturacion` (1→tasa `ITBIS1` de Totales, 2→`ITBIS2`, 3→`ITBIS3`, 4/0→exento/0%); ITBIS del ítem = monto × tasa.

## 6. Client.Address

- `Client.Address` (`string?`), migración EF `AddClientAddress` (longitud 300).
- `ClientCreateDto.Address` (`[StringLength(300)]`) — hereda a `ClientUpdateDto`/`ClientViewDto`; mapeos de crear/actualizar/consultar en `ClientService`.
- Frontend: campo Address en tipos y formulario de cliente (crear/editar) y donde se muestre el detalle.
- `CertificationRiModelService.BuildCompanyHeaderAsync`: `Address = client.Address` con fallback a la dirección de la sucursal principal (comportamiento actual).

## 7. Documentación

- Corregir `dgii_ecf_requirements.md` (33=Nota de **Débito**, 34=Nota de **Crédito**).

## 8. Errores

- Igual que el spec anterior: XML inválido → error por comprobante sin romper el ZIP; tipo sin plantilla → `RiInvoicePdf`; fallo QR → se omite la imagen.
- Campos opcionales ausentes en el XML (`NumeroFacturaInterna`, `FechaLimitePago`, `InformacionReferencia`) → la fila/bloque se omite, nunca "N/D" salvo donde el original ya lo hacía (RNC/CED del cliente).

## 9. Testing

- **`EcfRiDataMapper`/`EcfRiTemplateMapper`** (unit, XMLs reales de certificación): subtotal exento; campos nuevos (VALIDO HASTA, NumeroFacturaInterna, TipoPago, InformacionReferencia); 41 con roles correctos (Company=Emisor) y retenciones; 43 → modelo de gasto.
- **Plantillas** (humo + texto con PdfPig): E31 contado (VALIDO HASTA, TIPO/COND PAGO, PEDRO, ARTÍCULOS, TOTAL RECIBIDO/SU CAMBIO con redondeo), E34 (título nota de crédito, sin VALIDO HASTA, bloques de modificación), E41 (empresa/suplidor correctos, retenciones, Total Neto), E43 (plantilla de gasto).
- Fidelidad visual: render por controller y revisión de los PDFs.
