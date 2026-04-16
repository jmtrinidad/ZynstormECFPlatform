namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEncryptedService
{
    string EncryptString(string plainText);

    string DecryptString(string cipherText);

    /// <summary>
    /// Cifra un array de bytes usando la SecretKey del cliente como base para derivar una clave AES-256.
    /// El IV generado aleatoriamente se antepone al resultado cifrado.
    /// </summary>
    string EncryptWithSecret(byte[] plainData, string secretKey);

    /// <summary>
    /// Descifra un string Base64 que fue cifrado con EncryptWithSecret, usando la SecretKey del cliente.
    /// </summary>
    byte[] DecryptWithSecret(string cipherText, string secretKey);
}