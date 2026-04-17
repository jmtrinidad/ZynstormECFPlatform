using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Jobs;

public class EcfTrackingJob
{
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IDgiiAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<EcfTrackingJob> _logger;

    public EcfTrackingJob(
        IDgiiTransmissionService transmissionService,
        IDgiiAuthService authService,
        ICacheService cacheService,
        ILogger<EcfTrackingJob> logger)
    {
        _transmissionService = transmissionService;
        _authService = authService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task Execute(string trackId, DgiiEnvironment environment, string rncEmisor, string certBase64, string certPass)
    {
        _logger.LogInformation("Checking status for TrackId: {TrackId}", trackId);

        try
        {
            // 1. Get Token (using cached token from AuthService usually)
            string token = await _authService.GetTokenAsync(rncEmisor, environment, certBase64, certPass);

            // 2. Query Status
            var statusResponse = await _transmissionService.GetStatusAsync(environment, token, trackId);

            // 3. Update Cache
            string cacheKey = $"EcfStatus_{trackId}";
            _cacheService.Set(cacheKey, statusResponse, TimeSpan.FromHours(1));

            _logger.LogInformation("TrackId {TrackId} status: {Status}", trackId, statusResponse.Estado);

            // 4. Polling logic: If "Recibido", schedule retry in 3 seconds
            // DGII statuses for pending are typically "Recibido" or "En Proceso"
            if (statusResponse.Estado.Equals("Recibido", StringComparison.OrdinalIgnoreCase))
            {
                BackgroundJob.Schedule<EcfTrackingJob>(
                    j => j.Execute(trackId, environment, rncEmisor, certBase64, certPass), 
                    TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking status for TrackId: {TrackId}", trackId);
            // We could retry here too if needed, but Hangfire has built-in retries for failures
        }
    }
}
