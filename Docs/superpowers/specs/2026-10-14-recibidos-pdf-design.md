# Comprobantes recibidos en PDF: panel, API y librería

Fecha: 2026-10-14. Estado: aprobado por el usuario.

## Objetivo

1. El panel (Facturas → Recibidas) permite descargar cada e-CF recibido de un cliente como PDF, con un diseño profesional y moderno.
2. La API con API key expone tres endpoints para el integrador: consultar si hay recibidos, descargar el PDF y descargar el XML.
3. La librería `Zynstorm.DGII.ECF` (`C:\Projects\Zynstorm.DGII.ECF`) los expone con métodos propios y se documenta en las tres guías (`.md`, `.html` y `.pdf`).

## Contexto

- Los recibidos viven en `ReceivedB2BMessage` (`RawXml`, `ENcf`, `RncEmisor`, `RncComprador`, `MessageType`, `ReceivedAtUtc`). `MessageType.Ecf` es un e-CF de un tercero hacia el cliente; `AprobacionComercial` es la respuesta a uno que el cliente emitió.
- `ReceivedB2BMessagesCleanupJob` borra los recibidos con más de **30 días**. El integrador tiene que descargar dentro de ese plazo; las guías lo dicen.
- El panel ya lista, ve, descarga el XML y envía por correo (`FeController`, `client/{clientId}/received-files...`, con sesión JWT).
- Los PDF de RI actuales (QuestPDF) y los formatos HTML por tipo de cliente son del **emisor**. El recibido es de un tercero, así que no los usa: lleva un diseño propio.
- `EcfController` ya tiene el patrón de API key: `HttpContext.Items["ClientId"]`, 403 sin él, eNCF validado con `EcfLookupLogic.NormalizeNcf` (`Ecf/by-ncf/{eNcf}`).
- La librería habla solo con `EcfController`, devuelve resultados sin lanzar excepciones por red o rechazo, y soporta `net452`, `netstandard2.0` y `net8.0`.

## Diseño

### API (EcfController, `[ApiKeyAuth]`, acotado al cliente de la key)

| Endpoint | Respuesta |
|---|---|
| `GET Ecf/received?since=<ISO UTC>&limit=100` | `200 { count, hasMore, items:[{ eNcf, rncEmisor, ecfType, total, receivedAtUtc }] }` |
| `GET Ecf/received/{eNcf}/pdf[?rncEmisor=]` | `200 application/pdf`, archivo `ECF_{eNcf}.pdf` |
| `GET Ecf/received/{eNcf}/xml[?rncEmisor=]` | `200 application/xml`, archivo `ECF_{eNcf}.xml` |

- Solo `MessageType.Ecf`. Las aprobaciones comerciales no se listan ni se descargan por aquí.
- Lista: `ReceivedAtUtc > since` (exclusivo), orden ascendente, `limit` por defecto 100 y máximo 200. `hasMore` indica que hay más. El siguiente `since` es el `receivedAtUtc` del último ítem. Sin `since` devuelve desde lo más antiguo que se conserve. `total` sale del XML (`MontoTotal`); es `null` si el XML no se puede leer.
- `since` llega como texto y se interpreta como UTC (`DateTimeOffset.TryParse` con `AssumeUniversal`); inválido → 400.
- El eNCF solo es único por emisor. Si hay recibidos con ese eNCF de más de un RNC emisor y no se indica `rncEmisor` → `409 { found:true, message, emisores:[rnc...] }`. Con `rncEmisor` (se comparan solo los dígitos) se acota. Si hay varios del mismo emisor (reenvío) se toma el más reciente.
- Errores: sin API key de cliente → 403; eNCF inválido → 400; no existe → 404 `{ found:false, message }`; PDF de un XML ilegible → 422 `{ message }`.
- El QR del PDF usa el ambiente de `EcfXmlValidation:TargetDgiiEnvironment` y, si no existe, `Production`.

### Panel (FeController)

- `GET client/{clientId}/received-files/{id}/pdf` (sesión JWT), solo para `MessageType.Ecf` (400 para aprobaciones). Mismo renderizador que la API.

### Diseño del PDF

QuestPDF, A4, `ReceivedEcfPdf`, para todos los tipos (31 a 47). Paleta del panel: azul marino `#0A2540`, acento verde azulado `#0D9488`, texto `#0F172A`, apagado `#64748B`, líneas `#E2E8F0`, fondo suave `#F1F5F9`.

