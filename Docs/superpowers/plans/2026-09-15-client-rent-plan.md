# Plan de renta con tope de usuarios — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Agregar un segundo tipo de plan, **Renta**, para clientes que pagan una renta fija y tienen un tope informativo de usuarios, con último pago, próximo pago, flag de año completo y descuento porcentual por pago adelantado sobre el cliente.

**Architecture:** `Plan` gana un discriminador `PlanTypeId` y `MaxUsers`; los cuatro datos de pago son columnas de `Client`. El cálculo vive en `RentCalculator`, una clase estática pura en `ZynstormECFPlatform.Services/Billing` hermana de `BillingCalculator` — nada de tablas nuevas ni jobs. `ClientController` expone `GET v1/Client/rent` y suma las filas de renta al reporte mensual existente. El frontend Next.js agrega el tipo al editor de planes, los campos de renta al cliente, la página `/renta` y las filas de renta en `/consumo`.

**Tech Stack:** .NET 10, EF Core + Npgsql (PostgreSQL), AutoMapper, xUnit. Frontend: Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-09-15-client-rent-plan-design.md`

## Global Constraints

- Repo backend: `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`. Frontend: `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.
- Rama de trabajo en ambos repos: `feature/client-plans-monthly-usage` (ya existe; este plan continúa sobre ella).
- Los comandos `dotnet` se ejecutan desde la raíz del repo backend.
- Build: `dotnet build ZynstormECFPlatform.slnx -v q`. Tests: `dotnet test ZynstormECFPlatform.Tests -v q`. Ambos deben quedar verdes al final de cada task backend.
- Migraciones: `dotnet ef migrations add <Nombre> --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`.
- Frontend: `npm run lint` y `npm run build` deben quedar verdes al final de cada task frontend. **No hay test runner en el frontend** — la verificación es lint + build.
- **No hay librería de mocking en `ZynstormECFPlatform.Tests`** (solo xUnit). Toda regla de negocio que deba tener test se extrae a una función pura; el cableado en servicios y controllers se verifica con build.
- `Plan.MonthlyDocumentLimit = -1` = ilimitado. `Plan.MaxUsers = -1` = usuarios ilimitados; cualquier otro valor debe ser `> 0`.
- Un plan de renta **fuerza** `MonthlyDocumentLimit = -1` al guardar (se sobreescribe, no se valida) y **rechaza** tramos de excedente.
- El descuento aplica al ciclo configurado, mensual o anual; **no** depende del check de año completo.
- `RentPaymentWarningDays` va en `AppSettings` con default 15 y en los tres `appsettings*.json`, junto a `CertificateExpirationWarningDays`.
- Los días se cuentan en hora RD: `DateTimeExtensions.DrNow` (namespace `ZynstormECFPlatform.Common`).
- Nombres de tabla en PostgreSQL = nombre de la clase, singular, entre comillas dobles: `"Plan"`, `"Client"`.
- **Los enums se serializan como número**: `Program.cs` no registra `JsonStringEnumConverter`. `RentStatus` llega al frontend como `0..3`.
- **Trampa de fechas:** `Program.cs` registra `DrDateTimeConverter` / `DrNullableDateTimeConverter` globalmente, que aplican `ToDrTime()` (UTC−4) a **todo** `DateTime` al serializar. Una fecha calendario guardada a las 00:00 saldría con el día anterior. Las cuatro fechas de renta usan `[JsonConverter(typeof(CalendarDateJsonConverter))]` a nivel de propiedad, que tiene precedencia sobre los convertidores globales.
- Identificadores en inglés, textos de UI y mensajes de error en español, igual que el código existente.
- Mapeo spec → código de los estados: `SinFecha` = `RentStatus.NoDate`, `AlDia` = `RentStatus.Current`, `PorVencer` = `RentStatus.DueSoon`, `Vencido` = `RentStatus.Overdue`.
- Renta y planes: solo rol `SA` (backend `[Authorize(Roles = "SA")]` o `if (!IsSA) return Forbid();`; frontend `user?.userType === 1`).

## File Structure

**Backend — crear**

| Archivo | Responsabilidad |
|---|---|
| `ZynstormECFPlatform.Core/Enums/PlanTypeEnum.cs` | Discriminador de tipo de plan |
| `ZynstormECFPlatform.Core/Enums/RentStatus.cs` | Estado del semáforo de renta |
| `ZynstormECFPlatform.Services/Billing/RentCalculator.cs` | Cálculo puro de renta, monto del mes, estado y validación del plan de renta |
| `ZynstormECFPlatform.Dtos/Converters/CalendarDateJsonConverter.cs` | Serializa fechas calendario como `yyyy-MM-dd` sin desplazamiento de zona |
| `ZynstormECFPlatform.Dtos/ClientRentDtos.cs` | `ClientRentDto`, fila del reporte de renta |
| `ZynstormECFPlatform.Tests/Billing/RentCalculatorTests.cs` | Tests de `RentCalculator` |

**Backend — modificar**

| Archivo | Cambio |
|---|---|
| `ZynstormECFPlatform.Core/Entities/Plan.cs` | `+ PlanTypeId`, `+ MaxUsers` |
| `ZynstormECFPlatform.Core/Entities/Client.cs` | `+` cuatro campos de renta |
| `ZynstormECFPlatform.Core/AppSettings.cs` | `+ RentPaymentWarningDays` |
| `ZynstormECFPlatform.Data/StorageContext.cs` | Configuración de las columnas nuevas |
| `ZynstormECFPlatform.Services/Billing/BillingCalculator.cs` | `+ AccruesDocumentUsage`, `+` overload de `ValidatePlan` con tipo de plan |
| `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs` | Sale temprano si el plan es de renta |
| `ZynstormECFPlatform.Dtos/PlanDtos.cs` | `+ PlanTypeId`, `+ MaxUsers` |
| `ZynstormECFPlatform.Dtos/ClientDtos.cs` | `+` campos de renta y de vista |
| `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs` | `+ PlanTypeId` |
| `ZynstormECFPlatform.Mappings/MappingProfiles.cs` | Mapear los campos nuevos |
| `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs` | Validación por tipo y forzado del límite |
| `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` | `FillRentStatusAsync`, `GET rent`, unión en `usage`, validación de fechas |
| `ZynstormECFPlatform.Web.Api/appsettings{,.Development,.Staging}.json` | `+ RentPaymentWarningDays: 15` |

**Frontend — crear**

| Archivo | Responsabilidad |
|---|---|
| `types/rent.type.ts` | `ClientRent`, `RentStatus`, `UNLIMITED_USERS` |
| `services/rent.service.ts` | `getRentClients()` |
| `lib/rent.ts` | `suggestNextPaymentDate`, formateadores y etiquetas de estado |
| `app/renta/page.tsx` | Página del reporte de renta |
| `components/rent-alert.tsx` | Aviso de rentas vencidas / por vencer, reusable en el dashboard |

**Frontend — modificar**

| Archivo | Cambio |
|---|---|
| `types/plan.type.ts` | `+ planTypeId`, `+ maxUsers`, `+ PlanType`, `+ UNLIMITED_USERS` |
| `types/client.type.ts` | `+` campos de renta |
| `types/usage.type.ts` | `+ planTypeId` |
| `app/configuraciones/planes/page.tsx` | Selector de tipo de plan |
| `app/clientes/page.tsx` | Campos de renta en el formulario, badge y contador en la tabla |
| `app/consumo/page.tsx` | Filas de renta |
| `app/page.tsx` | Aviso de renta para SA |
| `components/sidebar.tsx` | Item `Renta` |

---

### Task 1: `RentCalculator` — cálculo, monto del mes, semáforo y validación

**Files:**
- Create: `ZynstormECFPlatform.Core/Enums/PlanTypeEnum.cs`
- Create: `ZynstormECFPlatform.Core/Enums/RentStatus.cs`
- Create: `ZynstormECFPlatform.Services/Billing/RentCalculator.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/BillingCalculator.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/RentCalculatorTests.cs`

**Interfaces:**
- Consumes: `DateTimeExtensions.DrNow` de `ZynstormECFPlatform.Common`; `BillingCalculator.ValidatePlan(int, decimal, IEnumerable<OverageTier>)` y `OverageTier` ya existentes.
- Produces:
  - `enum PlanTypeEnum { Documents = 1, Rent = 2 }` en `ZynstormECFPlatform.Core.Enums`
  - `enum RentStatus { NoDate = 0, Current = 1, DueSoon = 2, Overdue = 3 }` en `ZynstormECFPlatform.Core.Enums`
  - `record RentCalculationResult(decimal MonthlyFee, int MonthsCovered, decimal GrossAmount, decimal DiscountPercent, decimal DiscountAmount, decimal Total)`
  - `RentCalculator.UnlimitedUsers` = `-1`, `RentCalculator.DefaultWarningDays` = `15`
  - `RentCalculator.Calculate(decimal monthlyFee, bool paidFullYear, decimal discountPercent) → RentCalculationResult`
  - `RentCalculator.GetAmountForMonth(RentCalculationResult result, DateTime? nextPaymentDate, int year, int month) → decimal`
  - `RentCalculator.GetRentStatus(DateTime? nextPaymentDate, int warningDays, DateTime? today = null) → (RentStatus Status, int? DaysToDue)`
  - `RentCalculator.ValidateRentPlan(decimal monthlyFee, int? maxUsers, bool hasOverageTiers) → List<string>`
  - `BillingCalculator.AccruesDocumentUsage(int planTypeId) → bool`
  - `BillingCalculator.ValidatePlan(PlanTypeEnum planType, int monthlyDocumentLimit, decimal monthlyFee, int? maxUsers, IEnumerable<OverageTier> tiers) → List<string>`

- [ ] **Step 1: Crear los dos enums**

`ZynstormECFPlatform.Core/Enums/PlanTypeEnum.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Enums;

public enum PlanTypeEnum
{
    /// <summary>Plan por comprobantes: límite mensual y cobro por tramos de excedente.</summary>
    Documents = 1,

    /// <summary>Plan de renta fija con tope informativo de usuarios.</summary>
    Rent = 2
}
```

`ZynstormECFPlatform.Core/Enums/RentStatus.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Enums;

/// <summary>Semáforo del próximo pago de renta. Se serializa como número.</summary>
public enum RentStatus
{
    NoDate = 0,
    Current = 1,
    DueSoon = 2,
    Overdue = 3
}
```

- [ ] **Step 2: Escribir los tests que fallan**

Crear `ZynstormECFPlatform.Tests/Billing/RentCalculatorTests.cs`:

```csharp
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class RentCalculatorTests
{
    [Fact]
    public void Calculate_MonthlyWithoutDiscount_ChargesOneMonth()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: false, discountPercent: 0m);

        Assert.Equal(1, result.MonthsCovered);
        Assert.Equal(2000m, result.GrossAmount);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(2000m, result.Total);
    }

    [Fact]
    public void Calculate_FullYearWithoutDiscount_ChargesTwelveMonths()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: true, discountPercent: 0m);

        Assert.Equal(12, result.MonthsCovered);
        Assert.Equal(24000m, result.GrossAmount);
        Assert.Equal(24000m, result.Total);
    }

    [Fact]
    public void Calculate_FullYearWithTenPercent_Is21600()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: true, discountPercent: 10m);

        Assert.Equal(24000m, result.GrossAmount);
        Assert.Equal(2400m, result.DiscountAmount);
        Assert.Equal(21600m, result.Total);
    }

    [Fact]
    public void Calculate_MonthlyWithDiscount_AppliesDiscountAnyway()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: false, discountPercent: 10m);

        Assert.Equal(1, result.MonthsCovered);
        Assert.Equal(1800m, result.Total);
    }

    [Fact]
    public void GetAmountForMonth_MonthAlreadyCovered_IsZero()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: true, discountPercent: 0m);

        // Consultamos septiembre 2026 y el próximo pago es junio 2027: el mes está cubierto.
        Assert.Equal(0m, RentCalculator.GetAmountForMonth(result, new DateTime(2027, 6, 10), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_RenewalMonth_ChargesFullCycle()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: true, discountPercent: 10m);

        Assert.Equal(21600m, RentCalculator.GetAmountForMonth(result, new DateTime(2026, 9, 20), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_OverdueDate_ChargesFullCycle()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: false, discountPercent: 0m);

        Assert.Equal(2000m, RentCalculator.GetAmountForMonth(result, new DateTime(2026, 7, 5), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_WithoutNextPaymentDate_ChargesFullCycle()
    {
        var result = RentCalculator.Calculate(2000m, paidFullYear: false, discountPercent: 0m);

        Assert.Equal(2000m, RentCalculator.GetAmountForMonth(result, null, 2026, 9));
    }

    [Fact]
    public void GetRentStatus_WithoutDate_IsNoDate()
    {
        var (status, days) = RentCalculator.GetRentStatus(null, 15);

        Assert.Equal(RentStatus.NoDate, status);
        Assert.Null(days);
    }

    [Fact]
    public void GetRentStatus_Yesterday_IsOverdue()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = RentCalculator.GetRentStatus(today.AddDays(-1), 15, today);

        Assert.Equal(RentStatus.Overdue, status);
        Assert.Equal(-1, days);
    }

    [Fact]
    public void GetRentStatus_Today_IsDueSoon()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = RentCalculator.GetRentStatus(today, 15, today);

        Assert.Equal(RentStatus.DueSoon, status);
        Assert.Equal(0, days);
    }

    [Fact]
    public void GetRentStatus_AtWarningBoundary_IsDueSoon()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = RentCalculator.GetRentStatus(today.AddDays(15), 15, today);

        Assert.Equal(RentStatus.DueSoon, status);
        Assert.Equal(15, days);
    }

    [Fact]
    public void GetRentStatus_PastWarningBoundary_IsCurrent()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = RentCalculator.GetRentStatus(today.AddDays(16), 15, today);

        Assert.Equal(RentStatus.Current, status);
        Assert.Equal(16, days);
    }

    [Fact]
    public void GetRentStatus_WithoutExplicitToday_UsesDominicanDate()
    {
        var (status, days) = RentCalculator.GetRentStatus(DateTimeExtensions.DrNow.Date.AddDays(30), 15);

        Assert.Equal(RentStatus.Current, status);
        Assert.Equal(30, days);
    }

    [Fact]
    public void ValidateRentPlan_ValidPlan_HasNoErrors()
    {
        Assert.Empty(RentCalculator.ValidateRentPlan(2000m, 5, hasOverageTiers: false));
    }

    [Fact]
    public void ValidateRentPlan_UnlimitedUsers_IsValid()
    {
        Assert.Empty(RentCalculator.ValidateRentPlan(2000m, RentCalculator.UnlimitedUsers, hasOverageTiers: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-2)]
    public void ValidateRentPlan_InvalidMaxUsers_ReturnsError(int? maxUsers)
    {
        Assert.Contains(RentCalculator.ValidateRentPlan(2000m, maxUsers, hasOverageTiers: false),
            e => e.Contains("usuarios", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRentPlan_WithOverageTiers_ReturnsError()
    {
        Assert.Contains(RentCalculator.ValidateRentPlan(2000m, 5, hasOverageTiers: true),
            e => e.Contains("tramos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRentPlan_NegativeFee_ReturnsError()
    {
        Assert.NotEmpty(RentCalculator.ValidateRentPlan(-1m, 5, hasOverageTiers: false));
    }

    [Fact]
    public void AccruesDocumentUsage_RentPlan_IsFalse()
    {
        Assert.False(BillingCalculator.AccruesDocumentUsage((int)PlanTypeEnum.Rent));
    }

    [Fact]
    public void AccruesDocumentUsage_DocumentPlan_IsTrue()
    {
        Assert.True(BillingCalculator.AccruesDocumentUsage((int)PlanTypeEnum.Documents));
    }

    [Fact]
    public void ValidatePlan_RentPlanIgnoresDocumentLimit()
    {
        // Un plan de renta con límite 0 es válido: el backend fuerza -1 al guardar.
        Assert.Empty(BillingCalculator.ValidatePlan(PlanTypeEnum.Rent, 0, 2000m, 5, []));
    }

    [Fact]
    public void ValidatePlan_DocumentPlanWithMaxUsers_ReturnsError()
    {
        Assert.Contains(
            BillingCalculator.ValidatePlan(PlanTypeEnum.Documents, 500, 3000m, 5, [new(1, null, 6m)]),
            e => e.Contains("usuarios", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatePlan_DocumentPlanWithoutMaxUsers_IsValid()
    {
        Assert.Empty(
            BillingCalculator.ValidatePlan(PlanTypeEnum.Documents, 500, 3000m, null, [new(1, null, 6m)]));
    }
}
```

- [ ] **Step 3: Correr los tests y confirmar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: FALLA en compilación — `error CS0103: The name 'RentCalculator' does not exist` y `'BillingCalculator' does not contain a definition for 'AccruesDocumentUsage'`.

- [ ] **Step 4: Implementar `RentCalculator`**

Crear `ZynstormECFPlatform.Services/Billing/RentCalculator.cs`:

```csharp
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record RentCalculationResult(
    decimal MonthlyFee,
    int MonthsCovered,
    decimal GrossAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total);

/// <summary>
/// Cálculo de la renta de un cliente con plan de tipo <see cref="PlanTypeEnum.Rent"/>.
/// Función pura: no toca base de datos ni configuración.
/// </summary>
public static class RentCalculator
{
    /// <summary>-1 en <c>Plan.MaxUsers</c> significa usuarios ilimitados.</summary>
    public const int UnlimitedUsers = -1;

    /// <summary>Días de anticipación para considerar una renta próxima a vencer.</summary>
    public const int DefaultWarningDays = 15;

    private const int MonthsInFullYear = 12;

    public static RentCalculationResult Calculate(decimal monthlyFee, bool paidFullYear, decimal discountPercent)
    {
        var months = paidFullYear ? MonthsInFullYear : 1;
        var gross = monthlyFee * months;
        var discount = Math.Round(gross * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new RentCalculationResult(
            monthlyFee,
            months,
            gross,
            discountPercent,
            discount,
            gross - discount);
    }

    /// <summary>
    /// Monto a facturar en el mes consultado. Si el próximo pago cae después de ese mes,
    /// el mes ya está cubierto y no se cobra nada; si cae en ese mes o antes (vencido),
    /// se cobra el ciclo completo.
    /// </summary>
    public static decimal GetAmountForMonth(RentCalculationResult result, DateTime? nextPaymentDate, int year, int month)
    {
        if (nextPaymentDate is not DateTime due)
            return result.Total;

        var firstDayAfterMonth = new DateTime(year, month, 1).AddMonths(1);
        return due.Date < firstDayAfterMonth ? result.Total : 0m;
    }

    /// <summary>
    /// Estado del próximo pago y días calendario que faltan (negativo si ya venció).
    /// <paramref name="today"/> solo se pasa en pruebas; por defecto usa la fecha en hora RD.
    /// </summary>
    public static (RentStatus Status, int? DaysToDue) GetRentStatus(
        DateTime? nextPaymentDate,
        int warningDays,
        DateTime? today = null)
    {
        if (nextPaymentDate is not DateTime due)
            return (RentStatus.NoDate, null);

        var reference = (today ?? DateTimeExtensions.DrNow).Date;
        var days = (due.Date - reference).Days;

        if (days < 0) return (RentStatus.Overdue, days);
        if (days <= warningDays) return (RentStatus.DueSoon, days);
        return (RentStatus.Current, days);
    }

    public static List<string> ValidateRentPlan(decimal monthlyFee, int? maxUsers, bool hasOverageTiers)
    {
        var errors = new List<string>();

        if (monthlyFee < 0)
            errors.Add("La renta mensual no puede ser negativa.");

        if (maxUsers is not int users || (users != UnlimitedUsers && users <= 0))
            errors.Add("Los usuarios permitidos deben ser mayor que 0, o -1 para ilimitado.");

        if (hasOverageTiers)
            errors.Add("Un plan de renta no lleva tramos de excedente.");

        return errors;
    }
}
```

- [ ] **Step 5: Agregar `AccruesDocumentUsage` y el overload de `ValidatePlan`**

En `ZynstormECFPlatform.Services/Billing/BillingCalculator.cs`, agregar el using al inicio del archivo:

```csharp
using ZynstormECFPlatform.Core.Enums;
```

y agregar estos dos miembros dentro de `public static class BillingCalculator`, justo antes del `ValidatePlan` existente:

```csharp
    /// <summary>Solo los planes por comprobantes acumulan consumo mensual.</summary>
    public static bool AccruesDocumentUsage(int planTypeId) =>
        planTypeId == (int)PlanTypeEnum.Documents;

    /// <summary>
    /// Validación según el tipo de plan. En los de renta se ignora el límite de documentos
    /// (el controller lo fuerza a -1) y se exige el tope de usuarios sin tramos.
    /// </summary>
    public static List<string> ValidatePlan(
        PlanTypeEnum planType,
        int monthlyDocumentLimit,
        decimal monthlyFee,
        int? maxUsers,
        IEnumerable<OverageTier> tiers)
    {
        var tierList = tiers.ToList();

        if (planType == PlanTypeEnum.Rent)
            return RentCalculator.ValidateRentPlan(monthlyFee, maxUsers, tierList.Count > 0);

        var errors = ValidatePlan(monthlyDocumentLimit, monthlyFee, tierList);

        if (maxUsers.HasValue)
            errors.Add("Los usuarios permitidos solo aplican a los planes de renta.");

        return errors;
    }
```

- [ ] **Step 6: Correr los tests y confirmar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: PASS. Los 13 tests existentes de `BillingCalculatorTests` siguen verdes (el `ValidatePlan` de 3 argumentos no cambió) y los 23 nuevos pasan.

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Core/Enums/PlanTypeEnum.cs ZynstormECFPlatform.Core/Enums/RentStatus.cs ZynstormECFPlatform.Services/Billing/RentCalculator.cs ZynstormECFPlatform.Services/Billing/BillingCalculator.cs ZynstormECFPlatform.Tests/Billing/RentCalculatorTests.cs
git commit -m "feat(billing): RentCalculator con ciclo, monto del mes, semáforo y validación del plan de renta"
```

---

### Task 2: Modelo de datos + migración

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/Plan.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Modify: `ZynstormECFPlatform.Core/AppSettings.cs`
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs:453-493` (bloque `Entity<Plan>`) y `:441-451` (bloque `Entity<Client>`)
- Modify: `ZynstormECFPlatform.Web.Api/appsettings.json`, `appsettings.Development.json`, `appsettings.Staging.json`
- Create: `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddRentPlanFields.cs` (lo genera `dotnet ef`)

**Interfaces:**
- Consumes: `PlanTypeEnum` de la Task 1.
- Produces:
  - `Plan.PlanTypeId` (int, default 1), `Plan.MaxUsers` (int?)
  - `Client.LastRentPaymentDate` (DateTime?), `Client.NextRentPaymentDate` (DateTime?), `Client.RentPaidFullYear` (bool), `Client.RentDiscountPercent` (decimal)
  - `AppSettings.RentPaymentWarningDays` (int, default 15)

- [ ] **Step 1: Agregar los campos a `Plan`**

En `ZynstormECFPlatform.Core/Entities/Plan.cs`, después de `MonthlyDocumentLimit`:

```csharp
    /// <summary>Tipo de plan: 1 = Comprobantes, 2 = Renta (<see cref="Enums.PlanTypeEnum"/>).</summary>
    public int PlanTypeId { get; set; } = (int)Enums.PlanTypeEnum.Documents;

    /// <summary>Usuarios permitidos al mismo tiempo. Solo planes de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }
```

- [ ] **Step 2: Agregar los campos de renta a `Client`**

En `ZynstormECFPlatform.Core/Entities/Client.cs`, después de `public bool ClientInactive { get; set; }`:

```csharp
    /// <summary>Fecha calendario del último pago de renta.</summary>
    public DateTime? LastRentPaymentDate { get; set; }

    /// <summary>Fecha calendario del próximo pago de renta.</summary>
    public DateTime? NextRentPaymentDate { get; set; }

    /// <summary>El cliente pagó el año completo de renta.</summary>
    public bool RentPaidFullYear { get; set; }

    /// <summary>Descuento por pago adelantado, en porcentaje (0–100). Por defecto 0.</summary>
    public decimal RentDiscountPercent { get; set; }
```

