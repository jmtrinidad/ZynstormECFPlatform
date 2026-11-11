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
