using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientUsageServiceTests
{
    private readonly Mock<IClientService> _clients = new();
    private readonly Mock<IPlanService> _plans = new();
    private readonly Mock<IBillingAccountService> _accounts = new();
    private readonly Mock<IClientMonthlyUsageService> _usages = new();

    private string? _executedSql;
    private object? _executedParameters;

    public ClientUsageServiceTests()
    {
        SetupExecute(rowsAffected: 1);
    }

    private ClientUsageService CreateService() => new(
        _clients.Object, _plans.Object, _accounts.Object, _usages.Object,
        NullLogger<ClientUsageService>.Instance);

    private void SetupExecute(int rowsAffected) =>
        _usages
            .Setup(u => u.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback<string, object, CancellationToken>((sql, parameters, _) =>
            {
                _executedSql = sql;
                _executedParameters = parameters;
            })
            .ReturnsAsync(rowsAffected);

    private void GivenClient(int? planId, int? billingAccountId) =>
        _clients
            .Setup(c => c.GetNoTrackingByAsync(It.IsAny<Expression<Func<Client, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Client { ClientId = 7, PlanId = planId, BillingAccountId = billingAccountId });

    private void GivenPlan(Plan? plan) =>
        _plans
            .Setup(p => p.GetNoTrackingByAsync(It.IsAny<Expression<Func<Plan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

    private void GivenAccount(BillingAccount? account) =>
        _accounts
            .Setup(a => a.GetNoTrackingByAsync(It.IsAny<Expression<Func<BillingAccount, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

    private static Plan DocumentsPlan(int planId = 5, string name = "Pool 1000", decimal fee = 3000m, int limit = 1000) => new()
    {
        PlanId = planId,
        Name = name,
        MonthlyFee = fee,
        MonthlyDocumentLimit = limit,
        PlanTypeId = (int)PlanTypeEnum.Documents,
        StatusId = (int)StatusEnum.Active
    };

    private static BillingAccount Account(int planId = 5) => new()
    {
        BillingAccountId = 3,
        PlanId = planId,
        StatusId = (int)StatusEnum.Active
    };

    private T Parameter<T>(string name) =>
        (T)_executedParameters!.GetType().GetProperty(name)!.GetValue(_executedParameters)!;

    [Fact]
    public async Task ClientWithoutAccount_UsesTheClientSqlAndTheClientPlan()
    {
        GivenClient(planId: 9, billingAccountId: null);
        GivenPlan(DocumentsPlan(planId: 9, name: "Plan suelto", fee: 500m, limit: 100));

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.True(counted);
        Assert.Equal(ClientUsageSql.RegisterAccepted, _executedSql);
        Assert.Equal(9, Parameter<int>("PlanId"));
        Assert.Equal("Plan suelto", Parameter<string>("PlanName"));
        _accounts.Verify(a => a.GetNoTrackingByAsync(It.IsAny<Expression<Func<BillingAccount, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClientWithAccount_UsesTheAccountSqlAndTheAccountPlan_EvenIfTheClientHasItsOwnPlan()
    {
        GivenClient(planId: 99, billingAccountId: 3);
        GivenAccount(Account(planId: 5));
        GivenPlan(DocumentsPlan(planId: 5, name: "Pool 1000", fee: 3000m, limit: 1000));

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.True(counted);
        Assert.Equal(ClientUsageSql.RegisterAcceptedInAccount, _executedSql);
        Assert.Equal(3, Parameter<int>("BillingAccountId"));
        Assert.Equal(5, Parameter<int>("PlanId"));
        Assert.Equal("Pool 1000", Parameter<string>("PlanName"));
        Assert.Equal(3000m, Parameter<decimal>("MonthlyFee"));
        Assert.Equal(1000, Parameter<int>("MonthlyDocumentLimit"));
        Assert.Equal(1, Parameter<int>("EcfDocumentId"));
    }

    [Fact]
    public async Task ClientWithAccount_AccountMissingOrInactive_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(null);
        GivenPlan(DocumentsPlan());

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_AccountPlanMissingOrInactive_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        GivenPlan(null);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_RentPlan_DoesNotAccrueUsage()
    {
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        var rent = DocumentsPlan();
        rent.PlanTypeId = (int)PlanTypeEnum.Rent;
        GivenPlan(rent);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }

    [Fact]
    public async Task ClientWithAccount_DocumentAlreadyCounted_ReturnsFalse()
    {
        SetupExecute(rowsAffected: 0);
        GivenClient(planId: null, billingAccountId: 3);
        GivenAccount(Account());
        GivenPlan(DocumentsPlan());

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
    }

    [Fact]
    public async Task ClientWithoutPlanNorAccount_DoesNotCount()
    {
        GivenClient(planId: null, billingAccountId: null);

        var counted = await CreateService().RegisterAcceptedAsync(ecfDocumentId: 1, clientId: 7);

        Assert.False(counted);
        Assert.Null(_executedSql);
    }
}
