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
