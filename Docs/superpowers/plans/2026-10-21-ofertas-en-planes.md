# Ofertas en planes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que un plan pueda tener una oferta (precio promocional por X meses, luego precio real), que el cobro de la mensualidad respete esa oferta mes por mes, y que se vea cuánto tiempo le falta a la oferta de cada cliente.

**Architecture:** El plan guarda `OfferPrice` y `OfferMonths`; el cliente guarda `OfferStartDate` (se fija al asignarle el plan). Una función pura `OfferPricing` calcula estado, fin y días restantes; `PaymentCalculator.Calculate` pasa a cobrar mes por mes con esa oferta. La API del cliente expone los campos calculados y el frontend los muestra en `/clientes` (badge, filtro, panel en el diálogo) y en los diálogos de pago.

**Tech Stack:** .NET 10 / EF Core 10 (Npgsql, PostgreSQL) / AutoMapper / xUnit; Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-10-21-ofertas-en-planes-design.md` (commit `94060a5`). La sección "5. Ajustes" que agrega la Task 1 recoge las decisiones tomadas durante la planificación.

## Global Constraints

- Backend en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, frontend en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`. Son dos repos git distintos; ambos están en la rama `Staging`.
- Antes de cada commit confirmar la rama **en el mismo comando** (`git branch --show-current`, debe imprimir `Staging`). No hacer `git push`.
- Cada mensaje de commit termina con estas dos líneas exactas:
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`
  `Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW`
- Precio de oferta fijo (no porcentaje): `OfferPrice` con `0 <= OfferPrice < MonthlyFee`; `OfferMonths` entre 1 y 120; los dos campos van juntos o ninguno.
- La oferta empieza al asignar el plan al cliente (`Client.OfferStartDate`). Un plan que recibe oferta después **no** afecta retroactivamente a clientes que ya lo tenían.
- Pago por adelantado que cruza el fin de la oferta: se cobra mes por mes. El mes k (0-based) cubre el periodo que empieza en `inicioPeriodo + k meses` y usa `OfferPrice` si esa fecha es **anterior** a `OfferStartDate + OfferMonths meses`, y `MonthlyFee` si no. `PrepaymentDiscountPercent` se aplica sobre el bruto ya calculado.
- Fechas calendario en hora RD: `DateTimeExtensions.DrNow.Date` (backend) y `todayDrYmd()` (frontend).
- Mensajes de usuario en español. Estilo del código existente: constructores primarios, `namespace` con ámbito de archivo en `Services/Billing`, sin comentarios innecesarios (los existentes son `///` en español).
- `ClientMonthlyUsage.MonthlyFee` (snapshot de consumo) y `BuildUsageDtosAsync` en `ClientController` **no se tocan** (ver nota al final).
- No ejecutar `dotnet ef database update` sin que el usuario lo apruebe.
- Comandos backend, desde `ZynstormECFPlatform`: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`. Comandos frontend, desde `ZynstormECFPlatform-FrontEnd`: `npx tsc --noEmit` (no hay runner de pruebas en el frontend).

---

## File Structure

**Backend (`ZynstormECFPlatform`)**
- Modify `ZynstormECFPlatform.Core/Entities/Plan.cs` — `OfferPrice`, `OfferMonths`.
- Modify `ZynstormECFPlatform.Core/Entities/Client.cs` — `OfferStartDate`.
- Modify `ZynstormECFPlatform.Core/Entities/ClientPaymentItem.cs` — `OfferMonthsCovered`, `OfferUnitPrice`.
- Modify `ZynstormECFPlatform.Data/StorageContext.cs` — tipo de columna de `OfferStartDate`.
- Create `ZynstormECFPlatform.Data/Migrations/<ts>_AddPlanOffers.cs` (+ Designer, + snapshot) — lo genera `dotnet ef`.
- Create `ZynstormECFPlatform.Services/Billing/OfferPricing.cs` — lógica pura de la oferta.
- Modify `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs` — cobro mes por mes y `CalculateCycle`.
- Modify `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs` — usa `CalculateCycle`, guarda/expone el desglose.
- Modify `ZynstormECFPlatform.Services/Billing/PaymentReminderService.cs` — monto del aviso con oferta.
- Modify `ZynstormECFPlatform.Dtos/PlanDtos.cs`, `ClientDtos.cs`, `ClientPaymentRegistrationDtos.cs`.
- Modify `ZynstormECFPlatform.Mappings/MappingProfiles.cs` — `PlanOfferPrice`, `PlanOfferMonths`.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs` — validación de la oferta.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs` — fecha de inicio, campos calculados, filtro, ciclo con oferta.
- Create `ZynstormECFPlatform.Tests/Billing/OfferPricingTests.cs`, `PaymentCalculatorOfferTests.cs`.

**Frontend (`ZynstormECFPlatform-FrontEnd`)**
- Create `types/offer.type.ts`, `lib/offer.ts`.
- Modify `types/plan.type.ts`, `types/client.type.ts`, `types/paymentRegistration.type.ts`.
- Modify `services/client.service.ts`, `hooks/useClients.ts` — parámetro `offerExpiring`.
- Modify `app/configuraciones/planes/page.tsx` — campos y etiqueta de oferta.
- Modify `app/clientes/page.tsx` — badge, filtro, panel en el diálogo, estimado del ciclo.
- Modify `components/payments/register-payment-dialog.tsx`, `components/payments/payment-receipt-dialog.tsx`.

---

### Task 1: Modelo de datos, migración y ajuste del spec

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/Plan.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Modify: `ZynstormECFPlatform.Core/Entities/ClientPaymentItem.cs`
- Modify: `ZynstormECFPlatform.Data/StorageContext.cs` (bloque `modelBuilder.Entity<Client>`, cerca de la línea 517)
- Create: `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddPlanOffers.cs` (generado)
- Modify: `Docs/superpowers/specs/2026-10-21-ofertas-en-planes-design.md`

**Interfaces:**
- Produces: `Plan.OfferPrice (decimal?)`, `Plan.OfferMonths (int?)`, `Client.OfferStartDate (DateTime?)`, `ClientPaymentItem.OfferMonthsCovered (int?)`, `ClientPaymentItem.OfferUnitPrice (decimal?)`. Todas nulas, sin default.

- [ ] **Step 1: Agregar las propiedades a las entidades**

En `Plan.cs`, después de `MaxUsers`:

```csharp
    /// <summary>Precio mensual promocional durante la oferta. Null = el plan no tiene oferta.</summary>
    public decimal? OfferPrice { get; set; }

    /// <summary>Duración de la oferta en meses, contados desde que se asigna el plan al cliente.</summary>
    public int? OfferMonths { get; set; }
```

En `Client.cs`, después de `PlanId`:

```csharp
    /// <summary>Fecha calendario en que empezó la oferta del plan para este cliente. Null = sin oferta.</summary>
    public DateTime? OfferStartDate { get; set; }
```

En `ClientPaymentItem.cs`, después de `DiscountAmount`:

```csharp
    /// <summary>Meses de este pago cobrados al precio de oferta.</summary>
    public int? OfferMonthsCovered { get; set; }

    /// <summary>Precio mensual de oferta aplicado en este pago.</summary>
    public decimal? OfferUnitPrice { get; set; }
```

- [ ] **Step 2: Configurar el tipo de columna de `OfferStartDate`**

En `StorageContext.cs`, dentro de `modelBuilder.Entity<Client>(entity => { ... })` (el bloque que contiene `entity.Property(e => e.PaidMonths)`), insertar antes de ese `entity.Property(e => e.PaidMonths)`:

```csharp
            entity.Property(e => e.OfferStartDate)
                  .HasColumnType(DateTimeColumnType);

```

Los `decimal?` ya toman `numeric(18,2)` por la convención de `ConfigureConventions`.

- [ ] **Step 3: Compilar**

Run (desde `ZynstormECFPlatform`): `dotnet build ZynstormECFPlatform.Data`
Expected: `Build succeeded.` con 0 errores.

- [ ] **Step 4: Generar la migración**

Run: `dotnet ef migrations add AddPlanOffers --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: `Done.` y archivos nuevos `*_AddPlanOffers.cs`, `*_AddPlanOffers.Designer.cs`, `StorageContextModelSnapshot.cs` modificado.

- [ ] **Step 5: Revisar que la migración solo agrega las 5 columnas**

Abrir `ZynstormECFPlatform.Data/Migrations/*_AddPlanOffers.cs`. `Up` debe contener exactamente cinco `AddColumn` y nada más:
`OfferStartDate` (`timestamp without time zone`, nullable) en la tabla de `Client`; `OfferPrice` (`numeric(18,2)`, nullable) y `OfferMonths` (`integer`, nullable) en la de `Plan`; `OfferMonthsCovered` (`integer`, nullable) y `OfferUnitPrice` (`numeric(18,2)`, nullable) en la de `ClientPaymentItem`. `Down` debe tener los cinco `DropColumn`.
Si aparece cualquier otra operación (renombrados, índices, tablas ajenas): `dotnet ef migrations remove --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`, corregir la configuración y repetir el Step 4.

- [ ] **Step 6: Agregar la sección 5 al spec**

Al final de `Docs/superpowers/specs/2026-10-21-ofertas-en-planes-design.md` agregar:

