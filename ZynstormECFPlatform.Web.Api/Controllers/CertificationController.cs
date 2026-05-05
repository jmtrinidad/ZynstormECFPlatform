using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Filters;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class CertificationController(
    ICertificationService certificationService,
    ICacheService cacheService,
    IWebHostEnvironment env,
    IClientService clientService,
    ICertificationProcessService certificationProcessService) : ControllerBase
{
    [HttpGet("clients/{clientGuidId}/progress")]
    public async Task<ActionResult<ClientCertificationProgressDto>> GetClientProgress(string clientGuidId, CancellationToken cancellationToken)
    {
        var client = await clientService.GetByAsync(x => x.GuidId == clientGuidId, cancellationToken);

        if (client == null)
            return NotFound("Cliente no encontrado.");

        var process = await certificationProcessService.GetByAsync(
            x => x.ClientId == client.ClientId && x.Status == CertificationStatus.InProgress,
            cancellationToken
        );

        if (process == null)
        {
            return Ok(new ClientCertificationProgressDto
            {
                ClientGuidId = clientGuidId,
                CurrentStep = client.IsCertified ? 15 : 1,
                CompletedSteps = client.IsCertified ? [15] : [],
                IsCertified = client.IsCertified,
            });
        }

        var currentStep = process.CurrentStepId ?? 1;

        return Ok(new ClientCertificationProgressDto
        {
            ClientGuidId = clientGuidId,
            CurrentStep = currentStep,
            CompletedSteps = Enumerable.Range(1, currentStep).ToList(),
            IsCertified = client.IsCertified || currentStep >= 15,
        });
    }

    [HttpPost("clients/{clientGuidId}/steps")]
    public async Task<ActionResult<ClientCertificationProgressDto>> RegisterClientStep(string clientGuidId, [FromBody] RegisterCertificationStepDto dto, CancellationToken cancellationToken)
    {
        if (dto.Step < 1 || dto.Step > 15)
            return BadRequest("El paso debe estar entre 1 y 15.");

        var client = await clientService.GetByAsync(x => x.GuidId == clientGuidId, cancellationToken);
        if (client == null)
            return NotFound("Cliente no encontrado.");

        var process = await certificationProcessService.GetByAsync(
            x => x.ClientId == client.ClientId && x.Status == CertificationStatus.InProgress,
            cancellationToken
        );

        if (process == null)
        {
            process = new CertificationProcess
            {
                ClientId = client.ClientId,
                Environment = DgiiEnvironment.CerteCF,
                Status = CertificationStatus.InProgress,
                CurrentStepId = dto.Step,
                StartDate = DateTime.UtcNow,
            };
            await certificationProcessService.InsertAsync(process);
        }
        else
        {
            process.CurrentStepId = Math.Max(process.CurrentStepId ?? 1, dto.Step);
            process.LastUpdateUtc = DateTime.UtcNow;
            await certificationProcessService.UpdateAsync(process);
        }

        if ((process.CurrentStepId ?? dto.Step) >= 15)
        {
            process.CurrentStepId = 15;
            process.Status = CertificationStatus.Approved;
            process.EndDate = DateTime.UtcNow;
            await certificationProcessService.UpdateAsync(process);

            client.IsCertified = true;
            client.LastUpdateUtc = DateTime.UtcNow;
            await clientService.UpdateAsync(client);
        }

        var currentStep = process.CurrentStepId ?? dto.Step;

        return Ok(new ClientCertificationProgressDto
        {
            ClientGuidId = clientGuidId,
            CurrentStep = currentStep,
            CompletedSteps = Enumerable.Range(1, currentStep).ToList(),
            IsCertified = client.IsCertified || currentStep >= 15,
        });
    }

    [HttpGet("tests")]
    public async Task<ActionResult<List<CertificationTestDto>>> GetTests()
    {
        var tests = await certificationService.GetTestsAsync();
        return Ok(tests);
    }

    [HttpPost("run/{index}")]
    public async Task<ActionResult<DgiiTransmissionResult>> RunTest(int index)
    {
        var result = await certificationService.RunTestAsync(index, env.WebRootPath);
        if (result.Success)
            return Ok(result);
        return BadRequest(result);
    }

    [ApiKeyAuth]
    [HttpGet("status/{trackId}")]
    public ActionResult<DgiiStatusResponse> GetStatus(string trackId)
    {
        string cacheKey = $"EcfStatus_{trackId}";
        var status = cacheService.Get<DgiiStatusResponse>(cacheKey);
        if (status == null)
            return NotFound("Status no encontrado o expirado.");
        return Ok(status);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CertificationSummaryDto>> GetSummary()
    {
        var summary = await certificationService.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("pruebas-datos-ecf")]
    public async Task<ActionResult<object>> PruebasDatosEcf([FromForm] IFormFile excelFile, [FromForm] string clientGuidId)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest("Debe proporcionar un archivo Excel de certificación.");

        if (string.IsNullOrWhiteSpace(clientGuidId))
            return BadRequest("Debe proporcionar el GuidId del cliente.");

        using var ms = new MemoryStream();
        await excelFile.CopyToAsync(ms);
        var jobId = await certificationService.EnqueueCertificationJobAsync(ms.ToArray(), excelFile.FileName, env.WebRootPath);

        return Ok(new { jobId, clientGuidId, step = 2, tests = Array.Empty<object>(), generatedFiles = Array.Empty<object>(), message = "Proceso de pruebas de datos e-CF iniciado en segundo plano." });
    }

    [HttpGet("ws")]
    public async Task Ws([FromQuery] string jobId, CancellationToken cancellationToken = default)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Se requiere una conexión WebSocket.", cancellationToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(jobId))
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsync("Debe proporcionar jobId.", cancellationToken);
            return;
        }

        using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        while (!cancellationToken.IsCancellationRequested && webSocket.State == WebSocketState.Open)
        {
            var status = await certificationService.GetJobStatusAsync(jobId);
            var completedSteps = status.CompletedSteps;
            var comprobantesSteps = completedSteps.Where(s => s.Step is "1" or "2").ToList();
            var resumenesSteps = completedSteps.Where(s => s.Step == "3").ToList();
            var generatedSteps = completedSteps.Where(s => s.Step == "4").ToList();
            var payload = JsonSerializer.Serialize(new
            {
                jobId,
                status = status.Status,
                step = status.CurrentStep,
                totalSteps = status.TotalSteps,
                currentNcf = status.CurrentNcf,
                logs = completedSteps,
                // Progress counters for real-time panel
                comprobantesApproved = comprobantesSteps.Count(s => s.Status == "Aceptado"),
                comprobantesTotal = status.TotalComprobantes,
                resumenesApproved = resumenesSteps.Count(s => s.Status == "Aceptado" || s.Status == "Generado"),
                resumenesTotal = status.TotalResumenes,
                generatedFiles = string.IsNullOrWhiteSpace(status.DownloadUrl)
                    ? Array.Empty<object>()
                    : new[] { new { name = $"cert_step4_{jobId}.zip", url = $"/v1/Certification/download/{jobId}" } }
            });
            var buffer = Encoding.UTF8.GetBytes(payload);
            await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            if (status.Status is "Completed" or "Failed")
                break;
        }
    }

    [HttpGet("job-status/{jobId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetJobStatus(string jobId)
    {
        var status = await certificationService.GetJobStatusAsync(jobId);
        return Ok(status);
    }

    [HttpGet("download/{jobId}")]
    public async Task<ActionResult> DownloadStep4Results(string jobId)
    {
        var status = await certificationService.GetJobStatusAsync(jobId);

        if (status.HighestCompletedStep < 3)
            return BadRequest("La descarga solo está permitida una vez que el Paso 3 (Resúmenes B2C) haya sido completado exitosamente.");

        if (string.IsNullOrEmpty(status.DownloadUrl))
            return BadRequest("El archivo aún no ha sido generado.");

        var bytes = await System.IO.File.ReadAllBytesAsync(status.DownloadUrl);
        return File(bytes, "application/zip", $"cert_step4_{jobId}.zip");
    }

    [HttpGet("job-status/{jobId}/logs")]
    public async Task<ActionResult<List<CertificationStepResultDto>>> GetJobLogs(string jobId)
    {
        var logs = await certificationService.GetJobLogsAsync(jobId);
        return Ok(logs);
    }

    [HttpPost("aprobacion-comercial")]
    public async Task<ActionResult<List<DgiiTransmissionResult>>> ProcessAprobacionComercial([FromForm] IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest("Debe proporcionar el archivo Excel 'Aprobacion comerciar.xlsx'.");

        using var ms = new MemoryStream();
        await excelFile.CopyToAsync(ms);
        var results = await certificationService.ProcessAprobacionComercialAsync(ms.ToArray());

        return Ok(results);
    }

    [HttpPost("simulacion-ecf")]
    public async Task<ActionResult<string>> SimulacionEcf([FromBody] EcfInvoiceRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Debe proporcionar los datos de la factura en formato JSON.");

        try
        {
            var jobId = await certificationService.EnqueueSimulacionEcfJobAsync(dto, env.WebRootPath);
            return Ok(new { JobId = jobId, Message = "Simulación de e-CF iniciada en segundo plano." });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("simulacion-uno-a-uno")]
    public async Task<ActionResult<string>> SimulacionUnoAUno([FromBody] EcfInvoiceRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Debe proporcionar los datos de la factura en formato JSON.");

        try
        {
            var result = await certificationService.ProcessSimulacionUnoAUnoAsync(dto, env.WebRootPath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("sign-xml")]
    public async Task<ActionResult> SignXml([FromForm] IFormFile xmlFile, [FromForm] string rnc)
    {
        if (xmlFile == null || xmlFile.Length == 0)
            return BadRequest("Debe proporcionar un archivo XML.");

        if (string.IsNullOrEmpty(rnc))
            return BadRequest("Debe proporcionar el RNC para firmar el XML.");

        try
        {
            var (content, fileName) = await certificationService.SignXmlAsync(xmlFile.OpenReadStream(), rnc);
            return File(content, "application/xml", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public class RegisterCertificationStepDto
{
    public int Step { get; set; }
}

public class ClientCertificationProgressDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public List<int> CompletedSteps { get; set; } = [];
    public bool IsCertified { get; set; }
}