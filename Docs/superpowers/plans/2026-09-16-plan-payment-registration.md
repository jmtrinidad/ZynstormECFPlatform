# Registro de pagos de planes e historial — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Registrar desde `/pagos` el pago de la mensualidad del plan y de excedentes de meses cerrados, con historial (recibo + líneas), actualización de fechas del cliente y reactivación si estaba suspendido por pago.

**Architecture:** Reglas puras en `PaymentRegistrationPolicy` (con tests). `ClientPaymentRegistrationService` (Services/Billing) arma el preview, registra el recibo `ClientPayment` + `ClientPaymentItem` y actualiza `Client` en una transacción, y calcula excedentes pendientes con `BillingCalculator`. `ClientController` expone preview/registro/historial y agrega el excedente pendiente a `GET v1/Client/payments`. El frontend agrega tres diálogos (registrar, pago recibido, historial) a la página `/pagos`.

**Tech Stack:** .NET 10, EF Core + Npgsql (PostgreSQL), AutoMapper, xUnit. Frontend Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-09-16-plan-payment-registration-design.md`

## Global Constraints

- Repos: backend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, frontend `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`. Rama `feature/client-plans-monthly-usage` en ambos.
- Comandos en PowerShell con rutas absolutas. Backend: `dotnet build C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.slnx -v q`, `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q`.
- EF siempre con `$env:ASPNETCORE_ENVIRONMENT = 'Development'` (base `zynstorm_ecf_platform_dev2_db`). Antes de `database update`, confirmar con `dotnet ef dbcontext info` que `Database name` es `zynstorm_ecf_platform_dev2_db`; si no, detenerse.
- Frontend: `pnpm exec tsc --noEmit -p .` y `pnpm exec eslint <archivos>` desde el repo frontend. Hay un error de tipos preexistente en `app/configuraciones/cuenta/page.tsx` que no se toca.
- Monto de la mensualidad: **fijo**, `PaymentCalculator.Calculate(plan.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent).Total`. Meses y descuento del cliente, no editables.
- Nuevo próximo pago = `NextPaymentDate` anterior + meses; sin fecha previa, desde la fecha del pago. `LastPaymentDate` = fecha del pago.
- Excedente: solo meses **cerrados** (anteriores al mes actual en hora RD), monto fijo `BillingCalculator.Calculate(...).OverageAmount`, una sola vez por mes. No afecta avisos, estado ni suspensión.
- Reactivación solo si estaba suspendido por pago (`PaymentSuspendedAtUtc != null`), el pago incluye mensualidad y el nuevo próximo pago es posterior a hoy (RD).
- Métodos de pago: 1 Efectivo, 2 Transferencia, 3 Tarjeta, 4 Cheque, 5 Otro. Referencia ≤ 100, nota ≤ 500. Fecha del pago requerida y no futura.
- Fechas calendario en DTOs: `DateTime?` con `[JsonConverter(typeof(CalendarDateJsonConverter))]` (formato `yyyy-MM-dd`).
- Número de recibo: `REC-` + `ClientPaymentId` con 6 dígitos (`REC-000001`).
- Endpoints solo SA (`if (!IsSA) return Forbid();`).
- Mensajes y UI en español; identificadores en inglés.

---

### Task 1: `PaymentRegistrationPolicy` (reglas puras)

**Files:**
- Create: `ZynstormECFPlatform.Core/Enums/PaymentMethod.cs`
- Create: `ZynstormECFPlatform.Core/Enums/ClientPaymentItemType.cs`
- Create: `ZynstormECFPlatform.Services/Billing/PaymentRegistrationPolicy.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/PaymentRegistrationPolicyTests.cs`

**Interfaces:**
- Produces:
  - `enum PaymentMethod { Cash = 1, Transfer = 2, Card = 3, Check = 4, Other = 5 }` (namespace `ZynstormECFPlatform.Core.Enums`)
  - `enum ClientPaymentItemType { PlanFee = 1, Overage = 2 }` (mismo namespace)
  - `static class PaymentRegistrationPolicy` (namespace `ZynstormECFPlatform.Services.Billing`):
    - `const int ReferenceMaxLength = 100; const int NotesMaxLength = 500;`
    - `DateTime CalculateNewNextPaymentDate(DateTime? currentNextPaymentDate, DateTime paymentDate, int paidMonths)`
    - `bool IsOverageMonthPayable(int year, int month, DateTime today)`
    - `bool ShouldReactivate(bool paymentSuspended, bool includesPlanFee, DateTime? newNextPaymentDate, DateTime today)`
    - `List<string> ValidateRequest(DateTime? paymentDate, int paymentMethod, bool includePlanFee, int overageCount, string? reference, string? notes, DateTime today)`
    - `string FormatReceiptNumber(int clientPaymentId)`

- [ ] **Step 1: Escribir los tests**

`ZynstormECFPlatform.Tests/Billing/PaymentRegistrationPolicyTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentRegistrationPolicyTests
{
    private static readonly DateTime Today = new(2026, 9, 18);

    [Fact]
    public void NewNextPaymentDate_AdvancesFromDueDate_NotFromPaymentDate()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), new DateTime(2026, 9, 18), 1);
        Assert.Equal(new DateTime(2026, 10, 15), next);
    }

    [Fact]
    public void NewNextPaymentDate_AdvancesByPaidMonths()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), new DateTime(2026, 9, 1), 12);
        Assert.Equal(new DateTime(2027, 9, 15), next);
    }

    [Fact]
    public void NewNextPaymentDate_WithoutPreviousDate_StartsFromPaymentDate()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(null, new DateTime(2026, 9, 18), 3);
        Assert.Equal(new DateTime(2026, 12, 18), next);
    }

    [Fact]
    public void NewNextPaymentDate_EndOfMonth_ClampsToLastDay()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2027, 1, 31), new DateTime(2027, 2, 1), 1);
        Assert.Equal(new DateTime(2027, 2, 28), next);
    }

    [Fact]
    public void NewNextPaymentDate_ZeroMonths_TreatedAsOne()
    {
        var next = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(new DateTime(2026, 9, 15), Today, 0);
        Assert.Equal(new DateTime(2026, 10, 15), next);
    }

    [Theory]
    [InlineData(2026, 8, true)]
    [InlineData(2025, 12, true)]
    [InlineData(2026, 9, false)]
    [InlineData(2026, 10, false)]
    public void OverageMonthPayable_OnlyClosedMonths(int year, int month, bool expected)
    {
        Assert.Equal(expected, PaymentRegistrationPolicy.IsOverageMonthPayable(year, month, Today));
    }

    [Fact]
    public void Reactivate_WhenSuspendedAndPlanFeeBringsClientCurrent()
    {
        Assert.True(PaymentRegistrationPolicy.ShouldReactivate(true, true, new DateTime(2026, 10, 15), Today));
    }

    [Fact]
    public void Reactivate_NotWhenStillOverdueAfterPayment()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, true, new DateTime(2026, 8, 15), Today));
    }

    [Fact]
    public void Reactivate_NotWhenNextPaymentIsToday()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, true, Today, Today));
    }

    [Fact]
    public void Reactivate_NotForOverageOnlyPayment()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(true, false, new DateTime(2027, 1, 1), Today));
    }

    [Fact]
    public void Reactivate_NotWhenNotSuspended()
    {
        Assert.False(PaymentRegistrationPolicy.ShouldReactivate(false, true, new DateTime(2026, 10, 15), Today));
    }

    [Fact]
    public void ValidateRequest_ValidPlanFeeRequest_HasNoErrors()
    {
        Assert.Empty(PaymentRegistrationPolicy.ValidateRequest(Today, 2, true, 0, "TRX-1", null, Today));
    }

    [Fact]
    public void ValidateRequest_OverageOnly_IsValid()
    {
        Assert.Empty(PaymentRegistrationPolicy.ValidateRequest(Today, 1, false, 2, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_MissingDate_ReturnsError()
    {
        Assert.Contains("La fecha del pago es requerida.", PaymentRegistrationPolicy.ValidateRequest(null, 1, true, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_FutureDate_ReturnsError()
    {
        Assert.Contains("La fecha del pago no puede ser futura.",
            PaymentRegistrationPolicy.ValidateRequest(Today.AddDays(1), 1, true, 0, null, null, Today));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void ValidateRequest_InvalidMethod_ReturnsError(int method)
    {
        Assert.Contains("El método de pago no es válido.",
            PaymentRegistrationPolicy.ValidateRequest(Today, method, true, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_NothingSelected_ReturnsError()
    {
        Assert.Contains("Debe seleccionar la mensualidad o al menos un excedente.",
            PaymentRegistrationPolicy.ValidateRequest(Today, 1, false, 0, null, null, Today));
    }

    [Fact]
    public void ValidateRequest_TooLongReferenceAndNotes_ReturnErrors()
    {
        var errors = PaymentRegistrationPolicy.ValidateRequest(Today, 1, true, 0, new string('x', 101), new string('x', 501), Today);
        Assert.Contains("La referencia no puede exceder 100 caracteres.", errors);
        Assert.Contains("La nota no puede exceder 500 caracteres.", errors);
    }

    [Fact]
    public void ReceiptNumber_IsPaddedToSixDigits()
    {
        Assert.Equal("REC-000042", PaymentRegistrationPolicy.FormatReceiptNumber(42));
    }
}
```

- [ ] **Step 2: Verificar que falla**

Run: `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~PaymentRegistrationPolicyTests"`
Expected: error de compilación `The name 'PaymentRegistrationPolicy' does not exist`.

- [ ] **Step 3: Implementar enums y política**

`ZynstormECFPlatform.Core/Enums/PaymentMethod.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Enums;

/// <summary>Método con el que se recibió un pago. Se serializa como número.</summary>
public enum PaymentMethod
{
    Cash = 1,
    Transfer = 2,
    Card = 3,
    Check = 4,
    Other = 5
}
```

`ZynstormECFPlatform.Core/Enums/ClientPaymentItemType.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Enums;

/// <summary>Concepto de una línea de pago.</summary>
public enum ClientPaymentItemType
{
    PlanFee = 1,
    Overage = 2
}
```

`ZynstormECFPlatform.Services/Billing/PaymentRegistrationPolicy.cs`:

```csharp
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas para registrar un pago de plan o de excedente. Funciones puras.</summary>
public static class PaymentRegistrationPolicy
{
    public const int ReferenceMaxLength = 100;
    public const int NotesMaxLength = 500;

    /// <summary>
    /// El ciclo avanza desde la fecha que vencía (no desde el día en que se pagó).
    /// Sin fecha previa, se cuenta desde la fecha del pago.
    /// </summary>
    public static DateTime CalculateNewNextPaymentDate(DateTime? currentNextPaymentDate, DateTime paymentDate, int paidMonths) =>
        (currentNextPaymentDate ?? paymentDate).Date.AddMonths(Math.Max(1, paidMonths));

    /// <summary>Solo se cobra el excedente de meses cerrados (anteriores al mes de <paramref name="today"/>).</summary>
    public static bool IsOverageMonthPayable(int year, int month, DateTime today) =>
        new DateTime(year, month, 1) < new DateTime(today.Year, today.Month, 1);

    public static bool ShouldReactivate(bool paymentSuspended, bool includesPlanFee, DateTime? newNextPaymentDate, DateTime today) =>
        paymentSuspended
        && includesPlanFee
        && newNextPaymentDate is DateTime next
        && next.Date > today.Date;

    public static List<string> ValidateRequest(
        DateTime? paymentDate,
        int paymentMethod,
        bool includePlanFee,
        int overageCount,
        string? reference,
        string? notes,
        DateTime today)
    {
        var errors = new List<string>();

        if (paymentDate is not DateTime date)
            errors.Add("La fecha del pago es requerida.");
        else if (date.Date > today.Date)
            errors.Add("La fecha del pago no puede ser futura.");

        if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
            errors.Add("El método de pago no es válido.");

        if (!includePlanFee && overageCount <= 0)
            errors.Add("Debe seleccionar la mensualidad o al menos un excedente.");

        if (reference?.Trim().Length > ReferenceMaxLength)
            errors.Add($"La referencia no puede exceder {ReferenceMaxLength} caracteres.");

        if (notes?.Trim().Length > NotesMaxLength)
            errors.Add($"La nota no puede exceder {NotesMaxLength} caracteres.");

        return errors;
    }

    public static string FormatReceiptNumber(int clientPaymentId) => $"REC-{clientPaymentId:D6}";
}
```

- [ ] **Step 4: Verificar que pasan**

Run: `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~PaymentRegistrationPolicyTests"`
Expected: `Passed!` con 0 fallos (23 casos contando los `InlineData`).

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Core/Enums/PaymentMethod.cs ZynstormECFPlatform.Core/Enums/ClientPaymentItemType.cs ZynstormECFPlatform.Services/Billing/PaymentRegistrationPolicy.cs ZynstormECFPlatform.Tests/Billing/PaymentRegistrationPolicyTests.cs
git commit -m "feat(payments): reglas de registro de pagos (fechas, excedente, reactivación, validación)"
```

---

### Task 2: Modelo de datos, migración y servicios de datos

**Files:**
- Create: `ZynstormECFPlatform.Core/Entities/ClientPayment.cs`
- Create: `ZynstormECFPlatform.Core/Entities/ClientPaymentItem.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs` (colección `Payments`)
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs` (después del bloque `modelBuilder.Entity<ClientMonthlyUsage>`, ~línea 612)
- Create: `ZynstormECFPlatform.Abstractions/DataServices/IClientPaymentService.cs`, `IClientPaymentItemService.cs`
- Create: `ZynstormECFPlatform.Data.Services/ClientPaymentService.cs`, `ClientPaymentItemService.cs`
- Create (generada): `ZynstormECFPlatform.Data/Migrations/*_AddClientPayments.cs`

**Interfaces:**
- Produces:
  - `ClientPayment { int ClientPaymentId; int ClientId; DateTime PaymentDate; int PaymentMethod; string? Reference; string? Notes; decimal TotalAmount; string? RegisteredByUserId; Client Client; ICollection<ClientPaymentItem> Items }`
  - `ClientPaymentItem { int ClientPaymentItemId; int ClientPaymentId; int ItemType; decimal Amount; int? PlanId; string PlanName; int? MonthsCovered; decimal? GrossAmount; decimal? DiscountPercent; decimal? DiscountAmount; DateTime? PreviousNextPaymentDate; DateTime? NewNextPaymentDate; int? ClientMonthlyUsageId; int? Year; int? Month; int? OverageDocuments; ClientPayment ClientPayment; ClientMonthlyUsage? ClientMonthlyUsage }`
  - `Client.Payments`
  - `IClientPaymentService : IRepository<ClientPayment>`, `IClientPaymentItemService : IRepository<ClientPaymentItem>` (DI automática por nombre en `AddDataServices`).

- [ ] **Step 1: Entidades**

`ZynstormECFPlatform.Core/Entities/ClientPayment.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Recibo de un pago recibido de un cliente. Sus líneas indican qué cubre.</summary>
public partial class ClientPayment : BaseEntity
{
    public int ClientPaymentId { get; set; }

    public int ClientId { get; set; }

    /// <summary>Fecha calendario en que se recibió el pago.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary><see cref="Enums.PaymentMethod"/>.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string? RegisteredByUserId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ClientPaymentItem> Items { get; set; } = [];
}
```

`ZynstormECFPlatform.Core/Entities/ClientPaymentItem.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Línea de un recibo: mensualidad del plan o excedente de un mes.</summary>
public partial class ClientPaymentItem : BaseEntity
{
    public int ClientPaymentItemId { get; set; }

    public int ClientPaymentId { get; set; }

    /// <summary><see cref="Enums.ClientPaymentItemType"/>.</summary>
    public int ItemType { get; set; }

    public decimal Amount { get; set; }

    // Snapshot del plan al momento del pago
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    // Mensualidad
    public int? MonthsCovered { get; set; }

    public decimal? GrossAmount { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime? PreviousNextPaymentDate { get; set; }

    public DateTime? NewNextPaymentDate { get; set; }

    // Excedente
    public int? ClientMonthlyUsageId { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? OverageDocuments { get; set; }

    public virtual ClientPayment ClientPayment { get; set; } = null!;

    public virtual ClientMonthlyUsage? ClientMonthlyUsage { get; set; }
}
```

En `Client.cs`, después de `public virtual ICollection<ClientMonthlyUsage> MonthlyUsages { get; set; } = [];`:

```csharp

    public virtual ICollection<ClientPayment> Payments { get; set; } = [];
```

- [ ] **Step 2: Configuración EF**

En `StorageContext.cs`, justo después del `});` que cierra `modelBuilder.Entity<ClientMonthlyUsage>` (antes de `modelBuilder.Entity<ClientBranche>`):

```csharp

        modelBuilder.Entity<ClientPayment>(entity =>
        {
            entity.HasKey(e => e.ClientPaymentId);

            entity.Property(e => e.PaymentDate)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.Reference)
                  .HasMaxLength(100)
                  .IsUnicode(false);

            entity.Property(e => e.Notes)
                  .HasMaxLength(500)
                  .IsUnicode(false);

            entity.Property(e => e.RegisteredByUserId)
                  .HasMaxLength(450)
                  .IsUnicode(false);

            entity.HasIndex(e => new { e.ClientId, e.PaymentDate })
                  .HasDatabaseName("IX_ClientPayment_ClientId_PaymentDate");

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.Client)
                  .WithMany(p => p.Payments)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientPayment_Client");
        });

        modelBuilder.Entity<ClientPaymentItem>(entity =>
        {
            entity.HasKey(e => e.ClientPaymentItemId);

            entity.Property(e => e.PlanName)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.PreviousNextPaymentDate)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.NewNextPaymentDate)
                  .HasColumnType(DateTimeColumnType);

            // Un mes de excedente se paga una sola vez.
            entity.HasIndex(e => e.ClientMonthlyUsageId)
                  .IsUnique()
                  .HasFilter("\"ItemType\" = 2 AND \"ClientMonthlyUsageId\" IS NOT NULL AND NOT \"IsDeleted\"")
                  .HasDatabaseName("IX_ClientPaymentItem_Overage_Usage");

            entity.Property(e => e.RegisteredAt)
                  .HasColumnType(DateTimeColumnType)
                  .HasDefaultValueSql(DefaultDateTimeSqlValue);

            entity.Property(c => c.LastUpdateUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(c => c.DeletedTimeUtc)
                  .HasColumnType(DateTimeColumnType);

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.Property(e => e.GuidId)
                  .IsRequired()
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .HasDefaultValueSql(DefaultGUIDSqlValue);

            entity.HasQueryFilter(c => !c.IsDeleted);

            entity.HasOne(d => d.ClientPayment)
                  .WithMany(p => p.Items)
                  .HasForeignKey(d => d.ClientPaymentId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_ClientPaymentItem_ClientPayment");

            entity.HasOne(d => d.ClientMonthlyUsage)
                  .WithMany()
                  .HasForeignKey(d => d.ClientMonthlyUsageId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientPaymentItem_ClientMonthlyUsage");
        });
```

- [ ] **Step 3: Servicios de datos**

`ZynstormECFPlatform.Abstractions/DataServices/IClientPaymentService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientPaymentService : IRepository<ClientPayment>
{
}
```

`ZynstormECFPlatform.Abstractions/DataServices/IClientPaymentItemService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientPaymentItemService : IRepository<ClientPaymentItem>
{
}
```

`ZynstormECFPlatform.Data.Services/ClientPaymentService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientPaymentService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientPayment>(context, sqlGenerator), IClientPaymentService
{
}
```

`ZynstormECFPlatform.Data.Services/ClientPaymentItemService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientPaymentItemService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientPaymentItem>(context, sqlGenerator), IClientPaymentItemService
{
}
```

- [ ] **Step 4: Build**

Run: `dotnet build C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.slnx -v q`
Expected: `Build succeeded`, 0 errores.

- [ ] **Step 5: Generar la migración**

Run:
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'; dotnet ef migrations add AddClientPayments --project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Data --startup-project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Web.Api
```
Expected: `Done.` El `Up` generado solo contiene `CreateTable` de `"ClientPayment"` y `"ClientPaymentItem"`, los índices `IX_ClientPayment_ClientId_PaymentDate`, `IX_ClientPaymentItem_Overage_Usage` (único, con `filter`), `IX_ClientPaymentItem_ClientPaymentId` y las FKs `FK_ClientPayment_Client`, `FK_ClientPaymentItem_ClientPayment` (cascade), `FK_ClientPaymentItem_ClientMonthlyUsage`. Si aparece cualquier otro cambio, quitarlo de `Up` y `Down`.

- [ ] **Step 6: Aplicar a dev2**

Run:
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'; dotnet ef dbcontext info --project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Data --startup-project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Web.Api
```
Expected: `Database name: zynstorm_ecf_platform_dev2_db`. Si no, **detenerse**. Luego:
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'; dotnet ef database update --project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Data --startup-project C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Web.Api
```
Expected: `Applying migration '..._AddClientPayments'.` y `Done.`

- [ ] **Step 7: Tests + commit**

Run: `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q` → todos pasan.

```bash
git add ZynstormECFPlatform.Core/Entities ZynstormECFPlatform.Data/StorageContext.cs ZynstormECFPlatform.Data/Migrations ZynstormECFPlatform.Abstractions/DataServices ZynstormECFPlatform.Data.Services
git commit -m "feat(payments): entidades ClientPayment y ClientPaymentItem con índice único de excedente"
```

---

### Task 3: DTOs y `ClientPaymentRegistrationService`

**Files:**
- Create: `ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs`
- Modify: `ZynstormECFPlatform.Dtos/ClientPaymentDtos.cs` (campos de excedente pendiente)
- Create: `ZynstormECFPlatform.Services/Billing/IClientPaymentRegistrationService.cs`
- Create: `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (después de `services.AddTransient<Billing.IPaymentReminderService, Billing.PaymentReminderService>();`)

**Interfaces:**
- Consumes: `PaymentRegistrationPolicy`, `PaymentMethod`, `ClientPaymentItemType` (Task 1); `ClientPayment`, `ClientPaymentItem`, `IClientPaymentService`, `IClientPaymentItemService` (Task 2); existentes `PaymentCalculator.Calculate`, `BillingCalculator.Calculate`, `OverageTier`, `IClientService`, `IClientMonthlyUsageService`, `IPlanOverageTierService`, `IUnitOfWork`, `DateTimeExtensions.DrNow`.
- Produces:
  - DTOs: `PendingOverageDto`, `PlanFeePreviewDto`, `PaymentPreviewDto`, `RegisterPaymentRequestDto`, `PaymentReceiptItemDto`, `PaymentReceiptDto` (propiedades exactas en el Step 1).
  - `ClientPaymentDto.PendingOverageAmount` (`decimal`), `ClientPaymentDto.PendingOverageMonths` (`int`).
  - `enum PaymentRegistrationOutcome { Ok, NotFound, Invalid, Conflict }`
  - `sealed record PaymentRegistrationResult<T>(PaymentRegistrationOutcome Outcome, T? Value, List<string> Errors)`
  - `IClientPaymentRegistrationService`:
    - `Task<PaymentRegistrationResult<PaymentPreviewDto>> GetPreviewAsync(string clientGuid, CancellationToken cancellationToken = default)`
    - `Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(string clientGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default)`
    - `Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string clientGuid, CancellationToken cancellationToken = default)`
    - `Task<Dictionary<int, List<PendingOverageDto>>> GetPendingOveragesAsync(IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken = default)`

- [ ] **Step 1: DTOs**

`ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs`:

```csharp
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Mes cerrado con excedente sin pagar.</summary>
public class PendingOverageDto
{
    public int ClientMonthlyUsageId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int OverageDocuments { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Mensualidad que se registraría hoy con los meses y descuento del cliente.</summary>
public class PlanFeePreviewDto
{
    public string PlanName { get; set; } = string.Empty;
    public int PlanTypeId { get; set; }
    public decimal MonthlyFee { get; set; }
    public int MonthsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? CurrentNextPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NewNextPaymentDate { get; set; }
}

public class PaymentPreviewDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool PaymentSuspended { get; set; }

    /// <summary>Null si el plan no tiene mensualidad.</summary>
    public PlanFeePreviewDto? PlanFee { get; set; }

    public List<PendingOverageDto> PendingOverages { get; set; } = [];
}

public class RegisterPaymentRequestDto
{
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }

    /// <summary>1 Efectivo, 2 Transferencia, 3 Tarjeta, 4 Cheque, 5 Otro.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IncludePlanFee { get; set; }
    public List<int> OverageUsageIds { get; set; } = [];
}

public class PaymentReceiptItemDto
{
    /// <summary>1 Mensualidad, 2 Excedente.</summary>
    public int ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? MonthsCovered { get; set; }
    public decimal? DiscountAmount { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PreviousNextPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NewNextPaymentDate { get; set; }

    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? OverageDocuments { get; set; }
}

public class PaymentReceiptDto
{
    public int ClientPaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }

    public int PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Próximo pago tras este recibo; null si el recibo no incluyó mensualidad.</summary>
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }

    /// <summary>Solo en la respuesta del registro: el cliente fue reactivado.</summary>
    public bool Reactivated { get; set; }

    /// <summary>Solo en la respuesta del registro: estaba suspendido y sigue suspendido.</summary>
    public bool StillSuspended { get; set; }

    public List<PaymentReceiptItemDto> Items { get; set; } = [];
}
```

En `ClientPaymentDtos.cs`, dentro de `ClientPaymentDto`, después de `public bool HasEmail { get; set; }`:

```csharp

    /// <summary>Suma del excedente de meses cerrados sin pagar (solo planes de comprobantes).</summary>
    public decimal PendingOverageAmount { get; set; }

    public int PendingOverageMonths { get; set; }
```

- [ ] **Step 2: Interfaz y resultado**

`ZynstormECFPlatform.Services/Billing/IClientPaymentRegistrationService.cs`:

```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public enum PaymentRegistrationOutcome
{
    Ok,
    NotFound,
    Invalid,
    Conflict
}

public sealed record PaymentRegistrationResult<T>(PaymentRegistrationOutcome Outcome, T? Value, List<string> Errors)
{
    public static PaymentRegistrationResult<T> Success(T value) => new(PaymentRegistrationOutcome.Ok, value, []);
    public static PaymentRegistrationResult<T> NotFound(string message) => new(PaymentRegistrationOutcome.NotFound, default, [message]);
    public static PaymentRegistrationResult<T> Invalid(List<string> errors) => new(PaymentRegistrationOutcome.Invalid, default, errors);
    public static PaymentRegistrationResult<T> Invalid(string message) => new(PaymentRegistrationOutcome.Invalid, default, [message]);
    public static PaymentRegistrationResult<T> Conflict(string message) => new(PaymentRegistrationOutcome.Conflict, default, [message]);
}

public interface IClientPaymentRegistrationService
{
    Task<PaymentRegistrationResult<PaymentPreviewDto>> GetPreviewAsync(string clientGuid, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string clientGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string clientGuid, CancellationToken cancellationToken = default);

    /// <summary>Excedentes de meses cerrados sin pagar, por ClientId.</summary>
    Task<Dictionary<int, List<PendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Implementación**

`ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs`:

```csharp
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class ClientPaymentRegistrationService(
    IClientService clientService,
    IClientPaymentService clientPaymentService,
    IClientPaymentItemService clientPaymentItemService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    IPlanOverageTierService planOverageTierService,
    IUnitOfWork unitOfWork) : IClientPaymentRegistrationService
{
    private const string ClientNotFound = "Cliente no encontrado.";
    private const string NoPlan = "El cliente no tiene un plan asignado.";
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public async Task<PaymentRegistrationResult<PaymentPreviewDto>> GetPreviewAsync(string clientGuid, CancellationToken cancellationToken = default)
    {
        var client = await clientService.Table.AsNoTracking()
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<PaymentPreviewDto>.NotFound(ClientNotFound);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentPreviewDto>.Invalid(NoPlan);

        var today = DateTimeExtensions.DrNow.Date;
        var pending = await GetPendingOveragesAsync([client.ClientId], cancellationToken);

        return PaymentRegistrationResult<PaymentPreviewDto>.Success(new PaymentPreviewDto
        {
            ClientGuidId = client.GuidId,
            ClientName = client.Name,
            ClientRnc = client.Rnc,
            PaymentSuspended = client.PaymentSuspendedAtUtc != null,
            PlanFee = client.Plan.MonthlyFee > 0 ? BuildPlanFeePreview(client, today) : null,
            PendingOverages = pending.GetValueOrDefault(client.ClientId) ?? []
        });
    }

    public async Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string clientGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var overageIds = request.OverageUsageIds.Distinct().ToList();

        var errors = PaymentRegistrationPolicy.ValidateRequest(
            request.PaymentDate, request.PaymentMethod, request.IncludePlanFee, overageIds.Count,
            request.Reference, request.Notes, today);
        if (errors.Count > 0) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(errors);

        var client = await clientService.Table
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<PaymentReceiptDto>.NotFound(ClientNotFound);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(NoPlan);
        if (request.IncludePlanFee && client.Plan.MonthlyFee <= 0)
            return PaymentRegistrationResult<PaymentReceiptDto>.Invalid("El plan del cliente no tiene mensualidad a pagar.");

        var pending = (await GetPendingOveragesAsync([client.ClientId], cancellationToken))
            .GetValueOrDefault(client.ClientId) ?? [];
        var selectedOverages = pending.Where(p => overageIds.Contains(p.ClientMonthlyUsageId)).ToList();
        if (selectedOverages.Count != overageIds.Count)
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(
                "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.");

        var paymentDate = request.PaymentDate!.Value.Date;
        var wasSuspended = client.PaymentSuspendedAtUtc != null;

        var payment = new ClientPayment
        {
            ClientId = client.ClientId,
            PaymentDate = paymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RegisteredByUserId = userId
        };

        if (request.IncludePlanFee)
        {
            var calculation = PaymentCalculator.Calculate(client.Plan.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);
            var previousNext = client.NextPaymentDate?.Date;
            var newNext = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(previousNext, paymentDate, calculation.MonthsCovered);

            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.PlanFee,
                Amount = calculation.Total,
                PlanId = client.PlanId,
                PlanName = client.Plan.Name,
                MonthsCovered = calculation.MonthsCovered,
                GrossAmount = calculation.GrossAmount,
                DiscountPercent = calculation.DiscountPercent,
                DiscountAmount = calculation.DiscountAmount,
                PreviousNextPaymentDate = previousNext,
                NewNextPaymentDate = newNext
            });

            client.LastPaymentDate = paymentDate;
            client.NextPaymentDate = newNext;
        }

        foreach (var overage in selectedOverages.OrderBy(o => o.Year).ThenBy(o => o.Month))
        {
            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.Overage,
                Amount = overage.Amount,
                PlanId = overage.PlanId,
                PlanName = overage.PlanName,
                ClientMonthlyUsageId = overage.ClientMonthlyUsageId,
                Year = overage.Year,
                Month = overage.Month,
                OverageDocuments = overage.OverageDocuments
            });
        }

        payment.TotalAmount = payment.Items.Sum(i => i.Amount);

        var reactivated = PaymentRegistrationPolicy.ShouldReactivate(wasSuspended, request.IncludePlanFee, client.NextPaymentDate, today);
        if (reactivated)
        {
            client.ClientInactive = false;
            client.PaymentSuspendedAtUtc = null;
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientPaymentService.InsertAsync(payment);
                await clientService.UpdateAsync(client);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // El índice único de excedente se disparó por un registro concurrente.
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(
                "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.");
        }

        var receipt = ToReceipt(payment, client);
        receipt.Reactivated = reactivated;
        receipt.StillSuspended = wasSuspended && !reactivated;

        return PaymentRegistrationResult<PaymentReceiptDto>.Success(receipt);
    }

    public async Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string clientGuid, CancellationToken cancellationToken = default)
    {
        var client = await clientService.Table.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<List<PaymentReceiptDto>>.NotFound(ClientNotFound);

        var payments = await clientPaymentService.Table.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.ClientId == client.ClientId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ClientPaymentId)
            .ToListAsync(cancellationToken);

        return PaymentRegistrationResult<List<PaymentReceiptDto>>.Success(payments.Select(p => ToReceipt(p, client)).ToList());
    }

    public async Task<Dictionary<int, List<PendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0) return [];

        var today = DateTimeExtensions.DrNow.Date;
        var ids = clientIds.ToList();

        var usages = await clientMonthlyUsageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.ClientId)
                        && (u.Year < today.Year || (u.Year == today.Year && u.Month < today.Month)))
            .ToListAsync(cancellationToken);

        if (usages.Count == 0) return [];

        var usageIds = usages.Select(u => u.ClientMonthlyUsageId).ToList();
        var paidUsageIds = (await clientPaymentItemService.Table.AsNoTracking()
                .Where(i => i.ItemType == (int)ClientPaymentItemType.Overage
                            && i.ClientMonthlyUsageId != null
                            && usageIds.Contains(i.ClientMonthlyUsageId.Value))
                .Select(i => i.ClientMonthlyUsageId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
        var tiers = await planOverageTierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        return usages
            .Where(u => !paidUsageIds.Contains(u.ClientMonthlyUsageId)
                        && PaymentRegistrationPolicy.IsOverageMonthPayable(u.Year, u.Month, today))
            .Select(u =>
            {
                var calculation = BillingCalculator.Calculate(
                    u.MonthlyFee,
                    u.MonthlyDocumentLimit,
                    tiers.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                    u.AcceptedDocuments);

                return new PendingOverageDto
                {
                    ClientMonthlyUsageId = u.ClientMonthlyUsageId,
                    Year = u.Year,
                    Month = u.Month,
                    PlanId = u.PlanId,
                    PlanName = u.PlanName,
                    AcceptedDocuments = u.AcceptedDocuments,
                    MonthlyDocumentLimit = u.MonthlyDocumentLimit,
                    OverageDocuments = calculation.OverageDocuments,
                    Amount = calculation.OverageAmount
                };
            })
            .Where(p => p.Amount > 0)
            .GroupBy(p => usages.First(u => u.ClientMonthlyUsageId == p.ClientMonthlyUsageId).ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(p => p.Year).ThenBy(p => p.Month).ToList());
    }

    private static PlanFeePreviewDto BuildPlanFeePreview(Client client, DateTime today)
    {
        var calculation = PaymentCalculator.Calculate(client.Plan!.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);

        return new PlanFeePreviewDto
        {
            PlanName = client.Plan.Name,
            PlanTypeId = client.Plan.PlanTypeId,
            MonthlyFee = calculation.MonthlyFee,
            MonthsCovered = calculation.MonthsCovered,
            GrossAmount = calculation.GrossAmount,
            DiscountPercent = calculation.DiscountPercent,
            DiscountAmount = calculation.DiscountAmount,
            Total = calculation.Total,
            CurrentNextPaymentDate = client.NextPaymentDate?.Date,
            NewNextPaymentDate = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(client.NextPaymentDate, today, calculation.MonthsCovered)
        };
    }

    private static PaymentReceiptDto ToReceipt(ClientPayment payment, Client client) => new()
    {
        ClientPaymentId = payment.ClientPaymentId,
        ReceiptNumber = PaymentRegistrationPolicy.FormatReceiptNumber(payment.ClientPaymentId),
        ClientGuidId = client.GuidId,
        ClientName = client.Name,
        PaymentDate = payment.PaymentDate,
        PaymentMethod = payment.PaymentMethod,
        Reference = payment.Reference,
        Notes = payment.Notes,
        TotalAmount = payment.TotalAmount,
        NextPaymentDate = payment.Items
            .FirstOrDefault(i => i.ItemType == (int)ClientPaymentItemType.PlanFee)?.NewNextPaymentDate,
        Items = payment.Items
            .OrderBy(i => i.ItemType)
            .ThenBy(i => i.Year)
            .ThenBy(i => i.Month)
            .Select(i => new PaymentReceiptItemDto
            {
                ItemType = i.ItemType,
                Description = DescribeItem(i),
                Amount = i.Amount,
                MonthsCovered = i.MonthsCovered,
                DiscountAmount = i.DiscountAmount,
                PreviousNextPaymentDate = i.PreviousNextPaymentDate,
                NewNextPaymentDate = i.NewNextPaymentDate,
                Year = i.Year,
                Month = i.Month,
                OverageDocuments = i.OverageDocuments
            })
            .ToList()
    };

    private static string DescribeItem(ClientPaymentItem item)
    {
        if (item.ItemType == (int)ClientPaymentItemType.PlanFee)
        {
            var months = item.MonthsCovered == 1 ? "1 mes" : $"{item.MonthsCovered} meses";
            return $"Mensualidad {item.PlanName} ({months})";
        }

        var period = item.Year is int year && item.Month is int month
            ? Es.TextInfo.ToTitleCase(new DateTime(year, month, 1).ToString("MMMM yyyy", Es))
            : string.Empty;
        return $"Excedente {period} ({item.OverageDocuments} comprobantes)";
    }
}
```

- [ ] **Step 4: Registrar en DI**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, después de `services.AddTransient<Billing.IPaymentReminderService, Billing.PaymentReminderService>();`:

```csharp
        services.AddTransient<Billing.IClientPaymentRegistrationService, Billing.ClientPaymentRegistrationService>();