```markdown

## 5. Ajustes tomados al planificar

- **Desglose en el recibo.** `ClientPaymentItem` agrega `OfferMonthsCovered` (meses cobrados a precio de oferta) y `OfferUnitPrice`, para que el historial de recibos pueda mostrar el desglose oferta/real. Son 2 columnas nuevas además de las 3 del apartado 1.
- **`PaymentCalculator.CalculateCycle(Client, fallbackPeriodStart)`.** Un solo punto que arma el ciclo de un cliente con su plan y su oferta. El primer mes cubierto empieza en `NextPaymentDate`, o en `fallbackPeriodStart` si no hay fecha. Lo usan el registro y la previsualización de pago, el aviso de pago y los dos puntos de `ClientController` (renta mensual y `payments`).
- **Campos calculados.** Los calcula `ClientController` (no AutoMapper, porque `ZynstormECFPlatform.Mappings` no referencia `Services`). `ClientViewDto` también lleva `PlanOfferPrice` y `PlanOfferMonths`.
- **Filtro "Ofertas por vencer".** El listado de clientes está paginado en el servidor, así que el filtro es el parámetro `offerExpiring=true` de `GET /Client`: se buscan en memoria los clientes con oferta activa que vencen en 30 días o menos y se restringe la consulta a esos ids.
- **Fuera de alcance, sin cambio.** `BuildUsageDtosAsync` (consumo de planes por comprobantes) sigue calculando la mensualidad con el precio real del snapshot `ClientMonthlyUsage.MonthlyFee`.
```

- [ ] **Step 7: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Core ZynstormECFPlatform.Data Docs/superpowers/specs/2026-10-21-ofertas-en-planes-design.md
git commit -q -m "$(cat <<'EOF'
Agregar campos de oferta a planes, clientes y recibos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```
Expected: `Rama: Staging` y el commit nuevo.

---

### Task 2: `OfferPricing` (lógica pura) con pruebas

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/OfferPricing.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/OfferPricingTests.cs`

**Interfaces:**
- Produces (todo en `ZynstormECFPlatform.Services.Billing`):
  - `enum OfferStatus { None = 0, Active = 1, Ended = 2 }`
  - `sealed record OfferTerms(decimal OfferPrice, int OfferMonths, DateTime StartDate)` con `DateTime EndDate` (primer día a precio real).
  - `sealed record OfferState(OfferStatus Status, DateTime? EndDate, int? DaysRemaining, int? MonthsRemaining)`
  - `static class OfferPricing`: `const int DefaultWarningDays = 30`, `const int MaxOfferMonths = 120`, `bool PlanHasOffer(decimal?, int?)`, `OfferTerms? TermsFor(decimal? offerPrice, int? offerMonths, DateTime? startDate)`, `OfferState Evaluate(OfferTerms? terms, DateTime today)`, `bool IsExpiringSoon(OfferTerms? terms, DateTime today, int warningDays = DefaultWarningDays)`, `DateTime? ResolveStartDate(int? previousPlanId, int? newPlanId, bool newPlanHasOffer, DateTime? currentStart, DateTime today)`, `List<string> ValidateOffer(decimal monthlyFee, decimal? offerPrice, int? offerMonths)`, `decimal? GetFullPriceUnit(decimal grossAmount, int monthsCovered, int offerMonthsCovered, decimal offerUnitPrice)`.

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Billing/OfferPricingTests.cs`:

```csharp
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class OfferPricingTests
{
    private static readonly DateTime Start = new(2026, 10, 21);
    private static readonly OfferTerms ThreeMonths = new(1500m, 3, Start);

    [Fact]
    public void PlanHasOffer_TrueWithPriceAndPositiveMonths()
    {
        Assert.True(OfferPricing.PlanHasOffer(1500m, 3));
    }

    [Fact]
    public void PlanHasOffer_FalseWhenAnyPartIsMissingOrMonthsAreZero()
    {
        Assert.False(OfferPricing.PlanHasOffer(null, null));
        Assert.False(OfferPricing.PlanHasOffer(1500m, null));
        Assert.False(OfferPricing.PlanHasOffer(null, 3));
        Assert.False(OfferPricing.PlanHasOffer(1500m, 0));
    }

    [Fact]
    public void TermsFor_BuildsTermsAndDropsTheTimeOfDay()
    {
        var terms = OfferPricing.TermsFor(1500m, 3, new DateTime(2026, 10, 21, 15, 30, 0));

        Assert.NotNull(terms);
        Assert.Equal(1500m, terms!.OfferPrice);
        Assert.Equal(3, terms.OfferMonths);
        Assert.Equal(new DateTime(2026, 10, 21), terms.StartDate);
    }

    [Fact]
    public void TermsFor_NullWithoutStartDateOrWithoutPlanOffer()
    {
        Assert.Null(OfferPricing.TermsFor(1500m, 3, null));
        Assert.Null(OfferPricing.TermsFor(null, null, Start));
    }

    [Fact]
    public void EndDate_AddsTheOfferMonthsAndClampsToTheLastDayOfMonth()
    {
        Assert.Equal(new DateTime(2027, 1, 21), ThreeMonths.EndDate);
        Assert.Equal(new DateTime(2027, 2, 28), new OfferTerms(1500m, 3, new DateTime(2026, 11, 30)).EndDate);
    }

    [Fact]
    public void Evaluate_WithoutTerms_IsNone()
    {
        var state = OfferPricing.Evaluate(null, Start);

        Assert.Equal(OfferStatus.None, state.Status);
        Assert.Null(state.EndDate);
        Assert.Null(state.DaysRemaining);
        Assert.Null(state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_OnTheStartDay_IsActiveWithAllTheTimeLeft()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, Start);

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(new DateTime(2027, 1, 21), state.EndDate);
        Assert.Equal(92, state.DaysRemaining);
        Assert.Equal(3, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_MidOffer_CountsWholeMonthsAndDays()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2026, 11, 30));

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(52, state.DaysRemaining);
        Assert.Equal(1, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_LastDayBeforeTheEnd_IsActiveWithOneDay()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 1, 20));

        Assert.Equal(OfferStatus.Active, state.Status);
        Assert.Equal(1, state.DaysRemaining);
        Assert.Equal(0, state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_OnTheEndDate_IsEnded()
    {
        var state = OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 1, 21));

        Assert.Equal(OfferStatus.Ended, state.Status);
        Assert.Equal(new DateTime(2027, 1, 21), state.EndDate);
        Assert.Null(state.DaysRemaining);
        Assert.Null(state.MonthsRemaining);
    }

    [Fact]
    public void Evaluate_AfterTheEnd_IsEnded()
    {
        Assert.Equal(OfferStatus.Ended, OfferPricing.Evaluate(ThreeMonths, new DateTime(2027, 2, 1)).Status);
    }

    [Fact]
    public void IsExpiringSoon_TrueWithinTheWarningWindow()
    {
        Assert.True(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 25)));
        Assert.True(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 22)));
    }

    [Fact]
    public void IsExpiringSoon_FalseOutsideTheWindowOrWhenEndedOrWithoutOffer()
    {
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2026, 12, 21)));
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, Start));
        Assert.False(OfferPricing.IsExpiringSoon(ThreeMonths, new DateTime(2027, 1, 21)));
        Assert.False(OfferPricing.IsExpiringSoon(null, Start));
    }

    [Fact]
    public void ResolveStartDate_NewClientOnAPlanWithOffer_StartsToday()
    {
        Assert.Equal(Start, OfferPricing.ResolveStartDate(null, 5, true, null, Start));
    }

    [Fact]
    public void ResolveStartDate_ChangingToAnotherPlanWithOffer_RestartsToday()
    {
        var previous = new DateTime(2026, 1, 10);

        Assert.Equal(Start, OfferPricing.ResolveStartDate(3, 5, true, previous, Start));
    }

    [Fact]
    public void ResolveStartDate_SamePlan_KeepsTheCurrentStart()
    {
        var current = new DateTime(2026, 9, 1);

        Assert.Equal(current, OfferPricing.ResolveStartDate(5, 5, true, current, Start));
    }

    [Fact]
    public void ResolveStartDate_SamePlanThatGotAnOfferLater_IsNotRetroactive()
    {
        Assert.Null(OfferPricing.ResolveStartDate(5, 5, true, null, Start));
    }

    [Fact]
    public void ResolveStartDate_PlanWithoutOffer_ClearsTheStart()
    {
        Assert.Null(OfferPricing.ResolveStartDate(3, 5, false, new DateTime(2026, 9, 1), Start));
        Assert.Null(OfferPricing.ResolveStartDate(5, 5, false, new DateTime(2026, 9, 1), Start));
        Assert.Null(OfferPricing.ResolveStartDate(null, null, false, null, Start));
    }

    [Fact]
    public void ValidateOffer_NoOfferFields_IsValid()
    {
        Assert.Empty(OfferPricing.ValidateOffer(2500m, null, null));
    }

    [Fact]
    public void ValidateOffer_PriceBelowFeeWithMonths_IsValid()
    {
        Assert.Empty(OfferPricing.ValidateOffer(2500m, 1500m, 3));
        Assert.Empty(OfferPricing.ValidateOffer(2500m, 0m, 120));
    }

    [Fact]
    public void ValidateOffer_OnlyOneOfTheTwoFields_Fails()
    {
        Assert.Contains("La oferta requiere precio y meses de oferta.", OfferPricing.ValidateOffer(2500m, 1500m, null));
        Assert.Contains("La oferta requiere precio y meses de oferta.", OfferPricing.ValidateOffer(2500m, null, 3));
    }

    [Fact]
    public void ValidateOffer_MonthsOutOfRange_Fails()
    {
        Assert.Contains("Los meses de oferta deben estar entre 1 y 120.", OfferPricing.ValidateOffer(2500m, 1500m, 0));
        Assert.Contains("Los meses de oferta deben estar entre 1 y 120.", OfferPricing.ValidateOffer(2500m, 1500m, 121));
    }

    [Fact]
    public void ValidateOffer_PriceNotBelowTheFee_Fails()
    {
        Assert.Contains("El precio de oferta debe ser menor que la mensualidad.", OfferPricing.ValidateOffer(2500m, 2500m, 3));
        Assert.Contains("El precio de oferta debe ser menor que la mensualidad.", OfferPricing.ValidateOffer(2500m, 3000m, 3));
    }

    [Fact]
    public void ValidateOffer_NegativePrice_Fails()
    {
        Assert.Contains("El precio de oferta no puede ser negativo.", OfferPricing.ValidateOffer(2500m, -1m, 3));
    }

    [Fact]
    public void GetFullPriceUnit_DerivesTheFullPriceFromTheGross()
    {
        Assert.Equal(2500m, OfferPricing.GetFullPriceUnit(12000m, 6, 3, 1500m));
    }

    [Fact]
    public void GetFullPriceUnit_NullWhenEveryMonthWasOffer()
    {
        Assert.Null(OfferPricing.GetFullPriceUnit(4500m, 3, 3, 1500m));
    }
}
```

- [ ] **Step 2: Ejecutar para ver que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~OfferPricingTests"`
Expected: error de compilación (`OfferPricing`, `OfferTerms`, `OfferStatus` no existen).

