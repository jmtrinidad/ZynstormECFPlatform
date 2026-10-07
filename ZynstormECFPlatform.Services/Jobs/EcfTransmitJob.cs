using Hangfire;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Transmite a la DGII un e-CF que <c>emit?deferred=true</c> ya firmó y respondió. Cola propia
/// para que el envío no compita con los jobs de mantenimiento. Hangfire no reintenta: los
/// reintentos por fallo de transporte los programa este mismo job con esperas crecientes.
///
/// Solo recibe identificadores: el certificado y su contraseña se recargan de la base de
/// datos, porque Hangfire guarda los argumentos de un job en claro.
/// </summary>
[Queue("ecf-transmit")]
[AutomaticRetry(Attempts = 0)]
public class EcfTransmitJob(IReceivedEcfProductionService service)
{
    public async Task Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)
    {
        var outcome = await service.TransmitDeferredAsync(ecfDocumentId, attemptNumber, environment);

        if (outcome.RetryAfter is { } delay)
        {
            BackgroundJob.Schedule<EcfTransmitJob>(
                job => job.Execute(ecfDocumentId, attemptNumber + 1, environment),
                delay);
        }
    }
}
