# `emit` idempotente y diferido (plataforma + librería)

Fecha: 2026-10-07

Cubre dos repos que comparten un solo contrato:
`ZynstormECFPlatform` (este repo) y `C:\Projects\Zynstorm.DGII.ECF` (la DLL que usan
EasyInvoice y MechanicalServ). Cada parte tendrá su propio plan de implementación.

## Problema

1. **Doble envío.** La plataforma no es idempotente: cada `POST v1/Ecf/emit` crea un
   `EcfDocument` nuevo y lo transmite a la DGII aunque ese eNCF ya exista. Si el integrador
   pierde la respuesta (timeout de 20-30 s) y reenvía, la DGII responde 1209/75 («secuencia ya
   utilizada») y el comprobante aceptado queda marcado como fallido en el integrador. Pasó
   con la factura 118 (E320000000098): dos filas, una aceptada y una rechazada.
2. **Entrega lenta.** `emit` mantiene la petición HTTP abierta hasta que la DGII da el estado
   final (`FinalStatusTimeoutSeconds` = 60 s). El código ya es `async/await`; lo síncrono es el
   contrato. El QR, el código de seguridad y la fecha de firma ya existen al firmar, pero el
   integrador solo los recibe si todo el recorrido termina a tiempo.
3. **Cada proyecto tendría que implementar su propia protección**, y hoy ninguno puede:
   no tienen forma de preguntarle a la plataforma qué pasó.

## Decisiones

- La **plataforma** es la autoridad de la idempotencia (es la única con base de datos). La
  **librería** aporta lo que puede hacer sin estado.
- La identidad de un documento es `(ClientId, eNCF)`, derivada del cuerpo del `emit`
  (RNC del emisor + eNCF). No hay header de idempotencia.
- **Modo diferido opt-in:** `emit` sigue síncrono para quien no pida `deferred=true`. La DLL
  nueva lo pide por defecto. Cuando los integradores ya consulten el estado final, se invierte
  el predeterminado en un cambio pequeño. Así EasyInvoice y MechanicalServ no cambian de
  comportamiento hasta actualizar la DLL.
- Se conserva el rastro completo de cada intento (logs), también de los reenvíos ignorados.
- El contenido de un reenvío **no se compara** con el original: si el documento ya está
  aceptado o en proceso, se devuelve lo guardado.

Fuera de alcance: cambios en EasyInvoice/MechanicalServ más allá de copiar la DLL nueva,
webhooks, y arreglar que el cliente HTTP de la librería trate el 504 como error de contrato.

## Plataforma

### 1. Decisión por documento existente

Al recibir `emit` (ya validado el DTO y resuelto el cliente), se buscan los `EcfDocument` no
borrados con ese `(ClientId, eNCF)` y se decide con una función pura
`EcfEmitDecision.Decide(existing, nowUtc, staleAfter)`:

| Documento existente (el que corresponda según prioridad de `EcfLookupLogic.PickBest`) | Decisión |
|---|---|
| `Accepted` o `AcceptedConditional` | `Replay`: no transmite; devuelve lo guardado |
| `Pending` (ids 7, 8, 9) o en vuelo (ids 1, 2, 4, 5, 6) con `RegisteredAt`/`LastUpdateUtc` más reciente que `staleAfter` | `Replay`: no transmite; devuelve el estado actual |
| `Rejected`, `Error`, `NotSent`/`ValidationFailed` (3), o en vuelo hace más de `staleAfter` | `Retry`: nuevo intento. Se crea un `EcfDocument` nuevo; los anteriores quedan como historial |
| Ninguno | `Create` |

- `staleAfter` = `EcfIdempotency:StaleInFlightMinutes`, 5 por defecto.
- Un documento `Pending` con `staleAfter` vencido sigue siendo `Replay` si tiene TrackId: la
  DGII lo tiene y el job de seguimiento lo resolverá.

### 2. Candado

La decisión y el «reclamo» del documento (crear o pasar el existente a estado 1/2) ocurren
bajo `pg_advisory_xact_lock` de Postgres, con una clave derivada de `(ClientId, eNCF)`, dentro
de una transacción corta que termina antes de firmar y transmitir. Una petición concurrente
con el mismo eNCF espera el candado y luego ve el documento ya reclamado → `Replay`. No se usa
un índice único porque producción ya tiene eNCF duplicados.

### 3. Replay

