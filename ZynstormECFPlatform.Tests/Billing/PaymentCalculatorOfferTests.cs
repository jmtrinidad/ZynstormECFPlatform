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
