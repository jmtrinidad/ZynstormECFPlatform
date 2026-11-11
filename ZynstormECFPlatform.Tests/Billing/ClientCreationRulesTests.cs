using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientCreationRulesTests
{
    [Fact]
    public void CreatorWithoutAccount_KeepsTheRequestedPlan() =>
        Assert.Equal(5, ClientCreationRules.ResolvePlanId(5, ownedAccountId: null));

    [Fact]
    public void CreatorWithoutAccount_AndNoPlan_StaysWithoutPlan() =>
        Assert.Null(ClientCreationRules.ResolvePlanId(null, ownedAccountId: null));

    [Fact]
    public void CreatorWithAccount_DropsTheRequestedPlan_BecauseTheClientBillsThroughTheAccount() =>
        Assert.Null(ClientCreationRules.ResolvePlanId(5, ownedAccountId: 3));
}
