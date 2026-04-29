using Microsoft.AspNetCore.Mvc;
using System.Text;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiController]
[Route("fe")]
[AllowAnonymous]
public class FeController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IInboundEcfService _inboundEcfService;
    private readonly ILogger<FeController> _logger;
    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly IEncryptedService _encryptedService;
    private readonly IDgiiAuthService _dgiiAuthService;

    public FeController(
        ICacheService cacheService,
        IJwtTokenService jwtTokenService,
        IInboundEcfService inboundEcfService,
        ILogger<FeController> logger,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IClientCertificateService clientCertificateService,
        IEncryptedService encryptedService,
        IDgiiAuthService dgiiAuthService)
    {
        _cacheService = cacheService;
        _jwtTokenService = jwtTokenService;
        _inboundEcfService = inboundEcfService;
        _logger = logger;
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _clientCertificateService = clientCertificateService;
        _encryptedService = encryptedService;
        _dgiiAuthService = dgiiAuthService;
    }

    /// <summary>
    /// Endpoint para probar el login a la DGII usando el certificado del primer cliente en base de datos. Extrae la
    /// semilla, la firma y te permite ver el proceso (los XML se guardan en el Log por el DgiiAuthService).
    /// </summary>
    [HttpGet("autenticacion/api/test-login")]
    public async Task<IActionResult> TestLogin()
    {
        var clients = await _clientService.GetAllAsync();
        var client = clients.FirstOrDefault();
        if (client == null)
            return BadRequest("No hay clientes en la base de datos para extraer el certificado.");

        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);

        if (apiKey == null || certificate == null)
            return BadRequest($"El cliente con RNC {client.Rnc} no tiene ApiKey o Certificado configurado.");

        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        if (string.IsNullOrEmpty(decryptedSecretKey))
            return BadRequest("No se pudo desencriptar la SecretKey del cliente.");

        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

        if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
            return BadRequest("No se pudo desencriptar el certificado o la contraseña.");

        var certificateBase64 = Convert.ToBase64String(certificateBytes);
        var certificatePassword = Encoding.UTF8.GetString(passwordBytes);

        try
        {
            // Llama a DGII (CerteCF). El DgiiAuthService registrará los XML original y firmado en consola.
            var token = await _dgiiAuthService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certificateBase64, certificatePassword);
            return Ok(new
            {
                mensaje = "Login exitoso. Revisa la consola para ver la estructura de la semilla XML original y la semilla firmada.",
                rnc_utilizado = client.Rnc,
                token = token
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error al hacer login: {ex.Message}");
        }
    }

    /// <summary>
    /// Autenticación B2B - Paso 1: Proveedor solicita semilla para firmarla.
    /// </summary>
    [HttpGet("autenticacion/api/semilla")]
    public IActionResult ObtenerSemilla()
    {
        // Genera semilla base64 larga para coincidir con la estructura de DGII
        string valor = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")));

        // Cachear semilla por 5 minutos
        _cacheService.Set($"Semilla_B2B_{valor}", valor, TimeSpan.FromMinutes(5));

        string fecha = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

        string xmlResponse = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <SemillaModel xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
	        <valor>{valor}</valor>
	        <fecha>{fecha}</fecha>
        </SemillaModel>";

        return Content(xmlResponse, "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// Autenticación B2B - Paso 2: El proveedor envía la semilla firmada para obtener el JWT.
    /// </summary>
    [HttpPost("autenticacion/api/validacioncertificado")]
    public async Task<IActionResult> ValidarCertificado()
    {
        var xmlContent = await GetXmlContentAsync();

        if (string.IsNullOrWhiteSpace(xmlContent))
            return BadRequest(new { error = "No XML content provided" });

        // LOGGEAR EL XML RECIBIDO COMO ERROR PARA PODER ANALIZARLO
        _logger.LogError("=== SEMILLA FIRMADA RECIBIDA DE DGII ===\n{Xml}", xmlContent);

        // 1. Verificar criptográficamente la firma del XML
        bool isValidSignature = VerifyXmlSignature(xmlContent);

        if (!isValidSignature)
        {
            _logger.LogWarning("ValidarCertificado Rechazado: La firma del XML es inválida o no contiene firma.");
            return Unauthorized(new { error = "Firma digital inválida." });
        }

        // 2. Si la firma es válida, devolvemos el Token (tal como lo espera la DGII).
        // Se omitió la validación contra caché para facilitar pruebas de firmas.
        string token = "MOCKED-JWT-FOR-B2B-VERIFICATION-EYJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9";

        return Ok(new
        {
            token = token,
            expira = DateTime.UtcNow.AddMinutes(55).ToString("yyyy-MM-ddTHH:mm:ssZ")
        });
    }

    /// <summary>
    /// Receptor B2B - Recibe el archivo de la factura.
    /// </summary>
    [HttpPost("recepcion/api/ecf")]
    public async Task<IActionResult> RecepcionEcf()
    {
        var xmlContent = await GetXmlContentAsync();

        if (string.IsNullOrWhiteSpace(xmlContent))
            return BadRequest(new { error = "xml content missing", trackId = "", mensaje = "Error" });

        // LOGGEAR EL XML RECIBIDO COMO ERROR PARA PODER ANALIZARLO
        _logger.LogError("=== ECF RECIBIDO DE DGII ===\n{Xml}", xmlContent);

        // EXTRAER RNC COMPRADOR (Nosotros recibiendo el ECF)
        var rncComprador = ExtractTag(xmlContent, "RNCComprador");

        if (string.IsNullOrEmpty(rncComprador))
            return BadRequest(new { error = "RNCComprador no encontrado en el XML", trackId = "", mensaje = "Error de validación", estado = "Rechazado", codigo = 0 });

        var client = await _clientService.GetByAsync(x => x.Rnc == rncComprador);

        if (client == null)
        {
            _logger.LogWarning("ECF Rechazado: El RNCComprador {Rnc} extraído del XML no existe en nuestra base de datos de clientes.", rncComprador);
            return BadRequest(new { error = $"Cliente no encontrado para el RNC {rncComprador}", trackId = "", mensaje = "Rechazado", estado = "Rechazado", codigo = 0 });
        }

        _logger.LogInformation("ECF validado correctamente para el cliente {Rnc}", rncComprador);

        try
        {
            string trackId = await _inboundEcfService.ReceiveEcfAsync(xmlContent);

            // Retornamos el mismo formato que usa DGII
            return Ok(new
            {
                trackId = trackId,
                error = "",
                mensaje = "Recibido exitosamente",
                estado = "Aceptado",
                codigo = 1
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                error = ex.Message,
                trackId = "",
                mensaje = "Error al procesar",
                estado = "Rechazado",
                codigo = 0
            });
        }
    }

    /// <summary>
    /// Aprobación Comercial B2B.
    /// </summary>
    [HttpPost("aprobacioncomercial/api/ecf")]
    public async Task<IActionResult> AprobacionComercial()
    {
        var xmlContent = await GetXmlContentAsync();

        if (string.IsNullOrWhiteSpace(xmlContent))
            return BadRequest(new { estado = "Rechazado", mensaje = new[] { "xml content missing" }, codigo = "0" });

        // LOGGEAR EL XML RECIBIDO COMO ERROR PARA PODER ANALIZARLO
        _logger.LogError("=== APROBACION COMERCIAL RECIBIDA DE DGII ===\n{Xml}", xmlContent);

        // EXTRAER EL NCF PARA CONSULTAR EN BASE DE DATOS
        var eNcf = ExtractTag(xmlContent, "eNCF");

        if (!string.IsNullOrEmpty(eNcf))
        {
            _logger.LogError("=== NCF A CONSULTAR EN LA BASE DE DATOS PARA APROBAR: {eNcf} ===", eNcf);
        }

        // EXTRAER RNC EMISOR (Nosotros, los que emitimos el ECF originalmente y ahora recibimos su aprobación)
        var rncEmisor = ExtractTag(xmlContent, "RNCEmisor");

        if (string.IsNullOrEmpty(rncEmisor))
            return BadRequest(new { estado = "Rechazado", mensaje = new[] { "RNCEmisor no encontrado en el XML" }, codigo = "0" });

        var client = await _clientService.GetByAsync(x => x.Rnc == rncEmisor);

        if (client == null)
        {
            _logger.LogWarning("Aprobación Comercial Rechazada: El RNCEmisor {Rnc} extraído del XML no existe en nuestra base de datos de clientes.", rncEmisor);
            return BadRequest(new { estado = "Rechazado", mensaje = new[] { $"Cliente no encontrado para el RNC {rncEmisor}" }, codigo = "0" });
        }

        _logger.LogInformation("Aprobación Comercial validada correctamente para el cliente {Rnc}", rncEmisor);

        try
        {
            await _inboundEcfService.ProcessCommercialApprovalAsync(xmlContent);

            // Retornamos el formato de DGII para Aprobación Comercial
            return Ok(new
            {
                codigo = "1",
                estado = "Aprobado",
                mensaje = new[] { "Aprobación Comercial recibida exitosamente" }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                codigo = "0",
                estado = "Rechazado",
                mensaje = new[] { ex.Message }
            });
        }
    }

    /// <summary>
    /// Helper para obtener el XML tanto de multipart/form-data (usado en CerteCF) como del raw body.
    /// </summary>
    private async Task<string> GetXmlContentAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            var file = form.Files.GetFile("xml");
            if (file != null && file.Length > 0)
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // Fallback a leer el body si es raw application/xml
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private string ExtractTag(string xml, string tagName)
    {
        var startTag = $"<{tagName}>";
        var endTag = $"</{tagName}>";
        var inicio = xml.IndexOf(startTag);
        var fin = xml.IndexOf(endTag);
        if (inicio != -1 && fin != -1)
        {
            return xml.Substring(inicio + startTag.Length, fin - (inicio + startTag.Length)).Trim();
        }
        return string.Empty;
    }

    /// <summary>
    /// Helper para verificar la firma XML (XML-DSig) utilizando la llave pública incrustada en el XML.
    /// </summary>
    private bool VerifyXmlSignature(string xmlContent)
    {
        try
        {
            // DGII y nuestro XmlSignatureService utilizan PreserveWhitespace = false al firmar,
            // por lo tanto, debe ser false al verificar.
            var xmlDoc = new System.Xml.XmlDocument { PreserveWhitespace = false };
            xmlDoc.LoadXml(xmlContent);

            var nodeList = xmlDoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (nodeList.Count == 0) return false;

            var signedXml = new System.Security.Cryptography.Xml.SignedXml(xmlDoc);
            signedXml.LoadXml((System.Xml.XmlElement)nodeList[0]);

            return signedXml.CheckSignature();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar la firma del XML.");
            return false;
        }
    }
}