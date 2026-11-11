# Cuenta de facturación con pool — Fase 2: cobro, suspensión, recordatorios y API del SA

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que una `BillingAccount` se cobre como se cobra hoy un cliente (mensualidad + excedente del pool), se suspenda por falta de pago bloqueando la emisión de todos sus clientes, reciba recordatorios, y que el SA pueda operarla por API.

**Architecture:** Un `BillingTerms` (condiciones de cobro) permite reutilizar los cálculos puros de `PaymentCalculator` para clientes y cuentas. El flujo de pago, los recordatorios y la suspensión de cuentas son servicios paralelos a los de clientes que comparten las funciones puras (`PaymentRegistrationPolicy`, `PaymentReminderPolicy`, `BillingCalculator`, plantillas de correo); los servicios de clientes casi no se tocan. La emisión se bloquea con una consulta extra a la cuenta del cliente. Un `BillingAccountController` solo para SA expone el alta, la asignación de clientes y los pagos.

**Tech Stack:** .NET 10, EF Core + Npgsql (PostgreSQL), xUnit + Moq.

Spec: `Docs/superpowers/specs/2026-11-11-cuenta-facturacion-pool-design.md`. Fase anterior: `Docs/superpowers/plans/2026-11-11-cuenta-facturacion-fase1-datos-consumo.md`.

## Global Constraints

- **Nunca apliques la migración ni corras la API con la configuración por defecto:** el `DefaultConnection` activo de `ZynstormECFPlatform.Web.Api/appsettings.Development.json` apunta a **producción**. Cualquier uso de base de datos se hace con `ConnectionStrings__DefaultConnection` apuntando a `zynstorm_ecf_platform_dev2_db`, y solo con permiso del usuario (Task 10).
- `dotnet ef migrations add` no conecta a la base; es seguro con la configuración por defecto. `dotnet ef database update` **no** lo es.
- Cambios de esquema aditivos, salvo `ClientPayment.ClientId`, que pasa a nullable (spec §1). Los pagos y clientes existentes no cambian.
- Un cliente con `BillingAccountId` factura solo contra su cuenta: queda fuera de los pagos, recordatorios y suspensión individuales (Task 6).
- La cuenta suspendida bloquea la emisión de todos sus clientes con el mismo resultado `ClientInactive = true` (HTTP 403) que usa hoy la suspensión de un cliente.
- Una cuenta solo acepta planes `Documents` activos; su dueño es un usuario con rol `Admin`; un usuario es dueño de una sola cuenta.
- Solo para SA (`[Authorize(Roles = "SA")]`) en `BillingAccountController`. Los permisos del Admin revendedor y las validaciones de `UserController` son la fase 3; las pantallas, la fase 4.
- Los servicios que dependen de `IQueryable` de EF (`BillingAccountPaymentService`, `BillingAccountReminderService`, `BillingAccountController`) no se prueban con Moq, porque `ToListAsync` exige el proveedor de EF. Su lógica está en funciones puras con pruebas; el flujo completo se verifica contra PostgreSQL en la Task 10.
- Mensajes de commit en español, con las líneas `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` y `Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V` al final. Cada comando de commit imprime la rama con `git branch --show-current`; confirma la rama con el usuario antes del Task 1 (el repo estaba en `dev`).

## Estructura de archivos

| Archivo | Acción | Responsabilidad |
|---|---|---|
| `ZynstormECFPlatform.Services/Billing/BillingTerms.cs` | Crear | Condiciones de cobro comunes a cliente y cuenta |
| `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs` | Modificar | Sobrecargas con `BillingTerms` |
| `ZynstormECFPlatform.Core/Entities/ClientPayment.cs`, `ClientPaymentItem.cs`, `BillingAccount.cs` | Modificar | Recibos de cuenta |
| `ZynstormECFPlatform.Data/StorageContext.cs` | Modificar | Mapeo de los recibos de cuenta |
| `ZynstormECFPlatform.Data/Migrations/*_AddBillingAccountPayments.cs` | Generar | Migración |
| `ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountMonthlyUsageService.cs`, `ZynstormECFPlatform.Data.Services/BillingAccountMonthlyUsageService.cs` | Crear | Repositorio del consumo del pool |
| `ZynstormECFPlatform.Dtos/BillingAccountPaymentDtos.cs`, `ClientPaymentRegistrationDtos.cs` | Crear/Modificar | DTOs de pago de cuenta, `IsAccount` en el recibo |
| `ZynstormECFPlatform.Services/Billing/PaymentReceiptMapper.cs` | Crear | Recibo → DTO (movido de `ClientPaymentRegistrationService`) |
| `ZynstormECFPlatform.Services/Billing/PlanFeePreviewBuilder.cs` | Crear | Vista previa de la mensualidad |
| `ZynstormECFPlatform.Services/Billing/PendingOverageCalculator.cs` | Crear | Excedentes pendientes de cuentas |
| `ZynstormECFPlatform.Services/Billing/AccountPaymentBuilder.cs` | Crear | Arma el recibo de una cuenta |
| `ZynstormECFPlatform.Services/Billing/IBillingAccountPaymentService.cs`, `BillingAccountPaymentService.cs` | Crear | Vista previa, registro, historial y pendientes de cuenta |
| `ZynstormECFPlatform.Services/Billing/BillingAccountAccess.cs` | Crear | Regla "cuenta suspendida" |
| `ZynstormECFPlatform.Services/Billing/ClientStatusEvaluator.cs` | Modificar | Estado del cliente según su cuenta |
| `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs` | Modificar | Compuerta de emisión por cuenta suspendida |
| `ZynstormECFPlatform.Services/Billing/BillingScope.cs` | Crear | Filtro de clientes con cobro individual |
| `ZynstormECFPlatform.Services/Billing/IBillingAccountReminderService.cs`, `BillingAccountReminderService.cs` | Crear | Recordatorios y suspensión de cuentas |
| `ZynstormECFPlatform.Services/Jobs/PaymentReminderJob.cs`, `PaymentSuspensionJob.cs` | Modificar | Incluir cuentas |
| `ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs` | Crear | Validaciones de cuentas |
| `ZynstormECFPlatform.Dtos/BillingAccountDtos.cs` | Crear | DTOs de la API de cuentas |
| `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs` | Crear | API del SA |
| `ZynstormECFPlatform.Tests/Billing/*` | Crear/Modificar | Pruebas |

Todos los comandos se corren desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`.

---

### Task 1: `BillingTerms` y `PaymentCalculator`

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/BillingTerms.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/BillingTermsTests.cs`

**Interfaces:**
- Produces:
  - `BillingTerms(Plan Plan, decimal? CustomMonthlyFee, DateTime? OfferStartDate, int PaidMonths, decimal PrepaymentDiscountPercent, DateTime? NextPaymentDate)` con `BillingTerms.For(Client)` y `BillingTerms.For(BillingAccount)`; ambos lanzan `ArgumentException` si el plan no está cargado.
  - `PaymentCalculator.GetMonthlyFee(BillingTerms)` → `decimal` y `PaymentCalculator.CalculateCycle(BillingTerms, DateTime)` → `PaymentCalculationResult`. Las sobrecargas con `Client` se conservan y delegan.

- [ ] **Step 1: Escribir las pruebas (fallan: `BillingTerms` no existe)**

`ZynstormECFPlatform.Tests/Billing/BillingTermsTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingTermsTests
{
    private static Plan MakePlan(decimal fee = 3000m, decimal? offerPrice = null, int? offerMonths = null) => new()
    {
        PlanId = 5,
        Name = "Pool 1000",
        MonthlyFee = fee,
        MonthlyDocumentLimit = 1000,
        PlanTypeId = 1,
        OfferPrice = offerPrice,
        OfferMonths = offerMonths
    };

    [Fact]
    public void Account_WithoutCustomFee_UsesThePlanFee()
    {
        var account = new BillingAccount { Plan = MakePlan(3000m) };

        Assert.Equal(3000m, PaymentCalculator.GetMonthlyFee(BillingTerms.For(account)));
    }

    [Fact]
    public void Account_CustomFee_ReplacesThePlanFee()
    {
        var account = new BillingAccount { Plan = MakePlan(3000m), CustomMonthlyFee = 2500m };

        Assert.Equal(2500m, PaymentCalculator.GetMonthlyFee(BillingTerms.For(account)));
    }

    [Fact]
    public void Account_CycleEqualsTheClientCycle_ForTheSameTerms()
    {
        var plan = MakePlan(3000m);
        var next = new DateTime(2026, 11, 15);
        var client = new Client { Plan = plan, PaidMonths = 3, PrepaymentDiscountPercent = 10m, NextPaymentDate = next };
        var account = new BillingAccount { Plan = plan, PaidMonths = 3, PrepaymentDiscountPercent = 10m, NextPaymentDate = next };

        var fallback = new DateTime(2026, 11, 2);

        Assert.Equal(
            PaymentCalculator.CalculateCycle(client, fallback),
            PaymentCalculator.CalculateCycle(BillingTerms.For(account), fallback));
    }

    [Fact]
    public void Account_Cycle_AppliesThePrepaymentDiscount()
    {
        var account = new BillingAccount { Plan = MakePlan(3000m), PaidMonths = 3, PrepaymentDiscountPercent = 10m };

        var result = PaymentCalculator.CalculateCycle(BillingTerms.For(account), new DateTime(2026, 11, 2));

        Assert.Equal(9000m, result.GrossAmount);
        Assert.Equal(900m, result.DiscountAmount);
        Assert.Equal(8100m, result.Total);
    }

    [Fact]
    public void Account_Cycle_ChargesOfferMonthsAtTheOfferPrice()
    {
        var account = new BillingAccount
        {
            Plan = MakePlan(3000m, offerPrice: 2000m, offerMonths: 2),
            OfferStartDate = new DateTime(2026, 11, 1),
            NextPaymentDate = new DateTime(2026, 11, 1),
            PaidMonths = 3
        };

        var result = PaymentCalculator.CalculateCycle(BillingTerms.For(account), new DateTime(2026, 11, 1));

        Assert.Equal(2, result.OfferMonthsCovered);
        Assert.Equal(7000m, result.GrossAmount);
    }

    [Fact]
    public void Account_WithoutPlanLoaded_Throws()
    {
        var account = new BillingAccount { Plan = null! };

        Assert.Throws<ArgumentException>(() => BillingTerms.For(account));
    }

    [Fact]
    public void Client_WithoutPlanLoaded_StillThrows()
    {
        Assert.Throws<ArgumentException>(() => PaymentCalculator.GetMonthlyFee(new Client()));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingTermsTests" 2>&1 | grep -E "error|Passed!|Failed!" | sort -u | head`
Expected: error de compilación `The type or namespace name 'BillingTerms' could not be found`.

- [ ] **Step 3: Crear `BillingTerms`**

`ZynstormECFPlatform.Services/Billing/BillingTerms.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Condiciones de cobro de quien paga un plan: un cliente suelto o una cuenta de facturación.</summary>
public sealed record BillingTerms(
    Plan Plan,
    decimal? CustomMonthlyFee,
    DateTime? OfferStartDate,
    int PaidMonths,
    decimal PrepaymentDiscountPercent,
    DateTime? NextPaymentDate)
{
    public static BillingTerms For(Client client) => new(
        client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client)),
        client.CustomMonthlyFee,
        client.OfferStartDate,
        client.PaidMonths,
        client.PrepaymentDiscountPercent,
        client.NextPaymentDate);

    public static BillingTerms For(BillingAccount account) => new(
        account.Plan ?? throw new ArgumentException("La cuenta no tiene el plan cargado.", nameof(account)),
        account.CustomMonthlyFee,
        account.OfferStartDate,
        account.PaidMonths,
        account.PrepaymentDiscountPercent,
        account.NextPaymentDate);
}
```

- [ ] **Step 4: Generalizar `PaymentCalculator`**

En `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs`, reemplazar `GetMonthlyFee(Client)`:

```csharp
    public static decimal GetMonthlyFee(Client client)
    {
        var plan = client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client));
        return client.CustomMonthlyFee ?? plan.MonthlyFee;
    }
```

por:

```csharp
    public static decimal GetMonthlyFee(Client client) => GetMonthlyFee(BillingTerms.For(client));

    public static decimal GetMonthlyFee(BillingTerms terms) => terms.CustomMonthlyFee ?? terms.Plan.MonthlyFee;
```

y reemplazar `CalculateCycle(Client, DateTime)`:

```csharp
    public static PaymentCalculationResult CalculateCycle(Client client, DateTime fallbackPeriodStart)
    {
        var monthlyFee = GetMonthlyFee(client);
        var offer = client.CustomMonthlyFee.HasValue
            ? null
            : OfferPricing.TermsFor(client.Plan!.OfferPrice, client.Plan.OfferMonths, client.OfferStartDate);

        return Calculate(
            monthlyFee,
            client.PaidMonths,
            client.PrepaymentDiscountPercent,
            offer,
            client.NextPaymentDate?.Date ?? fallbackPeriodStart.Date);
    }
```

por:

```csharp
    public static PaymentCalculationResult CalculateCycle(Client client, DateTime fallbackPeriodStart) =>
        CalculateCycle(BillingTerms.For(client), fallbackPeriodStart);

    public static PaymentCalculationResult CalculateCycle(BillingTerms terms, DateTime fallbackPeriodStart)
    {
        var monthlyFee = GetMonthlyFee(terms);
        var offer = terms.CustomMonthlyFee.HasValue
            ? null
            : OfferPricing.TermsFor(terms.Plan.OfferPrice, terms.Plan.OfferMonths, terms.OfferStartDate);

        return Calculate(
            monthlyFee,
            terms.PaidMonths,
            terms.PrepaymentDiscountPercent,
            offer,
            terms.NextPaymentDate?.Date ?? fallbackPeriodStart.Date);
    }
```

- [ ] **Step 5: Correr las pruebas de cobro (nuevas y existentes)**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingTermsTests|FullyQualifiedName~PaymentCalculator|FullyQualifiedName~CustomPriceTests|FullyQualifiedName~OfferPricingTests" 2>&1 | grep -E "error|Passed!|Failed" | sort -u`
Expected: `Passed!  - Failed: 0` (las 7 nuevas más las existentes de `PaymentCalculatorTests`, `PaymentCalculatorOfferTests`, `CustomPriceTests`, `OfferPricingTests`).

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services/Billing/BillingTerms.cs ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs ZynstormECFPlatform.Tests/Billing/BillingTermsTests.cs && git commit -q -m "$(cat <<'EOF'
Calcular el ciclo de pago también para cuentas de facturación

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 2: Esquema de los pagos de cuenta

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/ClientPayment.cs`, `ClientPaymentItem.cs`, `BillingAccount.cs`
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs`
- Create: `ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountMonthlyUsageService.cs`, `ZynstormECFPlatform.Data.Services/BillingAccountMonthlyUsageService.cs`
- Create (generado): `ZynstormECFPlatform.Data/Migrations/*_AddBillingAccountPayments.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs`

**Interfaces:**
- Produces: `ClientPayment.ClientId` (`int?`), `ClientPayment.BillingAccountId` (`int?`), `ClientPayment.BillingAccount`; `ClientPaymentItem.BillingAccountMonthlyUsageId` (`int?`) y `.BillingAccountMonthlyUsage`; `BillingAccount.Payments`; `IBillingAccountMonthlyUsageService : IRepository<BillingAccountMonthlyUsage>`.

- [ ] **Step 1: Escribir las pruebas del modelo (fallan: las propiedades no existen)**

En `ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs`, reemplazar el final de la clase:

```csharp
        Assert.Equal(3, entity.FindProperty(nameof(BillingAccount.PaymentGraceDays))!.GetDefaultValue());
    }
}
```

por:

```csharp
        Assert.Equal(3, entity.FindProperty(nameof(BillingAccount.PaymentGraceDays))!.GetDefaultValue());
    }

    [Fact]
    public void ClientPayment_BelongsToExactlyOneOfClientOrAccount()
    {
        var entity = BuildModel().FindEntityType(typeof(ClientPayment))!;

        Assert.True(entity.FindProperty(nameof(ClientPayment.ClientId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(ClientPayment.BillingAccountId))!.IsNullable);
        Assert.Contains(entity.GetCheckConstraints(), c => c.Name == "CK_ClientPayment_ClientOrAccount");
    }

    [Fact]
    public void ClientPaymentItem_AccountOverageCanBePaidOnlyOnce()
    {
        var entity = BuildModel().FindEntityType(typeof(ClientPaymentItem))!;

        var index = Assert.Single(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["BillingAccountMonthlyUsageId"]));

        Assert.True(index.IsUnique);
        Assert.Contains("\"ItemType\" = 2", index.GetFilter());
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountModelTests" 2>&1 | grep -E "error|Passed!|Failed!" | sort -u | head -5`
Expected: error de compilación (`'ClientPayment' does not contain a definition for 'BillingAccountId'`).

- [ ] **Step 3: Cambiar las entidades**

Reemplazar el contenido de `ZynstormECFPlatform.Core/Entities/ClientPayment.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Recibo de un pago recibido de un cliente suelto o de una cuenta de facturación (exactamente uno de los dos).
/// Sus líneas indican qué cubre.
/// </summary>
public partial class ClientPayment : BaseEntity
{
    public int ClientPaymentId { get; set; }

    /// <summary>Cliente suelto que pagó. Null cuando el pago es de una cuenta.</summary>
    public int? ClientId { get; set; }

    /// <summary>Cuenta de facturación que pagó. Null cuando el pago es de un cliente suelto.</summary>
    public int? BillingAccountId { get; set; }

    /// <summary>Fecha calendario en que se recibió el pago.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary><see cref="Enums.PaymentMethod"/>.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string? RegisteredByUserId { get; set; }

    public virtual Client? Client { get; set; }

    public virtual BillingAccount? BillingAccount { get; set; }

    public virtual ICollection<ClientPaymentItem> Items { get; set; } = [];
}
```

En `ClientPaymentItem.cs`, reemplazar:

```csharp
    public int? ClientMonthlyUsageId { get; set; }
```

por:

```csharp
    public int? ClientMonthlyUsageId { get; set; }

    /// <summary>Mes de consumo de la cuenta que se está pagando. Solo en líneas de excedente de una cuenta.</summary>
    public int? BillingAccountMonthlyUsageId { get; set; }
```

y reemplazar:

```csharp
    public virtual ClientMonthlyUsage? ClientMonthlyUsage { get; set; }
```

por:

```csharp
    public virtual ClientMonthlyUsage? ClientMonthlyUsage { get; set; }

    public virtual BillingAccountMonthlyUsage? BillingAccountMonthlyUsage { get; set; }
```

En `BillingAccount.cs`, reemplazar:

```csharp
    public virtual ICollection<BillingAccountMonthlyUsage> MonthlyUsages { get; set; } = [];
```

por:

```csharp
    public virtual ICollection<BillingAccountMonthlyUsage> MonthlyUsages { get; set; } = [];

    public virtual ICollection<ClientPayment> Payments { get; set; } = [];
```

- [ ] **Step 4: Mapear en `StorageContext`**

En el bloque `modelBuilder.Entity<ClientPayment>`, reemplazar:

```csharp
                  .HasConstraintName("FK_ClientPayment_Client");
        });
```

por:

```csharp
                  .HasConstraintName("FK_ClientPayment_Client");

            entity.HasOne(d => d.BillingAccount)
                  .WithMany(p => p.Payments)
                  .HasForeignKey(d => d.BillingAccountId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientPayment_BillingAccount");

            entity.HasIndex(e => new { e.BillingAccountId, e.PaymentDate })
                  .HasDatabaseName("IX_ClientPayment_BillingAccountId_PaymentDate");

            // Un recibo es de un cliente suelto o de una cuenta, nunca de ambos ni de ninguno.
            entity.ToTable(t => t.HasCheckConstraint(
                "CK_ClientPayment_ClientOrAccount",
                "(\"ClientId\" IS NULL) <> (\"BillingAccountId\" IS NULL)"));
        });
```

En el bloque `modelBuilder.Entity<ClientPaymentItem>`, reemplazar:

```csharp
                  .HasConstraintName("FK_ClientPaymentItem_ClientMonthlyUsage");
        });
```

por:

```csharp
                  .HasConstraintName("FK_ClientPaymentItem_ClientMonthlyUsage");

            entity.HasOne(d => d.BillingAccountMonthlyUsage)
                  .WithMany()
                  .HasForeignKey(d => d.BillingAccountMonthlyUsageId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_ClientPaymentItem_BillingAccountMonthlyUsage");

            // Un mes de excedente de una cuenta se paga una sola vez.
            entity.HasIndex(e => e.BillingAccountMonthlyUsageId)
                  .IsUnique()
                  .HasFilter("\"ItemType\" = 2 AND \"BillingAccountMonthlyUsageId\" IS NOT NULL AND NOT \"IsDeleted\"")
                  .HasDatabaseName("IX_ClientPaymentItem_AccountOverage_Usage");
        });
```

- [ ] **Step 5: Servicio de datos del consumo del pool**

`ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountMonthlyUsageService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IBillingAccountMonthlyUsageService : IRepository<BillingAccountMonthlyUsage>
{
}
```

`ZynstormECFPlatform.Data.Services/BillingAccountMonthlyUsageService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BillingAccountMonthlyUsageService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BillingAccountMonthlyUsage>(context, sqlGenerator), IBillingAccountMonthlyUsageService
{
}
```

- [ ] **Step 6: Compilar y correr las pruebas del modelo**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head` y luego `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountModelTests" 2>&1 | grep -E "error|Passed!|Failed!" | sort -u`
Expected: `Build succeeded.` y `Passed!  - Failed: 0, Passed: 6`.

Si el build falla porque algún código usa `ClientPayment.ClientId` como `int` (por ejemplo `clientIds.Contains(p.ClientId)`), corrígelo con `p.ClientId!.Value` o `p.ClientId == x`; hoy solo `ClientPaymentRegistrationService` toca ese campo y compara con `==`.

- [ ] **Step 7: Generar y revisar la migración**

Run: `dotnet ef migrations add AddBillingAccountPayments --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api 2>&1 | grep -v "warning\|NETSDK" | tail -4`
Expected: `Done.`

Run: `grep -n "AlterColumn\|AddColumn\|CreateIndex\|AddForeignKey\|DropForeignKey\|AddCheckConstraint\|DropTable\|DropColumn\|CreateTable" ZynstormECFPlatform.Data/Migrations/*_AddBillingAccountPayments.cs | head -30`
Expected en `Up`: `AlterColumn` de `ClientPayment.ClientId` a nullable (EF puede soltar y recrear `FK_ClientPayment_Client`), `AddColumn` de `ClientPayment.BillingAccountId` y `ClientPaymentItem.BillingAccountMonthlyUsageId`, los dos `CreateIndex` (`IX_ClientPayment_BillingAccountId_PaymentDate`, `IX_ClientPaymentItem_AccountOverage_Usage` con filtro), los `AddForeignKey` nuevos y `AddCheckConstraint` `CK_ClientPayment_ClientOrAccount`. Si aparece un `DropTable`, `DropColumn` o cambios en tablas ajenas, **detente** y repórtalo al usuario.

- [ ] **Step 8: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Core/Entities ZynstormECFPlatform.Data ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountMonthlyUsageService.cs ZynstormECFPlatform.Data.Services/BillingAccountMonthlyUsageService.cs ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs && git commit -q -m "$(cat <<'EOF'
Esquema de recibos de pago para cuentas de facturación

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 3: Piezas puras del pago de cuenta

**Files:**
- Create: `ZynstormECFPlatform.Dtos/BillingAccountPaymentDtos.cs`
- Modify: `ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs` (`IsAccount` en el recibo)
- Create: `ZynstormECFPlatform.Services/Billing/PaymentReceiptMapper.cs`, `PlanFeePreviewBuilder.cs`, `PendingOverageCalculator.cs`, `AccountPaymentBuilder.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs` (usar el mapper y el builder)
- Test: `ZynstormECFPlatform.Tests/Billing/PaymentReceiptMapperTests.cs`, `PendingOverageCalculatorTests.cs`, `AccountPaymentBuilderTests.cs`

**Interfaces:**
- Consumes: `BillingTerms`, `PaymentCalculator` (Task 1); entidades del Task 2.
- Produces:
  - `AccountPendingOverageDto` (`BillingAccountMonthlyUsageId`, `Year`, `Month`, `PlanId`, `PlanName`, `AcceptedDocuments`, `MonthlyDocumentLimit`, `OverageDocuments`, `Amount`) y `AccountPaymentPreviewDto` (`AccountGuidId`, `AccountName`, `PaymentSuspended`, `PlanFee`, `PendingOverages`).
  - `PaymentReceiptDto.IsAccount` (`bool`).
  - `PaymentReceiptMapper.ToReceipt(ClientPayment payment, string payerGuid, string payerName, bool isAccount = false)` → `PaymentReceiptDto`.
  - `PlanFeePreviewBuilder.Build(BillingTerms terms, DateTime today)` → `PlanFeePreviewDto`.
  - `PendingOverageCalculator.ForAccounts(IEnumerable<BillingAccountMonthlyUsage> usages, ISet<int> paidUsageIds, IEnumerable<PlanOverageTier> tiers, DateTime today)` → `Dictionary<int, List<AccountPendingOverageDto>>` (clave = `BillingAccountId`).
  - `AccountPaymentBuilder.Build(BillingAccount account, RegisterPaymentRequestDto request, IReadOnlyCollection<AccountPendingOverageDto> selectedOverages, DateTime paymentDate, string? userId)` → `AccountPaymentDraft(ClientPayment Payment, DateTime? NewNextPaymentDate)`. No modifica la cuenta.

- [ ] **Step 1: DTOs**

`ZynstormECFPlatform.Dtos/BillingAccountPaymentDtos.cs`:

```csharp
namespace ZynstormECFPlatform.Dtos;

/// <summary>Mes cerrado de una cuenta con excedente del pool sin pagar.</summary>
public class AccountPendingOverageDto
{
    public int BillingAccountMonthlyUsageId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int OverageDocuments { get; set; }
    public decimal Amount { get; set; }
}

public class AccountPaymentPreviewDto
{
    public string AccountGuidId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool PaymentSuspended { get; set; }

    /// <summary>Null si el plan no tiene mensualidad.</summary>
    public PlanFeePreviewDto? PlanFee { get; set; }

    public List<AccountPendingOverageDto> PendingOverages { get; set; } = [];
}
```

En `ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs`, reemplazar:

```csharp
    public string ClientName { get; set; } = string.Empty;

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }
```

por:

```csharp
    public string ClientName { get; set; } = string.Empty;

    /// <summary>True si el recibo es de una cuenta de facturación; entonces <c>ClientGuidId</c> y <c>ClientName</c> son los de la cuenta.</summary>
    public bool IsAccount { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }
```

- [ ] **Step 2: Escribir las pruebas (fallan: las clases no existen)**

`ZynstormECFPlatform.Tests/Billing/PaymentReceiptMapperTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentReceiptMapperTests
{
    private static ClientPayment SamplePayment() => new()
    {
        ClientPaymentId = 12,
        PaymentDate = new DateTime(2026, 11, 2),
        PaymentMethod = 2,
        TotalAmount = 3300m,
        Items =
        {
            new ClientPaymentItem
            {
                ItemType = 1, Amount = 3000m, PlanName = "Pool 1000", MonthsCovered = 1,
                NewNextPaymentDate = new DateTime(2026, 11, 15)
            },
            new ClientPaymentItem
            {
                ItemType = 2, Amount = 300m, PlanName = "Pool 1000", Year = 2026, Month = 10, OverageDocuments = 50
            }
        }
    };

    [Fact]
    public void ToReceipt_ForAnAccount_FillsThePayerAndTheAccountFlag()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "acct-guid", "Mi Cuenta", isAccount: true);

        Assert.Equal("REC-000012", receipt.ReceiptNumber);
        Assert.True(receipt.IsAccount);
        Assert.Equal("acct-guid", receipt.ClientGuidId);
        Assert.Equal("Mi Cuenta", receipt.ClientName);
        Assert.Equal(3300m, receipt.TotalAmount);
        Assert.Equal(new DateTime(2026, 11, 15), receipt.NextPaymentDate);
    }

    [Fact]
    public void ToReceipt_ForAClient_IsNotMarkedAsAccount()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "client-guid", "Cliente");

        Assert.False(receipt.IsAccount);
    }

    [Fact]
    public void ToReceipt_DescribesTheFeeAndTheOverage()
    {
        var receipt = PaymentReceiptMapper.ToReceipt(SamplePayment(), "g", "n");

        Assert.Equal("Mensualidad Pool 1000 (1 mes)", receipt.Items[0].Description);
        Assert.Equal("Excedente Octubre 2026 (50 comprobantes)", receipt.Items[1].Description);
    }
}
```

`ZynstormECFPlatform.Tests/Billing/PendingOverageCalculatorTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PendingOverageCalculatorTests
{
    private static readonly DateTime Today = new(2026, 11, 11);

    private static readonly PlanOverageTier[] Tiers =
    [
        new() { PlanId = 5, FromUnit = 1, ToUnit = 100, UnitPrice = 6.00m },
        new() { PlanId = 5, FromUnit = 101, ToUnit = null, UnitPrice = 9.00m }
    ];

    private static BillingAccountMonthlyUsage Usage(int id, int year, int month, int accepted, int limit = 500, int accountId = 3) => new()
    {
        BillingAccountMonthlyUsageId = id,
        BillingAccountId = accountId,
        Year = year,
        Month = month,
        AcceptedDocuments = accepted,
        PlanId = 5,
        PlanName = "Pool 500",
        MonthlyFee = 3000m,
        MonthlyDocumentLimit = limit
    };

    private static readonly HashSet<int> NoPaid = [];

    [Fact]
    public void ClosedMonthOverTheLimit_ChargesThePoolOverageByTiers()
    {
        var result = PendingOverageCalculator.ForAccounts([Usage(1, 2026, 10, 650)], NoPaid, Tiers, Today);

        var pending = Assert.Single(result[3]);
        Assert.Equal(150, pending.OverageDocuments);
        Assert.Equal(1050m, pending.Amount);
        Assert.Equal(1, pending.BillingAccountMonthlyUsageId);
        Assert.Equal(650, pending.AcceptedDocuments);
    }

    [Fact]
    public void CurrentMonth_IsNotPayableYet()
    {
        var result = PendingOverageCalculator.ForAccounts([Usage(1, 2026, 11, 900)], NoPaid, Tiers, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void PaidMonth_IsExcluded()
    {
        var result = PendingOverageCalculator.ForAccounts([Usage(1, 2026, 10, 650)], new HashSet<int> { 1 }, Tiers, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void MonthWithinTheLimit_HasNoPendingOverage()
    {
        var result = PendingOverageCalculator.ForAccounts([Usage(1, 2026, 10, 400)], NoPaid, Tiers, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void UnlimitedPlan_NeverHasPendingOverage()
    {
        var result = PendingOverageCalculator.ForAccounts([Usage(1, 2026, 10, 99999, limit: -1)], NoPaid, Tiers, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void GroupsByAccountAndOrdersByMonth()
    {
        var usages = new[]
        {
            Usage(2, 2026, 10, 650),
            Usage(1, 2026, 9, 600),
            Usage(3, 2026, 10, 700, accountId: 8)
        };

        var result = PendingOverageCalculator.ForAccounts(usages, NoPaid, Tiers, Today);

        Assert.Equal([9, 10], result[3].Select(p => p.Month));
        Assert.Single(result[8]);
    }
}
```

`ZynstormECFPlatform.Tests/Billing/AccountPaymentBuilderTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class AccountPaymentBuilderTests
{
    private static BillingAccount Account(int paidMonths = 1, decimal discount = 0m) => new()
    {
        BillingAccountId = 3,
        Name = "Mi Cuenta",
        PlanId = 5,
        Plan = new Plan { PlanId = 5, Name = "Pool 1000", MonthlyFee = 3000m, MonthlyDocumentLimit = 1000, PlanTypeId = 1 },
        PaidMonths = paidMonths,
        PrepaymentDiscountPercent = discount,
        NextPaymentDate = new DateTime(2026, 10, 15)
    };

    private static RegisterPaymentRequestDto Request(bool includeFee, string? reference = null) => new()
    {
        PaymentDate = new DateTime(2026, 11, 2),
        PaymentMethod = (int)PaymentMethod.Transfer,
        IncludePlanFee = includeFee,
        Reference = reference
    };

    private static AccountPendingOverageDto Overage(int id, int month, decimal amount) => new()
    {
        BillingAccountMonthlyUsageId = id, Year = 2026, Month = month, PlanId = 5, PlanName = "Pool 1000",
        OverageDocuments = 50, Amount = amount
    };

    [Fact]
    public void Fee_AdvancesTheNextPaymentFromTheDueDate()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [], new DateTime(2026, 11, 2), "user-1");

        Assert.Equal(new DateTime(2026, 11, 15), draft.NewNextPaymentDate);
        var item = Assert.Single(draft.Payment.Items);
        Assert.Equal((int)ClientPaymentItemType.PlanFee, item.ItemType);
        Assert.Equal(3000m, item.Amount);
        Assert.Equal(new DateTime(2026, 10, 15), item.PreviousNextPaymentDate);
        Assert.Equal(3000m, draft.Payment.TotalAmount);
    }

    [Fact]
    public void Payment_BelongsToTheAccountNotToAClient()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [], new DateTime(2026, 11, 2), "user-1");

        Assert.Null(draft.Payment.ClientId);
        Assert.Equal(3, draft.Payment.BillingAccountId);
        Assert.Equal("user-1", draft.Payment.RegisteredByUserId);
    }

    [Fact]
    public void PrepaidMonthsWithDiscount_ChargeTheDiscountedTotal()
    {
        var draft = AccountPaymentBuilder.Build(Account(paidMonths: 3, discount: 10m), Request(true), [], new DateTime(2026, 11, 2), null);

        Assert.Equal(8100m, draft.Payment.TotalAmount);
        Assert.Equal(new DateTime(2027, 1, 15), draft.NewNextPaymentDate);
    }

    [Fact]
    public void OveragesOnly_AddOverageItemsAndKeepTheDueDate()
    {
        var draft = AccountPaymentBuilder.Build(
            Account(), Request(false), [Overage(2, 10, 300m), Overage(1, 9, 200m)], new DateTime(2026, 11, 2), null);

        Assert.Null(draft.NewNextPaymentDate);
        Assert.Equal(500m, draft.Payment.TotalAmount);
        Assert.All(draft.Payment.Items, i => Assert.Equal((int)ClientPaymentItemType.Overage, i.ItemType));
        Assert.Equal([1, 2], draft.Payment.Items.Select(i => i.BillingAccountMonthlyUsageId!.Value));
    }

    [Fact]
    public void FeeAndOverages_SumInTheTotal()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [Overage(1, 10, 300m)], new DateTime(2026, 11, 2), null);

        Assert.Equal(2, draft.Payment.Items.Count);
        Assert.Equal(3300m, draft.Payment.TotalAmount);
    }

    [Fact]
    public void ReferenceIsTrimmedAndBlankBecomesNull()
    {
        var trimmed = AccountPaymentBuilder.Build(Account(), Request(true, "  TR-99  "), [], new DateTime(2026, 11, 2), null);
        var blank = AccountPaymentBuilder.Build(Account(), Request(true, "   "), [], new DateTime(2026, 11, 2), null);

        Assert.Equal("TR-99", trimmed.Payment.Reference);
        Assert.Null(blank.Payment.Reference);
    }
}
```

- [ ] **Step 3: Correr y verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~PaymentReceiptMapperTests|FullyQualifiedName~PendingOverageCalculatorTests|FullyQualifiedName~AccountPaymentBuilderTests" 2>&1 | grep -E "error" | sort -u | head -5`
Expected: errores de compilación (`PaymentReceiptMapper`, `PendingOverageCalculator`, `AccountPaymentBuilder` no existen).

- [ ] **Step 4: Crear `PaymentReceiptMapper` y `PlanFeePreviewBuilder`**

`ZynstormECFPlatform.Services/Billing/PaymentReceiptMapper.cs`:

```csharp
using System.Globalization;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Convierte un recibo guardado en su DTO. Sirve para recibos de clientes sueltos y de cuentas.</summary>
public static class PaymentReceiptMapper
{
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public static PaymentReceiptDto ToReceipt(ClientPayment payment, string payerGuid, string payerName, bool isAccount = false) => new()
    {
        ClientPaymentId = payment.ClientPaymentId,
        ReceiptNumber = PaymentRegistrationPolicy.FormatReceiptNumber(payment.ClientPaymentId),
        ClientGuidId = payerGuid,
        ClientName = payerName,
        IsAccount = isAccount,
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
                OfferMonthsCovered = HasOffer(i) ? i.OfferMonthsCovered : null,
                OfferUnitPrice = HasOffer(i) ? i.OfferUnitPrice : null,
                FullPriceMonthsCovered = HasOffer(i) ? i.MonthsCovered - i.OfferMonthsCovered : null,
                FullPriceUnitPrice = HasOffer(i)
                    ? OfferPricing.GetFullPriceUnit(i.GrossAmount ?? i.Amount, i.MonthsCovered!.Value, i.OfferMonthsCovered!.Value, i.OfferUnitPrice!.Value)
                    : null,
                PreviousNextPaymentDate = i.PreviousNextPaymentDate,
                NewNextPaymentDate = i.NewNextPaymentDate,
                Year = i.Year,
                Month = i.Month,
                OverageDocuments = i.OverageDocuments
            })
            .ToList()
    };

    private static bool HasOffer(ClientPaymentItem item) =>
        item is { ItemType: (int)ClientPaymentItemType.PlanFee, OfferMonthsCovered: > 0, OfferUnitPrice: not null, MonthsCovered: not null };

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

`ZynstormECFPlatform.Services/Billing/PlanFeePreviewBuilder.cs`:

```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Mensualidad que se registraría hoy con las condiciones de cobro de un cliente o una cuenta.</summary>
public static class PlanFeePreviewBuilder
{
    public static PlanFeePreviewDto Build(BillingTerms terms, DateTime today)
    {
        var calculation = PaymentCalculator.CalculateCycle(terms, today);

        return new PlanFeePreviewDto
        {
            PlanName = terms.Plan.Name,
            PlanTypeId = terms.Plan.PlanTypeId,
            MonthlyFee = calculation.MonthlyFee,
            MonthsCovered = calculation.MonthsCovered,
            GrossAmount = calculation.GrossAmount,
            DiscountPercent = calculation.DiscountPercent,
            DiscountAmount = calculation.DiscountAmount,
            Total = calculation.Total,
            OfferMonthsCovered = calculation.OfferMonthsCovered,
            OfferUnitPrice = calculation.OfferUnitPrice,
            CurrentNextPaymentDate = terms.NextPaymentDate?.Date,
            NewNextPaymentDate = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(terms.NextPaymentDate, today, calculation.MonthsCovered)
        };
    }
}
```

- [ ] **Step 5: Crear `PendingOverageCalculator` y `AccountPaymentBuilder`**

`ZynstormECFPlatform.Services/Billing/PendingOverageCalculator.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Excedentes de meses cerrados sin pagar. Función pura: recibe los datos ya leídos.</summary>
public static class PendingOverageCalculator
{
    public static Dictionary<int, List<AccountPendingOverageDto>> ForAccounts(
        IEnumerable<BillingAccountMonthlyUsage> usages,
        ISet<int> paidUsageIds,
        IEnumerable<PlanOverageTier> tiers,
        DateTime today)
    {
        var tierList = tiers.ToList();

        return usages
            .Where(u => !paidUsageIds.Contains(u.BillingAccountMonthlyUsageId)
                        && PaymentRegistrationPolicy.IsOverageMonthPayable(u.Year, u.Month, today))
            .Select(u =>
            {
                var calculation = BillingCalculator.Calculate(
                    u.MonthlyFee,
                    u.MonthlyDocumentLimit,
                    tierList.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                    u.AcceptedDocuments);

                return (u.BillingAccountId, Pending: new AccountPendingOverageDto
                {
                    BillingAccountMonthlyUsageId = u.BillingAccountMonthlyUsageId,
                    Year = u.Year,
                    Month = u.Month,
                    PlanId = u.PlanId,
                    PlanName = u.PlanName,
                    AcceptedDocuments = u.AcceptedDocuments,
                    MonthlyDocumentLimit = u.MonthlyDocumentLimit,
                    OverageDocuments = calculation.OverageDocuments,
                    Amount = calculation.OverageAmount
                });
            })
            .Where(x => x.Pending.Amount > 0)
            .GroupBy(x => x.BillingAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Pending).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList());
    }
}
```

`ZynstormECFPlatform.Services/Billing/AccountPaymentBuilder.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record AccountPaymentDraft(ClientPayment Payment, DateTime? NewNextPaymentDate);

