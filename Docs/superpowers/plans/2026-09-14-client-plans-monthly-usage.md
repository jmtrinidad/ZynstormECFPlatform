# Planes de cliente, consumo mensual y cliente inactivo — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Asignar planes (mensualidad + límite + tramos de excedente) a los clientes, contar mensualmente los e-CF aceptados para facturar al cierre, bloquear la emisión de clientes inactivos y devolver siempre `EcfDocumentId` en `POST v1/Ecf/emit`.

**Architecture:** El cálculo del cobro es una función pura (`BillingCalculator`) en `ZynstormECFPlatform.Services/Billing`. El conteo lo hace `ClientUsageService.RegisterAcceptedAsync`, con una sola sentencia SQL atómica e idempotente (CTE `UPDATE "EcfDocument" ... RETURNING` + `INSERT ... ON CONFLICT` en `ClientMonthlyUsage`), invocada en los tres puntos donde un e-CF queda aceptado. Los planes se administran con `PlanController` y el reporte mensual sale de `ClientController`. El frontend Next.js agrega la página de planes, la de consumo y los campos nuevos del cliente.

**Tech Stack:** .NET 10, EF Core + Npgsql (PostgreSQL), Dapper (`IRepository.ExecuteAsync`), AutoMapper, xUnit. Frontend: Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-09-14-client-plans-monthly-usage-design.md`

## Global Constraints

- Repo backend: `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`. Frontend: `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.
- Los comandos `dotnet` se ejecutan desde la raíz del repo backend.
- Build: `dotnet build ZynstormECFPlatform.slnx -v q`. Tests: `dotnet test ZynstormECFPlatform.Tests -v q`. Ambos deben quedar verdes al final de cada task backend.
- Migraciones: `dotnet ef migrations add <Nombre> --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`.
- `Plan.MonthlyDocumentLimit = -1` significa **ilimitado**; cualquier otro valor debe ser `> 0`.
- Cliente sin plan (`PlanId = null`) o con plan inactivo/eliminado: **no** se registra consumo y no tiene límite.
- Solo cuentan los e-CF con estado DGII `Aceptado` o `Aceptado Condicional(mente)`.
- El mes de consumo es el mes de aceptación en hora RD: `DateTimeExtensions.DrNow` (namespace `ZynstormECFPlatform.Common`).
- Mensaje para cliente inactivo (texto exacto): `"El cliente se encuentra desactivado. Por favor, comuníquese con soporte."`. HTTP `403`, `clientInactive = true`, sin crear documento ni enviar a DGII.
- Nombres de tabla en PostgreSQL = nombre de la clase (singular, entre comillas dobles): `"Plan"`, `"PlanOverageTier"`, `"ClientMonthlyUsage"`, `"EcfDocument"`, `"Client"`.
- Planes y reporte de consumo: solo rol `SA` (backend `[Authorize(Roles = "SA")]`; frontend `user?.userType === 1`).
- Mensajes y UI en español; código e identificadores en inglés, igual que el código existente.

---

### Task 1: `BillingCalculator` (cálculo escalonado + validación de tramos)

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/BillingCalculator.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/BillingCalculatorTests.cs`

**Interfaces:**
- Produces (namespace `ZynstormECFPlatform.Services.Billing`):
  - `public sealed record OverageTier(int FromUnit, int? ToUnit, decimal UnitPrice);`
  - `public sealed record OverageTierCharge(int FromUnit, int? ToUnit, decimal UnitPrice, int Units, decimal Amount);`
  - `public sealed record BillingCalculationResult(decimal MonthlyFee, int MonthlyDocumentLimit, int AcceptedDocuments, int OverageDocuments, IReadOnlyList<OverageTierCharge> Tiers, decimal OverageAmount, decimal Total);`
  - `public static class BillingCalculator` con `const int UnlimitedDocuments = -1`, `BillingCalculationResult Calculate(decimal monthlyFee, int monthlyDocumentLimit, IEnumerable<OverageTier> tiers, int acceptedDocuments)`, `List<string> ValidatePlan(int monthlyDocumentLimit, decimal monthlyFee, IEnumerable<OverageTier> tiers)`.

- [ ] **Step 1: Escribir los tests que fallan**

Crear `ZynstormECFPlatform.Tests/Billing/BillingCalculatorTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingCalculatorTests
{
    private static readonly OverageTier[] StandardTiers =
    [
        new(1, 100, 6.00m),
        new(101, null, 9.00m)
    ];

    [Fact]
    public void Calculate_WithinLimit_ChargesOnlyMonthlyFee()
    {
        var result = BillingCalculator.Calculate(3000m, 500, StandardTiers, 480);

        Assert.Equal(0, result.OverageDocuments);
        Assert.Equal(0m, result.OverageAmount);
        Assert.Equal(3000m, result.Total);
        Assert.All(result.Tiers, t => Assert.Equal(0, t.Units));
    }

    [Fact]
    public void Calculate_OverageInsideFirstTier_UsesFirstTierPrice()
    {
        var result = BillingCalculator.Calculate(3000m, 500, StandardTiers, 600);

        Assert.Equal(100, result.OverageDocuments);
        Assert.Equal(600m, result.OverageAmount);
        Assert.Equal(3600m, result.Total);
        Assert.Equal(100, result.Tiers[0].Units);
        Assert.Equal(0, result.Tiers[1].Units);
    }

    [Fact]
    public void Calculate_OverageCrossingTiers_Is1050ForExcessOf150()
    {
        var result = BillingCalculator.Calculate(3000m, 500, StandardTiers, 650);

        Assert.Equal(150, result.OverageDocuments);
        Assert.Equal(100, result.Tiers[0].Units);
        Assert.Equal(600m, result.Tiers[0].Amount);
        Assert.Equal(50, result.Tiers[1].Units);
        Assert.Equal(450m, result.Tiers[1].Amount);
        Assert.Equal(1050m, result.OverageAmount);
        Assert.Equal(4050m, result.Total);
    }

    [Fact]
    public void Calculate_UnlimitedPlan_NeverChargesOverage()
    {
        var result = BillingCalculator.Calculate(6300m, BillingCalculator.UnlimitedDocuments, StandardTiers, 100000);

        Assert.Equal(0, result.OverageDocuments);
        Assert.Equal(6300m, result.Total);
    }

    [Fact]
    public void Calculate_PlanWithoutTiers_ChargesOnlyMonthlyFee()
    {
        var result = BillingCalculator.Calculate(3000m, 500, [], 900);

        Assert.Equal(400, result.OverageDocuments);
        Assert.Empty(result.Tiers);
        Assert.Equal(3000m, result.Total);
    }

    [Fact]
    public void ValidatePlan_StandardTiers_IsValid()
    {
        Assert.Empty(BillingCalculator.ValidatePlan(500, 3000m, StandardTiers));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void ValidatePlan_InvalidLimit_ReturnsError(int limit)
    {
        Assert.Contains(BillingCalculator.ValidatePlan(limit, 3000m, StandardTiers),
            e => e.Contains("límite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatePlan_NegativeFee_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, -1m, StandardTiers));
    }

    [Fact]
    public void ValidatePlan_FirstTierNotStartingAtOne_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, 3000m, [new(2, null, 6m)]));
    }

    [Fact]
    public void ValidatePlan_GapBetweenTiers_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, 3000m, [new(1, 100, 6m), new(102, null, 9m)]));
    }

    [Fact]
    public void ValidatePlan_OpenEndedTierNotLast_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, 3000m, [new(1, null, 6m), new(101, null, 9m)]));
    }

    [Fact]
    public void ValidatePlan_ToUnitLowerThanFromUnit_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, 3000m, [new(1, 0, 6m)]));
    }

    [Fact]
    public void ValidatePlan_NegativeUnitPrice_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidatePlan(500, 3000m, [new(1, null, -6m)]));
    }
}
```

- [ ] **Step 2: Ejecutar los tests y verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~BillingCalculatorTests"`
Expected: error de compilación `The type or namespace name 'Billing' does not exist`.

- [ ] **Step 3: Implementar `BillingCalculator`**

Crear `ZynstormECFPlatform.Services/Billing/BillingCalculator.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

public sealed record OverageTier(int FromUnit, int? ToUnit, decimal UnitPrice);

public sealed record OverageTierCharge(int FromUnit, int? ToUnit, decimal UnitPrice, int Units, decimal Amount);

public sealed record BillingCalculationResult(
    decimal MonthlyFee,
    int MonthlyDocumentLimit,
    int AcceptedDocuments,
    int OverageDocuments,
    IReadOnlyList<OverageTierCharge> Tiers,
    decimal OverageAmount,
    decimal Total);

public static class BillingCalculator
{
    public const int UnlimitedDocuments = -1;

    /// <summary>
    /// Calcula mensualidad + excedente escalonado. Cada tramo cubre las unidades de excedente
    /// dentro de [FromUnit, ToUnit] (ToUnit null = sin tope).
    /// </summary>
    public static BillingCalculationResult Calculate(
        decimal monthlyFee,
        int monthlyDocumentLimit,
        IEnumerable<OverageTier> tiers,
        int acceptedDocuments)
    {
        var overage = monthlyDocumentLimit == UnlimitedDocuments
            ? 0
            : Math.Max(0, acceptedDocuments - monthlyDocumentLimit);

        var charges = new List<OverageTierCharge>();
        foreach (var tier in tiers.OrderBy(t => t.FromUnit))
        {
            var upper = tier.ToUnit ?? int.MaxValue;
            var units = overage < tier.FromUnit ? 0 : Math.Min(overage, upper) - tier.FromUnit + 1;
            charges.Add(new OverageTierCharge(tier.FromUnit, tier.ToUnit, tier.UnitPrice, units, units * tier.UnitPrice));
        }

        var overageAmount = charges.Sum(c => c.Amount);

        return new BillingCalculationResult(
            monthlyFee,
            monthlyDocumentLimit,
            acceptedDocuments,
            overage,
            charges,
            overageAmount,
            monthlyFee + overageAmount);
    }

    public static List<string> ValidatePlan(int monthlyDocumentLimit, decimal monthlyFee, IEnumerable<OverageTier> tiers)
    {
        var errors = new List<string>();

        if (monthlyDocumentLimit != UnlimitedDocuments && monthlyDocumentLimit <= 0)
            errors.Add("El límite mensual debe ser mayor que 0, o -1 para ilimitado.");

        if (monthlyFee < 0)
            errors.Add("La mensualidad no puede ser negativa.");

        var ordered = tiers.OrderBy(t => t.FromUnit).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var tier = ordered[i];
            var position = i + 1;

            if (i == 0 && tier.FromUnit != 1)
                errors.Add("El primer tramo debe iniciar en 1.");

            if (i > 0 && ordered[i - 1].ToUnit is int previousTo && tier.FromUnit != previousTo + 1)
                errors.Add($"El tramo {position} debe iniciar en {previousTo + 1}.");

            if (tier.ToUnit is null && i < ordered.Count - 1)
                errors.Add("Solo el último tramo puede no tener tope.");

            if (tier.ToUnit is int to && to < tier.FromUnit)
                errors.Add($"En el tramo {position} el 'hasta' no puede ser menor que el 'desde'.");

            if (tier.UnitPrice < 0)
                errors.Add($"El precio del tramo {position} no puede ser negativo.");
        }

        return errors;
    }
}
```

