using System.Text.RegularExpressions;

namespace ZynstormECFPlatform.Services.Ri.Html;

/// <summary>
/// Valida el logo del emisor: un data URI base64 PNG/JPEG/WebP de hasta 500 KB cuyo contenido
/// coincide con el tipo declarado. SVG se rechaza a propósito (puede llevar scripts).
/// </summary>
public static class RiLogoValidator
{
    public const int MaxBytes = 500 * 1024;

    private static readonly Regex Pattern = new(
        @"^data:image/(png|jpeg|webp);base64,([A-Za-z0-9+/]+={0,2})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Devuelve null si el logo es válido; si no, el mensaje de error.</summary>
    public static string? Validate(string? dataUri)
    {
        if (string.IsNullOrWhiteSpace(dataUri))
        {
            return "El logo está vacío.";
        }

        var match = Pattern.Match(dataUri.Trim());
        if (!match.Success)
        {
            return "El logo debe ser una imagen PNG, JPG o WebP.";
        }

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(match.Groups[2].Value);
        }
        catch (FormatException)
        {
            return "El logo no es una imagen válida.";
        }

        if (bytes.Length > MaxBytes)
        {
            return "El logo supera el tamaño máximo de 500 KB.";
        }

        var matchesType = match.Groups[1].Value switch
        {
            "png" => bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47,
            "jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
            "webp" => bytes.Length >= 12
                      && bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F'
                      && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P',
            _ => false
        };

        return matchesType ? null : "El contenido del archivo no coincide con su formato de imagen.";
    }
}
