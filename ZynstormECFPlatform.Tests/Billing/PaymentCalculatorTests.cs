using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class PaymentCalculatorTests
{
    [Fact]
    public void Calculate_MonthlyWithoutDiscount_ChargesOneMonth()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 1, discountPercent: 0m);

        Assert.Equal(1, result.MonthsCovered);
        Assert.Equal(2000m, result.GrossAmount);
        Assert.Equal(0m, result.DiscountAmount);
        Assert.Equal(2000m, result.Total);
    }

    [Fact]
    public void Calculate_FullYearWithoutDiscount_ChargesTwelveMonths()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 12, discountPercent: 0m);

        Assert.Equal(12, result.MonthsCovered);
        Assert.Equal(24000m, result.GrossAmount);
        Assert.Equal(24000m, result.Total);
    }

    [Fact]
    public void Calculate_FullYearWithTenPercent_Is21600()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 12, discountPercent: 10m);

        Assert.Equal(24000m, result.GrossAmount);
        Assert.Equal(2400m, result.DiscountAmount);
        Assert.Equal(21600m, result.Total);
    }

    [Fact]
    public void Calculate_ArbitraryMonths_MultipliesByMonths()
    {
        var result = PaymentCalculator.Calculate(3000m, paidMonths: 18, discountPercent: 5m);

        Assert.Equal(18, result.MonthsCovered);
        Assert.Equal(54000m, result.GrossAmount);
        Assert.Equal(2700m, result.DiscountAmount);
        Assert.Equal(51300m, result.Total);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Calculate_InvalidMonths_CountsAsOneMonth(int months)
    {
        Assert.Equal(1, PaymentCalculator.Calculate(3000m, months, 0m).MonthsCovered);
    }

    [Fact]
    public void IsMonthCovered_NextPaymentInLaterMonth_IsTrue()
    {
        Assert.True(PaymentCalculator.IsMonthCovered(new DateTime(2026, 10, 1), 2026, 9));
        Assert.False(PaymentCalculator.IsMonthCovered(new DateTime(2026, 9, 30), 2026, 9));
        Assert.False(PaymentCalculator.IsMonthCovered(null, 2026, 9));
    }

    [Fact]
    public void Calculate_MonthlyWithDiscount_AppliesDiscountAnyway()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 1, discountPercent: 10m);

        Assert.Equal(1, result.MonthsCovered);
        Assert.Equal(1800m, result.Total);
    }

    [Fact]
    public void GetAmountForMonth_MonthAlreadyCovered_IsZero()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 12, discountPercent: 0m);

        // Consultamos septiembre 2026 y el próximo pago es junio 2027: el mes está cubierto.
        Assert.Equal(0m, PaymentCalculator.GetAmountForMonth(result, new DateTime(2027, 6, 10), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_RenewalMonth_ChargesFullCycle()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 12, discountPercent: 10m);

        Assert.Equal(21600m, PaymentCalculator.GetAmountForMonth(result, new DateTime(2026, 9, 20), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_OverdueDate_ChargesFullCycle()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 1, discountPercent: 0m);

        Assert.Equal(2000m, PaymentCalculator.GetAmountForMonth(result, new DateTime(2026, 7, 5), 2026, 9));
    }

    [Fact]
    public void GetAmountForMonth_WithoutNextPaymentDate_ChargesFullCycle()
    {
        var result = PaymentCalculator.Calculate(2000m, paidMonths: 1, discountPercent: 0m);

        Assert.Equal(2000m, PaymentCalculator.GetAmountForMonth(result, null, 2026, 9));
    }

    [Fact]
    public void GetPaymentStatus_WithoutDate_IsNoDate()
    {
        var (status, days) = PaymentCalculator.GetPaymentStatus(null, 15);

        Assert.Equal(PaymentStatus.NoDate, status);
        Assert.Null(days);
    }

    [Fact]
    public void GetPaymentStatus_Yesterday_IsOverdue()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = PaymentCalculator.GetPaymentStatus(today.AddDays(-1), 15, today);

        Assert.Equal(PaymentStatus.Overdue, status);
        Assert.Equal(-1, days);
    }

    [Fact]
    public void GetPaymentStatus_Today_IsDueSoon()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = PaymentCalculator.GetPaymentStatus(today, 15, today);

        Assert.Equal(PaymentStatus.DueSoon, status);
        Assert.Equal(0, days);
    }

    [Fact]
    public void GetPaymentStatus_AtWarningBoundary_IsDueSoon()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = PaymentCalculator.GetPaymentStatus(today.AddDays(15), 15, today);

        Assert.Equal(PaymentStatus.DueSoon, status);
        Assert.Equal(15, days);
    }

    [Fact]
    public void GetPaymentStatus_PastWarningBoundary_IsCurrent()
    {
        var today = new DateTime(2026, 9, 15);
        var (status, days) = PaymentCalculator.GetPaymentStatus(today.AddDays(16), 15, today);

        Assert.Equal(PaymentStatus.Current, status);
        Assert.Equal(16, days);
    }

    [Fact]
    public void GetPaymentStatus_WithoutExplicitToday_UsesDominicanDate()
    {
        var (status, days) = PaymentCalculator.GetPaymentStatus(DateTimeExtensions.DrNow.Date.AddDays(30), 15);

        Assert.Equal(PaymentStatus.Current, status);
        Assert.Equal(30, days);
    }

    [Fact]
    public void ValidateRentPlan_ValidPlan_HasNoErrors()
    {
        Assert.Empty(BillingCalculator.ValidateRentPlan(2000m, 5, hasOverageTiers: false));
    }

    [Fact]
    public void ValidateRentPlan_UnlimitedUsers_IsValid()
    {
        Assert.Empty(BillingCalculator.ValidateRentPlan(2000m, BillingCalculator.UnlimitedUsers, hasOverageTiers: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-2)]
    public void ValidateRentPlan_InvalidMaxUsers_ReturnsError(int? maxUsers)
    {
        Assert.Contains(BillingCalculator.ValidateRentPlan(2000m, maxUsers, hasOverageTiers: false),
            e => e.Contains("usuarios", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRentPlan_WithOverageTiers_ReturnsError()
    {
        Assert.Contains(BillingCalculator.ValidateRentPlan(2000m, 5, hasOverageTiers: true),
            e => e.Contains("tramos", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateRentPlan_NegativeFee_ReturnsError()
    {
        Assert.NotEmpty(BillingCalculator.ValidateRentPlan(-1m, 5, hasOverageTiers: false));
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