Arma la respuesta con `EcfLookupLogic.BuildResponse` (ya existente) y la traduce al DTO de
`emit` con `Replayed = true`, `Attempt` = intentos del documento, y los mismos campos de
siempre (`Success` solo si está aceptado, `IsPending`, `IsAcceptedConditional`, `TrackId`,
`SecurityCode`, `SignatureDate`, `QrUrl`). Deja rastro: una fila de `SystemLog` y una de
`EcfStatusHistory` («Reenvío ignorado: el documento ya está en estado X»). HTTP 200 siempre
(también para `Pending`: la librería solo trata 2xx como éxito).

### 4. Retry

Crea un `EcfDocument` nuevo, como hoy, con el contenido corregido. Los documentos anteriores
con ese eNCF no se tocan: son el historial de intentos, y `attempt` / `attempts` es la cantidad
de documentos con ese eNCF (la misma definición que usa `by-ncf`). El consumo del cliente se
cuenta una sola vez porque solo se registra al aceptar, y un eNCF aceptado nunca llega a
`Retry`. Dentro de un documento, `EcfTransmission.AttemptNumber` solo crece con los reintentos
de transporte de la sección 5b.

### 5. Modo diferido (`?deferred=true`)

1. Se prepara y firma el documento (igual que hoy), se guarda XML y estado
   `SendPending` (7), y se calculan QR, código de seguridad y fecha de firma.
2. Se encola `EcfTransmitJob.Execute(ecfDocumentId)` en Hangfire, `[AutomaticRetry(Attempts = 0)]`.
3. `emit` responde de inmediato (HTTP 200): `Success=false`, `IsPending=true`,
   `Message` = «Firmado; transmisión a la DGII en curso», con `SecurityCode`, `SignatureDate` y
   `QrUrl` ya completos. `TrackId` va vacío: aún no existe.
4. **`EcfTransmitJob`:** recarga el documento, el XML firmado y el certificado **desde la base
   de datos** (no recibe el certificado ni su contraseña como argumentos, a diferencia de
   `EcfTrackingJob`, cuyos argumentos Hangfire guarda en claro). Antes de transmitir, pasa el
   documento de 7 a 8 de forma condicional; si ya no está en 7, termina sin hacer nada (evita
   que dos ejecuciones transmitan). Luego ejecuta el mismo método de transmisión y seguimiento
   que usa el modo síncrono, que cubre las dos ramas actuales: producción (token + envío +
   espera de estado final + `EcfTrackingJob` si sigue pendiente) y el validador interno de
   staging.
5. Si el job se cae a medias, el documento queda en estado 8 y, pasado `staleAfter`, la regla
   de la sección 1 permite reenviar.

El modo síncrono pasa por el mismo método de transmisión; solo cambia quién lo espera.

### 5b. Reintento de la transmisión ante timeout

Dentro de `EcfTransmitJob`, y solo para fallos de **transporte** hacia la DGII (timeout,
red caída, HTTP 5xx; nunca un rechazo de la DGII ni «En Proceso», que ya resuelve
`EcfTrackingJob`):

- Se reprograma el mismo job con espera creciente: 5 s, 30 s y 2 min (3 reintentos como
  máximo; configurable en `EcfTransmit:RetryDelaysSeconds`).
- Entre intentos el documento vuelve a estado 7 (`SendPending`) con el motivo en el historial;
  cada intento agrega una fila `EcfTransmission` (`AttemptNumber` +1) y un log.
- Agotados los reintentos, el documento queda en estado 12 (`Error`) y pasa a ser `Retry`
  permitido por la sección 1.
- **El TrackId se guarda en cuanto la DGII responde la recepción.** La DGII devuelve un
  TrackId por cada documento que recibe. Hoy la plataforma lo persiste recién después de esperar
  el estado final (hasta 60 s), así que una petición cortada en esa espera lo pierde. Desde
  ahora, apenas vuelve `SendEcfAsync` con TrackId, se inserta la `EcfTransmission` (estado 9,
  `Sent`) y el documento pasa a 9; el estado final solo la actualiza. Así `by-ncf` y el job de
  seguimiento siempre tienen el TrackId de lo que la DGII ya recibió.
- **Ventana de riesgo que queda:** que la respuesta de la recepción se pierda entre la DGII y
  la plataforma (la DGII recibió pero no llegó el TrackId). En ese caso el reintento del job
  recibe 1209/75 por culpa del primer envío, y el TrackId original no se conoce. Un 1209/75 en
  un reintento **propio** del mismo documento (con envíos previos sin TrackId) no se trata como
  secuencia quemada: el documento queda `Pending` con el aviso «la DGII ya recibió este
  comprobante; falta confirmar su TrackId», sin pedir un NCF nuevo. Un 1209/75 en el primer
  envío de un documento sin intentos previos sigue siendo secuencia quemada.