- [ ] **Step 3: Agregar `RentPaymentWarningDays` a `AppSettings`**

En `ZynstormECFPlatform.Core/AppSettings.cs`, después de `CertificateExpirationWarningDays`:

```csharp
    /// <summary>Días de anticipación para considerar una renta próxima a vencer.</summary>
    public int RentPaymentWarningDays { get; set; } = 15;
```

- [ ] **Step 4: Configurar las columnas nuevas en `StorageContext`**

En `ZynstormECFPlatform.Data/StorageContext.cs`, dentro de `modelBuilder.Entity<Plan>(entity => { ... })`, después del bloque `entity.Property(e => e.Description)`:

```csharp
            entity.Property(e => e.PlanTypeId)
                  .HasDefaultValue((int)ZynstormECFPlatform.Core.Enums.PlanTypeEnum.Documents)
                  .IsRequired();

            entity.Property(e => e.MaxUsers);
```

Y dentro de `modelBuilder.Entity<Client>(entity => { ... })`, justo después del bloque `entity.Property(e => e.ClientInactive)`:

```csharp
            entity.Property(e => e.LastRentPaymentDate)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.NextRentPaymentDate)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.RentPaidFullYear)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.RentDiscountPercent)
                  .HasColumnType("decimal(5, 2)")
                  .HasDefaultValue(0m)
                  .IsRequired();
```

> `decimal(5, 2)` explícito: la convención global del contexto es `HavePrecision(18, 2)` y el spec pide 5,2 para el porcentaje.

- [ ] **Step 5: Agregar la clave a los tres `appsettings`**

En `ZynstormECFPlatform.Web.Api/appsettings.json`, `appsettings.Development.json` y `appsettings.Staging.json`, en el mismo objeto donde está `"CertificateExpirationWarningDays": 20`, agregar inmediatamente después:

```json
    "RentPaymentWarningDays": 15,
```

- [ ] **Step 6: Compilar antes de generar la migración**

Run: `dotnet build ZynstormECFPlatform.slnx -v q`
Expected: Build succeeded, 0 errores.

- [ ] **Step 7: Generar la migración**

Run:

```bash
dotnet ef migrations add AddRentPlanFields --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api
```

Expected: crea `Migrations/<timestamp>_AddRentPlanFields.cs` y su `.Designer.cs`, y actualiza `StorageContextModelSnapshot.cs`.

- [ ] **Step 8: Revisar el `Up()` generado**

Abrir el archivo de migración y confirmar que contiene exactamente estas seis columnas y nada más:

```csharp
migrationBuilder.AddColumn<int>(
    name: "PlanTypeId", table: "Plan", type: "integer", nullable: false, defaultValue: 1);

migrationBuilder.AddColumn<int>(
    name: "MaxUsers", table: "Plan", type: "integer", nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "LastRentPaymentDate", table: "Client", type: "timestamp without time zone", nullable: true);

migrationBuilder.AddColumn<DateTime>(
    name: "NextRentPaymentDate", table: "Client", type: "timestamp without time zone", nullable: true);

migrationBuilder.AddColumn<bool>(
    name: "RentPaidFullYear", table: "Client", type: "boolean", nullable: false, defaultValue: false);

migrationBuilder.AddColumn<decimal>(
    name: "RentDiscountPercent", table: "Client", type: "numeric(5,2)", nullable: false, defaultValue: 0m);
```

Si aparece cualquier otra operación (renombrados, índices, cambios en tablas ajenas), borrar la migración con `dotnet ef migrations remove --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`, corregir la configuración y volver al Step 7.

- [ ] **Step 9: Aplicar la migración**

Run:

```bash
dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api
```

Expected: `Applying migration '<timestamp>_AddRentPlanFields'.` y `Done.`

- [ ] **Step 10: Correr build y tests**

Run: `dotnet build ZynstormECFPlatform.slnx -v q && dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build succeeded y todos los tests en PASS.

- [ ] **Step 11: Commit**

```bash
git add ZynstormECFPlatform.Core ZynstormECFPlatform.Data ZynstormECFPlatform.Web.Api/appsettings.json ZynstormECFPlatform.Web.Api/appsettings.Development.json ZynstormECFPlatform.Web.Api/appsettings.Staging.json
git commit -m "feat(plans): tipo de plan, tope de usuarios y campos de renta del cliente"
```

---

### Task 3: DTOs, convertidor de fecha calendario y mappings

**Files:**
- Create: `ZynstormECFPlatform.Dtos/Converters/CalendarDateJsonConverter.cs`
- Create: `ZynstormECFPlatform.Dtos/ClientRentDtos.cs`
- Modify: `ZynstormECFPlatform.Dtos/PlanDtos.cs`
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs`
- Modify: `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`
- Modify: `ZynstormECFPlatform.Mappings/MappingProfiles.cs:16-45`

**Interfaces:**
- Consumes: `PlanTypeEnum` de la Task 1; las propiedades de entidad de la Task 2.
- Produces:
  - `CalendarDateJsonConverter : JsonConverter<DateTime?>` en `ZynstormECFPlatform.Dtos.Converters`
  - `PlanCreateDto.PlanTypeId` (int), `PlanCreateDto.MaxUsers` (int?)
  - `ClientCreateDto.LastRentPaymentDate`, `.NextRentPaymentDate` (DateTime?), `.RentPaidFullYear` (bool), `.RentDiscountPercent` (decimal)
  - `ClientViewDto.PlanTypeId` (int?), `.MaxUsers` (int?), `.ActiveUsersCount` (int), `.RentStatus` (int?), `.RentDaysToDue` (int?), `.RentCycleAmount` (decimal?)
  - `ClientMonthlyUsageDto.PlanTypeId` (int)
  - `ClientRentDto` con todas las propiedades listadas en el Step 3

- [ ] **Step 1: Crear el convertidor de fecha calendario**

Crear `ZynstormECFPlatform.Dtos/Converters/CalendarDateJsonConverter.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Dtos.Converters;

/// <summary>
/// Fechas calendario (sin hora): se leen y escriben como "yyyy-MM-dd", sin desplazamiento de zona.
/// Necesario porque Program.cs registra DrDateTimeConverter / DrNullableDateTimeConverter de forma
/// global, que aplican ToDrTime() (UTC-4) a todo DateTime y moverían un día hacia atrás una fecha
/// guardada a las 00:00. Un [JsonConverter] a nivel de propiedad tiene precedencia sobre esos
/// convertidores globales.
/// </summary>
public class CalendarDateJsonConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None).Date;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
```

- [ ] **Step 2: Agregar tipo y tope de usuarios a los DTOs de plan**

En `ZynstormECFPlatform.Dtos/PlanDtos.cs`, dentro de `PlanCreateDto`, después de `MonthlyDocumentLimit`:

```csharp
    /// <summary>1 = Comprobantes, 2 = Renta.</summary>
    [Range(1, 2)]
    public int PlanTypeId { get; set; } = 1;

    /// <summary>Usuarios permitidos al mismo tiempo. Solo planes de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }
```

- [ ] **Step 3: Crear `ClientRentDto`**

Crear `ZynstormECFPlatform.Dtos/ClientRentDtos.cs`:

```csharp
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Fila del reporte de renta. <see cref="Total"/> es el ciclo completo con descuento.</summary>
public class ClientRentDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }

    /// <summary>-1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    public int ActiveUsersCount { get; set; }

    public bool RentPaidFullYear { get; set; }
    public decimal RentDiscountPercent { get; set; }

    public int MonthsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastRentPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextRentPaymentDate { get; set; }

    /// <summary>0 = sin fecha, 1 = al día, 2 = por vencer, 3 = vencido.</summary>
    public int RentStatus { get; set; }

    /// <summary>Días calendario para el próximo pago; negativo si ya venció.</summary>
    public int? RentDaysToDue { get; set; }
}
```

- [ ] **Step 4: Agregar los campos de renta a los DTOs de cliente**

En `ZynstormECFPlatform.Dtos/ClientDtos.cs`, agregar los usings al inicio del archivo:

```csharp
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;
```

Dentro de `ClientCreateDto`, después de `public bool ClientInactive { get; set; }`:

```csharp
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastRentPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextRentPaymentDate { get; set; }

    public bool RentPaidFullYear { get; set; }

    /// <summary>Descuento por pago adelantado, en porcentaje. Por defecto 0.</summary>
    [Range(0, 100)]
    public decimal RentDiscountPercent { get; set; }
```

Dentro de `ClientViewDto`, después de `public decimal? PlanMonthlyFee { get; set; }`:

```csharp
    /// <summary>1 = Comprobantes, 2 = Renta. Null si el cliente no tiene plan.</summary>
    public int? PlanTypeId { get; set; }

    /// <summary>Usuarios permitidos por el plan de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    /// <summary>Usuarios activos y no eliminados asignados al cliente.</summary>
    public int ActiveUsersCount { get; set; }

    /// <summary>0 = sin fecha, 1 = al día, 2 = por vencer, 3 = vencido. Solo planes de renta.</summary>
    public int? RentStatus { get; set; }

    public int? RentDaysToDue { get; set; }

    /// <summary>Ciclo completo de renta con descuento aplicado. Solo planes de renta.</summary>
    public decimal? RentCycleAmount { get; set; }
```

- [ ] **Step 5: Agregar `PlanTypeId` al DTO de consumo**

En `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`, dentro de `ClientMonthlyUsageDto`, después de `public string PlanName { get; set; } = string.Empty;`:

```csharp
    /// <summary>1 = Comprobantes, 2 = Renta. En las filas de renta los campos de documentos van en cero.</summary>
    public int PlanTypeId { get; set; } = 1;
```

- [ ] **Step 6: Mapear los campos nuevos**

En `ZynstormECFPlatform.Mappings/MappingProfiles.cs`, en el bloque `CreateMap<Client, ClientViewDto>()`, agregar dos `ForMember` después del de `PlanMonthlyFee`:

```csharp
            .ForMember(dest => dest.PlanTypeId, opt => opt.MapFrom(src => src.Plan != null ? (int?)src.Plan.PlanTypeId : null))
            .ForMember(dest => dest.MaxUsers, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.MaxUsers : null))
```

`PlanTypeId`, `MaxUsers` y los cuatro campos de renta del cliente tienen el mismo nombre en DTO y entidad, así que AutoMapper los mapea por convención en `ClientCreateDto → Client`, `ClientUpdateDto → Client`, `PlanCreateDto → Plan`, `PlanUpdateDto → Plan` y `Plan → PlanViewDto` sin configuración extra. `ActiveUsersCount`, `RentStatus`, `RentDaysToDue` y `RentCycleAmount` no se mapean: los llena `FillRentStatusAsync` en la Task 5.

- [ ] **Step 7: Verificar que la configuración de AutoMapper sigue válida**

Run: `dotnet build ZynstormECFPlatform.slnx -v q && dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build succeeded y tests en PASS.

- [ ] **Step 8: Commit**

```bash
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Mappings
git commit -m "feat(dtos): tipo de plan, tope de usuarios y datos de renta del cliente"
```

---

### Task 4: `PlanController` — validación por tipo y forzado del límite; `ClientUsageService` no cuenta renta

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs:56-121` (`Post`, `Put`, `Validate`)
- Modify: `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs:47-50`

**Interfaces:**
- Consumes: `BillingCalculator.ValidatePlan(PlanTypeEnum, int, decimal, int?, IEnumerable<OverageTier>)` y `BillingCalculator.AccruesDocumentUsage(int)` de la Task 1; `PlanCreateDto.PlanTypeId` / `.MaxUsers` de la Task 3.
- Produces: `POST`/`PUT v1/Plan` rechazan planes de renta inválidos y normalizan `MonthlyDocumentLimit`/`MaxUsers`/`OverageTiers`; `ClientUsageService.RegisterAcceptedAsync` devuelve `false` sin tocar la base para clientes con plan de renta.

- [ ] **Step 1: Reemplazar `Validate` y agregar la normalización en `PlanController`**

En `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs`, reemplazar el método `Validate` del final de la clase:

```csharp
        private static List<string> Validate(PlanCreateDto dto) =>
            BillingCalculator.ValidatePlan(
                dto.MonthlyDocumentLimit,
                dto.MonthlyFee,
                dto.OverageTiers.Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)));
```