```

- [ ] **Step 5: Build + tests**

Run: `dotnet build C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.slnx -v q` y luego `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q`
Expected: build OK y tests verdes.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs ZynstormECFPlatform.Dtos/ClientPaymentDtos.cs ZynstormECFPlatform.Services/Billing/IClientPaymentRegistrationService.cs ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs
git commit -m "feat(payments): servicio de registro de pagos, historial y excedentes pendientes"
```

---

### Task 4: Endpoints en `ClientController`

**Files:**
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` (constructor ~línea 34; `GetPaymentClients` ~líneas 666-720; nuevos endpoints después de `SendPaymentSummary`)

**Interfaces:**
- Consumes: `IClientPaymentRegistrationService`, `PaymentRegistrationResult<T>`, `PaymentRegistrationOutcome` (Task 3); DTOs (Task 3).
- Produces (JSON camelCase):
  - `GET v1/Client/guid/{guid}/payments/preview` → `PaymentPreviewDto`
  - `POST v1/Client/guid/{guid}/payments` (body `RegisterPaymentRequestDto`) → `PaymentReceiptDto`; 400 `{ message, errors }`; 404 `{ message }`; 409 `{ message, errors }`
  - `GET v1/Client/guid/{guid}/payments` → `PaymentReceiptDto[]`
  - `GET v1/Client/payments` filas con `pendingOverageAmount` y `pendingOverageMonths`.

- [ ] **Step 1: Inyectar el servicio**

En el constructor primario, reemplazar:

```csharp
        IPaymentReminderService paymentReminderService,