/// <summary>Arma el recibo de una cuenta a partir de lo que se paga. No modifica la cuenta ni toca la base de datos.</summary>
public static class AccountPaymentBuilder
{
    public static AccountPaymentDraft Build(
        BillingAccount account,
        RegisterPaymentRequestDto request,
        IReadOnlyCollection<AccountPendingOverageDto> selectedOverages,
        DateTime paymentDate,
        string? userId)
    {
        var date = paymentDate.Date;

        var payment = new ClientPayment
        {
            BillingAccountId = account.BillingAccountId,
            PaymentDate = date,
            PaymentMethod = request.PaymentMethod,
            Reference = Clean(request.Reference),
            Notes = Clean(request.Notes),
            RegisteredByUserId = userId
        };

        DateTime? newNext = null;

        if (request.IncludePlanFee)
        {
            var calculation = PaymentCalculator.CalculateCycle(BillingTerms.For(account), date);
            var previousNext = account.NextPaymentDate?.Date;
            newNext = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(previousNext, date, calculation.MonthsCovered);

            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.PlanFee,
                Amount = calculation.Total,
                PlanId = account.PlanId,
                PlanName = account.Plan.Name,
                MonthsCovered = calculation.MonthsCovered,
                GrossAmount = calculation.GrossAmount,
                DiscountPercent = calculation.DiscountPercent,
                DiscountAmount = calculation.DiscountAmount,
                OfferMonthsCovered = calculation.OfferMonthsCovered > 0 ? calculation.OfferMonthsCovered : null,
                OfferUnitPrice = calculation.OfferUnitPrice,
                PreviousNextPaymentDate = previousNext,
                NewNextPaymentDate = newNext
            });
        }

        foreach (var overage in selectedOverages.OrderBy(o => o.Year).ThenBy(o => o.Month))
        {
            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.Overage,
                Amount = overage.Amount,
                PlanId = overage.PlanId,
                PlanName = overage.PlanName,
                BillingAccountMonthlyUsageId = overage.BillingAccountMonthlyUsageId,
                Year = overage.Year,
                Month = overage.Month,
                OverageDocuments = overage.OverageDocuments
            });
        }

        payment.TotalAmount = payment.Items.Sum(i => i.Amount);

        return new AccountPaymentDraft(payment, newNext);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

- [ ] **Step 6: Que el servicio de clientes use el mapper y el builder**

En `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs`:

1. Borrar todo desde `private static PaymentReceiptDto ToReceipt(` hasta el final de la clase (incluye `ToReceipt`, `HasOffer` y `DescribeItem`) y cerrar la clase:

Run: `f=ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs; n=$(grep -n "private static PaymentReceiptDto ToReceipt" $f | cut -d: -f1); head -n $((n-2)) $f > "$f.tmp" && printf '}\n' >> "$f.tmp" && mv "$f.tmp" $f && tail -5 $f`
Expected: el archivo termina con el cierre de `BuildPlanFeePreview` (`    }`) seguido de `}`.

2. Reemplazar el cuerpo de `BuildPlanFeePreview`:

```csharp
    private static PlanFeePreviewDto BuildPlanFeePreview(Client client, DateTime today)
    {
        var calculation = PaymentCalculator.CalculateCycle(client, today);

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
            OfferMonthsCovered = calculation.OfferMonthsCovered,
            OfferUnitPrice = calculation.OfferUnitPrice,
            CurrentNextPaymentDate = client.NextPaymentDate?.Date,
            NewNextPaymentDate = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(client.NextPaymentDate, today, calculation.MonthsCovered)
        };
    }
```

por:

```csharp
    private static PlanFeePreviewDto BuildPlanFeePreview(Client client, DateTime today) =>
        PlanFeePreviewBuilder.Build(BillingTerms.For(client), today);
```

3. Reemplazar `payments.Select(p => ToReceipt(p, client)).ToList()` por `payments.Select(p => PaymentReceiptMapper.ToReceipt(p, client.GuidId, client.Name)).ToList()` y `var receipt = ToReceipt(payment, client);` por `var receipt = PaymentReceiptMapper.ToReceipt(payment, client.GuidId, client.Name);`.

4. Quitar el campo que quedó sin uso: borrar la línea `    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");` y, si ya nada usa `CultureInfo`, la línea `using System.Globalization;`.

- [ ] **Step 7: Compilar y correr las pruebas**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y luego `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing" 2>&1 | grep -E "error|Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Failed: 0` (las 3 clases nuevas más todas las de `Billing`).

- [ ] **Step 8: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Services/Billing ZynstormECFPlatform.Tests/Billing && git commit -q -m "$(cat <<'EOF'
Piezas puras del pago de cuentas: recibo, pendientes y armado del pago

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 4: Servicio de pagos de cuenta

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/IBillingAccountPaymentService.cs`, `BillingAccountPaymentService.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: todo lo de los Tasks 1–3; `PaymentRegistrationResult<T>` y `PaymentRegistrationOutcome` de `IClientPaymentRegistrationService.cs`.
- Produces: `IBillingAccountPaymentService` con:
  - `GetPreviewAsync(string accountGuid, CancellationToken)` → `PaymentRegistrationResult<AccountPaymentPreviewDto>`
  - `RegisterAsync(string accountGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken)` → `PaymentRegistrationResult<PaymentReceiptDto>`
  - `GetHistoryAsync(string accountGuid, CancellationToken)` → `PaymentRegistrationResult<List<PaymentReceiptDto>>`
  - `GetPendingOveragesAsync(IReadOnlyCollection<int> accountIds, CancellationToken)` → `Dictionary<int, List<AccountPendingOverageDto>>`

- [ ] **Step 1: Interfaz**

`ZynstormECFPlatform.Services/Billing/IBillingAccountPaymentService.cs`:

```csharp
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountPaymentService
{
    Task<PaymentRegistrationResult<AccountPaymentPreviewDto>> GetPreviewAsync(string accountGuid, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string accountGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string accountGuid, CancellationToken cancellationToken = default);

    /// <summary>Excedentes del pool de meses cerrados sin pagar, por BillingAccountId.</summary>
    Task<Dictionary<int, List<AccountPendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implementación**

`ZynstormECFPlatform.Services/Billing/BillingAccountPaymentService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountPaymentService(
    IBillingAccountService billingAccountService,
    IClientPaymentService clientPaymentService,
    IClientPaymentItemService clientPaymentItemService,
    IBillingAccountMonthlyUsageService usageService,
    IPlanOverageTierService planOverageTierService,
    IUnitOfWork unitOfWork) : IBillingAccountPaymentService
{
    private const string AccountNotFound = "Cuenta no encontrada.";
    private const string OveragesUnavailable = "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.";

    public async Task<PaymentRegistrationResult<AccountPaymentPreviewDto>> GetPreviewAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await billingAccountService.Table.AsNoTracking()
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<AccountPaymentPreviewDto>.NotFound(AccountNotFound);

        var today = DateTimeExtensions.DrNow.Date;
        var terms = BillingTerms.For(account);
        var pending = await GetPendingOveragesAsync([account.BillingAccountId], cancellationToken);

        return PaymentRegistrationResult<AccountPaymentPreviewDto>.Success(new AccountPaymentPreviewDto
        {
            AccountGuidId = account.GuidId,
            AccountName = account.Name,
            PaymentSuspended = account.PaymentSuspendedAtUtc != null,
            PlanFee = PaymentCalculator.GetMonthlyFee(terms) > 0 ? PlanFeePreviewBuilder.Build(terms, today) : null,
            PendingOverages = pending.GetValueOrDefault(account.BillingAccountId) ?? []
        });
    }

    public async Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string accountGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var overageIds = request.OverageUsageIds.Distinct().ToList();

        var errors = PaymentRegistrationPolicy.ValidateRequest(
            request.PaymentDate, request.PaymentMethod, request.IncludePlanFee, overageIds.Count,
            request.Reference, request.Notes, today);
        if (errors.Count > 0) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(errors);

        var account = await billingAccountService.Table
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<PaymentReceiptDto>.NotFound(AccountNotFound);
        if (request.IncludePlanFee && PaymentCalculator.GetMonthlyFee(BillingTerms.For(account)) <= 0)
            return PaymentRegistrationResult<PaymentReceiptDto>.Invalid("El plan de la cuenta no tiene mensualidad a pagar.");

        var pending = (await GetPendingOveragesAsync([account.BillingAccountId], cancellationToken))
            .GetValueOrDefault(account.BillingAccountId) ?? [];
        var selected = pending.Where(p => overageIds.Contains(p.BillingAccountMonthlyUsageId)).ToList();
        if (selected.Count != overageIds.Count)
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(OveragesUnavailable);

        var paymentDate = request.PaymentDate!.Value.Date;
        var wasSuspended = account.PaymentSuspendedAtUtc != null;

        // Se arma antes de mover las fechas de la cuenta: el ciclo parte del vencimiento actual.
        var draft = AccountPaymentBuilder.Build(account, request, selected, paymentDate, userId);

        if (request.IncludePlanFee)
        {
            account.LastPaymentDate = paymentDate;
            account.NextPaymentDate = draft.NewNextPaymentDate;
        }

        var reactivated = PaymentRegistrationPolicy.ShouldReactivate(wasSuspended, request.IncludePlanFee, account.NextPaymentDate, today);
        if (reactivated)
            account.PaymentSuspendedAtUtc = null;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientPaymentService.InsertAsync(draft.Payment);
                await billingAccountService.UpdateAsync(account);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // El índice único de excedente se disparó por un registro concurrente.
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(OveragesUnavailable);
        }

        var receipt = PaymentReceiptMapper.ToReceipt(draft.Payment, account.GuidId, account.Name, isAccount: true);
        receipt.Reactivated = reactivated;
        receipt.StillSuspended = wasSuspended && !reactivated;

        return PaymentRegistrationResult<PaymentReceiptDto>.Success(receipt);
    }

    public async Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await billingAccountService.Table.AsNoTracking()
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<List<PaymentReceiptDto>>.NotFound(AccountNotFound);

        var payments = await clientPaymentService.Table.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.BillingAccountId == account.BillingAccountId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ClientPaymentId)
            .ToListAsync(cancellationToken);

        return PaymentRegistrationResult<List<PaymentReceiptDto>>.Success(
            payments.Select(p => PaymentReceiptMapper.ToReceipt(p, account.GuidId, account.Name, isAccount: true)).ToList());
    }

    public async Task<Dictionary<int, List<AccountPendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0) return [];

        var today = DateTimeExtensions.DrNow.Date;
        var ids = accountIds.ToList();

        var usages = await usageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.BillingAccountId)
                        && (u.Year < today.Year || (u.Year == today.Year && u.Month < today.Month)))
            .ToListAsync(cancellationToken);

        if (usages.Count == 0) return [];

        var usageIds = usages.Select(u => u.BillingAccountMonthlyUsageId).ToList();
        var paidUsageIds = (await clientPaymentItemService.Table.AsNoTracking()
                .Where(i => i.ItemType == (int)ClientPaymentItemType.Overage
                            && i.BillingAccountMonthlyUsageId != null
                            && usageIds.Contains(i.BillingAccountMonthlyUsageId.Value))
                .Select(i => i.BillingAccountMonthlyUsageId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
        var tiers = await planOverageTierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        return PendingOverageCalculator.ForAccounts(usages, paidUsageIds, tiers, today);
    }
}
```

- [ ] **Step 3: Registrar en DI**

En `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`, reemplazar:

```csharp
        services.AddTransient<Billing.IClientPaymentRegistrationService, Billing.ClientPaymentRegistrationService>();
```

por:

```csharp
        services.AddTransient<Billing.IClientPaymentRegistrationService, Billing.ClientPaymentRegistrationService>();
        services.AddTransient<Billing.IBillingAccountPaymentService, Billing.BillingAccountPaymentService>();
