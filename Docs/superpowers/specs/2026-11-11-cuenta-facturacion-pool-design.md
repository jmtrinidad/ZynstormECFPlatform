# Cuenta de facturación con pool de comprobantes — Diseño

Fecha: 2026-11-11
Estado: aprobado en conversación, pendiente de revisión del spec escrito.

## Objetivo

Permitir que un usuario (revendedor) tenga un plan de comprobantes propio, con una cantidad mensual que comparten todos sus clientes. Si la suma de los comprobantes aceptados de esos clientes supera el límite del plan, se cobra excedente con los mismos tramos (`PlanOverageTier`) que ya usan los demás planes.

El revendedor crea sus propios clientes y, a su vez, crea usuarios para esos clientes para que den seguimiento a sus envíos.

## Decisiones tomadas

| Tema | Decisión |
|---|---|
| División del cupo | Pool compartido, con un cupo **informativo** opcional por cliente. Pasar el cupo solo genera una alerta; no bloquea la emisión y el cobro se calcula sobre el total. |
| Cobro al revendedor | Solo la mensualidad del plan más el excedente por tramos. Sus clientes no pagan nada a Zynstorm. |
| Límite de clientes | Sin límite. El único tope es el volumen de comprobantes, que ya se cobra con el excedente. |
| Quién crea la cuenta | Solo SA: crea la cuenta, asigna dueño y plan, y registra los pagos. |
| Clientes existentes | No cambian. Un cliente sin cuenta factura como hoy. |

## Situación actual (resumen)

- La unidad de cobro es `Client`: `PlanId`, fechas de pago, `PaymentSuspendedAtUtc`, descuentos.
- El consumo se cuenta por cliente en `ClientMonthlyUsage (ClientId, Year, Month)`. `ClientUsageService.RegisterAcceptedAsync` usa un SQL atómico e idempotente que marca `EcfDocument.BillingCountedAtUtc`. Se llama desde `EcfTrackingJob` y `ReceivedEcfProductionService`.
- `BillingCalculator.Calculate(fee, limit, tiers, accepted)` calcula mensualidad y excedente por tramos.
- Los pagos son `ClientPayment` con líneas `ClientPaymentItem` (`PlanFee` u `Overage`). Un índice único impide pagar dos veces el excedente de un mismo mes.
- `UserClient` es solo acceso (muchos a muchos). `User.CreatedByUserId` indica quién creó al usuario. No existe un nivel por encima del cliente que tenga plan.
- `POST /User` y `PUT /User` (roles SA y Admin) no validan que los `ClientIds` asignados pertenezcan al Admin que los asigna.
- Las pantallas de consumo y pagos son solo para SA.

## Diseño

### 1. Datos

**Tablas nuevas**

- `BillingAccount`
  - `BillingAccountId`, `GuidId`, `Name`, `StatusId`.
  - `OwnerUserId`: el Admin revendedor.
  - `PlanId`, `CustomMonthlyFee`, `OfferStartDate`.
  - Campos de pago equivalentes a los de `Client`: `LastPaymentDate`, `NextPaymentDate`, `PaidMonths`, `PrepaymentDiscountPercent`, `PaymentGraceDays`, `FirstPaymentReminderSentFor`, `FinalPaymentReminderSentFor`, `PaymentSuspendedAtUtc`.
- `BillingAccountMonthlyUsage`
  - `BillingAccountId`, `Year`, `Month`, `AcceptedDocuments`.
  - Snapshot del plan (`PlanId`, `PlanName`, `MonthlyFee`, `MonthlyDocumentLimit`), igual que `ClientMonthlyUsage`.
  - Índice único `(BillingAccountId, Year, Month)`.

**Cambios a tablas existentes** (todos nullable y aditivos)

- `Client.BillingAccountId`: si tiene valor, el cliente factura contra el pool y su `PlanId` propio se ignora. Si es null, todo funciona como hoy.
- `Client.QuotaDocuments`: cupo informativo mensual.
- `ClientPayment.BillingAccountId` y `ClientPayment.ClientId` pasan a ser nullable. Una restricción check exige que exactamente uno de los dos tenga valor.
- `ClientPaymentItem.BillingAccountMonthlyUsageId`: referencia al excedente de la cuenta, con un índice único filtrado análogo a `IX_ClientPaymentItem_Overage_Usage`.

**Reglas**

- `PlanTypeEnum` no cambia. Un plan `Documents` sirve para una cuenta o para un cliente suelto.
- Una cuenta tiene un solo plan y un solo dueño, y un usuario es dueño de **una sola** cuenta (índice único sobre `OwnerUserId`). Si más adelante hace falta más de una por usuario, se quita el índice y se agrega un selector de cuenta.
- Un plan `Rent` no se puede asignar a una cuenta.

### 2. Consumo y cobro

