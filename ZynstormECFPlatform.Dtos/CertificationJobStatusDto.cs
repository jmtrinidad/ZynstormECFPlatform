using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class CertificationJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public int TotalSteps { get; set; }
    public int CurrentStep { get; set; }
    public string CurrentNcf { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string DgiiResponse { get; set; } = string.Empty;
    public List<CertificationStepResultDto> CompletedSteps { get; set; } = new();
    public string DownloadUrl { get; set; } = string.Empty;
}

public class CertificationStepResultDto
{
    public int Index { get; set; }
    public string Ncf { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
