using Hangfire;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Reconsulta a la DGII el estado de un comprobante que se quedó «Enviado» sin resolverse (el
/// seguimiento original se agotó). No retransmite. Lo dispara el re-encolado manual.
///
/// Solo recibe identificadores: el TrackId y el certificado se cargan de la base de datos en cada
/// consulta, porque Hangfire guarda los argumentos de un job en claro. Se reprograma a sí mismo con
/// espera creciente (<see cref="EcfRetrackSchedule"/>) hasta que la DGII resuelva o se agoten los
/// intentos.
/// </summary>
[Queue("ecf-transmit")]
[AutomaticRetry(Attempts = 0)]
public class EcfRetrackJob(IReceivedEcfProductionService service, EcfTrackingJob tracking)
{
    public async Task Execute(int ecfDocumentId, int attemptNumber, DgiiEnvironment environment)
    {
        var material = await service.PrepareRetrackAsync(ecfDocumentId);
        if (material is null)
            return;

        var stillPending = await tracking.CheckAsync(
            material.TrackId, environment, material.IssuerRnc, material.CertBase64, material.CertPass,
            ecfDocumentId, attemptNumber);

        if (stillPending && EcfRetrackSchedule.NextDelay(attemptNumber) is { } delay)
        {
            BackgroundJob.Schedule<EcfRetrackJob>(
                job => job.Execute(ecfDocumentId, attemptNumber + 1, environment),
                delay);
        }
    }
}
