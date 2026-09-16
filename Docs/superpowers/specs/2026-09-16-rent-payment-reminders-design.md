# Recordatorios de pago de renta y suspensión automática — Diseño

Fecha: 2026-09-16

Extiende el [plan de renta](2026-09-15-client-rent-plan-design.md).

## Objetivo

Avisar por correo, de forma automática y manual, a los clientes con plan de renta que no han pagado, enviar un
resumen de pendientes al correo administrativo y suspender al cliente si no paga dentro de sus días de gracia.

## Reglas

- **Pago aplicado** = el SA actualiza el último y el próximo pago del cliente. Mientras `NextRentPaymentDate` sea
  anterior a hoy (hora RD), la renta está pendiente.
- **Días de gracia por cliente**: `Client.RentPaymentGraceDays`, rango 1–30, por defecto 3. Se cuentan desde la
  fecha de pago. Fecha límite = `NextRentPaymentDate + RentPaymentGraceDays`.
- Sea `vencida = hoy − NextRentPaymentDate` en días y `g` los días de gracia. Con pago el 15 y `g = 3`:

| Día | `vencida` | Acción automática |
|---|---|---|
| 16 | 1 | **Primer aviso** al cliente |
| 17 | 2 | — |
| 18 | 3 = g | **Último aviso** al cliente (hoy vence el plazo) |
| 19 | 4 = g+1 | **Suspensión**: `ClientInactive = true` y correo de suspensión |

- Cada aviso se envía **una sola vez por fecha de pago**. Se guarda para qué fecha salió:
  `RentFirstReminderSentFor`, `RentFinalReminderSentFor`. Al avanzar la fecha de pago, el ciclo empieza de cero.
- **Catch-up**: si el job no corrió, se aplica lo que corresponda al día. Nunca se envía el primer aviso si ya salió
  el último.
- **Suspensión** solo si el último aviso ya se había enviado **en una corrida anterior** para esa fecha, así el
  cliente siempre recibe el último aviso antes del corte. Excepción: clientes sin correo se suspenden al llegar a
  `vencida = g+1`.
- Una corrida aplica **una sola acción** por cliente.
- **Reactivación automática**: la suspensión se marca con `RentSuspendedAtUtc`. Al guardar un cliente suspendido por
  renta con un `NextRentPaymentDate` posterior a hoy, se reactiva (`ClientInactive = false`,
  `RentSuspendedAtUtc = null`). Si el SA apaga a mano "Cliente inactivo", también se limpia `RentSuspendedAtUtc`.
  Un cliente desactivado a mano por otro motivo no se toca.
- No se envían avisos a clientes ya inactivos ni a planes de renta inactivos.

## Correos

Todos usan el mismo marco HTML que el aviso de certificados.

- **Primer aviso** (amigable): llegó el día de pago de la renta del plan, monto del ciclo, fecha de pago; "le
  agradecemos realizar el pago antes del *fecha límite* para evitar cortes del servicio y recargos"; "si ya realizó el
  pago, ignore este mensaje"; agradecimiento.
- **Último aviso**: igual, pero "hoy vence el plazo".
- **Suspensión**: el servicio quedó suspendido por falta de pago y se reactiva al registrar el pago.
- **Resumen administrativo**: tabla con cliente, RNC, fecha de pago, días vencida, monto, fecha límite y
  estado/acción del día (Primer aviso enviado, Último aviso enviado, Suspendido hoy, Suspendido, Sin correo). Solo se
  envía si hay pendientes. Destino: `AppSettings.RentAlertEmail`; si está vacío, `CertificateAlertEmail`.

## Componentes backend

1. `RentReminderPolicy` (pura, `Services/Billing`):
   `Decide(nextPaymentDate, graceDays, firstSentFor, finalSentFor, hasEmail, isSuspended, today) → RentReminderAction`
   con `None | FirstReminder | FinalReminder | Suspend`, y `GetDeadline`, `GetDaysOverdue`.
2. `RentReminderEmails` (plantillas HTML).
3. `IRentReminderService` / `RentReminderService`:
   - `RunDailyAsync` — aplica la política a cada cliente, envía y persiste; al final envía el resumen.
   - `SendClientReminderAsync(guid)` — manual; exige renta vencida hoy o antes y correo; envía el aviso que corresponda
     (suspensión si ya está suspendido) y marca la etapa. Nunca suspende.
   - `SendAdminSummaryAsync` — manual; resumen sin ejecutar acciones.
4. `RentReminderJob` en Hangfire, diario 8:00 AM hora RD.
5. `ClientController`: `POST guid/{guid}/rent-reminder/notify`, `POST rent/reminders/summary`; reglas de reactivación
   en `Put`; nuevos campos en `GET rent`.
6. Migración: `Client.RentPaymentGraceDays` (int, default 3), `RentFirstReminderSentFor`, `RentFinalReminderSentFor`
   (DateTime?), `RentSuspendedAtUtc` (DateTime?).

## Frontend

- Formulario de cliente, bloque Renta: campo "Días de gracia" y aviso "Suspendido por falta de pago; se reactiva al
  registrar el pago" cuando aplica.
- `/renta`: columna de acciones con **Enviar recordatorio** (habilitado si vence hoy o ya venció), botón
  **Enviar resumen a mi correo**, fecha límite, estado del aviso y badge **Suspendido**.

## Pruebas

`RentReminderPolicy`: sin fecha; al día; día de pago (0); día 1 → primero; día 1 con primero enviado → nada;
día g → último; día g con último enviado → nada; día g+1 con último enviado → suspender; día g+1 sin último →
último (catch-up); sin correo en g+1 → suspender; ya suspendido → nada; cambio de ciclo (marcas de otra fecha);
gracia 1 → el día 1 es el último; fecha límite.

## Fuera de alcance

Recargos calculados, historial de avisos, notificación por WhatsApp/SMS.