por estos dos métodos:

```csharp
        private static List<string> Validate(PlanCreateDto dto) =>
            BillingCalculator.ValidatePlan(
                (PlanTypeEnum)dto.PlanTypeId,
                dto.MonthlyDocumentLimit,
                dto.MonthlyFee,
                dto.MaxUsers,
                dto.OverageTiers.Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)));

        /// <summary>
        /// Un plan de renta no tiene límite de comprobantes ni tramos: se fuerzan, no se validan.
        /// Un plan de comprobantes no lleva tope de usuarios.
        /// </summary>
        private static void Normalize(PlanCreateDto dto)
        {
            if ((PlanTypeEnum)dto.PlanTypeId == PlanTypeEnum.Rent)
            {
                dto.MonthlyDocumentLimit = BillingCalculator.UnlimitedDocuments;
                dto.OverageTiers = [];
            }
            else
            {
                dto.MaxUsers = null;
            }
        }
```

Agregar el using de enums al inicio del archivo, junto a los demás:

```csharp
using ZynstormECFPlatform.Core.Enums;
```

- [ ] **Step 2: Llamar a `Normalize` antes de validar en `Post` y en `Put`**

En el mismo archivo, en `Post`, cambiar la primera línea del cuerpo:

```csharp
            var errors = Validate(dto);
```

por:

```csharp
            Normalize(dto);
            var errors = Validate(dto);
```

Hacer el mismo cambio en `Put` (tiene las mismas dos líneas al inicio).

> `Normalize` corre antes de `Validate`, así que un plan de renta enviado con tramos desde un cliente viejo se limpia en vez de rechazarse; lo que se rechaza son los planes de renta sin `MaxUsers` y los de comprobantes con tramos inválidos.

- [ ] **Step 3: Hacer que `ClientUsageService` ignore los planes de renta**

En `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs`, después del bloque que carga el plan:

```csharp
            var plan = await planService.GetNoTrackingByAsync(
                p => p.PlanId == planId && p.StatusId == (int)StatusEnum.Active, cancellationToken);
            if (plan == null)
                return false;
```

agregar:

```csharp
            // Los planes de renta no acumulan consumo mensual ni excedente.
            if (!BillingCalculator.AccruesDocumentUsage(plan.PlanTypeId))
                return false;
```

- [ ] **Step 4: Correr build y tests**

Run: `dotnet build ZynstormECFPlatform.slnx -v q && dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build succeeded y tests en PASS.

- [ ] **Step 5: Verificar a mano con la API levantada**

Run: `dotnet run --project ZynstormECFPlatform.Web.Api`

Con un token de SA, crear un plan de renta inválido y confirmar el `400`:

```bash
curl -s -o /dev/null -w "%{http_code}\n" -X POST "http://localhost:5000/v1/Plan" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Renta sin tope","monthlyFee":2000,"planTypeId":2,"isActive":true,"overageTiers":[]}'
```

Expected: `400`, con `message` conteniendo "Los usuarios permitidos deben ser mayor que 0, o -1 para ilimitado."

Luego crear uno válido y confirmar que el límite salió forzado a `-1`:

```bash
curl -s -X POST "http://localhost:5000/v1/Plan" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"name":"Renta 5 usuarios","monthlyFee":2000,"planTypeId":2,"maxUsers":5,"monthlyDocumentLimit":300,"isActive":true,"overageTiers":[{"fromUnit":1,"toUnit":null,"unitPrice":6}]}'
```

Expected: `200`, y en la respuesta `"monthlyDocumentLimit": -1`, `"maxUsers": 5`, `"overageTiers": []`.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs ZynstormECFPlatform.Services/Billing/ClientUsageService.cs
git commit -m "feat(plans): validar plan de renta, forzar límite ilimitado y excluirlo del consumo"
```

---

### Task 5: `ClientController` — estado de renta, `GET rent` y renta en el reporte mensual

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` — constructor (`:19-32`), helpers (`:34-63`), `Get` (`:141,158`), `Post`/`Put` (validación), `GetMonthlyUsage` (`:502`), `GetClientMonthlyUsage` (`:524`), `BuildUsageDtosAsync` (`:547`)

**Interfaces:**
- Consumes: `RentCalculator.Calculate`, `.GetAmountForMonth`, `.GetRentStatus`, `.DefaultWarningDays` de la Task 1; `AppSettings.RentPaymentWarningDays` de la Task 2; `ClientRentDto`, `ClientViewDto.*` y `ClientMonthlyUsageDto.PlanTypeId` de la Task 3.
- Produces:
  - `GET v1/Client/rent` → `List<ClientRentDto>`
  - `GET v1/Client/usage?year=&month=` → `List<ClientMonthlyUsageDto>` incluyendo filas de renta
  - `GET v1/Client/guid/{guid}/usage?year=&month=` → fila de consumo o de renta
  - `ClientViewDto` con `ActiveUsersCount`, `PlanTypeId`, `MaxUsers`, `RentStatus`, `RentDaysToDue`, `RentCycleAmount` llenos en `GET v1/Client`

- [ ] **Step 1: Inyectar el repositorio de `UserClient`**

En el constructor primario de `ClientController`, agregar un parámetro después de `IRepository<PlanOverageTier> planOverageTierRepository,`:

```csharp
        IRepository<UserClient> userClientRepository,
```

- [ ] **Step 2: Agregar los helpers de renta**

En `ClientController`, justo después del método `FillCertificateExpirationAsync`, agregar:

```csharp
        private int RentWarningDays => appSettings.Value.RentPaymentWarningDays > 0
            ? appSettings.Value.RentPaymentWarningDays
            : RentCalculator.DefaultWarningDays;

        /// <summary>Usuarios activos y no eliminados por cliente.</summary>
        private async Task<Dictionary<int, int>> ActiveUserCountsAsync(List<int> clientIds, CancellationToken cancellationToken)
        {
            if (clientIds.Count == 0) return [];

            var counts = await userClientRepository.Table
                .AsNoTracking()
                .Where(uc => clientIds.Contains(uc.ClientId) && uc.User.IsActive && !uc.User.IsDeleted)
                .GroupBy(uc => uc.ClientId)
                .Select(g => new { ClientId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return counts.ToDictionary(c => c.ClientId, c => c.Count);
        }

        /// <summary>Completa usuarios activos y, en los clientes de plan de renta, estado y monto del ciclo.</summary>
        private async Task FillRentStatusAsync(IEnumerable<ClientViewDto> clients, CancellationToken cancellationToken)
        {
            var list = clients.ToList();
            if (list.Count == 0) return;

            var countByClient = await ActiveUserCountsAsync(list.Select(c => c.ClientId).ToList(), cancellationToken);

            foreach (var dto in list)
            {
                dto.ActiveUsersCount = countByClient.TryGetValue(dto.ClientId, out var count) ? count : 0;

                if (dto.PlanTypeId != (int)PlanTypeEnum.Rent) continue;

                var (status, days) = RentCalculator.GetRentStatus(dto.NextRentPaymentDate, RentWarningDays);
                dto.RentStatus = (int)status;
                dto.RentDaysToDue = days;
                dto.RentCycleAmount = RentCalculator
                    .Calculate(dto.PlanMonthlyFee ?? 0m, dto.RentPaidFullYear, dto.RentDiscountPercent)
                    .Total;
            }
        }

        /// <summary>Filas del reporte mensual para los clientes con plan de renta activo.</summary>
        private async Task<List<ClientMonthlyUsageDto>> BuildRentUsageRowsAsync(
            int year, int month, string? clientGuid, CancellationToken cancellationToken)
        {
            var query = RentClientsQuery();
            if (!string.IsNullOrEmpty(clientGuid))
                query = query.Where(c => c.GuidId == clientGuid);

            var clients = await query.ToListAsync(cancellationToken);

            return clients.Select(c =>
            {
                var calculation = RentCalculator.Calculate(c.Plan!.MonthlyFee, c.RentPaidFullYear, c.RentDiscountPercent);

                return new ClientMonthlyUsageDto
                {
                    ClientGuidId = c.GuidId,
                    ClientName = c.Name,
                    ClientRnc = c.Rnc,
                    ClientInactive = c.ClientInactive,
                    Year = year,
                    Month = month,
                    PlanName = c.Plan.Name,
                    PlanTypeId = (int)PlanTypeEnum.Rent,
                    MonthlyFee = c.Plan.MonthlyFee,
                    MonthlyDocumentLimit = BillingCalculator.UnlimitedDocuments,
                    AcceptedDocuments = 0,
                    OverageDocuments = 0,
                    OverageAmount = 0m,
                    Total = RentCalculator.GetAmountForMonth(calculation, c.NextRentPaymentDate, year, month),
                    Tiers = []
                };
            }).ToList();
        }

        private IQueryable<Client> RentClientsQuery() =>
            Repository.Table.AsNoTracking()
                .Include(c => c.Plan)
                .Where(c => c.Plan != null
                         && c.Plan.PlanTypeId == (int)PlanTypeEnum.Rent
                         && c.Plan.StatusId == (int)StatusEnum.Active);
```

- [ ] **Step 3: Llamar a `FillRentStatusAsync` en los tres puntos de `Get`**

En `ClientController.Get`, después de cada `await FillCertificateExpirationAsync(...)`, agregar la llamada hermana. Son dos lugares:

```csharp
                    var mappedItems = Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results).ToList();
                    await FillCertificateExpirationAsync(mappedItems, cancellationToken);
                    await FillRentStatusAsync(mappedItems, cancellationToken);
```

y

```csharp
                    var mappedList = Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results).ToList();
                    await FillCertificateExpirationAsync(mappedList, cancellationToken);
                    await FillRentStatusAsync(mappedList, cancellationToken);
```

Y en la rama que devuelve un solo cliente por `id` (la que hace `return Ok(Mapper.Map<Client, ClientViewDto>(result));`), reemplazar esa línea por:

```csharp
                    var single = Mapper.Map<Client, ClientViewDto>(result);
                    await FillRentStatusAsync([single], cancellationToken);
                    return Ok(single);
```

- [ ] **Step 4: Validar las fechas de renta al crear y actualizar**

En `ClientController`, agregar este método junto a `PlanExistsAsync` (al final de la clase):

```csharp
        private static string? ValidateRentDates(ClientCreateDto dto) =>
            dto.LastRentPaymentDate is DateTime last
            && dto.NextRentPaymentDate is DateTime next
            && next.Date < last.Date
                ? "La fecha de próximo pago no puede ser anterior a la del último pago."
                : null;
```

En `Post`, después de la validación del plan:

```csharp
                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");
```

agregar:

```csharp
                if (ValidateRentDates(dto) is string rentDateError)
                    return BadRequest(rentDateError);
```

Agregar las mismas dos líneas en `Put`, después de su propia comprobación de `PlanExistsAsync`.

- [ ] **Step 5: Agregar el endpoint `GET rent`**

En `ClientController`, después de `GetClientMonthlyUsage`, agregar:

```csharp
        [HttpGet]
        [Route("rent", Order = 1)]
        public async Task<IActionResult> GetRentClients(CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();

            try
            {
                var clients = await RentClientsQuery().OrderBy(c => c.Name).ToListAsync(cancellationToken);
                var countByClient = await ActiveUserCountsAsync(clients.Select(c => c.ClientId).ToList(), cancellationToken);

                var rows = clients.Select(c =>
                {
                    var calculation = RentCalculator.Calculate(c.Plan!.MonthlyFee, c.RentPaidFullYear, c.RentDiscountPercent);
                    var (status, days) = RentCalculator.GetRentStatus(c.NextRentPaymentDate, RentWarningDays);

                    return new ClientRentDto
                    {
                        ClientGuidId = c.GuidId,
                        ClientName = c.Name,
                        ClientRnc = c.Rnc,
                        ClientInactive = c.ClientInactive,
                        PlanName = c.Plan.Name,
                        MonthlyFee = c.Plan.MonthlyFee,
                        MaxUsers = c.Plan.MaxUsers,
                        ActiveUsersCount = countByClient.TryGetValue(c.ClientId, out var count) ? count : 0,
                        RentPaidFullYear = c.RentPaidFullYear,
                        RentDiscountPercent = c.RentDiscountPercent,
                        MonthsCovered = calculation.MonthsCovered,
                        GrossAmount = calculation.GrossAmount,
                        DiscountAmount = calculation.DiscountAmount,
                        Total = calculation.Total,
                        LastRentPaymentDate = c.LastRentPaymentDate,
                        NextRentPaymentDate = c.NextRentPaymentDate,
                        RentStatus = (int)status,
                        RentDaysToDue = days
                    };
                }).ToList();

                return Ok(rows);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
```

- [ ] **Step 6: Unir las filas de renta al reporte mensual**

En `GetMonthlyUsage`, reemplazar el cuerpo del `try`:

```csharp
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month);

                return Ok(await BuildUsageDtosAsync(query, cancellationToken));
```

por:

```csharp
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month);

                var documentRows = await BuildUsageDtosAsync(query, cancellationToken);
                var rentRows = await BuildRentUsageRowsAsync(year, month, clientGuid: null, cancellationToken);

                return Ok(documentRows.Concat(rentRows).OrderBy(d => d.ClientName).ToList());
