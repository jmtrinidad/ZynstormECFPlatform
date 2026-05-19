using Asp.Versioning;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>
/// Controller independiente para la validación exhaustiva de XMLs e-CF. Desacoplado de la lógica de generación y
/// transmisión.
/// </summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class EcfXmlValidationController : ControllerBase
{
    private readonly IEcfXmlValidationService _validationService;

    public EcfXmlValidationController(IEcfXmlValidationService validationService)
    {
        _validationService = validationService;
    }

    /// <summary>
    /// Recibe un XML (e-CF) ya sea de forma cruda (Raw Body) o como un archivo (form-data). Responde con TrackId y
    /// ejecuta la validacion en segundo plano para replicar el flujo DGII.
    /// </summary>
    [HttpPost("validate")]
    [Consumes("application/xml", "text/xml", "multipart/form-data")]
    [Produces("application/json")]
    public async Task<ActionResult<EcfXmlValidationReceipt>> ValidateXml(IFormFile? xml)
    {
        try
        {
            string xmlContent;

            if (xml != null)
            {
                using var reader = new StreamReader(xml.OpenReadStream());
                xmlContent = await reader.ReadToEndAsync();
            }
            else
            {
                using var reader = new StreamReader(Request.Body);
                xmlContent = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(xmlContent))
            {
                return BadRequest(new { error = "No se proporciono contenido XML. Envie el XML en el body o como un archivo en el campo 'xml'." });
            }

            var trackId = $"VAL-{Guid.NewGuid():N}".ToUpperInvariant();
            var receipt = _validationService.RegisterReceived(trackId);

            BackgroundJob.Enqueue<IEcfXmlValidationService>(x => x.ProcessValidationJobAsync(trackId, xmlContent));

            return Accepted(receipt);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error interno recibiendo el XML.", details = ex.Message });
        }
    }

    /// <summary>
    /// Consulta el estado de una validacion asincrona por TrackId.
    /// </summary>
    [HttpGet("estado/{trackId}")]
    [Produces("application/json")]
    public ActionResult<EcfXmlValidationTrackStatus> GetEstadoByRoute(string trackId)
    {
        return GetEstado(trackId);
    }

    /// <summary>
    /// Consulta el estado de una validacion asincrona por TrackId, compatible con consultas tipo DGII.
    /// </summary>
    [HttpGet("estado")]
    [Produces("application/json")]
    public ActionResult<EcfXmlValidationTrackStatus> GetEstado([FromQuery] string? trackId)
    {
        if (string.IsNullOrWhiteSpace(trackId))
            return BadRequest(new { error = "Debe proporcionar el TrackId." });

        var status = _validationService.GetStatus(trackId.Trim().ToUpperInvariant());
        if (status == null)
            return NotFound(new { error = $"El TrackId '{trackId}' no existe o expiro del cache." });

        return Ok(status);
    }

    /// <summary>
    /// Alias de consulta por TrackId para clientes que usan rutas similares a DGII.
    /// </summary>
    [HttpGet("trackid/{trackId}")]
    [Produces("application/json")]
    public ActionResult<EcfXmlValidationTrackStatus> GetTrackId(string trackId)
    {
        return GetEstado(trackId);
    }

    /// <summary>
    /// Consulta el estado de un e-NCF previamente validado con éxito. Replica los parámetros que recibe el portal de la
    /// DGII. Si se solicita desde un navegador (Accept: text/html), devuelve una vista premium.
    /// </summary>
    [HttpGet("verificacion")]
    [Produces("application/json", "text/html")]
    public ActionResult GetVerificacion(
        [FromQuery] string? rncEmisor,
        [FromQuery] string? rncComprador,
        [FromQuery] string? eNcf,
        [FromQuery] string? encf,
        [FromQuery] string? fechaEmision,
        [FromQuery] string? montoTotal,
        [FromQuery] string? fechaFirma,
        [FromQuery] string? codigoSeguridad)
    {
        var targetNcf = eNcf ?? encf;

        if (string.IsNullOrWhiteSpace(targetNcf))
            return BadRequest(new { error = "Debe proporcionar un e-NCF (parametro 'eNcf' o 'encf')." });

        var info = _validationService.GetVerificacion(targetNcf.Trim().ToUpper());

        // Detectar si la petición viene de un navegador
        var acceptHeader = Request.Headers.Accept.ToString();
        bool isBrowser = acceptHeader.Contains("text/html");

        if (info == null)
        {
            if (isBrowser) return Content(GetNotFoundHtml(targetNcf), "text/html");
            return NotFound(new { error = $"El e-NCF '{targetNcf}' no ha sido validado recientemente o no existe en la caché." });
        }

        if (isBrowser)
        {
            return Content(GenerateVerificationHtml(info), "text/html");
        }

        return Ok(info);
    }

    private string GenerateVerificationHtml(EcfVerificacionInfo info)
    {
        bool isSimplified = (info.EcfType == 32 && decimal.TryParse(info.MontoTotal, out var m) && m < 250000m);

        var rows = new List<(string Label, string? Value)>
        {
            ("RNC Emisor", info.RncEmisor),
            ("Razón Social", info.RazonSocialEmisor)
        };

        if (!isSimplified)
        {
            rows.Add(("RNC Comprador", info.RncComprador));
            rows.Add(("Razón Social comprador", info.RazonSocialComprador));
        }

        rows.Add(("e-NCF", info.ENcf));

        if (!isSimplified)
        {
            rows.Add(("Fecha de Emisión", info.FechaEmision));
            rows.Add(("Total de ITBIS", info.TotalItbis));
            rows.Add(("Monto Total", info.MontoTotal));
        }

        rows.Add(("Estado", info.Estado));

        var rowHtml = string.Join("\n", rows.Select(r => $@"
            <tr>
                <td class='label'>{r.Label}</td>
                <td class='value'>{r.Value ?? "---"}</td>
            </tr>"));

        return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Verificación e-NCF | Zynstorm Simulation</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary: #1a73e8;
            --bg: #f8f9fa;
            --text: #202124;
            --border: #dadce0;
        }}
        body {{
            font-family: 'Inter', sans-serif;
            background-color: var(--bg);
            color: var(--text);
            margin: 0;
            display: flex;
            justify-content: center;
            padding: 20px;
        }}
        .container {{
            background: white;
            width: 100%;
            max-width: 500px;
            border-radius: 8px;
            box-shadow: 0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24);
            overflow: hidden;
            padding-bottom: 20px;
        }}
        .header {{
            padding: 20px;
            border-bottom: 1px solid var(--border);
        }}
        .logo {{
            height: 30px;
            margin-bottom: 15px;
            display: flex;
            align-items: center;
            font-weight: 600;
            color: var(--primary);
        }}
        h1 {{
            font-size: 1.2rem;
            margin: 0;
            font-weight: 600;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
        }}
        tr {{
            border-bottom: 1px solid #f1f3f4;
        }}
        td {{
            padding: 12px 20px;
            font-size: 0.95rem;
        }}
        .label {{
            color: #5f6368;
            width: 40%;
            background-color: #fafafa;
        }}
        .value {{
            font-weight: 400;
            color: #3c4043;
        }}
        .status-accepted {{
            color: #1e8e3e;
            font-weight: 600;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>
                <svg width='24' height='24' viewBox='0 0 24 24' fill='currentColor' style='margin-right:8px'>
                    <path d='M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z'/>
                </svg>
                Zynstorm Platform
            </div>
            <h1>Verificación e-NCF</h1>
        </div>
        <table>
            {rowHtml}
        </table>
    </div>
</body>
</html>";
    }

    private string GetNotFoundHtml(string ncf)
    {
        return $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>e-NCF No Encontrado</title>
    <style>
        body {{ font-family: sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; background: #f8f9fa; }}
        .card {{ background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); text-align: center; max-width: 400px; }}
        h1 {{ color: #d93025; font-size: 1.5rem; }}
        p {{ color: #5f6368; }}
        .ncf {{ font-weight: bold; color: #202124; }}
    </style>
</head>
<body>
    <div class='card'>
        <h1>Documento No Encontrado</h1>
        <p>El e-NCF <span class='ncf'>{ncf}</span> no ha sido validado recientemente en nuestra plataforma de simulación.</p>
        <p>Por favor, realice la validación del XML primero.</p>
    </div>
</body>
</html>";
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