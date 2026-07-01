# Diseño — Representación Impresa (RI) a partir de un PDF modelo (Paso 5)

- **Fecha:** 2026-06-30
- **Estado:** Aprobado (pendiente de plan de implementación)
- **Repos afectados:** `ZynstormECFPlatform` (backend), `ZynstormECFPlatform-FrontEnd` (frontend)

## 1. Objetivo

En el **Paso 5** de la certificación ("Prueba Simulación Representación Impresa") permitir subir un **PDF modelo** por cliente para generar automáticamente la **Representación Impresa (RI)** de los comprobantes generados en el **Paso 4**. Cada PDF modelo puede asignarse a **varios tipos** de e-CF. La RI generada **debe incluir el QR** del comprobante para poder validarse en la DGII. La generación/descarga es un **proceso manual** (vista previa + descarga individual y ZIP).

### Decisiones acordadas
- **Rol del PDF:** extraer estilo y **reconstruir** (no overlay).
- **Fidelidad:** **réplica automática** del layout, viable porque los PDF son de **formato conocido/estandarizado** (se ancla en etiquetas conocidas).
- **Alcance del modelo:** **por cliente** (cada cliente sube sus propios modelos).
- **Salida Paso 5:** **vista previa + descarga individual y ZIP** (mismo patrón que el Paso 4).
- El QR se genera **reutilizando la lógica canónica de URL ya corregida** (portal FC para E32<250k, `CodigoSeguridadeCF ?? SignatureValue[..6]`, tipo derivado del NCF, monto `0.##`).

### No-objetivos (YAGNI)
- No réplica pixel-perfect (QuestPDF es de flujo; el eje vertical se adapta a la cantidad de ítems).
- No OCR (los PDF de entrada tienen texto seleccionable).
- No editor visual de plantilla (la extracción es automática; el usuario solo confirma vía preview).
- Modelos **no** globales ni con fallback global (solo por cliente).

## 2. Arquitectura y componentes

Backend en `ZynstormECFPlatform.Services`, carpeta nueva `Ri/`. QuestPDF ya está referenciado; se agregan los paquetes **UglyToad.PdfPig** (lectura) y **QRCoder** (imagen del QR).

1. **`RiModelExtractor`** (PdfPig): PDF modelo → **`LayoutJson`** (descriptor de layout).
2. **`EcfRiDataMapper`**: XML firmado del Paso 4 → **`RiData`** (emisor, comprador, ítems, totales, eNCF, fechas, `securityCode`, URL del QR). Se porta de `PreviewController.BuildPreviewModel` de Mechanic-Service.
3. **`EcfQrUrlBuilder`** (compartido, en `Common`/`Services`): construye la URL del QR. Se **extrae desde `ReceivedEcfProductionService.BuildQrUrl`** para que producción y la RI usen la **misma** lógica (un solo lugar con el fix ya validado contra la DGII).
4. **`RiPdfRenderer`** (QuestPDF): `LayoutJson + RiData → bytes PDF` (incluye QR con QRCoder).
5. **`CertificationRiModelService`**: orquesta subir modelo, extraer, confirmar, listar, y generar RIs (individual/ZIP).

**Flujo de datos:**
- Subida: `PDF modelo → RiModelExtractor → LayoutJson (persistido)`.
- Generación: `XML Paso 4 → EcfRiDataMapper → RiData` + `LayoutJson → RiPdfRenderer → PDF RI`.

## 3. Modelo de datos

Se refactoriza la entidad existente `CertificationInvoicePrintTemplate` (hoy scaffoldeada, **sin uso ni datos**) para representar **un modelo = un PDF**, y se agrega la relación multi-tipo.

### `CertificationInvoicePrintTemplate` (el modelo)
- `Id`, `ClientId` (FK), `Name`, `Description?`
- `FileName`, `ContentType`, `FileData` (byte[]) → **el PDF fuente se guarda en BD** (pequeño; igual que `ClientCertificate`). Se **elimina** `EcfTypeId` único (se reemplaza por join). `FileUrl` opcional/eliminado.
- `LayoutJson` (nvarchar max) → descriptor extraído (§4).
- `Status` (enum): `PendingExtraction` / `Extracted` / `Confirmed` / `Failed`.
- `ExtractionWarnings?` (JSON con anclas no encontradas, etc.).
- Campos de `BaseEntity` (GuidId, timestamps).

