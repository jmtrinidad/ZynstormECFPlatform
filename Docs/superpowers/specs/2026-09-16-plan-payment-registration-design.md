# Registro de pagos de planes e historial — Diseño

Fecha: 2026-09-16

## Objetivo

Desde la página **Pagos** registrar los pagos que recibe cada cliente, llevar un historial y mantener al día los datos de pago del cliente:

1. Registrar el pago de la **mensualidad del plan** (renta o comprobantes): avanza el próximo pago y actualiza el último pago.
2. En planes de **comprobantes**, registrar también el pago del **excedente** de meses cerrados, aunque la mensualidad esté cubierta (por ejemplo, pagó el año completo).
3. Si el cliente estaba **suspendido por falta de pago**, reactivarlo al registrar el pago cuando queda al día.
4. Mostrar un modal **"Pago recibido"** con el resumen.

## Contexto actual

- `Client` guarda `LastPaymentDate`, `NextPaymentDate`, `PaidMonths`, `PrepaymentDiscountPercent`, `PaymentGraceDays`, marcas de avisos y `PaymentSuspendedAtUtc`. Hoy solo cambian editando el cliente (`PUT v1/Client`), que ya reactiva cuando la nueva `NextPaymentDate` es posterior a hoy.
- `PaymentCalculator.Calculate(monthlyFee, paidMonths, discountPercent)` calcula el ciclo; `BillingCalculator.Calculate` calcula el excedente de un `ClientMonthlyUsage`.
- No existe historial de pagos ni registro de excedentes pagados.

## Reglas de negocio

### Mensualidad
- Meses y descuento: los configurados en el cliente (`PaidMonths`, `PrepaymentDiscountPercent`). No se eligen en el modal.
- Monto: **fijo**, el total de `PaymentCalculator.Calculate`. No se edita.
- Nuevo próximo pago: `NextPaymentDate` anterior + `PaidMonths` meses. Si el cliente no tenía `NextPaymentDate`, se cuenta desde la fecha del pago.
- `LastPaymentDate` = fecha del pago.
- Solo clientes con plan asignado y mensualidad mayor que 0.

### Excedente (solo planes de comprobantes)
- Se pueden pagar meses **cerrados** (anteriores al mes actual en hora RD) con excedente mayor que 0 y **no pagados**.
- Monto: **fijo**, el `OverageAmount` de `BillingCalculator.Calculate` para esa fila de `ClientMonthlyUsage`, guardado en la línea del pago.
- Un mes de excedente se paga una sola vez (índice único).
- El excedente pendiente **solo se informa**: no afecta estado vencido, avisos ni suspensión.

### Registro
- Al menos un concepto (mensualidad y/o uno o más meses de excedente).
- Datos del pago: fecha de recepción (por defecto hoy RD, no futura), método (1 Efectivo, 2 Transferencia, 3 Tarjeta, 4 Cheque, 5 Otro), referencia opcional (100), nota opcional (500).
- Todo en una transacción: recibo, líneas y actualización del cliente.

### Reactivación
- Si `PaymentSuspendedAtUtc != null` y el pago incluye mensualidad y la nueva `NextPaymentDate` es posterior a hoy (RD): `ClientInactive = false`, `PaymentSuspendedAtUtc = null`.
- Si debía varios ciclos y la nueva fecha sigue vencida, **sigue suspendido**; la respuesta lo indica y el modal lo muestra.
- Un pago solo de excedente nunca reactiva.

## Modelo de datos (una migración)

| Entidad | Campos |
|---|---|
| `ClientPayment` (nueva, `BaseEntity`) | `ClientPaymentId`, `ClientId` FK, `PaymentDate` (date), `PaymentMethod` int, `Reference?` (100), `Notes?` (500), `TotalAmount` decimal(18,2), `RegisteredByUserId?` (450) |
| `ClientPaymentItem` (nueva, `BaseEntity`) | `ClientPaymentItemId`, `ClientPaymentId` FK (cascade), `ItemType` int (1 Mensualidad, 2 Excedente), `Amount` decimal(18,2), `PlanId?`, `PlanName` (100); mensualidad: `MonthsCovered?`, `GrossAmount?`, `DiscountPercent?`, `DiscountAmount?`, `PreviousNextPaymentDate?`, `NewNextPaymentDate?`; excedente: `ClientMonthlyUsageId?` FK, `Year?`, `Month?`, `OverageDocuments?` |

