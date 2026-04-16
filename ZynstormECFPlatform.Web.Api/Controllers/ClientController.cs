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
                /*

                {
                  "name": "TRANSPORTE NJ SRL",
                  "rnc": "133009889",
                  "email": "JM.TRINIDAD.99@HOTMAIL.COM",
                  "phone": "809-876-4046"
                }

                 */
                Client? model = null;

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    model = Mapper.Map<ClientCreateDto, Client>(dto);

                    model = await Repository.InsertAsync(model);

                    if (model != null && !string.IsNullOrEmpty(model.Email))
                    {
                        var apiKey = KeyGenerator.GenerateApiKey();
                        var secretKey = KeyGenerator.GenerateSecretKey();

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