### `CertificationInvoicePrintTemplateEcfType` (join nuevo)
- `Id`, `CertificationInvoicePrintTemplateId` (FK), `EcfTypeId` (FK). Único por `(template, type)`.
- **Regla de negocio:** para un `(ClientId, EcfTypeId)` hay **a lo sumo un** modelo activo. Asignar un tipo a un modelo nuevo **lo reasigna** (se quita del anterior) → al generar la RI de un comprobante hay un único modelo determinístico por tipo.

### Navegaciones / contexto
- `Client` → `ICollection<CertificationInvoicePrintTemplate>`.
- `EcfType` vía join.
- Registrar en `StorageContext` + **migración** (sin datos que migrar).

### Almacenamiento de salida
- Las RI se generan **on-demand** (preview/descarga). El ZIP se arma temporalmente en `wwwroot` como los ZIP del Paso 4. Solo el PDF modelo persiste.

## 4. Motor de extracción y esquema `LayoutJson`

`RiModelExtractor` usa PdfPig (palabras con *bounding box*, imágenes, trazos vectoriales, tamaño de página) con **extracción anclada en etiquetas conocidas**:

1. **Diccionario de anclas** (case/acento-insensible): `RNC`, `NCF`/`e-NCF`, `Fecha`, comprador (`Cliente`/`Razón Social`), cabeceras (`Descripción`, `Cantidad`, `Precio`, `ITBIS`, `Valor`/`Importe`), totales (`Sub-Total`, `ITBIS`, `Total`), `Código de Seguridad`, `Fecha de Firma`.
2. Por cada ancla → **slot del campo** (posición del dato, típicamente a la derecha o debajo de la etiqueta).
3. **Logo**: imagen más grande de la zona superior → bytes + posición/tamaño.
4. **Tabla de ítems**: rangos X de columnas desde cabeceras + Y del encabezado.
5. **Colores/fuentes**: color de texto dominante, fondo de encabezado (rect vectorial), niveles de tamaño.
6. **Textos fijos** no mapeados (leyendas/pie legales) → verbatim con posición.
7. **Slot del QR**: si hay región tipo QR cerca de "Código de Seguridad" se usa; si no, se **reserva posición por defecto** (inferior, según norma RI DGII) para el QR + código de seguridad.

Salida `LayoutJson` con **coordenadas normalizadas (0..1)**:

```jsonc
{
  "page": { "widthPt": 612, "heightPt": 792, "margin": 0.04 },
  "palette": { "primary": "#1a3e6f", "text": "#000", "headerBg": "#eef2f7" },
  "logo": { "x":0.06,"y":0.03,"w":0.25,"h":0.08,"imageRef":"logo1" },
  "fieldSlots": { "eNCF": {"x":0.7,"y":0.12,"label":"NCF:"}, "rncComprador": {"x":0.2,"y":0.22,"label":"RNC:"} },
  "itemsTable": { "topY":0.35, "columns":[
    {"field":"descripcion","x":0.06,"w":0.44,"align":"left"},
    {"field":"cantidad","x":0.50,"w":0.10,"align":"right"},
    {"field":"precio","x":0.60,"w":0.12,"align":"right"},
    {"field":"itbis","x":0.72,"w":0.12,"align":"right"},
    {"field":"valor","x":0.84,"w":0.12,"align":"right"} ] },
  "totals": [ {"field":"subtotal","label":"Sub-Total"}, {"field":"itbis","label":"ITBIS"}, {"field":"total","label":"Total"} ],
  "qr": { "x":0.08,"y":0.80,"size":0.15 },
  "fixedTexts": [ {"text":"REPRESENTACIÓN IMPRESA DEL e-CF","x":0.5,"y":0.96,"align":"center","fontSize":8} ],
  "images": { "logo1":"<base64>" }
}
```

Es **best-effort**: anclas no halladas → `ExtractionWarnings` + *defaults* en el renderer. La vista previa de confirmación (§6) permite detectarlo.

## 5. Mapper, Renderer y QR

**`EcfRiDataMapper`**: XML firmado → `RiData`. Campos: emisor (RNC, razón social, dirección, teléfono), comprador, `eNCF`, `tipoeCF`, `fechaEmision`, `fechaFirma`, ítems (descripción, cantidad, precio, itbis, monto), totales (subtotal, itbis, exento, gravado, total), `securityCode` (`CodigoSeguridadeCF ?? SignatureValue[..6]`) y **URL del QR**.

**QR:**
- URL con `EcfQrUrlBuilder` compartido (mismo `BuildQrUrl` corregido; ambiente `CerteCF` para certificación).
- Imagen con **QRCoder** (PNG) incrustada en QuestPDF junto al Código de Seguridad, en el `qr` slot del layout.