- [ ] **Step 3: Implementar `OfferPricing`**

Crear `ZynstormECFPlatform.Services/Billing/OfferPricing.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

public enum OfferStatus
{
    None = 0,
    Active = 1,
    Ended = 2
}

/// <summary>Condiciones de la oferta de un cliente: precio promocional, duración y fecha de inicio.</summary>
public sealed record OfferTerms(decimal OfferPrice, int OfferMonths, DateTime StartDate)
{
    /// <summary>Primer día en que se cobra el precio real.</summary>
    public DateTime EndDate => StartDate.Date.AddMonths(OfferMonths);
}

public sealed record OfferState(OfferStatus Status, DateTime? EndDate, int? DaysRemaining, int? MonthsRemaining);

/// <summary>Reglas de las ofertas de plan. Funciones puras: no tocan base de datos ni configuración.</summary>
public static class OfferPricing
{
    /// <summary>Días de anticipación para considerar una oferta próxima a vencer.</summary>
    public const int DefaultWarningDays = 30;

    public const int MaxOfferMonths = 120;

    public static bool PlanHasOffer(decimal? offerPrice, int? offerMonths) =>
        offerPrice.HasValue && offerMonths is > 0;

    /// <summary>Null si el plan no tiene oferta o el cliente no tiene fecha de inicio de oferta.</summary>
    public static OfferTerms? TermsFor(decimal? offerPrice, int? offerMonths, DateTime? startDate) =>
        PlanHasOffer(offerPrice, offerMonths) && startDate is DateTime start
            ? new OfferTerms(offerPrice!.Value, offerMonths!.Value, start.Date)
            : null;

    /// <summary>
    /// Estado de la oferta al día <paramref name="today"/>. Está activa hasta el día anterior a
    /// <see cref="OfferTerms.EndDate"/>; desde esa fecha se cobra el precio real.
    /// </summary>
    public static OfferState Evaluate(OfferTerms? terms, DateTime today)
    {
        if (terms is null)
            return new OfferState(OfferStatus.None, null, null, null);

        var end = terms.EndDate;
        var reference = today.Date;

        if (reference >= end)
            return new OfferState(OfferStatus.Ended, end, null, null);

        var months = 0;
        while (reference.AddMonths(months + 1) <= end) months++;

        return new OfferState(OfferStatus.Active, end, (end - reference).Days, months);
    }

    public static bool IsExpiringSoon(OfferTerms? terms, DateTime today, int warningDays = DefaultWarningDays)
    {
        var state = Evaluate(terms, today);
        return state.Status == OfferStatus.Active && state.DaysRemaining <= warningDays;
    }

    /// <summary>
    /// Fecha de inicio de la oferta tras guardar un cliente. La oferta arranca (o se reinicia) al asignar
    /// un plan distinto; con el mismo plan se conserva la fecha actual, así un plan que recibe oferta
    /// después no afecta retroactivamente a quienes ya lo tenían.
    /// </summary>
    public static DateTime? ResolveStartDate(
        int? previousPlanId,
        int? newPlanId,
        bool newPlanHasOffer,
        DateTime? currentStart,
        DateTime today)
    {
        if (!newPlanHasOffer) return null;

        return previousPlanId != newPlanId ? today.Date : currentStart;
    }

    public static List<string> ValidateOffer(decimal monthlyFee, decimal? offerPrice, int? offerMonths)
    {
        var errors = new List<string>();

        if (offerPrice is null && offerMonths is null)
            return errors;

        if (offerPrice is null || offerMonths is null)
        {
            errors.Add("La oferta requiere precio y meses de oferta.");
            return errors;
        }

        if (offerMonths < 1 || offerMonths > MaxOfferMonths)
            errors.Add($"Los meses de oferta deben estar entre 1 y {MaxOfferMonths}.");

        if (offerPrice < 0)
            errors.Add("El precio de oferta no puede ser negativo.");
        else if (offerPrice >= monthlyFee)
            errors.Add("El precio de oferta debe ser menor que la mensualidad.");

        return errors;
    }

    /// <summary>
    /// Precio mensual real que se cobró en un pago que mezcló meses de oferta y meses a precio real.
    /// Null si todos los meses fueron de oferta.
    /// </summary>
    public static decimal? GetFullPriceUnit(decimal grossAmount, int monthsCovered, int offerMonthsCovered, decimal offerUnitPrice)
    {
        var fullMonths = monthsCovered - offerMonthsCovered;
        if (fullMonths <= 0) return null;

        return Math.Round((grossAmount - offerMonthsCovered * offerUnitPrice) / fullMonths, 2, MidpointRounding.AwayFromZero);
    }
}
```

- [ ] **Step 4: Ejecutar para ver que pasa**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~OfferPricingTests"`
Expected: todas las pruebas pasan (`Passed!`, 0 fallidas).

- [ ] **Step 5: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Services/Billing/OfferPricing.cs ZynstormECFPlatform.Tests/Billing/OfferPricingTests.cs
git commit -q -m "$(cat <<'EOF'
Agregar OfferPricing con la lógica de ofertas de plan

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 3: `PaymentCalculator` cobra mes por mes con la oferta

**Files:**
- Modify: `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/PaymentCalculatorOfferTests.cs`

**Interfaces:**
- Consumes: `OfferTerms` y `OfferPricing.TermsFor` (Task 2); `Client`, `Plan` (Task 1).
- Produces:
  - `PaymentCalculationResult` agrega al final dos parámetros con default: `int OfferMonthsCovered = 0`, `decimal? OfferUnitPrice = null`. Las llamadas y pruebas existentes siguen compilando.
  - `PaymentCalculator.Calculate(decimal monthlyFee, int paidMonths, decimal discountPercent, OfferTerms? offer = null, DateTime? periodStart = null)`. Sin `offer` o sin `periodStart` el resultado es idéntico al actual.
  - `PaymentCalculator.CalculateCycle(Client client, DateTime fallbackPeriodStart)`: exige `client.Plan` cargado (lanza `ArgumentException` si no).

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Billing/PaymentCalculatorOfferTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentCalculatorOfferTests
{
    private static readonly DateTime Start = new(2026, 10, 21);
    private static readonly OfferTerms ThreeMonthOffer = new(1500m, 3, Start);

    [Fact]
    public void Calculate_MonthInsideTheOffer_ChargesTheOfferPrice()
    {
        var result = PaymentCalculator.Calculate(2500m, 1, 0m, ThreeMonthOffer, Start);

        Assert.Equal(2500m, result.MonthlyFee);
        Assert.Equal(1500m, result.GrossAmount);
        Assert.Equal(1500m, result.Total);
        Assert.Equal(1, result.OfferMonthsCovered);
        Assert.Equal(1500m, result.OfferUnitPrice);
    }

    [Fact]
    public void Calculate_PaymentCrossingTheOfferEnd_IsProratedByMonth()
    {
        var result = PaymentCalculator.Calculate(2500m, 6, 0m, ThreeMonthOffer, Start);

        Assert.Equal(6, result.MonthsCovered);
        Assert.Equal(3, result.OfferMonthsCovered);
        Assert.Equal(12000m, result.GrossAmount);
        Assert.Equal(12000m, result.Total);
    }

    [Fact]
    public void Calculate_LastOfferMonth_OnlyThatMonthIsOffer()
    {
        var result = PaymentCalculator.Calculate(2500m, 2, 0m, ThreeMonthOffer, new DateTime(2026, 12, 21));

        Assert.Equal(1, result.OfferMonthsCovered);
        Assert.Equal(4000m, result.GrossAmount);
    }

    [Fact]
    public void Calculate_PeriodStartingOnTheEndDate_ChargesTheFullPrice()
    {
        var result = PaymentCalculator.Calculate(2500m, 1, 0m, ThreeMonthOffer, new DateTime(2027, 1, 21));

        Assert.Equal(0, result.OfferMonthsCovered);
        Assert.Null(result.OfferUnitPrice);
        Assert.Equal(2500m, result.GrossAmount);
    }

    [Fact]
    public void Calculate_PrepaymentDiscountAppliesOverTheProratedGross()
    {
        var result = PaymentCalculator.Calculate(2500m, 6, 10m, ThreeMonthOffer, Start);

        Assert.Equal(12000m, result.GrossAmount);
        Assert.Equal(1200m, result.DiscountAmount);
        Assert.Equal(10800m, result.Total);
    }

    [Fact]
    public void Calculate_OfferWithoutPeriodStart_IsIgnored()
    {
        var result = PaymentCalculator.Calculate(2500m, 3, 0m, ThreeMonthOffer, null);

        Assert.Equal(7500m, result.GrossAmount);
        Assert.Equal(0, result.OfferMonthsCovered);
    }

    [Fact]
    public void Calculate_WithoutOffer_MatchesTheLegacyResult()
    {
        var result = PaymentCalculator.Calculate(2500m, 3, 0m, null, Start);

        Assert.Equal(7500m, result.GrossAmount);
        Assert.Equal(0, result.OfferMonthsCovered);
        Assert.Null(result.OfferUnitPrice);
    }

    [Fact]
    public void CalculateCycle_UsesThePlanOfferAndTheClientNextPaymentDate()
    {
        var client = new Client
        {
            Plan = new Plan { MonthlyFee = 2500m, OfferPrice = 1500m, OfferMonths = 3 },
            PaidMonths = 6,
            OfferStartDate = Start,
            NextPaymentDate = Start
        };

        var result = PaymentCalculator.CalculateCycle(client, new DateTime(2030, 1, 1));

        Assert.Equal(12000m, result.Total);
        Assert.Equal(3, result.OfferMonthsCovered);
    }

    [Fact]
    public void CalculateCycle_WithoutNextPaymentDate_StartsAtTheFallbackDate()
    {
        var client = new Client
        {
            Plan = new Plan { MonthlyFee = 2500m, OfferPrice = 1500m, OfferMonths = 3 },
            PaidMonths = 1,
            OfferStartDate = Start,
            NextPaymentDate = null
        };

        Assert.Equal(1500m, PaymentCalculator.CalculateCycle(client, Start).Total);
    }

    [Fact]
    public void CalculateCycle_ClientWithoutOfferStart_ChargesTheFullPrice()
    {
        var client = new Client
        {
            Plan = new Plan { MonthlyFee = 2500m, OfferPrice = 1500m, OfferMonths = 3 },
            PaidMonths = 1,
            OfferStartDate = null,
            NextPaymentDate = Start
        };

        Assert.Equal(2500m, PaymentCalculator.CalculateCycle(client, Start).Total);
    }

    [Fact]
    public void CalculateCycle_WithoutAPlan_Throws()
    {
        Assert.Throws<ArgumentException>(() => PaymentCalculator.CalculateCycle(new Client(), Start));
    }
}
```

- [ ] **Step 2: Ejecutar para ver que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~PaymentCalculatorOfferTests"`
Expected: error de compilación (`Calculate` no acepta 5 argumentos, `CalculateCycle` no existe, `OfferMonthsCovered` no existe).