- `SendEcfAsync` hoy solo captura `HttpRequestException`: un timeout (`TaskCanceledException`)
  se escapa al `catch` general de `ProcessAsync` y deja el documento en `Error` sin reintento.
  El job lo captura y lo trata como fallo de transporte.

### 6. QR en toda respuesta posterior a la firma

`SecurityCode`, `SignatureDate` y `QrUrl` viajan también cuando la DGII rechaza o hay error de
transmisión (hoy el resultado se arma igual, pero el integrador solo los aprovecha si fue
aceptado). Antes de la firma (errores de DTO o de XSD) no existen y van vacíos.

### 7. Consulta del estado final

Se mantiene `GET v1/Ecf/by-ncf/{eNcf}` (ya implementado). Es lo que usa el integrador, o la
librería, para conocer el resultado después de una respuesta diferida. Si la DGII rechaza, el
integrador corrige y reenvía el mismo eNCF: la sección 1 lo permite.

### Contrato `emit`

- Petición: `POST v1/Ecf/emit?environment=…&deferred=true|false` (por defecto `false`).
- Respuesta: los campos actuales más `replayed` (bool) y `attempt` (int).

## Rendimiento

Objetivo: que la respuesta de `emit?deferred=true` tarde una fracción de segundo (meta: p95
menor a 1 s, medida en staging; se confirma o se ajusta con la medición de la fase 0) y que el
envío a la DGII deje de ocupar peticiones HTTP.

Diagnóstico del código actual: el token de la DGII ya se cachea 55 min con candado por RNC y
los esquemas XSD ya se compilan una sola vez. Lo que domina el tiempo es la espera de la DGII
(envío más sondeo cada 500 ms hasta 60 s), que el modo diferido saca de la respuesta. Lo que
queda en la respuesta son unas diez consultas a la base de datos antes de firmar, la firma y
el XSD.

Medidas, en este orden:

0. **Medir primero.** Un cronómetro por fase de `emit` (consultas previas, generación + XSD,
   firma, guardado, transmisión, espera del estado final) que se escribe en el log estructurado
   con el eNCF y el modo. Las fases siguientes se verifican contra estos números, no a ojo.
1. **Modo diferido** (sección anterior): quita de la respuesta el envío y la espera de la DGII.
2. **Menos viajes a la base de datos antes de firmar.** Moneda DOP, tipo de e-CF y los datos
   del cliente (cliente, API key, sucursal principal) casi nunca cambian: se cachean en
   memoria con `ICacheService` y TTL corto (5 min) y se invalidan al editar el cliente.
   **No** se cachea el certificado ni su contraseña descifrada: esos se leen y descifran en
   cada firma.
3. **Menos `SaveChanges`.** Los historiales y logs que hoy se agregan por separado se
   acumulan y se guardan junto con el cambio de estado, en un solo viaje por fase.
4. **Cola propia para transmitir.** `EcfTransmitJob` va en una cola `ecf-transmit` con sus
   propios workers, para que el envío a la DGII no compita con los jobs de mantenimiento,
   reportes y recordatorios.
5. **Token precalentado.** En modo diferido el token se pide dentro del job, no en la
   respuesta; si el caché está frío, solo retrasa la transmisión, no la entrega de la factura.

No se hace en esta tanda: paralelizar consultas sobre un mismo `DbContext` (no es seguro) ni
cambiar el motor de firma o de validación XSD, salvo que la fase 0 demuestre que son el cuello
de botella.

## Librería (`Zynstorm.DGII.ECF`)

- **Un solo envío en vuelo por eNCF dentro del proceso.** `EcfClient.EmitAsync` guarda en un
  `ConcurrentDictionary<string, Task<EcfOutcome>>` la tarea en curso por clave
  `RNCEmisor|eNCF`. Dos llamadas simultáneas al mismo comprobante hacen un solo POST y
  comparten el resultado. La clave se libera siempre al terminar, con éxito o error, para no
  bloquear un reenvío legítimo. La tarea compartida usa el `CancellationToken` de la primera
  llamada (se documenta).
- **Reintento único ante timeout contra la plataforma.** Si `EmitAsync` termina en timeout
  (`TransportError` por tiempo agotado), la librería repite el envío **una vez**, tras una
  espera corta (`EcfOptions.TimeoutRetryDelay`, 2 s). Es seguro porque la plataforma es
  idempotente: si el primer envío llegó, el segundo devuelve lo guardado (`Replayed`). Se puede
  apagar con `EcfOptions.RetryOnTimeout = false`. Otros errores de red no se reintentan.
