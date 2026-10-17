using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>Tipos de cliente y biblioteca de formatos HTML de Representación Impresa.</summary>
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize]
public class RiFormatController(IRiFormatService formatService) : ControllerBase
{
    [HttpGet("client-types")]
    public async Task<ActionResult<IReadOnlyList<ClientTypeDto>>> ListClientTypes() =>
        Ok(await formatService.ListClientTypesAsync());

    [HttpPost("client-types")]
    public Task<ActionResult> CreateClientType([FromBody] ClientTypeSaveDto dto) =>
        Run(async () => Ok(await formatService.CreateClientTypeAsync(dto)));

    [HttpPut("client-types/{guidId}")]
    public Task<ActionResult> UpdateClientType(string guidId, [FromBody] ClientTypeSaveDto dto) =>
        Run(async () => Ok(await formatService.UpdateClientTypeAsync(guidId, dto)));

    [HttpDelete("client-types/{guidId}")]
    public Task<ActionResult> DeleteClientType(string guidId) =>
        Run(async () =>
        {
            await formatService.DeleteClientTypeAsync(guidId);
            return NoContent();
        });

    [HttpPost("client-types/{guidId}/templates/{templateGuidId}")]
    public Task<ActionResult> AddTemplateToClientType(string guidId, string templateGuidId) =>
        Run(async () => Ok(await formatService.AddTemplateToClientTypeAsync(guidId, templateGuidId)));

    [HttpDelete("client-types/{guidId}/templates/{templateGuidId}")]
    public Task<ActionResult> RemoveTemplateFromClientType(string guidId, string templateGuidId) =>
        Run(async () => Ok(await formatService.RemoveTemplateFromClientTypeAsync(guidId, templateGuidId)));

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<RiHtmlTemplateDto>>> ListTemplates() =>
        Ok(await formatService.ListTemplatesAsync());

    [HttpPost("templates")]
    public Task<ActionResult> UploadTemplate([FromBody] RiHtmlTemplateUploadDto dto) =>
        Run(async () =>
        {
            var result = await formatService.UploadTemplateAsync(dto);
            return result.Errors.Count > 0 ? UnprocessableEntity(result) : Ok(result);
        });

    [HttpDelete("templates/{guidId}")]
    public Task<ActionResult> DeleteTemplate(string guidId) =>
        Run(async () =>
        {
            await formatService.DeleteTemplateAsync(guidId);
            return NoContent();
        });

    [HttpGet("templates/sample")]
    public ActionResult DownloadSample() =>
        File(Encoding.UTF8.GetBytes(SampleHtml), "text/html", "formato-ri-ejemplo.html");

    [HttpPut("clients/{clientGuidId}/assignment")]
    public Task<ActionResult> Assign(string clientGuidId, [FromBody] ClientRiFormatAssignDto dto) =>
        Run(async () =>
        {
            await formatService.AssignToClientAsync(clientGuidId, dto);
            return NoContent();
        });

    [HttpGet("clients/{clientGuidId}/resolved")]
    public Task<ActionResult> Resolved(string clientGuidId) =>
        Run(async () => Ok(await formatService.GetResolvedAsync(clientGuidId)));

    [HttpGet("clients/{clientGuidId}/logo")]
    public Task<ActionResult> GetLogo(string clientGuidId) =>
        Run(async () => Ok(await formatService.GetClientLogoAsync(clientGuidId)));

    [HttpPut("clients/{clientGuidId}/logo")]
    public Task<ActionResult> SetLogo(string clientGuidId, [FromBody] ClientLogoDto dto) =>
        Run(async () =>
        {
            await formatService.SetClientLogoAsync(clientGuidId, dto);
            return NoContent();
        });

    private static async Task<ActionResult> Run(Func<Task<ActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(ex.Message);
        }
    }

    /// <summary>HTML de ejemplo con todos los marcadores soportados.</summary>
    private const string SampleHtml = """
<!DOCTYPE html>
<html><head><meta charset="utf-8"><title>Factura</title>
<style>
 body{font-family:Arial,sans-serif;width:300px;margin:0 auto}
 .row{display:flex;justify-content:space-between}
 @media print{body{width:auto}}
</style></head>
<body>
 <div style="border:1px dashed #000;height:60px;text-align:center">{{EmisorLogo}}</div>
 <div style="text-align:center"><b>{{EmisorNombre}}</b></div>
 <div style="text-align:center">{{EmisorDireccion}}</div>
 <div style="text-align:center"><b>RNC: {{EmisorRNC}}</b></div>
 <div style="text-align:center"><b>Tel.: {{EmisorTelefono}}</b></div>
 <h2><u>FACTURA</u></h2>
 <div style="text-align:center"><b>{{TipoComprobante}}</b></div>
 <div class="row"><span>eNCF:</span><span>{{eNCF}}</span></div>
 <div class="row"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
 <div class="row"><span>e-NCF Modificado:</span><span>{{NcfModificado}}</span></div>
 <div class="row"><span>Modificación:</span><span>{{CodigoModificacion}}</span></div>
 <div class="row"><span>No. Factura:</span><b>{{NoFactura}}</b></div>
 <div class="row"><span>Fecha:</span><span>{{Fecha}}</span></div>
 <div class="row"><span>Nombre:</span><b>{{Nombre}}</b></div>
 <div class="row"><span>Forma Pago:</span><b>{{FormaPago}}</b></div>
 <table style="width:100%">
  <thead><tr><th align="left">Cant.</th><th align="left">Descripción</th><th align="right">Precio</th><th align="right">ITBIS</th><th align="right">Valor</th></tr></thead>
  <tbody><tr><td>{{Cantidad}} {{Unidad}}</td><td><b>{{Exento}}</b> {{Descripcion}}</td><td align="right">{{Precio}}</td><td align="right">{{ITBIS}}</td><td align="right">{{Monto}}</td></tr></tbody>
 </table>
 <div class="row"><b>Subtotal gravado:</b><b>{{SubtotalGravado}}</b></div>
 <div class="row"><b>Subtotal exento:</b><b>{{SubtotalExento}}</b></div>
 <div class="row"><b>Total:</b><b>{{Total}}</b></div>
 <div class="row"><b>Total Descuento:</b><b>{{TotalDescuento}}</b></div>
 <div style="border:1px dashed #000;width:110px;height:110px;margin:12px auto">{{QR}}</div>
 <div style="text-align:center"><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
 <div style="text-align:center"><b>Fecha firma digital:</b> {{FechaFirma}}</div>
 <div style="text-align:center"><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</body></html>
""";
}