- [ ] **Step 3: Implementar**

En `PaymentCalculator.cs`, reemplazar el `using` inicial, el record y `Calculate`. El archivo queda así hasta `GetAmountForMonth` (el resto del archivo no cambia):

```csharp
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentCalculationResult(
    decimal MonthlyFee,
    int MonthsCovered,
    decimal GrossAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total,
    int OfferMonthsCovered = 0,
    decimal? OfferUnitPrice = null);

/// <summary>
/// Cálculo del pago por adelantado de un plan (renta o mensualidad de comprobantes).
/// Función pura: no toca base de datos ni configuración.
/// </summary>
public static class PaymentCalculator
{
    /// <summary>Días de anticipación para considerar un pago próximo a vencer.</summary>
    public const int DefaultWarningDays = 15;

    /// <summary>
    /// Con <paramref name="offer"/> y <paramref name="periodStart"/> cada mes cubierto se cobra a su precio:
    /// el mes k empieza en <c>periodStart + k meses</c> y usa el precio de oferta si esa fecha es anterior al
    /// fin de la oferta. Sin alguno de los dos se cobra el precio real en todos los meses.
    /// </summary>
    public static PaymentCalculationResult Calculate(
        decimal monthlyFee,
        int paidMonths,
        decimal discountPercent,
        OfferTerms? offer = null,
        DateTime? periodStart = null)
    {
        var months = Math.Max(1, paidMonths);
        var offerMonths = offer is not null && periodStart is DateTime start
            ? CountOfferMonths(offer, start, months)
            : 0;

        var gross = offerMonths * (offer?.OfferPrice ?? 0m) + (months - offerMonths) * monthlyFee;
        var discount = Math.Round(gross * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new PaymentCalculationResult(
            monthlyFee,
            months,
            gross,
            discountPercent,
            discount,
            gross - discount,
            offerMonths,
            offerMonths > 0 ? offer!.OfferPrice : null);
    }

    /// <summary>
    /// Ciclo de pago de un cliente con su plan y su oferta. El primer mes cubierto empieza en el próximo pago,
    /// o en <paramref name="fallbackPeriodStart"/> si el cliente no tiene fecha.
    /// </summary>
    public static PaymentCalculationResult CalculateCycle(Client client, DateTime fallbackPeriodStart)
    {
        var plan = client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client));
        var offer = OfferPricing.TermsFor(plan.OfferPrice, plan.OfferMonths, client.OfferStartDate);

        return Calculate(
            plan.MonthlyFee,
            client.PaidMonths,
            client.PrepaymentDiscountPercent,
            offer,
            client.NextPaymentDate?.Date ?? fallbackPeriodStart.Date);
    }

    private static int CountOfferMonths(OfferTerms offer, DateTime periodStart, int months)
    {
        var count = 0;
        while (count < months && periodStart.Date.AddMonths(count) < offer.EndDate) count++;
        return count;
    }
```

Dejar `GetAmountForMonth`, `IsMonthCovered` y `GetPaymentStatus` exactamente como están, y cerrar la clase.

- [ ] **Step 4: Ejecutar todas las pruebas de Billing**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: pasan las nuevas y las existentes (`PaymentCalculatorTests`, `BillingCalculatorTests`, etc.), 0 fallidas.

- [ ] **Step 5: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs ZynstormECFPlatform.Tests/Billing/PaymentCalculatorOfferTests.cs
git commit -q -m "$(cat <<'EOF'
Cobrar la mensualidad mes por mes con la oferta del plan

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 4: API de planes (DTO, mapeo y validación)

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/PlanDtos.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs`

**Interfaces:**
- Consumes: `OfferPricing.ValidateOffer` (Task 2).
- Produces: `PlanCreateDto.OfferPrice (decimal?)` y `PlanCreateDto.OfferMonths (int?)`, heredados por `PlanUpdateDto` y `PlanViewDto`. AutoMapper los mapea por nombre en `PlanCreateDto → Plan`, `PlanUpdateDto → Plan` y `Plan → PlanViewDto` (no hace falta tocar `MappingProfiles` para planes).

- [ ] **Step 1: Agregar los campos al DTO**

En `PlanDtos.cs`, dentro de `PlanCreateDto`, después de `MaxUsers`:

```csharp
    /// <summary>Precio mensual promocional. Va junto con <see cref="OfferMonths"/>; null = sin oferta.</summary>
    [Range(0, 99999999)]
    public decimal? OfferPrice { get; set; }

    /// <summary>Meses que dura la oferta desde que se asigna el plan al cliente.</summary>
    [Range(1, 120)]
    public int? OfferMonths { get; set; }
```

- [ ] **Step 2: Validar la oferta en el controller**

En `PlanController.cs`, reemplazar el método `Validate`:

```csharp
        private static List<string> Validate(PlanCreateDto dto)
        {
            var errors = BillingCalculator.ValidatePlan(
                (PlanTypeEnum)dto.PlanTypeId,
                dto.MonthlyDocumentLimit,
                dto.MonthlyFee,
                dto.MaxUsers,
                dto.OverageTiers.Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)));

            errors.AddRange(OfferPricing.ValidateOffer(dto.MonthlyFee, dto.OfferPrice, dto.OfferMonths));
            return errors;
        }
```

`PlanController` ya importa `ZynstormECFPlatform.Services.Billing`.

- [ ] **Step 3: Compilar**

Run: `dotnet build ZynstormECFPlatform.Web.Api`
Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Dtos/PlanDtos.cs ZynstormECFPlatform.Web.Api/Controllers/PlanController.cs
git commit -q -m "$(cat <<'EOF'
Exponer y validar la oferta en la API de planes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 5: Registro de pagos, previsualización, recibos y aviso con la oferta

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientPaymentRegistrationDtos.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/PaymentReminderService.cs`

**Interfaces:**
- Consumes: `PaymentCalculator.CalculateCycle`, `PaymentCalculationResult.OfferMonthsCovered/OfferUnitPrice` (Task 3), `OfferPricing.GetFullPriceUnit` (Task 2), `ClientPaymentItem.OfferMonthsCovered/OfferUnitPrice` (Task 1).
- Produces (JSON hacia el frontend):
  - `PlanFeePreviewDto.OfferMonthsCovered (int)`, `PlanFeePreviewDto.OfferUnitPrice (decimal?)`.
  - `PaymentReceiptItemDto.OfferMonthsCovered (int?)`, `OfferUnitPrice (decimal?)`, `FullPriceMonthsCovered (int?)`, `FullPriceUnitPrice (decimal?)` — solo se llenan en líneas de mensualidad que tuvieron meses de oferta.

- [ ] **Step 1: Ampliar los DTOs**

En `ClientPaymentRegistrationDtos.cs`, en `PlanFeePreviewDto`, después de `Total`:

```csharp
    /// <summary>Meses del pago cobrados a precio de oferta (0 si no aplica).</summary>
    public int OfferMonthsCovered { get; set; }

    /// <summary>Precio mensual de oferta aplicado; null si ningún mes fue de oferta.</summary>
    public decimal? OfferUnitPrice { get; set; }
```

En `PaymentReceiptItemDto`, después de `DiscountAmount`:

