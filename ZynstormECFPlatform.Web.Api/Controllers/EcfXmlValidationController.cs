using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Filters;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>
/// Controller independiente para la validación exhaustiva de XMLs e-CF.
/// Desacoplado de la lógica de generación y transmisión.
/// </summary>
[Route("api/v1/[controller]")]
[ApiController]
[ApiKeyAuth] // Requiere un API Key válido en el header X-Api-Key
public class EcfXmlValidationController : ControllerBase
{
    private readonly IEcfXmlValidationService _validationService;

    public EcfXmlValidationController(IEcfXmlValidationService validationService)
    {
        _validationService = validationService;
    }

    /// <summary>
    /// Recibe un XML crudo (e-CF) y ejecuta las 4 capas de validación.
    /// Si el XML es válido, se almacena en memoria para futuras consultas (estilo DGII).
    /// </summary>
    /// <returns>EcfXmlValidationResult con el desglose de errores por capa.</returns>
    [HttpPost("validate")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/json")]
    public async Task<ActionResult<EcfXmlValidationResult>> ValidateXml()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var xmlContent = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return BadRequest(new { error = "El cuerpo de la petición está vacío. Debe enviar un XML." });
            }

            var result = _validationService.Validate(xmlContent);

            // Si es válido retornamos 200 OK. Si hay errores retornamos 400 BadRequest
            // para que el cliente sepa inmediatamente que falló.
            if (result.IsValid)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error interno durante la validación.", details = ex.Message });
        }
    }

    /// <summary>
    /// Consulta el estado de un e-NCF previamente validado con éxito.
    /// Simula la pantalla "Verificación e-NCF" de la DGII.
    /// </summary>
    /// <param name="eNcf">El e-NCF a consultar (ej: E310000000001)</param>
    [HttpGet("verificacion/{eNcf}")]
    [Produces("application/json")]
    public ActionResult<EcfVerificacionInfo> GetVerificacion(string eNcf)
    {
        if (string.IsNullOrWhiteSpace(eNcf))
            return BadRequest(new { error = "Debe proporcionar un e-NCF válido." });

        var info = _validationService.GetVerificacion(eNcf.Trim().ToUpper());
        
        if (info == null)
        {
            return NotFound(new { error = $"El e-NCF '{eNcf}' no ha sido validado recientemente o no existe en la caché." });
        }

        return Ok(info);
    }

    /// <summary>
    /// Retorna el historial reciente de todos los XMLs que pasaron la validación exitosamente.
    /// </summary>
    [HttpGet("verificaciones-recientes")]
    [Produces("application/json")]
    public ActionResult<List<EcfVerificacionInfo>> GetVerificacionesRecientes()
    {
        var list = _validationService.GetAllVerificaciones();
        return Ok(list);
    }
}