```

- [ ] **Step 4: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5`
Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services && git commit -q -m "$(cat <<'EOF'
Servicio de pagos de cuentas de facturación

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 5: Cuenta suspendida bloquea la emisión

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/BillingAccountAccess.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/ClientStatusEvaluator.cs`
- Modify: `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` (`GetStatusByKey`)
- Test: `ZynstormECFPlatform.Tests/Billing/ClientStatusEvaluatorAccountTests.cs`, `ZynstormECFPlatform.Tests/Production/AccountSuspendedResultTests.cs`

**Interfaces:**
- Produces:
  - `BillingAccountAccess.IsSuspended(BillingAccount? account)` → `bool`; `BillingAccountAccess.SuspendedMessage` (`string`).
  - `ClientStatusEvaluator.AccountSuspendedMessage` (`string`). El cliente con cuenta toma `NextPaymentDate` y `PlanName` de la cuenta.
  - `ReceivedEcfProductionService.AccountSuspendedMessage` y `BuildAccountSuspendedResult(ReceivedEcfEmissionResultDto)` (estáticos, públicos).

- [ ] **Step 1: Escribir las pruebas (fallan: no existen los miembros)**

`ZynstormECFPlatform.Tests/Billing/ClientStatusEvaluatorAccountTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientStatusEvaluatorAccountTests
{
    private static BillingAccount Account(DateTime? suspendedAt = null) => new()
    {
        BillingAccountId = 3,
        Name = "Mi Cuenta",
        PaymentSuspendedAtUtc = suspendedAt,
        NextPaymentDate = new DateTime(2026, 12, 1),
        Plan = new Plan { Name = "Pool 1000" }
    };

    private static ApiKey KeyFor(BillingAccount? account, bool clientInactive = false) => new()
    {
        Apikey = "k",
        StatusId = (int)StatusEnum.Active,
        Client = new Client
        {
            Name = "Cliente",
            Rnc = "131000001",
            GuidId = "client-guid",
            StatusId = (int)StatusEnum.Active,
            ClientInactive = clientInactive,
            BillingAccount = account,
            BillingAccountId = account?.BillingAccountId
        }
    };

    [Fact]
    public void SuspendedAccount_BlocksItsClient()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(suspendedAt: DateTime.UtcNow)));

        Assert.False(result.IsActive);
        Assert.True(result.PaymentSuspended);
        Assert.True(result.ClientInactive);
        Assert.Equal(ClientStatusEvaluator.AccountSuspendedMessage, result.Message);
        Assert.Equal("client-guid", result.ClientGuid);
    }

    [Fact]
    public void SuspendedAccount_ReportsThePlanAndNextPaymentOfTheAccount()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(suspendedAt: DateTime.UtcNow)));

        Assert.Equal("Pool 1000", result.PlanName);
        Assert.Equal(new DateTime(2026, 12, 1), result.NextPaymentDate);
    }

    [Fact]
    public void ActiveAccount_LeavesItsClientActive_AndShowsTheAccountPlan()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account()));

        Assert.True(result.IsActive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientActiveMessage, result.Message);
        Assert.Equal("Pool 1000", result.PlanName);
    }

    [Fact]
    public void ClientWithoutAccount_IsNotAffected()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(account: null));

        Assert.True(result.IsActive);
    }

    [Fact]
    public void ManuallyInactiveClient_InActiveAccount_IsStillInactive()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(), clientInactive: true));

        Assert.False(result.IsActive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientInactiveMessage, result.Message);
    }

    [Fact]
    public void Access_IsSuspended_OnlyWhenTheAccountHasASuspensionDate()
    {
        Assert.False(BillingAccountAccess.IsSuspended(null));
        Assert.False(BillingAccountAccess.IsSuspended(Account()));
        Assert.True(BillingAccountAccess.IsSuspended(Account(DateTime.UtcNow)));
    }
}
```

`ZynstormECFPlatform.Tests/Production/AccountSuspendedResultTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class AccountSuspendedResultTests
{
    [Fact]
    public void BuildAccountSuspendedResult_UsesTheClientInactiveFlagSoTheApiAnswers403()
    {
        var result = ReceivedEcfProductionService.BuildAccountSuspendedResult(new ReceivedEcfEmissionResultDto { Success = true });

        Assert.False(result.Success);
        Assert.True(result.ClientInactive);
        Assert.Equal(ReceivedEcfProductionService.AccountSuspendedMessage, result.Message);
        Assert.Equal(0, result.EcfDocumentId);
    }
}
```

- [ ] **Step 2: Correr y verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientStatusEvaluatorAccountTests|FullyQualifiedName~AccountSuspendedResultTests" 2>&1 | grep -E "error" | sort -u | head -4`
Expected: errores de compilación (`BillingAccountAccess`, `AccountSuspendedMessage`, `BuildAccountSuspendedResult`).

- [ ] **Step 3: `BillingAccountAccess`**

`ZynstormECFPlatform.Services/Billing/BillingAccountAccess.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Regla de acceso por cuenta: una cuenta suspendida bloquea a todos sus clientes.</summary>
public static class BillingAccountAccess
{
    public const string SuspendedMessage = "La cuenta de facturación está suspendida por falta de pago. Comuníquese con su proveedor.";

    public static bool IsSuspended(BillingAccount? account) => account?.PaymentSuspendedAtUtc != null;
}
```

- [ ] **Step 4: `ClientStatusEvaluator` considera la cuenta**

Reemplazar el contenido completo de `ZynstormECFPlatform.Services/Billing/ClientStatusEvaluator.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>
/// Evaluador de estado activo de un cliente a partir de su entidad ApiKey y Client asociada.
/// Un cliente que pertenece a una cuenta de facturación toma de ella el plan, el próximo pago y la suspensión.
/// Función pura: no toca base de datos ni contexto HTTP.
/// </summary>
public static class ClientStatusEvaluator
{
    public const string KeyNotFoundOrInvalidMessage = "ApiKey no encontrada o no válida.";
    public const string KeyInactiveMessage = "El ApiKey proporcionado se encuentra inactivo.";
    public const string ClientNotFoundOrInactiveMessage = "El cliente asociado a este ApiKey no existe o se encuentra inactivo.";
    public const string PaymentSuspendedMessage = "Cliente suspendido por falta de pago.";
    public const string AccountSuspendedMessage = BillingAccountAccess.SuspendedMessage;
    public const string ClientInactiveMessage = "El cliente se encuentra desactivado. Por favor, comuníquese con soporte.";
    public const string ClientActiveMessage = "Cliente activo.";

    public static ClientActiveStatusDto Evaluate(ApiKey? apiKeyEntity)
    {
        if (apiKeyEntity == null || apiKeyEntity.IsDeleted)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = KeyNotFoundOrInvalidMessage
            };
        }

        if (apiKeyEntity.StatusId != (int)StatusEnum.Active)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = KeyInactiveMessage
            };
        }

        var client = apiKeyEntity.Client;
        if (client == null || client.IsDeleted || client.StatusId != (int)StatusEnum.Active)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                Message = ClientNotFoundOrInactiveMessage
            };
        }

        var account = client.BillingAccount;
        var accountSuspended = BillingAccountAccess.IsSuspended(account);
        var isPaymentSuspended = client.PaymentSuspendedAtUtc != null || accountSuspended;
        var isClientInactive = client.ClientInactive;
        var nextPaymentDate = account != null ? account.NextPaymentDate : client.NextPaymentDate;
        var planName = account != null ? account.Plan?.Name : client.Plan?.Name;

        if (isPaymentSuspended)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                ClientGuid = client.GuidId,
                ClientName = client.Name,
                Rnc = client.Rnc,
                ClientInactive = true,
                PaymentSuspended = true,
                NextPaymentDate = nextPaymentDate,
                PlanName = planName,
                IsDgiiProduction = client.IsDgiiProduction,
                IsCertified = client.IsCertified,
                Message = accountSuspended ? AccountSuspendedMessage : PaymentSuspendedMessage
            };
        }

        if (isClientInactive)
        {
            return new ClientActiveStatusDto
            {
                IsActive = false,
                ClientGuid = client.GuidId,
                ClientName = client.Name,
                Rnc = client.Rnc,
                ClientInactive = true,
                PaymentSuspended = false,
                NextPaymentDate = nextPaymentDate,
                PlanName = planName,
                IsDgiiProduction = client.IsDgiiProduction,
                IsCertified = client.IsCertified,
                Message = ClientInactiveMessage
            };
        }

        return new ClientActiveStatusDto
        {
            IsActive = true,
            ClientGuid = client.GuidId,
            ClientName = client.Name,
            Rnc = client.Rnc,
            ClientInactive = false,
            PaymentSuspended = false,
            NextPaymentDate = nextPaymentDate,
            PlanName = planName,
            IsDgiiProduction = client.IsDgiiProduction,
            IsCertified = client.IsCertified,
            Message = ClientActiveMessage
        };
    }
}
```

- [ ] **Step 5: Compuerta de emisión**

En `ZynstormECFPlatform.Services/Production/ReceivedEcfProductionService.cs`:

1. Reemplazar `    private readonly EcfReferenceCache _referenceCache;` por:

```csharp
    private readonly EcfReferenceCache _referenceCache;
    private readonly IBillingAccountService _billingAccountService;
```

2. Reemplazar `        EcfReferenceCache referenceCache)` por:

```csharp
        EcfReferenceCache referenceCache,
        IBillingAccountService billingAccountService)
```

3. Reemplazar `        _referenceCache = referenceCache;` por:

```csharp
        _referenceCache = referenceCache;
        _billingAccountService = billingAccountService;
```

4. Reemplazar:

```csharp
        if (client.ClientInactive)
            return BuildClientInactiveResult(resultDto);
```

por:

```csharp
        if (client.ClientInactive)
            return BuildClientInactiveResult(resultDto);

        // Un cliente con cuenta factura contra ella: si la cuenta está suspendida, no emite.
        if (client.BillingAccountId is int billingAccountId)
        {
            var account = await _billingAccountService.GetNoTrackingByAsync(a => a.BillingAccountId == billingAccountId);
            if (BillingAccountAccess.IsSuspended(account))
                return BuildAccountSuspendedResult(resultDto);
        }
```

5. Reemplazar:

```csharp
    private static ReceivedEcfEmissionResultDto FailConfiguration(ReceivedEcfEmissionResultDto resultDto, string message)
```

por:

```csharp
    public const string AccountSuspendedMessage = BillingAccountAccess.SuspendedMessage;

    /// <summary>Misma bandera que la suspensión de un cliente, para que la API responda 403.</summary>
    public static ReceivedEcfEmissionResultDto BuildAccountSuspendedResult(ReceivedEcfEmissionResultDto resultDto)
    {
        resultDto.Success = false;
        resultDto.ClientInactive = true;
        resultDto.Message = AccountSuspendedMessage;
        return resultDto;
    }

    private static ReceivedEcfEmissionResultDto FailConfiguration(ReceivedEcfEmissionResultDto resultDto, string message)
```

- [ ] **Step 6: `GetStatusByKey` carga la cuenta**

En `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`, reemplazar:

```csharp
                    .Include(k => k.Client)
                        .ThenInclude(c => c.Plan)
                    .FirstOrDefaultAsync(k => k.Apikey == effectiveKey, cancellationToken);
```

por:

```csharp
                    .Include(k => k.Client)
                        .ThenInclude(c => c.Plan)
                    .Include(k => k.Client)
                        .ThenInclude(c => c.BillingAccount)
                            .ThenInclude(a => a!.Plan)
                    .FirstOrDefaultAsync(k => k.Apikey == effectiveKey, cancellationToken);
```

- [ ] **Step 7: Compilar y correr las pruebas**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y luego `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientStatusEvaluator|FullyQualifiedName~AccountSuspendedResultTests|FullyQualifiedName~ClientInactiveResultTests" 2>&1 | grep -E "error|Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Failed: 0` (las nuevas más las existentes de `ClientStatusEvaluatorTests`).

