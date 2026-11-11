# Cuenta de facturación con pool — Fase 4: pantallas

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el SA administre cuentas y sus pagos desde la interfaz, que el Admin revendedor vea su pool y fije el cupo de sus clientes en una página "Mi cuenta", y que `consumo` y `pagos` muestren las cuentas junto a los clientes.

**Architecture:** Un pequeño soporte de backend (consumo de cuentas en el formato del reporte actual, `billingAccountId` en el cliente, y dejar fuera del reporte de clientes las filas de detalle de los miembros) permite que el frontend reutilice sus pantallas. Los tres diálogos de pago (registrar, historial, recibo) pasan a recibir un `PaymentTarget` (cliente, cuenta o cuenta propia) en vez de un `ClientPayment`. Dos páginas nuevas: `/cuenta` (solo Admin) y `/configuraciones/cuentas` (solo SA).

**Tech Stack:** .NET 10 + xUnit (backend); Next.js 16, React 19, TypeScript 5.7, Tailwind 4, shadcn/ui, zustand (frontend).

Spec: `Docs/superpowers/specs/2026-11-11-cuenta-facturacion-pool-design.md` (§3 Frontend). API ya hecha en las fases 2 y 3.

## Global Constraints

- **No toques ninguna base de datos ni levantes la API contra producción.** El `DefaultConnection` activo de `appsettings.Development.json` apunta a producción. Esta fase solo compila y prueba.
- El frontend no tiene corredor de pruebas (no hay Jest ni Vitest) y no se agrega uno: su verificación es `npx tsc --noEmit`, `npx eslint` sobre los archivos tocados y `npm run build`. La lógica del backend sí lleva pruebas xUnit.
- Estilo del frontend: componentes de `components/ui`, `MainLayout`, textos en español, importes con `Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })` y fechas calendario como `yyyy-MM-dd` (`formatCalendarDate`), igual que las páginas existentes. Roles: `UserType.SA = 1`, `UserType.Admin = 2`.
- `/cuenta` solo para Admin; `/configuraciones/cuentas` solo para SA. Los roles restringidos (Normal) nunca ven consumo, pagos ni cuenta.
- El cupo es informativo: la interfaz solo muestra una alerta, nunca bloquea nada.
- Fuera de esta fase: el campo de cupo dentro del formulario de cliente (el cupo se edita en `/cuenta`), recordatorio manual por cuenta, y los riesgos abiertos de la fase 3 (`CertificationController` sin `[Authorize]`, `FeController` anónimo).
- Mensajes de commit en español, con las líneas `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` y `Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V` al final. El backend es un repo (`ZynstormECFPlatform`, rama `dev`) y el frontend otro (`ZynstormECFPlatform-FrontEnd`): cada commit se hace en el repo que corresponde, imprimiendo la rama con `git branch --show-current`. Confirma las ramas con el usuario antes del Task 1.

## Estructura de archivos

| Archivo | Repo | Acción | Responsabilidad |
|---|---|---|---|
| `ZynstormECFPlatform.Services/Billing/AccountUsageReportBuilder.cs` | backend | Crear | Fila de consumo de una cuenta en el formato del reporte |
| `ZynstormECFPlatform.Services/Billing/IBillingAccountViewService.cs`, `BillingAccountViewService.cs` | backend | Modificar | `GetMonthlyUsageAsync` |
| `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`, `ClientDtos.cs` | backend | Modificar | `IsAccount`, `BillingAccountId` |
| `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`, `ClientController.cs` | backend | Modificar | `GET BillingAccount/usage`; filtro del reporte de clientes |
| `types/billingAccount.type.ts`, `services/billingAccount.service.ts` | frontend | Crear | Tipos y llamadas del API de cuentas |
| `types/paymentRegistration.type.ts`, `services/payment.service.ts`, `lib/payment.ts` | frontend | Modificar | `PaymentTarget` y funciones por objetivo |
| `components/payments/register-payment-dialog.tsx`, `payment-history-dialog.tsx`, `payment-receipt-dialog.tsx` | frontend | Modificar | Reciben un `PaymentTarget` |
| `components/payments/account-payments-card.tsx` | frontend | Crear | Tabla de cuentas con registrar pago e historial |
| `app/pagos/page.tsx`, `app/consumo/page.tsx` | frontend | Modificar | Muestran cuentas |
| `app/cuenta/page.tsx` | frontend | Crear | "Mi cuenta" del Admin |
| `app/configuraciones/cuentas/page.tsx` | frontend | Crear | Administración de cuentas del SA |
| `constants/page-access.ts`, `components/sidebar.tsx` | frontend | Modificar | Rutas y menú |
| `app/clientes/page.tsx`, `types/client.type.ts` | frontend | Modificar | Aviso de plan no aplicable |

Los comandos del backend se corren desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`; los del frontend, desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.

---

### Task 1: Soporte de backend

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs` (`IsAccount`), `ZynstormECFPlatform.Dtos/ClientDtos.cs` (`BillingAccountId`)
- Create: `ZynstormECFPlatform.Services/Billing/AccountUsageReportBuilder.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/IBillingAccountViewService.cs`, `BillingAccountViewService.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`, `ClientController.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/AccountUsageReportBuilderTests.cs`

**Interfaces:**
- Produces:
  - `ClientMonthlyUsageDto.IsAccount` (`bool`) y `ClientViewDto.BillingAccountId` (`int?`).
  - `AccountUsageReportBuilder.Build(BillingAccount account, BillingAccountMonthlyUsage usage, IEnumerable<PlanOverageTier> tiers)` → `ClientMonthlyUsageDto` con `ClientGuidId` = GUID de la cuenta, `ClientName` = nombre de la cuenta, `ClientRnc` = nombre del dueño, `IsAccount = true`.
  - `IBillingAccountViewService.GetMonthlyUsageAsync(int year, int month, CancellationToken)` → `List<ClientMonthlyUsageDto>`.
  - `GET v1/BillingAccount/usage?year=&month=` (SA) → `List<ClientMonthlyUsageDto>`.
- Cambia: `GET v1/Client/usage` ya no incluye las filas de detalle de los clientes miembros de una cuenta (las que tienen `PlanId` nulo): esas filas son solo detalle y cobrarían 0.

- [ ] **Step 1: Escribir las pruebas (fallan: no existe el constructor)**

`ZynstormECFPlatform.Tests/Billing/AccountUsageReportBuilderTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class AccountUsageReportBuilderTests
{
    private static readonly PlanOverageTier[] Tiers =
    [
        new() { PlanId = 5, FromUnit = 1, ToUnit = 100, UnitPrice = 6.00m },
        new() { PlanId = 5, FromUnit = 101, ToUnit = null, UnitPrice = 9.00m }
    ];

    private static BillingAccount Account(DateTime nextPayment, decimal? customFee = null) => new()
    {
        GuidId = "acct-guid",
        Name = "Mi Cuenta",
        OwnerUser = new User { FirstName = "Ana", LastName = "Pérez" },
        PaidMonths = 1,
        CustomMonthlyFee = customFee,
        NextPaymentDate = nextPayment
    };

    private static BillingAccountMonthlyUsage Usage(int accepted = 650) => new()
    {
        BillingAccountId = 3,
        Year = 2026,
        Month = 11,
        AcceptedDocuments = accepted,
        PlanId = 5,
        PlanName = "Pool 500",
        MonthlyFee = 3000m,
        MonthlyDocumentLimit = 500
    };

    [Fact]
    public void Row_IdentifiesTheAccountAndItsOwner()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(), Tiers);

        Assert.True(row.IsAccount);
        Assert.Equal("acct-guid", row.ClientGuidId);
        Assert.Equal("Mi Cuenta", row.ClientName);
        Assert.Equal("Ana Pérez", row.ClientRnc);
        Assert.Equal("Pool 500", row.PlanName);
        Assert.Equal(1, row.PlanTypeId);
    }

    [Fact]
    public void PoolOverage_IsChargedByTiers()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(650), Tiers);

        Assert.Equal(650, row.AcceptedDocuments);
        Assert.Equal(150, row.OverageDocuments);
        Assert.Equal(1050m, row.OverageAmount);
        Assert.Equal(2, row.Tiers.Count);
    }

    [Fact]
    public void FeeNotYetCovered_IsChargedAndAddedToTheTotal()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(650), Tiers);

        Assert.False(row.MonthlyFeeCovered);
        Assert.Equal(3000m, row.MonthlyFeeCharged);
        Assert.Equal(4050m, row.Total);
    }

    [Fact]
    public void FeeCoveredByAnAdvancePayment_IsNotChargedButOverageStillIs()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 12, 15)), Usage(650), Tiers);

        Assert.True(row.MonthlyFeeCovered);
        Assert.Equal(0m, row.MonthlyFeeCharged);
        Assert.Equal(1050m, row.Total);
    }

    [Fact]
    public void CustomFee_ReplacesTheSnapshotFee()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15), customFee: 2500m), Usage(400), Tiers);

        Assert.Equal(2500m, row.MonthlyFee);
        Assert.Equal(2500m, row.Total);
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~AccountUsageReportBuilderTests" 2>&1 | grep -E " error " | sort -u | head -3`
Expected: error de compilación (`AccountUsageReportBuilder` no existe, `IsAccount` no existe).

- [ ] **Step 3: DTOs**

En `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`, reemplazar:

```csharp
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }
```

por:

```csharp
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }
    /// <summary>True si la fila es una cuenta de facturación: entonces los campos Client* son los de la cuenta (RNC = nombre del dueño).</summary>
    public bool IsAccount { get; set; }
```

En `ZynstormECFPlatform.Dtos/ClientDtos.cs`, dentro de `ClientViewDto`, reemplazar:

```csharp
public class ClientViewDto : ClientUpdateDto
{
    public bool IsCertified { get; set; }
```

por:

```csharp
public class ClientViewDto : ClientUpdateDto
{
    public bool IsCertified { get; set; }

    /// <summary>Cuenta de facturación a la que pertenece. Null = cliente suelto.</summary>
    public int? BillingAccountId { get; set; }
```

(AutoMapper la llena por nombre desde `Client.BillingAccountId`.)

- [ ] **Step 4: Crear el constructor de la fila**