```csharp
    /// <summary>Meses cobrados a precio de oferta; null si el pago no tuvo oferta.</summary>
    public int? OfferMonthsCovered { get; set; }

    public decimal? OfferUnitPrice { get; set; }

    /// <summary>Meses cobrados a precio real cuando el pago mezcló oferta y precio real.</summary>
    public int? FullPriceMonthsCovered { get; set; }

    public decimal? FullPriceUnitPrice { get; set; }
```

- [ ] **Step 2: Registrar el pago con el ciclo con oferta**

En `ClientPaymentRegistrationService.RegisterAsync`, dentro de `if (request.IncludePlanFee)`, reemplazar la primera línea del bloque

```csharp
            var calculation = PaymentCalculator.Calculate(client.Plan.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);
```

por

```csharp
            var calculation = PaymentCalculator.CalculateCycle(client, paymentDate);
```

y en el inicializador de `new ClientPaymentItem { ... }` de la mensualidad, después de `DiscountAmount = calculation.DiscountAmount,` agregar:

```csharp
                OfferMonthsCovered = calculation.OfferMonthsCovered > 0 ? calculation.OfferMonthsCovered : null,
                OfferUnitPrice = calculation.OfferUnitPrice,
```

(El primer mes cubierto empieza en `client.NextPaymentDate` o, si no hay, en `paymentDate`; es la misma fecha desde la que `CalculateNewNextPaymentDate` encadena el ciclo.)

- [ ] **Step 3: Previsualización con oferta**

En `BuildPlanFeePreview`, reemplazar la línea del cálculo

```csharp
        var calculation = PaymentCalculator.Calculate(client.Plan!.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);
```

por

```csharp
        var calculation = PaymentCalculator.CalculateCycle(client, today);
```

y en el inicializador de `PlanFeePreviewDto`, después de `Total = calculation.Total,` agregar:

```csharp
            OfferMonthsCovered = calculation.OfferMonthsCovered,
            OfferUnitPrice = calculation.OfferUnitPrice,
```

`BuildPlanFeePreview` sigue usando `client.Plan!.Name` y `client.Plan.PlanTypeId` más abajo; no cambian.

- [ ] **Step 4: Desglose en el recibo**

En `ToReceipt`, en el inicializador de `PaymentReceiptItemDto` (dentro de `.Select(i => new PaymentReceiptItemDto { ... })`), después de `DiscountAmount = i.DiscountAmount,` agregar:

```csharp
                OfferMonthsCovered = HasOffer(i) ? i.OfferMonthsCovered : null,
                OfferUnitPrice = HasOffer(i) ? i.OfferUnitPrice : null,
                FullPriceMonthsCovered = HasOffer(i) ? i.MonthsCovered - i.OfferMonthsCovered : null,
                FullPriceUnitPrice = HasOffer(i)
                    ? OfferPricing.GetFullPriceUnit(i.GrossAmount ?? i.Amount, i.MonthsCovered!.Value, i.OfferMonthsCovered!.Value, i.OfferUnitPrice!.Value)
                    : null,
```

y agregar el método privado junto a `DescribeItem`:

```csharp
    private static bool HasOffer(ClientPaymentItem item) =>
        item is { ItemType: (int)ClientPaymentItemType.PlanFee, OfferMonthsCovered: > 0, OfferUnitPrice: not null, MonthsCovered: not null };
```

- [ ] **Step 5: Monto del aviso de pago con oferta**

En `PaymentReminderService.EmailData`, reemplazar

```csharp
        var amount = PaymentCalculator.Calculate(client.Plan!.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent).Total;
```

por

```csharp
        var amount = PaymentCalculator.CalculateCycle(client, due).Total;
```

(`due` ya está declarado en la línea anterior del método.) Como esa línea era la que llevaba el `client.Plan!`, en el `return new PaymentReminderEmailData(...)` cambiar `client.Plan.Name` por `client.Plan!.Name` y `client.Plan.PlanTypeId` por `client.Plan!.PlanTypeId` para no introducir advertencias de nulabilidad.

- [ ] **Step 6: Compilar y correr las pruebas de Billing**

Run: `dotnet build ZynstormECFPlatform.Services && dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: `Build succeeded.` y todas las pruebas pasan (incluidas `PaymentReminderEmailsTests`).

- [ ] **Step 7: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Services
git commit -q -m "$(cat <<'EOF'
Aplicar la oferta al registro de pagos, recibos y avisos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 6: API de clientes (inicio de oferta, campos calculados, filtro, ciclo con oferta)

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs`
- Modify: `ZynstormECFPlatform.Mappings/MappingProfiles.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`

**Interfaces:**
- Consumes: `OfferPricing.*` (Task 2), `PaymentCalculator.CalculateCycle` (Task 3), `Plan.OfferPrice/OfferMonths`, `Client.OfferStartDate` (Task 1).
- Produces (JSON de `ClientViewDto`): `planOfferPrice`, `planOfferMonths`, `offerStartDate`, `offerEndDate` (fechas `yyyy-MM-dd`), `offerDaysRemaining`, `offerMonthsRemaining`, `offerStatus` (0 sin oferta, 1 activa, 2 finalizada). `GET /v1/Client?offerExpiring=true` restringe a clientes con oferta activa que vence en 30 días o menos.

- [ ] **Step 1: DTO del cliente**

En `ClientDtos.cs`, en `ClientViewDto`, después de `PlanMonthlyFee`:

```csharp
    /// <summary>Precio mensual de oferta del plan. Null si el plan no tiene oferta.</summary>
    public decimal? PlanOfferPrice { get; set; }

    public int? PlanOfferMonths { get; set; }

    /// <summary>Día en que empezó la oferta para este cliente. Null si no tiene oferta.</summary>
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? OfferStartDate { get; set; }

    /// <summary>Primer día en que se cobra el precio real.</summary>
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? OfferEndDate { get; set; }

    /// <summary>Días calendario que faltan para terminar la oferta. Null si no está activa.</summary>
    public int? OfferDaysRemaining { get; set; }

    /// <summary>Meses completos que faltan para terminar la oferta. Null si no está activa.</summary>
    public int? OfferMonthsRemaining { get; set; }

    /// <summary>0 = sin oferta, 1 = activa, 2 = finalizada.</summary>
    public int OfferStatus { get; set; }
```

- [ ] **Step 2: Mapeo**

En `MappingProfiles.cs`, en `CreateMap<Client, ClientViewDto>()`, después de la línea de `PlanMonthlyFee`:

```csharp
            .ForMember(dest => dest.PlanOfferPrice, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.OfferPrice : null))
            .ForMember(dest => dest.PlanOfferMonths, opt => opt.MapFrom(src => src.Plan != null ? src.Plan.OfferMonths : null))
```

`OfferStartDate` se mapea solo por nombre. `ClientCreateDto`/`ClientUpdateDto` no tienen `OfferStartDate`, así que guardar un cliente nunca lo pisa.

- [ ] **Step 3: Helpers en `ClientController`**

Agregar, junto a `PlanExistsAsync` (línea ~970), reemplazándolo:

```csharp
        private async Task<Plan?> FindPlanAsync(int? planId) =>
            planId.HasValue ? await planService.GetNoTrackingByAsync(p => p.PlanId == planId.Value) : null;

        /// <summary>Completa fin, días restantes y estado de la oferta del cliente; devuelve las condiciones vigentes.</summary>
        private static OfferTerms? FillOffer(ClientViewDto dto)
        {
            var terms = OfferPricing.TermsFor(dto.PlanOfferPrice, dto.PlanOfferMonths, dto.OfferStartDate);
            var state = OfferPricing.Evaluate(terms, DateTimeExtensions.DrNow.Date);

            dto.OfferStatus = (int)state.Status;
            dto.OfferEndDate = state.EndDate;
            dto.OfferDaysRemaining = state.DaysRemaining;
            dto.OfferMonthsRemaining = state.MonthsRemaining;
            return terms;
        }

        /// <summary>Ids de los clientes con oferta activa que vence dentro de la ventana de aviso.</summary>
        private async Task<List<int>> OfferExpiringClientIdsAsync(CancellationToken cancellationToken)
        {
            var today = DateTimeExtensions.DrNow.Date;

            var candidates = await Repository.Table.AsNoTracking()
                .Where(c => c.OfferStartDate != null && c.Plan != null && c.Plan.OfferPrice != null && c.Plan.OfferMonths != null)
                .Select(c => new { c.ClientId, c.OfferStartDate, c.Plan!.OfferPrice, c.Plan.OfferMonths })
                .ToListAsync(cancellationToken);

            return candidates
                .Where(c => OfferPricing.IsExpiringSoon(OfferPricing.TermsFor(c.OfferPrice, c.OfferMonths, c.OfferStartDate), today))
                .Select(c => c.ClientId)
                .ToList();
        }
```

- [ ] **Step 4: Campos calculados y ciclo con oferta en `FillPaymentStatusAsync`**

Reemplazar el cuerpo del `foreach` de `FillPaymentStatusAsync`:

```csharp
            foreach (var dto in list)
            {
                var offer = FillOffer(dto);
                dto.ActiveUsersCount = countByClient.TryGetValue(dto.ClientId, out var count) ? count : 0;

                if (dto.PlanTypeId is null) continue;

                var (status, days) = PaymentCalculator.GetPaymentStatus(dto.NextPaymentDate, PaymentWarningDays);
                dto.PaymentStatus = (int)status;
                dto.PaymentDaysToDue = days;
                dto.PaymentCycleAmount = PaymentCalculator
                    .Calculate(
                        dto.PlanMonthlyFee ?? 0m,
                        dto.PaidMonths,
                        dto.PrepaymentDiscountPercent,
                        offer,
                        dto.NextPaymentDate?.Date ?? DateTimeExtensions.DrNow.Date)
                    .Total;
            }
```