- [ ] **Step 8: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs ZynstormECFPlatform.Tests && git commit -q -m "$(cat <<'EOF'
Bloquear la emisión de los clientes de una cuenta suspendida

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 6: Los clientes con cuenta salen del cobro individual

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/BillingScope.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/PaymentReminderService.cs`, `ClientPaymentRegistrationService.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` (`ActivePlanClientsQuery`)
- Test: `ZynstormECFPlatform.Tests/Billing/BillingScopeTests.cs`

**Interfaces:**
- Produces: `BillingScope.IndividuallyBilled` (`Expression<Func<Client, bool>>`: `c => c.BillingAccountId == null`).

- [ ] **Step 1: Escribir la prueba (falla: no existe `BillingScope`)**

`ZynstormECFPlatform.Tests/Billing/BillingScopeTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingScopeTests
{
    [Fact]
    public void IndividuallyBilled_IncludesAClientWithoutAccount()
    {
        var matches = BillingScope.IndividuallyBilled.Compile();

        Assert.True(matches(new Client { PlanId = 5 }));
    }

    [Fact]
    public void IndividuallyBilled_ExcludesAClientThatBelongsToAnAccount_EvenIfItKeepsItsOwnPlan()
    {
        var matches = BillingScope.IndividuallyBilled.Compile();

        Assert.False(matches(new Client { PlanId = 5, BillingAccountId = 3 }));
    }
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingScopeTests" 2>&1 | grep -E "error" | sort -u | head -3`
Expected: error de compilación (`BillingScope` no existe).

- [ ] **Step 3: Crear `BillingScope`**

`ZynstormECFPlatform.Services/Billing/BillingScope.cs`:

```csharp
using System.Linq.Expressions;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

public static class BillingScope
{
    /// <summary>
    /// Clientes que se cobran por su propio plan (pagos, recordatorios y suspensión individuales).
    /// Los que pertenecen a una cuenta de facturación se cobran por la cuenta, aunque conserven un PlanId.
    /// </summary>
    public static readonly Expression<Func<Client, bool>> IndividuallyBilled = c => c.BillingAccountId == null;
}
```

- [ ] **Step 4: Aplicarlo**

En `PaymentReminderService.cs`, reemplazar:

```csharp
            .Include(c => c.Plan)
            .Where(c => c.Plan != null
                     && c.Plan.StatusId == (int)StatusEnum.Active
                     && c.NextPaymentDate != null);
```

por:

```csharp
            .Include(c => c.Plan)
            .Where(BillingScope.IndividuallyBilled)
            .Where(c => c.Plan != null
                     && c.Plan.StatusId == (int)StatusEnum.Active
                     && c.NextPaymentDate != null);
```

y, en `SendClientReminderAsync`, reemplazar:

```csharp
        if (client.Plan == null)
            return new(false, "El cliente no tiene un plan asignado.");
```

por:

```csharp
        if (client.BillingAccountId != null)
            return new(false, "El cliente pertenece a una cuenta de facturación; los avisos de pago se envían a la cuenta.");
        if (client.Plan == null)
            return new(false, "El cliente no tiene un plan asignado.");
```

En `ClientController.cs`, reemplazar:

```csharp
                .Include(c => c.Plan)
                .Where(c => c.Plan != null && c.Plan.StatusId == (int)StatusEnum.Active);
```

por:

```csharp
                .Include(c => c.Plan)
                .Where(BillingScope.IndividuallyBilled)
                .Where(c => c.Plan != null && c.Plan.StatusId == (int)StatusEnum.Active);
```

En `ClientPaymentRegistrationService.cs`, reemplazar la constante `    private const string NoPlan = "El cliente no tiene un plan asignado.";` por:

```csharp
    private const string NoPlan = "El cliente no tiene un plan asignado.";
    private const string BelongsToAccount = "El cliente pertenece a una cuenta de facturación; el pago se registra en la cuenta.";
```

y reemplazar las dos validaciones de plan:

```csharp
        if (client.Plan == null) return PaymentRegistrationResult<PaymentPreviewDto>.Invalid(NoPlan);
```

por:

```csharp
        if (client.BillingAccountId != null) return PaymentRegistrationResult<PaymentPreviewDto>.Invalid(BelongsToAccount);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentPreviewDto>.Invalid(NoPlan);
```

y:

```csharp
        if (client.Plan == null) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(NoPlan);
```

por:

```csharp
        if (client.BillingAccountId != null) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(BelongsToAccount);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(NoPlan);
```

- [ ] **Step 5: Compilar y correr**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y luego `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing" 2>&1 | grep -E "error|Passed!|Failed" | sort -u`
Expected: `Build succeeded.` y `Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs ZynstormECFPlatform.Tests && git commit -q -m "$(cat <<'EOF'
Excluir del cobro individual a los clientes que pertenecen a una cuenta

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 7: Recordatorios y suspensión de cuentas

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/IBillingAccountReminderService.cs`, `BillingAccountReminderService.cs`
- Modify: `ZynstormECFPlatform.Services/Jobs/PaymentReminderJob.cs`, `PaymentSuspensionJob.cs`
- Modify: `ZynstormECFPlatform.Services/Extensions/ServiceCollectionExtensions.cs`

**Interfaces:**
- Consumes: `BillingTerms`, `PaymentCalculator`, `PaymentReminderPolicy.Decide/GetDaysOverdue/GetDeadline`, `PaymentReminderEmails.Build*`, `PaymentReminderRunResult` (ya existentes).
- Produces: `IBillingAccountReminderService.RunDailyAsync(CancellationToken)` → `PaymentReminderRunResult` y `SuspendExpiredAccountsAsync(CancellationToken)` → `int`. Los avisos van al correo del dueño (`OwnerUser.Email`). Una cuenta se suspende poniendo `PaymentSuspendedAtUtc`; no toca a sus clientes.

- [ ] **Step 1: Interfaz**

`ZynstormECFPlatform.Services/Billing/IBillingAccountReminderService.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountReminderService
{
    /// <summary>Corrida diaria de avisos a los dueños de cuentas con pago vencido y resumen al correo administrativo.</summary>
    Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default);

    /// <summary>Suspende las cuentas cuyo período de gracia ya terminó. Al suspender una cuenta se bloquea la emisión de todos sus clientes.</summary>
    Task<int> SuspendExpiredAccountsAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Implementación**

`ZynstormECFPlatform.Services/Billing/BillingAccountReminderService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountReminderService(
    IBillingAccountService accountService,
    IEmailService emailService,
    IOptions<AppSettings> options,
    ILogger<BillingAccountReminderService> logger) : IBillingAccountReminderService
{
    private readonly AppSettings _settings = options.Value;

    private string AdminRecipient => !string.IsNullOrWhiteSpace(_settings.PaymentAlertEmail)
        ? _settings.PaymentAlertEmail
        : _settings.CertificateAlertEmail;

    /// <summary>Cuentas activas con plan activo y fecha de próximo pago registrada.</summary>
    private IQueryable<BillingAccount> Candidates() =>
        accountService.Table
            .Include(a => a.Plan)
            .Include(a => a.OwnerUser)
            .Where(a => a.StatusId == (int)StatusEnum.Active
                     && a.Plan.StatusId == (int)StatusEnum.Active
                     && a.NextPaymentDate != null);

    private static string? Recipient(BillingAccount account) => account.OwnerUser?.Email;

    private static bool HasEmail(BillingAccount account) => !string.IsNullOrWhiteSpace(Recipient(account));

    private static bool IsSuspended(BillingAccount account) => account.PaymentSuspendedAtUtc != null;

    private static PaymentReminderEmailData EmailData(BillingAccount account)
    {
        var due = account.NextPaymentDate!.Value.Date;
        var amount = PaymentCalculator.CalculateCycle(BillingTerms.For(account), due).Total;

        return new PaymentReminderEmailData(
            account.Name,
            account.Plan.Name,
            account.Plan.PlanTypeId,
            amount,
            Math.Max(1, account.PaidMonths),
            due,
            PaymentReminderPolicy.GetDeadline(due, account.PaymentGraceDays));
    }

    private static string DescribeStatus(BillingAccount account)
    {
        var due = account.NextPaymentDate!.Value.Date;

        if (IsSuspended(account)) return "Suspendida";
        if (!HasEmail(account)) return "Sin correo";
        if (account.FinalPaymentReminderSentFor?.Date == due) return "Último aviso enviado";
        if (account.FirstPaymentReminderSentFor?.Date == due) return "Primer aviso enviado";
        return "Pendiente de aviso";
    }

    private static PaymentSummaryItem SummaryItem(BillingAccount account, int daysOverdue, string status)
    {
        var data = EmailData(account);
        return new PaymentSummaryItem(
            $"{account.Name} (cuenta)", "—", data.PlanName, data.PlanTypeId, data.DueDate, daysOverdue, data.Amount, data.Deadline, status);
    }

    private async Task SendAsync(BillingAccount account, (string Subject, string HtmlBody) email, CancellationToken cancellationToken) =>
        await emailService.SendEmailAsync(Recipient(account)!, email.Subject, email.HtmlBody, cancellationToken: cancellationToken);

    /// <summary>Persiste primero la suspensión para que un fallo del correo no deje a la cuenta con acceso activo.</summary>
    private async Task<string> SuspendAccountAsync(BillingAccount account, CancellationToken cancellationToken)
    {
        account.PaymentSuspendedAtUtc = DateTime.UtcNow;
        await accountService.UpdateAsync(account);

        if (!HasEmail(account))
            return "Suspendida hoy";

        try
        {
            await SendAsync(account, PaymentReminderEmails.BuildSuspension(EmailData(account)), cancellationToken);
            return "Suspendida hoy";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pago: cuenta {AccountId} suspendida, pero no se pudo enviar el correo de suspensión.", account.BillingAccountId);
            return "Suspendida hoy (correo no enviado)";
        }
    }

    private static PaymentReminderAction Decide(BillingAccount account, DateTime today) =>
        PaymentReminderPolicy.Decide(
            account.NextPaymentDate!.Value.Date,
            account.PaymentGraceDays,
            account.FirstPaymentReminderSentFor,
            account.FinalPaymentReminderSentFor,
            HasEmail(account),
            IsSuspended(account),
            today);

    public async Task<int> SuspendExpiredAccountsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var accounts = await Candidates().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        var suspended = 0;

        foreach (var account in accounts)
        {
            if (Decide(account, today) != PaymentReminderAction.Suspend) continue;

            try
            {
                await SuspendAccountAsync(account, cancellationToken);
                suspended++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pago: error suspendiendo la cuenta {AccountId} después del período de gracia.", account.BillingAccountId);
            }
        }

        logger.LogInformation("Pago: {Suspended} cuenta(s) suspendida(s) por vencimiento.", suspended);
        return suspended;
    }

    public async Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var accounts = await Candidates().OrderBy(a => a.Name).ToListAsync(cancellationToken);

        var summary = new List<PaymentSummaryItem>();
        int first = 0, final = 0, suspended = 0;

        foreach (var account in accounts)
        {
            var due = account.NextPaymentDate!.Value.Date;
            var overdue = PaymentReminderPolicy.GetDaysOverdue(due, today);
            if (overdue < 1) continue;

            var hasEmail = HasEmail(account);
            var action = Decide(account, today);

            string status;
            try
            {
                switch (action)
                {
                    case PaymentReminderAction.FirstReminder when hasEmail:
                        await SendAsync(account, PaymentReminderEmails.BuildFirstReminder(EmailData(account)), cancellationToken);
                        account.FirstPaymentReminderSentFor = due;
                        await accountService.UpdateAsync(account);
                        first++;
                        status = "Primer aviso enviado hoy";
                        break;

                    case PaymentReminderAction.FinalReminder when hasEmail:
                        await SendAsync(account, PaymentReminderEmails.BuildFinalReminder(EmailData(account)), cancellationToken);
                        account.FinalPaymentReminderSentFor = due;
                        await accountService.UpdateAsync(account);
                        final++;
                        status = "Último aviso enviado hoy";
                        break;

                    case PaymentReminderAction.Suspend:
                        status = await SuspendAccountAsync(account, cancellationToken);
                        suspended++;
                        break;

                    default:
                        status = DescribeStatus(account);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pago: error procesando el aviso de la cuenta {AccountId}.", account.BillingAccountId);
                status = "Error al enviar el aviso";
            }

            summary.Add(SummaryItem(account, overdue, status));
        }

        if (summary.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(AdminRecipient))
            {
                logger.LogWarning("Pago: {Count} cuentas con pagos pendientes pero no hay PaymentAlertEmail ni CertificateAlertEmail configurado.", summary.Count);
            }
            else
            {
                var (subject, html) = PaymentReminderEmails.BuildAdminSummary(summary);
                await emailService.SendEmailAsync(AdminRecipient, subject, html, cancellationToken: cancellationToken);
            }
        }

        logger.LogInformation(
            "Pago (cuentas): {Pending} pendientes, {First} primeros avisos, {Final} últimos avisos, {Suspended} suspensiones.",
            summary.Count, first, final, suspended);

        return new PaymentReminderRunResult(first, final, suspended, summary.Count);
    }
}
```

- [ ] **Step 3: Jobs**

Reemplazar el contenido de `ZynstormECFPlatform.Services/Jobs/PaymentReminderJob.cs`:

```csharp
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Corre diariamente: envía el primer y el último aviso de pago vencido, suspende a los clientes y cuentas que no pagaron
/// dentro de sus días de gracia y manda el resumen de pendientes al correo administrativo.
/// </summary>
public class PaymentReminderJob(
    IPaymentReminderService paymentReminderService,
    IBillingAccountReminderService accountReminderService,
    ILogger<PaymentReminderJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Payment Reminder Job...");
        var result = await paymentReminderService.RunDailyAsync(cancellationToken);
        logger.LogInformation(
            "Payment Reminder Job finished: {Pending} pending, {First} first, {Final} final, {Suspended} suspended.",
            result.Pending, result.FirstReminders, result.FinalReminders, result.Suspended);

        var accounts = await accountReminderService.RunDailyAsync(cancellationToken);
        logger.LogInformation(
            "Payment Reminder Job (accounts) finished: {Pending} pending, {First} first, {Final} final, {Suspended} suspended.",
            accounts.Pending, accounts.FirstReminders, accounts.FinalReminders, accounts.Suspended);
    }
}
```

Reemplazar el contenido de `ZynstormECFPlatform.Services/Jobs/PaymentSuspensionJob.cs`:

```csharp
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Aplica el corte de acceso al iniciar el día posterior al período de gracia, a clientes sueltos y a cuentas.
/// Los recordatorios siguen ejecutándose en horario laboral en PaymentReminderJob.
/// </summary>
public class PaymentSuspensionJob(
    IPaymentReminderService paymentReminderService,
    IBillingAccountReminderService accountReminderService,
    ILogger<PaymentSuspensionJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Payment Suspension Job...");
        var suspended = await paymentReminderService.SuspendExpiredClientsAsync(cancellationToken);
        var suspendedAccounts = await accountReminderService.SuspendExpiredAccountsAsync(cancellationToken);
        logger.LogInformation(
            "Payment Suspension Job finished: {Suspended} clients and {Accounts} accounts suspended.", suspended, suspendedAccounts);
    }
}
```

- [ ] **Step 4: DI**

En `ServiceCollectionExtensions.cs`, reemplazar:

```csharp
        services.AddTransient<Billing.IBillingAccountPaymentService, Billing.BillingAccountPaymentService>();