`ZynstormECFPlatform.Services/Billing/AccountUsageReportBuilder.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>
/// Fila del reporte de consumo mensual para una cuenta de facturación, en el mismo formato que la de un cliente
/// para que la pantalla de consumo las muestre juntas. Función pura.
/// </summary>
public static class AccountUsageReportBuilder
{
    public static ClientMonthlyUsageDto Build(
        BillingAccount account,
        BillingAccountMonthlyUsage usage,
        IEnumerable<PlanOverageTier> tiers)
    {
        // El precio propio de la cuenta reemplaza la copia del precio del plan guardada al crear el mes.
        var fee = account.CustomMonthlyFee ?? usage.MonthlyFee;

        var calculation = BillingCalculator.Calculate(
            fee,
            usage.MonthlyDocumentLimit,
            tiers.Where(t => t.PlanId == usage.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
            usage.AcceptedDocuments);

        // El pago adelantado cubre solo la mensualidad; el excedente se cobra siempre.
        var payment = PaymentCalculator.Calculate(fee, account.PaidMonths, account.PrepaymentDiscountPercent);
        var feeCharged = PaymentCalculator.GetAmountForMonth(payment, account.NextPaymentDate, usage.Year, usage.Month);

        return new ClientMonthlyUsageDto
        {
            ClientGuidId = account.GuidId,
            ClientName = account.Name,
            ClientRnc = account.OwnerUser?.FullName ?? string.Empty,
            ClientInactive = false,
            IsAccount = true,
            Year = usage.Year,
            Month = usage.Month,
            PlanName = usage.PlanName,
            PlanTypeId = (int)PlanTypeEnum.Documents,
            MonthlyFee = calculation.MonthlyFee,
            MonthlyDocumentLimit = calculation.MonthlyDocumentLimit,
            AcceptedDocuments = calculation.AcceptedDocuments,
            OverageDocuments = calculation.OverageDocuments,
            OverageAmount = calculation.OverageAmount,
            MonthlyFeeCharged = feeCharged,
            MonthlyFeeCovered = PaymentCalculator.IsMonthCovered(account.NextPaymentDate, usage.Year, usage.Month),
            Total = feeCharged + calculation.OverageAmount,
            Tiers = calculation.Tiers.Select(t => new OverageTierChargeDto
            {
                FromUnit = t.FromUnit,
                ToUnit = t.ToUnit,
                UnitPrice = t.UnitPrice,
                Units = t.Units,
                Amount = t.Amount
            }).ToList()
        };
    }
}
```

- [ ] **Step 5: Servicio y endpoint**

En `IBillingAccountViewService.cs`, reemplazar:

```csharp
    /// <summary>GUID de la cuenta de la que el usuario es dueño; null si no tiene.</summary>
```

por:

```csharp
    /// <summary>Consumo del mes de todas las cuentas, en el formato del reporte de clientes.</summary>
    Task<List<ClientMonthlyUsageDto>> GetMonthlyUsageAsync(int year, int month, CancellationToken cancellationToken = default);

    /// <summary>GUID de la cuenta de la que el usuario es dueño; null si no tiene.</summary>
```

En `BillingAccountViewService.cs`, reemplazar:

```csharp
    public async Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
```

por:

```csharp
    public async Task<List<ClientMonthlyUsageDto>> GetMonthlyUsageAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var usages = await usageService.Table.AsNoTracking()
            .Where(u => u.Year == year && u.Month == month)
            .ToListAsync(cancellationToken);
        if (usages.Count == 0) return [];

        var accountIds = usages.Select(u => u.BillingAccountId).Distinct().ToList();
        var accounts = await accountService.Table.AsNoTracking()
            .Include(a => a.OwnerUser)
            .Where(a => accountIds.Contains(a.BillingAccountId))
            .ToDictionaryAsync(a => a.BillingAccountId, cancellationToken);

        var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
        var tiers = await tierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        return usages
            .Where(u => accounts.ContainsKey(u.BillingAccountId))
            .Select(u => AccountUsageReportBuilder.Build(accounts[u.BillingAccountId], u, tiers))
            .OrderBy(r => r.ClientName)
            .ToList();
    }

    public async Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
```

En `BillingAccountController.cs`, reemplazar:

```csharp
    [HttpGet("guid/{guid}")]
    public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
```

por:

```csharp
    [HttpGet("usage")]
    public async Task<IActionResult> GetMonthlyUsage([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
    {
        if (year < 2000 || month is < 1 or > 12)
            return BadRequest("Año o mes inválido.");

        try
        {
            return Ok(await viewService.GetMonthlyUsageAsync(year, month, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el consumo mensual de las cuentas");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}")]
    public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
```

En `ClientController.cs`, dentro de `GetMonthlyUsage`, reemplazar:

```csharp
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month);
```

por:

```csharp
                // Las filas de detalle de los miembros de una cuenta (sin plan) no se cobran: el cobro sale de la cuenta.
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month)
                    .Where(u => u.Client.BillingAccountId == null || u.PlanId != null);
```

- [ ] **Step 6: Compilar y correr**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~AccountUsageReportBuilderTests" 2>&1 | grep -E "Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Passed!  - Failed: 0, Passed: 5`.

- [ ] **Step 7: Commit (backend)**

```bash
git branch --show-current && git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Services ZynstormECFPlatform.Web.Api/Controllers ZynstormECFPlatform.Tests/Billing/AccountUsageReportBuilderTests.cs && git commit -q -m "$(cat <<'EOF'
Consumo mensual de cuentas en el reporte y billingAccountId en el cliente

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 2: Tipos, servicios y diálogos de pago por objetivo (frontend)

**Files:**
- Create: `types/billingAccount.type.ts`, `services/billingAccount.service.ts`
- Modify: `types/paymentRegistration.type.ts`, `types/client.type.ts`, `types/usage.type.ts`, `services/payment.service.ts`, `services/usage.service.ts`, `lib/payment.ts`
- Modify: `components/payments/register-payment-dialog.tsx`, `payment-history-dialog.tsx`, `payment-receipt-dialog.tsx`
- Modify: `app/pagos/page.tsx` (adaptar a los diálogos)

**Interfaces:**
- Produces:
  - Tipos `BillingAccount`, `BillingAccountDetail`, `BillingAccountClientUsage`, `BillingAccountSave`; `PaymentTarget { kind: "client" | "account" | "own-account"; guidId: string; name: string; subtitle: string }`.
  - Servicios: `getBillingAccounts()`, `getBillingAccount(guid)`, `createBillingAccount(dto)`, `updateBillingAccount(guid, dto)`, `setBillingAccountClients(guid, clientGuidIds)`, `getMyBillingAccount()`, `setMyClientQuota(clientGuid, quota)`; en `payment.service.ts`: `getPreviewFor(target)`, `registerFor(target, request)`, `getHistoryFor(target)`; `getMonthlyUsage(year, month)` ahora devuelve clientes y cuentas.
  - Helpers de `lib/payment.ts`: `clientPaymentTarget(client)`, `accountPaymentTarget(account)`.
  - `RegisterPaymentDialog` y `PaymentHistoryDialog` reciben `target: PaymentTarget | null` en lugar de `client`.

- [ ] **Step 1: Tipos de cuentas**

`types/billingAccount.type.ts`:

```ts
import { PaymentStatus } from "@/types/payment.type"

export interface BillingAccountClientUsage {
  clientGuidId: string
  clientName: string
  clientRnc: string
  acceptedDocuments: number
  quotaDocuments: number | null
  quotaExceeded: boolean
}

export interface BillingAccount {
  guidId: string
  name: string
  ownerUserId: string
  ownerName: string
  ownerEmail: string | null
  planId: number
  planName: string
  monthlyDocumentLimit: number
  monthlyFee: number
  customMonthlyFee: number | null
  statusId: number
  clientsCount: number
  currentMonthAcceptedDocuments: number
  currentMonthOverageDocuments: number
  currentMonthOverageAmount: number
  paidMonths: number
  prepaymentDiscountPercent: number
  paymentGraceDays: number
  lastPaymentDate: string | null
  nextPaymentDate: string | null
  paymentStatus: PaymentStatus
  paymentDaysToDue: number | null
  paymentSuspended: boolean
  pendingOverageAmount: number
  pendingOverageMonths: number
}

export interface BillingAccountDetail extends BillingAccount {
  clients: BillingAccountClientUsage[]
}

export interface BillingAccountSave {
  name: string
  /** Solo al crear. */
  ownerUserId?: string
  planId: number
  customMonthlyFee: number | null
  paidMonths: number
  prepaymentDiscountPercent: number
  paymentGraceDays: number
  lastPaymentDate: string | null
  nextPaymentDate: string | null
}
```

- [ ] **Step 2: Servicio de cuentas**

`services/billingAccount.service.ts`:

```ts
import { get, post, put } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { BillingAccount, BillingAccountDetail, BillingAccountSave } from "@/types/billingAccount.type"

const ACCOUNT_URL = `${API_BASE_URL}/v1/BillingAccount`
const OWN_ACCOUNT_URL = `${API_BASE_URL}/v1/MyBillingAccount`

// --- SA ---

export const getBillingAccounts = async (): Promise<BillingAccount[]> => {
  return await get<BillingAccount[]>(ACCOUNT_URL)
}

export const getBillingAccount = async (guidId: string): Promise<BillingAccountDetail> => {
  return await get<BillingAccountDetail>(`${ACCOUNT_URL}/guid/${guidId}`)
}

export const createBillingAccount = async (dto: BillingAccountSave): Promise<BillingAccountDetail> => {
  return await post<BillingAccountDetail>(ACCOUNT_URL, dto)
}

export const updateBillingAccount = async (guidId: string, dto: BillingAccountSave): Promise<BillingAccountDetail> => {
  return await put<BillingAccountDetail>(`${ACCOUNT_URL}/guid/${guidId}`, dto)
}

/** Deja como miembros exactamente los clientes indicados. */
export const setBillingAccountClients = async (
  guidId: string,
  clientGuidIds: string[],
): Promise<BillingAccountDetail> => {
  return await put<BillingAccountDetail>(`${ACCOUNT_URL}/guid/${guidId}/clients`, { clientGuidIds })
}

// --- Admin dueño ---

/** La cuenta del usuario actual. Responde 404 (ApiError con status 404) si no tiene una. */
export const getMyBillingAccount = async (): Promise<BillingAccountDetail> => {
  return await get<BillingAccountDetail>(OWN_ACCOUNT_URL)
}

/** Fija el cupo informativo mensual de un cliente de la cuenta propia; null lo quita. */
export const setMyClientQuota = async (clientGuidId: string, quota: number | null): Promise<BillingAccountDetail> => {
  return await put<BillingAccountDetail>(`${OWN_ACCOUNT_URL}/clients/${clientGuidId}/quota`, { quota })
}
```

