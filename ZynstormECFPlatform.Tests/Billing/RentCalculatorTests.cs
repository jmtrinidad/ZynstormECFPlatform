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