```

En `GetClientMonthlyUsage`, reemplazar:

```csharp
                var result = (await BuildUsageDtosAsync(query, cancellationToken)).FirstOrDefault();
                return result == null ? NotFound("El cliente no tiene consumo registrado en ese mes.") : Ok(result);
```

por:

```csharp
                var result = (await BuildUsageDtosAsync(query, cancellationToken)).FirstOrDefault()
                    ?? (await BuildRentUsageRowsAsync(year, month, guid, cancellationToken)).FirstOrDefault();

                return result == null ? NotFound("El cliente no tiene consumo registrado en ese mes.") : Ok(result);
```

- [ ] **Step 7: Marcar las filas de consumo como plan de comprobantes**

En `BuildUsageDtosAsync`, dentro del objeto `new ClientMonthlyUsageDto { ... }`, agregar después de `PlanName = u.PlanName,`:

```csharp
                        PlanTypeId = (int)PlanTypeEnum.Documents,
```

> Es correcto fijarlo: los planes de renta nunca generan filas en `ClientMonthlyUsage`, así que toda fila que llegue aquí es de un plan por comprobantes.

- [ ] **Step 8: Correr build y tests**

Run: `dotnet build ZynstormECFPlatform.slnx -v q && dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build succeeded y tests en PASS.

- [ ] **Step 9: Verificar los endpoints a mano**

Levantar la API (`dotnet run --project ZynstormECFPlatform.Web.Api`), asignar el plan "Renta 5 usuarios" a un cliente con `PUT v1/Client` incluyendo:

```json
{ "lastRentPaymentDate": "2026-09-01", "nextRentPaymentDate": "2027-09-01",
  "rentPaidFullYear": true, "rentDiscountPercent": 10 }
```

Luego:

```bash
curl -s "http://localhost:5000/v1/Client/rent" -H "Authorization: Bearer $TOKEN"
```

Expected: el cliente aparece con `"monthsCovered": 12`, `"grossAmount": 24000.00`, `"discountAmount": 2400.00`, `"total": 21600.00`, `"rentStatus": 1` y **`"lastRentPaymentDate": "2026-09-01"`** — si sale `"2026-08-31"`, el `[JsonConverter]` de la Task 3 no quedó aplicado.

```bash
curl -s "http://localhost:5000/v1/Client/usage?year=2026&month=9" -H "Authorization: Bearer $TOKEN"
```

Expected: la fila de ese cliente trae `"planTypeId": 2` y `"total": 0` (septiembre 2026 está cubierto porque el próximo pago es en septiembre 2027).

Repetir con `year=2027&month=9`.
Expected: `"total": 21600.00` — es el mes de renovación.

- [ ] **Step 10: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs
git commit -m "feat(clients): estado de renta, endpoint de renta y filas de renta en el reporte mensual"
```

---

### Task 6: Frontend — tipos, servicios y utilidades de renta

**Files:**
- Modify: `types/plan.type.ts`
- Modify: `types/client.type.ts`
- Modify: `types/usage.type.ts`
- Create: `types/rent.type.ts`
- Create: `services/rent.service.ts`
- Create: `lib/rent.ts`

Todas las rutas son relativas a `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.

**Interfaces:**
- Consumes: `GET v1/Client/rent` de la Task 5; los campos de DTO de la Task 3.
- Produces:
  - `PlanType` (enum), `UNLIMITED_USERS`, `PlanCreate.planTypeId`, `PlanCreate.maxUsers`
  - `Client` y `ClientCreate` con los campos de renta
  - `ClientMonthlyUsage.planTypeId`
  - `RentStatus` (enum), `ClientRent`
  - `getRentClients(): Promise<ClientRent[]>`
  - `suggestNextPaymentDate(lastPaymentDate: string, paidFullYear: boolean): string`
  - `rentStatusLabel(status: RentStatus, daysToDue?: number | null): string`
  - `rentStatusBadgeClass(status: RentStatus): string`
  - `formatUserSeats(maxUsers: number | null | undefined, activeUsersCount: number): string`

- [ ] **Step 1: Agregar tipo de plan y tope de usuarios a `types/plan.type.ts`**

Agregar al inicio del archivo, después de `export const UNLIMITED_DOCUMENTS = -1`:

```ts
export const UNLIMITED_USERS = -1

export enum PlanType {
  Documents = 1,
  Rent = 2,
}
```

Y dentro de `PlanCreate`, después de `monthlyDocumentLimit: number`:

```ts
  planTypeId: PlanType
  maxUsers: number | null
```

- [ ] **Step 2: Agregar los campos de renta a `types/client.type.ts`**

Dentro de `interface Client`, después de `planMonthlyFee?: number | null`:

```ts
  planTypeId?: number | null
  maxUsers?: number | null
  activeUsersCount?: number
  rentStatus?: number | null
  rentDaysToDue?: number | null
  rentCycleAmount?: number | null
  lastRentPaymentDate?: string | null
  nextRentPaymentDate?: string | null
  rentPaidFullYear?: boolean
  rentDiscountPercent?: number
```

Dentro de `interface ClientCreate`, después de `clientInactive?: boolean`:

```ts
  lastRentPaymentDate?: string | null
  nextRentPaymentDate?: string | null
  rentPaidFullYear?: boolean
  rentDiscountPercent?: number
```

- [ ] **Step 3: Agregar `planTypeId` a `types/usage.type.ts`**

Dentro de `interface ClientMonthlyUsage`, después de `planName: string`:

```ts
  planTypeId: number
```

- [ ] **Step 4: Crear `types/rent.type.ts`**

```ts
export enum RentStatus {
  NoDate = 0,
  Current = 1,
  DueSoon = 2,
  Overdue = 3,
}

export interface ClientRent {
  clientGuidId: string
  clientName: string
  clientRnc: string
  clientInactive: boolean
  planName: string
  monthlyFee: number
  maxUsers: number | null
  activeUsersCount: number
  rentPaidFullYear: boolean
  rentDiscountPercent: number
  monthsCovered: number
  grossAmount: number
  discountAmount: number
  total: number
  lastRentPaymentDate?: string | null
  nextRentPaymentDate?: string | null
  rentStatus: RentStatus
  rentDaysToDue?: number | null
}
```

- [ ] **Step 5: Crear `services/rent.service.ts`**

```ts
import { get } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { ClientRent } from "@/types/rent.type"

const CLIENT_URL = `${API_BASE_URL}/v1/Client`

export const getRentClients = async (): Promise<ClientRent[]> => {
  return await get<ClientRent[]>(`${CLIENT_URL}/rent`)
}
```

- [ ] **Step 6: Crear `lib/rent.ts`**

```ts
import { RentStatus } from "@/types/rent.type"
import { UNLIMITED_USERS } from "@/types/plan.type"

const pad = (value: number) => String(value).padStart(2, "0")

/**
 * Sugiere la fecha de próximo pago: +12 meses si pagó el año completo, +1 mes si no.
 * Trabaja sobre la cadena "yyyy-MM-dd" para no depender de la zona del navegador, y
 * recorta el día al último del mes destino (31 de enero + 1 mes = 28/29 de febrero).
 */
export const suggestNextPaymentDate = (lastPaymentDate: string, paidFullYear: boolean): string => {
  const [year, month, day] = lastPaymentDate.split("-").map(Number)
  if (!year || !month || !day) return ""

  const targetIndex = month - 1 + (paidFullYear ? 12 : 1)
  const targetYear = year + Math.floor(targetIndex / 12)
  const targetMonth = targetIndex % 12
  const lastDayOfTargetMonth = new Date(Date.UTC(targetYear, targetMonth + 1, 0)).getUTCDate()

  return `${targetYear}-${pad(targetMonth + 1)}-${pad(Math.min(day, lastDayOfTargetMonth))}`
}

export const rentStatusLabel = (status: RentStatus, daysToDue?: number | null): string => {
  switch (status) {
    case RentStatus.Overdue:
      return daysToDue != null ? `Vencida hace ${Math.abs(daysToDue)} día${daysToDue === -1 ? "" : "s"}` : "Vencida"
    case RentStatus.DueSoon:
      if (daysToDue === 0) return "Vence hoy"
      if (daysToDue === 1) return "Vence mañana"
      return daysToDue != null ? `Vence en ${daysToDue} días` : "Por vencer"
    case RentStatus.Current:
      return "Al día"
    default:
      return "Sin fecha de pago"
  }
}

export const rentStatusBadgeClass = (status: RentStatus): string => {
  switch (status) {
    case RentStatus.Overdue:
      return "bg-red-100 text-red-800 hover:bg-red-100"
    case RentStatus.DueSoon:
      return "bg-amber-100 text-amber-800 hover:bg-amber-100"
    case RentStatus.Current:
      return "bg-green-100 text-green-800 hover:bg-green-100"
    default:
      return "bg-secondary text-secondary-foreground hover:bg-secondary"
  }
}

export const formatUserSeats = (maxUsers: number | null | undefined, activeUsersCount: number): string => {
  if (maxUsers == null) return String(activeUsersCount)
  if (maxUsers === UNLIMITED_USERS) return `${activeUsersCount} (ilimitado)`
  return `${activeUsersCount} de ${maxUsers}`
}
```

- [ ] **Step 7: Mantener el build verde en el editor de planes**

`PlanCreate` ahora exige `planTypeId` y `maxUsers`, así que el `emptyPlan` de `app/configuraciones/planes/page.tsx` deja de compilar. Aquí se hace el arreglo mínimo para que el repo quede construible; el trabajo de UI va en la Task 7.

Agregar `PlanType` al import de tipos de esa página:

```ts
import { Plan, PlanCreate, PlanOverageTier, PlanType, UNLIMITED_DOCUMENTS } from "@/types/plan.type"
```

y agregar las dos propiedades a `emptyPlan`, después de `monthlyDocumentLimit: 100,`:

```ts
  planTypeId: PlanType.Documents,
  maxUsers: null,
```

- [ ] **Step 8: Verificar lint y build**

Run: `npm run lint && npm run build`
Expected: sin errores.

- [ ] **Step 9: Commit**

```bash
git add types/plan.type.ts types/client.type.ts types/usage.type.ts types/rent.type.ts services/rent.service.ts lib/rent.ts app/configuraciones/planes/page.tsx
git commit -m "feat(renta): tipos, servicio y utilidades de renta"
```

---

### Task 7: Frontend — tipo de plan en el editor de planes

**Files:**
- Modify: `app/configuraciones/planes/page.tsx`

**Interfaces:**
- Consumes: `PlanType`, `UNLIMITED_USERS`, `UNLIMITED_DOCUMENTS`, `PlanCreate` de la Task 6.
- Produces: el formulario envía `planTypeId` y `maxUsers`; la tabla muestra el tipo y el tope de usuarios.

- [ ] **Step 1: Actualizar imports, `emptyPlan` y los formateadores**

La Task 6 ya dejó `PlanType` importado y `emptyPlan` completo. Aquí solo falta `UNLIMITED_USERS` en el import de tipos:

```ts
import { Plan, PlanCreate, PlanOverageTier, PlanType, UNLIMITED_DOCUMENTS, UNLIMITED_USERS } from "@/types/plan.type"
```

