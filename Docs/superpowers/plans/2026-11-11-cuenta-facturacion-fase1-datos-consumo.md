# Cuenta de facturación con pool — Fase 1: datos y consumo

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que los comprobantes aceptados de los clientes de una `BillingAccount` se sumen en un total mensual de la cuenta (el pool), sin cambiar nada para los clientes que no tienen cuenta.

**Architecture:** Dos tablas nuevas (`BillingAccount`, `BillingAccountMonthlyUsage`) y dos columnas nuevas en `Client` (`BillingAccountId`, `QuotaDocuments`). `ClientUsageService.RegisterAcceptedAsync` bifurca: sin cuenta, el SQL actual; con cuenta, un SQL nuevo que en una sola sentencia marca el documento y suma en la fila del cliente y en la fila de la cuenta.

**Tech Stack:** .NET 10, EF Core + Npgsql (PostgreSQL), Dapper (vía `Repository.ExecuteAsync`), xUnit + Moq.

Spec: `Docs/superpowers/specs/2026-11-11-cuenta-facturacion-pool-design.md`.

## Global Constraints

- Todo cambio de esquema es nullable y aditivo; los clientes y planes existentes no cambian (spec §4).
- `BillingCalculator` no se modifica en esta fase.
- Una cuenta tiene un solo plan y un solo dueño; un usuario es dueño de **una sola** cuenta (índice único sobre `OwnerUserId`).
- Un plan `Rent` no acumula consumo: `BillingCalculator.AccruesDocumentUsage(plan.PlanTypeId)` se aplica también al plan de la cuenta.
- La fila de `ClientMonthlyUsage` de un cliente con cuenta guarda `PlanId` null, `MonthlyFee` 0, `MonthlyDocumentLimit` -1 y el nombre del plan de la cuenta en `PlanName`. El cobro sale solo de `BillingAccountMonthlyUsage`.
- La idempotencia sigue en `EcfDocument.BillingCountedAtUtc IS NULL`: un documento nunca se cuenta dos veces.
- Cupo informativo (`Client.QuotaDocuments`) no bloquea nada.
- Los índices `IX_ClientMonthlyUsage_ClientId_Year_Month` e `IX_BillingAccountMonthlyUsage_AccountId_Year_Month` no llevan filtro (los exige `ON CONFLICT`).
- Fuera de esta fase: pagos de cuenta (`ClientPayment`/`ClientPaymentItem`), suspensión, recordatorios, validaciones de `UserController`, endpoints y pantallas. No hay API para crear cuentas todavía; en esta fase se crean con SQL (Task 5).
- Mensajes de commit en español, con la línea `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>` al final.
- Antes del Task 1, confirma con el usuario en qué rama se trabaja (el repo estaba en `dev`; el usuario cambia de rama). Cada comando de commit imprime la rama con `git branch --show-current`.

## Estructura de archivos

| Archivo | Acción | Responsabilidad |
|---|---|---|
| `ZynstormECFPlatform.Core/Entities/BillingAccount.cs` | Crear | Entidad de la cuenta (plan, dueño, datos de pago) |
| `ZynstormECFPlatform.Core/Entities/BillingAccountMonthlyUsage.cs` | Crear | Total mensual del pool + snapshot del plan |
| `ZynstormECFPlatform.Core/Entities/Client.cs` | Modificar | `BillingAccountId`, `QuotaDocuments`, navegación |
| `ZynstormECFPlatform.Data/StorageContext.cs` | Modificar | Mapeo EF de las dos tablas y del FK en `Client` |
| `ZynstormECFPlatform.Data/Migrations/*_AddBillingAccounts.cs` | Generar | Migración |
| `ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountService.cs` | Crear | Interfaz del repositorio |
| `ZynstormECFPlatform.Data.Services/BillingAccountService.cs` | Crear | Repositorio (se registra solo por convención de nombre) |
| `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs` | Modificar | SQL nuevo + bifurcación cliente/cuenta |
| `ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs` | Crear | Pruebas del modelo EF sin base de datos |
| `ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs` | Modificar | Pruebas del SQL de cuenta |
| `ZynstormECFPlatform.Tests/Billing/ClientUsageServiceTests.cs` | Crear | Pruebas de la bifurcación con Moq |

Todos los comandos se corren desde `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`.

---

### Task 1: Entidades y mapeo EF

**Files:**
- Create: `ZynstormECFPlatform.Core/Entities/BillingAccount.cs`
- Create: `ZynstormECFPlatform.Core/Entities/BillingAccountMonthlyUsage.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs` (después del bloque `ClientMonthlyUsage`, ~línea 680; y el segundo bloque `Entity<Client>`, ~línea 2065)
- Test: `ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs`

**Interfaces:**
- Produces:
  - `BillingAccount` con `int BillingAccountId`, `string Name`, `string OwnerUserId`, `int PlanId`, `int StatusId`, `decimal? CustomMonthlyFee`, `DateTime? OfferStartDate`, `DateTime? LastPaymentDate`, `DateTime? NextPaymentDate`, `int PaidMonths`, `decimal PrepaymentDiscountPercent`, `int PaymentGraceDays`, `DateTime? FirstPaymentReminderSentFor`, `DateTime? FinalPaymentReminderSentFor`, `DateTime? PaymentSuspendedAtUtc`; navegaciones `Plan`, `Status`, `OwnerUser`, `Clients`, `MonthlyUsages`.
  - `BillingAccountMonthlyUsage` con `int BillingAccountMonthlyUsageId`, `int BillingAccountId`, `int Year`, `int Month`, `int AcceptedDocuments`, `int? PlanId`, `string PlanName`, `decimal MonthlyFee`, `int MonthlyDocumentLimit`.
  - `Client.BillingAccountId` (`int?`), `Client.QuotaDocuments` (`int?`), `Client.BillingAccount`.

