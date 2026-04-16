using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core;

namespace ZynstormECFPlatform.Services;

public class EncryptedService(IOptions<AppSettings> options, ILogger<EncryptedService> logger) : IEncryptedService
{
    private readonly AppSettings _appSettings = options.Value;
    private readonly ILogger<EncryptedService> _logger = logger;

    public string EncryptString(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                throw new ArgumentNullException(nameof(plainText));

            byte[] array;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_appSettings.Key);
                aes.IV = Encoding.UTF8.GetBytes(_appSettings.IV);

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using MemoryStream memoryStream = new();
                using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);

                using (StreamWriter streamWriter = new((Stream)cryptoStream))
                {
                    streamWriter.Write(plainText);
                }

                array = memoryStream.ToArray();
            }

            return Convert.ToBase64String(array);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, message: exception.Message);
            return string.Empty;
        }
    }

    public string DecryptString(string encrypted)
    {
        try
        {
            if (string.IsNullOrEmpty(encrypted))
                throw new ArgumentNullException(nameof(encrypted));

            using Aes aes = Aes.Create();
            byte[] buffer = Convert.FromBase64String(encrypted);

            aes.Key = Encoding.UTF8.GetBytes(_appSettings.Key);
            aes.IV = Encoding.UTF8.GetBytes(_appSettings.IV);
            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using MemoryStream memoryStream = new(buffer);

            using CryptoStream cryptoStream = new((Stream)memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new((Stream)cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return string.Empty;
        }
    }

    public string EncryptWithSecret(byte[] plainData, string secretKey)
    {
        try
        {
            using Aes aes = Aes.Create();

            // Derivamos una clave AES-256 a partir de la SecretKey del cliente
            using var sha256 = SHA256.Create();
            aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));

            // Generamos un IV aleatorio para mayor seguridad
            aes.GenerateIV();
            var iv = aes.IV;

            using var encryptor = aes.CreateEncryptor(aes.Key, iv);
            using var ms = new MemoryStream();

            // Anteponemos el IV al stream para recuperarlo al descifrar
            ms.Write(iv, 0, iv.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainData, 0, plainData.Length);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return string.Empty;
        }
    }

    public byte[] DecryptWithSecret(string cipherText, string secretKey)
    {
        try
        {
            var fullBuffer = Convert.FromBase64String(cipherText);

            using Aes aes = Aes.Create();

            // Derivamos la misma clave AES-256 usando SHA256 de la SecretKey
            using var sha256 = SHA256.Create();
            aes.Key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secretKey));

            // El IV está en los primeros 16 bytes del buffer
            var iv = new byte[16];
            Buffer.BlockCopy(fullBuffer, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Los datos cifrados comienzan después del IV
            var cipherData = new byte[fullBuffer.Length - iv.Length];
            Buffer.BlockCopy(fullBuffer, iv.Length, cipherData, 0, cipherData.Length);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipherData);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var result = new MemoryStream();

            cs.CopyTo(result);
            return result.ToArray();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, exception.Message);
            return [];
        }
    }
}