- [ ] **Step 3: `PaymentTarget` y campos nuevos en tipos existentes**

En `types/paymentRegistration.type.ts`, reemplazar:

```ts
export interface PendingOverage {
```

por:

```ts
/** A quién se le registra o consulta un pago: un cliente suelto, una cuenta (SA) o la cuenta propia (Admin dueño). */
export type PaymentTargetKind = "client" | "account" | "own-account"

export interface PaymentTarget {
  kind: PaymentTargetKind
  guidId: string
  name: string
  subtitle: string
}

export interface PendingOverage {
```

y reemplazar:

```ts
  clientName: string
  paymentDate: string | null
```

por:

```ts
  clientName: string
  /** True si el recibo es de una cuenta: entonces clientGuidId y clientName son los de la cuenta. */
  isAccount?: boolean
  paymentDate: string | null
```

En `types/client.type.ts`, reemplazar:

```ts
  planId?: number | null
  planName?: string | null
  planMonthlyFee?: number | null
```

por:

```ts
  planId?: number | null
  /** Cuenta de facturación a la que pertenece; null = cliente suelto. */
  billingAccountId?: number | null
  planName?: string | null
  planMonthlyFee?: number | null
```

En `types/usage.type.ts`, reemplazar:

```ts
  clientInactive: boolean
  year: number
```

por:

```ts
  clientInactive: boolean
  /** True si la fila es una cuenta de facturación (clientName es el nombre de la cuenta y clientRnc el del dueño). */
  isAccount?: boolean
  year: number
```

- [ ] **Step 4: Funciones de pago por objetivo**

Reemplazar el contenido completo de `services/payment.service.ts`:

```ts
import { get, post } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { ClientPayment, MessageResponse } from "@/types/payment.type"
import {
  PaymentPreview,
  PaymentReceipt,
  PaymentTarget,
  PendingOverage,
  PlanFeePreview,
  RegisterPaymentRequest,
} from "@/types/paymentRegistration.type"

const CLIENT_URL = `${API_BASE_URL}/v1/Client`
const ACCOUNT_URL = `${API_BASE_URL}/v1/BillingAccount`
const OWN_ACCOUNT_URL = `${API_BASE_URL}/v1/MyBillingAccount`

export const getPaymentClients = async (): Promise<ClientPayment[]> => {
  return await get<ClientPayment[]>(`${CLIENT_URL}/payments`)
}

export const sendPaymentReminder = async (clientGuidId: string): Promise<MessageResponse> => {
  return await post<MessageResponse>(`${CLIENT_URL}/guid/${clientGuidId}/payment-reminder/notify`)
}

export const sendPaymentSummary = async (): Promise<MessageResponse> => {
  return await post<MessageResponse>(`${CLIENT_URL}/payments/reminders/summary`)
}

export const getPaymentPreview = async (clientGuidId: string): Promise<PaymentPreview> => {
  return await get<PaymentPreview>(`${CLIENT_URL}/guid/${clientGuidId}/payments/preview`)
}

export const registerPayment = async (
  clientGuidId: string,
  request: RegisterPaymentRequest,
): Promise<PaymentReceipt> => {
  return await post<PaymentReceipt>(`${CLIENT_URL}/guid/${clientGuidId}/payments`, request)
}

export const getPaymentHistory = async (clientGuidId: string): Promise<PaymentReceipt[]> => {
  return await get<PaymentReceipt[]>(`${CLIENT_URL}/guid/${clientGuidId}/payments`)
}

// --- Por objetivo: cliente suelto, cuenta de facturación o cuenta propia ---

interface AccountPaymentPreviewResponse {
  accountGuidId: string
  accountName: string
  paymentSuspended: boolean
  planFee: PlanFeePreview | null
  pendingOverages: (Omit<PendingOverage, "clientMonthlyUsageId"> & { billingAccountMonthlyUsageId: number })[]
}

/** Vista previa del pago. La de una cuenta se adapta a la forma de la de un cliente para que el diálogo sea uno solo. */
export const getPreviewFor = async (target: PaymentTarget): Promise<PaymentPreview> => {
  if (target.kind === "client") return await getPaymentPreview(target.guidId)

  const raw = await get<AccountPaymentPreviewResponse>(`${ACCOUNT_URL}/guid/${target.guidId}/payments/preview`)

  return {
    clientGuidId: raw.accountGuidId,
    clientName: raw.accountName,
    clientRnc: "",
    paymentSuspended: raw.paymentSuspended,
    planFee: raw.planFee,
    pendingOverages: raw.pendingOverages.map(({ billingAccountMonthlyUsageId, ...rest }) => ({
      ...rest,
      clientMonthlyUsageId: billingAccountMonthlyUsageId,
    })),
  }
}

export const registerFor = async (target: PaymentTarget, request: RegisterPaymentRequest): Promise<PaymentReceipt> => {
  if (target.kind === "client") return await registerPayment(target.guidId, request)
  // El Admin dueño solo consulta: registrar pagos es del SA.
  return await post<PaymentReceipt>(`${ACCOUNT_URL}/guid/${target.guidId}/payments`, request)
}

export const getHistoryFor = async (target: PaymentTarget): Promise<PaymentReceipt[]> => {
  switch (target.kind) {
    case "client":
      return await getPaymentHistory(target.guidId)
    case "account":
      return await get<PaymentReceipt[]>(`${ACCOUNT_URL}/guid/${target.guidId}/payments`)
    case "own-account":
      return await get<PaymentReceipt[]>(`${OWN_ACCOUNT_URL}/payments`)
  }
}
```

Reemplazar el contenido completo de `services/usage.service.ts`:

```ts
import { get } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { ClientMonthlyUsage } from "@/types/usage.type"

const CLIENT_URL = `${API_BASE_URL}/v1/Client`
const ACCOUNT_URL = `${API_BASE_URL}/v1/BillingAccount`

/** Consumo del mes de los clientes sueltos y de las cuentas de facturación, juntos y ordenados por nombre. */
export const getMonthlyUsage = async (year: number, month: number): Promise<ClientMonthlyUsage[]> => {
  const [clients, accounts] = await Promise.all([
    get<ClientMonthlyUsage[]>(`${CLIENT_URL}/usage?year=${year}&month=${month}`),
    get<ClientMonthlyUsage[]>(`${ACCOUNT_URL}/usage?year=${year}&month=${month}`),
  ])

  return [...clients, ...accounts].sort((a, b) => a.clientName.localeCompare(b.clientName, "es"))
}

export const getClientMonthlyUsage = async (
  guidId: string,
  year: number,
  month: number,
): Promise<ClientMonthlyUsage> => {
  return await get<ClientMonthlyUsage>(`${CLIENT_URL}/guid/${guidId}/usage?year=${year}&month=${month}`)
}
```

- [ ] **Step 5: Helpers de objetivo en `lib/payment.ts`**

En `lib/payment.ts`, reemplazar:

```ts
import { PaymentStatus } from "@/types/payment.type"
```

por:

```ts
import { PaymentStatus } from "@/types/payment.type"
import { PaymentTarget } from "@/types/paymentRegistration.type"
```

y agregar al final del archivo:

```ts

export const clientPaymentTarget = (client: {
  clientGuidId: string
  clientName: string
  clientRnc: string
}): PaymentTarget => ({
  kind: "client",
  guidId: client.clientGuidId,
  name: client.clientName,
  subtitle: client.clientRnc,
})

export const accountPaymentTarget = (account: { guidId: string; name: string; ownerName: string }): PaymentTarget => ({
  kind: "account",
  guidId: account.guidId,
  name: account.name,
  subtitle: `Cuenta · ${account.ownerName}`,
})
```

- [ ] **Step 6: `RegisterPaymentDialog` recibe un objetivo**

En `components/payments/register-payment-dialog.tsx`, aplicar estos reemplazos exactos:

1. `import { getPaymentPreview, registerPayment } from "@/services/payment.service"` →
   `import { getPreviewFor, registerFor } from "@/services/payment.service"`
2. Borrar la línea `import { ClientPayment } from "@/types/payment.type"` y reemplazar `import { PaymentMethod, PaymentPreview, PaymentReceipt } from "@/types/paymentRegistration.type"` por
   `import { PaymentMethod, PaymentPreview, PaymentReceipt, PaymentTarget } from "@/types/paymentRegistration.type"`
3. `  client: ClientPayment | null\n  onClose: () => void\n  onRegistered: (receipt: PaymentReceipt) => void` →
   `  target: PaymentTarget | null\n  onClose: () => void\n  onRegistered: (receipt: PaymentReceipt) => void`
4. `export function RegisterPaymentDialog({ client, onClose, onRegistered }: RegisterPaymentDialogProps) {` →
   `export function RegisterPaymentDialog({ target, onClose, onRegistered }: RegisterPaymentDialogProps) {`
5. `    if (!client) return\n    let cancelled = false` → `    if (!target) return\n    let cancelled = false`
6. `    getPaymentPreview(client.clientGuidId)` → `    getPreviewFor(target)`
7. `  }, [client])` → `  }, [target])`
8. `    if (!client || !hasSelection) return` → `    if (!target || !hasSelection) return`
9. `      const receipt = await registerPayment(client.clientGuidId, {` → `      const receipt = await registerFor(target, {`
10. `    <Dialog open={Boolean(client)} onOpenChange={(open) => !open && !isSaving && onClose()}>` →
    `    <Dialog open={Boolean(target)} onOpenChange={(open) => !open && !isSaving && onClose()}>`
11. `              {client?.clientName} · {client?.clientRnc}` →
    `              {target?.name}{target?.subtitle ? ` · ${target.subtitle}` : ""}`

