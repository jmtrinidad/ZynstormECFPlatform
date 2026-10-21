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
