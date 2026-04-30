using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Common.Utilities;

/// <summary>
/// General utility methods for the Zynstorm ECF Platform.
/// </summary>
public static class Tools
{
    private const string DefaultCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>
    /// Generates a random alphanumeric code of the specified length.
    /// </summary>
    public static string GenerateRandomCode(int length, string charset = DefaultCharset)
    {
        var random = Random.Shared;
        return new string(Enumerable.Repeat(charset, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    /// <summary>
    /// Generates a cryptographically secure random string.
    /// </summary>
    public static string GenerateSecureRandomString(int length, string charset = "abcdefghijklMnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
    {
        var result = new StringBuilder(length);
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);

            foreach (var b in bytes)
            {
                result.Append(charset[b % charset.Length]);
            }
        }
        return result.ToString();
    }

    /// <summary>
    /// Performs a deep clone of an object using JSON serialization.
    /// </summary>
    public static T? DeepClone<T>(T source)
    {
        if (source == null) return default;
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// Formats a decimal to string with 2 decimal places using InvariantCulture.
    /// </summary>
    public static string? FormatDecimal(decimal? value)
    {
        return value?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a decimal from string using InvariantCulture.
    /// </summary>
    public static decimal? ParseDecimal(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the current Dominican Republic time.
    /// </summary>
    public static DateTime GetDrNow() => DateTimeExtensions.DrNow;
    
    /// <summary>
    /// Validates if an RNC/Cedula has the correct length (9 or 11 digits).
    /// </summary>
    public static bool IsValidRncFormat(string? rnc)
    {
        if (string.IsNullOrWhiteSpace(rnc)) return false;
        var cleanRnc = new string(rnc.Where(char.IsDigit).ToArray());
        return cleanRnc.Length == 9 || cleanRnc.Length == 11;
    }
}