```

por:

```csharp
        IPaymentReminderService paymentReminderService,
        IClientPaymentRegistrationService paymentRegistrationService,
```

- [ ] **Step 2: Excedente pendiente en la lista de pagos**

En `GetPaymentClients`, reemplazar:

```csharp
                var countByClient = await ActiveUserCountsAsync(clients.Select(c => c.ClientId).ToList(), cancellationToken);
```

por:

```csharp
                var countByClient = await ActiveUserCountsAsync(clients.Select(c => c.ClientId).ToList(), cancellationToken);
                var pendingByClient = await paymentRegistrationService.GetPendingOveragesAsync(
                    clients.Select(c => c.ClientId).ToList(), cancellationToken);
```

y reemplazar:

```csharp
                        HasEmail = !string.IsNullOrWhiteSpace(c.Email)
                    };
```

por:

```csharp
                        HasEmail = !string.IsNullOrWhiteSpace(c.Email),
                        PendingOverageAmount = pendingByClient.TryGetValue(c.ClientId, out var pending) ? pending.Sum(p => p.Amount) : 0m,
                        PendingOverageMonths = pending?.Count ?? 0
                    };
```

- [ ] **Step 3: Endpoints**

Después del método `SendPaymentSummary` (antes del comentario `/// <summary>` de `GetStatusByKey`), agregar:

```csharp
        [HttpGet]
        [Route("guid/{guid}/payments/preview", Order = 1)]
        public async Task<IActionResult> GetPaymentPreview(string guid, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();

            try
            {
                return ToPaymentActionResult(await paymentRegistrationService.GetPreviewAsync(guid, cancellationToken));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error consultando el pago a registrar del cliente {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el pago a registrar." });
            }
        }

        [HttpPost]
        [Route("guid/{guid}/payments", Order = 1)]
        public async Task<IActionResult> RegisterPayment(string guid, [FromBody] RegisterPaymentRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();

            try
            {
                var result = await paymentRegistrationService.RegisterAsync(guid, dto, CurrentUserId, cancellationToken);
                return ToPaymentActionResult(result);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error registrando el pago del cliente {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo registrar el pago." });
            }
        }

        [HttpGet]
        [Route("guid/{guid}/payments", Order = 1)]
        public async Task<IActionResult> GetPaymentHistory(string guid, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();

            try
            {
                return ToPaymentActionResult(await paymentRegistrationService.GetHistoryAsync(guid, cancellationToken));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error consultando el historial de pagos del cliente {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el historial de pagos." });
            }
        }

        private IActionResult ToPaymentActionResult<T>(PaymentRegistrationResult<T> result) => result.Outcome switch
        {
            PaymentRegistrationOutcome.Ok => Ok(result.Value),
            PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
            PaymentRegistrationOutcome.Conflict => Conflict(new { message = string.Join(" ", result.Errors), errors = result.Errors }),
            _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
        };
```

