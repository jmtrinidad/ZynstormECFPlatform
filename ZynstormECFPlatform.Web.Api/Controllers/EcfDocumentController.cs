using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using System.Security.Claims;
using Asp.Versioning;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [Authorize]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class EcfDocumentController(
        IEcfDocumentService ecfDocumentService,
        ILogger<EcfDocumentController> logger) : ControllerBase
    {
        private readonly IEcfDocumentService _ecfDocumentService = ecfDocumentService;
        private readonly ILogger<EcfDocumentController> _logger = logger;

        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? type,
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _ecfDocumentService.Table
                    .AsNoTracking()
                    .Include(e => e.EcfStatus)
                    .Include(e => e.EcfType)
                    .Include(e => e.EcfTransmissions)
                    .AsQueryable();

                // Filtrado por usuario / propiedad
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(e => e.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                // Búsqueda por NCF, Nombre o RNC del cliente (comprador)
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower().Trim();
                    query = query.Where(e =>
                        e.Ncf.ToLower().Contains(searchLower) ||
                        e.CustomerName.ToLower().Contains(searchLower) ||
                        e.CustomerRnc.Contains(searchLower)
                    );
                }

                // Filtrar por Estado
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Equals("accepted", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfStatus.Name == "Accepted");
                    }
                    else if (status.Equals("rejected", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfStatus.Name == "Rejected" || e.EcfStatus.Name == "ValidationFailed" || e.EcfStatus.Name == "Error");
                    }
                    else if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfStatus.Name == "SendPending" || e.EcfStatus.Name == "Sending" || e.EcfStatus.Name == "Sent");
                    }
                    else if (status.Equals("processing", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfStatus.Name != "Accepted" &&
                                                 e.EcfStatus.Name != "Rejected" &&
                                                 e.EcfStatus.Name != "ValidationFailed" &&
                                                 e.EcfStatus.Name != "Error" &&
                                                 e.EcfStatus.Name != "SendPending" &&
                                                 e.EcfStatus.Name != "Sending" &&
                                                 e.EcfStatus.Name != "Sent");
                    }
                }

                // Filtrar por Tipo (FE, FC, NC, ND)
                if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    if (type.Equals("FE", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfType.Code == "31");
                    }
                    else if (type.Equals("FC", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfType.Code == "32");
                    }
                    else if (type.Equals("ND", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfType.Code == "33");
                    }
                    else if (type.Equals("NC", StringComparison.OrdinalIgnoreCase))
                    {
                        query = query.Where(e => e.EcfType.Code == "34");
                    }
                }

                // Ordenar por fecha de emisión descendente para mostrar las más recientes primero
                query = query.OrderByDescending(e => e.IssueDateUtc);

                var totalCount = await query.CountAsync(cancellationToken);

                // Proyectar datos optimizados a tipo anónimo (sin XML para excelente rendimiento)
                var tempProjection = query.Select(e => new
                {
                    Id = e.GuidId,
                    Ncf = e.Ncf,
                    ClientName = e.CustomerName,
                    ClientRnc = e.CustomerRnc,
                    Type = e.EcfType.Code == "31" ? "FE" :
                           e.EcfType.Code == "32" ? "FC" :
                           e.EcfType.Code == "33" ? "ND" :
                           e.EcfType.Code == "34" ? "NC" : "FE",
                    Amount = e.Total,
                    Status = e.EcfStatus.Name == "Accepted" ? "accepted" :
                             (e.EcfStatus.Name == "Rejected" || e.EcfStatus.Name == "ValidationFailed" || e.EcfStatus.Name == "Error") ? "rejected" :
                             (e.EcfStatus.Name == "SendPending" || e.EcfStatus.Name == "Sending" || e.EcfStatus.Name == "Sent") ? "pending" : "processing",
                    DgiiTrackId = e.EcfTransmissions.OrderByDescending(t => t.SentAtUtc).Select(t => t.TrackId).FirstOrDefault(),
                    SentAtUtc = e.EcfTransmissions.OrderByDescending(t => t.SentAtUtc).Select(t => (DateTime?)t.SentAtUtc).FirstOrDefault(),
                    RegisteredAt = e.RegisteredAt,
                    SignatureDateTime = e.SignatureDateTime
                });

                // Paginación si se especifica
                int page = pageNumber ?? 1;
                int size = pageSize ?? 10;

                var tempResults = await tempProjection
                    .Skip((page - 1) * size)
                    .Take(size)
                    .ToListAsync(cancellationToken);

                // Mapear y formatear fechas en memoria usando ToDrTime() para tener fecha y hora exacta dominicana
                var results = tempResults.Select(x => new EcfDocumentViewDto
                {
                    Id = x.Id,
                    Ncf = x.Ncf,
                    ClientName = x.ClientName,
                    ClientRnc = x.ClientRnc,
                    Type = x.Type,
                    Amount = x.Amount,
                    Status = x.Status,
                    DgiiTrackId = x.DgiiTrackId,
                    SentDate = (x.SentAtUtc ?? x.RegisteredAt).ToDrTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    ResponseDate = x.SignatureDateTime.HasValue ? x.SignatureDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null
                }).ToList();

                var paginatedResponse = new PaginatedResponseDto<EcfDocumentViewDto>
                {
                    Items = results,
                    TotalCount = totalCount,
                    PageNumber = page,
                    PageSize = size,
                    TotalPages = (int)Math.Ceiling((double)totalCount / size)
                };

                return Ok(paginatedResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo listado de facturas: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al recuperar facturas." });
            }
        }

        [HttpGet("guid/{guid}")]
        public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _ecfDocumentService.Table
                    .Include(e => e.EcfStatus)
                    .Include(e => e.EcfType)
                    .Include(e => e.EcfTransmissions)
                    .Include(e => e.EcfXmlDocuments)
                    .Where(e => e.GuidId == guid)
                    .AsQueryable();

                // Filtrado por usuario / propiedad
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(e => e.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                var document = await query.FirstOrDefaultAsync(cancellationToken);
                if (document == null)
                    return NotFound(new { message = "No se encontró la factura o no tiene permisos para verla." });

                var dto = new EcfDocumentViewDto
                {
                    Id = document.GuidId,
                    Ncf = document.Ncf,
                    ClientName = document.CustomerName,
                    ClientRnc = document.CustomerRnc,
                    Type = document.EcfType.Code == "31" ? "FE" :
                           document.EcfType.Code == "32" ? "FC" :
                           document.EcfType.Code == "33" ? "ND" :
                           document.EcfType.Code == "34" ? "NC" : "FE",
                    Amount = document.Total,
                    Status = document.EcfStatus.Name == "Accepted" ? "accepted" :
                             (document.EcfStatus.Name == "Rejected" || document.EcfStatus.Name == "ValidationFailed" || document.EcfStatus.Name == "Error") ? "rejected" :
                             (document.EcfStatus.Name == "SendPending" || document.EcfStatus.Name == "Sending" || document.EcfStatus.Name == "Sent") ? "pending" : "processing",
                    DgiiTrackId = document.EcfTransmissions.OrderByDescending(t => t.SentAtUtc).Select(t => t.TrackId).FirstOrDefault(),
                    SentDate = (document.EcfTransmissions.OrderByDescending(t => t.SentAtUtc).Select(t => (DateTime?)t.SentAtUtc).FirstOrDefault() ?? document.RegisteredAt).ToDrTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    ResponseDate = document.SignatureDateTime.HasValue ? document.SignatureDateTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : null,
                    Xml = document.EcfXmlDocuments.Select(x => !string.IsNullOrEmpty(x.XmlSigned) ? x.XmlSigned : x.XmlUnsigned).FirstOrDefault()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo factura por GUID {Guid}: {Message}", guid, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al recuperar factura." });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
        {
            try
            {
                var query = _ecfDocumentService.Table
                    .AsNoTracking()
                    .Include(e => e.EcfStatus)
                    .AsQueryable();

                // Filtrado por usuario / propiedad
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(e => e.Client.UserClients.Any(uc => uc.UserId == userId));
                }

                var statsList = await query
                    .Select(e => e.EcfStatus.Name)
                    .ToListAsync(cancellationToken);

                var aceptadas = statsList.Count(name => name == "Accepted");
                var rechazadas = statsList.Count(name => name == "Rejected" || name == "ValidationFailed" || name == "Error");
                var pendientes = statsList.Count(name => name == "SendPending" || name == "Sending" || name == "Sent");
                var procesando = statsList.Count(name => name != "Accepted" &&
                                                        name != "Rejected" &&
                                                        name != "ValidationFailed" &&
                                                        name != "Error" &&
                                                        name != "SendPending" &&
                                                        name != "Sending" &&
                                                        name != "Sent");

                var dto = new EcfDocumentStatsDto
                {
                    Aceptadas = aceptadas,
                    Pendientes = pendientes,
                    Procesando = procesando,
                    Rechazadas = rechazadas
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas de facturas: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno al recuperar estadísticas." });
            }
        }
    }
}
