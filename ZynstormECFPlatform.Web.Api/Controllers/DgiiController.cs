using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public sealed class DgiiController(IDgiiRncService dgiiRncService) : ControllerBase
{
    [HttpGet("rnc-status")]
    [ProducesResponseType<DgiiRncStatusDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DgiiRncStatusDto>> GetStatus(CancellationToken cancellationToken = default)
    {
        var status = await dgiiRncService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    // Bajar y reindexar el padron pesa ~60 MB y lo comparten todos los clientes de la
    // plataforma: solo lo dispara la administracion.
    [HttpPost("download-rnc")]
    [Authorize(Roles = "SA,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StartDownload()
    {
        // Sin el token de la peticion: la corrida sigue en segundo plano despues de
        // responder y cancelarla al cerrar la conexion la cortaria a medias.
        await dgiiRncService.StartDownloadAndIndexingAsync(CancellationToken.None);
        return Ok(new { Message = "La descarga e indexación del RNC de la DGII se ha iniciado en segundo plano." });
    }

    [HttpGet("consult-rnc/{rnc}")]
    [ProducesResponseType<DgiiRncSearchResultDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DgiiRncSearchResultDto>> ConsultRnc(string rnc, CancellationToken cancellationToken = default)
    {
        var result = await dgiiRncService.LookupRncAsync(rnc, cancellationToken);
        return Ok(result);
    }
}
