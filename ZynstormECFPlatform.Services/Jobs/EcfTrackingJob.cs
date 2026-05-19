using System;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using ZynstormECFPlatform.Abstractions.DataServices;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Services.Jobs;

public class EcfTrackingJob
{
    private const int MaxAttempts = 20;

    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IDgiiAuthService _authService;
    private readonly ICacheService _cacheService;
    private readonly IEcfDocumentService _ecfDocumentService;
    private readonly IEcfXmlDocumentService _ecfXmlDocumentService;
    private readonly IEcfTransmissionService _ecfTransmissionService;
    private readonly IEcfStatusHistoryService _ecfStatusHistoryService;
    private readonly ISystemLogService _systemLogService;
    private readonly ILogger<EcfTrackingJob> _logger;

    public EcfTrackingJob(
        IDgiiTransmissionService transmissionService,
        IDgiiAuthService authService,
        ICacheService cacheService,
        IEcfDocumentService ecfDocumentService,
        IEcfXmlDocumentService ecfXmlDocumentService,
        IEcfTransmissionService ecfTransmissionService,
        IEcfStatusHistoryService ecfStatusHistoryService,
        ISystemLogService systemLogService,
        ILogger<EcfTrackingJob> logger)
    {
        _transmissionService = transmissionService;
        _authService = authService;
        _cacheService = cacheService;
        _ecfDocumentService = ecfDocumentService;
        _ecfXmlDocumentService = ecfXmlDocumentService;
        _ecfTransmissionService = ecfTransmissionService;
        _ecfStatusHistoryService = ecfStatusHistoryService;
        _systemLogService = systemLogService;
        _logger = logger;
    }

    public async Task Execute(
        string trackId,
        DgiiEnvironment environment,
        string rncEmisor,
        string certBase64,
        string certPass,
        int ecfDocumentId = 0,
        int attemptNumber = 1)
    {
        _logger.LogInformation("Checking status for TrackId: {TrackId}", trackId);

        try
        {
            string token = await _authService.GetTokenAsync(rncEmisor, environment, certBase64, certPass);
            var statusResponse = await _transmissionService.GetStatusAsync(environment, token, trackId);

            string cacheKey = $"EcfStatus_{trackId}";
            _cacheService.Set(cacheKey, statusResponse, TimeSpan.FromHours(1));

            _logger.LogInformation("TrackId {TrackId} status: {Status}", trackId, statusResponse.Estado);

            if (ecfDocumentId > 0)
            {
                await PersistStatusAsync(ecfDocumentId, trackId, rncEmisor, statusResponse, attemptNumber, environment);
            }

            if (ReceivedEcfProductionService.IsPendingDgiiStatus(statusResponse) && attemptNumber < MaxAttempts)
            {
                BackgroundJob.Schedule<EcfTrackingJob>(
                    j => j.Execute(trackId, environment, rncEmisor, certBase64, certPass, ecfDocumentId, attemptNumber + 1),
                    TimeSpan.FromSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking status for TrackId: {TrackId}", trackId);
            if (ecfDocumentId > 0)
            {
                await PersistJobErrorAsync(ecfDocumentId, trackId, ex);
            }
        }
    }

    private async Task PersistStatusAsync(int ecfDocumentId, string trackId, string rncEmisor, DgiiStatusResponse statusResponse, int attemptNumber, DgiiEnvironment environment = DgiiEnvironment.Production)
    {
        var ecfDocument = await _ecfDocumentService.GetAsync(ecfDocumentId);
        if (ecfDocument == null) return;

        var statusId = ReceivedEcfProductionService.MapDgiiStatusToEcfStatus(statusResponse);
        var message = $"Consulta DGII TrackId {trackId}: {statusResponse.Estado}";
        QrMetadata? qrMetadata = null;
        if (statusId == 10)
        {
            var xmlDocument = await _ecfXmlDocumentService.GetByAsync(x => x.EcfDocumentId == ecfDocumentId);
            qrMetadata = ReceivedEcfProductionService.BuildQrMetadata(ecfDocument, xmlDocument?.XmlSigned ?? string.Empty, rncEmisor, environment);
            message = $"{message}. CodigoSeguridad: {qrMetadata.SecurityCode}. FechaFirma: {qrMetadata.SignatureDate}. QR: {qrMetadata.QrUrl}";
        }

        ecfDocument.EcfStatusId = statusId;
        await _ecfDocumentService.UpdateAsync(ecfDocument);

        await _ecfStatusHistoryService.InsertAsync(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocumentId,
            EcfStatusId = statusId,
            Message = message
        });

        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocumentId,
            TrackId = trackId,
            AttemptNumber = attemptNumber,
            ResponsePayload = JsonSerializer.Serialize(new { statusResponse, qrMetadata }),
            EcfStatusId = statusId,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = statusResponse.Codigo ?? string.Empty,
            ResponseMessage = string.IsNullOrWhiteSpace(statusResponse.Mensaje) ? statusResponse.Estado : statusResponse.Mensaje,
            Success = statusId == 10
        });

        await _systemLogService.InsertAsync(new SystemLog
        {
            ClientId = ecfDocument.ClientId,
            EcfDocumentId = ecfDocumentId,
            LogLevel = "Information",
            Message = message,
            Exception = JsonSerializer.Serialize(new { statusResponse, qrMetadata }),
            CreateAtUtc = DateTime.UtcNow
        });
    }

    private async Task PersistJobErrorAsync(int ecfDocumentId, string trackId, Exception ex)
    {
        var ecfDocument = await _ecfDocumentService.GetAsync(ecfDocumentId);
        if (ecfDocument == null) return;

        ecfDocument.EcfStatusId = 12;
        await _ecfDocumentService.UpdateAsync(ecfDocument);

        await _systemLogService.InsertAsync(new SystemLog
        {
            ClientId = ecfDocument.ClientId,
            EcfDocumentId = ecfDocumentId,
            LogLevel = "Error",
            Message = $"Error consultando TrackId {trackId}.",
            Exception = ex.ToString(),
            CreateAtUtc = DateTime.UtcNow
        });
    }
}
