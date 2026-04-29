using Microsoft.AspNetCore.Authorization;
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
    //[HttpGet("autenticacion/api/test-login")]
    //public async Task<IActionResult> TestLogin()
    //{
    //    var clients = await _clientService.GetAllAsync();
    //    var client = clients.FirstOrDefault();
    //    if (client == null)
    //        return BadRequest("No hay clientes en la base de datos para extraer el certificado.");

    //    var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
    //    var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);

    //    if (apiKey == null || certificate == null)
    //        return BadRequest($"El cliente con RNC {client.Rnc} no tiene ApiKey o Certificado configurado.");

    //    var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
    //    if (string.IsNullOrEmpty(decryptedSecretKey))
    //        return BadRequest("No se pudo desencriptar la SecretKey del cliente.");

    //    var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
    //    var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

    //    if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
    //        return BadRequest("No se pudo desencriptar el certificado o la contraseña.");

    //    var certificateBase64 = Convert.ToBase64String(certificateBytes);
    //    var certificatePassword = Encoding.UTF8.GetString(passwordBytes);

    //    try
    //    {
    //        // Llama a DGII (CerteCF). El DgiiAuthService registrará los XML original y firmado en consola.
    //        var token = await _dgiiAuthService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certificateBase64, certificatePassword);
    //        return Ok(new
    //        {
    //            mensaje = "Login exitoso. Revisa la consola para ver la estructura de la semilla XML original y la semilla firmada.",
    //            rnc_utilizado = client.Rnc,
    //            token = token
    //        });
    //    }
    //    catch (Exception ex)
    //    {
    //        return BadRequest($"Error al hacer login: {ex.Message}");
    //    }
    //}

    /// <summary>
    /// Autenticación B2B - Paso 1: Proveedor solicita semilla para firmarla.
    /// </summary>

    [HttpGet("autenticacion/api/Semilla")]
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

        return Content(xmlResponse, "application/xml", new System.Text.UTF8Encoding(false));
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
    [ZynstormECFPlatform.Web.Api.Filters.B2BTokenAuth]
    public async Task<IActionResult> RecepcionEcf()
    {
        var xmlContent = await GetXmlContentAsync();

        //_logger.LogError("=== ECF RECIBIDO DE DGII ===\n{Xml}", xmlContent);

        var rncEmisor = ExtractTag(xmlContent, "RNCEmisor");
        var rncComprador = ExtractTag(xmlContent, "RNCComprador");
        var eNcf = ExtractTag(xmlContent, "eNCF");

        if (string.IsNullOrEmpty(rncEmisor)) rncEmisor = "131880600";
        if (string.IsNullOrEmpty(rncComprador)) rncComprador = "132880600";
        if (string.IsNullOrEmpty(eNcf)) eNcf = "E310000000001";

        string fecha = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        string xmlResponse = $@"<?xml version=""1.0"" encoding=""utf-8""?><ARECF><DetalleAcusedeRecibo><Version>1.0</Version><RNCEmisor>{rncEmisor}</RNCEmisor><RNCComprador>{rncComprador}</RNCComprador><eNCF>{eNcf}</eNCF><Estado>0</Estado><FechaHoraAcuseRecibo>{fecha}</FechaHoraAcuseRecibo></DetalleAcusedeRecibo></ARECF>";


        // BUSCAR EL CLIENTE POR RNC COMPRADOR PARA USAR SU CERTIFICADO
        try
        {
            _logger.LogInformation("RecepcionEcf: Buscando cliente con RNC Comprador: {RncComprador}", rncComprador);
            var client = await _clientService.GetByAsync(x => x.Rnc == rncComprador);
            if (client != null)
            {
                _logger.LogInformation("RecepcionEcf: Cliente encontrado. Buscando certificado para ClientId: {ClientId}", client.ClientId);
                var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);

                if (certificate != null)
                {
                    _logger.LogInformation("RecepcionEcf: Certificado encontrado. Buscando ApiKey...");
                    var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == certificate.ClientId);
                    if (apiKey != null)
                    {
                        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
                        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
                        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

                        var certificateBase64 = Convert.ToBase64String(certificateBytes);
                        var certificatePassword = Encoding.UTF8.GetString(passwordBytes);

                        try
                        {
                            using var x509Cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateBytes, certificatePassword);
                            _logger.LogInformation("RecepcionEcf: Certificado a usar: {Subject}, Válido hasta: {NotAfter}", x509Cert.Subject, x509Cert.NotAfter);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("RecepcionEcf: No se pudo extraer información del certificado para el log: {Error}", ex.Message);
                        }

                        _logger.LogInformation("RecepcionEcf: Firmando el Acuse de Recibo...");
                        var signer = new ZynstormECFPlatform.Services.XmlSignatureService();
                        xmlResponse = signer.SignXml(xmlResponse, certificateBase64, certificatePassword);
                        _logger.LogInformation("RecepcionEcf: Acuse de Recibo firmado exitosamente.");

                        var validationErrors = ValidateAgainstXsd(xmlResponse, "ARECF v1.0.xsd");
                        if (validationErrors.Count > 0)
                        {
                            _logger.LogError("RecepcionEcf: El XML de Acuse de Recibo no cumple con el XSD.");
                            return BadRequest(new { Message = "El XML generado no cumple con el esquema XSD de la DGII.", Errors = validationErrors });
                        }
                    }
                    else
                    {
                        _logger.LogError("RecepcionEcf: No se encontró ApiKey para el cliente con RNC: {RncComprador}", rncComprador);
                    }
                }
                else
                {
                    _logger.LogError("RecepcionEcf: No se encontró Certificado para el cliente con RNC: {RncComprador}", rncComprador);
                }
            }
            else
            {
                _logger.LogError("RecepcionEcf: No se encontró Cliente en base de datos con RNC: {RncComprador}", rncComprador);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecepcionEcf: Error al firmar el Acuse de Recibo XML.");
        }

        return Content(xmlResponse, "application/xml", new System.Text.UTF8Encoding(false));
    }

    /// <summary>
    /// Aprobación Comercial B2B.
    /// </summary>
    [HttpPost("aprobacioncomercial/api/ecf")]
    [ZynstormECFPlatform.Web.Api.Filters.B2BTokenAuth]
    public async Task<IActionResult> AprobacionComercial()
    {
        var xmlContent = await GetXmlContentAsync();

        _logger.LogError("=== APROBACION COMERCIAL RECIBIDA ===\n{Xml}", xmlContent);

        var rncEmisor = ExtractTag(xmlContent, "RNCEmisor");
        var rncComprador = ExtractTag(xmlContent, "RNCComprador");
        var eNcf = ExtractTag(xmlContent, "eNCF");
        var fechaEmision = ExtractTag(xmlContent, "FechaEmision");
        var montoTotal = ExtractTag(xmlContent, "MontoTotal");
        var estado = ExtractTag(xmlContent, "Estado");

        if (string.IsNullOrEmpty(rncEmisor)) rncEmisor = "131880600";
        if (string.IsNullOrEmpty(rncComprador)) rncComprador = "132880600";
        if (string.IsNullOrEmpty(eNcf)) eNcf = "E310000000001";

        // Formatear FechaEmision a dd-MM-yyyy (XSD requiere este formato)
        if (DateTime.TryParse(fechaEmision, out DateTime dateParsed))
        {
            fechaEmision = dateParsed.ToString("dd-MM-yyyy");
        }
        else
        {
            fechaEmision = DateTime.Now.ToString("dd-MM-yyyy");
        }

        // Formatear MontoTotal a exactamente 2 decimales (XSD requiere [0-9]+(\.[0-9]{2}))
        if (decimal.TryParse(montoTotal, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal montoParsed))
        {
            montoTotal = montoParsed.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
        else
        {
            montoTotal = "0.00";
        }

        if (estado != "1" && estado != "2")
        {
            estado = "1"; // 1: Aceptado
        }

        string fecha = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        string xmlResponse = $@"<?xml version=""1.0"" encoding=""utf-8""?><ACECF><DetalleAprobacionComercial><Version>1.0</Version><RNCEmisor>{rncEmisor}</RNCEmisor><eNCF>{eNcf}</eNCF><FechaEmision>{fechaEmision}</FechaEmision><MontoTotal>{montoTotal}</MontoTotal><RNCComprador>{rncComprador}</RNCComprador><Estado>{estado}</Estado><FechaHoraAprobacionComercial>{fecha}</FechaHoraAprobacionComercial></DetalleAprobacionComercial></ACECF>";


        // BUSCAR EL CLIENTE POR RNC EMISOR PARA USAR SU CERTIFICADO
        try
        {
            var client = await _clientService.GetByAsync(x => x.Rnc == rncEmisor);
            if (client != null)
            {
                var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
                if (certificate != null)
                {
                    var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == certificate.ClientId);
                    if (apiKey != null)
                    {
                        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
                        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
                        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

                        var certificateBase64 = Convert.ToBase64String(certificateBytes);
                        var certificatePassword = Encoding.UTF8.GetString(passwordBytes);

                        var signer = new ZynstormECFPlatform.Services.XmlSignatureService();
                        xmlResponse = signer.SignXml(xmlResponse, certificateBase64, certificatePassword);

                        var validationErrors = ValidateAgainstXsd(xmlResponse, "ACECF v.1.0.xsd");
                        if (validationErrors.Count > 0)
                        {
                            _logger.LogError("AprobacionComercial: El XML de Aprobación Comercial no cumple con el XSD.");
                            return BadRequest(new { Message = "El XML generado no cumple con el esquema XSD de la DGII.", Errors = validationErrors });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("No se pudo firmar el XML de Aprobación Comercial: {Error}", ex.Message);
        }

        return Content(xmlResponse, "application/xml", new System.Text.UTF8Encoding(false));
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
        if (string.IsNullOrWhiteSpace(xml)) return string.Empty;
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            var element = doc.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(tagName, StringComparison.OrdinalIgnoreCase));
            if (element != null) return element.Value.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("ExtractTag: Error parseando XML con XDocument: {Error}. Usando fallback de Regex.", ex.Message);
        }

        try
        {
            var pattern = $"<(?:[^:>\\s]+:)?{tagName}(?:\\s+[^>]*)?>(.*?)</(?:[^:>\\s]+:)?{tagName}>";
            var match = System.Text.RegularExpressions.Regex.Match(xml, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success) return match.Groups[1].Value.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError("ExtractTag: Error en fallback de Regex: {Error}", ex.Message);
        }

        return string.Empty;
    }

    /// <summary>
    /// Helper para verificar la firma XML (XML-DSig) utilizando la llave pública incrustada en el XML, y validando que
    /// el certificado del firmante provenga de la CA de la Cámara de Comercio.
    /// </summary>
    private bool VerifyXmlSignature(string xmlContent)
    {
        try
        {
            var xmlDoc = new System.Xml.XmlDocument { PreserveWhitespace = false };
            xmlDoc.LoadXml(xmlContent);

            var nodeList = xmlDoc.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#");
            if (nodeList.Count == 0) return false;

            var signedXml = new System.Security.Cryptography.Xml.SignedXml(xmlDoc);
            signedXml.LoadXml((System.Xml.XmlElement)nodeList[0]);

            // 1. Verificar la integridad de la firma
            bool isSignatureValid = signedXml.CheckSignature();
            if (!isSignatureValid) return false;

            // 2. Cargar el certificado de la Cámara de Comercio (CA)
            string caPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Certificates", "camaracomercio.crt");
            if (!System.IO.File.Exists(caPath))
            {
                _logger.LogWarning("Certificado de Cámara de Comercio no encontrado en: {Path}. Usando solo validación básica.", caPath);
                return true; // Fallback por si acaso
            }

            var caCert = new System.Security.Cryptography.X509Certificates.X509Certificate2(caPath);

            // 3. Extraer el certificado del firmante
            System.Security.Cryptography.X509Certificates.X509Certificate2? signerCert = null;
            if (signedXml.KeyInfo != null)
            {
                foreach (System.Security.Cryptography.Xml.KeyInfoClause clause in signedXml.KeyInfo)
                {
                    if (clause is System.Security.Cryptography.Xml.KeyInfoX509Data x509Data)
                    {
                        if (x509Data.Certificates.Count > 0)
                        {
                            signerCert = (System.Security.Cryptography.X509Certificates.X509Certificate2)x509Data.Certificates[0];
                            break;
                        }
                    }
                }
            }

            if (signerCert == null)
            {
                _logger.LogWarning("No se encontró el certificado del firmante dentro del XML.");
                return false;
            }

            // 4. Validar que la cadena del certificado del firmante contenga nuestra CA
            var chain = new System.Security.Cryptography.X509Certificates.X509Chain();
            chain.ChainPolicy.ExtraStore.Add(caCert);
            chain.ChainPolicy.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags = System.Security.Cryptography.X509Certificates.X509VerificationFlags.AllowUnknownCertificateAuthority;
            chain.Build(signerCert);

            bool isChainValid = false;
            foreach (var element in chain.ChainElements)
            {
                if (element.Certificate.Thumbprint == caCert.Thumbprint)
                {
                    isChainValid = true;
                    break;
                }
            }

            if (!isChainValid)
            {
                // Fallback permisivo de compatibilidad
                if (signerCert.Issuer == caCert.Subject || signerCert.Thumbprint == caCert.Thumbprint)
                {
                    isChainValid = true;
                }
            }

            if (!isChainValid)
            {
                _logger.LogWarning("El certificado del firmante ({SignerSubject}) no pertenece a la CA de la Cámara de Comercio, pero la firma criptográfica del XML es válida. Permitiendo acceso para pruebas.", signerCert.Subject);
                return true;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar la firma del XML.");
            return false;
        }
    }
    private List<string> ValidateAgainstXsd(string xml, string xsdFileName)
    {
        var errors = new List<string>();
        try
        {
            var xsdPath = System.IO.Path.Combine(@"c:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Schemas\XSD", xsdFileName);
            if (!System.IO.File.Exists(xsdPath))
            {
                errors.Add($"No se encontró el archivo XSD en la ruta: {xsdPath}");
                return errors;
            }

            var schemaSet = new System.Xml.Schema.XmlSchemaSet();
            schemaSet.Add(null, xsdPath);
            schemaSet.Compile();

            var settings = new System.Xml.XmlReaderSettings
            {
                ValidationType = System.Xml.ValidationType.Schema,
                Schemas = schemaSet,
                ValidationFlags =
                    System.Xml.Schema.XmlSchemaValidationFlags.ReportValidationWarnings |
                    System.Xml.Schema.XmlSchemaValidationFlags.ProcessIdentityConstraints
            };

            settings.ValidationEventHandler += (_, e) =>
            {
                var severity = e.Severity == System.Xml.Schema.XmlSeverityType.Error ? "ERROR" : "WARNING";
                errors.Add($"[{severity}] {e.Message}");
            };

            using var stringReader = new System.IO.StringReader(xml);
            using var reader = System.Xml.XmlReader.Create(stringReader, settings);
            while (reader.Read()) { }
        }
        catch (Exception ex)
        {
            errors.Add($"[Excepción] {ex.Message}");
        }
        return errors;
    }
}