**`RiPdfRenderer`** (`LayoutJson + RiData → bytes PDF`):
- Página con tamaño del modelo; aplica `palette`/fuentes; coloca logo, textos fijos y bloques por coordenadas normalizadas.
- **QuestPDF es de flujo, no de coordenadas absolutas.** Honra del modelo: **columnas de la tabla, etiquetas, branding, posiciones de encabezado/pie/QR**; el **eje vertical fluye** según la cantidad de ítems (la tabla arranca en `itemsTable.topY` y totales/QR se ubican después). Reconstrucción fiel de **estructura**, no pixel-perfect.
- Slots faltantes (warnings) → *defaults* razonables.
- Mismo renderer para **preview de confirmación** (datos de ejemplo) y **RI reales** (datos del XML).

## 6. Flujo Paso 5 — endpoints y frontend

### Endpoints nuevos (`CertificationController`)
- `POST /print-templates` (multipart: `pdfFile`, `clientGuidId`, `name`, `ecfTypeCodes[]`) → guarda modelo, corre extractor, estado `Extracted` (+ warnings).
- `GET /print-templates/{clientGuidId}` → lista modelos del cliente (tipos, estado, warnings).
- `GET /print-templates/{templateGuidId}/preview` → PDF de confirmación (datos de ejemplo).
- `PUT /print-templates/{templateGuidId}` → renombrar / reasignar tipos / `Confirm`.
- `DELETE /print-templates/{templateGuidId}` → eliminar.
- `GET /ri/{clientGuidId}/{ncf}/preview` y `/download` → RI de un comprobante del Paso 4 (usa `XmlSent` guardado + modelo del tipo).
- `GET /ri/{clientGuidId}/zip` → ZIP con todas las RI (on-demand en `wwwroot`).

Fuente de comprobantes del Paso 4: los `CertificationDocument` del cliente (`XmlSent`), vía la lógica existente de `last-results`.

### Frontend (`app/certificacion/page.tsx`, bloque `currentStep === 5`)
- **Modelos**: uploader PDF + nombre + multi-select de tipos presentes en el Paso 4; lista de modelos con tipos, badge de estado, warnings, "Ver preview" (confirmación), reasignar tipos, eliminar.
- **Generación**: lista de comprobantes del Paso 4 (de `last-results`), cada uno con su tipo y si tiene modelo asignado; por fila "Vista previa" + "Descargar" RI; botón "Descargar todas (ZIP)". Filas cuyo tipo no tiene modelo confirmado se marcan ("asigna un modelo para el tipo X").
- Preview en modal con visor PDF (`iframe`/`object` al endpoint de preview).

## 7. Manejo de errores
- PDF ilegible / cifrado / escaneado sin texto → `Failed` + mensaje claro ("usa un PDF con texto seleccionable"); no queda `Confirmed`.
- Anclas faltantes → warnings + defaults; se permite confirmar tras ver el preview.
- Tipo sin modelo confirmado al generar → RI de ese comprobante bloqueada con aviso; el ZIP marca/omite los faltantes.
- XML del Paso 4 inválido/incompleto → error por comprobante, no rompe el lote.
- Fallo de QRCoder/render → error controlado por comprobante.
- Subida: validación de `content-type` (application/pdf) y límite de tamaño.

## 8. Testing
- **`RiModelExtractor`** (unit, PDFs fixture de formato conocido): anclas detectadas, columnas, logo, warnings. Casos: PDF ok, PDF con anclas faltantes, PDF sin texto → `Failed`.
- **`EcfRiDataMapper`** (unit, XML reales existentes E31 / E32<250k / E32≥250k): `RiData` correcto + URL de QR correcta (FC vs regular).
- **`EcfQrUrlBuilder`** (unit): blinda el fix ya validado contra la DGII (portal, código, monto `0.##`, tipo por NCF).
- **`RiPdfRenderer`** (humo): bytes con cabecera `%PDF` no vacíos, con layout + data + QR.
- **Integración**: subir modelo → extraer → preview → generar RI de un comprobante.

## 9. Notas de implementación
- Paquetes nuevos: `UglyToad.PdfPig`, `QRCoder` en `ZynstormECFPlatform.Services`.
- Extraer `BuildQrUrl` de `ReceivedEcfProductionService` a `EcfQrUrlBuilder` compartido y hacer que producción lo consuma (sin cambiar comportamiento; cubierto por tests).
- Migración EF por el refactor de `CertificationInvoicePrintTemplate` + join.
- Reusar patrón de `IFormFile` (subidas) y de ZIP en `wwwroot` (Paso 4) ya presentes en `CertificationController`.
