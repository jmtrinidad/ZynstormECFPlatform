using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingAccountPolicyTests
{
    private static Plan DocumentsPlan() => new()
    {
        PlanId = 5, Name = "Pool", PlanTypeId = (int)PlanTypeEnum.Documents, StatusId = (int)StatusEnum.Active
    };

    private static List<string> Validate(
        string? name = "Mi Cuenta", Plan? plan = null, decimal? fee = null, int months = 1, decimal discount = 0m, int grace = 3) =>
        BillingAccountPolicy.ValidateAccount(name, plan ?? DocumentsPlan(), fee, months, discount, grace);

    [Fact]
    public void ValidAccount_HasNoErrors() => Assert.Empty(Validate());

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Name_IsRequired(string? name) => Assert.Contains(Validate(name: name), e => e.Contains("nombre"));

    [Fact]
    public void Name_CannotExceed100Characters() =>
        Assert.Contains(Validate(name: new string('a', 101)), e => e.Contains("100"));

    [Fact]
    public void MissingPlan_IsRejected() =>
        Assert.Contains(BillingAccountPolicy.ValidateAccount("Mi Cuenta", null, null, 1, 0m, 3), e => e.Contains("plan"));

    [Fact]
    public void RentPlan_IsRejected()
    {
        var plan = DocumentsPlan();
        plan.PlanTypeId = (int)PlanTypeEnum.Rent;

        Assert.Contains(Validate(plan: plan), e => e.Contains("comprobantes"));
    }

    [Fact]
    public void InactivePlan_IsRejected()
    {
        var plan = DocumentsPlan();
        plan.StatusId = (int)StatusEnum.Inactive;

        Assert.Contains(Validate(plan: plan), e => e.Contains("activo"));
    }

    [Fact]
    public void NegativeCustomFee_IsRejected() => Assert.Contains(Validate(fee: -1m), e => e.Contains("negativo"));

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void PaidMonths_MustBeBetween1And60(int months) => Assert.Contains(Validate(months: months), e => e.Contains("meses"));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Discount_MustBeBetween0And100(decimal discount) => Assert.Contains(Validate(discount: discount), e => e.Contains("descuento"));

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public void GraceDays_MustBeBetween1And30(int grace) => Assert.Contains(Validate(grace: grace), e => e.Contains("gracia"));

    [Fact]
    public void Owner_MustExistBeAdminAndHaveNoAccount()
    {
        Assert.Empty(BillingAccountPolicy.ValidateOwner(true, true, false));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(false, false, false), e => e.Contains("no existe"));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(true, false, false), e => e.Contains("Admin"));
        Assert.Contains(BillingAccountPolicy.ValidateOwner(true, true, true), e => e.Contains("ya tiene"));
    }

    [Fact]
    public void Members_AllRequestedMustExistAndBeFreeOrInTheSameAccount()
    {
        var free = new Client { Name = "Libre" };
        var mine = new Client { Name = "Mío", BillingAccountId = 3 };
        var other = new Client { Name = "Ajeno", BillingAccountId = 9 };

        Assert.Empty(BillingAccountPolicy.ValidateMembers([free, mine], 2, 3));
        Assert.Contains(BillingAccountPolicy.ValidateMembers([free], 2, 3), e => e.Contains("no existen") || e.Contains("no existe"));
        Assert.Contains(BillingAccountPolicy.ValidateMembers([other], 1, 3), e => e.Contains("Ajeno"));
    }

    [Fact]
    public void Quota_CanBeClearedOrZeroOrPositive()
    {
        Assert.Empty(BillingAccountPolicy.ValidateQuota(null));
        Assert.Empty(BillingAccountPolicy.ValidateQuota(0));
        Assert.Empty(BillingAccountPolicy.ValidateQuota(500));
    }

    [Fact]
    public void Quota_CannotBeNegative() =>
        Assert.Contains(BillingAccountPolicy.ValidateQuota(-1), e => e.Contains("negativo"));

    [Theory]
    [InlineData(100, null, false)]
    [InlineData(100, 100, false)]
    [InlineData(101, 100, true)]
    [InlineData(0, 0, false)]
    public void Quota_IsExceededOnlyAboveTheQuota(int accepted, int? quota, bool expected) =>
        Assert.Equal(expected, BillingAccountPolicy.IsQuotaExceeded(accepted, quota));
}
