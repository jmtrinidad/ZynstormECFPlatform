namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEmailService
{
    Task SendApiKeyEmailAsync(string email, string apiKey, string secretKey);
}
