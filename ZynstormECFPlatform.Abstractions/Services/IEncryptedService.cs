namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEncryptedService
{
    string EncryptString(string plainText);

    string DecryptString(string cipherText);
}