- [ ] **Step 1: Escribir la prueba del modelo (falla porque los tipos no existen)**

Crear `ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingAccountModelTests
{
    // Construir el modelo no abre conexión: la cadena solo selecciona el proveedor Npgsql.
    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<StorageContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x")
            .Options;
        using var context = new StorageContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void BillingAccountMonthlyUsage_HasUnconditionalUniqueIndexOnAccountYearMonth()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccountMonthlyUsage))!;

        var index = Assert.Single(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["BillingAccountId", "Year", "Month"]));

        Assert.True(index.IsUnique);
        Assert.Null(index.GetFilter());
        Assert.Equal("IX_BillingAccountMonthlyUsage_AccountId_Year_Month", index.GetDatabaseName());
    }

    [Fact]
    public void BillingAccount_AllowsOnlyOneActiveAccountPerOwner()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccount))!;

        var index = Assert.Single(entity.GetIndexes(), i =>
            i.Properties.Select(p => p.Name).SequenceEqual(["OwnerUserId"]));

        Assert.True(index.IsUnique);
        Assert.NotNull(index.GetFilter());
    }

    [Fact]
    public void Client_BillingAccountIdAndQuota_AreOptional()
    {
        var entity = BuildModel().FindEntityType(typeof(Client))!;

        Assert.True(entity.FindProperty(nameof(Client.BillingAccountId))!.IsNullable);
        Assert.True(entity.FindProperty(nameof(Client.QuotaDocuments))!.IsNullable);
        Assert.Contains(entity.GetForeignKeys(), fk =>
            fk.PrincipalEntityType.ClrType == typeof(BillingAccount)
            && fk.DeleteBehavior == DeleteBehavior.SetNull);
    }

    [Fact]
    public void BillingAccount_PaymentDefaultsMatchClient()
    {
        var entity = BuildModel().FindEntityType(typeof(BillingAccount))!;

        Assert.Equal(1, entity.FindProperty(nameof(BillingAccount.PaidMonths))!.GetDefaultValue());
        Assert.Equal(3, entity.FindProperty(nameof(BillingAccount.PaymentGraceDays))!.GetDefaultValue());
    }
}
```

- [ ] **Step 2: Correr la prueba y verificar que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountModelTests" 2>&1 | tail -15`
Expected: error de compilación `The type or namespace name 'BillingAccountMonthlyUsage' could not be found`.

- [ ] **Step 3: Crear las entidades**

`ZynstormECFPlatform.Core/Entities/BillingAccount.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Cuenta de un revendedor: tiene un plan de comprobantes cuyo total mensual comparten todos sus clientes.
/// Los campos de pago son los mismos que <see cref="Client"/> tiene para un cliente suelto.
/// </summary>
public partial class BillingAccount : BaseEntity
{
    public int BillingAccountId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Usuario Admin dueño de la cuenta. Una cuenta por usuario.</summary>
    public string OwnerUserId { get; set; } = null!;

    public int PlanId { get; set; }

    public int StatusId { get; set; }

    /// <summary>Precio mensual pactado. Reemplaza el precio del plan y su oferta. Null = usa el precio del plan.</summary>
    public decimal? CustomMonthlyFee { get; set; }

    /// <summary>Fecha calendario en que empezó la oferta del plan para esta cuenta. Null = sin oferta.</summary>
    public DateTime? OfferStartDate { get; set; }

    public DateTime? LastPaymentDate { get; set; }

    public DateTime? NextPaymentDate { get; set; }

    /// <summary>Meses que cubre el pago registrado. Por defecto 1.</summary>
    public int PaidMonths { get; set; } = 1;

    /// <summary>Descuento por pago adelantado, en porcentaje (0–100).</summary>
    public decimal PrepaymentDiscountPercent { get; set; }

    /// <summary>Días después de la fecha de pago para pagar antes de la suspensión. Por defecto 3.</summary>
    public int PaymentGraceDays { get; set; } = 3;

    public DateTime? FirstPaymentReminderSentFor { get; set; }

    public DateTime? FinalPaymentReminderSentFor { get; set; }

    /// <summary>Momento en que la cuenta fue suspendida por falta de pago. Null = no suspendida.</summary>
    public DateTime? PaymentSuspendedAtUtc { get; set; }

    public virtual Plan Plan { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Client> Clients { get; set; } = [];

    public virtual ICollection<BillingAccountMonthlyUsage> MonthlyUsages { get; set; } = [];
}
```

`ZynstormECFPlatform.Core/Entities/BillingAccountMonthlyUsage.cs`:

