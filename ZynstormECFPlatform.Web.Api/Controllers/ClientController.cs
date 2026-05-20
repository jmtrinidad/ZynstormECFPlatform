using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Reports;
using System.Security.Claims;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    public class ClientController(
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggerFactory loggerFactory,
        IRepository<EcfDocument> ecfDocumentRepository) : BaseController<ClientController, Client, ClientCreateDto, ClientUpdateDto, ClientViewDto>(clientService, mapper, loggerFactory)
    {
        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");

        [HttpGet]
        [Route("", Order = 1)]
        public override async Task<ActionResult> Get(
            [FromQuery] string? guidId, 
            [FromQuery] string? id, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Leer parámetros de consulta adicionales de Request.Query
                string? search = Request.Query.TryGetValue("search", out var searchVal) ? searchVal.ToString() : null;
                int? pageNumber = int.TryParse(Request.Query["pageNumber"], out var pn) ? pn : null;
                int? pageSize = int.TryParse(Request.Query["pageSize"], out var ps) ? ps : null;
                // Si se proporciona 'id', buscamos un único cliente por su GUID
                if (!string.IsNullOrEmpty(id))
                {
                    var query = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).Where(c => c.GuidId == id).AsQueryable();
                    if (!IsSA)
                    {
                        var userId = CurrentUserId;
                        query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                    }

                    var result = await query.FirstOrDefaultAsync(cancellationToken);
                    if (result == null) return NotFound();
                    return Ok(Mapper.Map<Client, ClientViewDto>(result));
                }

                // Si no hay 'id', devolvemos una lista (opcionalmente filtrada por guidId)
                var listQuery = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).AsQueryable();
                if (!string.IsNullOrEmpty(guidId))
                {
                    listQuery = listQuery.Where(x => x.GuidId == guidId);
                }

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    listQuery = listQuery.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                // Aplicar búsqueda si se proporciona
                if (!string.IsNullOrEmpty(search))
                {
                    var searchLower = search.ToLower().Trim();
                    listQuery = listQuery.Where(c => 
                        c.Name.ToLower().Contains(searchLower) || 
                        c.Rnc.Contains(searchLower) || 
                        (c.Email != null && c.Email.ToLower().Contains(searchLower))
                    );
                }

                // Paginación si se especifica
                if (pageNumber.HasValue || pageSize.HasValue)
                {
                    var page = pageNumber ?? 1;
                    var size = pageSize ?? 10;

                    var totalCount = await listQuery.CountAsync(cancellationToken);

                    var results = await listQuery
                        .OrderBy(c => c.Name)
                        .Skip((page - 1) * size)
                        .Take(size)
                        .ToListAsync(cancellationToken);

                    var mappedItems = Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results);

                    var paginatedResponse = new PaginatedResponseDto<ClientViewDto>
                    {
                        Items = mappedItems,
                        TotalCount = totalCount,
                        PageNumber = page,
                        PageSize = size,
                        TotalPages = (int)Math.Ceiling((double)totalCount / size)
                    };

                    return Ok(paginatedResponse);
                }
                else
                {
                    var results = await listQuery.OrderBy(c => c.Name).ToListAsync(cancellationToken);
                    return Ok(Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results));
                }
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpPost]
        [Route("", Order = 1)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(422)]
        [ProducesResponseType(503)]
        public override async Task<ActionResult<ClientViewDto>> Post([FromBody] ClientCreateDto dto)
        {
            try
            {
                Client? model = null;

                string? apiKey = null;
                string? secretKey = null;

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    model = Mapper.Map<ClientCreateDto, Client>(dto);

                    model = await Repository.InsertAsync(model);

                    if (model != null && !string.IsNullOrEmpty(model.Email))
                    {
                        apiKey = Tools.GenerateSecureRandomString(32);
                        secretKey = Tools.GenerateSecureRandomString(64);

                        var apiKeyEntity = new ApiKey
                        {
                            ClientId = model.ClientId,
                            Apikey = apiKey,
                            SecretKey = encryptedService.EncryptString(secretKey),
                            StatusId = (int)StatusEnum.Active
                        };

                        await apiKeyService.InsertAsync(apiKeyEntity);
                        model.ApiKeys.Add(apiKeyEntity);
                    }

                    // Asignamos el cliente al usuario que lo creó
                    var userId = CurrentUserId;

                    if (model != null && !string.IsNullOrEmpty(userId))
                    {
                        model.UserClients.Add(new UserClient
                        {
                            UserId = userId,
                            ClientId = model.ClientId
                        });

                        await Repository.UpdateAsync(model);
                    }
                });

                if (model != null && !string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(secretKey))
                {
                    await emailService.SendApiKeyEmailAsync(model.Email, apiKey, secretKey);
                }

                return Ok(Mapper.Map<Client, ClientViewDto>(model!));
            }
            catch (AutoMapperMappingException exception)
            {
                Logger.LogError(exception, exception.Message);

                return StatusCode(422,
                    exception.InnerException != null ?
                        exception.InnerException.Message
                        : "Error validando campos"
                );
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                       (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                Logger.LogError(ex, ex.Message);

                var message = ex.InnerException.Message.Contains("DocumentTypeId_Document") ?
                                                   "Ya existe un registro con ese tipo y número de documento." : "Existe un registro con esta descripción.";
                return Conflict(new
                {
                    error = "Duplicate",
                    message,
                    code = 409
                });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
        [HttpPut]
        [Route("", Order = 1)]
        public override async Task<ActionResult<ClientViewDto>> Put([FromBody] ClientUpdateDto dto)
        {
            try
            {
                var guid = dto.GuidId;
                if (string.IsNullOrEmpty(guid))
                    return BadRequest("El GuidId es obligatorio para la actualización.");

                var query = Repository.Table.Include(c => c.ApiKeys).Where(c => c.GuidId == guid).AsQueryable();

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var model = await query.FirstOrDefaultAsync();

                if (model == null)
                    return NotFound("No se encontró el cliente o no tiene permisos para actualizarlo.");

                Mapper.Map(dto, model);

                await Repository.UpdateAsync(model);

                return Ok(Mapper.Map<Client, ClientViewDto>(model));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<ActionResult<ClientViewDto>> GetByGuid(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).Where(c => c.GuidId == guid).AsQueryable();
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var result = await query.FirstOrDefaultAsync(cancellationToken);
                if (result == null) return NotFound();
                return Ok(Mapper.Map<Client, ClientViewDto>(result));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpDelete]
        [Route("", Order = 1)]
        public override async Task<IActionResult> Delete([FromQuery] string id)
        {
            try
            {
                var query = Repository.Table.Where(x => x.GuidId == id).AsQueryable();
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var result = await query.FirstOrDefaultAsync();
                if (result == null) return NotFound();

                await Repository.SoftDeleteAsync(result);
                return NoContent();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpDelete]
        [Route("guid/{guid}", Order = 1)]
        public override async Task<IActionResult> DeleteByGuid(string guid)
        {
            try
            {
                var query = Repository.Table.Where(x => x.GuidId == guid).AsQueryable();
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var result = await query.FirstOrDefaultAsync();
                if (result == null) return NotFound();

                await Repository.SoftDeleteAsync(result);
                return NoContent();
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}/daily-report/pdf")]
        public async Task<IActionResult> DownloadDailyReportPdf(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking().Where(c => c.GuidId == guid);
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var client = await query.FirstOrDefaultAsync(cancellationToken);
                if (client == null) return NotFound("Cliente no encontrado.");

                var now = DateTime.UtcNow;
                var start = now.AddDays(-1); // Last 24 Hours

                var documents = await ecfDocumentRepository.Table
                    .Include(d => d.EcfStatus)
                    .Where(d => d.ClientId == client.ClientId && !d.IsDeleted && d.RegisteredAt >= start && d.RegisteredAt <= now)
                    .ToListAsync(cancellationToken);

                var pdfBytes = ReportPdfGenerator.GenerateDailyReportPdf(client, documents, start, now);
                var filename = $"Resumen_Diario_{client.Name.Replace(" ", "_")}_{now.ToDrTime():yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error generating daily report PDF via API: {Message}", exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}/weekly-report/pdf")]
        public async Task<IActionResult> DownloadWeeklyReportPdf(string guid, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking().Where(c => c.GuidId == guid);
                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var client = await query.FirstOrDefaultAsync(cancellationToken);
                if (client == null) return NotFound("Cliente no encontrado.");

                var now = DateTime.UtcNow;
                var start = now.AddDays(-7); // Last 7 Days

                var documents = await ecfDocumentRepository.Table
                    .Include(d => d.EcfStatus)
                    .Where(d => d.ClientId == client.ClientId && !d.IsDeleted && d.RegisteredAt >= start && d.RegisteredAt <= now)
                    .ToListAsync(cancellationToken);

                var pdfBytes = ReportPdfGenerator.GenerateWeeklyReportPdf(client, documents, start, now);
                var filename = $"Reporte_Semanal_{client.Name.Replace(" ", "_")}_{now.ToDrTime():yyyyMMdd}.pdf";

                return File(pdfBytes, "application/pdf", filename);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error generating weekly report PDF via API: {Message}", exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}