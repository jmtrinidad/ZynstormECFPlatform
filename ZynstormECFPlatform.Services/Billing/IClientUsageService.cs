namespace ZynstormECFPlatform.Services.Billing;

public interface IClientUsageService
{
    /// <summary>
    /// Suma 1 al consumo mensual del cliente si tiene plan activo y el documento no fue contado antes.
    /// Nunca lanza excepción: los errores se registran y devuelve false.
    /// </summary>
    Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default);
}
