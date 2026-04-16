using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>
/// Recibe un XML de postulación DGII, lo firma digitalmente usando el certificado
/// del cliente identificado por el RNC del contribuyente y devuelve el XML firmado.
/// </summary>
//[Authorize]
[Route("[controller]")]
[ApiController]
public class XmlSignController(
    IClientService clientService,
    IApiKeyService apiKeyService,
    IClientCertificateService clientCertificateService,
    IEncryptedService encryptedService,
    IXmlSignatureService xmlSignatureService,
    ILoggerFactory loggerFactory) : ControllerBase
{
    private readonly ILogger<XmlSignController> _logger = loggerFactory.CreateLogger<XmlSignController>();

    /// <summary>
    /// Recibe un archivo XML, extrae el RNC del contribuyente, busca el cliente,
    /// obtiene su certificado y lo devuelve firmado como descarga.
    /// </summary>
    [HttpPost("sign")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> SignXml(IFormFile xmlFile, CancellationToken cancellationToken = default)
    {
        try
        {
            if (xmlFile == null || xmlFile.Length == 0)
                return BadRequest("Debe enviar un archivo XML.");

            // 1. Leemos el contenido del XML
            string xmlContent;
            using (var streamReader = new StreamReader(xmlFile.OpenReadStream(), Encoding.UTF8))
            {
                xmlContent = await streamReader.ReadToEndAsync(cancellationToken);
            }

            // 2. Parseamos el XML y extraemos el RNC del contribuyente
            string rnc;
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                var rncNode = xmlDoc.SelectSingleNode("//Contribuyente/RNCContribuyente")
                              ?? xmlDoc.SelectSingleNode("//*[local-name()='RNCContribuyente']");

                if (rncNode == null || string.IsNullOrWhiteSpace(rncNode.InnerText))
                    return BadRequest("El XML no contiene el nodo 'RNCContribuyente' o está vacío.");

                rnc = rncNode.InnerText.Trim();
            }
            catch (XmlException ex)
            {
                _logger.LogError(ex, "Error al parsear el XML recibido.");
                return BadRequest("El archivo no es un XML válido: " + ex.Message);
            }

            // 3. Buscamos al cliente por RNC
            var client = await clientService.GetByAsync(x => x.Rnc == rnc, cancellationToken);

            if (client == null)
                return NotFound($"No se encontró ningún cliente registrado con el RNC '{rnc}'.");

            // 4. Obtenemos la ApiKey activa del cliente para acceder a la SecretKey
            var apiKey = await apiKeyService.GetByAsync(x => x.ClientId == client.ClientId, cancellationToken);

            if (apiKey == null || string.IsNullOrEmpty(apiKey.SecretKey))
                return NotFound($"El cliente con RNC '{rnc}' no tiene una ApiKey activa configurada.");

            // 5. Desencriptamos la SecretKey del cliente con la clave global del sistema
            var decryptedSecretKey = encryptedService.DecryptString(apiKey.SecretKey);

            if (string.IsNullOrEmpty(decryptedSecretKey))
                return UnprocessableEntity("No se pudo desencriptar la SecretKey del cliente.");

            // 6. Obtenemos el certificado del cliente
            var certificate = await clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId, cancellationToken);

            if (certificate == null)
                return NotFound($"El cliente con RNC '{rnc}' no tiene un certificado digital registrado.");

            // 7. Desencriptamos el certificado y el password usando la SecretKey del cliente
            var certificateBytes = encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
            var passwordBytes = encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

            if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
                return UnprocessableEntity("No se pudo desencriptar el certificado del cliente.");

            var certificateBase64 = Convert.ToBase64String(certificateBytes);
            var certificatePassword = Encoding.UTF8.GetString(passwordBytes);

            // 8. Firmamos el XML con el certificado del cliente
            string signedXml;
            try
            {
                signedXml = xmlSignatureService.SignXml(xmlContent, certificateBase64, certificatePassword);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al firmar el XML para el cliente RNC '{Rnc}'.", rnc);
                return UnprocessableEntity("Ocurrió un error al firmar el XML: " + ex.Message);
            }

            // 9. Devolvemos el XML firmado como descarga
            var signedBytes = Encoding.UTF8.GetBytes(signedXml);
            var fileName = $"{rnc}_signed.xml";

            return File(signedBytes, "application/xml", fileName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("La solicitud de firma fue cancelada por el cliente.");
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al procesar la firma del XML.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
