namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEmailService
{
    Task SendEmailAsync(string recipientEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
    Task SendApiKeyEmailAsync(string email, string apiKey, string secretKey);
}
