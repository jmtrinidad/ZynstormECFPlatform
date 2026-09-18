using System.Security.Cryptography;
using System.Text;

namespace ZynstormECFPlatform.Services;

/// <summary>Genera, normaliza y deriva el hash de seriales.</summary>
public static class SerialCodeGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int GroupCount = 4;
    private const int GroupLength = 5;

    public static string Generate()
    {
        var characters = new char[GroupCount * GroupLength];

        for (var index = 0; index < characters.Length; index++)
            characters[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return string.Join('-', Enumerable.Range(0, GroupCount)
            .Select(group => new string(characters, group * GroupLength, GroupLength)));
    }

    public static string Normalize(string serial)
    {
        if (string.IsNullOrWhiteSpace(serial))
            return string.Empty;

        return new string(serial
            .Where(character => character != '-' && !char.IsWhiteSpace(character))
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    public static string ComputeHash(string serial)
    {
        var normalized = Normalize(serial);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
