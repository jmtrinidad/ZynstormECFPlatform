# Re-encolar un comprobante atascado desde el listado de facturas

Fecha: 2026-10-13 · Rama: `feature/emit-idempotente` (backend) y `Staging` (frontend)

## Problema

Con el modo diferido, un comprobante puede quedar atascado sin que nadie lo resuelva:

1. **Error de transporte agotado.** `EcfTransmitJob` reintenta 3 veces (5 s, 30 s, 2 min) y, agotados,
   deja el documento en estado 12 (`Error`). En la lista de facturas ese estado se muestra como
   «rechazada», aunque la DGII nunca lo rechazó.
2. **Seguimiento agotado.** `EcfTrackingJob` consulta el estado a la DGII hasta 20 veces (unos 1 o
   2 minutos). Si la DGII sigue «En Proceso», el documento queda en estado 9 (`Sent`) con TrackId
   para siempre: `by-ncf` solo lee lo guardado y no vuelve a preguntar.
3. **Job perdido.** Un reinicio o una caída deja el documento en 7 (cola) u 8 (enviando) sin job.

Hoy solo soporte, a mano, puede resolverlo. Las guías de la librería mandan al integrador a avisar a
soporte si un `Pending` pasa de unos 5 minutos.

## Objetivo

Que el usuario pueda **re-encolar** desde el listado de facturas un comprobante atascado: reenviarlo
a la DGII o volver a consultar su estado, sin intervención de soporte.

## Alcance y restricciones

- Solo en **`feature/emit-idempotente`** (backend). Depende de `EcfTransmitJob` y del envío
  diferido, que no existen en `Staging` ni en `main`. No se replica a `Staging` (backend).
- El **frontend** va en `Staging`. Si la plataforma no soporta la función, la lista no trae el campo
  nuevo y el botón no aparece.
- No cambia el comportamiento de `emit`, ni de la idempotencia, ni del modo síncrono.
- No hay migración: la elegibilidad se calcula con lo que ya se guarda.
- Permisos: igual que la lista de facturas. Un **SA** puede sobre cualquier cliente; los demás
  usuarios, solo sobre comprobantes de clientes a los que están vinculados (`UserClients`).

## Elegibilidad

Función pura `EcfRequeuePolicy.Evaluate(snapshot, nowUtc)` → `EcfRequeueAction?` (`Transmit`,
`Track` o ninguna). El servidor la calcula y la devuelve en la lista y en el detalle; la pantalla no
la reimplementa, porque el estado visible no basta (el `Error` se ve «rechazada»).

Un comprobante **no califica** si no tiene XML firmado, o si está en 3 (`ValidationFailed`),
10 (`Accepted`), 11 (`Rejected`, rechazo real de la DGII) o 13 (`Cancelled`), o si cambió hace menos
de 60 segundos (`LastUpdateUtc`/`RegisteredAt`): puede haber un job en curso.

| Condición | Acción | Qué significa |
|---|---|---|
| Sin TrackId en ninguna `EcfTransmission` y estado 12 | `Transmit` | Agotó los reintentos. |
| Sin TrackId y estado 7 u 8 con más de 5 min sin cambios (`EcfIdempotency:StaleInFlightMinutes`) | `Transmit` | El job se perdió. |
| Con TrackId, estado 9 y más de 2 min sin cambios | `Track` | El seguimiento se agotó; la DGII ya lo tiene. |
| Cualquier otra | ninguna | |

## Acción `Transmit` (reenviar)

1. Transición **condicional** a estado 7 (`UPDATE … WHERE EcfDocumentId = @id AND EcfStatusId = @actual`):
   si otra petición ya lo movió, responde 409 y no encola. Así dos clics no encolan dos envíos.
2. Encola `EcfTransmitJob.Execute(ecfDocumentId, attemptNumber, environment)` con
   `attemptNumber` = cantidad de `EcfTransmission` del documento + 1. El job ya exige estado 7 y lo
   pasa a 8, lo que evita transmisiones dobles aunque se encolen dos veces.
3. Antes de volver a transmitir un documento que ya tuvo intentos sin TrackId, el job le **pregunta
   a la DGII si ya lo recibió** (sección «Recuperar un TrackId perdido»). Si lo tiene, guarda su
   TrackId y pasa al seguimiento sin retransmitir; si no, transmite como siempre.

El ambiente de la DGII no se guarda en el documento; lo impone `EcfXmlValidation:TargetDgiiEnvironment`
(`Production` en producción, `Test` en Staging), de modo que se pasa el valor por defecto.

## Acción `Track` (reconsultar a la DGII)

No retransmite. `EcfRetrackJob.Execute(ecfDocumentId, attemptNumber)` (cola `ecf-transmit`,
`[AutomaticRetry(Attempts = 0)]`):

1. Recarga el documento, el TrackId (de su `EcfTransmission` más reciente con TrackId) y el
   certificado **desde la base de datos**. No recibe el certificado como argumento: Hangfire guarda
   los argumentos en claro.
2. Pide token, consulta el estado por TrackId y lo guarda con la misma lógica de
   `EcfTrackingJob` (estado, historial, QR si queda aceptado).
3. Si sigue pendiente y `attemptNumber < 20`, se reprograma a sí mismo a los 3 s; igual que el
   seguimiento actual, pero sin llevar el certificado en los argumentos.

## Recuperar un TrackId perdido (Consulta de TrackId de la DGII)

**Problema:** la DGII recibió el comprobante pero la respuesta con el TrackId se perdió entre la DGII
y la plataforma. Hoy el documento queda sin TrackId, y cada reenvío recibe 1209/75: la regla
existente lo deja `Pending` con el aviso «falta confirmar su TrackId» y solo soporte lo resuelve.

