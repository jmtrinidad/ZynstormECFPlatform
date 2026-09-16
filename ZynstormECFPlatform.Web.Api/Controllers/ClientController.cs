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
using ZynstormECFPlatform.Services.Billing;
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
        IRepository<EcfDocument> ecfDocumentRepository,
        IPlanService planService,
        IRepository<ClientMonthlyUsage> clientMonthlyUsageRepository,
        IRepository<PlanOverageTier> planOverageTierRepository,
        IRepository<UserClient> userClientRepository,
        IClientCertificateService clientCertificateService,
        Microsoft.Extensions.Options.IOptions<ZynstormECFPlatform.Core.AppSettings> appSettings) : BaseController<ClientController, Client, ClientCreateDto, ClientUpdateDto, ClientViewDto>(clientService, mapper, loggerFactory)
    {
        private int CertificateWarningDays => appSettings.Value.CertificateExpirationWarningDays > 0
            ? appSettings.Value.CertificateExpirationWarningDays
            : ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.DefaultWarningDays;

        /// <summary>Completa los datos del certificado vigente (vencimiento y aviso) en los clientes mapeados.</summary>
        private async Task FillCertificateExpirationAsync(IEnumerable<ClientViewDto> clients, CancellationToken cancellationToken)
        {
            var list = clients.ToList();
            if (list.Count == 0) return;

            var clientIds = list.Select(c => c.ClientId).ToList();
            var certificates = await clientCertificateService.Table
                .AsNoTracking()
                .Where(c => clientIds.Contains(c.ClientId))
                .ToListAsync(cancellationToken);

            var activeByClient = certificates
                .GroupBy(c => c.ClientId)
                .ToDictionary(g => g.Key, g => ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.SelectActive(g));

            foreach (var dto in list)
            {
                if (!activeByClient.TryGetValue(dto.ClientId, out var active) || active?.ExpirationDateUtc == null) continue;

                var days = ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.GetDaysToExpire(active.ExpirationDateUtc);
                dto.CertificateExpirationDateUtc = active.ExpirationDateUtc;
                dto.CertificateDaysToExpire = days;
                dto.CertificateExpiringSoon = ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.IsExpiringSoon(days, CertificateWarningDays);
            }
        }

        private int PaymentWarningDays => appSettings.Value.PaymentWarningDays > 0
            ? appSettings.Value.PaymentWarningDays
            : PaymentCalculator.DefaultWarningDays;

        /// <summary>Usuarios activos y no eliminados por cliente.</summary>
        private async Task<Dictionary<int, int>> ActiveUserCountsAsync(List<int> clientIds, CancellationToken cancellationToken)
        {
            if (clientIds.Count == 0) return [];

            var counts = await userClientRepository.Table
                .AsNoTracking()
                .Where(uc => clientIds.Contains(uc.ClientId) && uc.User.IsActive && !uc.User.IsDeleted)
                .GroupBy(uc => uc.ClientId)
                .Select(g => new { ClientId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return counts.ToDictionary(c => c.ClientId, c => c.Count);
        }

        /// <summary>Completa usuarios activos y, en los clientes con plan, estado del pago y monto del ciclo.</summary>
        private async Task FillPaymentStatusAsync(IEnumerable<ClientViewDto> clients, CancellationToken cancellationToken)
        {
            var list = clients.ToList();
            if (list.Count == 0) return;

            var countByClient = await ActiveUserCountsAsync(list.Select(c => c.ClientId).ToList(), cancellationToken);

            foreach (var dto in list)
            {
                dto.ActiveUsersCount = countByClient.TryGetValue(dto.ClientId, out var count) ? count : 0;

                if (dto.PlanTypeId is null) continue;

                var (status, days) = PaymentCalculator.GetPaymentStatus(dto.NextPaymentDate, PaymentWarningDays);
                dto.PaymentStatus = (int)status;
                dto.PaymentDaysToDue = days;
                dto.PaymentCycleAmount = PaymentCalculator
                    .Calculate(dto.PlanMonthlyFee ?? 0m, dto.PaidMonths, dto.PrepaymentDiscountPercent)
                    .Total;
            }
        }

        /// <summary>Filas del reporte mensual para los clientes con plan de renta activo.</summary>
        private async Task<List<ClientMonthlyUsageDto>> BuildRentUsageRowsAsync(
            int year, int month, string? clientGuid, CancellationToken cancellationToken)
        {
            var query = ActivePlanClientsQuery().Where(c => c.Plan!.PlanTypeId == (int)PlanTypeEnum.Rent);
            if (!string.IsNullOrEmpty(clientGuid))
                query = query.Where(c => c.GuidId == clientGuid);

            var clients = await query.ToListAsync(cancellationToken);

            return clients.Select(c =>
            {
                var calculation = PaymentCalculator.Calculate(c.Plan!.MonthlyFee, c.PaidMonths, c.PrepaymentDiscountPercent);
                var feeCharged = PaymentCalculator.GetAmountForMonth(calculation, c.NextPaymentDate, year, month);

                return new ClientMonthlyUsageDto
                {
                    ClientGuidId = c.GuidId,
                    ClientName = c.Name,
                    ClientRnc = c.Rnc,
                    ClientInactive = c.ClientInactive,
                    Year = year,
                    Month = month,
                    PlanName = c.Plan.Name,
                    PlanTypeId = (int)PlanTypeEnum.Rent,
                    MonthlyFee = c.Plan.MonthlyFee,
                    MonthlyDocumentLimit = BillingCalculator.UnlimitedDocuments,
                    AcceptedDocuments = 0,
                    OverageDocuments = 0,
                    OverageAmount = 0m,
                    MonthlyFeeCharged = feeCharged,
                    MonthlyFeeCovered = PaymentCalculator.IsMonthCovered(c.NextPaymentDate, year, month),
                    Total = feeCharged,
                    Tiers = []
                };
            }).ToList();
        }

        /// <summary>Clientes con un plan activo de cualquier tipo.</summary>
        private IQueryable<Client> ActivePlanClientsQuery() =>
            Repository.Table.AsNoTracking()
                .Include(c => c.Plan)
                .Where(c => c.Plan != null && c.Plan.StatusId == (int)StatusEnum.Active);

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
                // Por defecto se incluyen los inactivos; includeInactive=false devuelve solo activos
                bool includeInactive = !bool.TryParse(Request.Query["includeInactive"], out var ii) || ii;
                // Si se proporciona 'id', buscamos un único cliente por su GUID
                if (!string.IsNullOrEmpty(id))
                {
                    var query = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).Include(c => c.Plan).Where(c => c.GuidId == id).AsQueryable();
                    if (!IsSA)
                    {
                        var userId = CurrentUserId;
                        query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                    }

                    var result = await query.FirstOrDefaultAsync(cancellationToken);
                    if (result == null) return NotFound();

                    var single = Mapper.Map<Client, ClientViewDto>(result);
                    await FillPaymentStatusAsync([single], cancellationToken);
                    return Ok(single);
                }

                // Si no hay 'id', devolvemos una lista (opcionalmente filtrada por guidId)
                var listQuery = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).Include(c => c.Plan).AsQueryable();
                if (!string.IsNullOrEmpty(guidId))
                {
                    listQuery = listQuery.Where(x => x.GuidId == guidId);
                }

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    listQuery = listQuery.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                if (!includeInactive)
                {
                    listQuery = listQuery.Where(c => !c.ClientInactive);
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

                    var mappedItems = Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results).ToList();
                    await FillCertificateExpirationAsync(mappedItems, cancellationToken);
                    await FillPaymentStatusAsync(mappedItems, cancellationToken);

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
                    var mappedList = Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results).ToList();
                    await FillCertificateExpirationAsync(mappedList, cancellationToken);
                    await FillPaymentStatusAsync(mappedList, cancellationToken);
                    return Ok(mappedList);
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
                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");

                if (ValidatePaymentDates(dto) is string paymentDateError)
                    return BadRequest(paymentDateError);

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

                if (model?.PlanId != null)
                    model.Plan = await planService.GetNoTrackingByAsync(p => p.PlanId == model.PlanId);

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

                if (!await PlanExistsAsync(dto.PlanId))
                    return BadRequest("El plan seleccionado no existe.");

                if (ValidatePaymentDates(dto) is string paymentDateError)
                    return BadRequest(paymentDateError);

                var query = Repository.Table.Include(c => c.ApiKeys).Include(c => c.Plan).Where(c => c.GuidId == guid).AsQueryable();

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

                model.Plan = model.PlanId.HasValue
                    ? await planService.GetNoTrackingByAsync(p => p.PlanId == model.PlanId)
                    : null;

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
                var query = Repository.Table.AsNoTracking().Include(c => c.ApiKeys).Include(c => c.Plan).Where(c => c.GuidId == guid).AsQueryable();
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

        [HttpPost]
        [Route("guid/{guid}/certificate-expiration/notify", Order = 1)]
        public async Task<IActionResult> NotifyCertificateExpiration(string guid, CancellationToken cancellationToken = default)
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
                if (client == null)
                    return NotFound(new { message = "No se encontró el cliente." });

                if (string.IsNullOrWhiteSpace(client.Email))
                    return BadRequest(new { message = "El cliente no tiene un correo electrónico registrado." });

                var certificates = await clientCertificateService.Table
                    .AsNoTracking()
                    .Where(c => c.ClientId == client.ClientId)
                    .ToListAsync(cancellationToken);

                var active = ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.SelectActive(certificates);
                if (active?.ExpirationDateUtc == null)
                    return BadRequest(new { message = "El cliente no tiene un certificado con fecha de vencimiento." });

                var days = ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper.GetDaysToExpire(active.ExpirationDateUtc)!.Value;
                var (subject, htmlBody) = ZynstormECFPlatform.Services.Certificates.CertificateExpirationHelper
                    .BuildClientEmail(client.Name, active.ExpirationDateUtc.Value, days);

                await emailService.SendEmailAsync(client.Email, subject, htmlBody, cancellationToken: cancellationToken);

                return Ok(new { message = $"Aviso de vencimiento enviado a {client.Email}." });
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Error enviando aviso de vencimiento de certificado al cliente {Guid}", guid);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo enviar el aviso de vencimiento." });
            }
        }

        [HttpGet]
        [Route("usage", Order = 1)]
        public async Task<IActionResult> GetMonthlyUsage([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();
            if (year < 2000 || month is < 1 or > 12)
                return BadRequest("Año o mes inválido.");

            try
            {
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Year == year && u.Month == month);

                var documentRows = await BuildUsageDtosAsync(query, cancellationToken);
                var rentRows = await BuildRentUsageRowsAsync(year, month, clientGuid: null, cancellationToken);

                return Ok(documentRows.Concat(rentRows).OrderBy(d => d.ClientName).ToList());
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("guid/{guid}/usage", Order = 1)]
        public async Task<IActionResult> GetClientMonthlyUsage(string guid, [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();
            if (year < 2000 || month is < 1 or > 12)
                return BadRequest("Año o mes inválido.");

            try
            {
                var query = clientMonthlyUsageRepository.Table.AsNoTracking()
                    .Where(u => u.Client.GuidId == guid && u.Year == year && u.Month == month);

                var result = (await BuildUsageDtosAsync(query, cancellationToken)).FirstOrDefault()
                    ?? (await BuildRentUsageRowsAsync(year, month, guid, cancellationToken)).FirstOrDefault();

                return result == null ? NotFound("El cliente no tiene consumo registrado en ese mes.") : Ok(result);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("payments", Order = 1)]
        public async Task<IActionResult> GetPaymentClients(CancellationToken cancellationToken = default)
        {
            if (!IsSA) return Forbid();

            try
            {
                var clients = await ActivePlanClientsQuery().OrderBy(c => c.Name).ToListAsync(cancellationToken);
                var countByClient = await ActiveUserCountsAsync(clients.Select(c => c.ClientId).ToList(), cancellationToken);

                var rows = clients.Select(c =>
                {
                    var calculation = PaymentCalculator.Calculate(c.Plan!.MonthlyFee, c.PaidMonths, c.PrepaymentDiscountPercent);
                    var (status, days) = PaymentCalculator.GetPaymentStatus(c.NextPaymentDate, PaymentWarningDays);

                    return new ClientPaymentDto
                    {
                        ClientGuidId = c.GuidId,
                        ClientName = c.Name,
                        ClientRnc = c.Rnc,
                        ClientInactive = c.ClientInactive,
                        PlanName = c.Plan.Name,
                        PlanTypeId = c.Plan.PlanTypeId,
                        MonthlyFee = c.Plan.MonthlyFee,
                        MaxUsers = c.Plan.MaxUsers,
                        ActiveUsersCount = countByClient.TryGetValue(c.ClientId, out var count) ? count : 0,
                        PaidMonths = c.PaidMonths,
                        PrepaymentDiscountPercent = c.PrepaymentDiscountPercent,
                        MonthsCovered = calculation.MonthsCovered,
                        GrossAmount = calculation.GrossAmount,
                        DiscountAmount = calculation.DiscountAmount,
                        Total = calculation.Total,
                        LastPaymentDate = c.LastPaymentDate,
                        NextPaymentDate = c.NextPaymentDate,
                        PaymentStatus = (int)status,
                        PaymentDaysToDue = days
                    };
                }).ToList();

                return Ok(rows);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        private async Task<List<ClientMonthlyUsageDto>> BuildUsageDtosAsync(IQueryable<ClientMonthlyUsage> query, CancellationToken cancellationToken)
        {
            var usages = await query.Include(u => u.Client).ToListAsync(cancellationToken);

            var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
            var tiers = await planOverageTierRepository.Table.AsNoTracking()
                .Where(t => planIds.Contains(t.PlanId))
                .ToListAsync(cancellationToken);

            return usages
                .Select(u =>
                {
                    var calculation = BillingCalculator.Calculate(
                        u.MonthlyFee,
                        u.MonthlyDocumentLimit,
                        tiers.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                        u.AcceptedDocuments);

                    // El pago adelantado cubre solo la mensualidad; el excedente se cobra siempre.
                    var payment = PaymentCalculator.Calculate(u.MonthlyFee, u.Client.PaidMonths, u.Client.PrepaymentDiscountPercent);
                    var feeCharged = PaymentCalculator.GetAmountForMonth(payment, u.Client.NextPaymentDate, u.Year, u.Month);

                    return new ClientMonthlyUsageDto
                    {
                        ClientGuidId = u.Client.GuidId,
                        ClientName = u.Client.Name,
                        ClientRnc = u.Client.Rnc,
                        ClientInactive = u.Client.ClientInactive,
                        Year = u.Year,
                        Month = u.Month,
                        PlanName = u.PlanName,
                        PlanTypeId = (int)PlanTypeEnum.Documents,
                        MonthlyFee = calculation.MonthlyFee,
                        MonthlyDocumentLimit = calculation.MonthlyDocumentLimit,
                        AcceptedDocuments = calculation.AcceptedDocuments,
                        OverageDocuments = calculation.OverageDocuments,
                        OverageAmount = calculation.OverageAmount,
                        MonthlyFeeCharged = feeCharged,
                        MonthlyFeeCovered = PaymentCalculator.IsMonthCovered(u.Client.NextPaymentDate, u.Year, u.Month),
                        Total = feeCharged + calculation.OverageAmount,
                        Tiers = calculation.Tiers.Select(t => new OverageTierChargeDto
                        {
                            FromUnit = t.FromUnit,
                            ToUnit = t.ToUnit,
                            UnitPrice = t.UnitPrice,
                            Units = t.Units,
                            Amount = t.Amount
                        }).ToList()
                    };
                })
                .OrderBy(d => d.ClientName)
                .ToList();
        }

        private async Task<bool> PlanExistsAsync(int? planId) =>
            !planId.HasValue || await planService.GetNoTrackingByAsync(p => p.PlanId == planId.Value) != null;

        private static string? ValidatePaymentDates(ClientCreateDto dto) =>
            dto.LastPaymentDate is DateTime last
            && dto.NextPaymentDate is DateTime next
            && next.Date < last.Date
                ? "La fecha de próximo pago no puede ser anterior a la del último pago."
                : null;
    }
}