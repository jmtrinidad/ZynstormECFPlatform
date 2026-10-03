using System.Net.Mail;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Credentials;

public enum ClientApiKeySendOutcome
{
    Sent,
    InvalidEmail,
    SecretUnavailable
}

public sealed record ClientApiKeySendResult(ClientApiKeySendOutcome Outcome, string? ApiKey, bool Generated, string? Error)
{
    public static ClientApiKeySendResult Sent(string apiKey, bool generated) =>
        new(ClientApiKeySendOutcome.Sent, apiKey, generated, null);

    public static ClientApiKeySendResult InvalidEmail(string error) =>
        new(ClientApiKeySendOutcome.InvalidEmail, null, false, error);

    public static ClientApiKeySendResult SecretUnavailable(string error) =>
        new(ClientApiKeySendOutcome.SecretUnavailable, null, false, error);
}

public interface IClientApiKeyService
{
    /// <summary>
    /// Envía por correo la API Key y el Secret Key de un cliente. Si el cliente no tiene una key activa,
    /// la genera y la guarda antes de enviarla. Los errores de envío (SMTP) se propagan como excepción.
    /// </summary>
    Task<ClientApiKeySendResult> SendAsync(int clientId, string email, CancellationToken cancellationToken = default);
}

public class ClientApiKeyService(
    IApiKeyService apiKeyService,
    IEncryptedService encryptedService,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IClientApiKeyService
{
    private const string InvalidEmailMessage = "El correo electrónico no es válido.";
    private const string SecretUnavailableMessage =
        "La API Key existente no tiene un Secret Key recuperable. No se regeneró para no romper la integración del cliente; contacte a soporte.";

    public async Task<ClientApiKeySendResult> SendAsync(int clientId, string email, CancellationToken cancellationToken = default)
    {
        var recipient = email?.Trim();
        if (!IsValidEmail(recipient))
            return ClientApiKeySendResult.InvalidEmail(InvalidEmailMessage);

        var existing = await apiKeyService.GetNoTrackingByAsync(
            k => k.ClientId == clientId && k.StatusId == (int)StatusEnum.Active, cancellationToken);

        string apiKey;
        string secretKey;
        bool generated;

        if (existing != null)
        {
            secretKey = string.IsNullOrWhiteSpace(existing.SecretKey)
                ? string.Empty
                : encryptedService.DecryptString(existing.SecretKey);

            if (string.IsNullOrEmpty(secretKey))
                return ClientApiKeySendResult.SecretUnavailable(SecretUnavailableMessage);

            apiKey = existing.Apikey;
            generated = false;
        }
        else
        {
            apiKey = Tools.GenerateSecureRandomString(32);
            secretKey = Tools.GenerateSecureRandomString(64);

            var encryptedSecret = encryptedService.EncryptString(secretKey);
            if (string.IsNullOrEmpty(encryptedSecret))
                throw new InvalidOperationException("No se pudo cifrar el Secret Key del cliente.");

            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await apiKeyService.InsertAsync(new ApiKey
                {
                    ClientId = clientId,
                    Apikey = apiKey,
                    SecretKey = encryptedSecret,
                    StatusId = (int)StatusEnum.Active
                });
            }, cancellationToken);

            generated = true;
        }

        await emailService.SendApiKeyEmailAsync(recipient!, apiKey, secretKey);

        return ClientApiKeySendResult.Sent(apiKey, generated);
    }

    private static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && MailAddress.TryCreate(value, out var address)
        && address.Address == value
        && address.Host.Contains('.');
}