- [ ] **Step 7: `PaymentHistoryDialog` y `PaymentReceiptDialog`**

En `components/payments/payment-history-dialog.tsx`:

1. `import { getPaymentHistory } from "@/services/payment.service"` → `import { getHistoryFor } from "@/services/payment.service"`
2. Borrar la línea `import { ClientPayment } from "@/types/payment.type"`; reemplazar `import { PaymentReceipt } from "@/types/paymentRegistration.type"` por `import { PaymentReceipt, PaymentTarget } from "@/types/paymentRegistration.type"`
3. `  client: ClientPayment | null` → `  target: PaymentTarget | null`
4. `export function PaymentHistoryDialog({ client, onClose }: PaymentHistoryDialogProps) {` → `export function PaymentHistoryDialog({ target, onClose }: PaymentHistoryDialogProps) {`
5. `    if (!client) return` → `    if (!target) return`
6. `    getPaymentHistory(client.clientGuidId)` → `    getHistoryFor(target)`
7. `  }, [client])` → `  }, [target])`
8. `    <Dialog open={Boolean(client)} onOpenChange={(open) => !open && onClose()}>` → `    <Dialog open={Boolean(target)} onOpenChange={(open) => !open && onClose()}>`
9. `            {client?.clientName} · {client?.clientRnc}` → `            {target?.name}{target?.subtitle ? ` · ${target.subtitle}` : ""}`

En `components/payments/payment-receipt-dialog.tsx`, reemplazar el texto `El cliente fue reactivado y ya puede emitir comprobantes.` por la expresión
`{receipt.isAccount ? "La cuenta fue reactivada y sus clientes ya pueden emitir comprobantes." : "El cliente fue reactivado y ya puede emitir comprobantes."}`
y `El cliente sigue suspendido: el próximo pago todavía está vencido.` por
`{receipt.isAccount ? "La cuenta sigue suspendida: el próximo pago todavía está vencido." : "El cliente sigue suspendido: el próximo pago todavía está vencido."}`.
(Son líneas de texto dentro de JSX; al convertirlas en expresión, `receipt` ya existe dentro de ese bloque condicional porque el archivo usa `receipt?.` arriba; si el compilador marca `receipt` posiblemente nulo, usa `receipt?.isAccount`.)

- [ ] **Step 8: Adaptar `app/pagos/page.tsx`**

En `app/pagos/page.tsx`:

1. Reemplazar:
```tsx
import {
  formatCalendarDate,
  formatMonths,
```
por:
```tsx
import {
  clientPaymentTarget,
  formatCalendarDate,
  formatMonths,
```
2. Reemplazar `  const [historyClient, setHistoryClient] = useState<ClientPayment | null>(null)` por:
```tsx
  const [historyClient, setHistoryClient] = useState<ClientPayment | null>(null)
  // Objetivos memorizados: los diálogos recargan cuando cambia el objetivo, no en cada render.
  const payTarget = useMemo(() => (clientToPay ? clientPaymentTarget(clientToPay) : null), [clientToPay])
  const historyTarget = useMemo(() => (historyClient ? clientPaymentTarget(historyClient) : null), [historyClient])
```
3. Reemplazar `          client={clientToPay}` por `          target={payTarget}`.
4. Reemplazar `        <PaymentHistoryDialog client={historyClient} onClose={() => setHistoryClient(null)} />` por `        <PaymentHistoryDialog target={historyTarget} onClose={() => setHistoryClient(null)} />`.

- [ ] **Step 9: Verificar tipos**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -20`
Expected: sin errores. Si falla en `payment-receipt-dialog.tsx` por `receipt` nulo, aplica el `receipt?.isAccount` indicado.

- [ ] **Step 10: Commit (frontend)**

```bash
git branch --show-current && git add types services lib components/payments app/pagos/page.tsx && git commit -q -m "$(cat <<'EOF'
Diálogos de pago por objetivo y servicios de cuentas de facturación

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 3: Rutas, permisos y menú

**Files:**
- Modify: `constants/page-access.ts`, `components/sidebar.tsx`

**Interfaces:**
- Produces: `/cuenta` accesible solo para Admin; `/configuraciones/cuentas` solo para SA; ambas con ítem en el menú.

- [ ] **Step 1: Reglas de acceso**

En `constants/page-access.ts`, reemplazar:

```ts
export const SA_ONLY_PATHS = [
  "/certificacion",
  "/consumo",
  "/pagos",
  "/configuraciones/planes",
  "/configuraciones/seriales",
]
```

por:

```ts
export const SA_ONLY_PATHS = [
  "/certificacion",
  "/consumo",
  "/pagos",
  "/configuraciones/planes",
  "/configuraciones/seriales",
  "/configuraciones/cuentas",
]

// "Mi cuenta": la cuenta de facturación del Admin que la dirige. No se asigna a usuarios restringidos.
export const ADMIN_ONLY_PATHS = ["/cuenta"]
```

y reemplazar:

```ts
  if (SA_ONLY_PATHS.some((p) => matches(current, p))) return user?.userType === UserType.SA
  if (!isPageRestrictedRole(user?.userType)) return true
```

por:

```ts
  if (SA_ONLY_PATHS.some((p) => matches(current, p))) return user?.userType === UserType.SA
  if (ADMIN_ONLY_PATHS.some((p) => matches(current, p))) return user?.userType === UserType.Admin
  if (!isPageRestrictedRole(user?.userType)) return true
```

- [ ] **Step 2: Menú**

En `components/sidebar.tsx`, reemplazar:

```tsx
  Wallet,
  KeyRound,
```

por:

```tsx
  Wallet,
  KeyRound,
  Building2,
  PieChart,
```

y reemplazar:

```tsx
  { name: "Pagos", href: "/pagos", icon: Wallet },
  { name: "Planes", href: "/configuraciones/planes", icon: Layers },
```

por:

```tsx
  { name: "Pagos", href: "/pagos", icon: Wallet },
  { name: "Mi cuenta", href: "/cuenta", icon: PieChart },
  { name: "Cuentas", href: "/configuraciones/cuentas", icon: Building2 },
  { name: "Planes", href: "/configuraciones/planes", icon: Layers },
```

- [ ] **Step 3: Verificar tipos**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10`
Expected: sin errores. (`Building2` y `PieChart` existen en `lucide-react`.)

- [ ] **Step 4: Commit (frontend)**

```bash
git branch --show-current && git add constants components/sidebar.tsx && git commit -q -m "$(cat <<'EOF'
Rutas y menú de Mi cuenta (Admin) y Cuentas (SA)

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 4: Pagos — tarjeta de cuentas

**Files:**
- Create: `components/payments/account-payments-card.tsx`
- Modify: `app/pagos/page.tsx`

**Interfaces:**
- Consumes: `getBillingAccounts`, `accountPaymentTarget`, `RegisterPaymentDialog`, `PaymentHistoryDialog`, `PaymentReceiptDialog` (Task 2).
- Produces: `AccountPaymentsCard` (sin props): tabla de cuentas con pool del mes, próximo pago, estado y acciones "Registrar pago" e "Historial".

- [ ] **Step 1: Crear la tarjeta**

`components/payments/account-payments-card.tsx`:

```tsx
"use client"

import { useCallback, useEffect, useState } from "react"
import { Banknote, History, RefreshCw } from "lucide-react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { RegisterPaymentDialog } from "@/components/payments/register-payment-dialog"
import { PaymentReceiptDialog } from "@/components/payments/payment-receipt-dialog"
import { PaymentHistoryDialog } from "@/components/payments/payment-history-dialog"
import { getBillingAccounts } from "@/services/billingAccount.service"
import { BillingAccount } from "@/types/billingAccount.type"
import { PaymentReceipt, PaymentTarget } from "@/types/paymentRegistration.type"
import { UNLIMITED_DOCUMENTS } from "@/types/plan.type"
import {
  accountPaymentTarget,
  formatCalendarDate,
  paymentStatusBadgeClass,
  paymentStatusLabel,
} from "@/lib/payment"
import { cn } from "@/lib/utils"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

/** Cuentas de facturación (pool de comprobantes) con su seguimiento de pago. Solo SA. */
export function AccountPaymentsCard() {
  const [accounts, setAccounts] = useState<BillingAccount[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [payTarget, setPayTarget] = useState<PaymentTarget | null>(null)
  const [historyTarget, setHistoryTarget] = useState<PaymentTarget | null>(null)
  const [receipt, setReceipt] = useState<PaymentReceipt | null>(null)

  const load = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      setAccounts(await getBillingAccounts())
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudieron cargar las cuentas")
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    load()
  }, [load])

  const handleRegistered = async (registered: PaymentReceipt) => {
    setPayTarget(null)
    setReceipt(registered)
    await load()
  }

  // Sin cuentas no se muestra nada: la página de pagos queda como estaba.
  if (!isLoading && !error && accounts.length === 0) return null

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-3">
          <div>
            <CardTitle className="text-lg">Cuentas de facturación</CardTitle>
            <CardDescription>
              {accounts.length} cuenta{accounts.length === 1 ? "" : "s"} · el pago cubre a todos sus clientes
            </CardDescription>
          </div>
          <Button variant="outline" size="icon" onClick={load} disabled={isLoading} title="Actualizar cuentas">
            <RefreshCw className={isLoading ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
          </Button>
        </CardHeader>
        <CardContent className="overflow-x-auto">
          {error && (
            <div className="mb-3 rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              {error}
            </div>
          )}
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Cuenta</TableHead>
                <TableHead>Plan</TableHead>
                <TableHead className="text-center">Clientes</TableHead>
                <TableHead className="text-right">Pool del mes</TableHead>
                <TableHead className="text-right">Mensualidad</TableHead>
                <TableHead>Próximo pago</TableHead>
                <TableHead className="text-right">Acciones</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell colSpan={7} className="h-20 text-center text-muted-foreground">
                    Cargando cuentas...
                  </TableCell>
                </TableRow>
              ) : (
                accounts.map((account) => (
                  <TableRow key={account.guidId}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <span className="font-medium">{account.name}</span>
                        {account.paymentSuspended && (
                          <Badge className="bg-red-100 text-red-800 hover:bg-red-100">Suspendida</Badge>
                        )}
                      </div>
                      <span className="text-xs text-muted-foreground">{account.ownerName}</span>
                    </TableCell>
                    <TableCell>{account.planName}</TableCell>
                    <TableCell className="text-center">{account.clientsCount}</TableCell>
                    <TableCell className="text-right whitespace-nowrap">
                      <span className="font-medium">
                        {account.currentMonthAcceptedDocuments.toLocaleString("es-DO")}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {" / "}
                        {account.monthlyDocumentLimit === UNLIMITED_DOCUMENTS
                          ? "ilimitado"
                          : account.monthlyDocumentLimit.toLocaleString("es-DO")}
                      </span>
                      {account.pendingOverageAmount > 0 && (
                        <span
                          className="block text-xs font-medium text-amber-700"
                          title={`${account.pendingOverageMonths} mes${account.pendingOverageMonths === 1 ? "" : "es"} con excedente sin pagar`}
                        >
                          Excedente pendiente: {currency.format(account.pendingOverageAmount)}
                        </span>
                      )}
                    </TableCell>
                    <TableCell className="text-right whitespace-nowrap">
                      {currency.format(account.customMonthlyFee ?? account.monthlyFee)}
                    </TableCell>
                    <TableCell className="whitespace-nowrap">
                      <div className="flex flex-col items-start gap-1">
                        <span className="text-xs text-muted-foreground">{formatCalendarDate(account.nextPaymentDate)}</span>
                        <Badge className={cn("font-normal", paymentStatusBadgeClass(account.paymentStatus))}>
                          {paymentStatusLabel(account.paymentStatus, account.paymentDaysToDue)}
                        </Badge>
                      </div>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="flex justify-end gap-2">
                        <Button size="sm" className="gap-1" onClick={() => setPayTarget(accountPaymentTarget(account))}>
                          <Banknote className="h-3.5 w-3.5" />
                          Registrar pago
                        </Button>
                        <Button
                          variant="outline"
                          size="icon"
                          className="h-8 w-8"
                          title="Historial de pagos"
                          onClick={() => setHistoryTarget(accountPaymentTarget(account))}
                        >
                          <History className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <RegisterPaymentDialog target={payTarget} onClose={() => setPayTarget(null)} onRegistered={handleRegistered} />
      <PaymentReceiptDialog receipt={receipt} onClose={() => setReceipt(null)} />
      <PaymentHistoryDialog target={historyTarget} onClose={() => setHistoryTarget(null)} />
    </>
  )
}
```