- [ ] **Step 4: Build + tests**

Run: `dotnet build C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.slnx -v q` y `dotnet test C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform\ZynstormECFPlatform.Tests -v q`
Expected: build OK, tests verdes.

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs
git commit -m "feat(payments): endpoints de preview, registro e historial de pagos"
```

---

### Task 5: Frontend — tipos, servicio y utilidades

**Files (repo frontend):**
- Create: `types/paymentRegistration.type.ts`
- Modify: `types/payment.type.ts` (`ClientPayment` con excedente pendiente)
- Modify: `services/payment.service.ts`
- Modify: `lib/payment.ts` (métodos de pago, período, hoy RD)

**Interfaces:**
- Consumes: API de la Task 4.
- Produces:
  - `enum PaymentMethod { Cash = 1, Transfer = 2, Card = 3, Check = 4, Other = 5 }`, `enum PaymentItemType { PlanFee = 1, Overage = 2 }`
  - `PendingOverage`, `PlanFeePreview`, `PaymentPreview`, `RegisterPaymentRequest`, `PaymentReceiptItem`, `PaymentReceipt`
  - `ClientPayment.pendingOverageAmount: number`, `ClientPayment.pendingOverageMonths: number`
  - `getPaymentPreview(guid): Promise<PaymentPreview>`, `registerPayment(guid, request): Promise<PaymentReceipt>`, `getPaymentHistory(guid): Promise<PaymentReceipt[]>`
  - `PAYMENT_METHODS: { value: PaymentMethod; label: string }[]`, `paymentMethodLabel(method: number): string`, `formatPeriod(year: number, month: number): string`, `todayDrYmd(): string`

- [ ] **Step 1: Tipos**

`types/paymentRegistration.type.ts`:

```ts
export enum PaymentMethod {
  Cash = 1,
  Transfer = 2,
  Card = 3,
  Check = 4,
  Other = 5,
}

