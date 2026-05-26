using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Reports;

namespace ZynstormECFPlatform.Services.Jobs;

public class AutomaticReportsJob
{
    private readonly IRepository<Client> _clientRepository;
    private readonly IRepository<UserClient> _userClientRepository;
    private readonly IRepository<EcfDocument> _ecfDocumentRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<AutomaticReportsJob> _logger;

    public AutomaticReportsJob(
        IRepository<Client> clientRepository,
        IRepository<UserClient> userClientRepository,
        IRepository<EcfDocument> ecfDocumentRepository,
        IEmailService emailService,
        ILogger<AutomaticReportsJob> logger)
    {
        _clientRepository = clientRepository;
        _userClientRepository = userClientRepository;
        _ecfDocumentRepository = ecfDocumentRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteDailyReportAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Automatic Daily Reports Job execution...");
        var now = DateTime.UtcNow;
        var start = now.AddDays(-1); // Last 24 Hours

        var clients = await _clientRepository.Table
            .Where(c => !c.IsDeleted && c.StatusId == 1)
            .ToListAsync(cancellationToken);

        foreach (var client in clients)
        {
            try
            {
                // Find users subscribed to Daily Reports (NotificationTypeId = 3)
                var userClients = await _userClientRepository.Table
                    .Include(uc => uc.User)
                        .ThenInclude(u => u.UserNotificationConfigurations)
                    .Where(uc => uc.ClientId == client.ClientId && !uc.IsDeleted && !uc.User.IsDeleted && uc.User.IsActive)
                    .ToListAsync(cancellationToken);

                var usersToNotify = userClients
                    .Select(uc => uc.User)
                    .Where(u =>
                    {
                        var config = u.UserNotificationConfigurations.FirstOrDefault(c => c.NotificationTypeId == 3);
                        return config == null || config.IsEnabled;
                    })
                    .ToList();

                bool hasExternalEmails = !string.IsNullOrWhiteSpace(client.DailyReportEmails);
                if (!usersToNotify.Any() && !hasExternalEmails)
                {
                    continue;
                }

                // Query client's e-CF documents created in the timeframe
                var documents = await _ecfDocumentRepository.Table
                    .Include(d => d.EcfStatus)
                    .Where(d => d.ClientId == client.ClientId && !d.IsDeleted && d.RegisteredAt >= start && d.RegisteredAt <= now)
                    .ToListAsync(cancellationToken);

                if (!documents.Any())
                {
                    continue;
                }

                await SendDailyReportEmailAsync(client, usersToNotify, documents, start, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Daily Report for ClientId {ClientId}", client.ClientId);
            }
        }

        _logger.LogInformation("Finished Automatic Daily Reports Job execution.");
    }

    public async Task ExecuteWeeklyReportAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Automatic Weekly Reports Job execution...");
        var now = DateTime.UtcNow;
        var start = now.AddDays(-7); // Last 7 Days

        var clients = await _clientRepository.Table
            .Where(c => !c.IsDeleted && c.StatusId == 1)
            .ToListAsync(cancellationToken);

        foreach (var client in clients)
        {
            try
            {
                // Find users subscribed to Weekly Reports (NotificationTypeId = 4)
                var userClients = await _userClientRepository.Table
                    .Include(uc => uc.User)
                        .ThenInclude(u => u.UserNotificationConfigurations)
                    .Where(uc => uc.ClientId == client.ClientId && !uc.IsDeleted && !uc.User.IsDeleted && uc.User.IsActive)
                    .ToListAsync(cancellationToken);

                var usersToNotify = userClients
                    .Select(uc => uc.User)
                    .Where(u =>
                    {
                        var config = u.UserNotificationConfigurations.FirstOrDefault(c => c.NotificationTypeId == 4);
                        return config == null || config.IsEnabled;
                    })
                    .ToList();

                bool hasExternalEmails = !string.IsNullOrWhiteSpace(client.WeeklyReportEmails);
                if (!usersToNotify.Any() && !hasExternalEmails)
                {
                    continue;
                }

                // Query client's e-CF documents created in the timeframe
                var documents = await _ecfDocumentRepository.Table
                    .Include(d => d.EcfStatus)
                    .Where(d => d.ClientId == client.ClientId && !d.IsDeleted && d.RegisteredAt >= start && d.RegisteredAt <= now)
                    .ToListAsync(cancellationToken);

                if (!documents.Any())
                {
                    continue;
                }

                await SendWeeklyReportEmailAsync(client, usersToNotify, documents, start, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Weekly Report for ClientId {ClientId}", client.ClientId);
            }
        }

        _logger.LogInformation("Finished Automatic Weekly Reports Job execution.");
    }

    private async Task SendDailyReportEmailAsync(Client client, List<User> users, List<EcfDocument> documents, DateTime start, DateTime end)
    {
        var rangeStartFormatted = start.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");
        var rangeEndFormatted = end.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");

        int totalCount = documents.Count;
        int acceptedCount = documents.Count(d => d.EcfStatusId == 10);
        int rejectedCount = documents.Count(d => d.EcfStatusId == 11);
        int errorCount = documents.Count(d => d.EcfStatusId == 12);
        int pendingCount = totalCount - acceptedCount - rejectedCount - errorCount;

        // Financial Consolidation (Successful Invoices)
        var acceptedDocs = documents.Where(d => d.EcfStatusId == 10).ToList();
        decimal subTotalAcumulado = acceptedDocs.Sum(d => d.SubTotal);
        decimal itbisAcumulado = acceptedDocs.Sum(d => d.Itbistotal);
        decimal totalAcumulado = acceptedDocs.Sum(d => d.Total);

        // Group by e-CF Type (using standard Dominican descriptions)
        var typeGroups = documents
            .GroupBy(d => d.EcfTypeId)
            .Select(g => new
            {
                TypeId = g.Key,
                Name = GetEcfTypeName(g.Key),
                Count = g.Count(),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Recent Invoices (limit to 5)
        var recentDocs = documents
            .OrderByDescending(d => d.RegisteredAt)
            .Take(5)
            .ToList();

        // Build unified list of recipients (subscribed users + custom distribution list)
        var recipients = new List<(string Email, string Name)>();
        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                recipients.Add((user.Email.Trim(), user.FullName));
            }
        }

        if (!string.IsNullOrWhiteSpace(client.DailyReportEmails))
        {
            var extraEmails = client.DailyReportEmails
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            foreach (var email in extraEmails)
            {
                if (!recipients.Any(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    recipients.Add((email, client.Name));
                }
            }
        }

        if (!recipients.Any()) return;

        // Generate PDF bytes once
        byte[]? pdfBytes = null;
        string pdfFileName = $"Resumen_Diario_{client.Name.Replace(" ", "_")}_{end.ToDrTime():yyyyMMdd}.pdf";
        try
        {
            pdfBytes = ReportPdfGenerator.GenerateDailyReportPdf(client, documents, start, end);
            _logger.LogInformation("Daily Report PDF generated successfully ({Bytes} bytes) for ClientId {ClientId}", pdfBytes.Length, client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Daily Report PDF for ClientId {ClientId}", client.ClientId);
        }

        string subject = $"[Zynstorm ECF] Resumen Diario - {client.Name}";

        foreach (var recipient in recipients)
        {
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
            font-size: 24px;
            font-weight: 700;
            color: #ffffff;
            margin: 0;
        }}
        .logo span {{
            color: #3b82f6;
        }}
        .title-banner {{
            background-color: #f1f5f9;
            padding: 20px;
            text-align: center;
            border-bottom: 1px solid #e2e8f0;
        }}
        .title-banner h2 {{
            font-size: 18px;
            font-weight: 700;
            color: #0f172a;
            margin: 0 0 4px 0;
        }}
        .title-banner p {{
            font-size: 13px;
            color: #64748b;
            margin: 0;
        }}
        .content {{
            padding: 32px;
        }}
        .intro-text {{
            font-size: 15px;
            line-height: 1.6;
            color: #475569;
            margin-top: 0;
            margin-bottom: 28px;
        }}
        .grid-stats {{
            display: table;
            width: 100%;
            table-layout: fixed;
            margin-bottom: 28px;
            border-collapse: separate;
            border-spacing: 8px;
        }}
        .stat-card {{
            display: table-cell;
            background-color: #f8fafc;
            border-radius: 12px;
            padding: 16px;
            text-align: center;
            border: 1px solid #e2e8f0;
            vertical-align: middle;
        }}
        .stat-num {{
            font-size: 24px;
            font-weight: 700;
            color: #0f172a;
            line-height: 1;
            margin-bottom: 6px;
        }}
        .stat-num.accepted {{ color: #10b981; }}
        .stat-num.rejected {{ color: #f59e0b; }}
        .stat-num.error {{ color: #ef4444; }}
        .stat-label {{
            font-size: 11px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: #64748b;
        }}
        .section-title {{
            font-size: 14px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            color: #64748b;
            margin: 28px 0 16px 0;
            border-bottom: 1px solid #e2e8f0;
            padding-bottom: 8px;
        }}
        .financial-card {{
            background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
            border-radius: 12px;
            padding: 24px;
            color: #ffffff;
            margin-bottom: 28px;
        }}
        .fin-row {{
            display: table;
            width: 100%;
            margin-bottom: 10px;
        }}
        .fin-row:last-child {{
            margin-bottom: 0;
            border-top: 1px dashed rgba(255, 255, 255, 0.2);
            margin-top: 10px;
            padding-top: 10px;
        }}
        .fin-cell {{
            display: table-cell;
            font-size: 14px;
        }}
        .fin-cell.label {{
            color: #94a3b8;
        }}
        .fin-cell.value {{
            text-align: right;
            font-weight: 600;
        }}
        .fin-cell.total {{
            font-size: 20px;
            font-weight: 700;
            color: #3b82f6;
            text-align: right;
        }}
        .table-data {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 24px;
        }}
        .table-data th {{
            text-align: left;
            padding: 8px 12px;
            background-color: #f1f5f9;
            font-size: 12px;
            color: #64748b;
            text-transform: uppercase;
            font-weight: 600;
        }}
        .table-data td {{
            padding: 12px;
            border-bottom: 1px solid #f1f5f9;
            font-size: 13px;
        }}
        .badge {{
            display: inline-block;
            padding: 2px 8px;
            border-radius: 4px;
            font-size: 11px;
            font-weight: 600;
        }}
        .badge.success {{ background-color: #ecfdf5; color: #059669; }}
        .badge.warning {{ background-color: #fffbeb; color: #d97706; }}
        .badge.danger {{ background-color: #fef2f2; color: #dc2626; }}
        .badge.neutral {{ background-color: #f1f5f9; color: #475569; }}
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
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <!-- Header -->
            <div class=""header"">
                <h1 class=""logo"">Zynstorm<span>ECF</span></h1>
            </div>

            <!-- Title -->
            <div class=""title-banner"">
                <h2>Resumen Diario de Comprobantes</h2>
                <p>Rango: {rangeStartFormatted} - {rangeEndFormatted}</p>
            </div>

            <!-- Content -->
            <div class=""content"">
                <p class=""intro-text"">
                    Hola <strong>{recipient.Name}</strong>,<br>
                    A continuación te presentamos la consolidación diaria de comprobantes fiscales electrónicos (e-CF) procesados para <strong>{client.Name}</strong>. 
                    Hemos adjuntado una copia de este reporte en formato PDF listo para descargar e imprimir.
                </p>

                <!-- Numeric KPI Stats -->
                <div class=""grid-stats"">
                    <div class=""stat-card"">
                        <div class=""stat-num"">{totalCount}</div>
                        <div class=""stat-label"">Emitidos</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num accepted"">{acceptedCount}</div>
                        <div class=""stat-label"">Aceptados</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num rejected"">{rejectedCount}</div>
                        <div class=""stat-label"">Rechazados</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num error"">{errorCount}</div>
                        <div class=""stat-label"">Con Error</div>
                    </div>
                </div>

                <!-- Financial Card -->
                <h3 class=""section-title"">Consolidado Económico (Aceptados)</h3>
                <div class=""financial-card"">
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">Subtotal Acumulado</div>
                        <div class=""fin-cell value"">RD$ {subTotalAcumulado.ToString("N2")}</div>
                    </div>
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">ITBIS Acumulado</div>
                        <div class=""fin-cell value"">RD$ {itbisAcumulado.ToString("N2")}</div>
                    </div>
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">Monto Total Aceptado</div>
                        <div class=""fin-cell total"">RD$ {totalAcumulado.ToString("N2")}</div>
                    </div>
                </div>

                <!-- Breakdown by e-CF Type -->
                <h3 class=""section-title"">Distribución por Tipo de Comprobante</h3>
                <table class=""table-data"">
                    <thead>
                        <tr>
                            <th>Tipo de e-CF</th>
                            <th style=""text-align: center;"">Cant.</th>
                            <th style=""text-align: right;"">Monto Total</th>
                        </tr>
                    </thead>
                    <tbody>";

            if (!typeGroups.Any())
            {
                htmlBody += @"
                        <tr>
                            <td colspan=""3"" style=""text-align: center; color: #64748b; padding: 20px;"">Sin actividad registrada en este período.</td>
                        </tr>";
            }
            else
            {
                foreach (var group in typeGroups)
                {
                    htmlBody += $@"
                        <tr>
                            <td style=""font-weight: 500;"">{group.Name} ({group.TypeId})</td>
                            <td style=""text-align: center; font-weight: 600;"">{group.Count}</td>
                            <td style=""text-align: right; font-weight: 600; color: #0f172a;"">RD$ {group.Total.ToString("N2")}</td>
                        </tr>";
                }
            }

            htmlBody += @"
                    </tbody>
                </table>

                <!-- Recent Documents List -->
                <h3 class=""section-title"">Últimos Documentos Procesados</h3>
                <table class=""table-data"">
                    <thead>
                        <tr>
                            <th>e-NCF / Secuencia</th>
                            <th>Receptor</th>
                            <th style=""text-align: right;"">Total</th>
                            <th style=""text-align: right;"">Estado</th>
                        </tr>
                    </thead>
                    <tbody>";

            if (!recentDocs.Any())
            {
                htmlBody += @"
                        <tr>
                            <td colspan=""4"" style=""text-align: center; color: #64748b; padding: 20px;"">Sin actividad registrada en este período.</td>
                        </tr>";
            }
            else
            {
                foreach (var doc in recentDocs)
                {
                    string statusBadgeClass = doc.EcfStatusId switch
                    {
                        10 => "success",
                        11 => "warning",
                        12 => "danger",
                        _ => "neutral"
                    };
                    string statusText = doc.EcfStatus?.Name ?? "Pendiente";

                    htmlBody += $@"
                        <tr>
                            <td style=""font-family: monospace; font-weight: 600;"">{doc.Ncf}</td>
                            <td style=""color: #475569;"">{doc.CustomerName}</td>
                            <td style=""text-align: right; font-weight: 600;"">RD$ {doc.Total.ToString("N2")}</td>
                            <td style=""text-align: right;""><span class=""badge {statusBadgeClass}"">{statusText}</span></td>
                        </tr>";
                }
            }

            htmlBody += $@"
                    </tbody>
                </table>

                <!-- Action Button -->
                <a href=""https://zynstorm-ecf.com"" class=""action-button"">Ir al Panel de Control</a>
            </div>

            <!-- Footer -->
            <div class=""footer"">
                <p>Este es un reporte automático configurado. Puedes modificar las preferencias en el panel de configuraciones.</p>
                <p>&copy; {DateTime.UtcNow.Year} Zynstorm ECF Platform. Todos los derechos reservados.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            try
            {
                await _emailService.SendEmailAsync(recipient.Email, subject, htmlBody, pdfBytes, pdfFileName);
                _logger.LogInformation("Daily Report sent successfully to {Email} for ClientId {ClientId}", recipient.Email, client.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Daily Report to {Email} for ClientId {ClientId}", recipient.Email, client.ClientId);
            }
        }
    }

    private async Task SendWeeklyReportEmailAsync(Client client, List<User> users, List<EcfDocument> documents, DateTime start, DateTime end)
    {
        var rangeStartFormatted = start.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");
        var rangeEndFormatted = end.ToDrTime().ToString("dd/MM/yyyy hh:mm tt");

        int totalCount = documents.Count;
        int acceptedCount = documents.Count(d => d.EcfStatusId == 10);
        int rejectedCount = documents.Count(d => d.EcfStatusId == 11);
        int errorCount = documents.Count(d => d.EcfStatusId == 12);
        int pendingCount = totalCount - acceptedCount - rejectedCount - errorCount;

        // Financial Aggregates
        var acceptedDocs = documents.Where(d => d.EcfStatusId == 10).ToList();
        decimal subTotalAcumulado = acceptedDocs.Sum(d => d.SubTotal);
        decimal itbisAcumulado = acceptedDocs.Sum(d => d.Itbistotal);
        decimal totalAcumulado = acceptedDocs.Sum(d => d.Total);

        // Group by e-CF Type
        var typeGroups = documents
            .GroupBy(d => d.EcfTypeId)
            .Select(g => new
            {
                TypeId = g.Key,
                Name = GetEcfTypeName(g.Key),
                Count = g.Count(),
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Top 3 Buyers (by total purchase)
        var topBuyers = acceptedDocs
            .GroupBy(d => new { d.CustomerRnc, d.CustomerName })
            .Select(g => new
            {
                Name = g.Key.CustomerName,
                Rnc = g.Key.CustomerRnc,
                Total = g.Sum(d => d.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(3)
            .ToList();

        // Build unified list of recipients (subscribed users + custom distribution list)
        var recipients = new List<(string Email, string Name)>();
        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                recipients.Add((user.Email.Trim(), user.FullName));
            }
        }

        if (!string.IsNullOrWhiteSpace(client.WeeklyReportEmails))
        {
            var extraEmails = client.WeeklyReportEmails
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim())
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();

            foreach (var email in extraEmails)
            {
                if (!recipients.Any(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    recipients.Add((email, client.Name));
                }
            }
        }

        if (!recipients.Any()) return;

        // Generate PDF bytes once
        byte[]? pdfBytes = null;
        string pdfFileName = $"Reporte_Semanal_{client.Name.Replace(" ", "_")}_{end.ToDrTime():yyyyMMdd}.pdf";
        try
        {
            pdfBytes = ReportPdfGenerator.GenerateWeeklyReportPdf(client, documents, start, end);
            _logger.LogInformation("Weekly Report PDF generated successfully ({Bytes} bytes) for ClientId {ClientId}", pdfBytes.Length, client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate Weekly Report PDF for ClientId {ClientId}", client.ClientId);
        }

        string subject = $"[Zynstorm ECF] Reporte Semanal - {client.Name}";

        foreach (var recipient in recipients)
        {
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
            background: linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%);
            padding: 32px;
            text-align: center;
        }}
        .logo {{
            font-size: 24px;
            font-weight: 700;
            color: #ffffff;
            margin: 0;
        }}
        .logo span {{
            color: #60a5fa;
        }}
        .title-banner {{
            background-color: #f1f5f9;
            padding: 20px;
            text-align: center;
            border-bottom: 1px solid #e2e8f0;
        }}
        .title-banner h2 {{
            font-size: 18px;
            font-weight: 700;
            color: #1e3a8a;
            margin: 0 0 4px 0;
        }}
        .title-banner p {{
            font-size: 13px;
            color: #64748b;
            margin: 0;
        }}
        .content {{
            padding: 32px;
        }}
        .intro-text {{
            font-size: 15px;
            line-height: 1.6;
            color: #475569;
            margin-top: 0;
            margin-bottom: 28px;
        }}
        .grid-stats {{
            display: table;
            width: 100%;
            table-layout: fixed;
            margin-bottom: 28px;
            border-collapse: separate;
            border-spacing: 8px;
        }}
        .stat-card {{
            display: table-cell;
            background-color: #f8fafc;
            border-radius: 12px;
            padding: 16px;
            text-align: center;
            border: 1px solid #e2e8f0;
            vertical-align: middle;
        }}
        .stat-num {{
            font-size: 24px;
            font-weight: 700;
            color: #0f172a;
            line-height: 1;
            margin-bottom: 6px;
        }}
        .stat-num.accepted {{ color: #10b981; }}
        .stat-num.rejected {{ color: #f59e0b; }}
        .stat-num.error {{ color: #ef4444; }}
        .stat-label {{
            font-size: 11px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            color: #64748b;
        }}
        .section-title {{
            font-size: 14px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.05em;
            color: #64748b;
            margin: 28px 0 16px 0;
            border-bottom: 1px solid #e2e8f0;
            padding-bottom: 8px;
        }}
        .financial-card {{
            background: linear-gradient(135deg, #1e3a8a 0%, #0f172a 100%);
            border-radius: 12px;
            padding: 24px;
            color: #ffffff;
            margin-bottom: 28px;
        }}
        .fin-row {{
            display: table;
            width: 100%;
            margin-bottom: 10px;
        }}
        .fin-row:last-child {{
            margin-bottom: 0;
            border-top: 1px dashed rgba(255, 255, 255, 0.2);
            margin-top: 10px;
            padding-top: 10px;
        }}
        .fin-cell {{
            display: table-cell;
            font-size: 14px;
        }}
        .fin-cell.label {{
            color: #93c5fd;
        }}
        .fin-cell.value {{
            text-align: right;
            font-weight: 600;
        }}
        .fin-cell.total {{
            font-size: 20px;
            font-weight: 700;
            color: #60a5fa;
            text-align: right;
        }}
        .table-data {{
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 24px;
        }}
        .table-data th {{
            text-align: left;
            padding: 8px 12px;
            background-color: #f1f5f9;
            font-size: 12px;
            color: #64748b;
            text-transform: uppercase;
            font-weight: 600;
        }}
        .table-data td {{
            padding: 12px;
            border-bottom: 1px solid #f1f5f9;
            font-size: 13px;
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
    </style>
</head>
<body>
    <div class=""wrapper"">
        <div class=""container"">
            <!-- Header -->
            <div class=""header"">
                <h1 class=""logo"">Zynstorm<span>ECF</span></h1>
            </div>

            <!-- Title -->
            <div class=""title-banner"">
                <h2>Reporte Semanal Ejecutivo</h2>
                <p>Período: {rangeStartFormatted} - {rangeEndFormatted}</p>
            </div>

            <!-- Content -->
            <div class=""content"">
                <p class=""intro-text"">
                    Hola <strong>{recipient.Name}</strong>,<br>
                    Te compartimos el reporte semanal ejecutivo con la consolidación y estadísticas de facturación de <strong>{client.Name}</strong> correspondiente a los últimos 7 días.
                    Hemos adjuntado una copia de este reporte en formato PDF listo para descargar e imprimir.
                </p>

                <!-- Numeric KPI Stats -->
                <div class=""grid-stats"">
                    <div class=""stat-card"">
                        <div class=""stat-num"">{totalCount}</div>
                        <div class=""stat-label"">Emitidos</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num accepted"">{acceptedCount}</div>
                        <div class=""stat-label"">Aceptados</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num rejected"">{rejectedCount}</div>
                        <div class=""stat-label"">Rechazados</div>
                    </div>
                    <div class=""stat-card"">
                        <div class=""stat-num error"">{errorCount}</div>
                        <div class=""stat-label"">Con Error</div>
                    </div>
                </div>

                <!-- Financial Card -->
                <h3 class=""section-title"">Consolidado Financiero Semanal (Aceptados)</h3>
                <div class=""financial-card"">
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">Subtotal Semanal</div>
                        <div class=""fin-cell value"">RD$ {subTotalAcumulado.ToString("N2")}</div>
                    </div>
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">ITBIS Semanal</div>
                        <div class=""fin-cell value"">RD$ {itbisAcumulado.ToString("N2")}</div>
                    </div>
                    <div class=""fin-row"">
                        <div class=""fin-cell label"">Monto Semanal Facturado</div>
                        <div class=""fin-cell total"">RD$ {totalAcumulado.ToString("N2")}</div>
                    </div>
                </div>

                <!-- Breakdown by e-CF Type -->
                <h3 class=""section-title"">Actividad por Tipo de Comprobante</h3>
                <table class=""table-data"">
                    <thead>
                        <tr>
                            <th>Tipo de e-CF</th>
                            <th style=""text-align: center;"">Emitidos</th>
                            <th style=""text-align: right;"">Monto Total</th>
                        </tr>
                    </thead>
                    <tbody>";

            if (!typeGroups.Any())
            {
                htmlBody += @"
                        <tr>
                            <td colspan=""3"" style=""text-align: center; color: #64748b; padding: 20px;"">Sin actividad registrada en este período.</td>
                        </tr>";
            }
            else
            {
                foreach (var group in typeGroups)
                {
                    htmlBody += $@"
                        <tr>
                            <td style=""font-weight: 500;"">{group.Name} ({group.TypeId})</td>
                            <td style=""text-align: center; font-weight: 600;"">{group.Count}</td>
                            <td style=""text-align: right; font-weight: 600; color: #0f172a;"">RD$ {group.Total.ToString("N2")}</td>
                        </tr>";
                }
            }

            htmlBody += @"
                    </tbody>
                </table>

                <!-- Top Customers Weekly -->
                <h3 class=""section-title"">Top 3 Compradores de la Semana</h3>
                <table class=""table-data"">
                    <thead>
                        <tr>
                            <th>Razón Social</th>
                            <th>RNC</th>
                            <th style=""text-align: right;"">Total Facturado</th>
                        </tr>
                    </thead>
                    <tbody>";

            if (!topBuyers.Any())
            {
                htmlBody += @"
                        <tr>
                            <td colspan=""3"" style=""text-align: center; color: #64748b; padding: 20px;"">Sin cobros/facturas aceptadas en esta semana.</td>
                        </tr>";
            }
            else
            {
                foreach (var buyer in topBuyers)
                {
                    htmlBody += $@"
                        <tr>
                            <td style=""font-weight: 500;"">{buyer.Name}</td>
                            <td style=""color: #475569; font-family: monospace;"">{buyer.Rnc}</td>
                            <td style=""text-align: right; font-weight: 700; color: #1e3a8a;"">RD$ {buyer.Total.ToString("N2")}</td>
                        </tr>";
                }
            }

            htmlBody += $@"
                    </tbody>
                </table>

                <!-- Action Button -->
                <a href=""https://zynstorm-ecf.com"" class=""action-button"">Ver Estadísticas en la Plataforma</a>
            </div>

            <!-- Footer -->
            <div class=""footer"">
                <p>Este es un reporte semanal automático configurado. Puedes modificar las preferencias en el panel de configuraciones.</p>
                <p>&copy; {DateTime.UtcNow.Year} Zynstorm ECF Platform. Todos los derechos reservados.</p>
            </div>
        </div>
    </div>
</body>
</html>";

            try
            {
                await _emailService.SendEmailAsync(recipient.Email, subject, htmlBody, pdfBytes, pdfFileName);
                _logger.LogInformation("Weekly Report sent successfully to {Email} for ClientId {ClientId}", recipient.Email, client.ClientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Weekly Report to {Email} for ClientId {ClientId}", recipient.Email, client.ClientId);
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
