using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Web.Api.Services;

/// <summary>
/// Al arrancar el API, si el padron de la DGII no esta indexado, dispara la descarga en
/// segundo plano. Pasa tipicamente despues de un redeploy en Dokploy sin el volumen de
/// DGII_ROOT: sin esto hay que entrar a Configuraciones y lanzarla a mano en cada publicacion.
/// </summary>
public sealed class DgiiRncBootstrapService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<DgiiRncBootstrapService> logger) : BackgroundService
{
    // Se puede apagar con Dgii:AutoDownloadOnStartup = false (o DGII__AUTODOWNLOADONSTARTUP
    // como variable de entorno) para no bajar 60 MB en cada arranque local.
    private const string EnabledKey = "Dgii:AutoDownloadOnStartup";

    // Dias de antiguedad a partir de los cuales el padron se vuelve a bajar al arrancar.
    private const string MaxAgeKey = "Dgii:MaxAgeDays";

    // Margen para que el arranque (migraciones, warmup) no compita con la descarga.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue(EnabledKey, true))
        {
            logger.LogInformation("Descarga automatica del padron de la DGII desactivada por configuracion.");
            return;
        }

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var dgiiRncService = scope.ServiceProvider.GetRequiredService<IDgiiRncService>();

            var status = await dgiiRncService.GetStatusAsync(stoppingToken);

            if (status.IsDownloading || status.IsIndexing)
            {
                logger.LogInformation("El padron de la DGII ya se esta procesando; no se dispara otra descarga.");
                return;
            }

            var maxAgeDays = configuration.GetValue(MaxAgeKey, DgiiRefreshPolicy.DefaultMaxAgeDays);

            // La fecha del padron se guarda con DateTime.Now, asi que se compara con el
            // mismo reloj. Mezclarlo con la hora dominicana metería un desfase de 4 horas.
            if (!DgiiRefreshPolicy.ShouldRefreshOnStartup(
                    status.TotalRecords,
                    status.LastUpdated,
                    DateTime.Now,
                    maxAgeDays))
            {
                logger.LogInformation(
                    "Padron de la DGII al dia: {Count} registros, actualizado {LastUpdated}.",
                    status.TotalRecords,
                    status.LastUpdated);
                return;
            }

            logger.LogInformation(
                "Padron de la DGII ausente o vencido (actualizado {LastUpdated}, maximo {MaxAgeDays} dias). Iniciando descarga e indexacion en segundo plano.",
                status.LastUpdated,
                maxAgeDays);

            // El servicio es transient y la corrida sigue despues de soltar el scope: no
            // depende de nada scoped, solo de HttpClient y configuracion.
            await dgiiRncService.StartDownloadAndIndexingAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado normal del host mientras se esperaba el margen de arranque.
        }
        catch (Exception exception)
        {
            // Nunca tumbar el arranque del API por esto: el padron siempre se puede bajar a
            // mano desde Configuraciones.
            logger.LogError(exception, "No se pudo iniciar la descarga automatica del padron de la DGII.");
        }
    }
}
