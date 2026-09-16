# Pagos de planes: meses pagados, recordatorios y suspensión — Diseño

Fecha: 2026-09-16

Reemplaza la parte de pagos del [plan de renta](2026-09-15-client-rent-plan-design.md) y la generaliza a todos los
planes (renta y comprobantes).

## Objetivo

1. Registrar el pago de **cualquier plan**, por una cantidad libre de meses, con descuento por pago adelantado.
2. Avisar por correo (automático y manual) a los clientes que no han pagado y enviar un resumen de pendientes al
   correo administrativo.
3. Suspender al cliente si no paga dentro de sus días de gracia y reactivarlo al registrar el pago.

## Modelo de datos

Las columnas agregadas para renta se renombran a nombres genéricos (migración con `RenameColumn`, sin pérdida de
datos). `RentPaidFullYear` se reemplaza por `PaidMonths` (los `true` quedan en 12).

| Columna en `Client` | Tipo | Nota |
|---|---|---|
| `LastPaymentDate` | DateTime? | fecha calendario |
| `NextPaymentDate` | DateTime? | fecha calendario, sugerida y editable |
| `PaidMonths` | int, default 1 | meses que cubre el pago, ≥ 1, sin tope |
| `PrepaymentDiscountPercent` | decimal(5,2), default 0 | 0–100 |
| `PaymentGraceDays` | int, default 3 | 1–30 |
| `FirstPaymentReminderSentFor` | DateTime? | fecha de pago para la que salió el primer aviso |
| `FinalPaymentReminderSentFor` | DateTime? | fecha de pago para la que salió el último aviso |
| `PaymentSuspendedAtUtc` | DateTime? | null = no suspendido por falta de pago |

`AppSettings`: `PaymentWarningDays` (15), `PaymentAlertEmail` (vacío → `CertificateAlertEmail`).

## Cálculo

```
ciclo = MonthlyFee × PaidMonths × (1 − PrepaymentDiscountPercent / 100)
```

Monto del mes consultado: si `NextPaymentDate` es posterior al mes → 0 (cubierto); si cae en el mes o antes, o no
hay fecha → ciclo completo.

- **Renta**: el total del mes en `/consumo` es ese monto.
- **Comprobantes**: el pago cubre **solo la mensualidad**. Total del mes = monto del mes + excedente. Clientes sin
  fechas de pago: se comportan como hoy (mensualidad + excedente).

## Recordatorios y suspensión

Aplican a clientes con plan activo (cualquier tipo), `NextPaymentDate` y no desactivados a mano. Con pago el día D y
gracia g:

| Día | Acción automática |
|---|---|
| D+1 | Primer aviso |
| D+g | Último aviso (hoy vence el plazo) |
| D+g+1 | Suspensión (`ClientInactive = true`, `PaymentSuspendedAtUtc`) y correo de suspensión |

- Un aviso por fecha de pago; una acción por corrida; catch-up si el job no corrió.
- Suspensión solo si el último aviso salió en una corrida anterior; clientes sin correo se suspenden en D+g+1.
- Reactivación: al guardar un cliente suspendido por pago con `NextPaymentDate` posterior a hoy. Apagar a mano
  "Cliente inactivo" también limpia `PaymentSuspendedAtUtc`.
- Correos con el marco HTML compartido. Tono amigable; dicen "renta" o "mensualidad" según el tipo de plan, monto del
  ciclo, fecha de pago y fecha límite ("antes del … para evitar cortes del servicio y recargos").
- Resumen administrativo: cliente, RNC, plan, fecha de pago, días vencida, monto, fecha límite, estado/acción del día.
  Solo si hay pendientes.
- Job `PaymentReminderJob` diario 8:00 AM hora RD.

## API

- `GET v1/Client/payments` → `ClientPaymentDto` (todos los clientes con plan activo).
- `POST v1/Client/guid/{guid}/payment-reminder/notify` — manual; exige vencido hoy o antes y correo; envía el aviso
  que corresponde (suspensión si ya está suspendido), marca la etapa, nunca suspende.
- `POST v1/Client/payments/reminders/summary` — resumen manual.
- `GET v1/Client` llena `PaymentStatus`, `PaymentDaysToDue`, `PaymentCycleAmount`, `PaymentSuspended` para cualquier
  plan.

## Frontend

- Formulario de cliente: bloque **Pago** para cualquier plan; meses pagados (1 · 3 · 6 · 12 + número libre), días de
  gracia, descuento, resumen del ciclo, aviso de suspensión.
- `/renta` → **`/pagos`**: filtro Todos / Renta / Comprobantes, semáforo, fecha límite, estado de avisos, enviar
  recordatorio, enviar resumen, badge Suspendido. Aviso del dashboard para ambos tipos.
- `/consumo`: en comprobantes, mensualidad "Cubierta" cuando el mes está pagado.

## Pruebas

`PaymentCalculator` (meses libres, descuento, monto del mes), `PaymentReminderPolicy` (casos de avisos, catch-up,
suspensión, cambio de ciclo, gracia), plantillas de correo, convertidor de fechas con los nombres nuevos.

## Fuera de alcance

Recargos calculados, historial de pagos, cobertura del excedente.
