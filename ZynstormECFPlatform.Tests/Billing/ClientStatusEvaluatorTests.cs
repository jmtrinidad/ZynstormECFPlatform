using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Tests.Billing;

public class ClientStatusEvaluatorTests
{
    [Fact]
    public void Evaluate_NullApiKey_ReturnsInactiveWithNotFoundMessage()
    {
        var result = ClientStatusEvaluator.Evaluate(null);

        Assert.False(result.IsActive);
        Assert.Equal(ClientStatusEvaluator.KeyNotFoundOrInvalidMessage, result.Message);
        Assert.Null(result.ClientGuid);
    }

    [Fact]
    public void Evaluate_DeletedApiKey_ReturnsInactiveWithNotFoundMessage()
    {
        var apiKey = new ApiKey
        {
            Apikey = "key-deleted",
            IsDeleted = true,
            StatusId = (int)StatusEnum.Active
        };

        var result = ClientStatusEvaluator.Evaluate(apiKey);

        Assert.False(result.IsActive);
        Assert.Equal(ClientStatusEvaluator.KeyNotFoundOrInvalidMessage, result.Message);
    }

    [Fact]
    public void Evaluate_InactiveStatusApiKey_ReturnsInactiveWithInactiveKeyMessage()
    {
        var apiKey = new ApiKey
        {
            Apikey = "key-inactive",
            IsDeleted = false,
            StatusId = (int)StatusEnum.Inactive
        };

        var result = ClientStatusEvaluator.Evaluate(apiKey);

        Assert.False(result.IsActive);
        Assert.Equal(ClientStatusEvaluator.KeyInactiveMessage, result.Message);
    }

    [Fact]
    public void Evaluate_NullOrDeletedOrInactiveClient_ReturnsInactiveWithClientNotFoundMessage()
    {
        // Null Client
        var keyNoClient = new ApiKey
        {
            Apikey = "key-1",
            StatusId = (int)StatusEnum.Active,
            Client = null!
        };
        var res1 = ClientStatusEvaluator.Evaluate(keyNoClient);
        Assert.False(res1.IsActive);
        Assert.Equal(ClientStatusEvaluator.ClientNotFoundOrInactiveMessage, res1.Message);

        // Deleted Client
        var keyDeletedClient = new ApiKey
        {
            Apikey = "key-2",
            StatusId = (int)StatusEnum.Active,
            Client = new Client { IsDeleted = true, StatusId = (int)StatusEnum.Active }
        };
        var res2 = ClientStatusEvaluator.Evaluate(keyDeletedClient);
        Assert.False(res2.IsActive);
        Assert.Equal(ClientStatusEvaluator.ClientNotFoundOrInactiveMessage, res2.Message);

        // Inactive Status Client
        var keyInactiveClient = new ApiKey
        {
            Apikey = "key-3",
            StatusId = (int)StatusEnum.Active,
            Client = new Client { IsDeleted = false, StatusId = (int)StatusEnum.Inactive }
        };
        var res3 = ClientStatusEvaluator.Evaluate(keyInactiveClient);
        Assert.False(res3.IsActive);
        Assert.Equal(ClientStatusEvaluator.ClientNotFoundOrInactiveMessage, res3.Message);
    }

    [Fact]
    public void Evaluate_PaymentSuspendedClient_ReturnsInactiveWithPaymentSuspendedDetails()
    {
        var client = new Client
        {
            GuidId = "client-guid-1",
            Name = "Empresa Suspensa SRL",
            Rnc = "131234567",
            StatusId = (int)StatusEnum.Active,
            IsDeleted = false,
            ClientInactive = true,
            PaymentSuspendedAtUtc = DateTime.UtcNow.AddDays(-2),
            NextPaymentDate = new DateTime(2026, 9, 10),
            Plan = new Plan { Name = "Plan Renta Basico" },
            IsDgiiProduction = true,
            IsCertified = true
        };

        var apiKey = new ApiKey
        {
            Apikey = "key-valid",
            StatusId = (int)StatusEnum.Active,
            Client = client
        };

        var result = ClientStatusEvaluator.Evaluate(apiKey);

        Assert.False(result.IsActive);
        Assert.True(result.ClientInactive);
        Assert.True(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.PaymentSuspendedMessage, result.Message);
        Assert.Equal("client-guid-1", result.ClientGuid);
        Assert.Equal("Empresa Suspensa SRL", result.ClientName);
        Assert.Equal("131234567", result.Rnc);
        Assert.Equal(new DateTime(2026, 9, 10), result.NextPaymentDate);
        Assert.Equal("Plan Renta Basico", result.PlanName);
        Assert.True(result.IsDgiiProduction);
        Assert.True(result.IsCertified);
    }

    [Fact]
    public void Evaluate_ManuallyInactiveClient_ReturnsInactiveWithClientInactiveMessage()
    {
        var client = new Client
        {
            GuidId = "client-guid-2",
            Name = "Empresa Desactivada SRL",
            Rnc = "131999888",
            StatusId = (int)StatusEnum.Active,
            IsDeleted = false,
            ClientInactive = true,
            PaymentSuspendedAtUtc = null,
            NextPaymentDate = new DateTime(2026, 10, 1),
            Plan = new Plan { Name = "Plan Comprobantes 500" },
            IsDgiiProduction = false,
            IsCertified = false
        };

        var apiKey = new ApiKey
        {
            Apikey = "key-valid-inactive-client",
            StatusId = (int)StatusEnum.Active,
            Client = client
        };

        var result = ClientStatusEvaluator.Evaluate(apiKey);

        Assert.False(result.IsActive);
        Assert.True(result.ClientInactive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientInactiveMessage, result.Message);
        Assert.Equal("client-guid-2", result.ClientGuid);
        Assert.Equal("Empresa Desactivada SRL", result.ClientName);
        Assert.Equal("131999888", result.Rnc);
    }

    [Fact]
    public void Evaluate_FullyActiveClient_ReturnsActiveWithAllDetails()
    {
        var client = new Client
        {
            GuidId = "client-guid-active",
            Name = "Zynstorm Soluciones SRL",
            Rnc = "133009889",
            StatusId = (int)StatusEnum.Active,
            IsDeleted = false,
            ClientInactive = false,
            PaymentSuspendedAtUtc = null,
            NextPaymentDate = new DateTime(2026, 10, 15),
            Plan = new Plan { Name = "Plan Enterprise Ilimitado" },
            IsDgiiProduction = true,
            IsCertified = true
        };

        var apiKey = new ApiKey
        {
            Apikey = "key-enterprise",
            StatusId = (int)StatusEnum.Active,
            Client = client
        };

        var result = ClientStatusEvaluator.Evaluate(apiKey);

        Assert.True(result.IsActive);
        Assert.False(result.ClientInactive);
        Assert.False(result.PaymentSuspended);
        Assert.Equal(ClientStatusEvaluator.ClientActiveMessage, result.Message);
        Assert.Equal("client-guid-active", result.ClientGuid);
        Assert.Equal("Zynstorm Soluciones SRL", result.ClientName);
        Assert.Equal("133009889", result.Rnc);
        Assert.Equal(new DateTime(2026, 10, 15), result.NextPaymentDate);
        Assert.Equal("Plan Enterprise Ilimitado", result.PlanName);
        Assert.True(result.IsDgiiProduction);
        Assert.True(result.IsCertified);
    }
}