```csharp
namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Total mensual de comprobantes aceptados de todos los clientes de una cuenta (el pool).</summary>
public partial class BillingAccountMonthlyUsage : BaseEntity
{
    public int BillingAccountMonthlyUsageId { get; set; }

    public int BillingAccountId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int AcceptedDocuments { get; set; }

    // Snapshot del plan de la cuenta al crear la fila del mes
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal MonthlyFee { get; set; }

    public int MonthlyDocumentLimit { get; set; }

    public virtual BillingAccount BillingAccount { get; set; } = null!;

    public virtual Plan? Plan { get; set; }
}
```

En `ZynstormECFPlatform.Core/Entities/Client.cs`, justo después de la propiedad `PlanId` (línea ~25), agregar:

```csharp
    /// <summary>Cuenta de facturación (pool) a la que pertenece. Si tiene valor, factura contra el pool y <see cref="PlanId"/> se ignora. Null = cliente suelto.</summary>
    public int? BillingAccountId { get; set; }

    /// <summary>Cupo mensual informativo dentro del pool. No bloquea la emisión. Null = sin cupo.</summary>
    public int? QuotaDocuments { get; set; }
```

y junto a `public virtual Plan? Plan { get; set; }` agregar:

```csharp
    public virtual BillingAccount? BillingAccount { get; set; }
```

- [ ] **Step 4: Mapear en `StorageContext`**

En `ZynstormECFPlatform.Data/StorageContext.cs`, justo después del bloque `modelBuilder.Entity<ClientMonthlyUsage>(...)` (termina antes de `modelBuilder.Entity<ClientPayment>`), insertar:

```csharp
        modelBuilder.Entity<BillingAccount>(entity =>
        {
            entity.HasKey(e => e.BillingAccountId);

            entity.Property(e => e.Name)
                  .HasMaxLength(100)
                  .IsUnicode(false)
                  .IsRequired();

            entity.Property(e => e.OwnerUserId)
                  .HasMaxLength(450)
                  .IsUnicode(false)
                  .IsRequired();

            // Una cuenta por dueño; las eliminadas no cuentan.
            entity.HasIndex(e => e.OwnerUserId)
                  .IsUnique()
                  .HasFilter("NOT \"IsDeleted\"")
                  .HasDatabaseName("IX_BillingAccount_OwnerUserId");

            entity.Property(e => e.OfferStartDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.LastPaymentDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.NextPaymentDate).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.FirstPaymentReminderSentFor).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.FinalPaymentReminderSentFor).HasColumnType(DateTimeColumnType);
            entity.Property(e => e.PaymentSuspendedAtUtc).HasColumnType(DateTimeColumnType);

            entity.Property(e => e.PaidMonths)
                  .HasDefaultValue(1)
                  .IsRequired();

            entity.Property(e => e.PrepaymentDiscountPercent)
                  .HasPrecision(5, 2)
                  .HasDefaultValue(0m)
                  .IsRequired();

            entity.Property(e => e.PaymentGraceDays)
                  .HasDefaultValue(3)
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

            entity.HasOne(d => d.Plan)
                  .WithMany()
                  .HasForeignKey(d => d.PlanId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_BillingAccount_Plan");

            entity.HasOne(d => d.Status)
                  .WithMany()
                  .HasForeignKey(d => d.StatusId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_BillingAccount_Status");

            entity.HasOne(d => d.OwnerUser)
                  .WithMany()
                  .HasForeignKey(d => d.OwnerUserId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_BillingAccount_User");
        });

        modelBuilder.Entity<BillingAccountMonthlyUsage>(entity =>
        {
            entity.HasKey(e => e.BillingAccountMonthlyUsageId);

            // Requerido por el INSERT ... ON CONFLICT de ClientUsageService (no filtrar este índice)
            entity.HasIndex(e => new { e.BillingAccountId, e.Year, e.Month })
                  .IsUnique()
                  .HasDatabaseName("IX_BillingAccountMonthlyUsage_AccountId_Year_Month");

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

            entity.HasOne(d => d.BillingAccount)
                  .WithMany(p => p.MonthlyUsages)
                  .HasForeignKey(d => d.BillingAccountId)
                  .OnDelete(DeleteBehavior.ClientSetNull)
                  .HasConstraintName("FK_BillingAccountMonthlyUsage_BillingAccount");

            entity.HasOne(d => d.Plan)
                  .WithMany()
                  .HasForeignKey(d => d.PlanId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_BillingAccountMonthlyUsage_Plan");
        });
```

En el segundo bloque `modelBuilder.Entity<Client>(entity => { ... ClientType ... RiHtmlTemplate ... })` (~línea 2065, el que contiene `FK_Client_ClientType`), agregar al final del bloque:

```csharp
            entity.HasOne(d => d.BillingAccount)
                  .WithMany(p => p.Clients)
                  .HasForeignKey(d => d.BillingAccountId)
                  .OnDelete(DeleteBehavior.SetNull)
                  .HasConstraintName("FK_Client_BillingAccount");
```

- [ ] **Step 5: Correr la prueba y verificar que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~BillingAccountModelTests" 2>&1 | tail -15`
Expected: `Passed!  - Failed: 0, Passed: 4`.

Si `GetService<IDesignTimeModel>` no compila, reemplazar `BuildModel` por `return context.Model;`; si `FindProperty(...).GetDefaultValue()` devuelve el valor con otro tipo, ajustar el `Assert.Equal` al tipo real (`int`).

