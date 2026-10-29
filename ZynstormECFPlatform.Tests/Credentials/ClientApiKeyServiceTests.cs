using System.Linq.Expressions;
using Moq;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Credentials;

namespace ZynstormECFPlatform.Tests.Credentials;

public class ClientApiKeyServiceTests
{
    private const int ClientId = 7;

    private readonly Mock<IApiKeyService> _apiKeys = new();
    private readonly Mock<IEncryptedService> _encryption = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly List<string> _calls = [];
    private ApiKey? _inserted;

    private ClientApiKeyService CreateService(ApiKey? existing)
    {
        _apiKeys
            .Setup(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _apiKeys
            .Setup(s => s.InsertAsync(It.IsAny<ApiKey>()))
            .Returns((ApiKey key) =>
            {
                _calls.Add("insert");
                _inserted = key;
                return Task.FromResult<ApiKey?>(key);
            });

        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task> operation, CancellationToken token) => operation(token));

        _email
            .Setup(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(() =>
            {
                _calls.Add("email");
                return Task.CompletedTask;
            });

        return new ClientApiKeyService(_apiKeys.Object, _encryption.Object, _email.Object, _unitOfWork.Object);
    }

    private static ApiKey ExistingKey(string? secretKey = "CIPHER") => new()
    {
        ApiKeyId = 1,
        ClientId = ClientId,
        Apikey = "EXISTING-API-KEY",
        SecretKey = secretKey,
        StatusId = (int)StatusEnum.Active
    };

    [Fact]
    public async Task ExistingKey_ResendsDecryptedSecret_WithoutTouchingTheDatabase()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns("PLAIN-SECRET");

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.Equal("EXISTING-API-KEY", result.ApiKey);
        Assert.False(result.Generated);
        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", "EXISTING-API-KEY", "PLAIN-SECRET"), Times.Once);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoKey_GeneratesStoresEncryptedSecretAndSendsTheSameValues()
    {
        var service = CreateService(existing: null);
        string? plainSecret = null;
        _encryption
            .Setup(e => e.EncryptString(It.IsAny<string>()))
            .Returns((string plain) =>
            {
                plainSecret = plain;
                return "ENCRYPTED";
            });

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.True(result.Generated);
        Assert.NotNull(_inserted);
        Assert.Equal(ClientId, _inserted!.ClientId);
        Assert.Equal((int)StatusEnum.Active, _inserted.StatusId);
        Assert.Equal("ENCRYPTED", _inserted.SecretKey);
        Assert.Equal(32, _inserted.Apikey.Length);
        Assert.Equal(64, plainSecret!.Length);
        Assert.Equal(_inserted.Apikey, result.ApiKey);
        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", _inserted.Apikey, plainSecret), Times.Once);
    }

    [Fact]
    public async Task NoKey_InsertsBeforeSendingTheEmail()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED");

        await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(["insert", "email"], _calls);
    }

    [Fact]
    public async Task NoKey_WhenEmailFails_ErrorPropagatesButKeyStaysStored()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED");
        _email
            .Setup(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("smtp caído"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync(ClientId, "cliente@empresa.com"));

        Assert.NotNull(_inserted);
    }

    [Fact]
    public async Task NoKey_WhenEncryptionFails_ThrowsAndStoresNothing()
    {
        var service = CreateService(existing: null);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns(string.Empty);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendAsync(ClientId, "cliente@empresa.com"));

        Assert.Null(_inserted);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExistingKeyWithoutSecret_ReportsSecretUnavailable_AndSendsNothing(string? storedSecret)
    {
        var service = CreateService(ExistingKey(storedSecret));

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.SecretUnavailable, result.Outcome);
        Assert.NotNull(result.Error);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
    }

    [Fact]
    public async Task ExistingKeyThatCannotBeDecrypted_ReportsSecretUnavailable_AndDoesNotRegenerate()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns(string.Empty);

        var result = await service.SendAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.SecretUnavailable, result.Outcome);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-un-correo")]
    [InlineData("sin-dominio@")]
    [InlineData("a@sinpunto")]
    [InlineData("Juan Perez <juan@empresa.com>")]
    public async Task InvalidEmail_IsRejected_BeforeAnyWork(string email)
    {
        var service = CreateService(ExistingKey());

        var result = await service.SendAsync(ClientId, email);

        Assert.Equal(ClientApiKeySendOutcome.InvalidEmail, result.Outcome);
        Assert.NotNull(result.Error);
        _apiKeys.Verify(s => s.GetNoTrackingByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Email_IsTrimmedBeforeSending()
    {
        var service = CreateService(ExistingKey());
        _encryption.Setup(e => e.DecryptString("CIPHER")).Returns("PLAIN-SECRET");

        await service.SendAsync(ClientId, "  cliente@empresa.com  ");

        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", "EXISTING-API-KEY", "PLAIN-SECRET"), Times.Once);
    }

    [Fact]
    public async Task Regenerate_WhenExistingActiveKeys_DeactivatesOldKeysAndInsertsNewActiveKey()
    {
        var oldKey = ExistingKey();
        var service = CreateService(oldKey);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED-NEW");

        _apiKeys
            .Setup(s => s.GetManyByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldKey]);

        IEnumerable<ApiKey>? updatedList = null;
        _apiKeys
            .Setup(s => s.UpdateAsync(It.IsAny<IEnumerable<ApiKey>>()))
            .Returns((IEnumerable<ApiKey> list) =>
            {
                updatedList = list;
                return Task.CompletedTask;
            });

        var result = await service.RegenerateAsync(ClientId, "cliente@empresa.com");

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.True(result.Generated);
        Assert.NotNull(result.ApiKey);
        Assert.NotEqual("EXISTING-API-KEY", result.ApiKey);

        // Verifica que la anterior se haya desactivado
        Assert.NotNull(updatedList);
        var deactivated = updatedList!.First();
        Assert.Equal((int)StatusEnum.Inactive, deactivated.StatusId);

        // Verifica que la nueva sea activa
        Assert.NotNull(_inserted);
        Assert.Equal((int)StatusEnum.Active, _inserted!.StatusId);
        Assert.Equal("ENCRYPTED-NEW", _inserted.SecretKey);
        Assert.Equal(result.ApiKey, _inserted.Apikey);

        _email.Verify(e => e.SendApiKeyEmailAsync("cliente@empresa.com", result.ApiKey!, It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Regenerate_WithoutEmail_DoesNotSendEmail()
    {
        var oldKey = ExistingKey();
        var service = CreateService(oldKey);
        _encryption.Setup(e => e.EncryptString(It.IsAny<string>())).Returns("ENCRYPTED-NEW");

        _apiKeys
            .Setup(s => s.GetManyByAsync(It.IsAny<Expression<Func<ApiKey, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([oldKey]);

        var result = await service.RegenerateAsync(ClientId, null);

        Assert.Equal(ClientApiKeySendOutcome.Sent, result.Outcome);
        Assert.True(result.Generated);
        Assert.NotNull(result.ApiKey);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("invalido")]
    [InlineData("sin-punto@dominio")]
    public async Task Regenerate_InvalidEmail_ReturnsInvalidEmail_WithoutTouchingDatabase(string invalidEmail)
    {
        var service = CreateService(ExistingKey());

        var result = await service.RegenerateAsync(ClientId, invalidEmail);

        Assert.Equal(ClientApiKeySendOutcome.InvalidEmail, result.Outcome);
        _apiKeys.Verify(s => s.InsertAsync(It.IsAny<ApiKey>()), Times.Never);
        _email.Verify(e => e.SendApiKeyEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
