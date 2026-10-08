# Paso 4 (Simulación e-CF): reenviar un tipo o un comprobante

Fecha: 2026-10-08

## Problema

Hoy "Iniciar Simulación de Negocio" borra todos los `CertificationDocument` del proceso y vuelve a
enviar los 29 comprobantes desde cero. Si un solo tipo falla (o hay que repetir un comprobante),
hay que reiniciar todo.

## Objetivo

Desde la pantalla del Paso 4:

1. **Reenviar tipo**: en cada tarjeta, reenviar todos los comprobantes de ese grupo.
2. **Reenviar comprobante**: en cada fila del Historial de Transmisiones, reenviar ese comprobante.

Solo se envía lo seleccionado; todo lo demás ya aceptado queda intacto. No continúa con otros tipos.

## Restricción

**No se modifica nada fuera de certificación** (para no afectar producción). Archivos permitidos:

- `ZynstormECFPlatform.Services/Certification/OldSimulation/*` (servicio e interfaz)
- `ZynstormECFPlatform.Dtos/CertificationJobStatusDto.cs` (DTO solo de certificación; campo opcional)
- `ZynstormECFPlatform.Web.Api/Controllers/CertificationController.cs`
- `ZynstormECFPlatform-FrontEnd/app/certificacion/page.tsx`
- Tests nuevos en `ZynstormECFPlatform.Tests/Certification/`

No se tocan `DgiiTransmissionService`, `DgiiTransmissionResult`, `EcfRequestDtos`, generadores de
producción, ni entidades/migraciones.

## Grupos (una tarjeta = un grupo)

Mismos renglones de la matriz actual de `ProcessSimulacionEcfJobInternalAsync`:

| Grupo       | Tarjeta                   | Cant. | Notas                                        |
|-------------|---------------------------|-------|----------------------------------------------|
| `31`        | Factura de Crédito Fiscal | 4     |                                              |
| `33`        | Notas de Débito           | 1     | Referencia una 31 aceptada                   |
| `34`        | Notas de Crédito          | 2     | Referencia una 31 aceptada                   |
| `32-250K`   | Consumo (Individual)      | 2     | 32 con monto ≥ 250,000                       |
| `41`…`47`   | (cada tarjeta)            | 2     |                                              |
| `32-RFCE`   | Consumo (Resumen)         | 4     | Arrastra a `32-MANUAL`                       |
| `32-MANUAL` | Consumo (Manual)          | 4     | Sin botón propio; se regenera con `32-RFCE`  |

## Diseño

### Un solo motor con selección

`ProcessSimulacionEcfJobInternalAsync` recibe una selección:

- `All` (actual): borra todo y envía la matriz completa. Sin cambios de comportamiento.
- `Group(g)`: filtra la matriz al renglón `g` (y `32-MANUAL` si `g = 32-RFCE`).
- `Document(guid)`: un solo envío del grupo al que pertenece ese `CertificationDocument`.

El ciclo de envío (reintento por "secuencia ya utilizada", validación XSD, detención en cualquier
otro rechazo, persistencia, SignalR) es el mismo código para las tres selecciones.

### Qué se borra (reemplazar)

- `Group(g)`: al iniciar, se borran los `CertificationDocument` vigentes de ese grupo en el proceso
  (`32-RFCE` también borra los `32-MANUAL`). Luego se envían los N del grupo.
- `Document(guid)`: el registro viejo se borra **solo si el nuevo envío es aceptado**. Si el nuevo
  es rechazado, se conserva el viejo y el rechazo queda en el log del job. Si es un RFCE aceptado,
  se borra también el manual que usaba el eNCF del RFCE viejo y se genera el nuevo manual.
- Nunca se crea un `CertificationProcess` nuevo en un reenvío: se usa el proceso vigente. Si no
  existe, el reenvío se rechaza ("Primero ejecute la simulación completa").

Clasificación de un documento existente a su grupo: tipo por `EcfType.Code`; para 32,
`TrackId == "MANUAL"` → `32-MANUAL`, raíz `RFCE` en `XmlSent` → `32-RFCE`, si no → `32-250K`.

### Dependencias reconstruidas desde la base

- **31 aceptadas (para 33/34)**: si la lista en memoria está vacía, se carga de los
  `CertificationDocument` aceptados tipo 31 del proceso: eNCF, `FechaEmision` y `RNCComprador`
  del `XmlSent`; el DTO se arma desde la muestra 31 del tipo de negocio con el precio del primer
  item tomado del XML guardado. Si no hay ninguna → se detiene con
  "No hay facturas 31 aceptadas para referenciar".
- **RFCE aceptados (para manuales)**: solo hacen falta dentro del mismo job (`32-RFCE` siempre
  regenera sus manuales en el mismo envío), así que no se reconstruyen.

### Contadores y tarjetas

Las tarjetas se siguen alimentando de `SimulationStats`. En un reenvío, el job carga primero los
contadores vigentes desde la base (misma lógica que `GetLastSimulationResultsByClientAsync`) y
solo recalcula el grupo reenviado, para que las demás tarjetas no queden en 0.

### API

`POST v1/Certification/simulation/resend`

```json
{ "clientGuidId": "...", "businessTypeGuidId": "...", "group": "34", "documentGuidId": null }
```

Exactamente uno de `group` / `documentGuidId`. Devuelve `{ jobId }`; la UI sigue el progreso con el
SignalR y `simulation/job-status/{jobId}` existentes. Si el cliente tiene una simulación en curso →
`409`. Grupo inválido o documento de otro cliente → `400`.

`CertificationStepResultDto` gana `DocumentGuidId` (opcional) para que el historial sepa qué
documento reenviar.

### Frontend (`app/certificacion/page.tsx`)

- Botón "Reenviar" en cada tarjeta excepto Consumo (Manual).
- Ícono "Reenviar" en la columna Acción de cada fila del historial excepto filas manuales.
- Confirmación antes de enviar; botones deshabilitados mientras haya un job en curso.

## Errores

- Rechazo DGII (no secuencia) o XSD inválido: el comprobante queda "Rechazado" con el mensaje,
  `ErrorMessage` del job lo indica y el job termina (igual que hoy).
- Excepción de infraestructura (token, certificado): job `Failed` con el mensaje.

## Pruebas

- Unitarias: selección → renglones de la matriz que corren; grupo de un documento existente
  (incluye los tres casos de 32); reconstrucción de la 31 desde un `XmlSent` de ejemplo.
- Manual en staging contra DGII: reenviar un tipo, un comprobante, y `32-RFCE` con sus manuales.