- [ ] **Step 6: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Core/Entities/BillingAccount.cs ZynstormECFPlatform.Core/Entities/BillingAccountMonthlyUsage.cs ZynstormECFPlatform.Core/Entities/Client.cs ZynstormECFPlatform.Data/StorageContext.cs ZynstormECFPlatform.Tests/Billing/BillingAccountModelTests.cs && git commit -m "$(cat <<'EOF'
Agregar entidades BillingAccount y consumo mensual del pool

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Migración

**Files:**
- Create (generados): `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddBillingAccounts.cs`, `.Designer.cs`
- Modify (generado): `ZynstormECFPlatform.Data/Migrations/StorageContextModelSnapshot.cs`

**Interfaces:**
- Consumes: el modelo de Task 1.
- Produces: tablas `BillingAccount`, `BillingAccountMonthlyUsage` y columnas `Client.BillingAccountId`, `Client.QuotaDocuments`.

- [ ] **Step 1: Generar la migración**

Run: `dotnet ef migrations add AddBillingAccounts --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api 2>&1 | tail -10`
Expected: termina en `Done.` y crea los tres archivos en `ZynstormECFPlatform.Data/Migrations`.

- [ ] **Step 2: Revisar que la migración no arrastre cambios ajenos**

Run: `git status --short && grep -n "CreateTable\|AddColumn\|DropColumn\|DropTable\|AlterColumn\|CreateIndex\|DropIndex\|AddForeignKey\|DropForeignKey" ZynstormECFPlatform.Data/Migrations/*_AddBillingAccounts.cs`

Expected: solo estos elementos en `Up`:
- `CreateTable` `BillingAccount` y `BillingAccountMonthlyUsage`
- `AddColumn` `BillingAccountId` y `QuotaDocuments` en `Client`
- `CreateIndex` `IX_BillingAccount_OwnerUserId` (con `filter: "NOT \"IsDeleted\""`), `IX_BillingAccountMonthlyUsage_AccountId_Year_Month`, y los índices por FK
- `AddForeignKey` `FK_Client_BillingAccount`

Si aparece cualquier `AlterColumn`, `DropColumn`, `DropTable` u otra tabla distinta de estas, **detente**: significa que el snapshot estaba desfasado respecto al modelo. No lo borres; repórtalo al usuario antes de seguir.

