using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientStatusEvaluatorAccountTests
{
    private static BillingAccount Account(DateTime? suspendedAt = null) => new()
    {
        BillingAccountId = 3,
        Name = "Mi Cuenta",
        PaymentSuspendedAtUtc = suspendedAt,
        NextPaymentDate = new DateTime(2026, 12, 1),
        Plan = new Plan { Name = "Pool 1000" }
    };

    private static ApiKey KeyFor(BillingAccount? account, bool clientInactive = false) => new()
    {
        Apikey = "k",
        StatusId = (int)StatusEnum.Active,
        Client = new Client
        {
            Name = "Cliente",
            Rnc = "131000001",
            GuidId = "client-guid",
            StatusId = (int)StatusEnum.Active,
            ClientInactive = clientInactive,
            BillingAccount = account,
            BillingAccountId = account?.BillingAccountId
        }
    };

    [Fact]
    public void SuspendedAccount_BlocksItsClient()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(suspendedAt: DateTime.UtcNow)));

        Assert.False(result.IsActive);
        Assert.True(result.PaymentSuspended);
        Assert.True(result.ClientInactive);
        Assert.Equal(ClientStatusEvaluator.AccountSuspendedMessage, result.Message);
        Assert.Equal("client-guid", result.ClientGuid);
    }

    [Fact]
    public void SuspendedAccount_ReportsThePlanAndNextPaymentOfTheAccount()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(suspendedAt: DateTime.UtcNow)));

        Assert.Equal("Pool 1000", result.PlanName);
        Assert.Equal(new DateTime(2026, 12, 1), result.NextPaymentDate);
    }

    [Fact]
    public void ActiveAccount_LeavesItsClientActive_AndShowsTheAccountPlan()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account()));

        Assert.True(result.IsActive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientActiveMessage, result.Message);
        Assert.Equal("Pool 1000", result.PlanName);
    }

    [Fact]
    public void ClientWithoutAccount_IsNotAffected()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(account: null));

        Assert.True(result.IsActive);
    }

    [Fact]
    public void ManuallyInactiveClient_InActiveAccount_IsStillInactive()
    {
        var result = ClientStatusEvaluator.Evaluate(KeyFor(Account(), clientInactive: true));

        Assert.False(result.IsActive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientInactiveMessage, result.Message);
    }

    [Fact]
    public void Access_IsSuspended_OnlyWhenTheAccountHasASuspensionDate()
    {
        Assert.False(BillingAccountAccess.IsSuspended(null));
        Assert.False(BillingAccountAccess.IsSuspended(Account()));
        Assert.True(BillingAccountAccess.IsSuspended(Account(DateTime.UtcNow)));
    }
}
