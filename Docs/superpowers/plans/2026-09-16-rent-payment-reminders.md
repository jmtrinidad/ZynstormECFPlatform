# Recordatorios de pago de renta — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Recordatorios de renta (primer aviso, último aviso, suspensión) automáticos y manuales, resumen al correo
administrativo, días de gracia por cliente y reactivación automática al registrar el pago.

**Architecture:** Regla pura `RentReminderPolicy` + plantillas `RentReminderEmails` + servicio `RentReminderService`
que usan el job diario de Hangfire y dos endpoints manuales de `ClientController`.

**Spec:** `Docs/superpowers/specs/2026-09-16-rent-payment-reminders-design.md`

## Global Constraints

- Build `dotnet build ZynstormECFPlatform.slnx -v q`, tests `dotnet test ZynstormECFPlatform.Tests -v q`: verdes al
  cerrar cada task backend. Frontend: `npm run build` verde y lint sin errores nuevos (hay 2 preexistentes).
- Días en hora RD (`DateTimeExtensions.DrNow`). Fechas de renta en DTOs con `CalendarDateJsonConverter`.
- `RentPaymentGraceDays` 1–30, por defecto 3. Resumen a `RentAlertEmail` o, si vacío, `CertificateAlertEmail`.
- Job diario 8:00 AM `America/Santo_Domingo`.

---

### Task 1: `RentReminderPolicy` (TDD)

- Create `ZynstormECFPlatform.Services/Billing/RentReminderPolicy.cs`,
  `ZynstormECFPlatform.Tests/Billing/RentReminderPolicyTests.cs`.
- `enum RentReminderAction { None, FirstReminder, FinalReminder, Suspend }`
- `RentReminderPolicy.DefaultGraceDays = 3`
- `GetDaysOverdue(DateTime nextPaymentDate, DateTime today) → int`
- `GetDeadline(DateTime nextPaymentDate, int graceDays) → DateTime`
- `Decide(DateTime? nextPaymentDate, int graceDays, DateTime? firstSentFor, DateTime? finalSentFor, bool hasEmail, bool isSuspended, DateTime today) → RentReminderAction`
- Tests: los casos listados en la sección Pruebas del spec. Commit.

### Task 2: Modelo de datos + migración

- `Client`: `RentPaymentGraceDays` (int, default 3), `RentFirstReminderSentFor`, `RentFinalReminderSentFor`,
  `RentSuspendedAtUtc` (DateTime?). Configuración en `StorageContext`.
- `AppSettings.RentAlertEmail` + clave en los tres `appsettings*.json`.
- Migración `AddRentPaymentReminders` (revisar que solo agrega 4 columnas) y `database update`. Commit.

### Task 3: Plantillas y servicio

- Create `Services/Billing/RentReminderEmails.cs`: `BuildFirstReminder`, `BuildFinalReminder`, `BuildSuspension`,
  `BuildAdminSummary(IReadOnlyCollection<RentReminderSummaryItem>)`.
- Create `Services/Billing/IRentReminderService.cs`, `RentReminderService.cs`:
  `RunDailyAsync(ct)`, `SendClientReminderAsync(string clientGuid, ct) → RentReminderResult`,
  `SendAdminSummaryAsync(ct) → RentReminderResult`.
- Create `Services/Jobs/RentReminderJob.cs`; registrar servicio y job en `ServiceCollectionExtensions`; recurring job
  en `Program.cs`.
- Tests de plantillas: HTML codificado, fecha límite y monto presentes. Commit.

### Task 4: API

- DTOs: `ClientCreateDto.RentPaymentGraceDays` `[Range(1, 30)]` = 3; `ClientViewDto.RentSuspended`;
  `ClientRentDto` + `RentPaymentGraceDays`, `RentPaymentDeadline`, `RentDaysOverdue`, `RentFirstReminderSent`,
  `RentFinalReminderSent`, `RentSuspended`, `HasEmail`.
- `ClientController`: reactivación en `Put`; `POST guid/{guid}/rent-reminder/notify`;
  `POST rent/reminders/summary` (solo SA); campos nuevos en `GET rent`. Commit.

### Task 5: Frontend

- Tipos y `rent.service.ts` (`sendRentReminder`, `sendRentSummary`).
- Formulario de cliente: días de gracia y aviso de suspensión.
- `/renta`: acciones, resumen, fecha límite, estado de avisos, badge Suspendido. Commit.