- **Contar.** `RegisterAcceptedAsync` detecta si el cliente tiene `BillingAccountId`. En ese caso, el SQL atómico incrementa en una sola sentencia tanto `ClientMonthlyUsage` (detalle por cliente) como `BillingAccountMonthlyUsage` (total del pool), y marca `BillingCountedAtUtc`. Se mantiene la idempotencia ante reintentos y concurrencia.
- **Snapshot del plan.** La fila de la cuenta guarda el plan de la cuenta, no el del cliente. La fila del cliente guarda el mismo snapshot del plan de la cuenta, solo como referencia: el excedente nunca se cobra desde la fila del cliente.
- **Calcular.** `BillingCalculator.Calculate` no cambia. Se llama con el total del pool y el plan de la cuenta.
- **Cobrar.** `ClientPaymentRegistrationService` opera sobre un "cobrable" que puede ser un cliente suelto o una cuenta. Se reutilizan las fechas de pago, el descuento por adelanto, la oferta, el precio especial y los excedentes pendientes de meses cerrados.
- **Suspensión y recordatorios.**
  - El job de recordatorios y suspensión recorre cuentas además de clientes.
  - `ClientStatusEvaluator` revisa `PaymentSuspendedAtUtc` de la cuenta del cliente. Si la cuenta está suspendida, la API de emisión de todos sus clientes responde "Cliente suspendido por falta de pago".
  - Al pagar y reactivar la cuenta, se reactivan sus clientes.
- **Cupo informativo.** No se escribe nada al contar. La alerta "cliente X pasó su cupo" se calcula al leer, comparando `ClientMonthlyUsage.AcceptedDocuments` con `Client.QuotaDocuments`.

### 3. Usuarios y permisos

- **Crear clientes.** `POST /Client` ya asigna el cliente al creador. Se agrega: si el creador es dueño de una `BillingAccount`, el cliente nace con su `BillingAccountId`. Un Admin sin cuenta crea clientes sueltos como hoy.
- **Usuarios de clientes.**
  - Un Admin crea usuarios Normal y les asigna clientes.
  - `POST /User` y `PUT /User` validan que cada `ClientId` asignado pertenezca a la cuenta del Admin que lo asigna (SA queda exento). Esto cierra el hueco actual.
- **Qué ve cada rol.**
  - SA: todo. Crea cuentas, asigna dueño y plan, y registra pagos.
  - Admin revendedor: su pool, el consumo y el cupo por cliente, el excedente proyectado del mes y el historial de pagos de su cuenta (solo lectura).
  - Normal: solo sus clientes y sus envíos. No ve consumo ni pagos.
- **Frontend.**
  - Nueva página `/cuenta` ("Mi cuenta") para el revendedor: barra de uso del pool, tabla de consumo y cupo por cliente con alertas, y excedente proyectado con el cálculo por tramos.
  - Las páginas `consumo` y `pagos` del SA muestran las cuentas como una fila más, junto a los clientes sueltos.
  - El acceso a `/cuenta` se controla con los permisos por página existentes (`UserPageAccess`).
  - El formulario de cliente permite editar el cupo informativo.

### 4. Migración y compatibilidad

- Migración EF Core aditiva; sin backfill. Los clientes actuales quedan con `BillingAccountId` null.
- Convertir un cliente suelto en miembro de una cuenta es una acción manual del SA. El consumo ya acumulado del mes sigue en la fila del cliente y no se traslada al pool; el pool cuenta desde el momento en que el cliente se asigna.

## Pruebas

Se agregan pruebas xUnit junto a `BillingCalculatorTests`:

- Un pool con varios clientes cuya suma supera el límite genera el excedente correcto por tramos.
- Un pool que no supera el límite no genera excedente.
- Un documento se cuenta una sola vez en cliente y pool, aun con reintentos y concurrencia.
- Un cliente con cuenta no genera cobro propio.
- El cupo informativo genera alerta al pasarse pero no bloquea la emisión.
- Un Admin no puede asignar clientes de otra cuenta en `POST /User` ni `PUT /User`.
- La suspensión de la cuenta bloquea a todos sus clientes y el pago la reactiva.
- El pago de excedente de cuenta no se puede registrar dos veces (índice único).

## Fases de entrega

Cada fase tiene su propio plan de implementación.

1. **Datos y consumo:** entidades, migración, SQL atómico, tests.
2. **Cobro, suspensión y recordatorios.**
3. **Permisos de usuarios** y validaciones.
4. **Pantallas:** `/cuenta`, cambios en `consumo` y `pagos`, formulario de cliente.

Al terminar, se replica en `Staging` y en `feature/emit-idempotente`.

## Fuera de alcance

- Cupo estricto por cliente con bloqueo de emisión.
- Cargo por cliente o clientes con plan propio dentro de la misma cuenta.
- Límite de clientes por plan (`MaxClients`).
- Reasignación automática del consumo ya acumulado al mover un cliente a una cuenta.
