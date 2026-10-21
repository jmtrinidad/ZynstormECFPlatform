# Precio especial por cliente Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el rol SA pueda fijar a un cliente un precio mensual propio que reemplace el del plan (y su oferta) en todos los cobros y pantallas.

**Architecture:** `Client.CustomMonthlyFee` (nullable). `PaymentCalculator.GetMonthlyFee(Client)` devuelve el precio vigente y `CalculateCycle` lo usa descartando la oferta cuando hay precio propio. `ClientController` calcula `EffectiveMonthlyFee`/`HasCustomPrice` junto con los campos de oferta, restringe la edición a SA con una función pura y hace que `/consumo` use el precio propio. El frontend agrega el campo (solo SA) y muestra el precio vigente con una etiqueta.

**Tech Stack:** .NET 10 / EF Core 10 (Npgsql) / AutoMapper / xUnit; Next.js + TypeScript + shadcn/ui.

**Spec:** `Docs/superpowers/specs/2026-10-21-precio-especial-cliente-design.md` (commit `ab94263`). Construye sobre `2026-10-21-ofertas-en-planes.md`, ya implementado en la rama `Staging`.

## Global Constraints

- Backend en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform`, frontend en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`. Son dos repos git distintos; ambos están en la rama `Staging`.
- Antes de cada commit confirmar la rama **en el mismo comando** (`git branch --show-current`, debe imprimir `Staging`). No hacer `git push`.
- Cada mensaje de commit termina con estas dos líneas exactas:
  `Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>`
  `Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW`
- El precio propio reemplaza el precio del plan **y** su oferta. Se acepta `0` o más (`0` es un precio válido); no se compara con el precio del plan.
- Cambiar el plan del cliente **no** modifica el precio propio. Solo se quita a mano.
- Alcance: solo la mensualidad o renta. Límite de comprobantes y tramos de excedente siguen siendo los del plan.
- Solo el rol SA fija o quita el precio propio. Un usuario que no es SA, al guardar, conserva el valor actual (en crear queda null).
- `OfferStartDate` no se toca. Con precio propio el cliente se muestra sin oferta (`OfferStatus = 0`).
- `/consumo` usa el precio propio cuando exista; la copia `ClientMonthlyUsage.MonthlyFee` sigue guardando el precio del plan.
- El descuento por pago adelantado (`PrepaymentDiscountPercent`) se aplica sobre el bruto calculado con el precio propio.
- Mensajes de usuario en español. Fechas calendario en hora RD: `DateTimeExtensions.DrNow.Date` (backend) y `todayDrYmd()` (frontend).
- No ejecutar `dotnet ef database update` sin que el usuario lo apruebe.
- Comandos backend, desde `ZynstormECFPlatform`: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`. Comandos frontend, desde `ZynstormECFPlatform-FrontEnd`: `npx tsc --noEmit` (no hay runner de pruebas). La línea base de `tsc` tiene un error previo y ajeno en `app/configuraciones/cuenta/page.tsx(182,46)`; `tsc` modifica `tsconfig.tsbuildinfo`: restaurarlo con `git checkout -- tsconfig.tsbuildinfo` antes de commitear. La línea base de `eslint` en `app/clientes/page.tsx` y `components/payments` son 2 advertencias cada uno, previas.

---

## File Structure

**Backend (`ZynstormECFPlatform`)**
- Modify `ZynstormECFPlatform.Core/Entities/Client.cs` — `CustomMonthlyFee`.
- Create `ZynstormECFPlatform.Data/Migrations/<ts>_AddClientCustomMonthlyFee.cs` (+ Designer, + snapshot) — lo genera `dotnet ef`.
- Create `ZynstormECFPlatform.Services/Billing/ClientPricing.cs` — regla de permiso (función pura).
- Modify `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs` — `GetMonthlyFee` y `CalculateCycle`.
- Modify `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs` — usa el precio vigente.
- Modify `ZynstormECFPlatform.Dtos/ClientDtos.cs`.
- Modify `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`.
- Create `ZynstormECFPlatform.Tests/Billing/CustomPriceTests.cs`.

**Frontend (`ZynstormECFPlatform-FrontEnd`)**
- Modify `types/client.type.ts`.
- Modify `app/clientes/page.tsx` — campo SA, estimado, etiqueta en la lista y panel de oferta.

---

### Task 1: Modelo de datos y migración

**Files:**
- Modify: `ZynstormECFPlatform.Core/Entities/Client.cs`
- Create: `ZynstormECFPlatform.Data/Migrations/<timestamp>_AddClientCustomMonthlyFee.cs` (generado)

**Interfaces:**
- Produces: `Client.CustomMonthlyFee (decimal?)`, nullable, sin default. `decimal?` toma `numeric(18,2)` por la convención de `ConfigureConventions`; no hace falta tocar `StorageContext`.

- [ ] **Step 1: Agregar la propiedad**

En `Client.cs`, después de `OfferStartDate`:

```csharp

    /// <summary>Precio mensual pactado con este cliente. Reemplaza el precio del plan y su oferta. Null = usa el precio del plan.</summary>
    public decimal? CustomMonthlyFee { get; set; }
