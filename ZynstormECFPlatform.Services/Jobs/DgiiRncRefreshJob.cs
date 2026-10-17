using Hangfire;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Actualización diaria del padrón de la DGII.
///
/// Llama a la versión esperable del servicio a propósito: con el método fire-and-forget,
/// Hangfire marcaría el job como correcto antes de que la descarga siquiera empiece, y los
/// reintentos no servirían de nada.
/// </summary>
[DisableConcurrentExecution(timeoutInSeconds: 0)]
[AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 300, 900, 1800 })]
public sealed class DgiiRncRefreshJob(
    IDgiiRncService dgiiRncService,
    ILogger<DgiiRncRefreshJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Actualización diaria del padrón de la DGII: iniciando.");

        var ejecutada = await dgiiRncService.RunDownloadAndIndexingAsync(cancellationToken);

        if (!ejecutada)
        {
            logger.LogInformation("Ya había una descarga del padrón en curso; no se dispara otra.");
            return;
        }

        var status = await dgiiRncService.GetStatusAsync(cancellationToken);

        logger.LogInformation(
            "Actualización diaria del padrón de la DGII completada: {Count} registros.",
            status.TotalRecords);
    }
}
