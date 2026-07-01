using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Filters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class CertificationController(
    ICertificationExcelService excelService,
    ICertificationSimulationService simulationService,
    ZynstormECFPlatform.Services.Certification.OldSimulation.IOldCertificationSimulationService oldSimulationService,
    ICacheService cacheService,
    IWebHostEnvironment env,
    IClientService clientService,
    ICertificationProcessService certificationProcessService,
    ICertificationDocumentService documentService,
    ICertificationRiModelService riModelService) : ControllerBase
{
    [HttpGet("clients/{clientGuidId}/progress")]
    public async Task<ActionResult<ClientCertificationProgressDto>> GetClientProgress(string clientGuidId, CancellationToken cancellationToken)
    {
        var client = await clientService.GetByAsync(x => x.GuidId == clientGuidId, cancellationToken);

        if (client == null)
            return NotFound("Cliente no encontrado.");

        var process = await certificationProcessService.Table
            .Include(x => x.CertificationStep)
            .Where(x => x.ClientId == client.ClientId &&
                        (x.Status == CertificationStatus.InProgress ||
                         x.Status == CertificationStatus.Pending ||
                         x.Status == CertificationStatus.Rejected))
            .OrderByDescending(x => x.RegisteredAt)
            .FirstOrDefaultAsync(cancellationToken);

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

        var currentStep = process.CertificationStep?.Order ?? process.CurrentStepId ?? 1;

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
        var tests = await excelService.GetTestsAsync();
        return Ok(tests);
    }

    [HttpPost("run/{index}")]
    public async Task<ActionResult<DgiiTransmissionResult>> RunTest(int index)
    {
        var result = await excelService.RunTestAsync(index, env.WebRootPath);
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
        var summary = await excelService.GetSummaryAsync();
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

        var status = await excelService.EnqueueCertificationJobAsync(ms.ToArray(), excelFile.FileName, env.WebRootPath, clientGuidId);
        return Ok(new { jobId = status.JobId, clientGuidId, step = 2, tests = status.CompletedSteps, generatedFiles = Array.Empty<object>(), message = "Proceso de pruebas de datos e-CF iniciado en segundo plano." });
    }

    [HttpGet("business-types")]
    public async Task<ActionResult<IEnumerable<BusinessTypeDto>>> GetBusinessTypes()
    {
        return Ok(await simulationService.GetBusinessTypesAsync());
    }

    [HttpPost("business-simulation")]
    public async Task<ActionResult> StartBusinessSimulation([FromBody] StartSimulationRequestDto dto)
    {
        try
        {
            var jobId = await oldSimulationService.EnqueueBusinessSimulationJobAsync(dto.BusinessTypeGuidId, dto.ClientGuidId, env.WebRootPath);
            return Ok(new { JobId = jobId, Message = "Simulación por tipo de negocio iniciada." });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    [HttpGet("job-status/{jobId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetJobStatus(string jobId)
    {
        var status = await excelService.GetJobStatusAsync(jobId);
        return Ok(status);
    }

    [HttpGet("simulation/job-status/{jobId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetSimulationJobStatus(string jobId)
    {
        var status = await oldSimulationService.GetJobStatusAsync(jobId);
        if (status.Status == "NotFound")
            status = await simulationService.GetJobStatusAsync(jobId);

        NormalizeSimulationArtifactUrls(status, jobId);
        return Ok(status);
    }

    [HttpGet("simulation/last-results/{clientGuidId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetLastSimulationResults(string clientGuidId)
    {
        var status = await oldSimulationService.GetLastSimulationResultsByClientAsync(clientGuidId);
        return Ok(status);
    }

    [HttpGet("download/{jobId}")]
    public async Task<ActionResult> DownloadStep4Results(string jobId)
    {
        var status = await excelService.GetJobStatusAsync(jobId);

        if (status.HighestCompletedStep < 3)
            return BadRequest("La descarga solo está permitida una vez que el Paso 3 (Resúmenes B2C) haya sido completado exitosamente.");

        if (string.IsNullOrEmpty(status.DownloadUrl))
            return BadRequest("El archivo aún no ha sido generado.");

        string physicalPath = ResolveCertificationFilePath(status.DownloadUrl);

        if (!System.IO.Path.IsPathRooted(physicalPath))
        {
            physicalPath = System.IO.Path.Combine(env.WebRootPath, physicalPath.TrimStart('/'));
        }

        if (!System.IO.File.Exists(physicalPath))
            return BadRequest($"El archivo no se encontró en la ruta: {physicalPath}");

        var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(bytes, "application/zip", $"cert_step4_{jobId}.zip");
    }

    [HttpGet("simulation/download/{jobId}")]
    public async Task<ActionResult> DownloadSimulationResults(string jobId)
    {
        var status = await oldSimulationService.GetJobStatusAsync(jobId);
        if (status.Status == "NotFound")
            status = await simulationService.GetJobStatusAsync(jobId);

        if (string.IsNullOrEmpty(status.DownloadUrl))
            return BadRequest("El archivo aún no ha sido generado.");

        string physicalPath = ResolveCertificationFilePath(status.DownloadUrl);

        if (!System.IO.Path.IsPathRooted(physicalPath))
        {
            physicalPath = System.IO.Path.Combine(env.WebRootPath, physicalPath.TrimStart('/'));
        }

        if (!System.IO.File.Exists(physicalPath))
            return BadRequest($"El archivo no se encontró en la ruta: {physicalPath}");

        var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(bytes, "application/zip", $"simulacion_{jobId}.zip");
    }

    [HttpGet("simulation/download/approved/{jobId}")]
    public async Task<ActionResult> DownloadApprovedSimulationXmls(string jobId)
    {
        var status = await oldSimulationService.GetJobStatusAsync(jobId);
        if (status.Status == "NotFound")
            status = await simulationService.GetJobStatusAsync(jobId);

        NormalizeSimulationArtifactUrls(status, jobId);
        return await DownloadSimulationZipAsync(status.ApprovedXmlZipUrl, $"simulacion_aprobados_{jobId}.zip");
    }

    [HttpGet("simulation/download/manual/{jobId}")]
    public async Task<ActionResult> DownloadManualSimulationXmls(string jobId)
    {
        var status = await oldSimulationService.GetJobStatusAsync(jobId);
        if (status.Status == "NotFound")
            status = await simulationService.GetJobStatusAsync(jobId);

        NormalizeSimulationArtifactUrls(status, jobId);
        return await DownloadSimulationZipAsync(status.ManualXmlZipUrl, $"simulacion_subir_dgii_{jobId}.zip");
    }

    [HttpGet("simulation/qr-validation/{jobId}")]
    public async Task<ActionResult<List<CertificationStepResultDto>>> GetSimulationQrValidationItems(string jobId)
    {
        var status = await oldSimulationService.GetJobStatusAsync(jobId);
        if (status.Status == "NotFound")
            status = await simulationService.GetJobStatusAsync(jobId);

        var items = status.CompletedSteps
            .Where(x => !string.IsNullOrWhiteSpace(x.Ncf) &&
                        !string.IsNullOrWhiteSpace(x.SecurityCode) &&
                        !string.IsNullOrWhiteSpace(x.FechaFirma) &&
                        (x.Status.Equals("Aceptado", StringComparison.OrdinalIgnoreCase) ||
                         x.Status.Equals("GeneradoManual", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(x => x.Index)
            .ToList();

        return Ok(items);
    }

    [HttpGet("download-xml/{jobId}/{ncf}")]
    public async Task<ActionResult> DownloadIndividualXml(string jobId, string ncf)
    {
        // 1. Try to find in Excel Service (Step 2/3)
        var status = await excelService.GetJobStatusAsync(jobId);
        var stepResult = status.CompletedSteps.FirstOrDefault(x => x.Ncf == ncf && !string.IsNullOrEmpty(x.XmlFileName));

        if (stepResult != null)
        {
            // El job escribe los XML en certification_files/{jobId}/ (sin prefijo "suite_";
            // ese prefijo solo aplica al .xlsx subido).
            string jobDir = System.IO.Path.Combine(env.WebRootPath, "certification_files", jobId);
            string filePath = System.IO.Path.Combine(jobDir, stepResult.XmlFileName);

            if (System.IO.File.Exists(filePath))
            {
                var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(bytes, "application/xml", stepResult.XmlFileName);
            }
        }

        // 2. Try to find in Simulation Service (Step 4/5)
        var simStatus = await simulationService.GetJobStatusAsync(jobId);
        var simResult = simStatus.CompletedSteps.FirstOrDefault(x => x.Ncf == ncf);

        // Simulations might not save to disk, so we fall back to Database if not in disk

        // 3. Fallback to Database (persistent storage)
        var dbDoc = await documentService.Table
            .OrderByDescending(x => x.SentAt)
            .FirstOrDefaultAsync(x => x.ENcfSecuence == ncf);

        if (dbDoc != null)
        {
            var bytes = Encoding.UTF8.GetBytes(dbDoc.XmlSent);
            return File(bytes, "application/xml", $"{ncf}.xml");
        }

        return NotFound("XML no encontrado o aún no generado.");
    }

    [HttpGet("job-status/{jobId}/logs")]
    public async Task<ActionResult<List<CertificationStepResultDto>>> GetJobLogs(string jobId)
    {
        var logs = await excelService.GetJobLogsAsync(jobId);
        return Ok(logs);
    }

    [HttpGet("simulation/job-status/{jobId}/logs")]
    public async Task<ActionResult<List<CertificationStepResultDto>>> GetSimulationJobLogs(string jobId)
    {
        var logs = await oldSimulationService.GetJobLogsAsync(jobId);
        if (logs.Count == 0)
            logs = await simulationService.GetJobLogsAsync(jobId);

        return Ok(logs);
    }

    [HttpPost("aprobacion-comercial")]
    public async Task<ActionResult<object>> ProcessAprobacionComercial([FromForm] IFormFile excelFile, [FromForm] string clientGuidId)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest("Debe proporcionar el archivo Excel de aprobación comercial.");

        if (string.IsNullOrWhiteSpace(clientGuidId))
            return BadRequest("Debe proporcionar el GuidId del cliente.");

        using var ms = new MemoryStream();
        await excelFile.CopyToAsync(ms);

        var status = await excelService.EnqueueAprobacionComercialJobAsync(ms.ToArray(), excelFile.FileName, env.WebRootPath, clientGuidId);
        return Ok(new { jobId = status.JobId, clientGuidId, step = 3, tests = status.CompletedSteps, message = "Proceso de pruebas de aprobación comercial iniciado en segundo plano." });
    }

    [HttpPost("simulacion-ecf")]
    public async Task<ActionResult<string>> SimulacionEcf([FromBody] OldEcfInvoiceRequestDto dto)
    {
        if (dto == null)
            return BadRequest("Debe proporcionar los datos de la factura en formato JSON.");

        try
        {
            var jobId = await oldSimulationService.EnqueueSimulacionEcfJobAsync(dto, env.WebRootPath);
            return Ok(new { JobId = jobId, Message = "Simulación de e-CF iniciada en segundo plano (Legacy Matrix)." });
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
            var result = await simulationService.ProcessSimulacionUnoAUnoAsync(dto, env.WebRootPath);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("initialize-simulation-samples")]
    public async Task<ActionResult> InitializeSimulationSamples()
    {
        try
        {
            await simulationService.InitializeSimulationSamplesAsync();
            return Ok("Muestras de simulación inicializadas correctamente.");
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
            var (content, fileName) = await excelService.SignXmlAsync(xmlFile.OpenReadStream(), rnc);
            return File(content, "application/xml", fileName);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("print-templates")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> UploadRiModel([FromForm] IFormFile pdfFile, [FromForm] string clientGuidId,
        [FromForm] string name, [FromForm] List<string> ecfTypeCodes)
    {
        if (pdfFile == null || pdfFile.Length == 0)
            return BadRequest("Debe proporcionar un archivo PDF.");

        if (pdfFile.ContentType != "application/pdf")
            return BadRequest("El archivo debe ser PDF.");

        try
        {
            using var ms = new MemoryStream();
            await pdfFile.CopyToAsync(ms);
            var dto = await riModelService.UploadAsync(clientGuidId, name, ecfTypeCodes, ms.ToArray(), pdfFile.FileName);
            return Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("print-templates/{clientGuidId}")]
    public async Task<ActionResult> ListRiModels(string clientGuidId)
    {
        try
        {
            return Ok(await riModelService.ListByClientAsync(clientGuidId));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("print-templates/{templateGuidId}/preview")]
    public async Task<ActionResult> PreviewRiModel(string templateGuidId)
    {
        try
        {
            var bytes = await riModelService.GetReferenceRiAsync(templateGuidId);
            return bytes is null ? NotFound() : File(bytes, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("print-templates/{templateGuidId}")]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult> UpdateRiModel(string templateGuidId, [FromForm] string? name,
        [FromForm] List<string>? ecfTypeCodes, [FromForm] bool? confirm, [FromForm] IFormFile? pdfFile)
    {
        try
        {
            byte[]? src = null;
            string? fn = null;
            if (pdfFile is not null)
            {
                using var ms = new MemoryStream();
                await pdfFile.CopyToAsync(ms);
                src = ms.ToArray();
                fn = pdfFile.FileName;
            }

            return Ok(await riModelService.UpdateAsync(templateGuidId, name, ecfTypeCodes, confirm, src, fn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("print-templates/{templateGuidId}")]
    public async Task<ActionResult> DeleteRiModel(string templateGuidId)
    {
        try
        {
            await riModelService.DeleteAsync(templateGuidId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ri/{clientGuidId}/{ncf}/preview")]
    public async Task<ActionResult> PreviewRi(string clientGuidId, string ncf)
    {
        try
        {
            var bytes = await riModelService.RenderRiForDocumentAsync(clientGuidId, ncf);
            return File(bytes, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ri/{clientGuidId}/{ncf}/download")]
    public async Task<ActionResult> DownloadRi(string clientGuidId, string ncf)
    {
        try
        {
            var bytes = await riModelService.RenderRiForDocumentAsync(clientGuidId, ncf);
            return File(bytes, "application/pdf", $"RI_{ncf}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("ri/{clientGuidId}/zip")]
    public async Task<ActionResult> DownloadRiZip(string clientGuidId)
    {
        try
        {
            var bytes = await riModelService.RenderAllZipAsync(clientGuidId, env.WebRootPath);
            return File(bytes, "application/zip", $"RI_{clientGuidId}.zip");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private void NormalizeSimulationArtifactUrls(CertificationJobStatusDto status, string jobId)
    {
        status.ApprovedXmlZipUrl = FirstNonEmpty(
            status.ApprovedXmlZipUrl,
            status.DownloadUrl,
            BuildExistingCertificationFileUrl($"simulacion_aprobados_{jobId}.zip"));

        status.ManualXmlZipUrl = FirstNonEmpty(
            status.ManualXmlZipUrl,
            status.JsonDownloadUrl,
            BuildExistingCertificationFileUrl($"simulacion_subir_dgii_{jobId}.zip"));

        status.DownloadUrl = status.ApprovedXmlZipUrl;
        status.JsonDownloadUrl = status.ManualXmlZipUrl;
    }

    private async Task<ActionResult> DownloadSimulationZipAsync(string zipUrl, string downloadFileName)
    {
        if (string.IsNullOrEmpty(zipUrl))
            return BadRequest("El archivo aun no ha sido generado.");

        string physicalPath = ResolveCertificationFilePath(zipUrl);

        if (!System.IO.File.Exists(physicalPath))
            return BadRequest($"El archivo no se encontro en la ruta: {physicalPath}");

        var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(bytes, "application/zip", downloadFileName);
    }

    private string? BuildExistingCertificationFileUrl(string fileName)
    {
        var path = System.IO.Path.Combine(env.WebRootPath, "certification_files", fileName);
        return System.IO.File.Exists(path) ? $"/certification_files/{fileName}" : null;
    }

    private string ResolveCertificationFilePath(string urlOrPath)
    {
        if (System.IO.Path.IsPathRooted(urlOrPath))
            return urlOrPath;

        var relativePath = urlOrPath.TrimStart('/', '\\');
        const string staticPrefix = "certification_files/";

        if (relativePath.StartsWith(staticPrefix, StringComparison.OrdinalIgnoreCase))
            return System.IO.Path.Combine(env.WebRootPath, relativePath);

        return System.IO.Path.Combine(env.WebRootPath, "certification_files", relativePath);
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
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