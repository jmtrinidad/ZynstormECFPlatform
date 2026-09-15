# Planes de cliente, consumo mensual y cliente inactivo — Diseño

Fecha: 2026-09-14

## Objetivo

1. Asignar a cada cliente un **plan** con mensualidad, límite mensual de comprobantes y tramos de cobro por excedente.
2. Llevar el **conteo mensual** de comprobantes aceptados por la DGII para facturar al cierre del mes.
3. Poder **desactivar** un cliente (`ClientInactive`), bloqueando la emisión.
4. Devolver siempre `EcfDocumentId` (y `ClientInactive`) en la respuesta de `POST v1/Ecf/emit`.

## Reglas de negocio

- **Nunca se bloquea por límite.** Los comprobantes por encima del límite se cobran por tramos al cierre del mes.
- **Cliente sin plan** (`PlanId = null`) o con plan inactivo: envía sin límite y **no** se registra consumo.
- **Cliente con plan activo**: cada comprobante aceptado suma 1 al consumo del mes.
- `Plan.MonthlyDocumentLimit = -1` significa **ilimitado**: se registra consumo, se cobra solo la mensualidad, excedente = 0.
- **Cuentan solo** los comprobantes con estado DGII `Aceptado` o `Aceptado Condicional(mente)`. Rechazados, errores de XSD/DTO o pendientes no cuentan. La decisión se toma sobre el estado DGII (no sobre `EcfStatusId`, porque hoy condicional y rechazado comparten el id 11).
- El mes se determina por la fecha de aceptación en hora de República Dominicana (`ToDrTime()`).
- **Cliente inactivo** (`ClientInactive = true`), con o sin plan: `emit` responde `403` sin crear documento ni enviar a DGII:
  ```json
  { "success": false, "clientInactive": true, "ecfDocumentId": 0,
    "message": "El cliente se encuentra desactivado. Por favor, comuníquese con soporte." }
  ```

### Fórmula de cobro (escalonada)

```
excedente = límite == -1 ? 0 : max(0, aceptados - límite)
cargo_tramo(t) = unidades del excedente dentro de [t.FromUnit, t.ToUnit ?? ∞] × t.UnitPrice
total = MonthlyFee + Σ cargo_tramo
```

Ejemplo (tramos 1–100 a RD$6.00, 101+ a RD$9.00): excedente 100 → RD$600; excedente 150 → 100×6 + 50×9 = RD$1,050.

## Modelo de datos (una migración)

| Entidad | Campos |
|---|---|
| `Plan` (nueva, hereda `BaseEntity`) | `PlanId`, `Name` (100), `Description?` (300), `MonthlyFee` decimal(18,2), `MonthlyDocumentLimit` int (`-1` o `> 0`), `StatusId` |
| `PlanOverageTier` (nueva) | `PlanOverageTierId`, `PlanId` FK, `FromUnit` int (≥1), `ToUnit` int? (null = sin tope), `UnitPrice` decimal(18,2) |
| `Client` (modificada) | `PlanId?` FK, `ClientInactive` bool default `false` |
| `ClientMonthlyUsage` (nueva) | `ClientMonthlyUsageId`, `ClientId` FK, `Year`, `Month`, `AcceptedDocuments` int, snapshot: `PlanId`, `PlanName`, `MonthlyFee`, `MonthlyDocumentLimit`; índice único `(ClientId, Year, Month)` |
| `EcfDocument` (modificada) | `BillingCountedAtUtc` DateTime? |

- Los tramos no se copian al snapshot; si se requiere histórico exacto de precios se evaluará después (YAGNI). El snapshot congela mensualidad y límite del mes.
- Validación de tramos al guardar un plan: `FromUnit` empieza en 1, contiguos sin solapamiento, solo el último puede tener `ToUnit = null`.

## Componentes backend

1. **`IClientUsageService.RegisterAcceptedAsync(EcfDocument)`**
   - Carga el cliente con plan; si no hay plan activo → no hace nada.
   - Si `ecfDocument.BillingCountedAtUtc != null` → no hace nada (idempotencia).
   - Upsert de la fila del mes (creándola con snapshot del plan) e incremento atómico (`UPDATE ... SET AcceptedDocuments = AcceptedDocuments + 1`), luego marca `BillingCountedAtUtc`, todo en una transacción.
2. **`BillingCalculator.Calculate(monthlyFee, limit, tiers, acceptedDocuments)`** — función pura que devuelve excedente, desglose por tramo y total.
3. **Puntos de llamada** a `RegisterAcceptedAsync`:
   - `ReceivedEcfProductionService.ProcessAsync` — estado final inmediato de DGII y rama sin TrackId.
   - `ProcessWithStagingValidationAsync` — validación aceptada.
   - `EcfTrackingJob` — cuando el seguimiento obtiene estado aceptado.
4. **`emit`**
   - Verificación de `ClientInactive` antes de `CreateEcfDocumentAsync`; el controller devuelve `403` cuando `result.ClientInactive`.
   - `ReceivedEcfEmissionResultDto` agrega `ClientInactive`.
   - Cliente / ApiKey / certificado no encontrado: se devuelve `resultDto` con mensaje (`400`) en lugar de lanzar `Exception`.
   - Errores inesperados posteriores a la creación del documento conservan `ecfDocumentId` en la respuesta `500`.
5. **Endpoints**
   - `PlanController` (CRUD con tramos, patrón `BaseController`, solo rol SA).
   - `GET v1/Client/usage?year=&month=` — clientes con consumo del mes: aceptados, límite, excedente, desglose y total.
   - `GET v1/Client/guid/{guid}/usage?year=&month=` — detalle de un cliente.
   - `ClientCreateDto` / `ClientViewDto` agregan `PlanId`, `PlanName` (view) y `ClientInactive`.

## Frontend (Next.js)

- `/configuraciones/planes`: listado y formulario de plan (nombre, mensualidad, límite con check "Ilimitado" → `-1`, editor de tramos).
- `/clientes`: selector de plan (incluye "Sin plan") y switch "Inactivo" en el formulario; badge "Inactivo" en la tabla.
- `/consumo`: selector de mes/año y tabla de clientes con aceptados, límite, excedente y total a facturar; detalle con desglose por tramo.
- Nuevos `plan.service.ts`, `usage.service.ts` y tipos correspondientes.

## Pruebas

- `BillingCalculator`: sin excedente, excedente dentro del primer tramo, excedente cruzando tramos (150 → RD$1,050), ilimitado (`-1`), plan sin tramos.
- `ClientUsageService`: cliente sin plan no cuenta; plan inactivo no cuenta; documento ya contado no suma dos veces; primer aceptado del mes crea fila con snapshot.
- Validación de tramos de plan.
- `emit` con cliente inactivo retorna `clientInactive = true` y no invoca transmisión.

## Fuera de alcance

- Generación automática de la factura del cliente (el reporte mensual es la entrada para facturar).
- Histórico de precios de tramos por mes.
- Corregir el mapeo compartido de `EcfStatusId = 11` para condicional/rechazado.