- [ ] **Step 2: Mostrarla en `app/pagos/page.tsx`**

Reemplazar:

```tsx
import { RegisterPaymentDialog } from "@/components/payments/register-payment-dialog"
```

por:

```tsx
import { AccountPaymentsCard } from "@/components/payments/account-payments-card"
import { RegisterPaymentDialog } from "@/components/payments/register-payment-dialog"
```

y reemplazar:

```tsx
        <AlertDialog open={Boolean(clientToRemind)} onOpenChange={(open) => !open && !isSending && setClientToRemind(null)}>
```

por:

```tsx
        <AccountPaymentsCard />

        <AlertDialog open={Boolean(clientToRemind)} onOpenChange={(open) => !open && !isSending && setClientToRemind(null)}>
```

(La tarjeta solo se muestra para SA porque toda la página ya está limitada a SA.)

- [ ] **Step 3: Verificar**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10` y `npx eslint components/payments/account-payments-card.tsx app/pagos/page.tsx 2>&1 | tail -15`
Expected: sin errores de tipos; ESLint sin errores nuevos (los avisos que ya tenía `pagos/page.tsx` no cuentan).

- [ ] **Step 4: Commit (frontend)**

```bash
git branch --show-current && git add components/payments/account-payments-card.tsx app/pagos/page.tsx && git commit -q -m "$(cat <<'EOF'
Pagos: tarjeta de cuentas de facturación con registro de pago e historial

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 5: Consumo — filas de cuentas

**Files:**
- Modify: `app/consumo/page.tsx`

**Interfaces:**
- Consumes: `getMonthlyUsage` ya devuelve clientes y cuentas (Task 2); `ClientMonthlyUsage.isAccount`.

- [ ] **Step 1: Marcar las filas de cuenta**

En `app/consumo/page.tsx`, reemplazar:

```tsx
                              {row.clientInactive && <Badge variant="destructive">Inactivo</Badge>}
```

por:

```tsx
                              {row.isAccount && <Badge variant="secondary">Cuenta</Badge>}
                              {row.clientInactive && <Badge variant="destructive">Inactivo</Badge>}
```

y reemplazar la descripción del encabezado:

```tsx
              Comprobantes aceptados y renta del mes por cliente con plan activo, con el monto a facturar.
```

por:

```tsx
              Comprobantes aceptados y renta del mes por cliente con plan activo y por cuenta de facturación (el pool
              de todos sus clientes), con el monto a facturar.
```

y reemplazar `            <CardDescription>{rows.length} clientes por facturar</CardDescription>` por:

```tsx
            <CardDescription>{rows.length} por facturar (clientes y cuentas)</CardDescription>
```

- [ ] **Step 2: Verificar**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10`
Expected: sin errores.

- [ ] **Step 3: Commit (frontend)**

```bash
git branch --show-current && git add app/consumo/page.tsx && git commit -q -m "$(cat <<'EOF'
Consumo: mostrar las cuentas de facturación junto a los clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 6: Página "Mi cuenta" (`/cuenta`)

**Files:**
- Create: `app/cuenta/page.tsx`

**Interfaces:**
- Consumes: `getMyBillingAccount`, `setMyClientQuota` (Task 2), `PaymentHistoryDialog` con `target.kind = "own-account"`.
- Produces: la página del Admin dueño: barra de uso del pool, excedente proyectado, consumo y cupo por cliente con alerta, y el historial de pagos (solo lectura).

- [ ] **Step 1: Crear la página**

`app/cuenta/page.tsx`:

```tsx
"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { History, RefreshCw, Save } from "lucide-react"
import { MainLayout } from "@/components/main-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Progress } from "@/components/ui/progress"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { PaymentHistoryDialog } from "@/components/payments/payment-history-dialog"
import { getMyBillingAccount, setMyClientQuota } from "@/services/billingAccount.service"
import { BillingAccountDetail } from "@/types/billingAccount.type"
import { PaymentTarget } from "@/types/paymentRegistration.type"
import { UNLIMITED_DOCUMENTS } from "@/types/plan.type"
import { ApiError } from "@/lib/api-error"
import { formatCalendarDate, paymentStatusBadgeClass, paymentStatusLabel } from "@/lib/payment"
import { cn } from "@/lib/utils"
import { useAuthStore } from "@/store/authStore"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

export default function MiCuentaPage() {
  const user = useAuthStore((state) => state.user)
  const isAdmin = user?.userType === 2

  const [account, setAccount] = useState<BillingAccountDetail | null>(null)
  const [hasNoAccount, setHasNoAccount] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [quotaDrafts, setQuotaDrafts] = useState<Record<string, string>>({})
  const [savingQuota, setSavingQuota] = useState<string | null>(null)
  const [historyTarget, setHistoryTarget] = useState<PaymentTarget | null>(null)

  const load = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      setAccount(await getMyBillingAccount())
      setHasNoAccount(false)
      setQuotaDrafts({})
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setAccount(null)
        setHasNoAccount(true)
      } else {
        setError(err instanceof Error ? err.message : "No se pudo cargar la cuenta")
      }
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isAdmin) load()
  }, [isAdmin, load])

  const pool = useMemo(() => {
    if (!account) return { unlimited: false, percent: 0, remaining: 0 }
    const limit = account.monthlyDocumentLimit
    const unlimited = limit === UNLIMITED_DOCUMENTS
    const percent = unlimited || limit <= 0 ? 0 : Math.min(100, Math.round((account.currentMonthAcceptedDocuments * 100) / limit))
    return { unlimited, percent, remaining: unlimited ? 0 : Math.max(0, limit - account.currentMonthAcceptedDocuments) }
  }, [account])

  const saveQuota = async (clientGuidId: string) => {
    const draft = quotaDrafts[clientGuidId]
    if (draft === undefined) return

    const value = draft.trim() === "" ? null : Number(draft)
    if (value !== null && (!Number.isInteger(value) || value < 0)) {
      setError("El cupo debe ser un número entero mayor o igual a 0 (vacío para quitarlo).")
      return
    }

    try {
      setSavingQuota(clientGuidId)
      setError(null)
      setAccount(await setMyClientQuota(clientGuidId, value))
      setQuotaDrafts((current) => {
        const next = { ...current }
        delete next[clientGuidId]
        return next
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo guardar el cupo")
    } finally {
      setSavingQuota(null)
    }
  }

  if (!isAdmin) {
    return (
      <MainLayout>
        <p className="text-muted-foreground">No tienes permisos para ver esta página.</p>
      </MainLayout>
    )
  }

  return (
    <MainLayout>
      <div className="space-y-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Mi cuenta</h1>
            <p className="text-muted-foreground">
              Los comprobantes de todos tus clientes suman en un mismo total mensual. Si el total pasa el límite del
              plan, el excedente se cobra.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            {account && (
              <Button
                variant="outline"
                className="gap-2"
                onClick={() =>
                  setHistoryTarget({ kind: "own-account", guidId: account.guidId, name: account.name, subtitle: "Mi cuenta" })
                }
              >
                <History className="h-4 w-4" />
                Historial de pagos
              </Button>
            )}
            <Button variant="outline" size="icon" onClick={load} disabled={isLoading} title="Actualizar">
              <RefreshCw className={isLoading ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            </Button>
          </div>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        {hasNoAccount && (
          <Card>
            <CardContent className="py-10 text-center">
              <p className="font-medium text-foreground">No tienes una cuenta de facturación</p>
              <p className="mt-1 text-sm text-muted-foreground">
                Tu plan de comprobantes compartido lo configura el administrador de Zynstorm. Contáctalo para activarlo.
              </p>
            </CardContent>
          </Card>
        )}

        {account && (
          <>
            {account.paymentSuspended && (
              <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-800">
                La cuenta está suspendida por falta de pago: ninguno de tus clientes puede emitir comprobantes hasta
                que se registre el pago.
              </div>
            )}

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">{account.name}</CardTitle>
                <CardDescription>
                  {account.planName} · {account.clientsCount} cliente{account.clientsCount === 1 ? "" : "s"}
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-end justify-between gap-3">
                  <div>
                    <p className="text-sm text-muted-foreground">Comprobantes del mes (todos tus clientes)</p>
                    <p className="text-3xl font-bold">
                      {account.currentMonthAcceptedDocuments.toLocaleString("es-DO")}
                      <span className="ml-2 text-base font-normal text-muted-foreground">
                        de {pool.unlimited ? "ilimitado" : account.monthlyDocumentLimit.toLocaleString("es-DO")}
                      </span>
                    </p>
                  </div>
                  {!pool.unlimited && (
                    <span className="text-sm text-muted-foreground">
                      {account.currentMonthOverageDocuments > 0
                        ? "Pasaste el límite del plan"
                        : `Te quedan ${pool.remaining.toLocaleString("es-DO")}`}
                    </span>
                  )}
                </div>
                {!pool.unlimited && (
                  <Progress
                    value={pool.percent}
                    className={cn(account.currentMonthOverageDocuments > 0 && "[&_[data-slot=progress-indicator]]:bg-destructive")}
                  />
                )}
              </CardContent>
            </Card>

            <div className="grid gap-4 sm:grid-cols-3">
              <Card>
                <CardContent className="pt-6">
                  <p className="text-sm text-muted-foreground">Excedente proyectado del mes</p>
                  <p className={cn("text-2xl font-bold", account.currentMonthOverageAmount > 0 && "text-destructive")}>
                    {currency.format(account.currentMonthOverageAmount)}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {account.currentMonthOverageDocuments.toLocaleString("es-DO")} comprobante
                    {account.currentMonthOverageDocuments === 1 ? "" : "s"} sobre el límite
                  </p>
                </CardContent>
              </Card>
              <Card>
                <CardContent className="pt-6">
                  <p className="text-sm text-muted-foreground">Mensualidad</p>
                  <p className="text-2xl font-bold">{currency.format(account.customMonthlyFee ?? account.monthlyFee)}</p>
                  {account.pendingOverageAmount > 0 && (
                    <p className="text-xs font-medium text-amber-700">
                      Excedente pendiente de pago: {currency.format(account.pendingOverageAmount)}
                    </p>
                  )}
                </CardContent>
              </Card>
              <Card>
                <CardContent className="pt-6">
                  <p className="text-sm text-muted-foreground">Próximo pago</p>
                  <p className="text-2xl font-bold">{formatCalendarDate(account.nextPaymentDate)}</p>
                  <Badge className={cn("mt-1 font-normal", paymentStatusBadgeClass(account.paymentStatus))}>
                    {paymentStatusLabel(account.paymentStatus, account.paymentDaysToDue)}
                  </Badge>
                </CardContent>
              </Card>
            </div>

            <Card>
              <CardHeader>
                <CardTitle className="text-lg">Consumo por cliente</CardTitle>
                <CardDescription>
                  El cupo es solo una guía para repartir el total: pasarlo muestra una alerta, pero el cliente sigue
                  emitiendo y el cobro se calcula sobre el total de la cuenta.
                </CardDescription>
              </CardHeader>
              <CardContent className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Cliente</TableHead>
                      <TableHead className="text-right">Aceptados este mes</TableHead>
                      <TableHead className="w-64">Cupo</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {account.clients.length === 0 ? (
                      <TableRow>
                        <TableCell colSpan={3} className="h-20 text-center text-muted-foreground">
                          Todavía no tienes clientes en la cuenta. Los que crees desde Clientes aparecerán aquí.
                        </TableCell>
                      </TableRow>
                    ) : (
                      account.clients.map((client) => {
                        const draft = quotaDrafts[client.clientGuidId]
                        const isDirty = draft !== undefined
                        return (
                          <TableRow key={client.clientGuidId}>
                            <TableCell>
                              <span className="font-medium">{client.clientName}</span>
                              <code className="block text-xs text-muted-foreground">{client.clientRnc}</code>
                            </TableCell>
                            <TableCell className="text-right whitespace-nowrap">
                              <span className="font-medium">{client.acceptedDocuments.toLocaleString("es-DO")}</span>
                              {client.quotaExceeded && (
                                <Badge className="ml-2 bg-amber-100 font-normal text-amber-800 hover:bg-amber-100">
                                  Pasó su cupo
                                </Badge>
                              )}
                            </TableCell>
                            <TableCell>
                              <div className="flex items-center gap-2">
                                <Input
                                  type="number"
                                  min={0}
                                  step={1}
                                  inputMode="numeric"
                                  placeholder="Sin cupo"
                                  className="w-32"
                                  value={isDirty ? draft : (client.quotaDocuments ?? "")}
                                  onChange={(e) =>
                                    setQuotaDrafts((current) => ({ ...current, [client.clientGuidId]: e.target.value }))
                                  }
                                />
                                <Button
                                  size="sm"
                                  variant="outline"
                                  className="gap-1"
                                  disabled={!isDirty || savingQuota === client.clientGuidId}
                                  onClick={() => saveQuota(client.clientGuidId)}
                                >
                                  <Save className="h-3.5 w-3.5" />
                                  {savingQuota === client.clientGuidId ? "Guardando..." : "Guardar"}
                                </Button>
                              </div>
                            </TableCell>
                          </TableRow>
                        )
                      })
                    )}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          </>
        )}

        <PaymentHistoryDialog target={historyTarget} onClose={() => setHistoryTarget(null)} />
      </div>
    </MainLayout>
  )
}
```

- [ ] **Step 2: Verificar**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10` y `npx eslint app/cuenta/page.tsx 2>&1 | tail -10`
Expected: sin errores. (`ApiError` se exporta de `lib/api-error.ts`; `Progress` de `components/ui/progress.tsx`.)

- [ ] **Step 3: Commit (frontend)**

```bash
git branch --show-current && git add app/cuenta && git commit -q -m "$(cat <<'EOF'
Página Mi cuenta: pool, consumo y cupo por cliente e historial de pagos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 7: Administración de cuentas del SA (`/configuraciones/cuentas`)

**Files:**
- Create: `app/configuraciones/cuentas/page.tsx`

**Interfaces:**
- Consumes: servicios de cuentas (Task 2), `getPlans`, `getUsers`, `getClients`.
- Produces: tabla de cuentas con alta, edición y asignación de clientes. Al crear, solo ofrece como dueño a usuarios Admin que aún no tienen cuenta; solo ofrece planes de comprobantes activos; los clientes que ya están en otra cuenta aparecen deshabilitados.

- [ ] **Step 1: Crear la página**

`app/configuraciones/cuentas/page.tsx`:

```tsx
"use client"

import { FormEvent, useCallback, useEffect, useMemo, useState } from "react"
import { Building2, Pencil, Plus, Users as UsersIcon } from "lucide-react"
import { MainLayout } from "@/components/main-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field"
import {
  createBillingAccount,
  getBillingAccount,
  getBillingAccounts,
  setBillingAccountClients,
  updateBillingAccount,
} from "@/services/billingAccount.service"
import { getPlans } from "@/services/plan.service"
import { getUsers } from "@/services/user.service"
import { getClients } from "@/services/client.service"
import { BillingAccount, BillingAccountSave } from "@/types/billingAccount.type"
import { Client } from "@/types/client.type"
import { Plan, PlanType, UNLIMITED_DOCUMENTS } from "@/types/plan.type"
import { User, UserType } from "@/types/auth.type"
import { formatCalendarDate } from "@/lib/payment"
import { useAuthStore } from "@/store/authStore"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

interface FormState {
  name: string
  ownerUserId: string
  planId: string
  customMonthlyFee: string
  paidMonths: string
  prepaymentDiscountPercent: string
  paymentGraceDays: string
  lastPaymentDate: string
  nextPaymentDate: string
}

const emptyForm: FormState = {
  name: "",
  ownerUserId: "",
  planId: "",
  customMonthlyFee: "",
  paidMonths: "1",
  prepaymentDiscountPercent: "0",
  paymentGraceDays: "3",
  lastPaymentDate: "",
  nextPaymentDate: "",
}

const toSavePayload = (form: FormState, isCreate: boolean): BillingAccountSave => ({
  name: form.name.trim(),
  ownerUserId: isCreate ? form.ownerUserId : undefined,
  planId: Number(form.planId),
  customMonthlyFee: form.customMonthlyFee.trim() === "" ? null : Number(form.customMonthlyFee),
  paidMonths: Number(form.paidMonths),
  prepaymentDiscountPercent: Number(form.prepaymentDiscountPercent),
  paymentGraceDays: Number(form.paymentGraceDays),
  lastPaymentDate: form.lastPaymentDate || null,
  nextPaymentDate: form.nextPaymentDate || null,
})

export default function CuentasPage() {
  const currentUser = useAuthStore((state) => state.user)
  const isSA = currentUser?.userType === UserType.SA

  const [accounts, setAccounts] = useState<BillingAccount[]>([])
  const [plans, setPlans] = useState<Plan[]>([])
  const [users, setUsers] = useState<User[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editing, setEditing] = useState<BillingAccount | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm)
  const [formError, setFormError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const [clientsAccount, setClientsAccount] = useState<BillingAccount | null>(null)
  const [allClients, setAllClients] = useState<Client[]>([])
  const [memberGuids, setMemberGuids] = useState<Set<string>>(new Set())
  const [selectedGuids, setSelectedGuids] = useState<Set<string>>(new Set())
  const [clientSearch, setClientSearch] = useState("")
  const [isLoadingClients, setIsLoadingClients] = useState(false)
  const [clientsError, setClientsError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      const [accountList, planList, userList] = await Promise.all([getBillingAccounts(), getPlans(), getUsers()])
      setAccounts(accountList)
      setPlans(planList)
      setUsers(userList)
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudieron cargar las cuentas")
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isSA) load()
  }, [isSA, load])

  const documentPlans = useMemo(
    () => plans.filter((p) => p.planTypeId === PlanType.Documents && p.isActive),
    [plans],
  )

  // Dueños posibles: Admin que todavía no tienen una cuenta (el API solo permite una por usuario).
  const availableOwners = useMemo(() => {
    const taken = new Set(accounts.map((a) => a.ownerUserId))
    return users.filter((u) => u.userType === UserType.Admin && !taken.has(u.userId))
  }, [users, accounts])

  const openCreate = () => {
    setEditing(null)
    setForm(emptyForm)
    setFormError(null)
    setIsFormOpen(true)
  }

  const openEdit = (account: BillingAccount) => {
    setEditing(account)
    setForm({
      name: account.name,
      ownerUserId: account.ownerUserId,
      planId: String(account.planId),
      customMonthlyFee: account.customMonthlyFee == null ? "" : String(account.customMonthlyFee),
      paidMonths: String(account.paidMonths),
      prepaymentDiscountPercent: String(account.prepaymentDiscountPercent),
      paymentGraceDays: String(account.paymentGraceDays),
      lastPaymentDate: account.lastPaymentDate ?? "",
      nextPaymentDate: account.nextPaymentDate ?? "",
    })
    setFormError(null)
    setIsFormOpen(true)
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!form.name.trim()) return setFormError("El nombre de la cuenta es requerido.")
    if (!editing && !form.ownerUserId) return setFormError("Selecciona el usuario dueño de la cuenta.")
    if (!form.planId) return setFormError("Selecciona un plan de comprobantes.")

    try {
      setIsSaving(true)
      setFormError(null)
      const payload = toSavePayload(form, !editing)
      if (editing) await updateBillingAccount(editing.guidId, payload)
      else await createBillingAccount(payload)
      setIsFormOpen(false)
      await load()
    } catch (err) {
      setFormError(err instanceof Error ? err.message : "No se pudo guardar la cuenta")
    } finally {
      setIsSaving(false)
    }
  }

  const openClients = async (account: BillingAccount) => {
    setClientsAccount(account)
    setClientsError(null)
    setClientSearch("")
    setIsLoadingClients(true)
    try {
      const [detail, clientsResult] = await Promise.all([getBillingAccount(account.guidId), getClients()])
      const clientList = Array.isArray(clientsResult) ? clientsResult : clientsResult.items
      const members = new Set(detail.clients.map((c) => c.clientGuidId))
      setAllClients(clientList)
      setMemberGuids(members)
      setSelectedGuids(new Set(members))
    } catch (err) {
      setClientsError(err instanceof Error ? err.message : "No se pudieron cargar los clientes")
    } finally {
      setIsLoadingClients(false)
    }
  }

  const saveClients = async () => {
    if (!clientsAccount) return
    try {
      setIsSaving(true)
      setClientsError(null)
      await setBillingAccountClients(clientsAccount.guidId, [...selectedGuids])
      setClientsAccount(null)
      await load()
    } catch (err) {
      setClientsError(err instanceof Error ? err.message : "No se pudieron asignar los clientes")
    } finally {
      setIsSaving(false)
    }
  }

  const toggleClient = (guidId: string, checked: boolean) => {
    setSelectedGuids((current) => {
      const next = new Set(current)
      if (checked) next.add(guidId)
      else next.delete(guidId)
      return next
    })
  }

  const visibleClients = useMemo(() => {
    const term = clientSearch.trim().toLowerCase()
    if (!term) return allClients
    return allClients.filter((c) => c.name.toLowerCase().includes(term) || c.rnc.includes(term))
  }, [allClients, clientSearch])

  if (!isSA) {
    return (
      <MainLayout>
        <p className="text-muted-foreground">No tienes permisos para ver esta página.</p>
      </MainLayout>
    )
  }

  return (
    <MainLayout>
      <div className="space-y-6">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Cuentas de facturación</h1>
            <p className="text-muted-foreground">
              Un plan de comprobantes compartido por todos los clientes de un revendedor. Los pagos se registran en{" "}
              <a href="/pagos" className="font-medium text-primary underline">
                Pagos
              </a>
              .
            </p>
          </div>
          <Button className="gap-2" onClick={openCreate}>
            <Plus className="h-4 w-4" />
            Nueva cuenta
          </Button>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <Building2 className="h-5 w-5" />
              Cuentas
            </CardTitle>
            <CardDescription>{accounts.length} cuenta{accounts.length === 1 ? "" : "s"}</CardDescription>
          </CardHeader>
          <CardContent className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Cuenta</TableHead>
                  <TableHead>Plan</TableHead>
                  <TableHead className="text-center">Clientes</TableHead>
                  <TableHead className="text-right">Pool del mes</TableHead>
                  <TableHead>Próximo pago</TableHead>
                  <TableHead className="text-right">Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                      Cargando cuentas...
                    </TableCell>
                  </TableRow>
                ) : accounts.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={6} className="h-24 text-center text-muted-foreground">
                      Todavía no hay cuentas. Crea una para un usuario Admin y asígnale sus clientes.
                    </TableCell>
                  </TableRow>
                ) : (
                  accounts.map((account) => (
                    <TableRow key={account.guidId}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{account.name}</span>
                          {account.paymentSuspended && (
                            <Badge className="bg-red-100 text-red-800 hover:bg-red-100">Suspendida</Badge>
                          )}
                        </div>
                        <span className="text-xs text-muted-foreground">
                          {account.ownerName}
                          {account.ownerEmail ? ` · ${account.ownerEmail}` : ""}
                        </span>
                      </TableCell>
                      <TableCell>
                        {account.planName}
                        <span className="block text-xs text-muted-foreground">
                          {currency.format(account.customMonthlyFee ?? account.monthlyFee)}/mes
                        </span>
                      </TableCell>
                      <TableCell className="text-center">{account.clientsCount}</TableCell>
                      <TableCell className="text-right whitespace-nowrap">
                        {account.currentMonthAcceptedDocuments.toLocaleString("es-DO")}
                        <span className="text-xs text-muted-foreground">
                          {" / "}
                          {account.monthlyDocumentLimit === UNLIMITED_DOCUMENTS
                            ? "ilimitado"
                            : account.monthlyDocumentLimit.toLocaleString("es-DO")}
                        </span>
                        {account.currentMonthOverageAmount > 0 && (
                          <span className="block text-xs font-medium text-amber-700">
                            Excedente: {currency.format(account.currentMonthOverageAmount)}
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                        {formatCalendarDate(account.nextPaymentDate)}
                      </TableCell>
                      <TableCell className="text-right">
                        <div className="flex justify-end gap-2">
                          <Button variant="outline" size="sm" className="gap-1" onClick={() => openClients(account)}>
                            <UsersIcon className="h-3.5 w-3.5" />
                            Clientes
                          </Button>
                          <Button variant="outline" size="icon" className="h-8 w-8" title="Editar" onClick={() => openEdit(account)}>
                            <Pencil className="h-3.5 w-3.5" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        {/* Alta y edición */}
        <Dialog open={isFormOpen} onOpenChange={(open) => !open && !isSaving && setIsFormOpen(false)}>
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
            <form onSubmit={handleSubmit}>
              <DialogHeader>
                <DialogTitle>{editing ? "Editar cuenta" : "Nueva cuenta"}</DialogTitle>
                <DialogDescription>
                  {editing
                    ? "El dueño de una cuenta no se puede cambiar."
                    : "Cada usuario Admin puede tener una sola cuenta."}
                </DialogDescription>
              </DialogHeader>

              <FieldGroup className="py-4">
                {formError && (
                  <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {formError}
                  </div>
                )}

                <Field>
                  <FieldLabel htmlFor="account-name">Nombre</FieldLabel>
                  <Input
                    id="account-name"
                    value={form.name}
                    maxLength={100}
                    onChange={(e) => setForm((c) => ({ ...c, name: e.target.value }))}
                  />
                </Field>

                {!editing && (
                  <Field>
                    <FieldLabel htmlFor="account-owner">Usuario dueño (Admin)</FieldLabel>
                    <Select value={form.ownerUserId} onValueChange={(v) => setForm((c) => ({ ...c, ownerUserId: v }))}>
                      <SelectTrigger id="account-owner">
                        <SelectValue placeholder={availableOwners.length ? "Selecciona un usuario" : "No hay Admin disponibles"} />
                      </SelectTrigger>
                      <SelectContent>
                        {availableOwners.map((u) => (
                          <SelectItem key={u.userId} value={u.userId}>
                            {u.firstName} {u.lastName} · {u.email}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </Field>
                )}

                <Field>
                  <FieldLabel htmlFor="account-plan">Plan de comprobantes</FieldLabel>
                  <Select value={form.planId} onValueChange={(v) => setForm((c) => ({ ...c, planId: v }))}>
                    <SelectTrigger id="account-plan">
                      <SelectValue placeholder="Selecciona un plan" />
                    </SelectTrigger>
                    <SelectContent>
                      {documentPlans.map((p) => (
                        <SelectItem key={p.planId} value={String(p.planId)}>
                          {p.name} · {currency.format(p.monthlyFee)}/mes ·{" "}
                          {p.monthlyDocumentLimit === UNLIMITED_DOCUMENTS
                            ? "ilimitado"
                            : `${p.monthlyDocumentLimit.toLocaleString("es-DO")} comprobantes`}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </Field>

                <div className="grid gap-4 sm:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="account-fee">Precio especial (RD$)</FieldLabel>
                    <Input
                      id="account-fee"
                      type="number"
                      min={0}
                      step="0.01"
                      placeholder="Precio del plan"
                      value={form.customMonthlyFee}
                      onChange={(e) => setForm((c) => ({ ...c, customMonthlyFee: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="account-months">Meses por pago</FieldLabel>
                    <Input
                      id="account-months"
                      type="number"
                      min={1}
                      max={60}
                      value={form.paidMonths}
                      onChange={(e) => setForm((c) => ({ ...c, paidMonths: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="account-discount">Descuento por adelanto (%)</FieldLabel>
                    <Input
                      id="account-discount"
                      type="number"
                      min={0}
                      max={100}
                      step="0.01"
                      value={form.prepaymentDiscountPercent}
                      onChange={(e) => setForm((c) => ({ ...c, prepaymentDiscountPercent: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="account-grace">Días de gracia</FieldLabel>
                    <Input
                      id="account-grace"
                      type="number"
                      min={1}
                      max={30}
                      value={form.paymentGraceDays}
                      onChange={(e) => setForm((c) => ({ ...c, paymentGraceDays: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="account-last">Último pago</FieldLabel>
                    <Input
                      id="account-last"
                      type="date"
                      value={form.lastPaymentDate}
                      onChange={(e) => setForm((c) => ({ ...c, lastPaymentDate: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="account-next">Próximo pago</FieldLabel>
                    <Input
                      id="account-next"
                      type="date"
                      value={form.nextPaymentDate}
                      onChange={(e) => setForm((c) => ({ ...c, nextPaymentDate: e.target.value }))}
                    />
                  </Field>
                </div>
              </FieldGroup>

              <DialogFooter>
                <Button type="button" variant="outline" disabled={isSaving} onClick={() => setIsFormOpen(false)}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={isSaving}>
                  {isSaving ? "Guardando..." : "Guardar"}
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>

        {/* Clientes de la cuenta */}
        <Dialog open={Boolean(clientsAccount)} onOpenChange={(open) => !open && !isSaving && setClientsAccount(null)}>
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
            <DialogHeader>
              <DialogTitle>Clientes de la cuenta</DialogTitle>
              <DialogDescription>
                {clientsAccount?.name}. Los clientes marcados facturan contra la cuenta (su plan propio deja de
                cobrarse); el dueño recibe acceso a ellos. Para asignar un cliente que ya está en otra cuenta, suéltalo
                primero de esa.
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-3 py-2">
              {clientsError && (
                <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                  {clientsError}
                </div>
              )}
              <Input
                placeholder="Buscar por nombre o RNC"
                value={clientSearch}
                onChange={(e) => setClientSearch(e.target.value)}
              />
              <div className="max-h-72 space-y-1 overflow-y-auto rounded-lg border p-2">
                {isLoadingClients ? (
                  <p className="p-2 text-sm text-muted-foreground">Cargando clientes…</p>
                ) : visibleClients.length === 0 ? (
                  <p className="p-2 text-sm text-muted-foreground">No hay clientes que coincidan.</p>
                ) : (
                  visibleClients.map((client) => {
                    const inOtherAccount = client.billingAccountId != null && !memberGuids.has(client.guidId)
                    return (
                      <label
                        key={client.guidId}
                        className="flex cursor-pointer items-center gap-3 rounded-md px-2 py-1.5 text-sm hover:bg-secondary/50 has-[:disabled]:cursor-not-allowed has-[:disabled]:opacity-50"
                      >
                        <Checkbox
                          checked={selectedGuids.has(client.guidId)}
                          disabled={inOtherAccount}
                          onCheckedChange={(checked) => toggleClient(client.guidId, checked === true)}
                        />
                        <span className="flex-1">
                          <span className="font-medium">{client.name}</span>
                          <code className="ml-2 text-xs text-muted-foreground">{client.rnc}</code>
                        </span>
                        {inOtherAccount && <Badge variant="secondary">En otra cuenta</Badge>}
                      </label>
                    )
                  })
                )}
              </div>
              <p className="text-xs text-muted-foreground">{selectedGuids.size} seleccionado(s)</p>
            </div>

            <DialogFooter>
              <Button variant="outline" disabled={isSaving} onClick={() => setClientsAccount(null)}>
                Cancelar
              </Button>
              <Button disabled={isSaving || isLoadingClients} onClick={saveClients}>
                {isSaving ? "Guardando..." : "Guardar"}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    </MainLayout>
  )
}
```

