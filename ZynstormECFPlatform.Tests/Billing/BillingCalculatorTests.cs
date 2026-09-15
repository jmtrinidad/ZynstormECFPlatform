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
