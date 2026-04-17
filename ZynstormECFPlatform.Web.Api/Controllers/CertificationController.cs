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

    public CertificationController(ICertificationService certificationService, ICacheService cacheService)
    {
        _certificationService = certificationService;
        _cacheService = cacheService;
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
        5, 18,19,20,25,26,27
        */
        var result = await _certificationService.RunTestAsync(index);

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
}