Agregar `Select` a los imports de UI, junto a los demás:

```ts
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
```

Agregar los dos formateadores nuevos, después de `formatTier`:

```ts
const formatPlanType = (planTypeId: number) =>
  planTypeId === PlanType.Rent ? "Renta" : "Comprobantes"

const formatMaxUsers = (maxUsers: number | null) => {
  if (maxUsers == null) return "—"
  return maxUsers === UNLIMITED_USERS ? "Ilimitados" : `${maxUsers} usuarios`
}
```

- [ ] **Step 2: Cargar el tipo al editar y derivar los flags**

En `openEdit`, reemplazar el objeto pasado a `setFormData` por:

```ts
    setFormData({
      name: plan.name,
      description: plan.description ?? "",
      monthlyFee: plan.monthlyFee,
      monthlyDocumentLimit: plan.monthlyDocumentLimit,
      planTypeId: plan.planTypeId ?? PlanType.Documents,
      maxUsers: plan.maxUsers ?? null,
      isActive: plan.isActive,
      overageTiers: plan.overageTiers.map((t) => ({ ...t })),
    })
```

Debajo de la línea `const isUnlimited = formData.monthlyDocumentLimit === UNLIMITED_DOCUMENTS`, agregar:

```ts
  const isRent = formData.planTypeId === PlanType.Rent
  const hasUnlimitedUsers = formData.maxUsers === UNLIMITED_USERS

  const changePlanType = (planTypeId: PlanType) => {
    setFormData((current) => ({
      ...current,
      planTypeId,
      // Un plan de renta no tiene límite de comprobantes ni tramos; uno de comprobantes no tiene tope de usuarios.
      monthlyDocumentLimit: planTypeId === PlanType.Rent ? UNLIMITED_DOCUMENTS : 100,
      maxUsers: planTypeId === PlanType.Rent ? 5 : null,
      overageTiers:
        planTypeId === PlanType.Rent
          ? []
          : [
              { fromUnit: 1, toUnit: 100, unitPrice: 6 },
              { fromUnit: 101, toUnit: null, unitPrice: 9 },
            ],
    }))
  }
```

- [ ] **Step 3: Agregar el selector de tipo y el campo de usuarios al formulario**

En el `<FieldGroup className="py-4">`, reemplazar el `<div className="grid gap-4 sm:grid-cols-2">` que contiene "Comprobantes incluidos al mes" y los dos switches por:

```tsx
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="plan-type">Tipo de plan</FieldLabel>
                    <Select
                      value={String(formData.planTypeId)}
                      onValueChange={(value) => changePlanType(Number(value) as PlanType)}
                    >
                      <SelectTrigger id="plan-type">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value={String(PlanType.Documents)}>
                          Comprobantes (límite mensual y excedente)
                        </SelectItem>
                        <SelectItem value={String(PlanType.Rent)}>Renta (monto fijo y tope de usuarios)</SelectItem>
                      </SelectContent>
                    </Select>
                  </Field>

                  {isRent ? (
                    <Field>
                      <FieldLabel htmlFor="plan-max-users">Usuarios permitidos</FieldLabel>
                      <Input
                        id="plan-max-users"
                        type="number"
                        min={1}
                        disabled={hasUnlimitedUsers}
                        value={hasUnlimitedUsers ? "" : formData.maxUsers ?? ""}
                        onChange={(e) =>
                          setFormData((c) => ({ ...c, maxUsers: e.target.value === "" ? null : Number(e.target.value) }))
                        }
                      />
                    </Field>
                  ) : (
                    <Field>
                      <FieldLabel htmlFor="plan-limit">Comprobantes incluidos al mes</FieldLabel>
                      <Input
                        id="plan-limit"
                        type="number"
                        min={1}
                        disabled={isUnlimited}
                        value={isUnlimited ? "" : formData.monthlyDocumentLimit}
                        onChange={(e) => setFormData((c) => ({ ...c, monthlyDocumentLimit: Number(e.target.value) }))}
                      />
                    </Field>
                  )}
                </div>

                <div className="flex flex-col gap-3">
                  {isRent ? (
                    <label className="flex items-center gap-3 text-sm">
                      <Switch
                        checked={hasUnlimitedUsers}
                        onCheckedChange={(checked) =>
                          setFormData((c) => ({ ...c, maxUsers: checked ? UNLIMITED_USERS : 5 }))
                        }
                      />
                      Usuarios ilimitados
                    </label>
                  ) : (
                    <label className="flex items-center gap-3 text-sm">
                      <Switch
                        checked={isUnlimited}
                        onCheckedChange={(checked) =>
                          setFormData((c) => ({ ...c, monthlyDocumentLimit: checked ? UNLIMITED_DOCUMENTS : 100 }))
                        }
                      />
                      Comprobantes ilimitados
                    </label>
                  )}
                  <label className="flex items-center gap-3 text-sm">
                    <Switch
                      checked={formData.isActive}
                      onCheckedChange={(checked) => setFormData((c) => ({ ...c, isActive: checked }))}
                    />
                    Plan activo
                  </label>
                </div>
```

Cambiar la etiqueta del monto: en el `Field` de `plan-fee`, reemplazar el texto del label por:

```tsx
                    <FieldLabel htmlFor="plan-fee">{isRent ? "Renta mensual (RD$)" : "Mensualidad (RD$)"}</FieldLabel>
```

Cambiar la condición del editor de tramos, de `{!isUnlimited && (` a:

```tsx
                {!isRent && !isUnlimited && (
```

Y actualizar el `DialogDescription` para que explique los dos casos:

```tsx
                <DialogDescription>
                  {isRent
                    ? "Renta fija al mes con un tope informativo de usuarios. No lleva límite de comprobantes ni tramos."
                    : "Los comprobantes aceptados por encima del límite se cobran por tramos al cierre del mes."}
                </DialogDescription>
```

- [ ] **Step 4: Mostrar tipo y usuarios en la tabla**

En el `<TableHeader>`, reemplazar la fila de encabezados por:

```tsx
                <TableRow>
                  <TableHead>Plan</TableHead>
                  <TableHead>Tipo</TableHead>
                  <TableHead>Monto</TableHead>
                  <TableHead>Límite / Usuarios</TableHead>
                  <TableHead>Excedente</TableHead>
                  <TableHead className="text-center">Clientes</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead className="text-right">Acciones</TableHead>
                </TableRow>
```

Cambiar los tres `colSpan={7}` de las filas de "Cargando planes..." y "No hay planes registrados" a `colSpan={8}`.

En el cuerpo, después de la celda del nombre del plan (la que cierra con `</TableCell>` tras el bloque de `plan.description`), insertar:

```tsx
                      <TableCell>
                        <Badge variant={plan.planTypeId === PlanType.Rent ? "default" : "secondary"}>
                          {formatPlanType(plan.planTypeId)}
                        </Badge>
                      </TableCell>
```

Y reemplazar la celda del límite:

```tsx
                      <TableCell>{formatLimit(plan.monthlyDocumentLimit)}</TableCell>
```

por:

```tsx
                      <TableCell>
                        {plan.planTypeId === PlanType.Rent
                          ? formatMaxUsers(plan.maxUsers)
                          : formatLimit(plan.monthlyDocumentLimit)}
                      </TableCell>
```

Y la celda de excedente, para que los planes de renta muestren guion:

```tsx
                      <TableCell className="text-xs text-muted-foreground">
                        {plan.planTypeId === PlanType.Rent ||
                        plan.monthlyDocumentLimit === UNLIMITED_DOCUMENTS ||
                        plan.overageTiers.length === 0
                          ? "—"
                          : plan.overageTiers.map((tier) => <div key={tier.fromUnit}>{formatTier(tier)}</div>)}
                      </TableCell>
```

- [ ] **Step 5: Actualizar el subtítulo de la página**

Reemplazar:

```tsx
            <p className="text-muted-foreground">Mensualidad, límite de comprobantes y cobro por excedente.</p>
```

por:

```tsx
            <p className="text-muted-foreground">
              Planes por comprobantes con límite y excedente, o planes de renta fija con tope de usuarios.
            </p>
```

- [ ] **Step 6: Verificar lint y build**

Run: `npm run lint && npm run build`
Expected: sin errores.

- [ ] **Step 7: Verificar en el navegador**

Levantar `npm run dev`, entrar como SA a `/configuraciones/planes`, crear un plan con tipo "Renta", usuarios 5 y renta 2000. Confirmar que el editor de tramos y el campo de comprobantes desaparecen, que el plan se guarda, y que la tabla lo muestra con el badge "Renta", "5 usuarios" y "—" en excedente.

- [ ] **Step 8: Commit**

```bash
git add app/configuraciones/planes/page.tsx
git commit -m "feat(planes): tipo de plan y tope de usuarios en el editor"
```

---

### Task 8: Frontend — campos de renta en el formulario y la tabla de clientes

**Files:**
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: `suggestNextPaymentDate`, `rentStatusLabel`, `rentStatusBadgeClass`, `formatUserSeats` de la Task 6; `PlanType` de la Task 6; `Client.rent*` de la Task 6.
- Produces: el formulario envía los cuatro campos de renta cuando el plan seleccionado es de renta; la tabla muestra el badge de estado y el contador de usuarios.

- [ ] **Step 1: Agregar los imports**

Junto a los imports existentes de tipos y utilidades:

```ts
import { PlanType, UNLIMITED_USERS } from "@/types/plan.type"
import { RentStatus } from "@/types/rent.type"
import { formatUserSeats, rentStatusBadgeClass, rentStatusLabel, suggestNextPaymentDate } from "@/lib/rent"
```

- [ ] **Step 2: Agregar los campos de renta a `emptyForm`**

En `const emptyForm: ClientCreate = { ... }`, después de `clientInactive: false,`:

```ts
  lastRentPaymentDate: null,
  nextRentPaymentDate: null,
  rentPaidFullYear: false,
  rentDiscountPercent: 0,
```

- [ ] **Step 3: Cargar los campos al editar**

En la función que llena el formulario al editar un cliente (la que tiene `planId: client.planId ?? null,` y `clientInactive: client.clientInactive ?? false,`), agregar justo después:

```ts
      lastRentPaymentDate: client.lastRentPaymentDate ?? null,
      nextRentPaymentDate: client.nextRentPaymentDate ?? null,
      rentPaidFullYear: client.rentPaidFullYear ?? false,
      rentDiscountPercent: client.rentDiscountPercent ?? 0,
```

- [ ] **Step 4: Incluir los campos en el payload que se envía**

En el objeto que arma el payload (el que tiene `planId: formData.planId ?? null,` y `clientInactive: formData.clientInactive ?? false,`), agregar justo después:

```ts
      lastRentPaymentDate: formData.lastRentPaymentDate || null,
      nextRentPaymentDate: formData.nextRentPaymentDate || null,
      rentPaidFullYear: formData.rentPaidFullYear ?? false,
      rentDiscountPercent: formData.rentDiscountPercent ?? 0,
```

- [ ] **Step 5: Derivar si el plan seleccionado es de renta y la auto-sugerencia**

Dentro del componente, después de `const [plans, setPlans] = useState<Plan[]>([])` y sus hooks vecinos, agregar:

```ts
  const selectedPlan = plans.find((p) => p.planId === formData.planId) ?? null
  const isRentPlan = selectedPlan?.planTypeId === PlanType.Rent

  /** Cambia un dato del ciclo de renta y re-sugiere el próximo pago. */
  const updateRentCycle = (changes: Partial<Pick<ClientCreate, "lastRentPaymentDate" | "rentPaidFullYear">>) => {
    setFormData((current) => {
      const next = { ...current, ...changes }
      const last = next.lastRentPaymentDate
      return last
        ? { ...next, nextRentPaymentDate: suggestNextPaymentDate(last, next.rentPaidFullYear ?? false) }
        : next
    })
  }
```

- [ ] **Step 6: Agregar el bloque de renta al formulario**

Justo después del `<div className="grid gap-4 sm:grid-cols-2">` que contiene el selector de plan y el switch de "Cliente inactivo" (es decir, después de su `</div>` de cierre y antes del `</FieldGroup>`), insertar:

```tsx
                  {isRentPlan && (
                    <div className="space-y-4 rounded-lg border border-border/60 bg-secondary/20 p-4">
                      <p className="text-sm font-medium">Renta</p>

                      <div className="grid gap-4 sm:grid-cols-2">
                        <Field>
                          <FieldLabel htmlFor="lastRentPaymentDate">Último pago</FieldLabel>
                          <Input
                            id="lastRentPaymentDate"
                            type="date"
                            value={formData.lastRentPaymentDate ?? ""}
                            onChange={(e) => updateRentCycle({ lastRentPaymentDate: e.target.value || null })}
                          />
                        </Field>

                        <Field>
                          <FieldLabel htmlFor="nextRentPaymentDate">Próximo pago</FieldLabel>
                          <Input
                            id="nextRentPaymentDate"
                            type="date"
                            min={formData.lastRentPaymentDate ?? undefined}
                            value={formData.nextRentPaymentDate ?? ""}
                            onChange={(e) =>
                              setFormData((current) => ({ ...current, nextRentPaymentDate: e.target.value || null }))
                            }
                          />
                          <p className="text-xs text-muted-foreground">
                            Se sugiere automáticamente; puedes cambiarla.
                          </p>
                        </Field>
                      </div>

                      <div className="grid gap-4 sm:grid-cols-2">
                        <Field>
                          <FieldLabel htmlFor="rentDiscountPercent">Descuento por pago adelantado (%)</FieldLabel>
                          <Input
                            id="rentDiscountPercent"
                            type="number"
                            min={0}
                            max={100}
                            step="0.01"
                            value={formData.rentDiscountPercent ?? 0}
                            onChange={(e) =>
                              setFormData((current) => ({ ...current, rentDiscountPercent: Number(e.target.value) }))
                            }
                          />
                        </Field>

                        <Field>
                          <FieldLabel htmlFor="rentPaidFullYear">Pagó el año completo</FieldLabel>
                          <div className="flex items-center gap-3 pt-2">
                            <Switch
                              id="rentPaidFullYear"
                              checked={formData.rentPaidFullYear ?? false}
                              onCheckedChange={(checked) => updateRentCycle({ rentPaidFullYear: checked })}
                            />
                            <span className="text-sm text-muted-foreground">
                              {formData.rentPaidFullYear ? "12 meses por ciclo" : "1 mes por ciclo"}
                            </span>
                          </div>
                        </Field>
                      </div>

                      {selectedPlan && (
                        <p className="text-xs text-muted-foreground">
                          Ciclo:{" "}
                          {formatCurrency(
                            selectedPlan.monthlyFee *
                              (formData.rentPaidFullYear ? 12 : 1) *
                              (1 - (formData.rentDiscountPercent ?? 0) / 100),
                          )}
                          {" · "}
                          Usuarios permitidos:{" "}
                          {selectedPlan.maxUsers === UNLIMITED_USERS ? "ilimitados" : selectedPlan.maxUsers}
                        </p>
                      )}
                    </div>
                  )}
```

- [ ] **Step 7: Mostrar el badge de renta y el contador de usuarios en la tabla**

En la celda del plan (la que muestra `client.planMonthlyFee`), reemplazar el bloque completo:

```tsx
                          {client.planMonthlyFee != null ? (
                            <>
                              {formatCurrency(client.planMonthlyFee)}
                              <span className="text-xs font-normal text-muted-foreground">/mes</span>
                            </>
                          ) : (
                            <span className="text-xs text-muted-foreground">—</span>
                          )}
```

por:

```tsx
                          {client.planMonthlyFee != null ? (
                            <div className="flex flex-col items-start gap-1">
                              <span>
                                {formatCurrency(client.planMonthlyFee)}
                                <span className="text-xs font-normal text-muted-foreground">/mes</span>
                              </span>
                              {client.planTypeId === PlanType.Rent && (
                                <>
                                  <span className="text-xs font-normal text-muted-foreground">
                                    {formatUserSeats(client.maxUsers, client.activeUsersCount ?? 0)} usuarios
                                  </span>
                                  {client.rentStatus != null && client.rentStatus !== RentStatus.Current && (
                                    <Badge
                                      className={cn(
                                        "gap-1 whitespace-nowrap font-normal",
                                        rentStatusBadgeClass(client.rentStatus as RentStatus),
                                      )}
                                      title={
                                        client.nextRentPaymentDate
                                          ? `Próximo pago: ${formatDate(client.nextRentPaymentDate)}`
                                          : undefined
                                      }
                                    >
                                      <AlertTriangle className="h-3 w-3" />
                                      {rentStatusLabel(client.rentStatus as RentStatus, client.rentDaysToDue)}
                                    </Badge>
                                  )}
                                </>
                              )}
                            </div>
                          ) : (
                            <span className="text-xs text-muted-foreground">—</span>
                          )}
```

> `cn`, `AlertTriangle`, `Badge` y `formatDate` ya están importados en este archivo para el aviso de certificados.

- [ ] **Step 8: Verificar lint y build**

Run: `npm run lint && npm run build`
Expected: sin errores.

- [ ] **Step 9: Verificar en el navegador**

En `/clientes`, editar un cliente y asignarle el plan de renta: debe aparecer el bloque "Renta". Poner el último pago en `2026-09-01` y activar "Pagó el año completo": el próximo pago debe auto-sugerirse como `2027-09-01`. Guardar, reabrir el cliente y confirmar que **las fechas se muestran iguales a lo guardado** (si sale un día antes, revisar el `[JsonConverter]` de la Task 3). Poner el próximo pago en una fecha pasada y confirmar el badge rojo "Vencida hace N días" en la tabla.

- [ ] **Step 10: Commit**

```bash
git add app/clientes/page.tsx
git commit -m "feat(clientes): campos de renta, sugerencia de próximo pago y aviso de vencimiento"
```

---

### Task 9: Frontend — página `/renta`, navegación y aviso en el dashboard

**Files:**
- Create: `app/renta/page.tsx`
- Create: `components/rent-alert.tsx`
- Modify: `components/sidebar.tsx:23-32`
- Modify: `app/page.tsx`

**Interfaces:**
- Consumes: `getRentClients` de la Task 6; `ClientRent`, `RentStatus`; `rentStatusLabel`, `rentStatusBadgeClass`, `formatUserSeats` de `@/lib/rent`.
- Produces: la ruta `/renta`; `<RentAlert />` para el dashboard.

- [ ] **Step 1: Agregar el item de navegación**

En `components/sidebar.tsx`, agregar `Home` a los iconos importados de `lucide-react` (o usar `Layers`, ya importado; aquí usamos `Home` por claridad):

```ts
  Home,
```

En el arreglo `navigation`, después del item de Consumo:

```ts
  { name: "Renta", href: "/renta", icon: Home },
```

Y agregar `"Renta"` al arreglo de items solo para SA:

```ts
const saOnlyItems = ["Certificación", "Consumo", "Renta", "Planes"]
```

- [ ] **Step 2: Crear el aviso reusable**

Crear `components/rent-alert.tsx`:

```tsx
"use client"

import { useEffect, useState } from "react"
import Link from "next/link"
import { AlertTriangle } from "lucide-react"
import { getRentClients } from "@/services/rent.service"
import { RentStatus } from "@/types/rent.type"
import { useAuthStore } from "@/store/authStore"

/** Aviso de rentas vencidas o por vencer. Solo se renderiza para SA y cuando hay algo que avisar. */
export function RentAlert() {
  const user = useAuthStore((state) => state.user)
  const isSA = user?.userType === 1

  const [overdue, setOverdue] = useState(0)
  const [dueSoon, setDueSoon] = useState(0)

  useEffect(() => {
    if (!isSA) return

    let cancelled = false
    getRentClients()
      .then((rows) => {
        if (cancelled) return
        setOverdue(rows.filter((r) => r.rentStatus === RentStatus.Overdue).length)
        setDueSoon(rows.filter((r) => r.rentStatus === RentStatus.DueSoon).length)
      })
      .catch(() => {
        // El aviso es informativo: si falla, no se muestra nada.
      })

    return () => {
      cancelled = true
    }
  }, [isSA])

  if (!isSA || overdue + dueSoon === 0) return null

  const parts = [
    overdue > 0 ? `${overdue} renta${overdue === 1 ? "" : "s"} vencida${overdue === 1 ? "" : "s"}` : null,
    dueSoon > 0 ? `${dueSoon} por vencer` : null,
  ].filter(Boolean)

  return (
    <Link
      href="/renta"
      className={`flex items-center gap-3 px-4 py-2 rounded-lg border text-sm transition hover:opacity-90 ${
        overdue > 0
          ? "bg-destructive/10 border-destructive/20 text-destructive"
          : "bg-amber-500/10 border-amber-500/20 text-amber-700"
      }`}
    >
      <AlertTriangle className="h-4 w-4 shrink-0" />
      <span className="flex-1">{parts.join(" y ")}.</span>
      <span className="text-xs underline font-semibold">Ver renta</span>
    </Link>
  )
}
```

- [ ] **Step 3: Insertar el aviso en el dashboard**

En `app/page.tsx`, agregar el import:

```ts
import { RentAlert } from "@/components/rent-alert"
```

y colocar el componente justo después del bloque `{/* Error Alert */}` (después de su `)}` de cierre y antes de `{/* Stats Grid */}`):

```tsx
        <RentAlert />
```

- [ ] **Step 4: Crear la página `/renta`**

Crear `app/renta/page.tsx`:

```tsx
"use client"

import { useCallback, useEffect, useMemo, useState } from "react"
import { MainLayout } from "@/components/main-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { RefreshCw } from "lucide-react"
import { cn } from "@/lib/utils"
import { getRentClients } from "@/services/rent.service"
import { ClientRent, RentStatus } from "@/types/rent.type"
import { formatUserSeats, rentStatusBadgeClass, rentStatusLabel } from "@/lib/rent"
import { useAuthStore } from "@/store/authStore"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

/** Las fechas llegan como "yyyy-MM-dd"; se formatean sin construir un Date para no mover el día. */
const formatCalendarDate = (value?: string | null) => {
  if (!value) return "—"
  const [year, month, day] = value.slice(0, 10).split("-")
  return `${day}/${month}/${year}`
}

export default function RentaPage() {
  const user = useAuthStore((state) => state.user)
  const isSA = user?.userType === 1

  const [rows, setRows] = useState<ClientRent[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      setRows(await getRentClients())
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo cargar la renta")
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isSA) load()
  }, [isSA, load])

  const totals = useMemo(
    () => ({
      overdue: rows.filter((r) => r.rentStatus === RentStatus.Overdue).length,
      dueSoon: rows.filter((r) => r.rentStatus === RentStatus.DueSoon).length,
      cycle: rows.reduce((sum, r) => sum + r.total, 0),
    }),
    [rows],
  )

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
            <h1 className="text-2xl font-bold text-foreground">Renta</h1>
            <p className="text-muted-foreground">
              Clientes con plan de renta: ciclo, descuento por pago adelantado y próximo pago.
            </p>
          </div>
          <Button variant="outline" size="icon" onClick={load} disabled={isLoading} title="Actualizar">
            <RefreshCw className={isLoading ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
          </Button>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <div className="grid gap-4 sm:grid-cols-3">
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Rentas vencidas</p>
              <p className="text-2xl font-bold text-destructive">{totals.overdue}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Por vencer</p>
              <p className="text-2xl font-bold">{totals.dueSoon}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Total de los ciclos</p>
              <p className="text-2xl font-bold">{currency.format(totals.cycle)}</p>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Clientes de renta</CardTitle>
            <CardDescription>{rows.length} clientes</CardDescription>
          </CardHeader>
          <CardContent className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Cliente</TableHead>
                  <TableHead>Plan</TableHead>
                  <TableHead className="text-right">Renta</TableHead>
                  <TableHead className="text-center">Usuarios</TableHead>
                  <TableHead className="text-center">Ciclo</TableHead>
                  <TableHead className="text-right">Descuento</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                  <TableHead>Último pago</TableHead>
                  <TableHead>Próximo pago</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-24 text-center text-muted-foreground">
                      Cargando renta...
                    </TableCell>
                  </TableRow>
                ) : rows.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-24 text-center text-muted-foreground">
                      No hay clientes con plan de renta
                    </TableCell>
                  </TableRow>
                ) : (
                  rows.map((row) => (
                    <TableRow key={row.clientGuidId} className={cn(row.clientInactive && "opacity-60")}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <span className="font-medium">{row.clientName}</span>
                          {row.clientInactive && <Badge variant="destructive">Inactivo</Badge>}
                        </div>
                        <code className="text-xs text-muted-foreground">{row.clientRnc}</code>
                      </TableCell>
                      <TableCell>{row.planName}</TableCell>
                      <TableCell className="text-right">{currency.format(row.monthlyFee)}</TableCell>
                      <TableCell className="text-center">
                        {formatUserSeats(row.maxUsers, row.activeUsersCount)}
                      </TableCell>
                      <TableCell className="text-center">
                        {row.rentPaidFullYear ? "Año completo" : "Mensual"}
                      </TableCell>
                      <TableCell className="text-right">
                        {row.rentDiscountPercent > 0 ? (
                          <>
                            {currency.format(row.discountAmount)}
                            <span className="block text-xs text-muted-foreground">{row.rentDiscountPercent}%</span>
                          </>
                        ) : (
                          <span className="text-xs text-muted-foreground">—</span>
                        )}
                      </TableCell>
                      <TableCell className="text-right font-semibold">{currency.format(row.total)}</TableCell>
                      <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                        {formatCalendarDate(row.lastRentPaymentDate)}
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        <div className="flex flex-col items-start gap-1">
                          <span className="text-xs text-muted-foreground">
                            {formatCalendarDate(row.nextRentPaymentDate)}
                          </span>
                          <Badge className={cn("font-normal", rentStatusBadgeClass(row.rentStatus))}>
                            {rentStatusLabel(row.rentStatus, row.rentDaysToDue)}
                          </Badge>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      </div>
    </MainLayout>
  )
}
```

