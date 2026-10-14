# XML firmado por comprobante y por rango, y vista previa del PDF de recibidos

Fecha: 2026-10-14. Estado: aprobado por el usuario. Extiende [2026-10-14-recibidos-pdf-design.md](2026-10-14-recibidos-pdf-design.md).

## Objetivo

1. Endpoints para descargar el **XML firmado de los comprobantes emitidos**, por comprobante y por rango de fechas. Con más de un comprobante en el rango, la respuesta es un `.zip` con todos los XML firmados.
2. Los consumen el **panel** (con sesión) y la **API con API key**, y por tanto la **librería** `Zynstorm.DGII.ECF` y sus guías.
3. Botón de **vista previa del PDF** de los e-CF recibidos en el panel.

## Contexto

- El XML firmado de lo emitido está en `EcfXmlDocument.XmlSigned` (varias filas posibles por documento; vale la de mayor id con texto).
- Un mismo eNCF puede tener varios `EcfDocument` (reintentos). `EcfLookupLogic.ResolveState`, `IsUsable` y `PickBest` ya deciden cuál es el bueno (lo usa `Ecf/by-ncf`).
- La pestaña Enviadas filtra por **fecha de registro en hora de RD** (`ApplyCommonFilters`: `RegisteredAt` entre `fromDate.ConvertDominicanLocalToUtc()` y `toDate + 1 día`), con cliente seleccionable (o «todos»).
- `ApiKeyAuth` pone `HttpContext.Items["ClientId"]`; sin él los endpoints de consulta responden 403.
- Los recibidos y la librería 0.3.0 se implementaron en la misma fecha y **no se han distribuido**, así que esto entra en la 0.3.0.

## Diseño

### Servicio `ISignedXmlExportService` (Services/Production)

Una sola implementación para la API y el panel.

| Método | Qué hace |
|---|---|
| `GetByNcfAsync(clientId, eNcf)` | Documentos del cliente con ese eNCF; descarta los no válidos; elige el mejor con `PickBest`; devuelve su XML firmado. |
| `GetForDocumentAsync(ecfDocumentId)` | El XML firmado de ese documento exacto, en cualquier estado (lo usa el panel por fila). |
| `GetRangeAsync(clientId, fromDate, toDate)` | Documentos del cliente con `RegisteredAt` dentro del rango (días de RD, ambos inclusivos); solo válidos; **uno por eNCF** (el mejor); solo con XML firmado. |

- «Válido» = `EcfLookupLogic.IsUsable`: Aceptado, Aceptado condicional o En proceso. Rechazados, con error y no enviados no entran en el rango ni en `GetByNcfAsync`.
- Tope **2000** comprobantes por petición; si se pasa, `TooMany` con el total.
- Orden de los XML en el zip: por eNCF. Se entregan los **bytes exactos** de `XmlSigned` en UTF-8 sin BOM: reformatear invalidaría la firma.
- Resultado: `Ok(file)`, `NotFound(message)` o `TooMany(total, message)`. `file` = bytes, nombre, tipo y cantidad.
- Un solo comprobante → `{eNcf}.xml` (`application/xml`). Más de uno → `XML_firmados_{rnc}_{yyyyMMdd}_{yyyyMMdd}.zip` (`application/zip`) con una entrada `{eNcf}.xml` por comprobante. Cero → `NotFound`.

### API con API key (`EcfController`)

| Endpoint | Respuesta |
|---|---|
| `GET Ecf/signed-xml/{eNcf}` | `200` XML firmado, `X-Ecf-Count: 1` |
| `GET Ecf/signed-xml?from=yyyy-MM-dd&to=yyyy-MM-dd` | `200` XML (si hay 1) o ZIP (si hay más), con `X-Ecf-Count` |

- Códigos: 403 sin API key de cliente; 400 eNCF o fechas inválidas, `to < from`, o `TooMany` (`{ found:false, tooMany:true, total, message }`); 404 si no hay nada válido.
- Las fechas son `yyyy-MM-dd` estricto, en días de RD.

### Panel (`EcfDocumentController`, `[Authorize]`)

| Endpoint | Respuesta |
|---|---|
| `GET EcfDocument/guid/{guid}/signed-xml` | XML firmado de ese documento (cualquier estado); 404 si no existe, no es del usuario o no tiene firma |
| `GET EcfDocument/signed-xml?clientId=&fromDate=&toDate=` | Igual que el endpoint de la API; `clientId` obligatorio |

- Mismos permisos que la lista: un usuario que no es SA solo ve clientes suyos (`Client.UserClients`); un cliente ajeno responde 404.

### Panel (pantalla)

- Enviadas: el botón XML de cada fila usa el endpoint por comprobante. Un botón nuevo «XML firmados» junto a las fechas descarga el XML o el ZIP del cliente y las fechas seleccionadas; está deshabilitado mientras el cliente esté en «Todos».
- Recibidas: botón de vista previa (ícono distinto del ojo de «Ver XML») que abre un diálogo con el PDF embebido (`<iframe>` sobre un blob) y un botón «Descargar PDF». Reutiliza `GET Fe/client/{id}/received-files/{id}/pdf`; sin backend nuevo.

### Librería

```csharp
Task<EcfFile> GetSignedXmlAsync(string eNcf, CancellationToken ct = default);
Task<EcfFile> GetSignedXmlRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default);
```

- Con sus versiones síncronas. Devuelven el mismo tipo que el PDF y el XML de recibidos.
- Para que el nombre sirva a los cuatro métodos, se renombran antes de publicar: `EcfReceivedFile` → `EcfFile` y `EcfReceivedStatus` → `EcfQueryStatus`. `EcfFile` gana `Count` (cuántos comprobantes trae) e `IsZip`; el estado gana `TooManyResults`.
- `Count` sale del header `X-Ecf-Count`; `IsZip`, del tipo de contenido o la extensión.
- `GetSignedXmlRange` usa solo la parte de fecha (día de RD) de los argumentos.
- Guías (`.md`, `.html`, `.pdf`), README, CHANGELOG (dentro de 0.3.0) y paquete se actualizan.

## Pruebas

- Lógica pura (`SignedXmlExportLogic`): rango de fechas de RD a UTC, selección del mejor por eNCF solo con válidos, armado del XML único y del ZIP (entradas, nombres y bytes exactos), fechas estrictas.
- Librería: rutas, un solo XML, ZIP, `X-Ecf-Count`, 404, 400, `TooManyResults`, red y timeout, y los `[JsonProperty]` de los payloads.
- Panel: `tsc`/eslint sin errores nuevos; la vista previa y el ZIP se prueban a mano en Staging.

## Ramas y alcance

- Backend: `Staging` y `feature/emit-idempotente`. Panel: `Staging`. Librería: `feature/emit-idempotente`. Todo local, sin push.
- Fuera de alcance: XML de recibidos por rango, rangos de varios clientes en un solo ZIP, vista previa de las enviadas, paginación del ZIP.