export enum PaymentItemType {
  PlanFee = 1,
  Overage = 2,
}

export interface PendingOverage {
  clientMonthlyUsageId: number
  year: number
  month: number
  planId: number | null
  planName: string
  acceptedDocuments: number
  monthlyDocumentLimit: number
  overageDocuments: number
  amount: number
}

export interface PlanFeePreview {
  planName: string
  planTypeId: number
  monthlyFee: number
  monthsCovered: number
  grossAmount: number
  discountPercent: number
  discountAmount: number
  total: number
  currentNextPaymentDate: string | null
  newNextPaymentDate: string | null
}

export interface PaymentPreview {
  clientGuidId: string
  clientName: string
  clientRnc: string
  paymentSuspended: boolean
  planFee: PlanFeePreview | null
  pendingOverages: PendingOverage[]
}

export interface RegisterPaymentRequest {
  paymentDate: string
  paymentMethod: PaymentMethod
  reference: string | null
  notes: string | null
  includePlanFee: boolean
  overageUsageIds: number[]
}

export interface PaymentReceiptItem {
  itemType: PaymentItemType
  description: string
  amount: number
  monthsCovered: number | null
  discountAmount: number | null
  previousNextPaymentDate: string | null
  newNextPaymentDate: string | null
  year: number | null
  month: number | null
  overageDocuments: number | null
}

export interface PaymentReceipt {
  clientPaymentId: number
  receiptNumber: string
  clientGuidId: string
  clientName: string
  paymentDate: string | null
  paymentMethod: PaymentMethod
  reference: string | null
  notes: string | null
  totalAmount: number
  nextPaymentDate: string | null
  reactivated: boolean
  stillSuspended: boolean
  items: PaymentReceiptItem[]
}
```

En `types/payment.type.ts`, dentro de `ClientPayment`, después de `hasEmail: boolean`:

```ts
  pendingOverageAmount: number
  pendingOverageMonths: number
```

- [ ] **Step 2: Servicio**

Reemplazar el contenido de `services/payment.service.ts` por:

```ts
import { get, post } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { ClientPayment, MessageResponse } from "@/types/payment.type"
import { PaymentPreview, PaymentReceipt, RegisterPaymentRequest } from "@/types/paymentRegistration.type"

const CLIENT_URL = `${API_BASE_URL}/v1/Client`

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
```

- [ ] **Step 3: Utilidades**

Al final de `lib/payment.ts`, agregar:

```ts
export const PAYMENT_METHODS: { value: number; label: string }[] = [
  { value: 1, label: "Efectivo" },
  { value: 2, label: "Transferencia" },
  { value: 3, label: "Tarjeta" },
  { value: 4, label: "Cheque" },
  { value: 5, label: "Otro" },
]

export const paymentMethodLabel = (method: number): string =>
  PAYMENT_METHODS.find((m) => m.value === method)?.label ?? "—"

const MONTH_NAMES = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
]

export const formatPeriod = (year: number, month: number): string => `${MONTH_NAMES[month - 1]} ${year}`

/** Fecha de hoy en hora de República Dominicana, formato yyyy-MM-dd. */
export const todayDrYmd = (): string =>
  new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Santo_Domingo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date())
```

- [ ] **Step 4: Verificar tipos**

Run (desde el repo frontend): `pnpm exec tsc --noEmit -p .`
Expected: solo el error preexistente de `app/configuraciones/cuenta/page.tsx`.

- [ ] **Step 5: Commit (repo frontend)**

```bash
git add types/paymentRegistration.type.ts types/payment.type.ts services/payment.service.ts lib/payment.ts
git commit -m "feat(pagos): tipos, servicio y utilidades para registrar pagos"
```

---

### Task 6: Frontend — diálogos de pago

**Files (repo frontend):**
- Create: `components/payments/register-payment-dialog.tsx`
- Create: `components/payments/payment-receipt-dialog.tsx`
- Create: `components/payments/payment-history-dialog.tsx`

**Interfaces:**
- Consumes: Task 5.
- Produces:
  - `RegisterPaymentDialog({ client, onClose, onRegistered }: { client: ClientPayment | null; onClose: () => void; onRegistered: (receipt: PaymentReceipt) => void })`
  - `PaymentReceiptDialog({ receipt, onClose }: { receipt: PaymentReceipt | null; onClose: () => void })`
  - `PaymentHistoryDialog({ client, onClose }: { client: ClientPayment | null; onClose: () => void })`

- [ ] **Step 1: Diálogo Registrar pago**

`components/payments/register-payment-dialog.tsx`:

```tsx
"use client"

import { FormEvent, useEffect, useMemo, useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { Checkbox } from "@/components/ui/checkbox"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Field, FieldGroup, FieldLabel } from "@/components/ui/field"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { getPaymentPreview, registerPayment } from "@/services/payment.service"
import { ClientPayment } from "@/types/payment.type"
import { PaymentMethod, PaymentPreview, PaymentReceipt } from "@/types/paymentRegistration.type"
import { PAYMENT_METHODS, formatCalendarDate, formatMonths, formatPeriod, todayDrYmd } from "@/lib/payment"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

interface RegisterPaymentDialogProps {
  client: ClientPayment | null
  onClose: () => void
  onRegistered: (receipt: PaymentReceipt) => void
}