```

- [ ] **Step 2: Compilar**

Run (desde `ZynstormECFPlatform`): `dotnet build ZynstormECFPlatform.Data`
Expected: `0 Error(s)`.

- [ ] **Step 3: Generar la migración**

Run: `dotnet ef migrations add AddClientCustomMonthlyFee --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`
Expected: `Done.` y archivos nuevos `*_AddClientCustomMonthlyFee.cs` y `*.Designer.cs`; `StorageContextModelSnapshot.cs` modificado.

- [ ] **Step 4: Revisar la migración**

Abrir `ZynstormECFPlatform.Data/Migrations/*_AddClientCustomMonthlyFee.cs`. `Up` debe tener exactamente un `AddColumn<decimal>` de `CustomMonthlyFee` en la tabla `Client`, tipo `numeric(18,2)`, `nullable: true`; `Down`, un `DropColumn` de la misma columna. Si aparece cualquier otra operación: `dotnet ef migrations remove --project ZynstormECFPlatform.Data --startup-project ZynstormECFPlatform.Web.Api`, corregir y repetir el Step 3.

- [ ] **Step 5: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Core ZynstormECFPlatform.Data
git commit -q -m "$(cat <<'EOF'
Agregar precio mensual propio al cliente

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```
Expected: `Rama: Staging` y el commit nuevo.

---

### Task 2: Precio vigente, ciclo con precio propio y regla de permiso (con pruebas)

**Files:**
- Create: `ZynstormECFPlatform.Services/Billing/ClientPricing.cs`
- Modify: `ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs`
- Test: `ZynstormECFPlatform.Tests/Billing/CustomPriceTests.cs`

**Interfaces:**
- Consumes: `Client.CustomMonthlyFee` (Task 1), `Client.OfferStartDate`, `OfferPricing.TermsFor`, `PaymentCalculator.Calculate(…, OfferTerms?, DateTime?)` y `CalculateCycle` existentes.
- Produces:
  - `PaymentCalculator.GetMonthlyFee(Client client) : decimal` — `client.CustomMonthlyFee ?? client.Plan.MonthlyFee`; lanza `ArgumentException` si `Plan` es null.
  - `PaymentCalculator.CalculateCycle(Client, DateTime)` — misma firma, pero con precio propio usa ese precio y no aplica la oferta.
  - `ClientPricing.ResolveCustomFee(bool isSA, decimal? requested, decimal? current) : decimal?` — `requested` si es SA, `current` si no.

- [ ] **Step 1: Escribir las pruebas que fallan**

Crear `ZynstormECFPlatform.Tests/Billing/CustomPriceTests.cs`:

```csharp
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class CustomPriceTests
{
    private static readonly DateTime Start = new(2026, 10, 21);

    private static Client ClientWith(decimal? customFee, int paidMonths = 1, decimal discountPercent = 0m) => new()
    {
        Plan = new Plan { MonthlyFee = 2500m, OfferPrice = 1500m, OfferMonths = 3 },
        CustomMonthlyFee = customFee,
        PaidMonths = paidMonths,
        PrepaymentDiscountPercent = discountPercent,
        OfferStartDate = Start,
        NextPaymentDate = Start
    };

    [Fact]
    public void GetMonthlyFee_WithoutCustomPrice_IsThePlanFee()
    {
        Assert.Equal(2500m, PaymentCalculator.GetMonthlyFee(ClientWith(null)));
    }

    [Fact]
    public void GetMonthlyFee_WithCustomPrice_IsTheCustomPrice()
    {
        Assert.Equal(1800m, PaymentCalculator.GetMonthlyFee(ClientWith(1800m)));
    }

    [Fact]
    public void GetMonthlyFee_CustomPriceAboveThePlan_IsAccepted()
    {
        Assert.Equal(3200m, PaymentCalculator.GetMonthlyFee(ClientWith(3200m)));
    }

    [Fact]
    public void GetMonthlyFee_ZeroIsAValidCustomPrice()
    {
        Assert.Equal(0m, PaymentCalculator.GetMonthlyFee(ClientWith(0m)));
    }

    [Fact]
    public void GetMonthlyFee_WithoutAPlan_Throws()
    {
        Assert.Throws<ArgumentException>(() => PaymentCalculator.GetMonthlyFee(new Client { CustomMonthlyFee = 1800m }));
    }

    [Fact]
    public void CalculateCycle_CustomPriceReplacesThePlanFee()
    {
        var result = PaymentCalculator.CalculateCycle(ClientWith(1800m), Start);

        Assert.Equal(1800m, result.MonthlyFee);
        Assert.Equal(1800m, result.GrossAmount);
        Assert.Equal(1800m, result.Total);
    }

    [Fact]
    public void CalculateCycle_CustomPriceIgnoresThePlanOffer()
    {
        var result = PaymentCalculator.CalculateCycle(ClientWith(1800m, paidMonths: 6), Start);

        Assert.Equal(10800m, result.GrossAmount);
        Assert.Equal(0, result.OfferMonthsCovered);
        Assert.Null(result.OfferUnitPrice);
    }

    [Fact]
    public void CalculateCycle_CustomPriceKeepsThePrepaymentDiscount()
    {
        var result = PaymentCalculator.CalculateCycle(ClientWith(1800m, paidMonths: 6, discountPercent: 10m), Start);

        Assert.Equal(10800m, result.GrossAmount);
        Assert.Equal(1080m, result.DiscountAmount);
        Assert.Equal(9720m, result.Total);
    }

    [Fact]
    public void CalculateCycle_CustomPriceAboveThePlan_ChargesTheCustomPrice()
    {
        var result = PaymentCalculator.CalculateCycle(ClientWith(3200m, paidMonths: 2), Start);

        Assert.Equal(6400m, result.Total);
    }

    [Fact]
    public void CalculateCycle_WithoutCustomPrice_StillAppliesThePlanOffer()
    {
        var result = PaymentCalculator.CalculateCycle(ClientWith(null, paidMonths: 6), Start);

        Assert.Equal(12000m, result.GrossAmount);
        Assert.Equal(3, result.OfferMonthsCovered);
    }

    [Fact]
    public void ResolveCustomFee_SuperAdminUsesTheRequestedValue()
    {
        Assert.Equal(1800m, ClientPricing.ResolveCustomFee(true, 1800m, 2000m));
        Assert.Null(ClientPricing.ResolveCustomFee(true, null, 2000m));
    }

    [Fact]
    public void ResolveCustomFee_OtherUsersKeepTheCurrentValue()
    {
        Assert.Equal(2000m, ClientPricing.ResolveCustomFee(false, 1800m, 2000m));
        Assert.Null(ClientPricing.ResolveCustomFee(false, 1800m, null));
    }
}
```

- [ ] **Step 2: Ejecutar para ver que falla**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~CustomPriceTests"`
Expected: error de compilación (`GetMonthlyFee` y `ClientPricing` no existen).

- [ ] **Step 3: Crear `ClientPricing`**

Crear `ZynstormECFPlatform.Services/Billing/ClientPricing.cs`:

```csharp
namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas del precio especial por cliente. Funciones puras.</summary>
public static class ClientPricing
{
    /// <summary>Solo el rol SA fija o quita el precio propio; el resto conserva el valor actual.</summary>
    public static decimal? ResolveCustomFee(bool isSA, decimal? requested, decimal? current) =>
        isSA ? requested : current;
}
```

- [ ] **Step 4: `GetMonthlyFee` y `CalculateCycle` en `PaymentCalculator`**

Reemplazar el método `CalculateCycle` completo (incluido su comentario `///`) por:

```csharp
    /// <summary>
    /// Precio mensual vigente del cliente: el propio si tiene uno y, si no, el de su plan.
    /// </summary>
    public static decimal GetMonthlyFee(Client client)
    {
        var plan = client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client));
        return client.CustomMonthlyFee ?? plan.MonthlyFee;
    }

    /// <summary>
    /// Ciclo de pago de un cliente con su precio vigente y su oferta. El primer mes cubierto empieza en el
    /// próximo pago, o en <paramref name="fallbackPeriodStart"/> si el cliente no tiene fecha. El precio
    /// propio reemplaza al del plan y también a su oferta.
    /// </summary>
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

- [ ] **Step 5: Ejecutar las pruebas de Billing**

Run: `dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: `Passed!` con 0 fallidas (las nuevas, `PaymentCalculatorOfferTests` y el resto).

- [ ] **Step 6: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Services/Billing/ClientPricing.cs ZynstormECFPlatform.Services/Billing/PaymentCalculator.cs ZynstormECFPlatform.Tests/Billing/CustomPriceTests.cs
git commit -q -m "$(cat <<'EOF'
Calcular el ciclo de pago con el precio propio del cliente

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 3: Registro y previsualización de pagos con el precio vigente

**Files:**
- Modify: `ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs`

**Interfaces:**
- Consumes: `PaymentCalculator.GetMonthlyFee(Client)` (Task 2). `BuildPlanFeePreview`, `RegisterAsync` y `PaymentReminderService` ya usan `CalculateCycle`, así que cobran el precio vigente sin más cambios.

- [ ] **Step 1: Previsualización**

En `GetPreviewAsync`, reemplazar

```csharp
            PlanFee = client.Plan.MonthlyFee > 0 ? BuildPlanFeePreview(client, today) : null,
```

por

```csharp
            PlanFee = PaymentCalculator.GetMonthlyFee(client) > 0 ? BuildPlanFeePreview(client, today) : null,
```

- [ ] **Step 2: Validación al registrar**

En `RegisterAsync`, reemplazar

```csharp
        if (request.IncludePlanFee && client.Plan.MonthlyFee <= 0)
```

por

```csharp
        if (request.IncludePlanFee && PaymentCalculator.GetMonthlyFee(client) <= 0)
```

- [ ] **Step 3: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.Services && dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: `0 Error(s)` y pruebas en verde.

- [ ] **Step 4: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Services/Billing/ClientPaymentRegistrationService.cs
git commit -q -m "$(cat <<'EOF'
Usar el precio vigente del cliente al registrar y previsualizar pagos

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 4: API de clientes (DTOs, permiso, precio vigente, ciclos y consumo)

**Files:**
- Modify: `ZynstormECFPlatform.Dtos/ClientDtos.cs`
- Modify: `ZynstormECFPlatform.Web.Api/Controllers/ClientController.cs`

**Interfaces:**
- Consumes: `ClientPricing.ResolveCustomFee`, `PaymentCalculator.GetMonthlyFee` (Task 2); `Client.CustomMonthlyFee` (Task 1).
- Produces (JSON): `customMonthlyFee` (entrada y salida), `effectiveMonthlyFee` (decimal?, null sin plan), `hasCustomPrice` (bool). `ClientViewDto.OfferStatus` es 0 mientras haya precio propio. `PaymentCycleAmount`, `MonthlyFee` de renta y de `payments` y los montos de `/consumo` usan el precio vigente.

- [ ] **Step 1: DTOs**

En `ClientDtos.cs`, en `ClientCreateDto`, después de `public int? PlanId { get; set; }`:

```csharp

    /// <summary>Precio mensual pactado con el cliente; reemplaza el del plan y su oferta. Solo lo fija el rol SA. Null = usa el del plan.</summary>
    [Range(0, 99999999)]
    public decimal? CustomMonthlyFee { get; set; }
```

En `ClientViewDto`, antes del comentario `/// <summary>Precio mensual de oferta del plan. Null si el plan no tiene oferta.</summary>`:

```csharp
    /// <summary>Precio mensual que se cobra: el propio del cliente o, si no tiene, el de su plan. Null si no tiene plan.</summary>
    public decimal? EffectiveMonthlyFee { get; set; }

    public bool HasCustomPrice { get; set; }

```

El mapeo `Client → ClientViewDto` y `ClientUpdateDto → Client` copia `CustomMonthlyFee` por nombre; no hay que tocar `MappingProfiles`.

- [ ] **Step 2: Precio vigente y oferta oculta en `FillOffer`**

En `ClientController.cs`, reemplazar el método `FillOffer` completo por:

```csharp
        /// <summary>
        /// Completa el precio vigente y los datos de la oferta del cliente; devuelve las condiciones de oferta vigentes.
        /// El precio propio reemplaza la oferta del plan: con él el cliente se muestra sin oferta.
        /// </summary>
        private static OfferTerms? FillOffer(ClientViewDto dto)
        {
            dto.HasCustomPrice = dto.CustomMonthlyFee.HasValue;
            dto.EffectiveMonthlyFee = dto.PlanMonthlyFee is null ? null : dto.CustomMonthlyFee ?? dto.PlanMonthlyFee;

            var terms = dto.HasCustomPrice
                ? null
                : OfferPricing.TermsFor(dto.PlanOfferPrice, dto.PlanOfferMonths, dto.OfferStartDate);
            var state = OfferPricing.Evaluate(terms, DateTimeExtensions.DrNow.Date);

            dto.OfferStatus = (int)state.Status;
            dto.OfferEndDate = state.EndDate;
            dto.OfferDaysRemaining = state.DaysRemaining;
            dto.OfferMonthsRemaining = state.MonthsRemaining;
            return terms;
        }
```

- [ ] **Step 3: El filtro de ofertas por vencer excluye a los clientes con precio propio**

En `OfferExpiringClientIdsAsync`, reemplazar

```csharp
                .Where(c => c.OfferStartDate != null && c.Plan != null && c.Plan.OfferPrice != null && c.Plan.OfferMonths != null)
```

por

```csharp
                .Where(c => c.CustomMonthlyFee == null && c.OfferStartDate != null && c.Plan != null && c.Plan.OfferPrice != null && c.Plan.OfferMonths != null)
```

- [ ] **Step 4: El monto del ciclo en el listado usa el precio vigente**

En `FillPaymentStatusAsync`, dentro de `PaymentCalculator.Calculate(`, reemplazar la línea

```csharp
                        dto.PlanMonthlyFee ?? 0m,
```

por

```csharp
                        dto.EffectiveMonthlyFee ?? 0m,
```

(`FillOffer` ya corrió al inicio de la iteración, así que `EffectiveMonthlyFee` está llenado.)

- [ ] **Step 5: `MonthlyFee` de las filas de renta y de `payments`**

En `BuildRentUsageRowsAsync`, reemplazar

```csharp
                    PlanTypeId = (int)PlanTypeEnum.Rent,
                    MonthlyFee = c.Plan.MonthlyFee,
```

por

```csharp
                    PlanTypeId = (int)PlanTypeEnum.Rent,
                    MonthlyFee = PaymentCalculator.GetMonthlyFee(c),
```

En `GetPaymentClients`, reemplazar

```csharp
                        PlanTypeId = c.Plan.PlanTypeId,
                        MonthlyFee = c.Plan.MonthlyFee,
```

por

```csharp
                        PlanTypeId = c.Plan.PlanTypeId,
                        MonthlyFee = PaymentCalculator.GetMonthlyFee(c),
```

- [ ] **Step 6: Consumo con el precio propio**

En `BuildUsageDtosAsync`, reemplazar

```csharp
                    var calculation = BillingCalculator.Calculate(
                        u.MonthlyFee,
                        u.MonthlyDocumentLimit,
```

por

```csharp
                    // El precio propio del cliente reemplaza la copia del precio del plan guardada al crear el mes.
                    var fee = u.Client.CustomMonthlyFee ?? u.MonthlyFee;

                    var calculation = BillingCalculator.Calculate(
                        fee,
                        u.MonthlyDocumentLimit,
```

y, unas líneas más abajo, reemplazar

```csharp
                    var payment = PaymentCalculator.Calculate(u.MonthlyFee, u.Client.PaidMonths, u.Client.PrepaymentDiscountPercent);
```

por

```csharp
                    var payment = PaymentCalculator.Calculate(fee, u.Client.PaidMonths, u.Client.PrepaymentDiscountPercent);
```

- [ ] **Step 7: Permiso al crear**

En `Post`, dentro de la transacción, justo después del bloque

```csharp
                    model.OfferStartDate = OfferPricing.ResolveStartDate(
                        null, dto.PlanId, OfferPricing.PlanHasOffer(plan?.OfferPrice, plan?.OfferMonths), null, DateTimeExtensions.DrNow.Date);
```

agregar:

```csharp
                    model.CustomMonthlyFee = ClientPricing.ResolveCustomFee(IsSA, dto.CustomMonthlyFee, null);
```

- [ ] **Step 8: Permiso al actualizar**

En `Put`, después de `var previousPlanId = model.PlanId;` agregar:

```csharp
                var previousCustomFee = model.CustomMonthlyFee;
```

y, después de la sentencia `model.OfferStartDate = OfferPricing.ResolveStartDate(` (la que termina en `model.OfferStartDate, DateTimeExtensions.DrNow.Date);`), agregar:

```csharp
                model.CustomMonthlyFee = ClientPricing.ResolveCustomFee(IsSA, dto.CustomMonthlyFee, previousCustomFee);
```

(`Mapper.Map(dto, model)` ya copió `dto.CustomMonthlyFee` al modelo; esta línea lo corrige para quien no es SA y es lo que permite quitar el precio enviando null siendo SA.)

- [ ] **Step 9: Compilar y probar**

Run: `dotnet build ZynstormECFPlatform.Web.Api && dotnet test ZynstormECFPlatform.Tests --filter "FullyQualifiedName~Billing"`
Expected: `0 Error(s)` y pruebas en verde.

- [ ] **Step 10: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform
echo "Rama: $(git branch --show-current)"
git add ZynstormECFPlatform.Dtos ZynstormECFPlatform.Web.Api
git commit -q -m "$(cat <<'EOF'
Exponer el precio especial del cliente en la API y limitarlo a SA

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 5: Frontend — tipos, campo del precio especial (solo SA) y estimado

**Files:**
- Modify: `types/client.type.ts`
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: JSON de la Task 4.
- Produces: `Client.customMonthlyFee/effectiveMonthlyFee/hasCustomPrice`, `ClientCreate.customMonthlyFee`; constantes `isSA`, `hasCustomPrice`, `effectiveMonthlyFee` en `ClientesPage` que usa la Task 6.

Trabajar en `C:\Projects\ZynstormECF-WorkSpace\ZynstormECFPlatform-FrontEnd`.

- [ ] **Step 1: Tipos**

En `types/client.type.ts`, en `Client`, después de `planMonthlyFee?: number | null`:

```ts
  customMonthlyFee?: number | null
  effectiveMonthlyFee?: number | null
  hasCustomPrice?: boolean
```

y en `ClientCreate`, después de `planId?: number | null`:

```ts
  customMonthlyFee?: number | null
```

- [ ] **Step 2: Usuario SA**

En `app/clientes/page.tsx`, junto a los demás imports (después de `import { useClients } from "@/hooks/useClients"`):

```tsx
import { useAuthStore } from "@/store/authStore"
```

y dentro de `ClientesPage`, justo después del bloque `} = useClients()`:

```tsx
  const user = useAuthStore((state) => state.user)
  const isSA = user?.userType === 1
```

- [ ] **Step 3: Valor inicial, edición y envío**

En `emptyForm`, después de `planId: null,`:

```tsx
  customMonthlyFee: null,
```

En `openEditDialog`, después de `planId: client.planId ?? null,`:

```tsx
      customMonthlyFee: client.customMonthlyFee ?? null,
```

En `handleSubmit`, en el objeto `payload`, después de `planId: formData.planId ?? null,`:

```tsx
      customMonthlyFee: isSA ? (formData.customMonthlyFee ?? null) : null,
```

(Quien no es SA envía null y el servidor conserva el valor actual.)

- [ ] **Step 4: Precio vigente en el estimado**

Reemplazar el bloque que empieza en `const cycleEstimate = estimateCycleGross({` por:

```tsx
  const hasCustomPrice = formData.customMonthlyFee != null
  const effectiveMonthlyFee = formData.customMonthlyFee ?? selectedPlan?.monthlyFee ?? 0
  // El precio especial reemplaza el del plan y su oferta.
  const cycleEstimate = estimateCycleGross({
    monthlyFee: effectiveMonthlyFee,
    months: paymentMonths,
    offerPrice: hasCustomPrice ? null : selectedPlan?.offerPrice,
    offerMonths: hasCustomPrice ? null : selectedPlan?.offerMonths,
    offerStartDate,
    periodStart: formData.nextPaymentDate ?? todayDrYmd(),
  })
```

y en el renglón del "Total del pago", reemplazar

```tsx
                              : `${formatCurrency(selectedPlan.monthlyFee)} × ${formatMonths(paymentMonths)}`}
```

por

```tsx
                              : `${formatCurrency(effectiveMonthlyFee)} × ${formatMonths(paymentMonths)}`}
```

- [ ] **Step 5: Campo "Precio especial" (solo SA)**

Después del `</div>` que cierra la cuadrícula de Plan / Cliente inactivo (el que sigue a este fragmento único)

```tsx
                          {formData.clientInactive ? "No podrá emitir comprobantes" : "Activo"}
                        </span>
                      </div>
                    </Field>
                  </div>
```

insertar, dentro del mismo contenedor padre:

```tsx

                  {isSA && selectedPlan && (
                    <div className="mt-4">
                      <Field>
                        <FieldLabel htmlFor="customMonthlyFee">Precio especial (RD$)</FieldLabel>
                        <Input
                          id="customMonthlyFee"
                          type="number"
                          min={0}
                          step="0.01"
                          placeholder={`Precio del plan: ${formatCurrency(selectedPlan.monthlyFee)}`}
                          value={formData.customMonthlyFee ?? ""}
                          onChange={(e) =>
                            setFormData((current) => ({
                              ...current,
                              customMonthlyFee: e.target.value === "" ? null : Number(e.target.value),
                            }))
                          }
                        />
                        <p className="text-xs text-muted-foreground">
                          Opcional. Reemplaza el precio del plan y su oferta para este cliente; déjalo vacío para usar el
                          precio del plan.
                        </p>
                      </Field>
                    </div>
                  )}
```

- [ ] **Step 6: Verificar tipos y lint**

Run: `npx tsc --noEmit 2>&1 | grep error; npx eslint app/clientes/page.tsx 2>&1 | tail -3; git checkout -- tsconfig.tsbuildinfo`
Expected: solo el error previo de `cuenta/page.tsx`; eslint con `0 errors, 2 warnings` (las previas).

- [ ] **Step 7: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add types/client.type.ts app/clientes/page.tsx
git commit -q -m "$(cat <<'EOF'
Campo de precio especial por cliente (solo SA) en el formulario de clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 6: Frontend — etiqueta en la lista y aviso en el panel de oferta

**Files:**
- Modify: `app/clientes/page.tsx`

**Interfaces:**
- Consumes: `Client.effectiveMonthlyFee/hasCustomPrice` y las constantes `hasCustomPrice`, `effectiveMonthlyFee` de la Task 5.

- [ ] **Step 1: Precio vigente y etiqueta en la columna Precio**

En la celda de precio, reemplazar

```tsx
                              <span>
                                {formatCurrency(client.planMonthlyFee)}
                                <span className="text-xs font-normal text-muted-foreground">/mes</span>
                              </span>
```

por

```tsx
                              <span>
                                {formatCurrency(client.effectiveMonthlyFee ?? client.planMonthlyFee)}
                                <span className="text-xs font-normal text-muted-foreground">/mes</span>
                              </span>
                              {client.hasCustomPrice && (
                                <Badge
                                  className="gap-1 whitespace-nowrap bg-violet-100 font-normal text-violet-800 hover:bg-violet-100"
                                  title={`Precio del plan: ${formatCurrency(client.planMonthlyFee)}`}
                                >
                                  <Tag className="h-3 w-3" />
                                  Precio especial
                                </Badge>
                              )}
```

(El badge de oferta ya no aparece para estos clientes porque el servidor devuelve `offerStatus = 0`.)

- [ ] **Step 2: Aviso en el panel de oferta del diálogo**

En el panel "Oferta del plan", reemplazar el inicio del condicional

```tsx
                      {keepsCurrentPlan && editingClient?.offerStartDate ? (
```

por

```tsx
                      {hasCustomPrice ? (
                        <p>
                          El precio especial de este cliente ({formatCurrency(effectiveMonthlyFee)}) reemplaza la oferta
                          del plan.
                        </p>
                      ) : keepsCurrentPlan && editingClient?.offerStartDate ? (
```

El resto de la cadena (`) : keepsCurrentPlan ? (` y `) : (`) queda igual.

- [ ] **Step 3: Verificar tipos y lint**

Run: `npx tsc --noEmit 2>&1 | grep error; npx eslint app/clientes/page.tsx 2>&1 | tail -3; git checkout -- tsconfig.tsbuildinfo`
Expected: solo el error previo; eslint `0 errors, 2 warnings`.

- [ ] **Step 4: Commit**

```bash
cd /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd
echo "Rama: $(git branch --show-current)"
git add app/clientes/page.tsx
git commit -q -m "$(cat <<'EOF'
Mostrar el precio especial en la lista y el panel de oferta de clientes

Co-Authored-By: Claude Sonnet 5.5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018M2mGExTABMZ7foHRHhJkW
EOF
)"
git log --oneline -1
```

---

### Task 7: Verificación final

**Files:** ninguno. Usar la skill `superpowers:verification-before-completion` antes de declarar el trabajo terminado.

- [ ] **Step 1: Backend completo**

Run (desde `ZynstormECFPlatform`): `dotnet build ZynstormECFPlatform.Web.Api && dotnet test ZynstormECFPlatform.Tests`
Expected: compila y toda la suite pasa.

- [ ] **Step 2: Frontend**

Run (desde `ZynstormECFPlatform-FrontEnd`): `npx tsc --noEmit 2>&1 | grep error; git checkout -- tsconfig.tsbuildinfo`
Expected: solo el error previo de `cuenta/page.tsx`.

- [ ] **Step 3: Estado de los repos**

Run: `git -C /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform status --short && git -C /c/Projects/ZynstormECF-WorkSpace/ZynstormECFPlatform-FrontEnd status --short`
Expected: ambos limpios, en `Staging`.

- [ ] **Step 4: Prueba manual (requiere que el usuario apruebe y aplique las migraciones `AddPlanOffers` y `AddClientCustomMonthlyFee` en su BD de desarrollo)**

1. Como SA, editar un cliente con plan: aparece "Precio especial (RD$)" con el precio del plan como placeholder. Como usuario que no es SA el campo no aparece.
2. Fijar RD$ 1,800 en un cliente cuyo plan cuesta RD$ 2,500: la lista muestra RD$ 1,800/mes con la etiqueta "Precio especial" y el precio del plan en el tooltip; el "Total del pago" del diálogo usa 1,800.
3. Registrar un pago de 1 mes: la previsualización y el total son RD$ 1,800. En un cliente cuyo plan tiene oferta, el badge de oferta desaparece y el panel dice que el precio especial reemplaza la oferta.
4. Cambiar el cliente a otro plan: el precio especial se conserva.
5. Vaciar el campo y guardar: vuelve el precio del plan (y la oferta, si aún no terminó).
6. Fijar `0`: el cliente queda con mensualidad 0 y registrar el pago de la mensualidad responde que no hay mensualidad a pagar.
7. `/consumo` (plan por comprobantes): la mensualidad del cliente muestra el precio especial; el excedente no cambia.

---

## Notas para quien ejecute

- **Mensajes.** Se dejan sin cambio los textos "El plan del cliente no tiene mensualidad a pagar." (servicio y diálogo de pago); con precio propio igual a 0 siguen siendo comprensibles.
- **Consumo y ofertas.** Esta funcionalidad hace que `/consumo` respete el precio propio, pero `BuildUsageDtosAsync` sigue sin considerar la **oferta** del plan (decisión del spec de ofertas). Un cliente con oferta y sin precio propio aparece a precio real en `/consumo` para planes por comprobantes.
- **Migraciones.** Solo se generan. Hay dos pendientes de aplicar: `AddPlanOffers` y `AddClientCustomMonthlyFee`.
