namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEmailService
{
    Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, byte[]? attachmentBytes = null, string? attachmentFileName = null, CancellationToken cancellationToken = default);
    Task SendApiKeyEmailAsync(string email, string apiKey, string secretKey);
}
