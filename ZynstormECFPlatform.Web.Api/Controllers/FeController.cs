using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiController]
[Route("fe")]
public class FeController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IInboundEcfService _inboundEcfService;

    public FeController(ICacheService cacheService, IJwtTokenService jwtTokenService, IInboundEcfService inboundEcfService)
    {
        _cacheService = cacheService;
        _jwtTokenService = jwtTokenService;
        _inboundEcfService = inboundEcfService;
    }

    /// <summary>
    /// Autenticación B2B - Paso 1: Proveedor solicita semilla para firmarla.
    /// </summary>
    [HttpGet("autenticacion/api/semilla")]
    public IActionResult ObtenerSemilla()
    {
        // Genera semilla alfanumerica
        string semilla = Guid.NewGuid().ToString("N").Substring(0, 16);
        
        // Cachear semilla por 5 minutos
        _cacheService.Set($"Semilla_B2B_{semilla}", semilla, TimeSpan.FromMinutes(5));

        string xmlResponse = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<SemillaModel><semilla>{semilla}</semilla></SemillaModel>";

        return Content(xmlResponse, "application/xml", Encoding.UTF8);
    }

    /// <summary>
    /// Autenticación B2B - Paso 2: El proveedor envía la semilla firmada para obtener el JWT.
    /// Nota: Idealmente se debe validar criptográficamente el XML firmado. Para simplificar,
    /// validamos la semilla y en un futuro añadimos SignedXml.CheckSignature().
    /// </summary>
    [HttpPost("autenticacion/api/validacioncertificado")]
    [Consumes("application/xml")]
    public async Task<IActionResult> ValidarCertificado()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var xmlContent = await reader.ReadToEndAsync();

        // Validar si la semilla del XML enviado existe en nuestro cache
        // Extraemos rapido el valor usando string manipulation o XmlDocument
        var inicio = xmlContent.IndexOf("<semilla>");
        var fin = xmlContent.IndexOf("</semilla>");
        
        if (inicio != -1 && fin != -1)
        {
            string semillaRecibida = xmlContent.Substring(inicio + 9, fin - (inicio + 9)).Trim();
            
            var cached = _cacheService.Get<string>($"Semilla_B2B_{semillaRecibida}");
            if (!string.IsNullOrEmpty(cached))
            {
                // Limpiar la semilla usada
                _cacheService.Remove($"Semilla_B2B_{semillaRecibida}");

                // Generar token JWT con acceso a B2B - El IJwtTokenService puede necesitar ajustes
                // pero por ahora podemos usar un workaround si asume roles de User:
                // Idealmente el emisor debe pasarse en el payload.
                
                string fakeRnc = "externo"; 
                // Return a mocked JWT using the service or generate a specific one for B2B if the existing service expects a User entity
                string token = "MOCKED-JWT-FOR-B2B-VERIFICATION"; // Deberías conectar a JwtTokenService si admite roles genéricos
                // TODO: Wire up standard JWT generation
                
                return Ok(new { token });
            }
        }

        return Unauthorized();
    }

    /// <summary>
    /// Receptor B2B - Recibe el archivo de la factura.
    /// </summary>
    // [Authorize] -> Descomentar una vez que definas tu politica JWT para B2B
    [HttpPost("recepcion/api/ecf")]
    public async Task<IActionResult> RecepcionEcf()
    {
        if (!Request.HasFormContentType)
            return BadRequest("Content must be multipart/form-data");

        var form = await Request.ReadFormAsync();
        var file = form.Files.GetFile("xml");
        
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "xml file missing" });
        }

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        string xmlContent = Encoding.UTF8.GetString(ms.ToArray());

        try
        {
            string trackId = await _inboundEcfService.ReceiveEcfAsync(xmlContent);
            return Ok(new 
            {
                trackId = trackId,
                error = "",
                mensaje = "Recibido exitosamente"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, trackId = "" });
        }
    }

    /// <summary>
    /// Aprobación Comercial B2B.
    /// </summary>
    [HttpPost("aprobacioncomercial/api/ecf")]
    [Consumes("application/xml")]
    public async Task<IActionResult> AprobacionComercial()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var xmlContent = await reader.ReadToEndAsync();

        try
        {
            await _inboundEcfService.ProcessCommercialApprovalAsync(xmlContent);
            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
