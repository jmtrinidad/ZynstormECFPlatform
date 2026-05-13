$path = "c:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Web.Api\Controllers\CertificationController.cs"
$content = Get-Content $path -Raw
$newText = @"
    [HttpGet("simulation/last-results/{clientGuidId}")]
    public async Task<ActionResult<CertificationJobStatusDto>> GetLastSimulationResults(string clientGuidId)
    {
        var status = await oldSimulationService.GetLastSimulationResultsByClientAsync(clientGuidId);
        return Ok(status);
    }

    [HttpGet("download/{jobId}")]
"@
$content = $content.Replace('    [HttpGet("download/{jobId}")]', $newText)
Set-Content $path $content -NoNewline