**Solución:** la DGII ofrece el servicio *Consulta de TrackId e-CF* («Descripción técnica de
facturación electrónica», sección 24): `GET {base}/api/trackids/consulta?rncemisor=…&encf=…` con el
token, que devuelve `trackId`, `estado` y `fechaRecepcion` de lo recibido para ese eNCF (puede haber
varios si se remitió el mismo eNCF más de una vez). Requiere que el usuario del token esté delegado por
el emisor, igual que el resto de servicios.

- `IDgiiTransmissionService.GetTrackIdsAsync(environment, token, rncEmisor, eNcf, cancellationToken)`
  → `DgiiTrackIdsResult { Available, Entries, Error }`, con `DgiiTrackIdEntry { TrackId, Estado,
  FechaRecepcion }`. La lectura acepta una lista o un objeto suelto (la guía de la DGII muestra uno).
- Nueva configuración `DgiiUrls:{ambiente}:ConsultaTrackIds`: `Test` →
  `https://ecf.dgii.gov.do/TesteCF/ConsultaTrackIds`, `Production` →
  `https://ecf.dgii.gov.do/ecf/ConsultaTrackIds`. **`CerteCF` no la tiene** (la DGII no la publica en
  certificación) y con el validador interno (`UseInternalValidator`) tampoco existe: en esos casos
  `Available = false` y todo se comporta como hoy.
- `DgiiTrackIdPicker.Pick(entries)` (función pura): descarta entradas sin TrackId o con estado «No
  encontrado»; prefiere Aceptado o Aceptado Condicional, luego En proceso y por último Rechazado, y
  dentro de cada grupo el de `fechaRecepcion` más reciente. Devuelve la entrada elegida o `null`.

**Dónde se usa:**

1. En `TransmitDeferredAsync`, cuando el documento ya tuvo intentos sin TrackId
   (`earlierAttemptsWithoutTrackId`: cubre los reintentos automáticos y la acción `Transmit`), **antes**
   de transmitir. Si hay una entrada, se guarda una `EcfTransmission` con ese TrackId (documento a
   estado 9) y se continúa con la consulta de estado de ese TrackId, sin retransmitir. Si no hay
   entrada, o no está disponible, se transmite como hoy.
2. En la rama existente del 1209/75 de un reintento propio sin TrackId: antes de dejar el aviso, se
   intenta recuperar el TrackId; solo si no se logra se conserva el aviso actual.

Con esto desaparece el caso que exigía soporte, salvo en `CerteCF` y con el validador interno, donde
no hay forma de consultarlo.

## API

`POST v1/EcfDocument/guid/{guid}/requeue` · `[Authorize]`.

- 404 si no existe o el usuario no tiene acceso; 409 si ya no califica (cambió de estado o fue
  re-encolado hace poco); 200 con `{ action: "transmit" | "track", message }`.
- Deja rastro: una fila de `EcfStatusHistory` y una de `SystemLog` («Re-encolado por {usuario}:
  reenviar a la DGII» / «reconsultar estado a la DGII»).

La lista (`GET v1/EcfDocument`) y el detalle (`GET v1/EcfDocument/guid/{guid}`) agregan
`requeueAction` (`"transmit"`, `"track"` o `null`), calculado con la misma función pura. En la lista
se resuelve con subconsultas `EXISTS` (XML firmado y TrackId) sobre la página pedida.

## Pantalla (`app/facturas/page.tsx`)

- En cada fila, junto a ver y descargar XML, un botón con icono de reintento, visible solo si
  `requeueAction` no es `null`. También en el diálogo de detalle.
- Pide confirmación explicando qué hará: «Volver a enviar a la DGII» o «Volver a consultar el
  estado a la DGII (el comprobante ya está en la DGII; no se retransmite)».
- Al terminar, refresca la fila y muestra el mensaje del servidor. Los errores 404/409 se muestran
  tal cual.
- El estado visible no cambia de significado: un `Error` sigue mostrándose como hoy; la diferencia es
  que ahora tiene botón.

## Pruebas

- `EcfRequeuePolicy`: cada fila de la tabla de elegibilidad, el umbral de 60 s, los de 5 min y 2 min,
  con y sin TrackId, con y sin XML, y los estados excluidos (3, 10, 11, 13).
- Cálculo de `attemptNumber` y traducción de la decisión a la respuesta del endpoint.
- `DgiiTrackIdPicker`: lista vacía, solo «No encontrado», prioridad aceptado > en proceso > rechazado,
  desempate por fecha más reciente, y lectura de lista, de objeto suelto y de respuesta vacía.
- `GetTrackIdsAsync` con un `HttpMessageHandler` de prueba: arma la URL con `rncemisor` y `encf`,
  manda el token, y devuelve `Available = false` sin configuración (`CerteCF`) o con validador interno.
- `EcfRetrackJob`: reprogramación mientras siga pendiente y tope de 20 intentos (lógica pura
  extraída, sin Hangfire).
- La transición condicional, el job y el endpoint se verifican con el build y a mano en Staging (el
  repo no tiene base de datos de prueba).
- Frontend: `tsc` sin errores nuevos y verificación visual del botón en una factura en `Error`.

## Fuera de alcance

- Que `by-ncf` consulte a la DGII por su cuenta cuando un documento lleva mucho tiempo pendiente.
- Re-encolar en lote o desde la librería.
- Recuperar el TrackId en `CerteCF` o con el validador interno: no hay consulta disponible ahí.
- Cambiar el texto de las guías de la librería («avise a soporte»); se ajusta cuando esto se publique.

## Despliegue

Plataforma (`feature/emit-idempotente`) primero; el frontend después. Con el frontend nuevo contra una
plataforma sin esta función, el botón no aparece.
