using System.Security.Cryptography;
using System.Text;

namespace ZynstormECFPlatform.Common.Utilities;

public static class KeyGenerator
{
    private const string Chars = "abcdefghijklMnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string GenerateApiKey(int length = 32)
    {
        return GenerateRandomString(length);
    }

    public static string GenerateSecretKey(int length = 64)
    {
        return GenerateRandomString(length);
    }

    private static string GenerateRandomString(int length)
    {
        var result = new StringBuilder(length);
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            foreach (var b in bytes)
            {
                result.Append(Chars[b % Chars.Length]);
            }
        }
        return result.ToString();
    }
}
