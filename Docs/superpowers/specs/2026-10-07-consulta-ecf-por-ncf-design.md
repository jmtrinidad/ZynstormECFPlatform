# Consulta de e-CF por eNCF

Fecha: 2026-10-07

## Problema

Cuando un envío a `POST v1/Ecf/emit` termina en timeout del lado del integrador, el
comprobante pudo haber llegado y haber sido aceptado por la DGII. El integrador
(EasyInvoice) no recibe el `TrackId`, el código de seguridad ni el QR. Al reenviar, la
plataforma crea un documento nuevo (no hay idempotencia) y la DGII responde 75/1209
("secuencia ya utilizada"). El comprobante aceptado queda en el integrador como "No
Validado" y sin datos para imprimirlo.

Hoy no existe forma de preguntarle a la plataforma "¿qué pasó con este eNCF?":
`GET v1/Ecf/status/{trackId}` exige el `TrackId`, que es justo lo que se pierde.

## Alcance

Solo la plataforma: un endpoint de lectura que consulta por eNCF. El consumo desde
EasyInvoice (consultar antes de reenviar desde Consultas y desde el job) es otra tanda.

Fuera de alcance: idempotencia de `emit`, consultar a la DGII en vivo, cambios de esquema.

## Endpoint

`GET v1/Ecf/by-ncf/{eNcf}` en `EcfController` (hereda `[ApiKeyAuth]`).

- **Autenticación y aislamiento:** filtra siempre por el `ClientId` que `ApiKeyAuth`
  deja en `HttpContext.Items["ClientId"]`. Si no hay `ClientId` (petición autenticada
  por JWT y no por API key) responde 403. Nunca devuelve documentos de otro cliente.
- **Entrada:** el eNCF se normaliza (trim, mayúsculas) y debe cumplir `E` + 12 dígitos;
  si no, 400.
- **No modifica nada y no llama a la DGII.**

### Respuestas

- `200`:

  ```json
  {
    "found": true,
    "eNcf": "E320000000098",
    "state": "Accepted",
    "isUsable": true,
    "trackId": "…",
    "securityCode": "…",
    "signatureDate": "07-10-2026 09:01:23",
    "qrUrl": "https://…",
    "ecfDocumentId": 123,
    "total": 140.00,
    "issueDate": "2026-10-07T00:00:00Z",
    "attempts": 2,
    "message": ""
  }
  ```

  `state` ∈ `Accepted | AcceptedConditional | Pending | Rejected | Error | NotSent`.
  `isUsable` es true para `Accepted`, `AcceptedConditional` y `Pending` (el comprobante
  sirve aunque falte la confirmación final, igual que `EcfOutcome.Usable` de la librería).
  `attempts` es la cantidad de documentos de la plataforma con ese eNCF.
- `404` `{ "found": false, "message": … }` si la plataforma nunca recibió ese eNCF.
- `400` formato inválido; `403` sin API key.

## Servicio

`IEcfLookupService` / `EcfLookupService` en `ZynstormECFPlatform.Services/Production`,
registrado junto a los demás servicios de producción. Dependencias: `IEcfDocumentService`
(repositorio de `EcfDocument`), configuración (ambiente DGII para el QR).

### Selección del documento

Entre los `EcfDocument` del cliente con ese `Ncf` se elige uno por prioridad:
`Accepted` > `AcceptedConditional` > `Pending` > `Rejected` > `Error` > `NotSent`; a igual
prioridad, el más reciente. Así, en el caso de la factura 118 (uno aceptado y uno rechazado con 75)
gana el aceptado.

### Estado

Sale de `EcfStatusId`:

| Id | Estado de la plataforma | `state` |
|---|---|---|
| 10 | Accepted | `Accepted` |
| 7, 8, 9 | SendPending, Sending, Sent | `Pending` |
| 11 | Rejected (también Aceptado Condicional) | `AcceptedConditional` o `Rejected` |
| 3 | ValidationFailed | `Rejected` |
| 12 | Error | `Error` |
| 1, 2, 4, 5, 6, 13 | previos al envío o Cancelled | `NotSent` |

`NotSent` significa que el documento no llegó a salir hacia la DGII; no es utilizable y
reenviar es seguro. Se agrega a la lista de valores de `state`.

El id 11 se usa hoy tanto para `Rejected` como para `Aceptado Condicional`
(`MapDgiiStatusToEcfStatus`), así que para ese id se lee `estado` en el `ResponsePayload`
de la última transmisión: si es condicional → `AcceptedConditional`, si no → `Rejected`.

### Datos del comprobante

- `trackId`: de la última transmisión con `Success` del documento elegido.
- `securityCode` y `signatureDate`: del XML firmado guardado (`EcfXmlDocument.XmlSigned`,
  `CodigoSeguridad` y `FechaHoraFirma`), con los mismos extractores que usa
  `ReceivedEcfProductionService`.
- `qrUrl`: `EcfQrUrlBuilder.Build` con los datos del documento y el ambiente configurado
  (`EcfXmlValidation:TargetDgiiEnvironment`, igual que `ResolveTargetEnvironment`).

Si el XML firmado no existe o no se puede leer, se devuelve el estado sin
`securityCode`/`signatureDate`/`qrUrl` y con un `message` que lo explica. No es un 500.

## Pruebas

En `ZynstormECFPlatform.Tests/Production`, con la selección y el mapeo como funciones
estáticas puras (mismo estilo que `ClientInactiveResultTests`):

- Varios documentos con el mismo eNCF: gana el aceptado sobre el rechazado.
- Mapeo de estados, incluida la ambigüedad del id 11.
- Pendiente → `isUsable` true; rechazado/error → false.
- XML firmado ausente: devuelve estado sin QR.
- Aislamiento por cliente: un eNCF de otro cliente no se devuelve.
- Normalización y validación del formato del eNCF.

Verificación final: build de la solución.
