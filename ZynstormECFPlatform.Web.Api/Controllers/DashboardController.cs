using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Common;
using System.Security.Claims;
using Asp.Versioning;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class DashboardController(
        IClientService clientService,
        IEcfDocumentService ecfDocumentService,
        ICertificationProcessService certificationProcessService,
        ILogger<DashboardController> logger) : ControllerBase
    {
        private readonly IClientService _clientService = clientService;
        private readonly IEcfDocumentService _ecfDocumentService = ecfDocumentService;
        private readonly ICertificationProcessService _certificationProcessService = certificationProcessService;
        private readonly ILogger<DashboardController> _logger = logger;

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");

        [HttpGet]
        public async Task<ActionResult<DashboardSummaryDto>> Get(CancellationToken cancellationToken = default)
        {
            try
            {
                var userId = CurrentUserId;

                // 1. Base queries with security role boundaries
                var clientsQuery = _clientService.Table.AsNoTracking().Where(c => !c.IsDeleted);
                if (!IsSA)
                {
                    clientsQuery = clientsQuery.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var ecfQuery = _ecfDocumentService.Table.AsNoTracking().Where(e => !e.IsDeleted);
                if (!IsSA)
                {
                    ecfQuery = ecfQuery.Where(e => e.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                var processQuery = _certificationProcessService.Table.AsNoTracking().Where(cp => !cp.IsDeleted);
                if (!IsSA)
                {
                    processQuery = processQuery.Where(cp => cp.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                // 2. Count metrics
                var activeClientsCount = await clientsQuery.CountAsync(cancellationToken);
                var sentInvoicesCount = await ecfQuery.CountAsync(cancellationToken);
                var certifiedClientsCount = await clientsQuery.CountAsync(c => c.IsCertified, cancellationToken);
                var inProcessClientsCount = await clientsQuery.CountAsync(c => 
                    !c.IsCertified && 
                    c.CertificationProcesses.Any(cp => cp.Status == CertificationStatus.InProgress || cp.Status == CertificationStatus.Pending), 
                    cancellationToken);

                // 3. Calculate dynamic trends (based on 30-day window)
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                
                var newClientsCount = await clientsQuery.Where(c => c.RegisteredAt >= thirtyDaysAgo).CountAsync(cancellationToken);
                double clientTrendVal = activeClientsCount > 0 ? ((double)newClientsCount * 100.0) / activeClientsCount : 0.0;
                string activeClientsTrend = $"+{clientTrendVal:0}% este mes";

                var newInvoicesCount = await ecfQuery.Where(e => e.RegisteredAt >= thirtyDaysAgo).CountAsync(cancellationToken);
                double invoiceTrendVal = sentInvoicesCount > 0 ? ((double)newInvoicesCount * 100.0) / sentInvoicesCount : 0.0;
                string sentInvoicesTrend = $"+{invoiceTrendVal:0}% este mes";

                double certifiedTrendVal = activeClientsCount > 0 ? ((double)certifiedClientsCount * 100.0) / activeClientsCount : 0.0;
                string certifiedClientsTrend = $"{certifiedTrendVal:0}% del total";

                double inProcessTrendVal = activeClientsCount > 0 ? ((double)inProcessClientsCount * 100.0) / activeClientsCount : 0.0;
                string inProcessClientsTrend = $"{inProcessTrendVal:0}% del total";

                // 4. Fetch recent entries for unified feed (take 5 of each first to optimize)
                var recentDocs = await ecfQuery
                    .Include(e => e.Client)
                    .Include(e => e.EcfStatus)
                    .OrderByDescending(e => e.RegisteredAt)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                var recentClients = await clientsQuery
                    .OrderByDescending(c => c.RegisteredAt)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                var recentProcesses = await processQuery
                    .Include(cp => cp.Client)
                    .Include(cp => cp.CertificationStep)
                    .OrderByDescending(cp => cp.RegisteredAt)
                    .Take(5)
                    .ToListAsync(cancellationToken);

                // 5. Merge and format activities in memory
                var allActivities = new List<(DateTime RegisteredAt, DashboardActivityDto Dto)>();

                foreach (var doc in recentDocs)
                {
                    allActivities.Add((doc.RegisteredAt, new DashboardActivityDto
                    {
                        ClientName = doc.Client?.Name ?? "Cliente Desconocido",
                        Action = "Factura enviada",
                        Status = doc.EcfStatus?.Name ?? "Pendiente",
                        Time = GetRelativeTimeSpan(doc.RegisteredAt)
                    }));
                }

                foreach (var client in recentClients)
                {
                    allActivities.Add((client.RegisteredAt, new DashboardActivityDto
                    {
                        ClientName = client.Name,
                        Action = "Nuevo cliente",
                        Status = "Registrado",
                        Time = GetRelativeTimeSpan(client.RegisteredAt)
                    }));
                }

                foreach (var cp in recentProcesses)
                {
                    string statusStr = "Pendiente";
                    if (cp.Status == CertificationStatus.Approved)
                    {
                        statusStr = "Aprobada";
                    }
                    else if (cp.CertificationStep != null)
                    {
                        statusStr = cp.CertificationStep.Name;
                    }
                    else if (cp.CurrentStepId.HasValue)
                    {
                        statusStr = $"Paso {cp.CurrentStepId.Value}";
                    }

                    allActivities.Add((cp.RegisteredAt, new DashboardActivityDto
                    {
                        ClientName = cp.Client?.Name ?? "Cliente Desconocido",
                        Action = cp.Status == CertificationStatus.Approved ? "Certificación completada" : "Certificación en proceso",
                        Status = statusStr,
                        Time = GetRelativeTimeSpan(cp.RegisteredAt)
                    }));
                }

                var top5Activities = allActivities
                    .OrderByDescending(x => x.RegisteredAt)
                    .Take(5)
                    .Select(x => x.Dto)
                    .ToList();

                var response = new DashboardSummaryDto
                {
                    ActiveClientsCount = activeClientsCount,
                    ActiveClientsTrend = activeClientsTrend,
                    SentInvoicesCount = sentInvoicesCount,
                    SentInvoicesTrend = sentInvoicesTrend,
                    CertifiedClientsCount = certifiedClientsCount,
                    CertifiedClientsTrend = certifiedClientsTrend,
                    InProcessClientsCount = inProcessClientsCount,
                    InProcessClientsTrend = inProcessClientsTrend,
                    RecentActivities = top5Activities
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard summary data.");
                return StatusCode(500, "Internal server error occurred while retrieving dashboard summary.");
            }
        }

        private string GetRelativeTimeSpan(DateTime registeredAtUtc)
        {
            var dateDr = registeredAtUtc.ToDrTime();
            var nowDr = DateTimeExtensions.DrNow;
            var difference = nowDr - dateDr;

            if (difference.TotalSeconds < 0)
            {
                return "Ahora mismo";
            }
            if (difference.TotalMinutes < 1)
            {
                return "Hace unos instantes";
            }
            if (difference.TotalMinutes < 60)
            {
                var minutes = (int)difference.TotalMinutes;
                return $"Hace {minutes} min";
            }
            if (difference.TotalHours < 24)
            {
                var hours = (int)difference.TotalHours;
                return $"Hace {hours} hora{(hours > 1 ? "s" : "")}";
            }
            if (difference.TotalDays < 2)
            {
                return "Ayer";
            }
            if (difference.TotalDays < 7)
            {
                var days = (int)difference.TotalDays;
                return $"Hace {days} día{(days > 1 ? "s" : "")}";
            }

            return dateDr.ToString("dd/MM/yyyy hh:mm tt");
        }
    }
}