export function RegisterPaymentDialog({ client, onClose, onRegistered }: RegisterPaymentDialogProps) {
  const [preview, setPreview] = useState<PaymentPreview | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const [includePlanFee, setIncludePlanFee] = useState(false)
  const [overageIds, setOverageIds] = useState<number[]>([])
  const [paymentDate, setPaymentDate] = useState(todayDrYmd())
  const [paymentMethod, setPaymentMethod] = useState<PaymentMethod>(PaymentMethod.Transfer)
  const [reference, setReference] = useState("")
  const [notes, setNotes] = useState("")

  useEffect(() => {
    if (!client) return
    let cancelled = false

    setPreview(null)
    setError(null)
    setOverageIds([])
    setPaymentDate(todayDrYmd())
    setPaymentMethod(PaymentMethod.Transfer)
    setReference("")
    setNotes("")
    setIsLoading(true)

    getPaymentPreview(client.clientGuidId)
      .then((data) => {
        if (cancelled) return
        setPreview(data)
        setIncludePlanFee(Boolean(data.planFee))
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "No se pudo cargar el pago a registrar")
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [client])

  const total = useMemo(() => {
    if (!preview) return 0
    const planFee = includePlanFee && preview.planFee ? preview.planFee.total : 0
    const overage = preview.pendingOverages
      .filter((o) => overageIds.includes(o.clientMonthlyUsageId))
      .reduce((sum, o) => sum + o.amount, 0)
    return planFee + overage
  }, [preview, includePlanFee, overageIds])

  const hasSelection = includePlanFee || overageIds.length > 0

  const toggleOverage = (id: number, checked: boolean) => {
    setOverageIds((current) => (checked ? [...current, id] : current.filter((x) => x !== id)))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!client || !hasSelection) return

    try {
      setIsSaving(true)
      setError(null)
      const receipt = await registerPayment(client.clientGuidId, {
        paymentDate,
        paymentMethod,
        reference: reference.trim() || null,
        notes: notes.trim() || null,
        includePlanFee,
        overageUsageIds: overageIds,
      })
      onRegistered(receipt)
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo registrar el pago")
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={Boolean(client)} onOpenChange={(open) => !open && !isSaving && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Registrar pago</DialogTitle>
            <DialogDescription>
              {client?.clientName} · {client?.clientRnc}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-5 py-4">
            {error && (
              <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                {error}
              </div>
            )}

            {isLoading && <p className="text-sm text-muted-foreground">Cargando…</p>}

            {preview && (
              <>
                {preview.paymentSuspended && (
                  <div className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
                    El cliente está suspendido por falta de pago. Se reactivará si el pago de la mensualidad lo deja al día.
                  </div>
                )}

                <section className="space-y-2">
                  <h3 className="text-sm font-semibold">Mensualidad</h3>
                  {preview.planFee ? (
                    <label className="flex cursor-pointer items-start gap-3 rounded-lg border p-3">
                      <Checkbox
                        checked={includePlanFee}
                        onCheckedChange={(checked) => setIncludePlanFee(checked === true)}
                        className="mt-0.5"
                      />
                      <div className="flex-1 text-sm">
                        <div className="flex justify-between gap-2">
                          <span className="font-medium">
                            {preview.planFee.planName} · {formatMonths(preview.planFee.monthsCovered)}
                          </span>
                          <span className="font-semibold">{currency.format(preview.planFee.total)}</span>
                        </div>
                        {preview.planFee.discountAmount > 0 && (
                          <p className="text-xs text-muted-foreground">
                            {currency.format(preview.planFee.grossAmount)} − {preview.planFee.discountPercent}% (
                            {currency.format(preview.planFee.discountAmount)})
                          </p>
                        )}
                        <p className="text-xs text-muted-foreground">
                          Próximo pago: {formatCalendarDate(preview.planFee.currentNextPaymentDate)} →{" "}
                          <span className="font-medium text-foreground">
                            {formatCalendarDate(preview.planFee.newNextPaymentDate)}
                          </span>
                        </p>
                      </div>
                    </label>
                  ) : (
                    <p className="text-sm text-muted-foreground">El plan del cliente no tiene mensualidad a pagar.</p>
                  )}
                </section>

                {preview.pendingOverages.length > 0 && (
                  <section className="space-y-2">
                    <h3 className="text-sm font-semibold">Excedentes pendientes</h3>
                    {preview.pendingOverages.map((overage) => (
                      <label
                        key={overage.clientMonthlyUsageId}
                        className="flex cursor-pointer items-center gap-3 rounded-lg border p-3 text-sm"
                      >
                        <Checkbox
                          checked={overageIds.includes(overage.clientMonthlyUsageId)}
                          onCheckedChange={(checked) => toggleOverage(overage.clientMonthlyUsageId, checked === true)}
                        />
                        <div className="flex-1">
                          <span className="font-medium">{formatPeriod(overage.year, overage.month)}</span>
                          <span className="block text-xs text-muted-foreground">
                            {overage.overageDocuments.toLocaleString("es-DO")} comprobantes sobre el límite de{" "}
                            {overage.monthlyDocumentLimit.toLocaleString("es-DO")}
                          </span>
                        </div>
                        <span className="font-semibold">{currency.format(overage.amount)}</span>
                      </label>
                    ))}
                  </section>
                )}

                <FieldGroup>
                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field>
                      <FieldLabel htmlFor="payment-date">Fecha del pago</FieldLabel>
                      <Input
                        id="payment-date"
                        type="date"
                        required
                        max={todayDrYmd()}
                        value={paymentDate}
                        onChange={(e) => setPaymentDate(e.target.value)}
                      />
                    </Field>
                    <Field>
                      <FieldLabel htmlFor="payment-method">Método</FieldLabel>
                      <Select value={String(paymentMethod)} onValueChange={(v) => setPaymentMethod(Number(v) as PaymentMethod)}>
                        <SelectTrigger id="payment-method">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {PAYMENT_METHODS.map((method) => (
                            <SelectItem key={method.value} value={String(method.value)}>
                              {method.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </Field>
                  </div>
                  <Field>
                    <FieldLabel htmlFor="payment-reference">Referencia</FieldLabel>
                    <Input
                      id="payment-reference"
                      maxLength={100}
                      placeholder="Número de transferencia, cheque…"
                      value={reference}
                      onChange={(e) => setReference(e.target.value)}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="payment-notes">Nota</FieldLabel>
                    <Textarea
                      id="payment-notes"
                      maxLength={500}
                      rows={2}
                      className="resize-none"
                      value={notes}
                      onChange={(e) => setNotes(e.target.value)}
                    />
                  </Field>
                </FieldGroup>

                <div className="flex items-center justify-between rounded-lg bg-secondary/50 px-4 py-3">
                  <span className="text-sm text-muted-foreground">Total a registrar</span>
                  <span className="text-xl font-bold">{currency.format(total)}</span>
                </div>
              </>
            )}
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={onClose} disabled={isSaving}>
              Cancelar
            </Button>
            <Button type="submit" disabled={isSaving || isLoading || !preview || !hasSelection}>
              {isSaving ? "Registrando..." : "Registrar pago"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
```

- [ ] **Step 2: Diálogo Pago recibido**

`components/payments/payment-receipt-dialog.tsx`:

```tsx
"use client"

import { CheckCircle2 } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"
import { PaymentReceipt } from "@/types/paymentRegistration.type"
import { formatCalendarDate, paymentMethodLabel } from "@/lib/payment"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

interface PaymentReceiptDialogProps {
  receipt: PaymentReceipt | null
  onClose: () => void
}

export function PaymentReceiptDialog({ receipt, onClose }: PaymentReceiptDialogProps) {
  return (
    <Dialog open={Boolean(receipt)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader className="items-center text-center">
          <CheckCircle2 className="h-12 w-12 text-green-600" />
          <DialogTitle>Pago recibido</DialogTitle>
          <DialogDescription>
            {receipt?.receiptNumber} · {receipt?.clientName}
          </DialogDescription>
        </DialogHeader>

        {receipt && (
          <div className="space-y-4 text-sm">
            <ul className="divide-y rounded-lg border">
              {receipt.items.map((item, index) => (
                <li key={index} className="flex justify-between gap-3 px-3 py-2">
                  <span>{item.description}</span>
                  <span className="font-medium">{currency.format(item.amount)}</span>
                </li>
              ))}
            </ul>

            <div className="flex items-center justify-between">
              <span className="text-muted-foreground">Total recibido</span>
              <span className="text-xl font-bold">{currency.format(receipt.totalAmount)}</span>
            </div>

            <dl className="grid grid-cols-2 gap-y-1 text-xs">
              <dt className="text-muted-foreground">Fecha del pago</dt>
              <dd className="text-right">{formatCalendarDate(receipt.paymentDate)}</dd>
              <dt className="text-muted-foreground">Método</dt>
              <dd className="text-right">{paymentMethodLabel(receipt.paymentMethod)}</dd>
              {receipt.reference && (
                <>
                  <dt className="text-muted-foreground">Referencia</dt>
                  <dd className="text-right">{receipt.reference}</dd>
                </>
              )}
              {receipt.nextPaymentDate && (
                <>
                  <dt className="text-muted-foreground">Próximo pago</dt>
                  <dd className="text-right font-medium">{formatCalendarDate(receipt.nextPaymentDate)}</dd>
                </>
              )}
            </dl>

            {receipt.reactivated && (
              <div className="rounded-lg border border-green-200 bg-green-50 px-3 py-2 text-green-800">
                El cliente fue reactivado y ya puede emitir comprobantes.
              </div>
            )}
            {receipt.stillSuspended && (
              <div className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-amber-800">
                El cliente sigue suspendido: el próximo pago todavía está vencido.
              </div>
            )}
          </div>
        )}

        <DialogFooter>
          <Button onClick={onClose} className="w-full">
            Aceptar
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
```

- [ ] **Step 3: Diálogo Historial**

`components/payments/payment-history-dialog.tsx`:

```tsx
"use client"

import { useEffect, useState } from "react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import { getPaymentHistory } from "@/services/payment.service"
import { ClientPayment } from "@/types/payment.type"
import { PaymentReceipt } from "@/types/paymentRegistration.type"
import { formatCalendarDate, paymentMethodLabel } from "@/lib/payment"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

interface PaymentHistoryDialogProps {
  client: ClientPayment | null
  onClose: () => void
}

export function PaymentHistoryDialog({ client, onClose }: PaymentHistoryDialogProps) {
  const [receipts, setReceipts] = useState<PaymentReceipt[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!client) return
    let cancelled = false

    setReceipts([])
    setError(null)
    setIsLoading(true)

    getPaymentHistory(client.clientGuidId)
      .then((data) => {
        if (!cancelled) setReceipts(data)
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : "No se pudo cargar el historial")
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [client])

  return (
    <Dialog open={Boolean(client)} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>Historial de pagos</DialogTitle>
          <DialogDescription>
            {client?.clientName} · {client?.clientRnc}
          </DialogDescription>
        </DialogHeader>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
            {error}
          </div>
        )}

        {isLoading ? (
          <p className="text-sm text-muted-foreground">Cargando…</p>
        ) : receipts.length === 0 && !error ? (
          <p className="py-6 text-center text-sm text-muted-foreground">No hay pagos registrados.</p>
        ) : (
          <div className="space-y-3">
            {receipts.map((receipt) => (
              <div key={receipt.clientPaymentId} className="rounded-lg border p-3 text-sm">
                <div className="flex flex-wrap items-baseline justify-between gap-2">
                  <div>
                    <span className="font-semibold">{receipt.receiptNumber}</span>
                    <span className="ml-2 text-xs text-muted-foreground">
                      {formatCalendarDate(receipt.paymentDate)} · {paymentMethodLabel(receipt.paymentMethod)}
                      {receipt.reference && ` · ${receipt.reference}`}
                    </span>
                  </div>
                  <span className="font-bold">{currency.format(receipt.totalAmount)}</span>
                </div>
                <ul className="mt-2 space-y-1 text-xs">
                  {receipt.items.map((item, index) => (
                    <li key={index} className="flex justify-between gap-3">
                      <span className="text-muted-foreground">
                        {item.description}
                        {item.newNextPaymentDate && ` · próximo pago ${formatCalendarDate(item.newNextPaymentDate)}`}
                      </span>
                      <span>{currency.format(item.amount)}</span>
                    </li>
                  ))}
                </ul>
                {receipt.notes && <p className="mt-2 text-xs italic text-muted-foreground">{receipt.notes}</p>}
              </div>
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}
```

- [ ] **Step 4: Verificar tipos y lint**

Run (repo frontend): `pnpm exec tsc --noEmit -p .` y `pnpm exec eslint components/payments/register-payment-dialog.tsx components/payments/payment-receipt-dialog.tsx components/payments/payment-history-dialog.tsx`
Expected: tsc solo con el error preexistente de `cuenta/page.tsx`; eslint sin errores (se aceptan advertencias `set-state-in-effect`, mismo patrón que el resto de la app).

- [ ] **Step 5: Commit (repo frontend)**

```bash
git add components/payments
git commit -m "feat(pagos): diálogos de registrar pago, pago recibido e historial"
```

---

### Task 7: Frontend — integración en `/pagos`

**Files (repo frontend):**
- Modify: `app/pagos/page.tsx`

**Interfaces:**
- Consumes: `RegisterPaymentDialog`, `PaymentReceiptDialog`, `PaymentHistoryDialog` (Task 6); `ClientPayment.pendingOverageAmount`, `pendingOverageMonths` (Task 5).

- [ ] **Step 1: Imports**

Reemplazar:

```tsx
import { Mail, RefreshCw, Send } from "lucide-react"
```

por:

```tsx
import { Banknote, History, Mail, RefreshCw, Send } from "lucide-react"
import { RegisterPaymentDialog } from "@/components/payments/register-payment-dialog"
import { PaymentReceiptDialog } from "@/components/payments/payment-receipt-dialog"
import { PaymentHistoryDialog } from "@/components/payments/payment-history-dialog"
import { PaymentReceipt } from "@/types/paymentRegistration.type"
```

- [ ] **Step 2: Estado**

Reemplazar:

```tsx
  const [isSendingSummary, setIsSendingSummary] = useState(false)
```

por:

```tsx
  const [isSendingSummary, setIsSendingSummary] = useState(false)
  const [clientToPay, setClientToPay] = useState<ClientPayment | null>(null)
  const [receipt, setReceipt] = useState<PaymentReceipt | null>(null)
  const [historyClient, setHistoryClient] = useState<ClientPayment | null>(null)

  const handlePaymentRegistered = async (registered: PaymentReceipt) => {
    setClientToPay(null)
    setReceipt(registered)
    await load()
  }
```

- [ ] **Step 3: Excedente pendiente en la columna Total del ciclo**

Reemplazar:

```tsx
                          <span className="block text-xs text-muted-foreground">
                            {currency.format(row.monthlyFee)}/mes
                          </span>
```

por:

```tsx
                          <span className="block text-xs text-muted-foreground">
                            {currency.format(row.monthlyFee)}/mes
                          </span>
                          {row.pendingOverageAmount > 0 && (
                            <span
                              className="block text-xs font-medium text-amber-700"
                              title={`${row.pendingOverageMonths} mes${row.pendingOverageMonths === 1 ? "" : "es"} con excedente sin pagar`}
                            >
                              Excedente pendiente: {currency.format(row.pendingOverageAmount)}
                            </span>
                          )}
```

- [ ] **Step 4: Botones de acción**

Reemplazar:

```tsx
                        <TableCell className="text-right">
                          <span title={blockReason ?? "Enviar recordatorio de pago al cliente"}>
```

por:

```tsx
                        <TableCell className="text-right">
                          <div className="flex justify-end gap-2">
                          <Button size="sm" className="gap-1" onClick={() => setClientToPay(row)}>
                            <Banknote className="h-3.5 w-3.5" />
                            Registrar pago
                          </Button>
                          <Button
                            variant="outline"
                            size="icon"
                            className="h-8 w-8"
                            title="Historial de pagos"
                            onClick={() => setHistoryClient(row)}
                          >
                            <History className="h-3.5 w-3.5" />
                          </Button>
                          <span title={blockReason ?? "Enviar recordatorio de pago al cliente"}>
```

y reemplazar:

```tsx
                              Recordar
                            </Button>
                          </span>
                        </TableCell>
```

por:

```tsx
                              Recordar
                            </Button>
                          </span>
                          </div>
                        </TableCell>
```

- [ ] **Step 5: Montar los diálogos**

Reemplazar:

```tsx
        </AlertDialog>
      </div>
    </MainLayout>
```

por:

```tsx
        </AlertDialog>

        <RegisterPaymentDialog
          client={clientToPay}
          onClose={() => setClientToPay(null)}
          onRegistered={handlePaymentRegistered}
        />
        <PaymentReceiptDialog receipt={receipt} onClose={() => setReceipt(null)} />
        <PaymentHistoryDialog client={historyClient} onClose={() => setHistoryClient(null)} />
      </div>
    </MainLayout>
```

- [ ] **Step 6: Tipos y lint**

Run (repo frontend): `pnpm exec tsc --noEmit -p .` y `pnpm exec eslint app/pagos/page.tsx`
Expected: tsc solo con el error preexistente; eslint sin errores.

- [ ] **Step 7: Verificación manual**

Con la API local levantada en `Development` (Hangfire en `zynstorm_ecf_hangfire_dev_db`) y `pnpm dev`, como SA en `/pagos`:
1. *Registrar pago* en un cliente de renta: el modal muestra la mensualidad con próximo pago actual → nuevo; registrar con método Transferencia y referencia. Aparece *Pago recibido* con `REC-000001`, total y nuevo próximo pago; la fila se actualiza (último pago = fecha del pago, próximo pago avanzado desde la fecha que vencía).
2. *Historial*: muestra el recibo con su línea.
3. Cliente de comprobantes con excedente en un mes cerrado: la fila muestra "Excedente pendiente"; registrar solo el excedente (desmarcar mensualidad); el próximo pago no cambia y el excedente desaparece de pendientes. Intentar registrarlo de nuevo por API devuelve 409.
4. Cliente suspendido por pago (dev2): registrar la mensualidad que lo deja al día → *Pago recibido* indica reactivado y el badge *Suspendido* desaparece.

- [ ] **Step 8: Commit (repo frontend)**

```bash
git add app/pagos/page.tsx
git commit -m "feat(pagos): registrar pago, pago recibido e historial desde la página de pagos"
```

---

## Cobertura del spec

| Requisito | Task |
|---|---|
| Avance desde la fecha vencida; sin fecha previa desde el pago; `LastPaymentDate` | 1, 3 |
| Monto fijo con meses y descuento del cliente | 3, 6 |
| Excedente solo de meses cerrados, monto fijo, una vez por mes | 1, 2, 3 |
| Excedente no afecta estado/avisos/suspensión | 3 (no toca `PaymentReminderPolicy`) |
| Datos del pago: fecha no futura, método 1–5, referencia ≤100, nota ≤500 | 1, 2, 6 |
| Reactivación solo con mensualidad que deja al día; aviso si sigue suspendido | 1, 3, 6 |
| Recibo `ClientPayment` + líneas `ClientPaymentItem`, índice único parcial | 2 |
| Transacción única | 3 |
| Endpoints preview / registro / historial; 400/404/409 | 4 |
| Excedente pendiente en `GET v1/Client/payments` y en la tabla | 3, 4, 7 |
| Modal Registrar pago, modal Pago recibido con número de recibo, Historial | 6, 7 |
