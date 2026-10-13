using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Convierte el timeout de la DGII en un fallo de transporte (resultado con
/// <see cref="DgiiTransmissionResult.TransportFailure"/>) para el envío diferido de <c>emit</c>, que
/// reintenta esos fallos.
///
/// Vive aquí y no en <c>DgiiTransmissionService.SendEcfAsync</c> a propósito: la certificación y
/// cualquier otro llamador de <c>SendEcfAsync</c> dependen de que un timeout siga lanzando la
/// excepción, como en <c>main</c>.
/// </summary>
public static class DgiiSendGuard
{
    public static async Task<DgiiTransmissionResult> SendAsync(Func<Task<DgiiTransmissionResult>> send, bool isSummary)
    {
        try
        {
            return await send();
        }
        catch (TaskCanceledException ex)
        {
            // HttpClient reporta su timeout como cancelación: la DGII no respondió a tiempo.
            return new DgiiTransmissionResult
            {
                Error = $"La DGII no respondió a tiempo para {(isSummary ? "Resumen B2C/RFCE" : "e-CF")}.",
                Mensaje = ex.Message,
                TransportFailure = true
            };
        }
    }
}