- Índice único parcial `IX_ClientPaymentItem_Overage_Usage` sobre `ClientMonthlyUsageId` con filtro `"ItemType" = 2 AND NOT "IsDeleted"`.
- Enums en `Core/Enums`: `PaymentMethod`, `ClientPaymentItemType`.

## Componentes backend

1. **`PaymentRegistrationPolicy`** (`Services/Billing`, funciones puras):
   - `DateTime CalculateNewNextPaymentDate(DateTime? currentNext, DateTime paymentDate, int paidMonths)`
   - `bool IsOverageMonthPayable(int year, int month, DateTime today)` (mes cerrado)
   - `bool ShouldReactivate(bool paymentSuspended, bool includesPlanFee, DateTime? newNextPaymentDate, DateTime today)`
   - `List<string> ValidateRequest(...)` (fecha no futura, método 1–5, al menos un concepto)
2. **`IClientPaymentRegistrationService`** (`Services/Billing`):
   - `GetPreviewAsync(clientGuid)` → mensualidad calculada, fechas actual/nueva, excedentes pendientes.
   - `RegisterAsync(clientGuid, request, userId)` → recibo; valida, crea `ClientPayment` + líneas, actualiza `Client`, reactiva; todo en `IUnitOfWork.ExecuteInTransactionAsync`.
   - `GetHistoryAsync(clientGuid)` → recibos con líneas, más reciente primero.
   - `GetPendingOverageAsync(clientIds)` → para agregar a `GET v1/Client/payments`.
3. **Endpoints** en `ClientController` (solo SA):
   - `GET v1/Client/guid/{guid}/payments/preview`
   - `POST v1/Client/guid/{guid}/payments` → 200 recibo; 400 con errores; 404 cliente; 409 si un excedente ya fue pagado.
   - `GET v1/Client/guid/{guid}/payments`
   - `GET v1/Client/payments`: `ClientPaymentDto` agrega `PendingOverageAmount` y `PendingOverageMonths`.
4. **DTOs** (`ClientPaymentRegistrationDtos.cs`): `PaymentPreviewDto`, `PendingOverageDto`, `RegisterPaymentRequestDto`, `PaymentReceiptDto`, `PaymentReceiptItemDto`.

## Frontend (`/pagos`)

- Botón **"Registrar pago"** por fila, junto a *Recordar*.
- **Modal Registrar pago**: carga el preview.
  - Bloque *Mensualidad* (checkbox, marcado por defecto si aplica): meses, bruto, descuento, total, próximo pago actual → nuevo.
  - Bloque *Excedentes pendientes* (solo comprobantes): checkbox por mes con documentos excedentes y monto.
  - Datos: fecha, método, referencia, nota. Total a registrar (suma de lo marcado). Botón *Registrar pago*.
- **Modal Pago recibido**: número de recibo, conceptos, total, nuevo próximo pago, aviso de reactivado o de que sigue suspendido.
- Botón **"Historial"** por fila: modal con los recibos y sus líneas.
- Tabla: bajo *Total del ciclo*, "Excedente pendiente: RD$…" cuando `pendingOverageAmount > 0`.
- Nuevos `types/clientPaymentRegistration.type.ts` y funciones en `services/payment.service.ts`.

## Pruebas

- `PaymentRegistrationPolicyTests`: avance desde la fecha vencida (15/09 + 1 = 15/10 aunque pague el 18/09), sin fecha previa, fin de mes (31/01 + 1 = 28/02), mes cerrado vs. mes en curso, reactivación (al día / sigue vencido / solo excedente), validación.
- Verificación manual contra `zynstorm_ecf_platform_dev2_db`.

## Fuera de alcance

- Anular o editar pagos registrados.
- Adjuntar comprobantes (fotos/PDF).
- Enviar el recibo por correo.
- Pagos parciales o montos editables.
