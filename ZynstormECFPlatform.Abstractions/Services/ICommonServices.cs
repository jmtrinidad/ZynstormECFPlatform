namespace ZynstormECFPlatform.Abstractions.Services;

public interface IJwtTokenService
{
    string GenerateToken(string userId, string email, string role);
    bool ValidateToken(string token);
}

public interface IEncryptedService
{
    string Encrypt(string text);
    string Decrypt(string encryptedText);
}
