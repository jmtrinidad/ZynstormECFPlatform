using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
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
    private readonly IEmailService _emailService;
    private readonly IRepository<UserClient> _userClientRepository;
    private readonly IClientService _clientService;
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
        IEmailService emailService,
        IRepository<UserClient> userClientRepository,
        IClientService clientService,
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
        _emailService = emailService;
        _userClientRepository = userClientRepository;
        _clientService = clientService;
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

        // Dispatch notifications if status is final (Aceptado = 10, Rechazado = 11, Error = 12)
        if (statusId == 10 || statusId == 11 || statusId == 12)
        {
            try
            {
                await DispatchEcfEmailNotificationsAsync(ecfDocument, statusId, trackId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching email notifications for EcfDocumentId {EcfDocumentId}", ecfDocumentId);
            }
        }
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

        // Dispatch notifications for Error status
        try
        {
            await DispatchEcfEmailNotificationsAsync(ecfDocument, 12, trackId);
        }
        catch (Exception mailEx)
        {
            _logger.LogError(mailEx, "Error dispatching email notifications on job error for EcfDocumentId {EcfDocumentId}", ecfDocumentId);
        }
    }

    private async Task DispatchEcfEmailNotificationsAsync(EcfDocument ecfDocument, int statusId, string trackId)
    {
        var client = await _clientService.GetAsync(ecfDocument.ClientId);
        string clientName = client?.Name ?? "Zynstorm ECF Client";

        // Query all active and non-deleted users linked to this Client
        var userClients = await _userClientRepository.Table
            .Include(uc => uc.User)
                .ThenInclude(u => u.UserNotificationConfigurations)
            .Where(uc => uc.ClientId == ecfDocument.ClientId && !uc.IsDeleted && !uc.User.IsDeleted && uc.User.IsActive)
            .ToListAsync();

        if (userClients == null || !userClients.Any())
        {
            _logger.LogInformation("No active users found for ClientId {ClientId} to notify.", ecfDocument.ClientId);
            return;
        }

        int targetNotificationTypeId = statusId == 10 ? 1 : 2;

        var usersToNotify = userClients
            .Select(uc => uc.User)
            .Where(u =>
            {
                var config = u.UserNotificationConfigurations.FirstOrDefault(c => c.NotificationTypeId == targetNotificationTypeId);
                return config == null || config.IsEnabled;
            })
            .ToList();

        if (!usersToNotify.Any())
        {
            _logger.LogInformation("No users have enabled email notifications (NotificationTypeId {TypeId}) for ClientId {ClientId}.", targetNotificationTypeId, ecfDocument.ClientId);
            return;
        }

        string ecfTypeDescription = GetEcfTypeName(ecfDocument.EcfTypeId);
        string totalFormatted = ecfDocument.Total.ToString("N2");
        string subTotalFormatted = ecfDocument.SubTotal.ToString("N2");
        string itbisTotalFormatted = ecfDocument.Itbistotal.ToString("N2");
        string issueDateFormatted = ecfDocument.IssueDateUtc.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");
        string responseDateFormatted = DateTime.UtcNow.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");

        string statusText = statusId switch
        {
            10 => "Aceptada",
            11 => "Rechazada",
            12 => "Con Error",
            _ => "Procesada"
        };

        string subject = $"e-CF {statusText}: {ecfDocument.Ncf} - {clientName}";

        string bannerGradient = statusId == 10
            ? "linear-gradient(135deg, #10b981 0%, #059669 100%)" // Emerald green
            : "linear-gradient(135deg, #f43f5e 0%, #e11d48 100%)"; // Rose red

        string totalColor = statusId == 10 ? "#059669" : "#e11d48";

        string statusBadge = statusId switch
        {
            10 => "Aceptada por DGII",
            11 => "Rechazada por DGII",
            _ => "Error de Procesamiento"
        };

        string statusTitle = statusId switch
        {
            10 => "Factura Electrónica Aceptada",
            11 => "Factura Electrónica Rechazada",
            _ => "Error en Factura Electrónica"
        };

        int currentYear = DateTime.UtcNow.Year;

        foreach (var user in usersToNotify)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;

            string htmlBody = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{subject}</title>
    <style>
        body {{
            font-family: 'Inter', system-ui, -apple-system, sans-serif;
            background-color: #f8fafc;
            color: #1e293b;
            margin: 0;
            padding: 0;
            -webkit-font-smoothing: antialiased;
        }}
        .wrapper {{
            width: 100%;
            background-color: #f8fafc;
            padding: 40px 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 16px;
            box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.05), 0 8px 10px -6px rgba(0, 0, 0, 0.05);
            overflow: hidden;
            border: 1px solid #e2e8f0;
        }}
        .header {{
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
            padding: 32px;
            text-align: center;
        }}
        .logo {{
            font-family: 'Outfit', sans-serif;
            font-size: 24px;
            font-weight: 700;
            color: #ffffff;
            letter-spacing: -0.5px;
            margin: 0;
        }}
        .logo span {{
            color: #3b82f6;
        }}
        .status-banner {{
            background: {bannerGradient};
            padding: 24px;
            text-align: center;
            color: #ffffff;
        }}
        .status-badge {{
            display: inline-block;
            background-color: rgba(255, 255, 255, 0.15);
            padding: 6px 16px;
            border-radius: 9999px;
            font-size: 13px;
            font-weight: 600;
            letter-spacing: 0.5px;
            text-transform: uppercase;
            border: 1px solid rgba(255, 255, 255, 0.25);
            margin-bottom: 8px;
        }}
        .status-title {{
            font-size: 20px;
            font-weight: 700;
            margin: 0 0 4px 0;
        }}
        .status-subtitle {{
            font-size: 13px;
            margin: 0;
            opacity: 0.9;
        }}
        .content {{
            padding: 40px 32px;
        }}
        .intro-text {{
            font-size: 15px;
            line-height: 1.6;
            color: #475569;
            margin-top: 0;
            margin-bottom: 32px;
        }}
        .section-title {{
            font-size: 14px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            color: #64748b;
            margin: 0 0 16px 0;
            border-bottom: 1px solid #e2e8f0;
            padding-bottom: 8px;
        }}
        .info-table {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 32px;
        }}
        .info-table td {{
            padding: 12px 0;
            border-bottom: 1px solid #f1f5f9;
            font-size: 14px;
        }}
        .info-table td.label {{
            color: #64748b;
            font-weight: 500;
            width: 35%;
        }}
        .info-table td.value {{
            color: #0f172a;
            font-weight: 600;
            text-align: right;
        }}
        .financial-card {{
            background-color: #f8fafc;
            border-radius: 12px;
            padding: 24px;
            border: 1px solid #e2e8f0;
            margin-bottom: 32px;
        }}
        .financial-row {{
            display: table;
            width: 100%;
            margin-bottom: 12px;
        }}
        .financial-row:last-child {{
            margin-bottom: 0;
            border-top: 2px dashed #cbd5e1;
            margin-top: 12px;
            padding-top: 12px;
        }}
        .financial-cell {{
            display: table-cell;
            font-size: 14px;
        }}
        .financial-cell.label {{
            color: #64748b;
        }}
        .financial-cell.value {{
            text-align: right;
            font-weight: 600;
            color: #0f172a;
        }}
        .financial-cell.total-value {{
            font-size: 18px;
            font-weight: 700;
            color: {totalColor};
            text-align: right;
        }}
        .action-button {{
            display: block;
            text-align: center;
            background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
            color: #ffffff !important;
            text-decoration: none;
            padding: 16px 24px;
            border-radius: 8px;
            font-weight: 600;
            font-size: 15px;
            margin: 32px 0 0 0;
            box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2);
        }}
        .footer {{
            padding: 32px;
            text-align: center;
            background-color: #f8fafc;
            border-top: 1px solid #e2e8f0;
        }}
        .footer p {{
            font-size: 12px;
            color: #64748b;
            line-height: 1.5;
            margin: 0 0 8px 0;
        }}
        .footer p:last-child {{
            margin-bottom: 0;
        }}
        .footer a {{
            color: #2563eb;
            text-decoration: none;
            font-weight: 500;
        }}
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <!-- Header -->
            <div class=""header"">
                <h1 class=""logo"">Zynstorm<span>ECF</span></h1>
            </div>

            <!-- Status Banner -->
            <div class=""status-banner"">
                <span class=""status-badge"">{statusBadge}</span>
                <h2 class=""status-title"">{statusTitle}</h2>
                <p class=""status-subtitle"">ID de Seguimiento (TrackId): {trackId}</p>
            </div>

            <!-- Content -->
            <div class=""content"">
                <p class=""intro-text"">
                    Hola <strong>{user.FullName}</strong>,<br>
                    Te notificamos que la factura electrónica emitida a nombre de <strong>{clientName}</strong> ha sido procesada por la Dirección General de Impuestos Internos (DGII) con el siguiente resultado:
                </p>

                <!-- Document Info -->
                <h3 class=""section-title"">Detalles del Documento</h3>
                <table class=""info-table"">
                    <tr>
                        <td class=""label"">e-NCF / Secuencia</td>
                        <td class=""value"">{ecfDocument.Ncf}</td>
                    </tr>
                    <tr>
                        <td class=""label"">Tipo de e-CF</td>
                        <td class=""value"">{ecfTypeDescription} ({ecfDocument.EcfTypeId})</td>
                    </tr>
                    <tr>
                        <td class=""label"">Comprador</td>
                        <td class=""value"">{ecfDocument.CustomerName}</td>
                    </tr>
                    <tr>
                        <td class=""label"">RNC Comprador</td>
                        <td class=""value"">{ecfDocument.CustomerRnc}</td>
                    </tr>
                    <tr>
                        <td class=""label"">Fecha de Emisión</td>
                        <td class=""value"">{issueDateFormatted}</td>
                    </tr>
                    <tr>
                        <td class=""label"">Fecha de Respuesta</td>
                        <td class=""value"">{responseDateFormatted}</td>
                    </tr>
                </table>

                <!-- Financial Info -->
                <h3 class=""section-title"">Resumen Financiero</h3>
                <div class=""financial-card"">
                    <div class=""financial-row"">
                        <div class=""financial-cell label"">Subtotal</div>
                        <div class=""financial-cell value"">RD$ {subTotalFormatted}</div>
                    </div>
                    <div class=""financial-row"">
                        <div class=""financial-cell label"">ITBIS Total</div>
                        <div class=""financial-cell value"">RD$ {itbisTotalFormatted}</div>
                    </div>
                    <div class=""financial-row"">
                        <div class=""financial-cell label"">Monto Total</div>
                        <div class=""financial-cell total-value"">RD$ {totalFormatted}</div>
                    </div>
                </div>

                <!-- Call to Action -->
                <a href=""https://zynstorm-ecf.com"" class=""action-button"">Ir a la Plataforma</a>
            </div>

            <!-- Footer -->
            <div class=""footer"">
                <p>Este es un correo automático del sistema. Por favor no respondas a este mensaje.</p>
                <p>&copy; {currentYear} Zynstorm ECF Platform. Todos los derechos reservados.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
                _logger.LogInformation("Email notification sent successfully to {Email} for EcfDocumentId {EcfDocumentId}", user.Email, ecfDocument.EcfDocumentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification to {Email} for EcfDocumentId {EcfDocumentId}", user.Email, ecfDocument.EcfDocumentId);
            }
        }
    }

    private static string GetEcfTypeName(int ecfTypeId)
    {
        return ecfTypeId switch
        {
            31 => "Factura de Crédito Fiscal Electrónica",
            32 => "Factura de Consumo Electrónica",
            33 => "Nota de Débito Electrónica",
            34 => "Nota de Crédito Electrónica",
            41 => "Compras Electrónicas",
            43 => "Gastos Menores Electrónicos",
            44 => "Regímenes Especiales Electrónica",
            45 => "Gubernamental Electrónica",
            _ => "Comprobante Fiscal Electrónico"
        };
    }
}