- **Cabecera** de ancho completo en azul marino con filete verde azulado: etiqueta «COMPROBANTE RECIBIDO», nombre del tipo de comprobante y, a la derecha, el eNCF en grande.
- **Tarjetas** Emisor y Comprador, con barra lateral de acento: nombre, RNC, dirección, teléfono y correo. Comprador vacío → «Consumidor final».
- **Franja de datos** con fecha de emisión, válido hasta, forma de pago y factura interna; solo muestra los que vienen en el XML.
- **Documento afectado** (notas 33 y 34): NCF modificado, código de modificación y motivo.
- **Tabla de ítems** con cabecera azul marino que se repite en cada página, filas alternadas y columnas #, Descripción (con unidad), Cant., Precio, Desc., ITBIS e Importe.
- **Totales** a la derecha: subtotal, descuento, exento, ITBIS con su tasa, retenciones si hay, y el total destacado.
- **Pie** en cada página: QR de la DGII, código de seguridad, fecha de firma, la leyenda «Representación impresa de un e-CF recibido» y «Página X de Y».

Mapeo: `EcfRiDataMapper.Map` ya extrae emisor, comprador, ítems, totales, QR y código de seguridad; `ReceivedEcfMapper` lo convierte en el modelo del PDF y reutiliza los helpers de `EcfRiTemplateMapper` (nombre del tipo, etiquetas de pago, unidades, código de modificación).

### Librería

Métodos nuevos en `IEcfClient` y `EcfClient`, con versión async y sync:

```csharp
Task<EcfReceivedList> GetReceivedAsync(DateTime? sinceUtc = null, int limit = 100, CancellationToken ct = default);
Task<EcfReceivedFile> GetReceivedPdfAsync(string eNcf, string rncEmisor = null, CancellationToken ct = default);
Task<EcfReceivedFile> GetReceivedXmlAsync(string eNcf, string rncEmisor = null, CancellationToken ct = default);
```

- `EcfReceivedStatus`: `Ok`, `NotFound`, `Ambiguous`, `InvalidRequest`, `Unauthorized`, `TransportError`, `ContractError`. No lanzan excepciones por red, timeout o respuesta del servidor; sí `ArgumentException` por un eNCF vacío (igual que `GetByNcf`).
- `EcfReceivedList`: `Status`, `Message`, `Detail`, `IsTimeout`, `Items` (`EcfReceivedItem`), `Count`, `HasMore`, `NextSince`.
- `EcfReceivedFile`: `Status`, `Message`, `Detail`, `IsTimeout`, `Content` (bytes), `FileName`, `ContentType`, `Text` (XML decodificado), `Candidates` (RNC emisores cuando es `Ambiguous`).
- Cada propiedad de los payloads internos lleva `[JsonProperty]` explícito (el ofuscador renombra miembros internos; ver 0.2.1).
- Versión 0.3.0 (cambia la DLL; `IEcfClient` gana miembros, así que quien lo implemente o lo simule debe agregarlos).
- Guías: sección nueva en `GUIA-DE-USO`, ejemplo corto en `GUIA-RAPIDA`, detalle técnico en `GUIA-TECNICA` (`.md`), más `CHANGELOG`; se regeneran los `.html` y `.pdf` y el paquete.

## Pruebas

- Mapper y PDF con los fixtures existentes (31, 32, 34, 41, 43): el PDF es válido y trae eNCF, emisor, total, QR y etiquetas; un documento de 80 ítems pasa a varias páginas y repite la cabecera de la tabla.
- Lógica pura (`ReceivedEcfLogic`): normalizar RNC, elegir candidato (único, reenvío, ambiguo, filtrado por RNC), limitar `limit`, resolver ambiente.
- Librería: URLs, interpretación de cada código HTTP, fallos de red y timeout, nombre de archivo, `NextSince`.
- Revisión visual: se renderiza a PNG y se revisa a mano antes de dar el diseño por bueno.

## Ramas y alcance

- Backend: `Staging` y se replica a `feature/emit-idempotente` (la librería depende de ese backend). Frontend: la rama que corresponda. Librería: `feature/emit-idempotente`. Todo local, sin push.
- Fuera de alcance: PDF de aprobaciones comerciales, enviar el PDF por correo, usar el formato HTML por tipo de cliente.
