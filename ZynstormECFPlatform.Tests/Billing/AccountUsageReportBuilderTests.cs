using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class AccountUsageReportBuilderTests
{
    private static readonly PlanOverageTier[] Tiers =
    [
        new() { PlanId = 5, FromUnit = 1, ToUnit = 100, UnitPrice = 6.00m },
        new() { PlanId = 5, FromUnit = 101, ToUnit = null, UnitPrice = 9.00m }
    ];

    private static BillingAccount Account(DateTime nextPayment, decimal? customFee = null) => new()
    {
        GuidId = "acct-guid",
        Name = "Mi Cuenta",
        OwnerUser = new User { FirstName = "Ana", LastName = "Pérez" },
        PaidMonths = 1,
        CustomMonthlyFee = customFee,
        NextPaymentDate = nextPayment
    };

    private static BillingAccountMonthlyUsage Usage(int accepted = 650) => new()
    {
        BillingAccountId = 3,
        Year = 2026,
        Month = 11,
        AcceptedDocuments = accepted,
        PlanId = 5,
        PlanName = "Pool 500",
        MonthlyFee = 3000m,
        MonthlyDocumentLimit = 500
    };

    [Fact]
    public void Row_IdentifiesTheAccountAndItsOwner()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(), Tiers);

        Assert.True(row.IsAccount);
        Assert.Equal("acct-guid", row.ClientGuidId);
        Assert.Equal("Mi Cuenta", row.ClientName);
        Assert.Equal("Ana Pérez", row.ClientRnc);
        Assert.Equal("Pool 500", row.PlanName);
        Assert.Equal(1, row.PlanTypeId);
    }

    [Fact]
    public void PoolOverage_IsChargedByTiers()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(650), Tiers);

        Assert.Equal(650, row.AcceptedDocuments);
        Assert.Equal(150, row.OverageDocuments);
        Assert.Equal(1050m, row.OverageAmount);
        Assert.Equal(2, row.Tiers.Count);
    }

    [Fact]
    public void FeeNotYetCovered_IsChargedAndAddedToTheTotal()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15)), Usage(650), Tiers);

        Assert.False(row.MonthlyFeeCovered);
        Assert.Equal(3000m, row.MonthlyFeeCharged);
        Assert.Equal(4050m, row.Total);
    }

    [Fact]
    public void FeeCoveredByAnAdvancePayment_IsNotChargedButOverageStillIs()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 12, 15)), Usage(650), Tiers);

        Assert.True(row.MonthlyFeeCovered);
        Assert.Equal(0m, row.MonthlyFeeCharged);
        Assert.Equal(1050m, row.Total);
    }

    [Fact]
    public void CustomFee_ReplacesTheSnapshotFee()
    {
        var row = AccountUsageReportBuilder.Build(Account(new DateTime(2026, 10, 15), customFee: 2500m), Usage(400), Tiers);

        Assert.Equal(2500m, row.MonthlyFee);
        Assert.Equal(2500m, row.Total);
    }
}
