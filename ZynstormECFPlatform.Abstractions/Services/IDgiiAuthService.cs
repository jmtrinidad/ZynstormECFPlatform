using System.Threading.Tasks;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Service to handle authentication with the DGII API.
/// Retrieves passing seeds, applies digital signatures, and returns Bearer tokens.
/// </summary>
public interface IDgiiAuthService
{
    /// <summary>
    /// Gets a valid JWT token from the DGII authentication service.
    /// </summary>
    /// <param name="rncEmisor">The RNC of the issuer for caching the token.</param>
    /// <param name="isProduction">If true, uses Production DGII environment. If false, uses Test (Pre-certificación) environment.</param>
    /// <param name="certificateBase64">The base64 representation of the client's PKCS12 (.p12/.pfx) certificate.</param>
    /// <param name="certificatePassword">The password for the certificate.</param>
    /// <returns>A valid JWT Bearer token string.</returns>
    Task<string> GetTokenAsync(string rncEmisor, bool isProduction, string certificateBase64, string certificatePassword);
}
