using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ZynstormECFPlatform.Web.Api.Helpers;

/// <summary>
/// Punto único para el manejo de la cookie httpOnly que transporta el JWT.
/// El token deja de viajar en el cuerpo de la respuesta y deja de ser legible por JS,
/// de modo que un XSS no puede robar la sesión.
/// </summary>
public static class AuthCookie
{
    /// <summary>Nombre de la cookie. SignalR usa "access_token" por convención para el query string,
    /// pero aquí es solo el nombre de la cookie httpOnly.</summary>
    public const string Name = "access_token";

    private static SameSiteMode SameSite => SameSiteMode.Lax;

    /// <summary>
    /// Secure se desactiva SOLO en Development, donde el front habla con el backend por HTTP plano
    /// (mismo origen vía el proxy de Next). En Staging/Producción hay TLS, así que Secure = true.
    /// </summary>
    private static bool IsSecure(IHostEnvironment env) => !env.IsDevelopment();

    /// <summary>Opciones para FIJAR la cookie. Expira junto con el JWT.</summary>
    public static CookieOptions BuildSetOptions(IHostEnvironment env, DateTime expirationUtc)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = IsSecure(env),
            SameSite = SameSite,
            // El JWT se genera con DateTime.UtcNow.AddDays(1) => Kind = Utc.
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expirationUtc, DateTimeKind.Utc)),
            Path = "/",
            IsEssential = true,
        };
    }

    /// <summary>Opciones para BORRAR la cookie. Deben coincidir en Path/Secure/SameSite con las de set.</summary>
    public static CookieOptions BuildDeleteOptions(IHostEnvironment env)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = IsSecure(env),
            SameSite = SameSite,
            Path = "/",
            IsEssential = true,
        };
    }

    /// <summary>Lee el token desde la cookie (helper compartido por los handlers de autenticación).</summary>
    public static string? ReadToken(HttpRequest request)
    {
        return request.Cookies.TryGetValue(Name, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;
    }
}
