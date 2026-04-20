using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class CertificationController : ControllerBase
{
    private readonly ICertificationService _certificationService;
    private readonly ICacheService _cacheService;
    private readonly IWebHostEnvironment _env;

    public CertificationController(ICertificationService certificationService, ICacheService cacheService, IWebHostEnvironment env)
    {
        _certificationService = certificationService;
        _cacheService = cacheService;
        _env = env;
    }

    [HttpGet("tests")]
    public async Task<ActionResult<List<CertificationTestDto>>> GetTests()
    {
        var tests = await _certificationService.GetTestsAsync();

        return Ok(tests);
    }

    [HttpPost("run/{index}")]
    public async Task<ActionResult<DgiiTransmissionResult>> RunTest(int index)
    {
        /*
        CON PROBLEMAS:
        18,19,20,25,26,27
        */
        var result = await _certificationService.RunTestAsync(index, _env.WebRootPath);

        if (result.Success)
            return Ok(result);

        return BadRequest(result);
    }

    [HttpGet("status/{trackId}")]
    public ActionResult<DgiiStatusResponse> GetStatus(string trackId)
    {
        string cacheKey = $"EcfStatus_{trackId}";
        var status = _cacheService.Get<DgiiStatusResponse>(cacheKey);

        if (status == null)
            return NotFound("Status no encontrado o expirado.");

        return Ok(status);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CertificationSummaryDto>> GetSummary()
    {
        var summary = await _certificationService.GetSummaryAsync();
        return Ok(summary);
    }

    [HttpPost("automate")]
    public async Task<ActionResult<string>> AutomateCertification([FromForm] IFormFile excelFile)
    {
        if (excelFile == null || excelFile.Length == 0)
            return BadRequest("Debe proporcionar un archivo Excel de certificación.");

        using var ms = new MemoryStream();
        await excelFile.CopyToAsync(ms);
        var jobId = await _certificationService.EnqueueCertificationJobAsync(ms.ToArray(), excelFile.FileName, _env.WebRootPath);

        return Ok(new { JobId = jobId, Message = "Proceso de certificación iniciado en segundo plano." });
    }

    [HttpGet("job-status/{jobId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetJobStatus(string jobId)
    {
        var status = await _certificationService.GetJobStatusAsync(jobId);
        return Ok(status);
    }

    [HttpGet("download/{jobId}")]
    public async Task<ActionResult> DownloadStep4Results(string jobId)
    {
        var status = await _certificationService.GetJobStatusAsync(jobId);

        if (status.Status != "Completed" || string.IsNullOrEmpty(status.DownloadUrl))
            return BadRequest("El archivo aún no está listo o el proceso falló.");

        var bytes = await System.IO.File.ReadAllBytesAsync(status.DownloadUrl);
        return File(bytes, "application/zip", $"cert_step4_{jobId}.zip");
    }

    [HttpGet("job-status/{jobId}/logs")]
    public async Task<ActionResult<List<CertificationStepResultDto>>> GetJobLogs(string jobId)
    {
        var logs = await _certificationService.GetJobLogsAsync(jobId);
        return Ok(logs);
    }
}