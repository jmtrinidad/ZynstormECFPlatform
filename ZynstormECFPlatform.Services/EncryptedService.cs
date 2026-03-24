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
}