- [ ] **Step 2: Verificar**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -15` y `npx eslint app/configuraciones/cuentas/page.tsx 2>&1 | tail -10`
Expected: sin errores de tipos. Posibles ajustes si el compilador los pide:
- `clientsResult.items`: si `PaginatedResponse<Client>` usa otro nombre de campo para la lista, usa el real (`grep -n "PaginatedResponse" -A6 types/client.type.ts`).
- `u.userId`, `u.firstName`, `u.email`: vienen del `User` de `types/auth.type.ts` (zod); ya existen.
- Si `Checkbox.onCheckedChange` entrega `boolean | "indeterminate"`, el `checked === true` ya lo cubre.
- Reemplaza `<a href="/pagos">` por `Link` de `next/link` si ESLint (`@next/next/no-html-link-for-pages`) lo exige.

- [ ] **Step 3: Commit (frontend)**

```bash
git branch --show-current && git add app/configuraciones/cuentas && git commit -q -m "$(cat <<'EOF'
Administración de cuentas de facturación para el SA: alta, edición y clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 8: Formulario de cliente — aviso de plan no aplicable

**Files:**
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: `getMyBillingAccount` (Task 2) y `Client.billingAccountId`.
- Cambia: para un Admin dueño de cuenta, y para un cliente que ya pertenece a una cuenta, el selector de plan queda deshabilitado con un aviso: el plan propio no se cobra porque el cliente factura contra la cuenta (el API ya lo ignora).

- [ ] **Step 1: Estado y consulta de la cuenta propia**

En `app/clientes/page.tsx`, agregar el import junto a `import { getPlans } from "@/services/plan.service"`:

```tsx
import { getMyBillingAccount } from "@/services/billingAccount.service"
```

Reemplazar:

```tsx
  const isSA = user?.userType === 1
  const [searchQuery, setSearchQuery] = useState("")
```

por:

```tsx
  const isSA = user?.userType === 1
  const isAdmin = user?.userType === 2
  // Un Admin dueño de una cuenta de facturación crea clientes que facturan contra ella.
  const [ownsAccount, setOwnsAccount] = useState(false)
  const [searchQuery, setSearchQuery] = useState("")
```

Reemplazar:

```tsx
  useEffect(() => {
    getPlans()
      .then(setPlans)
      .catch(() => setPlans([]))
  }, [])
```

por:

```tsx
  useEffect(() => {
    getPlans()
      .then(setPlans)
      .catch(() => setPlans([]))
  }, [])

  useEffect(() => {
    if (!isAdmin) return
    // 404 = el Admin no tiene cuenta: sus clientes siguen con plan propio.
    getMyBillingAccount()
      .then(() => setOwnsAccount(true))
      .catch(() => setOwnsAccount(false))
  }, [isAdmin])
```

Reemplazar:

```tsx
  const selectedPlan = plans.find((p) => p.planId === formData.planId) ?? null
```

por:

```tsx
  const billedByAccount = ownsAccount || (editingClient?.billingAccountId ?? null) !== null
  const selectedPlan = plans.find((p) => p.planId === formData.planId) ?? null
```

- [ ] **Step 2: Deshabilitar el selector de plan y avisar**

Reemplazar:

```tsx
                      <Select
                        value={formData.planId ? String(formData.planId) : "none"}
```

por:

```tsx
                      <Select
                        disabled={billedByAccount}
                        value={formData.planId ? String(formData.planId) : "none"}
```

Reemplazar:

```tsx
                      {!plans.some((p) => p.planTypeId === PlanType.Rent) && (
```

por:

```tsx
                      {billedByAccount && (
                        <p className="text-xs text-muted-foreground">
                          Este cliente factura contra una cuenta de facturación: se cobra por el total de la cuenta y
                          su plan propio no aplica.
                        </p>
                      )}
                      {!billedByAccount && !plans.some((p) => p.planTypeId === PlanType.Rent) && (
```

- [ ] **Step 3: Verificar**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10` y `npx eslint app/clientes/page.tsx 2>&1 | tail -10`
Expected: sin errores nuevos.

- [ ] **Step 4: Commit (frontend)**

```bash
git branch --show-current && git add app/clientes/page.tsx && git commit -q -m "$(cat <<'EOF'
Clientes: avisar que el plan propio no aplica cuando se factura por una cuenta

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 9: Verificación final

**Files:** ninguno.

- [ ] **Step 1: Backend — compilar y suite completa**

Run (backend): `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u` y `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y la única falla es `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada` (ya fallaba antes de la fase 1).

- [ ] **Step 2: Frontend — tipos, lint y build**

Run (frontend): `npx tsc --noEmit 2>&1 | tail -10`, `npx eslint app/cuenta app/configuraciones/cuentas app/consumo app/pagos app/clientes components/payments constants services types lib 2>&1 | tail -30` y `npm run build 2>&1 | tail -25`
Expected: sin errores de tipos; ESLint sin errores en los archivos tocados (los avisos que ya existían no cuentan); `npm run build` termina con las rutas `/cuenta` y `/configuraciones/cuentas` en la lista. Si el build falla por variables de entorno ajenas a estos cambios (por ejemplo la URL del API), repórtalo con el mensaje exacto sin tocar la configuración.

- [ ] **Step 3: Revisar a mano las reglas de acceso**

Run (frontend): `grep -n "ADMIN_ONLY_PATHS\|SA_ONLY_PATHS" constants/page-access.ts`
Expected: `/cuenta` en `ADMIN_ONLY_PATHS` y `/configuraciones/cuentas` en `SA_ONLY_PATHS`; ambas comprobadas antes de `isPageRestrictedRole` en `canAccessPage`.

- [ ] **Step 4: Reportar al usuario**

Informar el resultado de la suite, `tsc`, `eslint` y `build`. Aclarar que **no se probó la interfaz en un navegador contra la API** (no hay base de desarrollo autorizada); la verificación real queda para cuando el usuario autorice dev2:

1. SA: crear una cuenta (Admin sin cuenta y plan de comprobantes), asignarle dos clientes, y ver que aparecen en `Cuentas`, `Consumo` (fila "Cuenta") y `Pagos` (tarjeta de cuentas).
2. Admin dueño: entrar a `Mi cuenta`, ver el pool y fijar un cupo; crear un cliente nuevo desde `Clientes` y comprobar que nace en la cuenta y el plan sale deshabilitado.
3. SA: registrar un pago de la cuenta desde `Pagos` y revisar el historial; con la cuenta suspendida, el Admin ve el aviso rojo.
4. Usuario Normal: no ve `Mi cuenta`, `Cuentas`, `Consumo` ni `Pagos`.

Recordar lo pendiente fuera de la fase: la verificación contra PostgreSQL de las fases 1 y 2, y los riesgos abiertos de la fase 3 (`CertificationController` sin `[Authorize]`, `FeController`) antes de dar rol Admin a un revendedor real.
