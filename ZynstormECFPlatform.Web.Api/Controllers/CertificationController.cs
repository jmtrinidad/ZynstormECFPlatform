using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CertificationController(ICertificationService certificationService, ICacheService cacheService, IWebHostEnvironment env) : ControllerBase
{
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

    [HttpPost("automate")]
    public async Task<ActionResult<string>> AutomateCertification([FromForm] IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest("Debe proporcionar un archivo Excel de certificación.");

        using var ms = new MemoryStream();
        await excelFile.CopyToAsync(ms);
        var jobId = await certificationService.EnqueueCertificationJobAsync(ms.ToArray(), excelFile.FileName, env.WebRootPath);

        return Ok(new { JobId = jobId, Message = "Proceso de certificación iniciado en segundo plano." });
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

    [HttpGet("ejemplos-json")]
    public ActionResult<List<object>> GetEjemplosJson()
    {
        try
        {
            string folder = Path.Combine(env.ContentRootPath, "..", "SamplePayloads");
            if (!Directory.Exists(folder))
            {
                folder = Path.Combine(env.ContentRootPath, "SamplePayloads");
            }

            if (!Directory.Exists(folder))
                return NotFound("Carpeta de ejemplos no encontrada.");

            var files = Directory.GetFiles(folder, "*.json")
                .OrderBy(f => Path.GetFileName(f))
                .ToList();

            var result = new List<object>();

            foreach (var file in files)
            {
                var jsonContent = System.IO.File.ReadAllText(file);
                var obj = System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent);
                if (obj != null)
                    result.Add(obj);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("files")]
    public ActionResult<List<string>> ListCertificationFiles()
    {
        string folder = Path.Combine(env.WebRootPath, "certification_files");
        if (!Directory.Exists(folder)) return Ok(new List<string>());

        var files = Directory.GetFiles(folder)
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f))
            .Cast<string>()
            .ToList();
        return Ok(files);
    }

    [HttpGet("files/{fileName}")]
    public ActionResult DownloadFile(string fileName)
    {
        // Sanitize fileName to prevent path traversal
        string safeFileName = Path.GetFileName(fileName);
        string folder = Path.Combine(env.WebRootPath, "certification_files");
        string filePath = Path.Combine(folder, safeFileName);

        if (!System.IO.File.Exists(filePath)) return NotFound("Archivo no encontrado.");

        var bytes = System.IO.File.ReadAllBytes(filePath);
        string contentType = safeFileName.EndsWith(".zip") ? "application/zip" : "text/xml";
        return File(bytes, contentType, safeFileName);
    }

    [HttpGet("simulacion/download/{jobId}")]
    public async Task<ActionResult> DownloadSimulacionZip(string jobId)
    {
        var status = await certificationService.GetJobStatusAsync(jobId);

        if (string.IsNullOrEmpty(status.DownloadUrl))
            return BadRequest("El archivo ZIP de simulación aún no ha sido generado o el JobId es inválido.");

        // The URL is relative like /certification_files/simulacion_xxx.zip
        string relativePath = status.DownloadUrl.TrimStart('/');
        string fullPath = Path.Combine(env.WebRootPath, relativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("El archivo ZIP de simulación no se encontró en el servidor.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, "application/zip", $"simulacion_{jobId}.zip");
    }

    /// <summary>
    /// Endpoint para descargar todos los archivos .json generados para la simulación para tenerlos de ejemplo para
    /// enviarlos.
    /// </summary>
    /// <param name="jobId">Identificador del trabajo de simulación</param>
    /// <returns>Archivo ZIP con los payloads JSON</returns>
    [HttpGet("simulacion/download-json/{jobId}")]
    public async Task<ActionResult> DownloadSimulacionJsonZip(string jobId)
    {
        var status = await certificationService.GetJobStatusAsync(jobId);

        if (string.IsNullOrEmpty(status.JsonDownloadUrl))
            return BadRequest("El archivo ZIP de JSONs de simulación aún no ha sido generado o el JobId es inválido.");

        // El URL es relativo como /certification_files/simulacion_json_xxx.zip
        string relativePath = status.JsonDownloadUrl.TrimStart('/');
        string fullPath = Path.Combine(env.WebRootPath, relativePath);

        if (!System.IO.File.Exists(fullPath))
            return NotFound("El archivo ZIP de JSONs de simulación no se encontró en el servidor.");

        var bytes = await System.IO.File.ReadAllBytesAsync(fullPath);
        return File(bytes, "application/zip", $"simulacion_json_{jobId}.zip");
    }
}