```

por:

```csharp
        services.AddTransient<Billing.IBillingAccountPaymentService, Billing.BillingAccountPaymentService>();
        services.AddTransient<Billing.IBillingAccountReminderService, Billing.BillingAccountReminderService>();
```

- [ ] **Step 5: Compilar y correr toda la suite**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -5` y luego `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y solo la falla ya conocida `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada`.

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services && git commit -q -m "$(cat <<'EOF'
Recordatorios y suspensión por falta de pago de cuentas

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 8: Validaciones y DTOs de la API de cuentas

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs`
- Create: `ZynstormECFPlatform.Dtos/BillingAccountDtos.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs`

**Interfaces:**
- Produces:
  - `BillingAccountPolicy.ValidateAccount(string? name, Plan? plan, decimal? customMonthlyFee, int paidMonths, decimal prepaymentDiscountPercent, int paymentGraceDays)` → `List<string>`
  - `BillingAccountPolicy.ValidateOwner(bool ownerExists, bool ownerIsAdmin, bool ownerAlreadyHasAccount)` → `List<string>`
  - `BillingAccountPolicy.ValidateMembers(IReadOnlyCollection<Client> found, int requestedCount, int accountId)` → `List<string>`
  - `BillingAccountPolicy.IsQuotaExceeded(int acceptedDocuments, int? quota)` → `bool`
  - DTOs: `BillingAccountSaveDto`, `BillingAccountClientsDto`, `BillingAccountClientUsageDto`, `BillingAccountViewDto`, `BillingAccountDetailDto`.

- [ ] **Step 1: Escribir las pruebas (fallan: no existe `BillingAccountPolicy`)**

`ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingAccountPolicyTests
{
    private static Plan DocumentsPlan() => new()
    {
        PlanId = 5, Name = "Pool", PlanTypeId = (int)PlanTypeEnum.Documents, StatusId = (int)StatusEnum.Active
    };

    private static List<string> Validate(
        string? name = "Mi Cuenta", Plan? plan = null, decimal? fee = null, int months = 1, decimal discount = 0m, int grace = 3) =>
        BillingAccountPolicy.ValidateAccount(name, plan ?? DocumentsPlan(), fee, months, discount, grace);

    [Fact]
    public void ValidAccount_HasNoErrors() => Assert.Empty(Validate());

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Name_IsRequired(string? name) => Assert.Contains(Validate(name: name), e => e.Contains("nombre"));

    [Fact]
    public void Name_CannotExceed100Characters() =>
        Assert.Contains(Validate(name: new string('a', 101)), e => e.Contains("100"));

    [Fact]
    public void MissingPlan_IsRejected() =>
        Assert.Contains(BillingAccountPolicy.ValidateAccount("Mi Cuenta", null, null, 1, 0m, 3), e => e.Contains("plan"));

    [Fact]
    public void RentPlan_IsRejected()
    {
        var plan = DocumentsPlan();
        plan.PlanTypeId = (int)PlanTypeEnum.Rent;

        Assert.Contains(Validate(plan: plan), e => e.Contains("comprobantes"));
    }

    [Fact]
    public void InactivePlan_IsRejected()
    {
        var plan = DocumentsPlan();
        plan.StatusId = (int)StatusEnum.Inactive;

        Assert.Contains(Validate(plan: plan), e => e.Contains("activo"));
    }

    [Fact]
    public void NegativeCustomFee_IsRejected() => Assert.Contains(Validate(fee: -1m), e => e.Contains("negativo"));

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void PaidMonths_MustBeBetween1And60(int months) => Assert.Contains(Validate(months: months), e => e.Contains("meses"));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_MustBeBetween0And100(decimal discount) => Assert.Contains(Validate(discount: discount), e => e.Contains("descuento"));

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void GraceDays_MustBeBetween1And30(int grace) => Assert.Contains(Validate(grace: grace), e => e.Contains("gracia"));

    [Fact]
    public void Owner_MustExistBeAdminAndHaveNoAccount()
    {
        Assert.Empty(BillingAccountPolicy.ValidateOwner(true, true, false));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(false, false, false), e => e.Contains("no existe"));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(true, false, false), e => e.Contains("Admin"));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(true, true, true), e => e.Contains("ya tiene"));
    }

    [Fact]
    public void Members_AllRequestedMustExistAndBeFreeOrInTheSameAccount()
    {
        var free = new Client { Name = "Libre" };
        var mine = new Client { Name = "Mío", BillingAccountId = 3 };
        var other = new Client { Name = "Ajeno", BillingAccountId = 9 };

        Assert.Empty(BillingAccountPolicy.ValidateMembers([free, mine], 2, 3));
        Assert.Contains(BillingAccountPolicy.ValidateMembers([free], 2, 3), e => e.Contains("no existen"));
        Assert.Contains(BillingAccountPolicy.ValidateMembers([other], 1, 3), e => e.Contains("Ajeno"));
    }

    [Theory]
    [InlineData(100, null, false)]
    [InlineData(100, 100, false)]
    [InlineData(101, 100, true)]
    [InlineData(0, 0, false)]
    public void Quota_IsExceededOnlyAboveTheQuota(int accepted, int? quota, bool expected) =>
        Assert.Equal(expected, BillingAccountPolicy.IsQuotaExceeded(accepted, quota));
}
```

- [ ] **Step 2: Correr y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountPolicyTests" 2>&1 | grep -E "error" | sort -u | head -3`
Expected: error de compilación (`BillingAccountPolicy` no existe).

- [ ] **Step 3: Crear la política**

`ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas para crear y editar cuentas de facturación. Funciones puras.</summary>
public static class BillingAccountPolicy
{
    public const int NameMaxLength = 100;
    public const int MaxPaidMonths = 60;

    public static List<string> ValidateAccount(
        string? name,
        Plan? plan,
        decimal? customMonthlyFee,
        int paidMonths,
        decimal prepaymentDiscountPercent,
        int paymentGraceDays)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(name))
            errors.Add("El nombre de la cuenta es requerido.");
        else if (name.Trim().Length > NameMaxLength)
            errors.Add($"El nombre no puede exceder {NameMaxLength} caracteres.");

        if (plan == null)
            errors.Add("El plan seleccionado no existe.");
        else
        {
            if (plan.PlanTypeId != (int)PlanTypeEnum.Documents)
                errors.Add("Solo los planes por comprobantes se pueden asignar a una cuenta.");
            if (plan.StatusId != (int)StatusEnum.Active)
                errors.Add("El plan seleccionado no está activo.");
        }

        if (customMonthlyFee < 0)
            errors.Add("El precio propio no puede ser negativo.");

        if (paidMonths < 1 || paidMonths > MaxPaidMonths)
            errors.Add($"Los meses a pagar deben estar entre 1 y {MaxPaidMonths}.");

        if (prepaymentDiscountPercent < 0 || prepaymentDiscountPercent > 100)
            errors.Add("El descuento por pago adelantado debe estar entre 0 y 100.");

        if (paymentGraceDays < 1 || paymentGraceDays > PaymentReminderPolicy.MaxGraceDays)
            errors.Add($"Los días de gracia deben estar entre 1 y {PaymentReminderPolicy.MaxGraceDays}.");

        return errors;
    }

    public static List<string> ValidateOwner(bool ownerExists, bool ownerIsAdmin, bool ownerAlreadyHasAccount)
    {
        var errors = new List<string>();

        if (!ownerExists)
            errors.Add("El usuario dueño no existe.");
        else
        {
            if (!ownerIsAdmin)
                errors.Add("El dueño de la cuenta debe tener el rol Admin.");
            if (ownerAlreadyHasAccount)
                errors.Add("El usuario ya tiene una cuenta de facturación.");
        }

        return errors;
    }

    /// <summary>Los clientes a asignar deben existir y estar libres o ya en esta misma cuenta.</summary>
    public static List<string> ValidateMembers(IReadOnlyCollection<Client> found, int requestedCount, int accountId)
    {
        var errors = new List<string>();

        if (found.Count != requestedCount)
            errors.Add("Alguno de los clientes seleccionados no existe.");

        foreach (var client in found.Where(c => c.BillingAccountId != null && c.BillingAccountId != accountId))
            errors.Add($"El cliente {client.Name} ya pertenece a otra cuenta.");

        return errors;
    }

    /// <summary>El cupo es informativo: solo sirve para mostrar una alerta, nunca bloquea la emisión.</summary>
    public static bool IsQuotaExceeded(int acceptedDocuments, int? quota) =>
        quota is int limit && acceptedDocuments > limit;
}
```

- [ ] **Step 4: DTOs**

`ZynstormECFPlatform.Dtos/BillingAccountDtos.cs`:

```csharp
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Alta y edición de una cuenta. <c>OwnerUserId</c> solo se usa al crear.</summary>
public class BillingAccountSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public int PlanId { get; set; }
    public decimal? CustomMonthlyFee { get; set; }
    public int PaidMonths { get; set; } = 1;
    public decimal PrepaymentDiscountPercent { get; set; }
    public int PaymentGraceDays { get; set; } = 3;

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }
}

public class BillingAccountClientsDto
{
    public List<string> ClientGuidIds { get; set; } = [];
}

public class BillingAccountClientUsageDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int? QuotaDocuments { get; set; }
    public bool QuotaExceeded { get; set; }
}

public class BillingAccountViewDto
{
    public string GuidId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int MonthlyDocumentLimit { get; set; }
    public decimal MonthlyFee { get; set; }
    public decimal? CustomMonthlyFee { get; set; }
    public int StatusId { get; set; }
    public int ClientsCount { get; set; }

    // Pool del mes en curso
    public int CurrentMonthAcceptedDocuments { get; set; }
    public int CurrentMonthOverageDocuments { get; set; }
    public decimal CurrentMonthOverageAmount { get; set; }

    // Cobro
    public int PaidMonths { get; set; }
    public decimal PrepaymentDiscountPercent { get; set; }
    public int PaymentGraceDays { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }

    public int PaymentStatus { get; set; }
    public int? PaymentDaysToDue { get; set; }
    public bool PaymentSuspended { get; set; }
    public decimal PendingOverageAmount { get; set; }
    public int PendingOverageMonths { get; set; }
}

public class BillingAccountDetailDto : BillingAccountViewDto
{
    public List<BillingAccountClientUsageDto> Clients { get; set; } = [];
}
```

- [ ] **Step 5: Correr las pruebas**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountPolicyTests" 2>&1 | grep -E "error|Passed!|Failed" | sort -u`
Expected: `Passed!  - Failed: 0`.

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services/Billing/BillingAccountPolicy.cs ZynstormECFPlatform.Dtos/BillingAccountDtos.cs ZynstormECFPlatform.Tests/Billing/BillingAccountPolicyTests.cs && git commit -q -m "$(cat <<'EOF'
Validaciones y DTOs para administrar cuentas de facturación

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 9: `BillingAccountController` (solo SA)

**Files:**
- Create: `ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`

**Interfaces:**
- Consumes: todo lo anterior.
- Produces (rutas bajo `v1/BillingAccount`, rol SA):
  - `GET ""` → `List<BillingAccountViewDto>`
  - `GET guid/{guid}` → `BillingAccountDetailDto`
  - `POST ""` (`BillingAccountSaveDto`) → `BillingAccountDetailDto`
  - `PUT guid/{guid}` (`BillingAccountSaveDto`) → `BillingAccountDetailDto`
  - `PUT guid/{guid}/clients` (`BillingAccountClientsDto`) → `BillingAccountDetailDto`
  - `GET guid/{guid}/payments/preview` → `AccountPaymentPreviewDto`
  - `POST guid/{guid}/payments` (`RegisterPaymentRequestDto`) → `PaymentReceiptDto`
  - `GET guid/{guid}/payments` → `List<PaymentReceiptDto>`

Esta clase depende de EF y no se prueba con Moq (ver Global Constraints); se verifica en la Task 10.

- [ ] **Step 1: Escribir el controlador**

`ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs`:

