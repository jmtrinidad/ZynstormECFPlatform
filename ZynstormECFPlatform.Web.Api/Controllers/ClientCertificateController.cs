using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    public class ClientCertificateController(
        IClientCertificateService certificateService,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILoggerFactory loggerFactory) : BaseController<ClientCertificateController, ClientCertificate, ClientCertificateCreateDto, ClientCertificateCreateDto, ClientCertificateViewDto>(certificateService, mapper, loggerFactory)
    {
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

                // 5. Validamos el certificado y extraemos metadatos
                string thumbprint;
                DateTime expirationDate;
                try
                {
                    using var x509 = new X509Certificate2(certificateBytes, dto.Password, X509KeyStorageFlags.Exportable);
                    thumbprint = x509.Thumbprint;
                    expirationDate = x509.NotAfter;
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

                ClientCertificate? model = null;

                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    model = new ClientCertificate
                    {
                        ClientId = client.ClientId,
                        Certificate = encryptedCertificate,
                        Password = encryptedPassword,
                        Thumbprint = thumbprint,
                        ExpirationDateUtc = expirationDate.ToUniversalTime()
                    };

                    await Repository.InsertAsync(model);
                });

                return Ok(Mapper.Map<ClientCertificate, ClientCertificateViewDto>(model!));
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, exception.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
