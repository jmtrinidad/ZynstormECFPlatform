using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
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
        ILoggerFactory loggerFactory) : BaseController<ClientController, Client, ClientCreateDto, ClientUpdateDto, ClientViewDto>(clientService, mapper, loggerFactory)
    {
        private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private bool IsSA => User.IsInRole("SA");

        [HttpGet]
        [Route("all", Order = 1)]
        public override async Task<ActionResult<IEnumerable<ClientViewDto>>> Get(CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking();

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var results = await query.ToListAsync(cancellationToken);

                return Ok(Mapper.Map<IEnumerable<Client>, IEnumerable<ClientViewDto>>(results));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        [HttpGet]
        [Route("", Order = 1)]
        public override async Task<ActionResult<ClientViewDto>> GetById([FromQuery] int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var query = Repository.Table.AsNoTracking().Where(c => c.ClientId == id);

                if (!IsSA)
                {
                    var userId = CurrentUserId;
                    query = query.Where(c => c.UserClients.Any(uc => uc.UserId == userId));
                }

                var result = await query.FirstOrDefaultAsync(cancellationToken);

                if (result == null)
                    return NotFound();

                return Ok(Mapper.Map<Client, ClientViewDto>(result));
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

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    model = Mapper.Map<ClientCreateDto, Client>(dto);

                    model = await Repository.InsertAsync(model);

                    if (model != null && !string.IsNullOrEmpty(model.Email))
                    {
                        var apiKey = Tools.GenerateSecureRandomString(32);
                        var secretKey = Tools.GenerateSecureRandomString(64);

                        var apiKeyEntity = new ApiKey
                        {
                            ClientId = model.ClientId,
                            Apikey = apiKey,
                            SecretKey = encryptedService.EncryptString(secretKey),
                            StatusId = (int)StatusEnum.Active
                        };

                        await apiKeyService.InsertAsync(apiKeyEntity);

                        // Enviamos el correo. Si falla, el UnitOfWork se encarga de revertir los cambios.
                        await emailService.SendApiKeyEmailAsync(model.Email, apiKey, secretKey);
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
    }
}