```csharp
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>Cuentas de facturación: un plan de comprobantes compartido por los clientes de un revendedor.</summary>
[Authorize(Roles = "SA")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BillingAccountController(
    IBillingAccountService accountService,
    IBillingAccountPaymentService paymentService,
    IBillingAccountMonthlyUsageService usageService,
    IClientMonthlyUsageService clientUsageService,
    IClientService clientService,
    IPlanService planService,
    IPlanOverageTierService tierService,
    IAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ILogger<BillingAccountController> logger) : ControllerBase
{
    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private IQueryable<BillingAccount> AccountsWithDetails() =>
        accountService.Table.AsNoTracking().Include(a => a.Plan).Include(a => a.OwnerUser);

    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await AccountsWithDetails().OrderBy(a => a.Name).ToListAsync(cancellationToken);
            return Ok(await BuildViewsAsync(accounts, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error listando las cuentas de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}")]
    public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await BuildDetailAsync(guid, cancellationToken);
            return detail == null ? NotFound(new { message = "Cuenta no encontrada." }) : Ok(detail);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);

            var owner = string.IsNullOrWhiteSpace(dto.OwnerUserId) ? null : await userAccountService.GetUserByIdAsync(dto.OwnerUserId);
            var ownerIsAdmin = owner != null && await userAccountService.IsUserInRoleAsync(owner, "Admin");
            var ownerHasAccount = owner != null && await accountService.Table.AnyAsync(a => a.OwnerUserId == owner.Id, cancellationToken);
            errors.AddRange(BillingAccountPolicy.ValidateOwner(owner != null, ownerIsAdmin, ownerHasAccount));

            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var account = new BillingAccount
            {
                Name = dto.Name.Trim(),
                OwnerUserId = owner!.Id,
                PlanId = plan!.PlanId,
                StatusId = (int)StatusEnum.Active,
                CustomMonthlyFee = dto.CustomMonthlyFee,
                OfferStartDate = OfferPricing.ResolveStartDate(
                    null, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths), null, DateTimeExtensions.DrNow.Date),
                PaidMonths = dto.PaidMonths,
                PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent,
                PaymentGraceDays = dto.PaymentGraceDays,
                LastPaymentDate = dto.LastPaymentDate?.Date,
                NextPaymentDate = dto.NextPaymentDate?.Date
            };

            var inserted = await accountService.InsertAsync(account);
            return Ok(await BuildDetailAsync(inserted!.GuidId, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return Conflict(new { message = "El usuario ya tiene una cuenta de facturación." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("guid/{guid}")]
    public async Task<IActionResult> Update(string guid, [FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.Include(a => a.Plan).FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var previousPlanId = account.PlanId;
            account.Name = dto.Name.Trim();
            account.PlanId = plan!.PlanId;
            account.CustomMonthlyFee = dto.CustomMonthlyFee;
            account.OfferStartDate = OfferPricing.ResolveStartDate(
                previousPlanId, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths),
                account.OfferStartDate, DateTimeExtensions.DrNow.Date);
            account.PaidMonths = dto.PaidMonths;
            account.PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent;
            account.PaymentGraceDays = dto.PaymentGraceDays;
            account.LastPaymentDate = dto.LastPaymentDate?.Date;
            account.NextPaymentDate = dto.NextPaymentDate?.Date;

            // Como en un cliente: poner una fecha de pago futura reactiva la cuenta suspendida.
            if (account.PaymentSuspendedAtUtc != null && account.NextPaymentDate > DateTimeExtensions.DrNow.Date)
                account.PaymentSuspendedAtUtc = null;

            await accountService.UpdateAsync(account);
            return Ok(await BuildDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error actualizando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Deja como miembros exactamente los clientes indicados: asigna los nuevos y suelta a los que ya no figuran.
    /// El PlanId propio de un cliente asignado se conserva pero deja de cobrarse.
    /// </summary>
    [HttpPut("guid/{guid}/clients")]
    public async Task<IActionResult> SetClients(string guid, [FromBody] BillingAccountClientsDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var requested = dto.ClientGuidIds.Distinct().ToList();
            var affected = await clientService.Table
                .Where(c => requested.Contains(c.GuidId) || c.BillingAccountId == account.BillingAccountId)
                .ToListAsync(cancellationToken);

            var requestedClients = affected.Where(c => requested.Contains(c.GuidId)).ToList();
            var errors = BillingAccountPolicy.ValidateMembers(requestedClients, requested.Count, account.BillingAccountId);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            foreach (var client in affected)
                client.BillingAccountId = requested.Contains(client.GuidId) ? account.BillingAccountId : null;

            await unitOfWork.ExecuteInTransactionAsync(async _ => await clientService.UpdateAsync(affected), cancellationToken);

            return Ok(await BuildDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error asignando clientes a la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}/payments/preview")]
    public async Task<IActionResult> GetPaymentPreview(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetPreviewAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el pago a registrar de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el pago a registrar." });
        }
    }

    [HttpPost("guid/{guid}/payments")]
    public async Task<IActionResult> RegisterPayment(string guid, [FromBody] RegisterPaymentRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.RegisterAsync(guid, dto, CurrentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error registrando el pago de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo registrar el pago." });
        }
    }

    [HttpGet("guid/{guid}/payments")]
    public async Task<IActionResult> GetPaymentHistory(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetHistoryAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el historial de pagos de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el historial de pagos." });
        }
    }

    private IActionResult ToActionResult<T>(PaymentRegistrationResult<T> result) => result.Outcome switch
    {
        PaymentRegistrationOutcome.Ok => Ok(result.Value),
        PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
        PaymentRegistrationOutcome.Conflict => Conflict(new { message = string.Join(" ", result.Errors), errors = result.Errors }),
        _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
    };

    private async Task<BillingAccountDetailDto?> BuildDetailAsync(string guid, CancellationToken cancellationToken)
    {
        var account = await AccountsWithDetails().FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
        if (account == null) return null;

        var view = (await BuildViewsAsync([account], cancellationToken)).Single();
        var today = DateTimeExtensions.DrNow;

        var clients = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId == account.BillingAccountId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var clientIds = clients.Select(c => c.ClientId).ToList();
        var accepted = await clientUsageService.Table.AsNoTracking()
            .Where(u => clientIds.Contains(u.ClientId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.ClientId, u => u.AcceptedDocuments, cancellationToken);

        var detail = new BillingAccountDetailDto();
        CopyView(view, detail);
        detail.Clients = clients.Select(c =>
        {
            var documents = accepted.GetValueOrDefault(c.ClientId);
            return new BillingAccountClientUsageDto
            {
                ClientGuidId = c.GuidId,
                ClientName = c.Name,
                ClientRnc = c.Rnc,
                AcceptedDocuments = documents,
                QuotaDocuments = c.QuotaDocuments,
                QuotaExceeded = BillingAccountPolicy.IsQuotaExceeded(documents, c.QuotaDocuments)
            };
        }).ToList();

        return detail;
    }

    private async Task<List<BillingAccountViewDto>> BuildViewsAsync(IReadOnlyList<BillingAccount> accounts, CancellationToken cancellationToken)
    {
        if (accounts.Count == 0) return [];

        var today = DateTimeExtensions.DrNow;
        var ids = accounts.Select(a => a.BillingAccountId).ToList();
        var planIds = accounts.Select(a => a.PlanId).Distinct().ToList();

        var poolNow = await usageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.BillingAccountId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.BillingAccountId, u => u.AcceptedDocuments, cancellationToken);

        var tiers = await tierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        var clientCounts = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId != null && ids.Contains(c.BillingAccountId.Value))
            .GroupBy(c => c.BillingAccountId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var pending = await paymentService.GetPendingOveragesAsync(ids, cancellationToken);

        return accounts.Select(a =>
        {
            var terms = BillingTerms.For(a);
            var documents = poolNow.GetValueOrDefault(a.BillingAccountId);
            var calculation = BillingCalculator.Calculate(
                PaymentCalculator.GetMonthlyFee(terms),
                a.Plan.MonthlyDocumentLimit,
                tiers.Where(t => t.PlanId == a.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                documents);
            var (status, days) = PaymentCalculator.GetPaymentStatus(a.NextPaymentDate, PaymentCalculator.DefaultWarningDays);
            var accountPending = pending.GetValueOrDefault(a.BillingAccountId);

            return new BillingAccountViewDto
            {
                GuidId = a.GuidId,
                Name = a.Name,
                OwnerUserId = a.OwnerUserId,
                OwnerName = a.OwnerUser?.FullName ?? string.Empty,
                OwnerEmail = a.OwnerUser?.Email,
                PlanId = a.PlanId,
                PlanName = a.Plan.Name,
                MonthlyDocumentLimit = a.Plan.MonthlyDocumentLimit,
                MonthlyFee = PaymentCalculator.GetMonthlyFee(terms),
                CustomMonthlyFee = a.CustomMonthlyFee,
                StatusId = a.StatusId,
                ClientsCount = clientCounts.GetValueOrDefault(a.BillingAccountId),
                CurrentMonthAcceptedDocuments = documents,
                CurrentMonthOverageDocuments = calculation.OverageDocuments,
                CurrentMonthOverageAmount = calculation.OverageAmount,
                PaidMonths = a.PaidMonths,
                PrepaymentDiscountPercent = a.PrepaymentDiscountPercent,
                PaymentGraceDays = PaymentReminderPolicy.NormalizeGraceDays(a.PaymentGraceDays),
                LastPaymentDate = a.LastPaymentDate,
                NextPaymentDate = a.NextPaymentDate,
                PaymentStatus = (int)status,
                PaymentDaysToDue = days,
                PaymentSuspended = a.PaymentSuspendedAtUtc != null,
                PendingOverageAmount = accountPending?.Sum(p => p.Amount) ?? 0m,
                PendingOverageMonths = accountPending?.Count ?? 0
            };
        }).ToList();
    }

    private static void CopyView(BillingAccountViewDto from, BillingAccountViewDto to)
    {
        foreach (var property in typeof(BillingAccountViewDto).GetProperties().Where(p => p.CanRead && p.CanWrite))
            property.SetValue(to, property.GetValue(from));
    }
}
```

Notas para quien implemente:
- `OfferPricing.ResolveStartDate(previousPlanId, newPlanId, newPlanHasOffer, currentStart, today)` ya existe y se usa igual en `ClientController`.
- `IUnitOfWork` vive en `ZynstormECFPlatform.Abstractions.Data`; `IPlanOverageTierService` y `IClientMonthlyUsageService` ya existen en `Abstractions/DataServices`.
- `clientService.UpdateAsync(IEnumerable<Client>)` está en `IRepository<T>`.
- `CopyView` usa reflexión para no repetir 25 asignaciones; si prefieres, cámbialo por una asignación explícita.

- [ ] **Step 2: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u | head -8`
Expected: `Build succeeded.` Si falla por un nombre de interfaz o un `using`, corrígelo con los nombres reales (`grep -rn "interface IPlanOverageTierService" ZynstormECFPlatform.Abstractions`).

- [ ] **Step 3: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Web.Api/Controllers/BillingAccountController.cs && git commit -q -m "$(cat <<'EOF'
API del SA para cuentas de facturación: alta, clientes y pagos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_01KwmBdjbXV1HtuTLn8ZNe8V
EOF
)"
```

---

### Task 10: Verificación final

**Files:** ninguno.

- [ ] **Step 1: Compilar todo y correr la suite completa**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | grep -E " error |Build succeeded" | sort -u` y `dotnet test ZynstormECFPlatform.Tests 2>&1 | grep -E " error |Passed!|Failed!|Failed Z" | sort -u`
Expected: `Build succeeded.` y la única falla es `AprobacionComercialResultTests.Codigo1_EsAprobacionComercialAceptada` (ya fallaba antes de la fase 1).

- [ ] **Step 2: Pedir permiso al usuario para tocar la base**

Pregunta al usuario, con la base **dev2** (`zynstorm_ecf_platform_dev2_db`, la misma que usa Staging) como destino propuesto, si aplicas las dos migraciones pendientes (`AddBillingAccountPayments`; `AddBillingAccounts` ya está aplicada) y corres el flujo de abajo. Nunca uses la cadena activa de `appsettings.Development.json` (producción). Si el usuario no autoriza, salta al Step 4.

- [ ] **Step 3: Verificación de punta a punta (con permiso)**

Aplicar: `ConnectionStrings__DefaultConnection=<cadena dev2> dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api --no-build` (confirma primero con `dotnet ef dbcontext info` que el `Database name` es `zynstorm_ecf_platform_dev2_db`).

Levantar la API local con la misma variable y, desde Swagger como SA, comprobar:
1. `POST /v1/BillingAccount` con un usuario Admin y un plan `Documents` → 200 con la cuenta. Repetir con el mismo dueño → 409/400 ("ya tiene una cuenta").
2. `PUT /v1/BillingAccount/guid/{guid}/clients` con dos clientes → `ClientsCount` = 2. Con un cliente de otra cuenta → 400.
3. Emitir un e-CF de cada cliente y consultar `GET /v1/BillingAccount/guid/{guid}`: `CurrentMonthAcceptedDocuments` sube 2 y cada cliente muestra su consumo.
4. `GET .../payments/preview` → mensualidad correcta; `POST .../payments` con `IncludePlanFee = true` → recibo con `IsAccount = true` y `NextPaymentDate` avanzada.
5. Poner `NextPaymentDate` en el pasado con `PUT` y correr `PaymentSuspensionJob` (Hangfire o `SuspendExpiredAccountsAsync`) pasado el período de gracia → la cuenta queda `PaymentSuspended = true` y la emisión de sus clientes responde 403 con `AccountSuspendedMessage`. Registrar un pago reactiva la cuenta y la emisión vuelve.
6. Un cliente de la cuenta ya no aparece en `GET /v1/Client/payments` y `POST /v1/Client/guid/{guid}/payments` responde que pertenece a una cuenta.

- [ ] **Step 4: Reportar al usuario**

Informar: resultado de la suite, si se verificó contra dev2 (y qué pasos), y lo que queda para las fases siguientes: permisos y validaciones de `UserController` (fase 3), pantallas `/cuenta`, `consumo` y `pagos` (fase 4), y la réplica a `Staging` y `feature/emit-idempotente` al terminar las 4 fases. Recordar que `Program.cs` ejecuta `Migrate()` al arrancar, así que al desplegar las migraciones se aplicarán solas en cada ambiente.