- **`WaitForFinalAsync(string eNcf, TimeSpan maxWait)`:** consulta `by-ncf` con espera
  creciente (1 s, 2 s, 4 s… hasta 15 s) hasta que el estado deje de ser `Pending` o se agote
  `maxWait`; devuelve el `EcfOutcome` final. La librería **no crea hilos ni colas propias**: el
  integrador lo ejecuta en su propio segundo plano y solo avisa al usuario si el resultado
  final es `Rejected` o un error definitivo. Sin Hangfire ni dependencias nuevas.
- **No se agrega Hangfire a la librería:** no puede calcular el QR sin la firma de la
  plataforma, exigiría almacenamiento propio en cada integrador y rompería la promesa de una
  DLL sin dependencias desde .NET Framework 4.5.2. La cola vive en la plataforma.
- **`EcfOptions.DeliveryMode`:** `Deferred` (predeterminado, agrega `deferred=true`) o
  `WaitForDgii` (comportamiento actual).
- **`EcfEmitResponse.Replayed` / `Attempt`** y **`EcfOutcome.Replayed`**: el integrador sabe
  que la plataforma devolvió lo guardado.
- **`GetByNcfAsync(string eNcf)` / `GetByNcf`:** llama a `Ecf/by-ncf/{eNcf}` y devuelve un
  `EcfOutcome`: `Accepted`→`Accepted`, `AcceptedConditional`→`AcceptedConditional`,
  `Pending`→`Pending`, `Rejected`→`Rejected`, `Error` y `NotSent`→`TransportError`
  (reenviar tal cual). Devuelve `null` si la plataforma no conoce ese eNCF (404).
- **Documentación:** `README.md`, `docs/GUIA-DE-USO.md`, `docs/GUIA-TECNICA.md` y
  `docs/CHANGELOG.md`; versión menor según `docs/VERSIONADO.md` (funcionalidad compatible).
  Regenerar la DLL ofuscada.
- Un `Pending` con QR en modo diferido **no es aceptado**: `outcome.Accepted` sigue en false.
  El integrador puede imprimir con el QR, pero no debe marcar el comprobante como validado
  hasta consultar el estado final.

## Pruebas

**Plataforma** (`ZynstormECFPlatform.Tests`):
- `EcfEmitDecision.Decide`: cada fila de la tabla, incluido el umbral `staleAfter`, varios
  documentos con el mismo eNCF y el caso de la 118 (aceptado + rechazado → `Replay`).
- Política de reintentos de la transmisión: qué fallos se reintentan (transporte) y cuáles no
  (rechazo, «En Proceso»), las esperas 5 s / 30 s / 2 min, y el estado final tras agotarlos.
- Traducción de `EcfLookupResponse` a la respuesta de `emit` con `Replayed`.
- Cálculo de `AttemptNumber`.
- Caché de datos de referencia: se reutiliza dentro del TTL, se invalida al editar el cliente,
  y nunca contiene el certificado.
- El cronómetro por fase registra todas las fases y no altera el resultado de `emit`.
- El candado, la transacción y el job se verifican con el build y una prueba manual en
  staging (el repo no tiene base de datos de prueba).

**Librería** (`Zynstorm.DGII.ECF.Tests`, con `StubHandler`):
- Dos `EmitAsync` simultáneos del mismo eNCF producen un solo POST y el mismo resultado.
- Tras un fallo o timeout, la clave se libera y un reenvío vuelve a enviar.
- eNCF distintos no se bloquean entre sí.
- `DeliveryMode` agrega o no `deferred=true` a la URL.
- Un timeout se reintenta una sola vez y el segundo resultado es el que se devuelve; con
  `RetryOnTimeout = false` no se reintenta; un error que no es timeout no se reintenta.
- `WaitForFinalAsync` termina al salir de `Pending`, respeta `maxWait` y no crea hilos.
- `Replayed` se lee de la respuesta; `GetByNcf` mapea cada estado y devuelve `null` en 404.

## Orden de despliegue

1. Plataforma: idempotencia + modo diferido. Con la DLL actual, `emit` sigue síncrono y ya
   no duplica envíos.
2. Probar en staging (`ecfstaging.zynstorm.com`): reenvío del mismo eNCF, concurrencia, modo
   diferido, job caído, y comparar los tiempos por fase antes y después.
3. Librería: versión nueva, DLL ofuscada, y copiarla a EasyInvoice y MechanicalServ.
4. En otra tanda: que los integradores consulten el estado final y se invierta el
   predeterminado.
