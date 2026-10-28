using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Certificates;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [Authorize(Roles = "SA,Admin")]
    public class ClientCertificateController(
        IClientCertificateService certificateService,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggerFactory loggerFactory) : BaseController<ClientCertificateController, ClientCertificate, ClientCertificateCreateDto, ClientCertificateCreateDto, ClientCertificateViewDto>(certificateService, mapper, loggerFactory)
    {
        [HttpGet]
        [Route("", Order = 1)]
        [ProducesResponseType(200)]
        public override async Task<ActionResult> Get([FromQuery] string? guidId, [FromQuery] string? id, CancellationToken cancellationToken = default)
        {
            try
            {
                // Si se proporciona 'id', se busca un único registro de certificado por su propio GUID
                if (!string.IsNullOrEmpty(id))
                {
                    var result = await certificateService.GetByAsync(x => x.GuidId == id, cancellationToken);

                    if (result == null) return NotFound();

                    return Ok(Mapper.Map<ClientCertificate, ClientCertificateViewDto>(result));
                }

                IEnumerable<ClientCertificate> results;
                if (!string.IsNullOrEmpty(guidId))
                {
                    // Buscamos primero al cliente por su GuidId
                    var client = await clientService.GetByAsync(x => x.GuidId == guidId, cancellationToken);

                    if (client == null)
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    // Usamos el ClientId interno para filtrar los certificados
                    results = await certificateService.GetManyByAsync(x => x.ClientId == client.ClientId, cancellationToken);
                }
                else
                {
                    results = await certificateService.GetAllAsync(cancellationToken);
                }

                return Ok(Mapper.Map<IEnumerable<ClientCertificate>, IEnumerable<ClientCertificateViewDto>>(results));
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
        [ProducesResponseType(404)]
        [ProducesResponseType(422)]
        [ProducesResponseType(503)]
        public override async Task<ActionResult<ClientCertificateViewDto>> Post([FromForm] ClientCertificateCreateDto dto)
        {
            try
            {
                // 1. Buscamos al cliente por su GuidId para obtener el ClientId interno
                var client = await clientService.GetByAsync(x => x.GuidId == dto.ClientGuidId);

                if (client == null)
                    return NotFound($"No se encontró un cliente con el GuidId proporcionado.");

                // 2. Buscamos la ApiKey asociada al cliente para obtener su SecretKey
                var apiKeyEntity = await apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);

                if (apiKeyEntity == null || string.IsNullOrEmpty(apiKeyEntity.SecretKey))
                    return NotFound($"No se encontró una ApiKey activa para el cliente con GuidId '{dto.ClientGuidId}'.");

                // 3. Desencriptamos la SecretKey del cliente (usando la clave global del sistema)
                var decryptedSecretKey = encryptedService.DecryptString(apiKeyEntity.SecretKey);

                if (string.IsNullOrEmpty(decryptedSecretKey))
                    return UnprocessableEntity("No se pudo obtener la SecretKey del cliente para realizar el cifrado.");

                // 4. Leemos el archivo del certificado
                using var ms = new MemoryStream();
                await dto.Certificate.CopyToAsync(ms);
                var certificateBytes = ms.ToArray();

                // 5. Validamos el certificado y extraemos metadatos (titular, emisor, vigencia)
                var model = new ClientCertificate { ClientId = client.ClientId };
                try
                {
                    using var x509 = new X509Certificate2(certificateBytes, dto.Password, X509KeyStorageFlags.Exportable);
                    CertificateDetailsReader.Apply(x509, model);
                }
                catch (CryptographicException ex)
                {
                    Logger.LogError(ex, "Error validando el certificado PFX: " + ex.Message);
                    return BadRequest("La contraseña del certificado es incorrecta o el archivo no es un PFX válido.");
                }

                // 6. Encriptamos el certificado y el password usando la SecretKey del cliente
                //    a través del servicio centralizado de encriptación
                var encryptedCertificate = encryptedService.EncryptWithSecret(certificateBytes, decryptedSecretKey);
                var encryptedPassword = encryptedService.EncryptWithSecret(Encoding.UTF8.GetBytes(dto.Password), decryptedSecretKey);

                model.Certificate = encryptedCertificate;
                model.Password = encryptedPassword;

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    await Repository.InsertAsync(model);
                });

                return Ok(Mapper.Map<ClientCertificate, ClientCertificateViewDto>(model));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        /// <summary>
        /// Relee los certificados ya registrados para completar los datos del titular y del emisor.
        /// Con guidId (cliente) solo procesa los de ese cliente; sin él, todos.
        /// </summary>
        [HttpPost]
        [Route("refresh-details", Order = 1)]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(503)]
        public async Task<ActionResult<ClientCertificateRefreshDetailsResultDto>> RefreshDetails([FromQuery] string? guidId, CancellationToken cancellationToken = default)
        {
            try
            {
                IEnumerable<ClientCertificate> certificates;
                if (!string.IsNullOrEmpty(guidId))
                {
                    var client = await clientService.GetByAsync(x => x.GuidId == guidId, cancellationToken);

                    if (client == null)
                        return NotFound("No se encontró un cliente con el GuidId proporcionado.");

                    certificates = await certificateService.GetManyByAsync(x => x.ClientId == client.ClientId, cancellationToken);
                }
                else
                {
                    certificates = await certificateService.GetAllAsync(cancellationToken);
                }

                var result = new ClientCertificateRefreshDetailsResultDto();

                foreach (var group in certificates.GroupBy(c => c.ClientId))
                {
                    var apiKeyEntity = await apiKeyService.GetByAsync(x => x.ClientId == group.Key, cancellationToken);
                    var secretKey = apiKeyEntity == null || string.IsNullOrEmpty(apiKeyEntity.SecretKey)
                        ? null
                        : encryptedService.DecryptString(apiKeyEntity.SecretKey);

                    foreach (var certificate in group)
                    {
                        try
                        {
                            if (string.IsNullOrEmpty(secretKey))
                                throw new InvalidOperationException($"El cliente {group.Key} no tiene una SecretKey válida.");

                            var certificateBytes = encryptedService.DecryptWithSecret(certificate.Certificate, secretKey);
                            var password = Encoding.UTF8.GetString(encryptedService.DecryptWithSecret(certificate.Password, secretKey));

                            using var x509 = new X509Certificate2(certificateBytes, password, X509KeyStorageFlags.Exportable);
                            CertificateDetailsReader.Apply(x509, certificate);

                            await Repository.UpdateAsync(certificate);
                            result.Updated++;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "No se pudieron leer los datos del certificado {CertificateId}", certificate.ClientCertificateId);
                            result.Failed++;
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}