- [ ] **Step 5: Ciclo con oferta en renta mensual y en `payments`**

En `BuildRentUsageRowsAsync`, reemplazar

```csharp
                var calculation = PaymentCalculator.Calculate(c.Plan!.MonthlyFee, c.PaidMonths, c.PrepaymentDiscountPercent);
```

por

```csharp
                var calculation = PaymentCalculator.CalculateCycle(c, DateTimeExtensions.DrNow.Date);
```

En `GetPaymentClients`, reemplazar la misma línea por la misma llamada:

```csharp
                    var calculation = PaymentCalculator.CalculateCycle(c, DateTimeExtensions.DrNow.Date);
```

Ambos usan `ActivePlanClientsQuery()`, que ya incluye `Plan`. No tocar `BuildUsageDtosAsync`.

- [ ] **Step 6: Filtro `offerExpiring` en el listado**

En `Get`, después de la línea de `includeInactive`:

```csharp
                bool offerExpiring = bool.TryParse(Request.Query["offerExpiring"], out var oe) && oe;
```

y después del bloque `if (!includeInactive) { ... }`:

```csharp
                if (offerExpiring)
                {
                    var expiringIds = await OfferExpiringClientIdsAsync(cancellationToken);
                    listQuery = listQuery.Where(c => expiringIds.Contains(c.ClientId));
                }
```

- [ ] **Step 7: Fecha de inicio al crear**

En `Post`, reemplazar

```csharp
                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");
```

por

```csharp
                var plan = await FindPlanAsync(dto.PlanId);
                if (dto.PlanId.HasValue && plan == null)
                    return BadRequest("El plan seleccionado no existe.");
```

y, dentro de la transacción, justo después de `model = Mapper.Map<ClientCreateDto, Client>(dto);`:

```csharp
                    model.OfferStartDate = OfferPricing.ResolveStartDate(
                        null, dto.PlanId, OfferPricing.PlanHasOffer(plan?.OfferPrice, plan?.OfferMonths), null, DateTimeExtensions.DrNow.Date);
```

Y el final del `try` (`return Ok(Mapper.Map<Client, ClientViewDto>(model!));`) pasa a:

```csharp
                var created = Mapper.Map<Client, ClientViewDto>(model!);
                FillOffer(created);
                return Ok(created);
```

- [ ] **Step 8: Fecha de inicio al actualizar**

En `Put`, reemplazar el mismo `if (!await PlanExistsAsync(dto.PlanId)) ...` por el mismo bloque de `FindPlanAsync` del Step 7. Antes de `Mapper.Map(dto, model);` agregar:

```csharp
                var previousPlanId = model.PlanId;
```

y justo después de `Mapper.Map(dto, model);`:

```csharp
                model.OfferStartDate = OfferPricing.ResolveStartDate(
                    previousPlanId, model.PlanId, OfferPricing.PlanHasOffer(plan?.OfferPrice, plan?.OfferMonths), model.OfferStartDate, DateTimeExtensions.DrNow.Date);
```

El final (`return Ok(Mapper.Map<Client, ClientViewDto>(model));`) pasa a:

```csharp
                var updated = Mapper.Map<Client, ClientViewDto>(model);
                FillOffer(updated);
                return Ok(updated);
```

- [ ] **Step 9: `GetByGuid` con los campos calculados**

En `GetByGuid`, reemplazar `return Ok(Mapper.Map<Client, ClientViewDto>(result));` por:

```csharp
                var view = Mapper.Map<Client, ClientViewDto>(result);
                FillOffer(view);
                return Ok(view);
```

- [ ] **Step 10: Compilar y probar**

Run: `grep -n "PlanExistsAsync" ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`
Expected: sin resultados (ya no queda ninguna referencia).

Run: `dotnet build ZynstormECFPlatform.Web.Api && dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: `Build succeeded.` y pruebas en verde.

- [ ] **Step 11: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Mappings ZynstormECFPlatform.Web.Api
git commit -q -m "$(cat <<'EOF'
Seguimiento de ofertas en la API de clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 7: Frontend — tipos, servicio y utilidades de oferta

**Files:**
- Create: `types/offer.type.ts`
- Create: `lib/offer.ts`
- Modify: `types/plan.type.ts`, `types/client.type.ts`, `types/paymentRegistration.type.ts`
- Modify: `services/client.service.ts`, `hooks/useClients.ts`

**Interfaces:**
- Consumes: JSON de las Tasks 4–6.
- Produces: `OfferStatus` (enum), `OFFER_WARNING_DAYS`, `hasPlanOffer`, `offerEndDate`, `countOfferMonths`, `estimateCycleGross`, `offerBadge`, `getClients(..., offerExpiring?)`, `loadClients(..., offerExpiring?)`.

Trabajar en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.

- [ ] **Step 1: Línea base de tipos**

Run: `npx tsc --noEmit 2>&1 | tail -5`
Expected: anotar el resultado actual (sin errores, o la lista de errores previos) para distinguir lo nuevo de lo existente.

- [ ] **Step 2: Crear `types/offer.type.ts`**

```ts
export enum OfferStatus {
  None = 0,
  Active = 1,
  Ended = 2,
}

/** Días antes del fin de la oferta en que se marca como por vencer. Igual que OfferPricing.DefaultWarningDays. */
export const OFFER_WARNING_DAYS = 30
```

- [ ] **Step 3: Ampliar los tipos existentes**

`types/plan.type.ts`, en `PlanCreate`, después de `maxUsers: number | null`:

```ts
  offerPrice: number | null
  offerMonths: number | null
```

`types/client.type.ts`, en `Client`, después de `planMonthlyFee?: number | null`:

```ts
  planOfferPrice?: number | null
  planOfferMonths?: number | null
  offerStartDate?: string | null
  offerEndDate?: string | null
  offerDaysRemaining?: number | null
  offerMonthsRemaining?: number | null
  offerStatus?: number
```

`types/paymentRegistration.type.ts`, en `PlanFeePreview`, después de `total: number`:

```ts
  offerMonthsCovered: number
  offerUnitPrice: number | null
```

y en `PaymentReceiptItem`, después de `discountAmount: number | null`:

```ts
  offerMonthsCovered: number | null
  offerUnitPrice: number | null
  fullPriceMonthsCovered: number | null
  fullPriceUnitPrice: number | null
```

- [ ] **Step 4: Crear `lib/offer.ts`**

```ts
import { addMonthsToDate, formatCalendarDate } from "@/lib/payment"
import type { Client } from "@/types/client.type"
import { OFFER_WARNING_DAYS, OfferStatus } from "@/types/offer.type"

export const hasPlanOffer = (offerPrice?: number | null, offerMonths?: number | null): boolean =>
  offerPrice != null && offerMonths != null && offerMonths > 0

/** Primer día a precio real: inicio de la oferta + meses de oferta. */
export const offerEndDate = (startDate: string, offerMonths: number): string => addMonthsToDate(startDate, offerMonths)

const monthStart = (periodStart: string, offset: number): string =>
  offset === 0 ? periodStart.slice(0, 10) : addMonthsToDate(periodStart, offset)

/** Meses del pago que caen dentro de la oferta. Misma regla que PaymentCalculator.Calculate del servidor. */
export const countOfferMonths = (periodStart: string, endDate: string, months: number): number => {
  let count = 0
  while (count < months && monthStart(periodStart, count) < endDate) count += 1
  return count
}

export interface CycleEstimate {
  gross: number
  offerMonths: number
}

/** Estimado del bruto de un ciclo pagado, mes por mes. El servidor es quien calcula el cobro real. */
export const estimateCycleGross = (input: {
  monthlyFee: number
  months: number
  offerPrice?: number | null
  offerMonths?: number | null
  offerStartDate?: string | null
  periodStart: string
}): CycleEstimate => {
  const { monthlyFee, months, offerPrice, offerMonths, offerStartDate, periodStart } = input

  if (!hasPlanOffer(offerPrice, offerMonths) || !offerStartDate) {
    return { gross: monthlyFee * months, offerMonths: 0 }
  }

  const end = offerEndDate(offerStartDate, offerMonths as number)
  const inOffer = countOfferMonths(periodStart, end, months)

  return { gross: inOffer * (offerPrice as number) + (months - inOffer) * monthlyFee, offerMonths: inOffer }
}

export interface OfferBadge {
  label: string
  className: string
  title: string
}

/** Etiqueta del estado de la oferta en la lista de clientes; null si el cliente no tiene oferta. */
export const offerBadge = (
  client: Pick<Client, "offerStatus" | "offerDaysRemaining" | "offerEndDate">,
): OfferBadge | null => {
  if (client.offerStatus === OfferStatus.Active && client.offerDaysRemaining != null) {
    const days = client.offerDaysRemaining
    return {
      label: days === 1 ? "Oferta: falta 1 día" : `Oferta: faltan ${days} días`,
      className:
        days <= OFFER_WARNING_DAYS
          ? "bg-amber-100 text-amber-800 hover:bg-amber-100"
          : "bg-sky-100 text-sky-800 hover:bg-sky-100",
      title: `Oferta hasta el ${formatCalendarDate(client.offerEndDate)}; después se cobra el precio real`,
    }
  }

  if (client.offerStatus === OfferStatus.Ended) {
    return {
      label: "Oferta finalizada",
      className: "bg-secondary text-secondary-foreground hover:bg-secondary",
      title: `La oferta terminó el ${formatCalendarDate(client.offerEndDate)}`,
    }
  }

  return null
}
```

- [ ] **Step 5: Parámetro `offerExpiring` en servicio y hook**

`services/client.service.ts`, reemplazar `getClients`:

```ts
export const getClients = async (
  pageNumber?: number,
  pageSize?: number,
  search?: string,
  includeInactive?: boolean,
  offerExpiring?: boolean,
): Promise<Client[] | PaginatedResponse<Client>> => {
  const params = new URLSearchParams()
  if (pageNumber !== undefined) params.append("pageNumber", pageNumber.toString())
  if (pageSize !== undefined) params.append("pageSize", pageSize.toString())
  if (search !== undefined) params.append("search", search)
  if (includeInactive !== undefined) params.append("includeInactive", includeInactive.toString())
  if (offerExpiring) params.append("offerExpiring", "true")

  const queryString = params.toString()
  const url = queryString ? `${CLIENT_URL}?${queryString}` : CLIENT_URL

  return await get<Client[] | PaginatedResponse<Client>>(url)
}
```

`hooks/useClients.ts`, en `loadClients`: agregar el parámetro `offerExpiring?: boolean,` después de `includeInactive?: boolean,` y cambiar la llamada a:

```ts
        const data = await getClients(pageNumber, pageSize, search, includeInactive, offerExpiring)