- [ ] **Step 4: Ejecutar los tests y verificar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~BillingCalculatorTests"`
Expected: `Passed! - Failed: 0, Passed: 14`

- [ ] **Step 5: Commit**

```bash
git add ZynstormECFPlatform.Services/Billing/BillingCalculator.cs ZynstormECFPlatform.Tests/Billing/BillingCalculatorTests.cs
git commit -m "feat(billing): calculadora de excedente escalonado y validación de planes"
```

---

### Task 2: Modelo de datos + migración

**Files:**
- Create: `ZynstormECFPlatform.Core/Entities/Plan.cs`
- Create: `ZynstormECFPlatform.Core/Entities/PlanOverageTier.cs`
- Create: `ZynstormECFPlatform.Core/Entities/ClientMonthlyUsage.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Status.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/EcfDocument.cs` (después de `HangfireJobId`, ~línea 68)
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs` (bloques `Entity<Client>` ~línea 381 y `Entity<EcfDocument>` ~línea 674)
- Create: `ZynstormECFPlatform.Abstractions/DataServices/IPlanService.cs`, `IPlanOverageTierService.cs`, `IClientMonthlyUsageService.cs`
- Create: `ZynstormECFPlatform.Data.Services/PlanService.cs`, `PlanOverageTierService.cs`, `ClientMonthlyUsageService.cs`
- Create (generada): `ZynstormECFPlatform.Data/Migrations/*_AddClientPlansAndMonthlyUsage.cs`

**Interfaces:**
- Produces:
  - `Plan { int PlanId; string Name; string? Description; decimal MonthlyFee; int MonthlyDocumentLimit; int StatusId; Status Status; ICollection<PlanOverageTier> OverageTiers; ICollection<Client> Clients }`
  - `PlanOverageTier { int PlanOverageTierId; int PlanId; int FromUnit; int? ToUnit; decimal UnitPrice; Plan Plan }`
  - `ClientMonthlyUsage { int ClientMonthlyUsageId; int ClientId; int Year; int Month; int AcceptedDocuments; int? PlanId; string PlanName; decimal MonthlyFee; int MonthlyDocumentLimit; Client Client; Plan? Plan }`
  - `Client.PlanId` (`int?`), `Client.ClientInactive` (`bool`), `Client.Plan` (`Plan?`), `Client.MonthlyUsages`
  - `EcfDocument.BillingCountedAtUtc` (`DateTime?`). EF **nunca** lo escribe en updates, así que los `UpdateAsync(ecfDocument)` existentes no lo pisan.
  - `IPlanService : IRepository<Plan>`, `IPlanOverageTierService : IRepository<PlanOverageTier>`, `IClientMonthlyUsageService : IRepository<ClientMonthlyUsage>`. Se registran solos en DI por convención de nombre (`AddDataServices`).

- [ ] **Step 1: Crear las entidades nuevas**

`ZynstormECFPlatform.Core/Entities/Plan.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public partial class Plan : BaseEntity
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal MonthlyFee { get; set; }

    /// <summary>Cantidad de comprobantes incluidos al mes. -1 = ilimitado.</summary>
    public int MonthlyDocumentLimit { get; set; }

    public int StatusId { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<PlanOverageTier> OverageTiers { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}
```

`ZynstormECFPlatform.Core/Entities/PlanOverageTier.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public partial class PlanOverageTier : BaseEntity
{
    public int PlanOverageTierId { get; set; }

    public int PlanId { get; set; }

    public int FromUnit { get; set; }

    /// <summary>Null = sin tope superior.</summary>
    public int? ToUnit { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Plan Plan { get; set; } = null!;
}
```

`ZynstormECFPlatform.Core/Entities/ClientMonthlyUsage.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientMonthlyUsage : BaseEntity
{
    public int ClientMonthlyUsageId { get; set; }

    public int ClientId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int AcceptedDocuments { get; set; }

    // Snapshot del plan al crear la fila del mes
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal MonthlyFee { get; set; }

    public int MonthlyDocumentLimit { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Plan? Plan { get; set; }
}
```

- [ ] **Step 2: Modificar entidades existentes**

En `Client.cs`, después de `public bool IsCertified { get; set; }`:

```csharp
    public int? PlanId { get; set; }

    public bool ClientInactive { get; set; }

    public virtual Plan? Plan { get; set; }

    public virtual ICollection<ClientMonthlyUsage> MonthlyUsages { get; set; } = [];
```

En `Status.cs`, después de `public virtual ICollection<Client> Clients { get; set; } = [];`:

```csharp

    public virtual ICollection<Plan> Plans { get; set; } = [];
```

En `EcfDocument.cs`, después de `public string? HangfireJobId { get; set; }`:

```csharp

    /// <summary>Fecha en que el documento se sumó al consumo mensual del cliente. Lo escribe solo ClientUsageService.</summary>
    public DateTime? BillingCountedAtUtc { get; set; }
```

- [ ] **Step 3: Configurar en `StorageContext`**

Agregar `using Microsoft.EntityFrameworkCore.Metadata;` al inicio de `StorageContext.cs` si no existe.

Dentro de `modelBuilder.Entity<Client>(entity => { ... })`, después del bloque `entity.HasOne(d => d.Status) ... .HasConstraintName("FK_Client_Status");`:

```csharp

            entity.Property(e => e.ClientInactive)
                  .HasDefaultValue(false)
                  .IsRequired();

            entity.HasOne(d => d.Plan)
                  .WithMany(p => p.Clients)
                  .HasForeignKey(d => d.PlanId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_Client_Plan");
```

Dentro de `modelBuilder.Entity<EcfDocument>(entity => { ... })`, después del bloque de `HangfireJobId`:

```csharp

            entity.Property(e => e.BillingCountedAtUtc)
                  .HasColumnType(DateTimeColumnType);
            entity.Property(e => e.BillingCountedAtUtc)
                  .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
```

Después del cierre `});` del bloque `modelBuilder.Entity<Client>`, agregar:

```csharp

        modelBuilder.Entity<Plan>(entity =>
        {
            entity.HasKey(e => e.PlanId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.Description)
                  .HasMaxLength(300)
                  .IsUnicode(false);

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

            entity.HasOne(d => d.Status)
                  .WithMany(p => p.Plans)
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_Plan_Status");
        });

        modelBuilder.Entity<PlanOverageTier>(entity =>
        {
            entity.HasKey(e => e.PlanOverageTierId);

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

            entity.HasOne(d => d.Plan)
                  .WithMany(p => p.OverageTiers)
                  .HasForeignKey(d => d.PlanId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .HasConstraintName("FK_PlanOverageTier_Plan");
        });

        modelBuilder.Entity<ClientMonthlyUsage>(entity =>
        {
            entity.HasKey(e => e.ClientMonthlyUsageId);

            // Requerido por el INSERT ... ON CONFLICT de ClientUsageService (no filtrar este índice)
            entity.HasIndex(e => new { e.ClientId, e.Year, e.Month })
                  .IsUnique()
                  .HasDatabaseName("IX_ClientMonthlyUsage_ClientId_Year_Month");

            entity.Property(e => e.AcceptedDocuments)
                  .HasDefaultValue(0);

            entity.Property(e => e.PlanName)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

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
                  .WithMany(p => p.MonthlyUsages)
                  .HasForeignKey(d => d.ClientId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientMonthlyUsage_Client");

            entity.HasOne(d => d.Plan)
                  .WithMany()
                  .HasForeignKey(d => d.PlanId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_ClientMonthlyUsage_Plan");
        });
```

- [ ] **Step 4: Crear servicios de datos (interfaces + implementaciones)**

`ZynstormECFPlatform.Abstractions/DataServices/IPlanService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IPlanService : IRepository<Plan>
{
}
```

`ZynstormECFPlatform.Abstractions/DataServices/IPlanOverageTierService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IPlanOverageTierService : IRepository<PlanOverageTier>
{
}
```

`ZynstormECFPlatform.Abstractions/DataServices/IClientMonthlyUsageService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientMonthlyUsageService : IRepository<ClientMonthlyUsage>
{
}
```

`ZynstormECFPlatform.Data.Services/PlanService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class PlanService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<Plan>(context, sqlGenerator), IPlanService
{
}
```

`ZynstormECFPlatform.Data.Services/PlanOverageTierService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class PlanOverageTierService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<PlanOverageTier>(context, sqlGenerator), IPlanOverageTierService
{
}
```

`ZynstormECFPlatform.Data.Services/ClientMonthlyUsageService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientMonthlyUsageService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientMonthlyUsage>(context, sqlGenerator), IClientMonthlyUsageService
{
}
```

- [ ] **Step 5: Build**

Run: `dotnet build ZynstormECFPlatform.slnx -v q`
Expected: `Build succeeded`, 0 errores.

- [ ] **Step 6: Generar la migración**

Run: `dotnet ef migrations add AddClientPlansAndMonthlyUsage --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: `Done.` Verificar que `Up` del archivo generado contiene **solo**:
- `CreateTable` de `"Plan"`, `"PlanOverageTier"` y `"ClientMonthlyUsage"` (con esos nombres exactos).
- `AddColumn` `PlanId` y `ClientInactive` (default `false`) en `"Client"`, y `BillingCountedAtUtc` en `"EcfDocument"`.
- Índice único `IX_ClientMonthlyUsage_ClientId_Year_Month` y las FKs `FK_Client_Plan`, `FK_Plan_Status`, `FK_PlanOverageTier_Plan`, `FK_ClientMonthlyUsage_Client`, `FK_ClientMonthlyUsage_Plan`.

Si aparece cualquier otro cambio no relacionado, quitarlo del `Up` y del `Down`. Si los nombres de tabla no coinciden con los de arriba, **detenerse y reportarlo**, porque el SQL de la Task 3 depende de ellos.

- [ ] **Step 7: Aplicar la migración a la base local**

Run: `dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: `Done.`

- [ ] **Step 8: Tests + commit**

Run: `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: todos pasan.

```bash
git add ZynstormECFPlatform.Core/Entities ZynstormECFPlatform.Data/StorageContext.cs ZynstormECFPlatform.Data/Migrations ZynstormECFPlatform.Abstractions/DataServices ZynstormECFPlatform.Data.Services
git commit -m "feat(plans): entidades Plan, PlanOverageTier, ClientMonthlyUsage y campos de cliente"
```

---

### Task 3: `ClientUsageService` + registro del consumo al aceptar

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/IClientUsageService.cs`
- Create: `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs` (sección `// --- Production ---`, ~línea 52)
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (constructor; ~líneas 292-293, 323-328, 459-461)
- Modify: `ZynstormECFPlatform.Services/Jobs/EcfTrackingJob.cs` (constructor; `PersistStatusAsync` ~línea 153)
- Test: `ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs`

**Interfaces:**
- Consumes: `IClientService`, `IPlanService`, `IClientMonthlyUsageService` (Task 2), `EcfDocument.BillingCountedAtUtc` (Task 2).
- Produces (namespace `ZynstormECFPlatform.Services.Billing`):
  - `public interface IClientUsageService { Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default); }`. Devuelve `true` si sumó 1 y `false` si no aplica (sin plan activo o ya contado). Nunca lanza: los errores se registran en el log y devuelve `false`.
  - `public static class ClientUsageSql { public const string RegisterAccepted; }`

- [ ] **Step 1: Escribir el test del SQL (guarda contra regresiones de idempotencia)**

Crear `ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientUsageSqlTests
{
    [Fact]
    public void RegisterAccepted_OnlyMarksDocumentsNotCountedYet()
    {
        Assert.Contains("\"BillingCountedAtUtc\" IS NULL", ClientUsageSql.RegisterAccepted);
    }

    [Fact]
    public void RegisterAccepted_UpsertsOnClientYearMonth()
    {
        Assert.Contains("ON CONFLICT (\"ClientId\", \"Year\", \"Month\")", ClientUsageSql.RegisterAccepted);
        Assert.Contains("\"AcceptedDocuments\" = \"ClientMonthlyUsage\".\"AcceptedDocuments\" + 1", ClientUsageSql.RegisterAccepted);
    }

    [Fact]
    public void RegisterAccepted_InsertsOnlyWhenDocumentWasMarked()
    {
        Assert.Contains("FROM marked", ClientUsageSql.RegisterAccepted);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~ClientUsageSqlTests"`
Expected: error de compilación `The name 'ClientUsageSql' does not exist`.

- [ ] **Step 3: Implementar el servicio**

`ZynstormECFPlatform.Services/Billing/IClientUsageService.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

public interface IClientUsageService
{
    /// <summary>
    /// Suma 1 al consumo mensual del cliente si tiene plan activo y el documento no fue contado antes.
    /// Nunca lanza excepción: los errores se registran y devuelve false.
    /// </summary>
    Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default);
}
```

`ZynstormECFPlatform.Services/Billing/ClientUsageService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public static class ClientUsageSql
{
    // Una sola sentencia: marca el documento (solo si no estaba contado) y, únicamente si lo marcó,
    // inserta o incrementa la fila del mes. Es atómica e idempotente ante reintentos y concurrencia.
    public const string RegisterAccepted = """
        WITH marked AS (
            UPDATE "EcfDocument"
            SET "BillingCountedAtUtc" = @Now
            WHERE "EcfDocumentId" = @EcfDocumentId AND "BillingCountedAtUtc" IS NULL
            RETURNING "ClientId"
        )
        INSERT INTO "ClientMonthlyUsage"
            ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
        SELECT "ClientId", @Year, @Month, 1, @PlanId, @PlanName, @MonthlyFee, @MonthlyDocumentLimit, @Now
        FROM marked
        ON CONFLICT ("ClientId", "Year", "Month")
        DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                      "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";
        """;
}

public class ClientUsageService(
    IClientService clientService,
    IPlanService planService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    ILogger<ClientUsageService> logger) : IClientUsageService
{
    public async Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await clientService.GetNoTrackingByAsync(c => c.ClientId == clientId, cancellationToken);
            if (client?.PlanId is not int planId)
                return false;

            var plan = await planService.GetNoTrackingByAsync(
                p => p.PlanId == planId && p.StatusId == (int)StatusEnum.Active, cancellationToken);
            if (plan == null)
                return false;

            var drNow = DateTimeExtensions.DrNow;
            var affected = await clientMonthlyUsageService.ExecuteAsync(ClientUsageSql.RegisterAccepted, new
            {
                Now = DateTime.UtcNow,
                EcfDocumentId = ecfDocumentId,
                Year = drNow.Year,
                Month = drNow.Month,
                PlanId = plan.PlanId,
                PlanName = plan.Name,
                MonthlyFee = plan.MonthlyFee,
                MonthlyDocumentLimit = plan.MonthlyDocumentLimit
            }, cancellationToken);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registrando consumo mensual. EcfDocumentId {EcfDocumentId}, ClientId {ClientId}", ecfDocumentId, clientId);
            return false;
        }
    }
}
```

> Nota: si `DateTimeExtensions` no está en el namespace `ZynstormECFPlatform.Common` (ver `ZynstormECFPlatform.Common/DateTimeExtensions.cs`), ajustar el `using` al namespace declarado en ese archivo.

- [ ] **Step 4: Registrar en DI**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, después de la línea `services.AddTransient<Production.IReceivedEcfProductionService, Production.ReceivedEcfProductionService>();`:

```csharp
        services.AddTransient<Billing.IClientUsageService, Billing.ClientUsageService>();
```

- [ ] **Step 5: Invocar desde `ReceivedEcfProductionService`**

Agregar `using ZynstormECFPlatform.Services.Billing;`. Agregar el campo `private readonly IClientUsageService _clientUsageService;`, el parámetro `IClientUsageService clientUsageService` al final del constructor (después de `IHostEnvironment hostEnvironment`) y la asignación `_clientUsageService = clientUsageService;`.

a) Respuesta con TrackId. Reemplazar:

```csharp
            await MarkDocumentAsync(ecfDocument, statusId, resultDto.Message);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId, signedXml, status);
```

por:

```csharp
            await MarkDocumentAsync(ecfDocument, statusId, resultDto.Message);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId, signedXml, status);

            if (resultDto.Success || resultDto.IsAcceptedConditional)
                await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, client.ClientId, cancellationToken);
```

b) Respuesta sin TrackId. Reemplazar:

```csharp
        if (resultDto.Success)
            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"e-CF aprobado. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");

        return resultDto;
```

por:

```csharp
        if (resultDto.Success)
        {
            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"e-CF aprobado. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");
            await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, client.ClientId, cancellationToken);
        }

        return resultDto;
```

c) Validación en staging (`ProcessWithStagingValidationAsync`). Reemplazar:

```csharp
                await AddLogAsync(ecfDocument, clientId, "Information", $"XML validado y aceptado por endpoint interno. QR: {resultDto.QrUrl}", statusBody);
                return resultDto;
```

por:

```csharp
                await AddLogAsync(ecfDocument, clientId, "Information", $"XML validado y aceptado por endpoint interno. QR: {resultDto.QrUrl}", statusBody);
                await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId);
                return resultDto;
```

- [ ] **Step 6: Invocar desde `EcfTrackingJob`**

Agregar `using ZynstormECFPlatform.Services.Billing;`, el campo `private readonly IClientUsageService _clientUsageService;`, el parámetro `IClientUsageService clientUsageService` antes de `ILogger<EcfTrackingJob> logger` y la asignación `_clientUsageService = clientUsageService;`.

En `PersistStatusAsync`, reemplazar:

```csharp
        // Dispatch notifications if status is final (Aceptado = 10, Rechazado = 11, Error = 12)
```

por:

```csharp
        if (statusId == 10 || ReceivedEcfProductionService.IsAcceptedConditionalDgiiStatus(statusResponse))
        {
            await _clientUsageService.RegisterAcceptedAsync(ecfDocumentId, ecfDocument.ClientId);
        }

        // Dispatch notifications if status is final (Aceptado = 10, Rechazado = 11, Error = 12)
```

- [ ] **Step 7: Verificar que no hay otros constructores manuales rotos**

Run: `dotnet build ZynstormECFPlatform.slnx -v q`
Expected: `Build succeeded`. Si `TestApp` u otro proyecto instancia `ReceivedEcfProductionService` o `EcfTrackingJob` con `new`, agregar ahí `new Mock<IClientUsageService>().Object` (o el equivalente) hasta que compile.

- [ ] **Step 8: Tests**

Run: `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: todos pasan (incluye los 3 de `ClientUsageSqlTests`).

- [ ] **Step 9: Verificación manual del SQL contra PostgreSQL local**

Con la API corriendo en el ambiente local y un cliente con plan activo, emitir un e-CF aceptado dos veces al mismo documento (o reencolar el job) y consultar:

```sql
SELECT "ClientId", "Year", "Month", "AcceptedDocuments", "PlanName" FROM "ClientMonthlyUsage";
SELECT "EcfDocumentId", "BillingCountedAtUtc" FROM "EcfDocument" ORDER BY "EcfDocumentId" DESC LIMIT 5;
```

Expected: una fila por cliente/mes; `AcceptedDocuments` sube 1 por documento aceptado (no 2 por el mismo documento); `BillingCountedAtUtc` con valor en los documentos contados.

- [ ] **Step 10: Commit**

```bash
git add ZynstormECFPlatform.Services ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs
git commit -m "feat(billing): registro idempotente de consumo mensual al aceptar e-CF"
```

---

### Task 4: `emit` — cliente inactivo y `EcfDocumentId` en todas las respuestas

**Files:**
- Modify: `ZynstormECFPlatform.Services/Production/IReceivedEcfProductionService.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` (`ProcessAsync`, líneas ~96-331)
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs` (`EmitEcf`, líneas ~91-125)
- Test: `ZynstormECFPlatform.Tests/Production/ClientInactiveResultTests.cs`

**Interfaces:**
- Consumes: `Client.ClientInactive` (Task 2).
- Produces:
  - `ReceivedEcfEmissionResultDto.ClientInactive` (`bool`), `.ConfigurationErrors` (`List<string>`), `.HasUnexpectedError` (`bool`, `[JsonIgnore]`).
  - `ReceivedEcfProductionService.ClientInactiveMessage` (`public const string`).
  - `public static ReceivedEcfEmissionResultDto ReceivedEcfProductionService.BuildClientInactiveResult(ReceivedEcfEmissionResultDto resultDto)`.

- [ ] **Step 1: Escribir el test que falla**

Crear `ZynstormECFPlatform.Tests/Production/ClientInactiveResultTests.cs`:

```csharp
using System.Text.Json;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class ClientInactiveResultTests
{
    [Fact]
    public void BuildClientInactiveResult_SetsFlagMessageAndFailure()
    {
        var result = ReceivedEcfProductionService.BuildClientInactiveResult(new ReceivedEcfEmissionResultDto { Success = true });

        Assert.False(result.Success);
        Assert.True(result.ClientInactive);
        Assert.Equal("El cliente se encuentra desactivado. Por favor, comuníquese con soporte.", result.Message);
        Assert.Equal(0, result.EcfDocumentId);
    }

    [Fact]
    public void ResultDto_SerializesClientInactiveAndEcfDocumentId()
    {
        var json = JsonSerializer.Serialize(
            new ReceivedEcfEmissionResultDto { ClientInactive = true, EcfDocumentId = 42 },
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"clientInactive\":true", json);
        Assert.Contains("\"ecfDocumentId\":42", json);
        Assert.DoesNotContain("hasUnexpectedError", json);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests -v q --filter "FullyQualifiedName~ClientInactiveResultTests"`
Expected: error de compilación `'ReceivedEcfEmissionResultDto' does not contain a definition for 'ClientInactive'`.

- [ ] **Step 3: Extender el DTO de resultado**

En `IReceivedEcfProductionService.cs`, dentro de `ReceivedEcfEmissionResultDto`, después de `public bool RequiresCorrection { get; set; }`:

```csharp
    public bool ClientInactive { get; set; }

    [JsonIgnore]
    public bool HasUnexpectedError { get; set; }
```

Y después de `public List<string> XmlProdErrors { get; set; } = [];`:

```csharp
    public List<string> ConfigurationErrors { get; set; } = [];
```

- [ ] **Step 4: Helper de cliente inactivo**

En `ReceivedEcfProductionService`, justo antes de `public async Task<ReceivedEcfEmissionResultDto> ProcessAsync(`:

```csharp
    public const string ClientInactiveMessage = "El cliente se encuentra desactivado. Por favor, comuníquese con soporte.";

    public static ReceivedEcfEmissionResultDto BuildClientInactiveResult(ReceivedEcfEmissionResultDto resultDto)
    {
        resultDto.Success = false;
        resultDto.ClientInactive = true;
        resultDto.Message = ClientInactiveMessage;
        return resultDto;
    }

```

- [ ] **Step 5: Reestructurar `ProcessAsync`**

a) Reemplazar el bloque de búsquedas que lanzan excepción:

```csharp
        var client = await _clientService.GetByAsync(c => c.Rnc == issuerRnc)
            ?? throw new Exception($"Cliente con RNC {issuerRnc} no encontrado.");
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("ApiKey no encontrada.");
        var clientBranch = await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
            ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId);
        var currency = await _currencyService.GetByAsync(x => x.Code == "DOP")
            ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0)
            ?? throw new Exception("No hay moneda configurada para registrar el e-CF.");
        var ecfTypeEntity = await _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString())
            ?? throw new Exception($"TipoeCF {ecfType} no esta configurado.");
```

por:

```csharp
        var client = await _clientService.GetByAsync(c => c.Rnc == issuerRnc);
        if (client == null)
            return FailConfiguration(resultDto, $"Cliente con RNC {issuerRnc} no encontrado.");

        if (client.ClientInactive)
            return BuildClientInactiveResult(resultDto);

        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        if (apiKey == null)
            return FailConfiguration(resultDto, "ApiKey no encontrada.");

        var clientBranch = await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
            ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId);
        var currency = await _currencyService.GetByAsync(x => x.Code == "DOP")
            ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0);
        if (currency == null)
            return FailConfiguration(resultDto, "No hay moneda configurada para registrar el e-CF.");

        var ecfTypeEntity = await _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString());
        if (ecfTypeEntity == null)
            return FailConfiguration(resultDto, $"TipoeCF {ecfType} no esta configurado.");
```

b) Envolver todo lo posterior a la creación del documento. **Cortar** desde la línea

```csharp
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = 1,
```

hasta el `return resultDto;` final de `ProcessAsync` (inclusive; el cierre `}` del método se queda). En su lugar, justo después de `resultDto.EcfDocumentId = ecfDocument.EcfDocumentId;`, pegar:

```csharp

        try
        {
            return await ContinueProcessingAsync(
                resultDto, dto, client, apiKey, ecfDocument, ecfType, targetEnvironment,
                issuerRnc, eNcf, statusDelayMilliseconds, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            resultDto.Success = false;
            resultDto.HasUnexpectedError = true;
            resultDto.Message = $"Error inesperado durante la emision del e-CF: {ex.Message}";
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Error", resultDto.Message, ex.ToString());
            return resultDto;
        }
    }

    private static ReceivedEcfEmissionResultDto FailConfiguration(ReceivedEcfEmissionResultDto resultDto, string message)
    {
        resultDto.Success = false;
        resultDto.Message = message;
        resultDto.ConfigurationErrors.Add(message);
        return resultDto;
    }

    private async Task<ReceivedEcfEmissionResultDto> ContinueProcessingAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfInvoiceRequestDto dto,
        Client client,
        ApiKey apiKey,
        EcfDocument ecfDocument,
        int ecfType,
        DgiiEnvironment targetEnvironment,
        string issuerRnc,
        string eNcf,
        int statusDelayMilliseconds,
        CancellationToken cancellationToken)
    {
```

y a continuación **pegar el bloque cortado** como cuerpo de `ContinueProcessingAsync`. El `}` original de `ProcessAsync` pasa a cerrar `ContinueProcessingAsync`.

c) Dentro del bloque pegado, reemplazar el certificado que lanza excepción:

```csharp
        var certificate = await _clientCertificateService.GetActiveCertificateAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("Certificado no encontrado.");
```

por:

```csharp
        var certificate = await _clientCertificateService.GetActiveCertificateAsync(x => x.ClientId == client.ClientId);
        if (certificate == null)
        {
            FailConfiguration(resultDto, "Certificado no encontrado.");
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message);
            return resultDto;
        }
```

> `AddLogAsync` se usa en el archivo con 4 y con 5 argumentos (`detail` opcional). Si la firma no acepta 4 argumentos, pasar `null` como quinto.

- [ ] **Step 6: Build**

Run: `dotnet build ZynstormECFPlatform.slnx -v q`
Expected: `Build succeeded`. Si hay errores de variables no definidas en `ContinueProcessingAsync`, agregarlas como parámetro con el mismo nombre y tipo que tenían en `ProcessAsync` (no recalcularlas).

- [ ] **Step 7: Actualizar `EmitEcf` en el controller**

En `EcfController.EmitEcf`, reemplazar:

```csharp
                if (result.DtoErrors.Count > 0 || result.XsdErrors.Count > 0 || result.XmlProdErrors.Count > 0 || result.XmlValidation?.IsValid == false)
                    return BadRequest(result);
```

por:

```csharp
                if (result.ClientInactive)
                    return StatusCode(StatusCodes.Status403Forbidden, result);

                if (result.HasUnexpectedError)
                    return StatusCode(StatusCodes.Status500InternalServerError, result);

                if (result.DtoErrors.Count > 0 || result.XsdErrors.Count > 0 || result.XmlProdErrors.Count > 0
                    || result.ConfigurationErrors.Count > 0 || result.XmlValidation?.IsValid == false)
                    return BadRequest(result);
```

Agregar `[ProducesResponseType(StatusCodes.Status403Forbidden)]` sobre `EmitEcf`. El `catch` final del controller se deja igual: solo atrapa fallos antes de crear el documento (no hay Id que devolver).

- [ ] **Step 8: Tests**

Run: `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: todos pasan (incluye `ClientInactiveResultTests`).

- [ ] **Step 9: Verificación manual**

Con la API local: poner `"ClientInactive" = true` a un cliente de prueba (`UPDATE "Client" SET "ClientInactive" = true WHERE "RNC" = '<rnc>';`) y llamar `POST /v1/Ecf/emit` con un payload de `SamplePayloads/`.
Expected: HTTP 403 con `clientInactive: true`, `ecfDocumentId: 0` y el mensaje exacto; ningún `EcfDocument` nuevo. Revertir a `false`, emitir de nuevo y confirmar `ecfDocumentId > 0` en la respuesta (200, 400 o 504).

- [ ] **Step 10: Commit**

```bash
git add ZynstormECFPlatform.Services/Production ZynstormECFPlatform.Web.Api/Controllers/EcfController.cs ZynstormECFPlatform.Tests/Production/ClientInactiveResultTests.cs
git commit -m "feat(emit): bloquear clientes inactivos y devolver EcfDocumentId en todas las respuestas"
```

---

### Task 5: API de planes (`PlanController`)

**Files:**
- Create: `ZynstormECFPlatform.Dtos/PlanDtos.cs`
- Modify: `ZynstormECFPlatform.Mappings/MappingProfiles.cs` (después del bloque `// Client`)
- Create: `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs`

**Interfaces:**
- Consumes: `IPlanService`, `IPlanOverageTierService` (Task 2); `BillingCalculator.ValidatePlan`, `OverageTier` (Task 1).
- Produces: `PlanOverageTierDto { int FromUnit; int? ToUnit; decimal UnitPrice }`, `PlanCreateDto { string Name; string? Description; decimal MonthlyFee; int MonthlyDocumentLimit; bool IsActive; List<PlanOverageTierDto> OverageTiers }`, `PlanUpdateDto : PlanCreateDto { string GuidId }`, `PlanViewDto : PlanUpdateDto { int PlanId; int ClientsCount; DateTime RegisteredAt }`. Rutas: `GET/POST/PUT v1/Plan`, `GET/DELETE v1/Plan/guid/{guid}`. JSON en camelCase.

- [ ] **Step 1: DTOs**

Crear `ZynstormECFPlatform.Dtos/PlanDtos.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class PlanOverageTierDto
{
    [Range(1, int.MaxValue)]
    public int FromUnit { get; set; }

    public int? ToUnit { get; set; }

    [Range(0, 9999999)]
    public decimal UnitPrice { get; set; }
}

public class PlanCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Range(0, 99999999)]
    public decimal MonthlyFee { get; set; }

    /// <summary>-1 = ilimitado.</summary>
    public int MonthlyDocumentLimit { get; set; }

    public bool IsActive { get; set; } = true;

    public List<PlanOverageTierDto> OverageTiers { get; set; } = [];
}

public class PlanUpdateDto : PlanCreateDto
{
    [Required]
    public string GuidId { get; set; } = string.Empty;
}

public class PlanViewDto : PlanUpdateDto
{
    public int PlanId { get; set; }

    public int ClientsCount { get; set; }

    public DateTime RegisteredAt { get; set; }
}
```

- [ ] **Step 2: Mapeos**

En `MappingProfiles.cs`, después del bloque `CreateMap<Client, ClientViewDto>() ... ));`:

```csharp

        // Plan
        CreateMap<PlanOverageTierDto, PlanOverageTier>();
        CreateMap<PlanOverageTier, PlanOverageTierDto>();

        CreateMap<PlanCreateDto, Plan>()
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.IsActive ? (int)Enums.StatusEnum.Active : (int)Enums.StatusEnum.Inactive));

        CreateMap<PlanUpdateDto, Plan>()
            .ForMember(dest => dest.PlanId, opt => opt.Ignore())
            .ForMember(dest => dest.OverageTiers, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => src.IsActive ? (int)Enums.StatusEnum.Active : (int)Enums.StatusEnum.Inactive));

        CreateMap<Plan, PlanViewDto>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.StatusId == (int)Enums.StatusEnum.Active))
            .ForMember(dest => dest.ClientsCount, opt => opt.MapFrom(src => src.Clients.Count))
            .ForMember(dest => dest.OverageTiers, opt => opt.MapFrom(src => src.OverageTiers.OrderBy(t => t.FromUnit)));
```

- [ ] **Step 3: Controller**

Crear `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs`:

```csharp
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [Authorize(Roles = "SA")]
    public class PlanController(
        IPlanService planService,
        IPlanOverageTierService planOverageTierService,
        IMapper mapper,
        ILoggerFactory loggerFactory) : BaseController<PlanController, Plan, PlanCreateDto, PlanUpdateDto, PlanViewDto>(planService, mapper, loggerFactory)
    {
        private IQueryable<Plan> PlansWithDetails =>
            Repository.Table.AsNoTracking().Include(p => p.OverageTiers).Include(p => p.Clients);

        [HttpGet]
        [Route("", Order = 1)]
        public override async Task<ActionResult> Get([FromQuery] string? guidId, [FromQuery] string? id, CancellationToken cancellationToken = default)
        {
            try
            {
                var plans = await PlansWithDetails.OrderBy(p => p.Name).ToListAsync(cancellationToken);
                return Ok(Mapper.Map<IEnumerable<Plan>, IEnumerable<PlanViewDto>>(plans));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> GetByGuid(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var plan = await PlansWithDetails.FirstOrDefaultAsync(p => p.GuidId == guid, cancellationToken);
                if (plan == null) return NotFound();
                return Ok(Mapper.Map<Plan, PlanViewDto>(plan));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        [Route("", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> Post([FromBody] PlanCreateDto dto)
        {
            var errors = Validate(dto);
            if (errors.Count > 0)
                return BadRequest(new { success = false, message = string.Join(" ", errors), errors });

            try
            {
                var model = Mapper.Map<PlanCreateDto, Plan>(dto);
                model = await Repository.InsertAsync(model);
                return Ok(Mapper.Map<Plan, PlanViewDto>(model!));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPut]
        [Route("", Order = 1)]
        public override async Task<ActionResult<PlanViewDto>> Put([FromBody] PlanUpdateDto dto)
        {
            var errors = Validate(dto);
            if (errors.Count > 0)
                return BadRequest(new { success = false, message = string.Join(" ", errors), errors });

            try
            {
                var model = await Repository.Table
                    .Include(p => p.OverageTiers)
                    .Include(p => p.Clients)
                    .FirstOrDefaultAsync(p => p.GuidId == dto.GuidId);

                if (model == null)
                    return NotFound("No se encontró el plan.");

                Mapper.Map(dto, model);

                var previousTiers = model.OverageTiers.ToList();
                if (previousTiers.Count > 0)
                    await planOverageTierService.HardDeleteAsync(previousTiers);

                model.OverageTiers = dto.OverageTiers
                    .Select(t => new PlanOverageTier { FromUnit = t.FromUnit, ToUnit = t.ToUnit, UnitPrice = t.UnitPrice })
                    .ToList();

                await Repository.UpdateAsync(model);

                return Ok(Mapper.Map<Plan, PlanViewDto>(model));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private static List<string> Validate(PlanCreateDto dto) =>
            BillingCalculator.ValidatePlan(
                dto.MonthlyDocumentLimit,
                dto.MonthlyFee,
                dto.OverageTiers.Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)));
    }
}
```

> El `DELETE` heredado de `BaseController` hace soft delete. Un cliente cuyo plan fue eliminado deja de contar consumo, porque el query filter oculta el plan.

- [ ] **Step 4: Build + tests**

Run: `dotnet build ZynstormECFPlatform.slnx -v q` y luego `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build OK, tests verdes.

- [ ] **Step 5: Verificación manual**

Con la API local y un token de usuario SA, `POST /v1/Plan`:

```json
{ "name": "Plan Pro", "monthlyFee": 3000, "monthlyDocumentLimit": 500, "isActive": true,
  "overageTiers": [ { "fromUnit": 1, "toUnit": 100, "unitPrice": 6 }, { "fromUnit": 101, "toUnit": null, "unitPrice": 9 } ] }
```

Expected: 200 con `planId`, `guidId` y los dos tramos. Repetir con `"fromUnit": 2` en el primer tramo: 400 con `"El primer tramo debe iniciar en 1."`. `PUT` con un solo tramo: `GET /v1/Plan/guid/{guid}` devuelve solo ese tramo.

- [ ] **Step 6: Commit**

```bash
git add ZynstormECFPlatform.Dtos/PlanDtos.cs ZynstormECFPlatform.Mappings/MappingProfiles.cs ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs
git commit -m "feat(plans): API CRUD de planes con tramos de excedente"
```

---

### Task 6: Cliente con plan/inactivo + reporte de consumo mensual

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs`
- Create: `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`
- Modify: `ZynstormECFPlatform.Mappings/MappingProfiles.cs` (bloque `CreateMap<Client, ClientViewDto>()`)
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`

**Interfaces:**
- Consumes: `Client.PlanId`, `Client.ClientInactive`, `ClientMonthlyUsage`, `PlanOverageTier` (Task 2); `BillingCalculator.Calculate`, `OverageTier` (Task 1); `IPlanService` (Task 2).
- Produces:
  - `ClientCreateDto.PlanId` (`int?`), `ClientCreateDto.ClientInactive` (`bool`), `ClientViewDto.PlanName` (`string?`).
  - `OverageTierChargeDto { int FromUnit; int? ToUnit; decimal UnitPrice; int Units; decimal Amount }`
  - `ClientMonthlyUsageDto { string ClientGuidId; string ClientName; string ClientRnc; bool ClientInactive; int Year; int Month; string PlanName; decimal MonthlyFee; int MonthlyDocumentLimit; int AcceptedDocuments; int OverageDocuments; decimal OverageAmount; decimal Total; List<OverageTierChargeDto> Tiers }`
  - `GET v1/Client/usage?year=&month=` → `ClientMonthlyUsageDto[]`, `GET v1/Client/guid/{guid}/usage?year=&month=` → `ClientMonthlyUsageDto` (404 si no hay consumo ese mes). Ambos solo SA.

- [ ] **Step 1: DTOs**

En `ClientDtos.cs`, dentro de `ClientCreateDto`, después de `WeeklyReportEmails`:

```csharp

    public int? PlanId { get; set; }

    public bool ClientInactive { get; set; }
```

En `ClientViewDto`, después de `public string? ApiKey { get; set; }`:

```csharp

    public string? PlanName { get; set; }
```

Crear `ZynstormECFPlatform.Dtos/ClientMonthlyUsageDtos.cs`:

```csharp
namespace ZynstormECFPlatform.Dtos;

public class OverageTierChargeDto
{
    public int FromUnit { get; set; }
    public int? ToUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
    public decimal Amount { get; set; }
}

public class ClientMonthlyUsageDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int AcceptedDocuments { get; set; }
    public int OverageDocuments { get; set; }
    public decimal OverageAmount { get; set; }
    public decimal Total { get; set; }
    public List<OverageTierChargeDto> Tiers { get; set; } = [];
}
```

- [ ] **Step 2: Mapeo de `PlanName`**

En `MappingProfiles.cs`, reemplazar:

```csharp
        CreateMap<Client, ClientViewDto>()
            .ForMember(dest => dest.ApiKey, opt => opt.MapFrom(src => 
```

por:

```csharp
        CreateMap<Client, ClientViewDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.Name : null))
            .ForMember(dest => dest.ApiKey, opt => opt.MapFrom(src => 
```

- [ ] **Step 3: `ClientController`: incluir plan y validar `PlanId`**

a) En `using`, agregar `using ZynstormECFPlatform.Services.Billing;`.

b) Agregar al constructor primario, después de `IRepository<EcfDocument> ecfDocumentRepository`:

```csharp
        IPlanService planService,
        IRepository<ClientMonthlyUsage> clientMonthlyUsageRepository,
        IRepository<PlanOverageTier> planOverageTierRepository
```

(con la coma correspondiente después de `ecfDocumentRepository`).

c) Incluir el plan en las lecturas. Reemplazar las **cuatro** apariciones de `.Include(c => c.ApiKeys)` en `Get` (2), `Put` (1) y `GetByGuid` (1) por `.Include(c => c.ApiKeys).Include(c => c.Plan)`.

d) Validación del plan. Agregar este método privado al final de la clase (antes del último `}` de la clase):

```csharp
        private async Task<bool> PlanExistsAsync(int? planId) =>
            !planId.HasValue || await planService.GetNoTrackingByAsync(p => p.PlanId == planId.Value) != null;
```

Al inicio del `try` de `Post` (antes de `Client? model = null;`):

```csharp
                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");

```

Al inicio de `Put`, justo después de la validación de `guid`:

```csharp
                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");
```

En `Post`, cambiar el retorno final `return Ok(Mapper.Map<Client, ClientViewDto>(model!));` por:

```csharp
                if (model?.PlanId != null)
                    model.Plan = await planService.GetNoTrackingByAsync(p => p.PlanId == model.PlanId);

                return Ok(Mapper.Map<Client, ClientViewDto>(model!));
```

En `Put`, antes de `return Ok(Mapper.Map<Client, ClientViewDto>(model));`:

```csharp
                model.Plan = model.PlanId.HasValue
                    ? await planService.GetNoTrackingByAsync(p => p.PlanId == model.PlanId)
                    : null;
```

- [ ] **Step 4: Endpoints de consumo**

Agregar a `ClientController`, antes del método `PlanExistsAsync`:

```csharp
        [HttpGet]
        [Route("usage", Order = 1)]
        public async Task<IActionResult> GetMonthlyUsage([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();
            if (year < 2000 || month is < 1 or > 12)
                return BadRequest("Año o mes inválido.");

            try
            {
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month);

                return Ok(await BuildUsageDtosAsync(query, cancellationToken));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}/usage", Order = 1)]
        public async Task<IActionResult> GetClientMonthlyUsage(string guid, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();
            if (year < 2000 || month is < 1 or > 12)
                return BadRequest("Año o mes inválido.");

            try
            {
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Client.GuidId == guid && u.Year == year && u.Month == month);

                var result = (await BuildUsageDtosAsync(query, cancellationToken)).FirstOrDefault();
                return result == null ? NotFound("El cliente no tiene consumo registrado en ese mes.") : Ok(result);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private async Task<List<ClientMonthlyUsageDto>> BuildUsageDtosAsync(IQueryable<ClientMonthlyUsage> query, CancellationToken cancellationToken)
        {
            var usages = await query.Include(u => u.Client).ToListAsync(cancellationToken);

            var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
            var tiers = await planOverageTierRepository.Table.AsNoTracking()
                .Where(t => planIds.Contains(t.PlanId))
                .ToListAsync(cancellationToken);

            return usages
                .Select(u =>
                {
                    var calculation = BillingCalculator.Calculate(
                        u.MonthlyFee,
                        u.MonthlyDocumentLimit,
                        tiers.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                        u.AcceptedDocuments);

                    return new ClientMonthlyUsageDto
                    {
                        ClientGuidId = u.Client.GuidId,
                        ClientName = u.Client.Name,
                        ClientRnc = u.Client.Rnc,
                        ClientInactive = u.Client.ClientInactive,
                        Year = u.Year,
                        Month = u.Month,
                        PlanName = u.PlanName,
                        MonthlyFee = calculation.MonthlyFee,
                        MonthlyDocumentLimit = calculation.MonthlyDocumentLimit,
                        AcceptedDocuments = calculation.AcceptedDocuments,
                        OverageDocuments = calculation.OverageDocuments,
                        OverageAmount = calculation.OverageAmount,
                        Total = calculation.Total,
                        Tiers = calculation.Tiers.Select(t => new OverageTierChargeDto
                        {
                            FromUnit = t.FromUnit,
                            ToUnit = t.ToUnit,
                            UnitPrice = t.UnitPrice,
                            Units = t.Units,
                            Amount = t.Amount
                        }).ToList()
                    };
                })
                .OrderBy(d => d.ClientName)
                .ToList();
        }
```

- [ ] **Step 5: Build + tests**

Run: `dotnet build ZynstormECFPlatform.slnx -v q` y luego `dotnet test ZynstormECFPlatform.Tests -v q`
Expected: build OK, tests verdes.

- [ ] **Step 6: Verificación manual**

Con token SA: `PUT /v1/Client` asignando `planId` del plan de la Task 5 y `clientInactive: false`. `GET /v1/Client/guid/{guid}` devuelve `planId`, `planName` y `clientInactive`. Después de emitir un e-CF aceptado para ese cliente, `GET /v1/Client/usage?year=<año RD>&month=<mes RD>` devuelve una fila con `acceptedDocuments >= 1`, `total = monthlyFee` (si no pasó el límite) y dos tramos con `units = 0`. Para probar el cálculo sin emitir 650 documentos: `UPDATE "ClientMonthlyUsage" SET "AcceptedDocuments" = 650 WHERE ...;` → `overageDocuments = 150`, `overageAmount = 1050`, `total = 4050`. Revertir el UPDATE.

- [ ] **Step 7: Commit**

```bash
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Mappings/MappingProfiles.cs ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs
git commit -m "feat(clients): plan e inactivo en cliente + reporte de consumo mensual"
```

---

### Task 7: Frontend — tipos, servicios y formulario de cliente

**Files (frontend repo):**
- Create: `types/plan.type.ts`
- Create: `types/usage.type.ts`
- Create: `services/plan.service.ts`
- Create: `services/usage.service.ts`
- Modify: `types/client.type.ts`
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: API de las Tasks 5 y 6 (JSON camelCase).
- Produces:
  - `Plan`, `PlanOverageTier`, `PlanCreate`, `PlanUpdate` (en `types/plan.type.ts`).
  - `ClientMonthlyUsage`, `OverageTierCharge` (en `types/usage.type.ts`).
  - `getPlans(): Promise<Plan[]>`, `getPlanByGuid(guid)`, `createPlan(plan: PlanCreate)`, `updatePlan(plan: PlanUpdate)`, `deletePlan(guid)`.
  - `getMonthlyUsage(year: number, month: number): Promise<ClientMonthlyUsage[]>`, `getClientMonthlyUsage(guid: string, year: number, month: number): Promise<ClientMonthlyUsage>`.
  - `UNLIMITED_DOCUMENTS = -1`.

- [ ] **Step 1: Tipos**

`types/plan.type.ts`:

```ts
export const UNLIMITED_DOCUMENTS = -1

export interface PlanOverageTier {
  fromUnit: number
  toUnit: number | null
  unitPrice: number
}

export interface PlanCreate {
  name: string
  description?: string | null
  monthlyFee: number
  monthlyDocumentLimit: number
  isActive: boolean
  overageTiers: PlanOverageTier[]
}

export interface PlanUpdate extends PlanCreate {
  guidId: string
}

export interface Plan extends PlanUpdate {
  planId: number
  clientsCount: number
  registeredAt?: string
}
```

`types/usage.type.ts`:

```ts
export interface OverageTierCharge {
  fromUnit: number
  toUnit: number | null
  unitPrice: number
  units: number
  amount: number
}

export interface ClientMonthlyUsage {
  clientGuidId: string
  clientName: string
  clientRnc: string
  clientInactive: boolean
  year: number
  month: number
  planName: string
  monthlyFee: number
  monthlyDocumentLimit: number
  acceptedDocuments: number
  overageDocuments: number
  overageAmount: number
  total: number
  tiers: OverageTierCharge[]
}
```

En `types/client.type.ts`, agregar a `Client` (después de `apiKey`):

```ts
  planId?: number | null
  planName?: string | null
  clientInactive?: boolean
```

Y a `ClientCreate` (después de `weeklyReportEmails`):

```ts
  planId?: number | null
  clientInactive?: boolean
```

- [ ] **Step 2: Servicios**

`services/plan.service.ts`:

```ts
import { destroy, get, post, put } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { Plan, PlanCreate, PlanUpdate } from "@/types/plan.type"

const PLAN_URL = `${API_BASE_URL}/v1/Plan`

export const getPlans = async (): Promise<Plan[]> => {
  return await get<Plan[]>(PLAN_URL)
}

export const getPlanByGuid = async (guidId: string): Promise<Plan> => {
  return await get<Plan>(`${PLAN_URL}/guid/${guidId}`)
}

export const createPlan = async (plan: PlanCreate): Promise<Plan> => {
  return await post<Plan>(PLAN_URL, plan)
}

export const updatePlan = async (plan: PlanUpdate): Promise<Plan> => {
  return await put<Plan>(PLAN_URL, plan)
}

export const deletePlan = async (guidId: string): Promise<void> => {
  await destroy<void>(`${PLAN_URL}/guid/${guidId}`)
}
```

`services/usage.service.ts`:

```ts
import { get } from "@/services/fetchHandler"
import { API_BASE_URL } from "@/lib/apiConfig"
import { ClientMonthlyUsage } from "@/types/usage.type"

const CLIENT_URL = `${API_BASE_URL}/v1/Client`

export const getMonthlyUsage = async (year: number, month: number): Promise<ClientMonthlyUsage[]> => {
  return await get<ClientMonthlyUsage[]>(`${CLIENT_URL}/usage?year=${year}&month=${month}`)
}

export const getClientMonthlyUsage = async (
  guidId: string,
  year: number,
  month: number,
): Promise<ClientMonthlyUsage> => {
  return await get<ClientMonthlyUsage>(`${CLIENT_URL}/guid/${guidId}/usage?year=${year}&month=${month}`)
}
```

- [ ] **Step 3: Formulario de cliente, estado y carga de planes**

En `app/clientes/page.tsx`:

a) Imports. Agregar después de `import { Label } from "@/components/ui/label"`:

```tsx
import { Switch } from "@/components/ui/switch"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import { getPlans } from "@/services/plan.service"
import { Plan } from "@/types/plan.type"
```

b) `emptyForm`: agregar después de `weeklyReportEmails: "",`:

```tsx
  planId: null,
  clientInactive: false,
```

c) Dentro del componente de la página, junto a los demás `useState` (al inicio del componente), agregar:

```tsx
  const [plans, setPlans] = useState<Plan[]>([])

  useEffect(() => {
    getPlans()
      .then(setPlans)
      .catch(() => setPlans([]))
  }, [])
```

> `getPlans` requiere rol SA. Para usuarios no SA falla y la lista queda vacía; el selector solo muestra "Sin plan".

d) `openEditDialog`: en el `setFormData({...})`, después de `weeklyReportEmails: client.weeklyReportEmails ?? "",`:

```tsx
      planId: client.planId ?? null,
      clientInactive: client.clientInactive ?? false,
```

e) `handleSubmit`: en `const payload = {...}`, después de `weeklyReportEmails: formData.weeklyReportEmails?.trim() || null,`:

```tsx
      planId: formData.planId ?? null,
      clientInactive: formData.clientInactive ?? false,
```

f) Campos del formulario. Reemplazar:

```tsx
                        <FieldError>{formErrors.weeklyReportEmails}</FieldError>
                      </Field>
                    </div>
                  </div>
                </FieldGroup>
```

por:

```tsx
                        <FieldError>{formErrors.weeklyReportEmails}</FieldError>
                      </Field>
                    </div>
                  </div>

                  <div className="grid gap-4 sm:grid-cols-2">
                    <Field>
                      <FieldLabel htmlFor="planId">Plan</FieldLabel>
                      <Select
                        value={formData.planId ? String(formData.planId) : "none"}
                        onValueChange={(value) =>
                          setFormData((current) => ({
                            ...current,
                            planId: value === "none" ? null : Number(value),
                          }))
                        }
                      >
                        <SelectTrigger id="planId">
                          <SelectValue placeholder="Selecciona un plan" />
                        </SelectTrigger>
                        <SelectContent>
                          <SelectItem value="none">Sin plan (sin límite)</SelectItem>
                          {plans.map((plan) => (
                            <SelectItem key={plan.planId} value={String(plan.planId)}>
                              {plan.name}{plan.isActive ? "" : " (inactivo)"}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </Field>

                    <Field>
                      <FieldLabel htmlFor="clientInactive">Cliente inactivo</FieldLabel>
                      <div className="flex items-center gap-3 pt-2">
                        <Switch
                          id="clientInactive"
                          checked={formData.clientInactive ?? false}
                          onCheckedChange={(checked) =>
                            setFormData((current) => ({ ...current, clientInactive: checked }))
                          }
                        />
                        <span className="text-sm text-muted-foreground">
                          {formData.clientInactive ? "No podrá emitir comprobantes" : "Activo"}
                        </span>
                      </div>
                    </Field>
                  </div>
                </FieldGroup>
```

g) Badge y plan en la tabla. Reemplazar:

```tsx
                              <p className="font-medium text-foreground">{client.name}</p>
```

por:

```tsx
                              <div className="flex items-center gap-2">
                                <p className="font-medium text-foreground">{client.name}</p>
                                {client.clientInactive && <Badge variant="destructive">Inactivo</Badge>}
                              </div>
                              <p className="text-xs text-muted-foreground">
                                {client.planName ? `Plan: ${client.planName}` : "Sin plan"}
                              </p>
```

- [ ] **Step 4: Lint + build**

Run (en el repo frontend): `pnpm lint` y luego `pnpm build`
Expected: sin errores nuevos de TypeScript/ESLint en los archivos tocados.

- [ ] **Step 5: Verificación manual en navegador**

`pnpm dev`, login como SA, `/clientes`: editar un cliente, asignar plan y marcar "Cliente inactivo", guardar. La fila muestra el badge "Inactivo" y "Plan: <nombre>". Reabrir el diálogo: los valores persisten.

- [ ] **Step 6: Commit (repo frontend)**

```bash
git add types/plan.type.ts types/usage.type.ts types/client.type.ts services/plan.service.ts services/usage.service.ts app/clientes/page.tsx
git commit -m "feat(clientes): plan asignado y switch de cliente inactivo"
```

---

### Task 8: Frontend — página de planes

**Files (frontend repo):**
- Create: `app/configuraciones/planes/page.tsx`
- Modify: `components/sidebar.tsx`

**Interfaces:**
- Consumes: `getPlans`, `createPlan`, `updatePlan`, `deletePlan`, `Plan`, `PlanCreate`, `PlanOverageTier`, `UNLIMITED_DOCUMENTS` (Task 7).

- [ ] **Step 1: Crear la página**

`app/configuraciones/planes/page.tsx`:

```tsx
"use client"

import { FormEvent, useCallback, useEffect, useState } from "react"
import { MainLayout } from "@/components/main-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Switch } from "@/components/ui/switch"
import { Badge } from "@/components/ui/badge"
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
import { Layers, Pencil, Plus, Trash2 } from "lucide-react"
import { createPlan, deletePlan, getPlans, updatePlan } from "@/services/plan.service"
import { Plan, PlanCreate, PlanOverageTier, UNLIMITED_DOCUMENTS } from "@/types/plan.type"
import { useAuthStore } from "@/store/authStore"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

const emptyPlan: PlanCreate = {
  name: "",
  description: "",
  monthlyFee: 0,
  monthlyDocumentLimit: 100,
  isActive: true,
  overageTiers: [
    { fromUnit: 1, toUnit: 100, unitPrice: 6 },
    { fromUnit: 101, toUnit: null, unitPrice: 9 },
  ],
}

const formatLimit = (limit: number) =>
  limit === UNLIMITED_DOCUMENTS ? "Ilimitado" : `${limit.toLocaleString("es-DO")} comprobantes`

const formatTier = (tier: PlanOverageTier) =>
  `${tier.fromUnit}–${tier.toUnit ?? "∞"}: ${currency.format(tier.unitPrice)}`

export default function PlanesPage() {
  const user = useAuthStore((state) => state.user)
  const isSA = user?.userType === 1

  const [plans, setPlans] = useState<Plan[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [editingPlan, setEditingPlan] = useState<Plan | null>(null)
  const [formData, setFormData] = useState<PlanCreate>(emptyPlan)

  const loadPlans = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      setPlans(await getPlans())
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudieron cargar los planes")
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isSA) loadPlans()
  }, [isSA, loadPlans])

  const openCreate = () => {
    setEditingPlan(null)
    setFormData(emptyPlan)
    setIsDialogOpen(true)
  }

  const openEdit = (plan: Plan) => {
    setEditingPlan(plan)
    setFormData({
      name: plan.name,
      description: plan.description ?? "",
      monthlyFee: plan.monthlyFee,
      monthlyDocumentLimit: plan.monthlyDocumentLimit,
      isActive: plan.isActive,
      overageTiers: plan.overageTiers.map((t) => ({ ...t })),
    })
    setIsDialogOpen(true)
  }

  const isUnlimited = formData.monthlyDocumentLimit === UNLIMITED_DOCUMENTS

  const updateTier = (index: number, changes: Partial<PlanOverageTier>) => {
    setFormData((current) => ({
      ...current,
      overageTiers: current.overageTiers.map((tier, i) => (i === index ? { ...tier, ...changes } : tier)),
    }))
  }

  const addTier = () => {
    setFormData((current) => {
      const tiers = [...current.overageTiers]
      const last = tiers[tiers.length - 1]
      if (last && last.toUnit === null) {
        tiers[tiers.length - 1] = { ...last, toUnit: last.fromUnit + 99 }
      }
      const fromUnit = last ? (tiers[tiers.length - 1].toUnit ?? last.fromUnit) + 1 : 1
      tiers.push({ fromUnit, toUnit: null, unitPrice: last?.unitPrice ?? 0 })
      return { ...current, overageTiers: tiers }
    })
  }

  const removeTier = (index: number) => {
    setFormData((current) => ({
      ...current,
      overageTiers: current.overageTiers.filter((_, i) => i !== index),
    }))
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    try {
      setIsSaving(true)
      setError(null)
      const payload: PlanCreate = {
        ...formData,
        name: formData.name.trim(),
        description: formData.description?.trim() || null,
      }
      if (editingPlan) {
        await updatePlan({ ...payload, guidId: editingPlan.guidId })
      } else {
        await createPlan(payload)
      }
      setIsDialogOpen(false)
      await loadPlans()
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo guardar el plan")
    } finally {
      setIsSaving(false)
    }
  }

  const handleDelete = async (plan: Plan) => {
    if (!window.confirm(`¿Eliminar el plan "${plan.name}"? Los clientes asignados dejarán de contar consumo.`)) return
    try {
      setError(null)
      await deletePlan(plan.guidId)
      await loadPlans()
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo eliminar el plan")
    }
  }

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
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-bold text-foreground">Planes</h1>
            <p className="text-muted-foreground">Mensualidad, límite de comprobantes y cobro por excedente.</p>
          </div>
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4" />
            Nuevo plan
          </Button>
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error}
          </div>
        )}

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">Lista de planes</CardTitle>
            <CardDescription>{plans.length} planes</CardDescription>
          </CardHeader>
          <CardContent className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Plan</TableHead>
                  <TableHead>Mensualidad</TableHead>
                  <TableHead>Límite</TableHead>
                  <TableHead>Excedente</TableHead>
                  <TableHead className="text-center">Clientes</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead className="text-right">Acciones</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                      Cargando planes...
                    </TableCell>
                  </TableRow>
                ) : plans.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={7} className="h-24 text-center text-muted-foreground">
                      No hay planes registrados
                    </TableCell>
                  </TableRow>
                ) : (
                  plans.map((plan) => (
                    <TableRow key={plan.guidId}>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Layers className="h-4 w-4 text-primary" />
                          <span className="font-medium">{plan.name}</span>
                        </div>
                        {plan.description && <p className="text-xs text-muted-foreground">{plan.description}</p>}
                      </TableCell>
                      <TableCell>{currency.format(plan.monthlyFee)}</TableCell>
                      <TableCell>{formatLimit(plan.monthlyDocumentLimit)}</TableCell>
                      <TableCell className="text-xs text-muted-foreground">
                        {plan.monthlyDocumentLimit === UNLIMITED_DOCUMENTS || plan.overageTiers.length === 0
                          ? "—"
                          : plan.overageTiers.map((tier) => <div key={tier.fromUnit}>{formatTier(tier)}</div>)}
                      </TableCell>
                      <TableCell className="text-center">{plan.clientsCount}</TableCell>
                      <TableCell>
                        {plan.isActive ? (
                          <Badge className="bg-green-100 text-green-800 hover:bg-green-100">Activo</Badge>
                        ) : (
                          <Badge variant="secondary">Inactivo</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <Button variant="ghost" size="icon" onClick={() => openEdit(plan)} title="Editar">
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button variant="ghost" size="icon" onClick={() => handleDelete(plan)} title="Eliminar">
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
            <form onSubmit={handleSubmit}>
              <DialogHeader>
                <DialogTitle>{editingPlan ? "Editar plan" : "Nuevo plan"}</DialogTitle>
                <DialogDescription>
                  Los comprobantes aceptados por encima del límite se cobran por tramos al cierre del mes.
                </DialogDescription>
              </DialogHeader>

              <FieldGroup className="py-4">
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="plan-name">Nombre</FieldLabel>
                    <Input
                      id="plan-name"
                      required
                      maxLength={100}
                      value={formData.name}
                      onChange={(e) => setFormData((c) => ({ ...c, name: e.target.value }))}
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="plan-fee">Mensualidad (RD$)</FieldLabel>
                    <Input
                      id="plan-fee"
                      type="number"
                      min={0}
                      step="0.01"
                      value={formData.monthlyFee}
                      onChange={(e) => setFormData((c) => ({ ...c, monthlyFee: Number(e.target.value) }))}
                    />
                  </Field>
                </div>

                <Field>
                  <FieldLabel htmlFor="plan-description">Descripción</FieldLabel>
                  <Input
                    id="plan-description"
                    maxLength={300}
                    value={formData.description ?? ""}
                    onChange={(e) => setFormData((c) => ({ ...c, description: e.target.value }))}
                  />
                </Field>

                <div className="grid gap-4 sm:grid-cols-2">
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
                  <div className="flex flex-col gap-3 pt-6">
                    <label className="flex items-center gap-3 text-sm">
                      <Switch
                        checked={isUnlimited}
                        onCheckedChange={(checked) =>
                          setFormData((c) => ({ ...c, monthlyDocumentLimit: checked ? UNLIMITED_DOCUMENTS : 100 }))
                        }
                      />
                      Ilimitado
                    </label>
                    <label className="flex items-center gap-3 text-sm">
                      <Switch
                        checked={formData.isActive}
                        onCheckedChange={(checked) => setFormData((c) => ({ ...c, isActive: checked }))}
                      />
                      Plan activo
                    </label>
                  </div>
                </div>

                {!isUnlimited && (
                  <div className="space-y-3">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-medium">Tramos de excedente</p>
                      <Button type="button" variant="outline" size="sm" onClick={addTier}>
                        <Plus className="mr-1 h-4 w-4" />
                        Agregar tramo
                      </Button>
                    </div>
                    {formData.overageTiers.map((tier, index) => {
                      const isLast = index === formData.overageTiers.length - 1
                      return (
                        <div key={index} className="grid grid-cols-[1fr_1fr_1fr_auto] items-end gap-2">
                          <Field>
                            <FieldLabel>Desde</FieldLabel>
                            <Input
                              type="number"
                              min={1}
                              value={tier.fromUnit}
                              onChange={(e) => updateTier(index, { fromUnit: Number(e.target.value) })}
                            />
                          </Field>
                          <Field>
                            <FieldLabel>Hasta</FieldLabel>
                            <Input
                              type="number"
                              min={tier.fromUnit}
                              placeholder={isLast ? "Sin tope" : ""}
                              value={tier.toUnit ?? ""}
                              onChange={(e) =>
                                updateTier(index, { toUnit: e.target.value === "" ? null : Number(e.target.value) })
                              }
                            />
                          </Field>
                          <Field>
                            <FieldLabel>Precio (RD$)</FieldLabel>
                            <Input
                              type="number"
                              min={0}
                              step="0.01"
                              value={tier.unitPrice}
                              onChange={(e) => updateTier(index, { unitPrice: Number(e.target.value) })}
                            />
                          </Field>
                          <Button type="button" variant="ghost" size="icon" onClick={() => removeTier(index)}>
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        </div>
                      )
                    })}
                    <p className="text-xs text-muted-foreground">
                      Los tramos cuentan unidades de excedente: el primero inicia en 1 y cada uno continúa donde terminó
                      el anterior. Deja "Hasta" vacío en el último para no tener tope.
                    </p>
                  </div>
                )}
              </FieldGroup>

              <DialogFooter>
                <Button type="button" variant="outline" onClick={() => setIsDialogOpen(false)} disabled={isSaving}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={isSaving}>
                  {isSaving ? "Guardando..." : "Guardar plan"}
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        </Dialog>
      </div>
    </MainLayout>
  )
}
```

> Los errores de validación del backend llegan como `400` con `message`. Si `fetchHandler` no expone ese `message` en `err.message`, revisar cómo lo hace para `/clientes` y replicarlo; no inventar otro manejo.

- [ ] **Step 2: Enlace en el sidebar**

En `components/sidebar.tsx`, agregar `Layers` y `BarChart3` al import de `lucide-react` (después de `Settings,`):

```tsx
  Layers,
  BarChart3,
```

Reemplazar el arreglo `navigation`:

```tsx
const navigation = [
  { name: "Dashboard", href: "/", icon: LayoutDashboard },
  { name: "Certificación", href: "/certificacion", icon: Award },
  { name: "Clientes", href: "/clientes", icon: Users },
  { name: "Facturas", href: "/facturas", icon: FileText },
  { name: "Consumo", href: "/consumo", icon: BarChart3 },
  { name: "Planes", href: "/configuraciones/planes", icon: Layers },
  { name: "Configuraciones", href: "/configuraciones", icon: Settings },
]

const saOnlyItems = ["Certificación", "Consumo", "Planes"]
```

Y el filtro:

```tsx
  const filteredNavigation = navigation.filter((item) => {
    if (saOnlyItems.includes(item.name)) {
      return isSA
    }
    return true
  })
```

- [ ] **Step 3: Lint + build**

Run: `pnpm lint` y luego `pnpm build`
Expected: sin errores nuevos.

- [ ] **Step 4: Verificación manual**

`/configuraciones/planes` como SA: crear "Plan Pro" (RD$3,000, 500 comprobantes, tramos 1–100 RD$6 y 101–∞ RD$9). Crear "Plan Ilimitado" (RD$6,300, switch Ilimitado): la columna Excedente muestra "—". Editar un tramo dejando un hueco (p. ej. 1–100 y 102–∞): se muestra el error del backend `El tramo 2 debe iniciar en 101.`. Con un usuario no SA, los enlaces "Planes" y "Consumo" no aparecen.

- [ ] **Step 5: Commit (repo frontend)**

```bash
git add app/configuraciones/planes/page.tsx components/sidebar.tsx
git commit -m "feat(planes): administración de planes y tramos de excedente"
```

---

### Task 9: Frontend — página de consumo mensual

**Files (frontend repo):**
- Create: `app/consumo/page.tsx`

**Interfaces:**
- Consumes: `getMonthlyUsage`, `ClientMonthlyUsage` (Task 7), `UNLIMITED_DOCUMENTS` (Task 7).

- [ ] **Step 1: Crear la página**

`app/consumo/page.tsx`:

```tsx
"use client"

import { Fragment, useCallback, useEffect, useMemo, useState } from "react"
import { MainLayout } from "@/components/main-layout"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { ChevronDown, ChevronRight, RefreshCw } from "lucide-react"
import { getMonthlyUsage } from "@/services/usage.service"
import { ClientMonthlyUsage } from "@/types/usage.type"
import { UNLIMITED_DOCUMENTS } from "@/types/plan.type"
import { useAuthStore } from "@/store/authStore"

const currency = new Intl.NumberFormat("es-DO", { style: "currency", currency: "DOP" })

const months = [
  "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
  "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre",
]

const currentDrDate = () => {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone: "America/Santo_Domingo",
    year: "numeric",
    month: "numeric",
  }).formatToParts(new Date())
  return {
    year: Number(parts.find((p) => p.type === "year")?.value),
    month: Number(parts.find((p) => p.type === "month")?.value),
  }
}

export default function ConsumoPage() {
  const user = useAuthStore((state) => state.user)
  const isSA = user?.userType === 1

  const initial = useMemo(currentDrDate, [])
  const [year, setYear] = useState(initial.year)
  const [month, setMonth] = useState(initial.month)
  const [rows, setRows] = useState<ClientMonthlyUsage[]>([])
  const [expanded, setExpanded] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const years = useMemo(() => Array.from({ length: 5 }, (_, i) => initial.year - i), [initial.year])

  const load = useCallback(async () => {
    try {
      setIsLoading(true)
      setError(null)
      setRows(await getMonthlyUsage(year, month))
    } catch (err) {
      setError(err instanceof Error ? err.message : "No se pudo cargar el consumo")
    } finally {
      setIsLoading(false)
    }
  }, [year, month])

  useEffect(() => {
    if (isSA) load()
  }, [isSA, load])

  const totals = useMemo(
    () => ({
      documents: rows.reduce((sum, r) => sum + r.acceptedDocuments, 0),
      overage: rows.reduce((sum, r) => sum + r.overageAmount, 0),
      total: rows.reduce((sum, r) => sum + r.total, 0),
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
            <h1 className="text-2xl font-bold text-foreground">Consumo mensual</h1>
            <p className="text-muted-foreground">
              Comprobantes aceptados por cliente con plan activo y monto a facturar.
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Select value={String(month)} onValueChange={(v) => setMonth(Number(v))}>
              <SelectTrigger className="w-40">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {months.map((name, index) => (
                  <SelectItem key={name} value={String(index + 1)}>
                    {name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <Select value={String(year)} onValueChange={(v) => setYear(Number(v))}>
              <SelectTrigger className="w-28">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {years.map((y) => (
                  <SelectItem key={y} value={String(y)}>
                    {y}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
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

        <div className="grid gap-4 sm:grid-cols-3">
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Comprobantes aceptados</p>
              <p className="text-2xl font-bold">{totals.documents.toLocaleString("es-DO")}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Excedente</p>
              <p className="text-2xl font-bold">{currency.format(totals.overage)}</p>
            </CardContent>
          </Card>
          <Card>
            <CardContent className="pt-6">
              <p className="text-sm text-muted-foreground">Total a facturar</p>
              <p className="text-2xl font-bold">{currency.format(totals.total)}</p>
            </CardContent>
          </Card>
        </div>

        <Card>
          <CardHeader>
            <CardTitle className="text-lg">
              {months[month - 1]} {year}
            </CardTitle>
            <CardDescription>{rows.length} clientes con consumo registrado</CardDescription>
          </CardHeader>
          <CardContent className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-8" />
                  <TableHead>Cliente</TableHead>
                  <TableHead>Plan</TableHead>
                  <TableHead className="text-right">Aceptados</TableHead>
                  <TableHead className="text-right">Límite</TableHead>
                  <TableHead className="text-right">Excedente</TableHead>
                  <TableHead className="text-right">Mensualidad</TableHead>
                  <TableHead className="text-right">Cargo excedente</TableHead>
                  <TableHead className="text-right">Total</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-24 text-center text-muted-foreground">
                      Cargando consumo...
                    </TableCell>
                  </TableRow>
                ) : rows.length === 0 ? (
                  <TableRow>
                    <TableCell colSpan={9} className="h-24 text-center text-muted-foreground">
                      No hay consumo registrado para este mes
                    </TableCell>
                  </TableRow>
                ) : (
                  rows.map((row) => {
                    const isOpen = expanded === row.clientGuidId
                    const hasTierDetail = row.overageDocuments > 0 && row.tiers.length > 0
                    return (
                      <Fragment key={row.clientGuidId}>
                        <TableRow>
                          <TableCell>
                            {hasTierDetail && (
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-6 w-6"
                                onClick={() => setExpanded(isOpen ? null : row.clientGuidId)}
                              >
                                {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
                              </Button>
                            )}
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center gap-2">
                              <span className="font-medium">{row.clientName}</span>
                              {row.clientInactive && <Badge variant="destructive">Inactivo</Badge>}
                            </div>
                            <code className="text-xs text-muted-foreground">{row.clientRnc}</code>
                          </TableCell>
                          <TableCell>{row.planName}</TableCell>
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
                        </TableRow>
                        {isOpen &&
                          row.tiers
                            .filter((tier) => tier.units > 0)
                            .map((tier) => (
                              <TableRow key={`${row.clientGuidId}-${tier.fromUnit}`} className="bg-secondary/30">
                                <TableCell />
                                <TableCell colSpan={4} className="text-sm text-muted-foreground">
                                  Tramo {tier.fromUnit}–{tier.toUnit ?? "∞"} · {tier.units.toLocaleString("es-DO")} ×{" "}
                                  {currency.format(tier.unitPrice)}
                                </TableCell>
                                <TableCell className="text-right text-sm">{tier.units.toLocaleString("es-DO")}</TableCell>
                                <TableCell />
                                <TableCell className="text-right text-sm">{currency.format(tier.amount)}</TableCell>
                                <TableCell />
                              </TableRow>
                            ))}
                      </Fragment>
                    )
                  })
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

- [ ] **Step 2: Lint + build**

Run: `pnpm lint` y luego `pnpm build`
Expected: sin errores nuevos.

- [ ] **Step 3: Verificación manual**

`/consumo` como SA con el mes actual: aparece el cliente de prueba de la Task 6. Con el `UPDATE` temporal a 650 aceptados (límite 500): Excedente 150, Cargo excedente RD$1,050.00, Total RD$4,050.00. Al expandir la fila se ven los tramos 1–100 (100 × RD$6.00 = RD$600.00) y 101–∞ (50 × RD$9.00 = RD$450.00). Revertir el UPDATE.

- [ ] **Step 4: Commit (repo frontend)**

```bash
git add app/consumo/page.tsx
git commit -m "feat(consumo): reporte mensual de comprobantes y monto a facturar"
```

---

## Cobertura del spec

| Requisito del spec | Task |
|---|---|
| Plan con mensualidad, límite (`-1` ilimitado) y tramos | 1, 2, 5, 8 |
| Cliente sin plan / plan inactivo no cuenta | 3 |
| Solo aceptados y aceptados condicionales cuentan | 3 |
| Idempotencia (`BillingCountedAtUtc`) | 2, 3 |
| Mes en hora RD | 3, 9 |
| Snapshot del plan en la fila del mes | 2, 3 |
| Fórmula escalonada (150 → RD$1,050) | 1, 6, 9 |
| `ClientInactive` → 403 + mensaje de soporte, sin DGII | 4 |
| `EcfDocumentId` en todas las respuestas | 4 |
| Reporte de consumo mensual (API + UI) | 6, 9 |
| Plan y switch inactivo en el formulario de clientes | 6, 7 |
