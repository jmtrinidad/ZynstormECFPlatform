using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class BillingScopeTests
{
    [Fact]
    public void IndividuallyBilled_IncludesAClientWithoutAccount()
    {
        var matches = BillingScope.IndividuallyBilled.Compile();

        Assert.True(matches(new Client { PlanId = 5 }));
    }

    [Fact]
    public void IndividuallyBilled_ExcludesAClientThatBelongsToAnAccount_EvenIfItKeepsItsOwnPlan()
    {
        var matches = BillingScope.IndividuallyBilled.Compile();

        Assert.False(matches(new Client { PlanId = 5, BillingAccountId = 3 }));
    }
}