```

- [ ] **Step 6: Verificar tipos**

Run: `npx tsc --noEmit 2>&1 | tail -20`
Expected: aparecen errores **solo** en `app/configuraciones/planes/page.tsx` (los objetos `emptyPlan`, `emptyRentPlan` y `openEdit` ya no cumplen `PlanCreate` porque faltan `offerPrice`/`offerMonths`); se arreglan en la Task 8. Ningún otro error nuevo respecto a la línea base.

- [ ] **Step 7: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add types lib services hooks
git commit -q -m "$(cat <<'EOF'
Agregar tipos y utilidades de ofertas de plan

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 8: Frontend — formulario y lista de planes

**Files:**
- Modify: `app/configuraciones/planes/page.tsx`

**Interfaces:**
- Consumes: `PlanCreate.offerPrice/offerMonths` (Task 7).

- [ ] **Step 1: Valores iniciales**

En `emptyPlan` y en `emptyRentPlan`, después de `maxUsers: ...,`:

```ts
  offerPrice: null,
  offerMonths: null,
```

En `openEdit`, después de `maxUsers: plan.maxUsers ?? null,`:

```ts
      offerPrice: plan.offerPrice ?? null,
      offerMonths: plan.offerMonths ?? null,
```

- [ ] **Step 2: Mostrar la oferta en la lista**

Reemplazar la celda del monto:

```tsx
                      <TableCell>{currency.format(plan.monthlyFee)}</TableCell>
```

por:

```tsx
                      <TableCell>
                        {currency.format(plan.monthlyFee)}
                        {plan.offerPrice != null && plan.offerMonths != null && (
                          <p className="text-xs text-muted-foreground">
                            Oferta: {currency.format(plan.offerPrice)} × {plan.offerMonths}{" "}
                            {plan.offerMonths === 1 ? "mes" : "meses"}
                          </p>
                        )}
                      </TableCell>
```

- [ ] **Step 3: Campos en el formulario**

En el formulario, justo después del campo de descripción (después del `</Field>` que cierra `id="plan-description"`), insertar:

```tsx
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field>
                    <FieldLabel htmlFor="plan-offer-price">Precio de oferta (RD$)</FieldLabel>
                    <Input
                      id="plan-offer-price"
                      type="number"
                      min={0}
                      step="0.01"
                      placeholder="Sin oferta"
                      value={formData.offerPrice ?? ""}
                      onChange={(e) =>
                        setFormData((c) => ({ ...c, offerPrice: e.target.value === "" ? null : Number(e.target.value) }))
                      }
                    />
                  </Field>
                  <Field>
                    <FieldLabel htmlFor="plan-offer-months">Meses de oferta</FieldLabel>
                    <Input
                      id="plan-offer-months"
                      type="number"
                      min={1}
                      max={120}
                      step={1}
                      placeholder="Sin oferta"
                      value={formData.offerMonths ?? ""}
                      onChange={(e) =>
                        setFormData((c) => ({ ...c, offerMonths: e.target.value === "" ? null : Number(e.target.value) }))
                      }
                    />
                  </Field>
                </div>
                <p className="-mt-2 text-xs text-muted-foreground">
                  Opcional. Durante esos meses, contados desde que se asigna el plan al cliente, se cobra el precio de
                  oferta; después se cobra la mensualidad normal. Déjalos vacíos si el plan no tiene oferta.
                </p>
```

El error del servidor (por ejemplo "El precio de oferta debe ser menor que la mensualidad.") ya se muestra con el `error` existente de la página.

- [ ] **Step 4: Verificar tipos**

Run: `npx tsc --noEmit 2>&1 | tail -20`
Expected: sin errores nuevos (los de la Task 7 Step 6 desaparecen).

- [ ] **Step 5: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add app/configuraciones/planes/page.tsx
git commit -q -m "$(cat <<'EOF'
Campos de oferta en el formulario y la lista de planes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 9: Frontend — seguimiento de ofertas en `/clientes`

**Files:**
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: `offerBadge`, `hasPlanOffer`, `estimateCycleGross` (Task 7), `loadClients(..., offerExpiring)` (Task 7), campos `offer*` del cliente (Task 6).

- [ ] **Step 1: Imports**

En el import de `lucide-react` (línea con `FileKey, Upload, Shield, ...`), agregar `Tag`:

```tsx
import { FileKey, Upload, Shield, Calendar, Fingerprint, Key, Copy, Check, Eye, EyeOff, Percent, Users, Loader2, Tag } from "lucide-react"
```

En el import de `@/lib/payment`, agregar `todayDrYmd`:

```tsx
import {
  formatCalendarDate,
  formatMonths,
  formatUserSeats,
  paymentStatusBadgeClass,
  paymentStatusLabel,
  suggestNextPaymentDate,
  todayDrYmd,
} from "@/lib/payment"
import { estimateCycleGross, hasPlanOffer, offerBadge } from "@/lib/offer"
import { OfferStatus } from "@/types/offer.type"
```

- [ ] **Step 2: Componente del badge**

Después de la función `getLocalCertificationStatus` (antes de `export default function ClientesPage`):

```tsx
function ClientOfferBadge({ client }: { client: Client }) {
  const badge = offerBadge(client)
  if (!badge) return null

  return (
    <Badge className={cn("gap-1 whitespace-nowrap font-normal", badge.className)} title={badge.title}>
      <Tag className="h-3 w-3" />
      {badge.label}
    </Badge>
  )
}
```

- [ ] **Step 3: Estimado del ciclo con oferta**

Reemplazar la línea

```tsx
  const paymentGross = (selectedPlan?.monthlyFee ?? 0) * paymentMonths
```

por:

```tsx
  const planHasOffer = hasPlanOffer(selectedPlan?.offerPrice, selectedPlan?.offerMonths)
  const keepsCurrentPlan = Boolean(editingClient && selectedPlan && editingClient.planId === selectedPlan.planId)
  // Con otro plan (o un cliente nuevo) la oferta arranca hoy al guardar; con el mismo plan se conserva su fecha.
  const offerStartDate = planHasOffer ? (keepsCurrentPlan ? (editingClient?.offerStartDate ?? null) : todayDrYmd()) : null
  const cycleEstimate = estimateCycleGross({
    monthlyFee: selectedPlan?.monthlyFee ?? 0,
    months: paymentMonths,
    offerPrice: selectedPlan?.offerPrice,
    offerMonths: selectedPlan?.offerMonths,
    offerStartDate,
    periodStart: formData.nextPaymentDate ?? todayDrYmd(),
  })
  const paymentGross = cycleEstimate.gross
```

Ese bloque queda después de `const paymentMonths = ...` y antes de `const paymentDiscount = ...`, que ya usa `paymentGross`.

- [ ] **Step 4: Filtro "Solo ofertas por vencer"**

Junto a `const [showInactive, setShowInactive] = useState(false)` agregar:

```tsx
  const [offerExpiringOnly, setOfferExpiringOnly] = useState(false)
```

En `fetchClients`:

```tsx
  const fetchClients = useCallback(async () => {
    const response = await loadClients(currentPage, pageSize, debouncedSearchQuery, showInactive, offerExpiringOnly)
    if (response) {
      setTotalPages(Math.max(1, response.totalPages))
      setTotalCount(response.totalCount)
      if (!debouncedSearchQuery) {
        setUnfilteredTotalCount(response.totalCount)
      }
    }
  }, [loadClients, currentPage, pageSize, debouncedSearchQuery, showInactive, offerExpiringOnly])
```

Después del `<label>` de "Mostrar inactivos" (el que cierra con `Mostrar inactivos\n            </label>`), insertar:

```tsx
            <label className="flex h-9 cursor-pointer items-center gap-2 rounded-md border border-input bg-background px-3 text-sm">
              <Checkbox
                checked={offerExpiringOnly}
                onCheckedChange={(checked) => {
                  setOfferExpiringOnly(checked === true)
                  setCurrentPage(1)
                }}
              />
              Ofertas por vencer
            </label>
```

- [ ] **Step 5: Badge en la lista**

En la celda de precio, después del bloque del badge de estado de pago (el que termina en `{paymentStatusLabel(client.paymentStatus as PaymentStatus, client.paymentDaysToDue)}\n                                  </Badge>\n                                )}`), agregar antes del `</div>` que cierra el contenedor `flex flex-col items-center gap-1`:

```tsx
                              <ClientOfferBadge client={client} />
