# Pagos de planes — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Pagos por meses libres para todos los planes, recordatorios automáticos/manuales, resumen administrativo,
suspensión y reactivación automáticas, y pantalla `/pagos`.

**Spec:** `Docs/superpowers/specs/2026-09-16-plan-payments-design.md`

## Global Constraints

- Build `dotnet build ZynstormECFPlatform.slnx -v q` y tests `dotnet test ZynstormECFPlatform.Tests -v q` verdes al
  cerrar cada task backend. Frontend: `npm run build` verde, lint sin errores nuevos (2 preexistentes).
- Hora RD (`DateTimeExtensions.DrNow`); fechas de pago en DTOs con `CalendarDateJsonConverter`.
- `PaidMonths ≥ 1`, `PaymentGraceDays 1–30` (3), `PrepaymentDiscountPercent 0–100` (0).

### Task 1 — Renombrado y `PaidMonths` (backend)

- Entidad, `StorageContext`, `AppSettings`, `appsettings*.json` con los nombres nuevos.
- `RentCalculator` → `PaymentCalculator` con `Calculate(monthlyFee, paidMonths, discountPercent)`;
  `RentCalculationResult` → `PaymentCalculationResult`; `GetRentStatus` → `GetPaymentStatus`;
  `RentStatus` → `PaymentStatus`; `ValidateRentPlan` y `UnlimitedUsers` a `BillingCalculator`.
- `RentReminderPolicy` → `PaymentReminderPolicy`, `RentReminderAction` → `PaymentReminderAction`.
- DTOs: `ClientRentDto` → `ClientPaymentDto`; campos de cliente con nombres nuevos.
- Controller compila con los nombres nuevos (comportamiento aún solo renta).
- Migración `GeneralizePlanPayments`: `RenameColumn` ×7, agregar `PaidMonths`, `UPDATE` desde `RentPaidFullYear`,
  eliminar `RentPaidFullYear`. Revisar SQL generado y aplicar.
- Tests renombrados y ajustados (meses libres). Commit.

### Task 2 — Cobertura por pago para todos los planes

- `ClientController`: `GET payments` para todos los planes activos; `FillPaymentStatusAsync` para cualquier plan con
  fecha; `/usage`: filas de comprobantes con mensualidad cubierta (`MonthlyFeeCovered`, total = monto del mes +
  excedente); filas de renta sin cambios.
- `ClientMonthlyUsageDto.MonthlyFeeCovered`. Commit.

### Task 3 — Correos, servicio y job

- `EmailLayout` compartido; `PaymentReminderEmails` (primer aviso, último aviso, suspensión, resumen) con tests.
- `IPaymentReminderService` / `PaymentReminderService`: `RunDailyAsync`, `SendClientReminderAsync`,
  `SendAdminSummaryAsync`.
- `PaymentReminderJob`, registro DI y recurring job 8:00 AM RD. Commit.

### Task 4 — API de avisos y reactivación

- `POST guid/{guid}/payment-reminder/notify`, `POST payments/reminders/summary`.
- Reactivación en `Put`; `ClientPaymentDto` con fecha límite, días vencida, marcas de aviso, suspensión, correo.
  Commit.

### Task 5 — Frontend

- Tipos, `payment.service.ts`, `lib/payment.ts`.
- Formulario de cliente: bloque Pago (meses, gracia, descuento, resumen, suspensión).
- `/pagos` (reemplaza `/renta`), sidebar, `PaymentAlert` en dashboard, enlaces.
- `/consumo`: mensualidad cubierta en comprobantes. Commit.