- [ ] **Step 5: Verificar lint y build**

Run: `npm run lint && npm run build`
Expected: sin errores, y `/renta` en la lista de rutas generadas.

- [ ] **Step 6: Verificar en el navegador**

Entrar como SA: el item "Renta" aparece en el sidebar y la página lista el cliente de renta con su ciclo, descuento, total y semáforo. Poner el próximo pago de ese cliente en una fecha pasada y confirmar que el dashboard muestra el aviso rojo con enlace a `/renta`. Entrar con un usuario que no sea SA y confirmar que ni el item del sidebar ni el aviso aparecen.

- [ ] **Step 7: Commit**

```bash
git add app/renta/page.tsx components/rent-alert.tsx components/sidebar.tsx app/page.tsx
git commit -m "feat(renta): página de renta, item de navegación y aviso en el dashboard"
```

---

### Task 10: Frontend — filas de renta en `/consumo`

**Files:**
- Modify: `app/consumo/page.tsx`

**Interfaces:**
- Consumes: `ClientMonthlyUsage.planTypeId` de la Task 6; `PlanType` de la Task 6; `GET v1/Client/usage` con filas de renta de la Task 5.
- Produces: `/consumo` muestra clientes de renta con columnas de documentos vacías, "Cubierto" cuando el monto es 0, y totales que suman ambos tipos.

- [ ] **Step 1: Agregar el import de `PlanType`**

Reemplazar:

```ts
import { UNLIMITED_DOCUMENTS } from "@/types/plan.type"
```

por:

```ts
import { PlanType, UNLIMITED_DOCUMENTS } from "@/types/plan.type"
```

- [ ] **Step 2: Contar la renta en los totales**

Reemplazar el bloque `totals`:

```ts
  const totals = useMemo(
    () => ({
      documents: rows.reduce((sum, r) => sum + r.acceptedDocuments, 0),
      overage: rows.reduce((sum, r) => sum + r.overageAmount, 0),
      total: rows.reduce((sum, r) => sum + r.total, 0),
    }),
    [rows],
  )
```

por:

```ts
  const totals = useMemo(
    () => ({
      documents: rows.reduce((sum, r) => sum + r.acceptedDocuments, 0),
      overage: rows.reduce((sum, r) => sum + r.overageAmount, 0),
      rent: rows.filter((r) => r.planTypeId === PlanType.Rent).reduce((sum, r) => sum + r.total, 0),
      total: rows.reduce((sum, r) => sum + r.total, 0),
    }),
    [rows],
  )
```

- [ ] **Step 3: Agregar la tarjeta de renta al resumen**

Cambiar la grilla de tarjetas de `sm:grid-cols-3` a `sm:grid-cols-4`:

```tsx
        <div className="grid gap-4 sm:grid-cols-4">
```

e insertar una tarjeta antes de la de "Total a facturar":

```tsx
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Renta del mes</p>
              <p className="text-2xl font-bold">{currency.format(totals.rent)}</p>
            </CardContent>
          </Card>
```

- [ ] **Step 4: Renderizar las filas de renta**

Dentro de `rows.map((row) => { ... })`, reemplazar las dos primeras líneas del cuerpo:

```ts
                    const isOpen = expanded === row.clientGuidId
                    const hasTierDetail = row.overageDocuments > 0 && row.tiers.length > 0
```

por:

```ts
                    const isOpen = expanded === row.clientGuidId
                    const isRent = row.planTypeId === PlanType.Rent
                    const hasTierDetail = !isRent && row.overageDocuments > 0 && row.tiers.length > 0
```

Y reemplazar las seis celdas que van desde "Aceptados" hasta "Total":

```tsx
                          <TableCell className="text-right">{row.acceptedDocuments.toLocaleString("es-DO")}</TableCell>
                          <TableCell className="text-right">
                            {row.monthlyDocumentLimit === UNLIMITED_DOCUMENTS
                              ? "Ilimitado"
                              : row.monthlyDocumentLimit.toLocaleString("es-DO")}
                          </TableCell>
                          <TableCell className="text-right">{row.overageDocuments.toLocaleString("es-DO")}</TableCell>
                          <TableCell className="text-right">{currency.format(row.monthlyFee)}</TableCell>
                          <TableCell className="text-right">{currency.format(row.overageAmount)}</TableCell>
                          <TableCell className="text-right font-semibold">{currency.format(row.total)}</TableCell>
```

por:

```tsx
                          <TableCell className="text-right">
                            {isRent ? (
                              <span className="text-xs text-muted-foreground">—</span>
                            ) : (
                              row.acceptedDocuments.toLocaleString("es-DO")
                            )}
                          </TableCell>
                          <TableCell className="text-right">
                            {isRent ? (
                              <span className="text-xs text-muted-foreground">Renta</span>
                            ) : row.monthlyDocumentLimit === UNLIMITED_DOCUMENTS ? (
                              "Ilimitado"
                            ) : (
                              row.monthlyDocumentLimit.toLocaleString("es-DO")
                            )}
                          </TableCell>
                          <TableCell className="text-right">
                            {isRent ? (
                              <span className="text-xs text-muted-foreground">—</span>
                            ) : (
                              row.overageDocuments.toLocaleString("es-DO")
                            )}
                          </TableCell>
                          <TableCell className="text-right">{currency.format(row.monthlyFee)}</TableCell>
                          <TableCell className="text-right">
                            {isRent ? (
                              <span className="text-xs text-muted-foreground">—</span>
                            ) : (
                              currency.format(row.overageAmount)
                            )}
                          </TableCell>
                          <TableCell className="text-right font-semibold">
                            {isRent && row.total === 0 ? (
                              <Badge className="bg-green-100 font-normal text-green-800 hover:bg-green-100">
                                Cubierto
                              </Badge>
                            ) : (
                              currency.format(row.total)
                            )}
                          </TableCell>
```

- [ ] **Step 5: Ajustar los textos de la página**

Reemplazar el subtítulo:

```tsx
            <p className="text-muted-foreground">
              Comprobantes aceptados por cliente con plan activo y monto a facturar.
            </p>
```

por:

```tsx
            <p className="text-muted-foreground">
              Comprobantes aceptados y renta del mes por cliente con plan activo, con el monto a facturar.
            </p>
```

Y el `CardDescription` de la tabla:

```tsx
            <CardDescription>{rows.length} clientes con consumo registrado</CardDescription>
```

por:

```tsx
            <CardDescription>{rows.length} clientes por facturar</CardDescription>
```

Y el texto de la fila vacía:

```tsx
                      No hay consumo registrado para este mes
```

por:

```tsx
                      No hay consumo ni renta por facturar en este mes
```

- [ ] **Step 6: Verificar lint y build**

Run: `npm run lint && npm run build`
Expected: sin errores.

- [ ] **Step 7: Verificar en el navegador**

En `/consumo`, con el cliente de renta del ejemplo (último pago 2026-09-01, año completo, 10%, próximo pago 2027-09-01):
- Septiembre 2026 → la fila aparece con "Renta" en la columna de límite, guiones en las de documentos, y el badge verde "Cubierto".
- Septiembre 2027 → la misma fila con total `RD$21,600.00`, y la tarjeta "Renta del mes" con ese monto.

- [ ] **Step 8: Commit**

```bash
git add app/consumo/page.tsx
git commit -m "feat(consumo): filas de renta en el reporte mensual"
```

---

## Cobertura del spec

| Requisito del spec | Task |
|---|---|
| `PlanTypeEnum { Documents = 1, Rent = 2 }` | 1 |
| Fórmula del ciclo (meses × renta × (1 − desc/100)) | 1 |
| Monto del mes consultado (cubierto → 0, renovación/vencido → ciclo) | 1 |
| Semáforo `SinFecha` / `Vencido` / `PorVencer` / `AlDía` | 1 |
| Descuento independiente del flag de año completo | 1 (`Calculate`), 8 (UI) |
| Plan de renta rechaza tramos y exige `MaxUsers` | 1 (regla), 4 (endpoint) |
| Plan de comprobantes rechaza `MaxUsers` | 1 (regla), 4 (endpoint) |
| `Plan.PlanTypeId`, `Plan.MaxUsers` | 2 |
| Cuatro campos de renta en `Client` | 2 |
| `RentPaymentWarningDays` default 15 en `AppSettings` y los tres `appsettings` | 2 |
| Migración única con las seis columnas | 2 |
| `MaxUsers = -1` = ilimitado | 1, 2, 6, 7 |
| Fechas calendario sin desplazamiento de zona | 3 |
| `ClientRentDto` con `Total` = ciclo con descuento | 3, 5 |
| `ClientMonthlyUsageDto.PlanTypeId` y campos de documentos en cero en filas de renta | 3, 5 |
| `ClientViewDto` con `PlanTypeId`, `MaxUsers`, `ActiveUsersCount`, `RentStatus`, `RentDaysToDue`, `RentCycleAmount` | 3, 5 |
| Plan de renta fuerza `MonthlyDocumentLimit = -1` | 4 |
| Plan de renta no registra consumo mensual | 4 |
| `ActiveUsersCount` = usuarios activos y no eliminados | 5 |
| `FillRentStatusAsync` hermana de `FillCertificateExpirationAsync` | 5 |
| `GET v1/Client/rent`, incluyendo clientes inactivos marcados | 5 |
| Unión de renta en `GET v1/Client/usage` (lista y por cliente) | 5 |
| `NextRentPaymentDate >= LastRentPaymentDate` validado en backend | 5 |
| `RentDiscountPercent` en rango 0–100 | 3 (`[Range]`), 8 (UI) |
| Selector de tipo en `/configuraciones/planes`, ocultando límite y tramos | 7 |
| Campos de renta en el formulario de cliente, visibles solo si el plan es de renta | 8 |
| Sugerencia editable del próximo pago (+12 / +1 mes) | 6 (`suggestNextPaymentDate`), 8 (UI) |
| Badge de renta vencida/por vencer y contador "3 de 5 usuarios" en `/clientes` | 8 |
| Página `/renta` con semáforo, descuento y total | 9 |
| Aviso de renta en el dashboard | 9 |
| Filas de renta en `/consumo` con "Cubierto" | 10 |
| Tope de usuarios informativo (no bloquea) | Ninguna validación de bloqueo, por diseño |

**Fuera de alcance, confirmado sin task:** historial de pagos de renta, corte automático por renta vencida, bloqueo al superar el tope de usuarios, control de concurrencia del API, generación automática de la factura.
