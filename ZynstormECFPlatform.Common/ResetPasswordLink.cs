namespace ZynstormECFPlatform.Common;

/// <summary>
/// Enlace del correo para restablecer la contraseña. Se arma con la URL pública del API
/// (AppSettings:PlatformUrl, que incluye el prefijo /api del proxy): detrás del proxy la
/// petición llega sin ese prefijo, así que Url.Action daría un enlace roto.
/// </summary>
public static class ResetPasswordLink
{
    public static string? Build(string? platformUrl, string encodedToken, string identifier)
    {
        if (string.IsNullOrWhiteSpace(platformUrl)) return null;

        return $"{platformUrl.Trim().TrimEnd('/')}/ResetPassword/reset-password" +
               $"?token={Uri.EscapeDataString(encodedToken)}&identifier={Uri.EscapeDataString(identifier)}";
    }
}
