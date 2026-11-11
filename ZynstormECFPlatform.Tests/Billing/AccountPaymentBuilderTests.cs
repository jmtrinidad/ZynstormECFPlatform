using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class AccountPaymentBuilderTests
{
    private static BillingAccount Account(int paidMonths = 1, decimal discount = 0m) => new()
    {
        BillingAccountId = 3,
        Name = "Mi Cuenta",
        PlanId = 5,
        Plan = new Plan { PlanId = 5, Name = "Pool 1000", MonthlyFee = 3000m, MonthlyDocumentLimit = 1000, PlanTypeId = 1 },
        PaidMonths = paidMonths,
        PrepaymentDiscountPercent = discount,
        NextPaymentDate = new DateTime(2026, 10, 15)
    };

    private static RegisterPaymentRequestDto Request(bool includeFee, string? reference = null) => new()
    {
        PaymentDate = new DateTime(2026, 11, 2),
        PaymentMethod = (int)PaymentMethod.Transfer,
        IncludePlanFee = includeFee,
        Reference = reference
    };

    private static AccountPendingOverageDto Overage(int id, int month, decimal amount) => new()
    {
        BillingAccountMonthlyUsageId = id, Year = 2026, Month = month, PlanId = 5, PlanName = "Pool 1000",
        OverageDocuments = 50, Amount = amount
    };

    [Fact]
    public void Fee_AdvancesTheNextPaymentFromTheDueDate()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [], new DateTime(2026, 11, 2), "user-1");

        Assert.Equal(new DateTime(2026, 11, 15), draft.NewNextPaymentDate);
        var item = Assert.Single(draft.Payment.Items);
        Assert.Equal((int)ClientPaymentItemType.PlanFee, item.ItemType);
        Assert.Equal(3000m, item.Amount);
        Assert.Equal(new DateTime(2026, 10, 15), item.PreviousNextPaymentDate);
        Assert.Equal(3000m, draft.Payment.TotalAmount);
    }

    [Fact]
    public void Payment_BelongsToTheAccountNotToAClient()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [], new DateTime(2026, 11, 2), "user-1");

        Assert.Null(draft.Payment.ClientId);
        Assert.Equal(3, draft.Payment.BillingAccountId);
        Assert.Equal("user-1", draft.Payment.RegisteredByUserId);
    }

    [Fact]
    public void PrepaidMonthsWithDiscount_ChargeTheDiscountedTotal()
    {
        var draft = AccountPaymentBuilder.Build(Account(paidMonths: 3, discount: 10m), Request(true), [], new DateTime(2026, 11, 2), null);

        Assert.Equal(8100m, draft.Payment.TotalAmount);
        Assert.Equal(new DateTime(2027, 1, 15), draft.NewNextPaymentDate);
    }

    [Fact]
    public void OveragesOnly_AddOverageItemsAndKeepTheDueDate()
    {
        var draft = AccountPaymentBuilder.Build(
            Account(), Request(false), [Overage(2, 10, 300m), Overage(1, 9, 200m)], new DateTime(2026, 11, 2), null);

        Assert.Null(draft.NewNextPaymentDate);
        Assert.Equal(500m, draft.Payment.TotalAmount);
        Assert.All(draft.Payment.Items, i => Assert.Equal((int)ClientPaymentItemType.Overage, i.ItemType));
        Assert.Equal([1, 2], draft.Payment.Items.Select(i => i.BillingAccountMonthlyUsageId!.Value));
    }

    [Fact]
    public void FeeAndOverages_SumInTheTotal()
    {
        var draft = AccountPaymentBuilder.Build(Account(), Request(true), [Overage(1, 10, 300m)], new DateTime(2026, 11, 2), null);

        Assert.Equal(2, draft.Payment.Items.Count);
        Assert.Equal(3300m, draft.Payment.TotalAmount);
    }

    [Fact]
    public void ReferenceIsTrimmedAndBlankBecomesNull()
    {
        var trimmed = AccountPaymentBuilder.Build(Account(), Request(true, "  TR-99  "), [], new DateTime(2026, 11, 2), null);
        var blank = AccountPaymentBuilder.Build(Account(), Request(true, "   "), [], new DateTime(2026, 11, 2), null);

        Assert.Equal("TR-99", trimmed.Payment.Reference);
        Assert.Null(blank.Payment.Reference);
    }
}