```

- [ ] **Step 6: Panel de oferta en el diálogo del cliente**

Insertar antes del bloque `{selectedPlan && (` que abre la sección "Pago" (el que contiene `<h4 ...>Pago</h4>`):

```tsx
                  {selectedPlan && planHasOffer && (
                    <div className="rounded-lg border border-sky-200 bg-sky-50 px-4 py-3 text-xs text-sky-900">
                      <p className="mb-1 flex items-center gap-1.5 text-sm font-semibold">
                        <Tag className="h-3.5 w-3.5" />
                        Oferta del plan
                      </p>
                      {keepsCurrentPlan && editingClient?.offerStartDate ? (
                        <dl className="grid grid-cols-2 gap-y-1">
                          <dt>Inicio</dt>
                          <dd className="text-right">{formatCalendarDate(editingClient.offerStartDate)}</dd>
                          <dt>Fin</dt>
                          <dd className="text-right">{formatCalendarDate(editingClient.offerEndDate)}</dd>
                          <dt>Tiempo restante</dt>
                          <dd className="text-right font-medium">
                            {editingClient.offerStatus === OfferStatus.Active && editingClient.offerDaysRemaining != null
                              ? `${editingClient.offerDaysRemaining} ${editingClient.offerDaysRemaining === 1 ? "día" : "días"}${
                                  editingClient.offerMonthsRemaining
                                    ? ` (${formatMonths(editingClient.offerMonthsRemaining)})`
                                    : ""
                                }`
                              : "Oferta finalizada"}
                          </dd>
                          <dt>Precio de oferta</dt>
                          <dd className="text-right">{formatCurrency(selectedPlan.offerPrice ?? 0)}</dd>
                          <dt>Precio real</dt>
                          <dd className="text-right">{formatCurrency(selectedPlan.monthlyFee)}</dd>
                        </dl>
                      ) : keepsCurrentPlan ? (
                        <p>Este cliente tenía el plan antes de la oferta, así que se le cobra el precio real.</p>
                      ) : (
                        <p>
                          Al guardar, la oferta empieza hoy: {formatCurrency(selectedPlan.offerPrice ?? 0)} durante{" "}
                          {formatMonths(selectedPlan.offerMonths ?? 0)}; después, {formatCurrency(selectedPlan.monthlyFee)}.
                        </p>
                      )}
                    </div>
                  )}

```

- [ ] **Step 7: Desglose en el "Total del pago" del diálogo**

En el bloque `bg-secondary/40`, reemplazar la línea del primer renglón

```tsx
                            {formatCurrency(selectedPlan.monthlyFee)} × {formatMonths(paymentMonths)}
```

por (con meses de oferta el renglón describe los dos tramos; sin ellos queda igual que antes):

```tsx
                            {cycleEstimate.offerMonths > 0
                              ? `${formatCurrency(selectedPlan.offerPrice ?? 0)} × ${formatMonths(cycleEstimate.offerMonths)} (oferta)` +
                                (paymentMonths > cycleEstimate.offerMonths
                                  ? ` + ${formatCurrency(selectedPlan.monthlyFee)} × ${formatMonths(paymentMonths - cycleEstimate.offerMonths)}`
                                  : "")
                              : `${formatCurrency(selectedPlan.monthlyFee)} × ${formatMonths(paymentMonths)}`}
```

El `{isRentPlan ? "" : " (solo mensualidad; ...)"}` que sigue en el mismo `<span>` no cambia.

- [ ] **Step 8: Verificar tipos y lint**

Run: `npx tsc --noEmit 2>&1 | tail -20 && npx eslint app/clientes/page.tsx lib/offer.ts`
Expected: sin errores nuevos respecto a la línea base.

- [ ] **Step 9: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add app/clientes/page.tsx
git commit -q -m "$(cat <<'EOF'
Seguimiento de ofertas en la lista y el diálogo de clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 10: Frontend — desglose de oferta al registrar y en el recibo

**Files:**
- Modify: `components/payments/register-payment-dialog.tsx`
- Modify: `components/payments/payment-receipt-dialog.tsx`

**Interfaces:**
- Consumes: `PlanFeePreview.offerMonthsCovered/offerUnitPrice`, `PaymentReceiptItem.offerMonthsCovered/offerUnitPrice/fullPriceMonthsCovered/fullPriceUnitPrice` (Task 7).

- [ ] **Step 1: Previsualización del pago**

En `register-payment-dialog.tsx`, después del bloque `{preview.planFee.discountAmount > 0 && ( ... )}` y antes del `<p>` de "Próximo pago", insertar:

```tsx
                        {preview.planFee.offerMonthsCovered > 0 && preview.planFee.offerUnitPrice != null && (
                          <p className="text-xs text-sky-700">
                            {formatMonths(preview.planFee.offerMonthsCovered)} a precio de oferta (
                            {currency.format(preview.planFee.offerUnitPrice)})
                            {preview.planFee.monthsCovered > preview.planFee.offerMonthsCovered &&
                              ` + ${formatMonths(preview.planFee.monthsCovered - preview.planFee.offerMonthsCovered)} a ${currency.format(preview.planFee.monthlyFee)}`}
                          </p>
                        )}
```

- [ ] **Step 2: Recibo**

En `payment-receipt-dialog.tsx`, cambiar el import:

```tsx
import { formatCalendarDate, formatMonths, paymentMethodLabel } from "@/lib/payment"
```

y reemplazar el `<li>` de cada línea por:

```tsx
                <li key={index} className="flex justify-between gap-3 px-3 py-2">
                  <div>
                    <span>{item.description}</span>
                    {item.offerMonthsCovered != null && item.offerMonthsCovered > 0 && item.offerUnitPrice != null && (
                      <p className="text-xs text-muted-foreground">
                        {formatMonths(item.offerMonthsCovered)} a precio de oferta ({currency.format(item.offerUnitPrice)})
                        {item.fullPriceMonthsCovered != null &&
                          item.fullPriceMonthsCovered > 0 &&
                          item.fullPriceUnitPrice != null &&
                          ` + ${formatMonths(item.fullPriceMonthsCovered)} a ${currency.format(item.fullPriceUnitPrice)}`}
                      </p>
                    )}
                  </div>
                  <span className="font-medium">{currency.format(item.amount)}</span>
                </li>
```

- [ ] **Step 3: Verificar tipos y lint**

Run: `npx tsc --noEmit 2>&1 | tail -20 && npx eslint components/payments`
Expected: sin errores nuevos.

- [ ] **Step 4: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add components/payments
git commit -q -m "$(cat <<'EOF'
Mostrar el desglose de oferta al registrar pagos y en el recibo

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 11: Verificación final

**Files:** ninguno (solo comandos). Usar la skill `superpowers:verification-before-completion` antes de declarar el trabajo terminado.

- [ ] **Step 1: Backend completo**

Run (desde `ZynstormECFPlatform`): `dotnet build ZynstormECFPlatform.Web.Api && dotnet test ZynstormECFPlatform.Tests`
Expected: compila y toda la suite pasa. Si fallan pruebas ajenas a Billing, comprobar con `git stash` si ya fallaban antes (no atribuirlas a este cambio sin verificar).

- [ ] **Step 2: Frontend**

Run (desde `ZynstormECFPlatform-FrontEnd`): `npx tsc --noEmit && npx eslint app/clientes app/configuraciones/planes components/payments lib/offer.ts`
Expected: sin errores nuevos respecto a la línea base de la Task 7 Step 1.

- [ ] **Step 3: Revisar el estado de ambos repos**

Run: `git -C /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform status --short && git -C /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd status --short`
Expected: ambos limpios, en `Staging`.

- [ ] **Step 4: Prueba manual (requiere que el usuario aplique la migración en su BD de desarrollo)**

Pedirle al usuario aprobación para `dotnet ef database update --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api` y arrancar API y frontend. Luego comprobar:
1. En `/configuraciones/planes`: crear un plan de RD$ 2,500 con oferta RD$ 1,500 × 3 meses. Con precio de oferta 2,500 debe rechazarlo ("El precio de oferta debe ser menor que la mensualidad.").
2. En `/clientes`: crear un cliente con ese plan. El diálogo muestra "Al guardar, la oferta empieza hoy…". Al guardar, la fila muestra "Oferta: faltan 92 días" (o los que correspondan) y, al reabrirlo, el panel muestra inicio, fin, tiempo restante y los dos precios.
3. Registrar un pago de 1 mes: previsualización y total = RD$ 1,500. Registrar uno de 6 meses: RD$ 12,000 y el recibo muestra "3 meses a precio de oferta (RD$ 1,500) + 3 meses a RD$ 2,500".
4. Marcar "Ofertas por vencer": un cliente recién creado no aparece; si se edita `OfferStartDate` en BD a hace 70 días, sí aparece con "faltan ~22 días" en ámbar.
5. Cambiar el cliente a otro plan sin oferta: desaparece el badge; volver a asignar el plan con oferta reinicia la fecha a hoy.

---

## Notas para quien ejecute

- **Consumo de planes por comprobantes.** `BuildUsageDtosAsync` queda sin cambios por decisión del spec: ese reporte calcula la mensualidad con el snapshot `ClientMonthlyUsage.MonthlyFee` (precio real). La mensualidad de un cliente en oferta aparecerá a precio real en `/consumo` para planes por comprobantes, aunque el pago se cobre a precio de oferta. Los planes de renta sí usan el ciclo con oferta (Task 6, Step 5). Si el usuario quiere que `/consumo` también respete la oferta, es un cambio aparte.
- **Migración.** Solo se genera, no se aplica. Staging se despliega con Dokploy; confirmar con el usuario cómo se aplican allí las migraciones antes de desplegar.
- **Clientes existentes.** Ninguno tiene `OfferStartDate`, así que no hay oferta activa para ellos hasta que se les (re)asigne un plan con oferta.