- [ ] **Step 3: Verificar que compila toda la solución y que `Down` revierte todo**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | tail -5 && grep -n "DropTable\|DropColumn\|DropForeignKey" ZynstormECFPlatform.Data/Migrations/*_AddBillingAccounts.cs`
Expected: `Build succeeded.` y en `Down`: `DropForeignKey` de `FK_Client_BillingAccount`, `DropTable` de las dos tablas, `DropColumn` de las dos columnas de `Client`.

- [ ] **Step 4: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Data/Migrations && git commit -m "$(cat <<'EOF'
Migración AddBillingAccounts

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Servicio de datos de la cuenta

**Files:**
- Create: `ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountService.cs`
- Create: `ZynstormECFPlatform.Data.Services/BillingAccountService.cs`

**Interfaces:**
- Consumes: `BillingAccount` (Task 1).
- Produces: `IBillingAccountService : IRepository<BillingAccount>` — se registra solo con `AddDataServices()` porque la clase se llama `BillingAccountService` y la interfaz `IBillingAccountService` está en `ZynstormECFPlatform.Abstractions`.

- [ ] **Step 1: Crear la interfaz**

`ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IBillingAccountService : IRepository<BillingAccount>
{
}
```

- [ ] **Step 2: Crear la implementación**

`ZynstormECFPlatform.Data.Services/BillingAccountService.cs`:

```csharp
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BillingAccountService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BillingAccount>(context, sqlGenerator), IBillingAccountService
{
}
```

- [ ] **Step 3: Compilar**

Run: `dotnet build ZynstormECFPlatform.slnx 2>&1 | tail -5`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Abstractions/DataServices/IBillingAccountService.cs ZynstormECFPlatform.Data.Services/BillingAccountService.cs && git commit -m "$(cat <<'EOF'
Agregar servicio de datos de BillingAccount

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Consumo del pool (SQL y servicio)

**Files:**
- Modify: `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs`
- Modify: `ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs`
- Create: `ZynstormECFPlatform.Tests/Billing/ClientUsageServiceTests.cs`

**Interfaces:**
- Consumes: `IBillingAccountService`, `Client.BillingAccountId`, `BillingAccount.PlanId/StatusId` (Tasks 1 y 3).
- Produces:
  - `ClientUsageSql.RegisterAcceptedInAccount` (string). Parámetros: `@Now`, `@EcfDocumentId`, `@Year`, `@Month`, `@BillingAccountId`, `@PlanId`, `@PlanName`, `@MonthlyFee`, `@MonthlyDocumentLimit`.
  - `ClientUsageService` con constructor `(IClientService, IPlanService, IBillingAccountService, IClientMonthlyUsageService, ILogger<ClientUsageService>)`. La interfaz `IClientUsageService` no cambia; el registro DI existente (`AddTransient<IClientUsageService, ClientUsageService>`) sigue valiendo.

- [ ] **Step 1: Escribir las pruebas del SQL (fallan: la constante no existe)**

Agregar al final de la clase `ClientUsageSqlTests` (antes de la llave de cierre):

```csharp
    [Fact]
    public void RegisterAcceptedInAccount_OnlyMarksDocumentsNotCountedYet()
    {
        Assert.Contains("\"BillingCountedAtUtc\" IS NULL", ClientUsageSql.RegisterAcceptedInAccount);
    }

    [Fact]
    public void RegisterAcceptedInAccount_UpsertsClientRowAndAccountRowFromTheSameMarkedDocument()
    {
        var sql = ClientUsageSql.RegisterAcceptedInAccount;

        Assert.Contains("INSERT INTO \"ClientMonthlyUsage\"", sql);
        Assert.Contains("ON CONFLICT (\"ClientId\", \"Year\", \"Month\")", sql);
        Assert.Contains("INSERT INTO \"BillingAccountMonthlyUsage\"", sql);
        Assert.Contains("ON CONFLICT (\"BillingAccountId\", \"Year\", \"Month\")", sql);
        Assert.Contains("\"AcceptedDocuments\" = \"BillingAccountMonthlyUsage\".\"AcceptedDocuments\" + 1", sql);
        Assert.Equal(2, CountOccurrences(sql, "FROM marked"));
    }

    [Fact]
    public void RegisterAcceptedInAccount_ClientRowNeverCarriesAFeeNorALimit()
    {
        // La fila del cliente es solo detalle: sin plan, sin mensualidad y con límite ilimitado (-1),
        // para que el cálculo de excedente por cliente nunca cobre nada desde ahí.
        Assert.Contains("CAST(NULL AS integer), @PlanName, 0, -1, @Now", ClientUsageSql.RegisterAcceptedInAccount);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        for (var i = text.IndexOf(value, StringComparison.Ordinal); i >= 0; i = text.IndexOf(value, i + value.Length, StringComparison.Ordinal))
            count++;
        return count;
    }
```

- [ ] **Step 2: Escribir las pruebas del servicio (fallan: el constructor no acepta `IBillingAccountService`)**

Crear `ZynstormECFPlatform.Tests/Billing/ClientUsageServiceTests.cs`:

```csharp
using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientUsageServiceTests
{
    private readonly Mock<IClientService> _clients = new();
    private readonly Mock<IPlanService> _plans = new();
    private readonly Mock<IBillingAccountService> _accounts = new();
    private readonly Mock<IClientMonthlyUsageService> _usages = new();

    private string? _executedSql;
    private object? _executedParameters;

    public ClientUsageServiceTests()
    {
        SetupExecute(rowsAffected: 1);
    }

    private ClientUsageService CreateService() => new(
        _clients.Object, _plans.Object, _accounts.Object, _usages.Object,
        NullLogger<ClientUsageService>.Instance);

    private void SetupExecute(int rowsAffected) =>
        _usages
            .Setup(u => u.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((sql, parameters, _) =>
            {
                _executedSql = sql;
                _executedParameters = parameters;
            })
            .ReturnsAsync(rowsAffected);

    private void GivenClient(int? planId, int? billingAccountId) =>
        _clients
            .Setup(c => c.GetNoTrackingByAsync(It.IsAny<Expression<Func<Client, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Client { ClientId = 7, PlanId = planId, BillingAccountId = billingAccountId });

    private void GivenPlan(Plan? plan) =>
        _plans
            .Setup(p => p.GetNoTrackingByAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

    private void GivenAccount(BillingAccount? account) =>
        _accounts
            .Setup(a => a.GetNoTrackingByAsync(It.IsAny<Expression<Func<BillingAccount, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

    private static Plan DocumentsPlan(int planId = 5, string name = "Pool 1000", decimal fee = 3000m, int limit = 1000) => new()
    {
        PlanId = planId,
        Name = name,
        MonthlyFee = fee,
        MonthlyDocumentLimit = limit,
        PlanTypeId = (int)PlanTypeEnum.Documents,
        StatusId = (int)StatusEnum.Active
    };

    private static BillingAccount Account(int planId = 5) => new()
    {
        BillingAccountId = 3,
        PlanId = planId,
        StatusId = (int)StatusEnum.Active
    };

    private T Parameter<T>(string name) =>
        (T)_executedParameters!.GetType().GetProperty(name)!.GetValue(_executedParameters)!;

    [Fact]
    public async Task ClientWithoutAccount_UsesTheClientSqlAndTheClientPlan()
    {
        GivenClient(planId: 9, billingAccountId: null);
        GivenPlan(DocumentsPlan(planId: 9, name: "Plan suelto", fee: 500m, limit: 100));

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.True(counted);
        Assert.Equal(ClientUsageSql.RegisterAccepted, _executedSql);
        Assert.Equal(9, Parameter<int>("PlanId"));
        Assert.Equal("Plan suelto", Parameter<string>("PlanName"));
        _accounts.Verify(a => a.GetNoTrackingByAsync(It.IsAny<Expression<Func<BillingAccount, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClientWithAccount_UsesTheAccountSqlAndTheAccountPlan_EvenIfTheClientHasItsOwnPlan()
    {
        GivenClient(planId: 99, billingAccountId: 3);
        GivenAccount(Account(planId: 5));
        GivenPlan(DocumentsPlan(planId: 5, name: "Pool 1000", fee: 3000m, limit: 1000));

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.True(counted);
        Assert.Equal(ClientUsageSql.RegisterAcceptedInAccount, _executedSql);
        Assert.Equal(3, Parameter<int>("BillingAccountId"));
        Assert.Equal(5, Parameter<int>("PlanId"));
        Assert.Equal("Pool 1000", Parameter<string>("PlanName"));
        Assert.Equal(3000m, Parameter<decimal>("MonthlyFee"));
        Assert.Equal(1000, Parameter<int>("MonthlyDocumentLimit"));
        Assert.Equal(1, Parameter<int>("EcfDocumentId"));
    }

    [Fact]
    public async Task ClientWithAccount_AccountMissingOrInactive_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(null);
        GivenPlan(DocumentsPlan());

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_AccountPlanMissingOrInactive_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        GivenPlan(null);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_RentPlan_DoesNotAccrueUsage()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        var rent = DocumentsPlan();
        rent.PlanTypeId = (int)PlanTypeEnum.Rent;
        GivenPlan(rent);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_DocumentAlreadyCounted_ReturnsFalse()
    {
        SetupExecute(rowsAffected: 0);
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        GivenPlan(DocumentsPlan());

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
    }

    [Fact]
    public async Task ClientWithoutPlanNorAccount_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: null);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }
}
```

- [ ] **Step 3: Correr las pruebas y verificar que fallan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientUsage" 2>&1 | tail -15`
Expected: error de compilación (`'ClientUsageSql' does not contain a definition for 'RegisterAcceptedInAccount'` y el constructor de `ClientUsageService`).

- [ ] **Step 4: Implementar**

Reemplazar el contenido completo de `ZynstormECFPlatform.Services/Billing/ClientUsageService.cs`:

```csharp
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
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

    // Igual que RegisterAccepted, pero para un cliente con cuenta: suma 1 en la fila del cliente
    // (solo detalle: sin plan, mensualidad 0 y límite -1) y 1 en la fila de la cuenta (el pool, que es
    // lo que se cobra). Las dos escrituras salen del mismo documento marcado y se ejecutan siempre:
    // PostgreSQL corre un CTE con INSERT aunque la consulta principal no lo lea.
    public const string RegisterAcceptedInAccount = """
        WITH marked AS (
            UPDATE "EcfDocument"
            SET "BillingCountedAtUtc" = @Now
            WHERE "EcfDocumentId" = @EcfDocumentId AND "BillingCountedAtUtc" IS NULL
            RETURNING "ClientId"
        ),
        client_usage AS (
            INSERT INTO "ClientMonthlyUsage"
                ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
            SELECT "ClientId", @Year, @Month, 1, CAST(NULL AS integer), @PlanName, 0, -1, @Now
            FROM marked
            ON CONFLICT ("ClientId", "Year", "Month")
            DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                          "LastUpdateUtc" = EXCLUDED."LastUpdateUtc"
            RETURNING "ClientId"
        )
        INSERT INTO "BillingAccountMonthlyUsage"
            ("BillingAccountId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
        SELECT @BillingAccountId, @Year, @Month, 1, @PlanId, @PlanName, @MonthlyFee, @MonthlyDocumentLimit, @Now
        FROM marked
        ON CONFLICT ("BillingAccountId", "Year", "Month")
        DO UPDATE SET "AcceptedDocuments" = "BillingAccountMonthlyUsage"."AcceptedDocuments" + 1,
                      "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";
        """;
}

public class ClientUsageService(
    IClientService clientService,
    IPlanService planService,
    IBillingAccountService billingAccountService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    ILogger<ClientUsageService> logger) : IClientUsageService
{
    public async Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await clientService.GetNoTrackingByAsync(c => c.ClientId == clientId, cancellationToken);
            if (client == null)
                return false;

            // Un cliente con cuenta factura contra el pool; su PlanId propio se ignora.
            if (client.BillingAccountId is int billingAccountId)
                return await RegisterInAccountAsync(ecfDocumentId, billingAccountId, cancellationToken);

            return await RegisterForClientAsync(ecfDocumentId, client, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registrando consumo mensual. EcfDocumentId {EcfDocumentId}, ClientId {ClientId}", ecfDocumentId, clientId);
            return false;
        }
    }

    private async Task<bool> RegisterForClientAsync(int ecfDocumentId, Client client, CancellationToken cancellationToken)
    {
        if (client.PlanId is not int planId)
            return false;

        var plan = await GetActiveAccruingPlanAsync(planId, cancellationToken);
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

    private async Task<bool> RegisterInAccountAsync(int ecfDocumentId, int billingAccountId, CancellationToken cancellationToken)
    {
        var account = await billingAccountService.GetNoTrackingByAsync(
            a => a.BillingAccountId == billingAccountId && a.StatusId == (int)StatusEnum.Active, cancellationToken);
        if (account == null)
            return false;

        var plan = await GetActiveAccruingPlanAsync(account.PlanId, cancellationToken);
        if (plan == null)
            return false;

        var drNow = DateTimeExtensions.DrNow;
        var affected = await clientMonthlyUsageService.ExecuteAsync(ClientUsageSql.RegisterAcceptedInAccount, new
        {
            Now = DateTime.UtcNow,
            EcfDocumentId = ecfDocumentId,
            Year = drNow.Year,
            Month = drNow.Month,
            BillingAccountId = account.BillingAccountId,
            PlanId = plan.PlanId,
            PlanName = plan.Name,
            MonthlyFee = plan.MonthlyFee,
            MonthlyDocumentLimit = plan.MonthlyDocumentLimit
        }, cancellationToken);

        return affected > 0;
    }

    /// <summary>Plan activo que acumula consumo. Los planes de renta no acumulan consumo mensual ni excedente.</summary>
    private async Task<Plan?> GetActiveAccruingPlanAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await planService.GetNoTrackingByAsync(
            p => p.PlanId == planId && p.StatusId == (int)StatusEnum.Active, cancellationToken);

        return plan != null && BillingCalculator.AccruesDocumentUsage(plan.PlanTypeId) ? plan : null;
    }
}
```

- [ ] **Step 5: Correr las pruebas de consumo y verificar que pasan**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~ClientUsage" 2>&1 | tail -15`
Expected: `Passed!  - Failed: 0` (3 pruebas originales + 3 nuevas de SQL + 7 del servicio = 13).

Si Moq falla por ambigüedad entre las sobrecargas de `ExecuteAsync` (`IDbTransaction` vs `CancellationToken`), el lambda de `SetupExecute` debe pasar exactamente `It.IsAny<CancellationToken>()` como tercer argumento, como está arriba; no uses `It.IsAny<IDbTransaction>()`.

- [ ] **Step 6: Correr toda la suite para descartar regresiones**

Run: `dotnet test ZynstormECFPlatform.Tests 2>&1 | tail -8`
Expected: `Failed: 0`. Si hay fallos que ya existían antes de este plan (compáralos con `git stash` solo si hace falta), repórtalos sin tocarlos.

- [ ] **Step 7: Commit**

```bash
git branch --show-current && git add ZynstormECFPlatform.Services/Billing/ClientUsageService.cs ZynstormECFPlatform.Tests/Billing/ClientUsageSqlTests.cs ZynstormECFPlatform.Tests/Billing/ClientUsageServiceTests.cs && git commit -m "$(cat <<'EOF'
Contar el consumo de clientes con cuenta en el pool

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Verificación contra PostgreSQL real

Las pruebas unitarias comprueban el texto del SQL y la bifurcación, pero no que PostgreSQL ejecute el CTE como se espera. Esta tarea lo verifica en una base de desarrollo. **Pide confirmación al usuario antes de aplicar la migración**: modifica su base de datos. Si no hay base de desarrollo disponible, deja esta tarea pendiente y repórtalo; no marques la fase como probada.

**Files:** ninguno (solo verificación; no se commitea nada).

- [ ] **Step 1: Aplicar la migración**

Run: `dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api 2>&1 | tail -5`
Expected: `Done.`

- [ ] **Step 2: Correr el SQL en una transacción que se revierte**

Guardar como `C:\Users\JOSETR~2\AppData\Local\Temp\claude\7c7aaff6be5a61dc\b7c5b597-abd4-499f-95b0-7a396a3f809c\scratchpad\verify_pool.sql` y correrlo con `psql -f` contra la base de desarrollo (host, base, usuario y contraseña salen de `ConnectionStrings:DefaultConnection` en `ZynstormECFPlatform.Web.Api/appsettings.Development.json`). El script usa variables de psql (`:nombre`) en lugar de los parámetros `@nombre` de Dapper; el texto de cada sentencia es el de `ClientUsageSql.RegisterAcceptedInAccount`.

```sql
\set ON_ERROR_STOP on
BEGIN;

-- Cuenta de prueba: dueño = cualquier usuario, plan = cualquier plan de comprobantes activo
INSERT INTO "BillingAccount" ("Name", "OwnerUserId", "PlanId", "StatusId")
SELECT 'TEST POOL', (SELECT "Id" FROM "AspNetUsers" LIMIT 1),
       (SELECT "PlanId" FROM "Plan" WHERE "PlanTypeId" = 1 AND "StatusId" = 1 LIMIT 1), 1;

SELECT "BillingAccountId" AS acct FROM "BillingAccount" WHERE "Name" = 'TEST POOL' \gset
SELECT "PlanId" AS plan_id, "Name" AS plan_name, "MonthlyFee" AS plan_fee, "MonthlyDocumentLimit" AS plan_limit
FROM "Plan" WHERE "PlanId" = (SELECT "PlanId" FROM "BillingAccount" WHERE "BillingAccountId" = :acct) \gset
SELECT EXTRACT(YEAR FROM now())::int AS yr, EXTRACT(MONTH FROM now())::int AS mo \gset

-- Los dos documentos más recientes (pueden ser del mismo cliente); se dejan como "no contados"
SELECT (SELECT "EcfDocumentId" FROM "EcfDocument" ORDER BY "EcfDocumentId" DESC LIMIT 1) AS doc_a,
       (SELECT "EcfDocumentId" FROM "EcfDocument" ORDER BY "EcfDocumentId" DESC OFFSET 1 LIMIT 1) AS doc_b \gset
UPDATE "EcfDocument" SET "BillingCountedAtUtc" = NULL WHERE "EcfDocumentId" IN (:doc_a, :doc_b);
UPDATE "Client" SET "BillingAccountId" = :acct
WHERE "ClientId" IN (SELECT "ClientId" FROM "EcfDocument" WHERE "EcfDocumentId" IN (:doc_a, :doc_b));

-- Corrida 1: documento A
WITH marked AS (
    UPDATE "EcfDocument" SET "BillingCountedAtUtc" = now()
    WHERE "EcfDocumentId" = :doc_a AND "BillingCountedAtUtc" IS NULL
    RETURNING "ClientId"
),
client_usage AS (
    INSERT INTO "ClientMonthlyUsage"
        ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
    SELECT "ClientId", :yr, :mo, 1, CAST(NULL AS integer), :'plan_name', 0, -1, now()
    FROM marked
    ON CONFLICT ("ClientId", "Year", "Month")
    DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                  "LastUpdateUtc" = EXCLUDED."LastUpdateUtc"
    RETURNING "ClientId"
)
INSERT INTO "BillingAccountMonthlyUsage"
    ("BillingAccountId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
SELECT :acct, :yr, :mo, 1, :plan_id, :'plan_name', :plan_fee, :plan_limit, now()
FROM marked
ON CONFLICT ("BillingAccountId", "Year", "Month")
DO UPDATE SET "AcceptedDocuments" = "BillingAccountMonthlyUsage"."AcceptedDocuments" + 1,
              "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";

-- Corrida 2: documento A otra vez (reintento: debe ser un no-op)
WITH marked AS (
    UPDATE "EcfDocument" SET "BillingCountedAtUtc" = now()
    WHERE "EcfDocumentId" = :doc_a AND "BillingCountedAtUtc" IS NULL
    RETURNING "ClientId"
),
client_usage AS (
    INSERT INTO "ClientMonthlyUsage"
        ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
    SELECT "ClientId", :yr, :mo, 1, CAST(NULL AS integer), :'plan_name', 0, -1, now()
    FROM marked
    ON CONFLICT ("ClientId", "Year", "Month")
    DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                  "LastUpdateUtc" = EXCLUDED."LastUpdateUtc"
    RETURNING "ClientId"
)
INSERT INTO "BillingAccountMonthlyUsage"
    ("BillingAccountId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
SELECT :acct, :yr, :mo, 1, :plan_id, :'plan_name', :plan_fee, :plan_limit, now()
FROM marked
ON CONFLICT ("BillingAccountId", "Year", "Month")
DO UPDATE SET "AcceptedDocuments" = "BillingAccountMonthlyUsage"."AcceptedDocuments" + 1,
              "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";

-- Corrida 3: documento B
WITH marked AS (
    UPDATE "EcfDocument" SET "BillingCountedAtUtc" = now()
    WHERE "EcfDocumentId" = :doc_b AND "BillingCountedAtUtc" IS NULL
    RETURNING "ClientId"
),
client_usage AS (
    INSERT INTO "ClientMonthlyUsage"
        ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
    SELECT "ClientId", :yr, :mo, 1, CAST(NULL AS integer), :'plan_name', 0, -1, now()
    FROM marked
    ON CONFLICT ("ClientId", "Year", "Month")
    DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                  "LastUpdateUtc" = EXCLUDED."LastUpdateUtc"
    RETURNING "ClientId"
)
INSERT INTO "BillingAccountMonthlyUsage"
    ("BillingAccountId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
SELECT :acct, :yr, :mo, 1, :plan_id, :'plan_name', :plan_fee, :plan_limit, now()
FROM marked
ON CONFLICT ("BillingAccountId", "Year", "Month")
DO UPDATE SET "AcceptedDocuments" = "BillingAccountMonthlyUsage"."AcceptedDocuments" + 1,
              "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";

SELECT 'pool' AS fila, "AcceptedDocuments" FROM "BillingAccountMonthlyUsage" WHERE "BillingAccountId" = :acct;

SELECT 'cliente' AS fila, "ClientId", "AcceptedDocuments", "PlanId", "MonthlyFee", "MonthlyDocumentLimit"
FROM "ClientMonthlyUsage"
WHERE "ClientId" IN (SELECT "ClientId" FROM "EcfDocument" WHERE "EcfDocumentId" IN (:doc_a, :doc_b))
  AND "Year" = :yr AND "Month" = :mo;

ROLLBACK;
```

Expected tras ejecutar el SQL (3 corridas: doc A, doc A otra vez, doc B):
- Fila `pool`: `AcceptedDocuments` = **2** (el reintento del doc A no suma).
- Filas `cliente`: la suma de `AcceptedDocuments` de ambos clientes = **2** (o una sola fila con 2 si los dos documentos son del mismo cliente), todas con `PlanId` NULL, `MonthlyFee` 0 y `MonthlyDocumentLimit` -1.
- El `ROLLBACK` final deja la base sin cambios.

Si el pool queda en 0 con los clientes en 2 (o al revés), el CTE no escribió ambas filas: detente y repórtalo; no sigas con la fase 2.

- [ ] **Step 3: Reportar al usuario**

Informar el resultado de las pruebas (`dotnet test` completo), si se aplicó la migración y el resultado de la verificación contra PostgreSQL. Recordar los puntos que quedan para las fases siguientes:
- Asignar un cliente a una cuenta se hace por SQL (`UPDATE "Client" SET "BillingAccountId" = ...`) y conviene a inicio de mes, porque la fila del mes ya abierta conserva su snapshot anterior.
- Aún no hay cobro, suspensión ni pantallas de cuenta: el pool acumula consumo pero nadie lo cobra hasta la fase 2.
- La réplica a `Staging` y `feature/emit-idempotente` se hace al terminar las 4 fases.
