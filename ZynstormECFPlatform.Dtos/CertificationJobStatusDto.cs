using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class CertificationJobStatusDto
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Completed, Failed
    public int TotalSteps { get; set; }
    public int TotalComprobantes { get; set; }
    public int TotalResumenes { get; set; }
    public int CurrentStep { get; set; }
    public string CurrentNcf { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string DgiiResponse { get; set; } = string.Empty;
    public List<CertificationStepResultDto> CompletedSteps { get; set; } = new();
    public int HighestCompletedStep { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string JsonDownloadUrl { get; set; } = string.Empty;
    public SimulationStatsDto SimulationStats { get; set; } = new();
}

public class SimulationStatsDto
{
    public int Type31 { get; set; }
    public int Type32Greater250k { get; set; }
    public int Type32Rfce { get; set; }
    public int Type33 { get; set; }
    public int Type34 { get; set; }
    public int Type41 { get; set; }
    public int Type43 { get; set; }
    public int Type44 { get; set; }
    public int Type45 { get; set; }
    public int Type46 { get; set; }
    public int Type47 { get; set; }

    // Expected Totals
    public int TotalType31 { get; set; } = 4;
    public int TotalType32Greater250k { get; set; } = 2;
    public int TotalType32Rfce { get; set; } = 4;
    public int TotalType33 { get; set; } = 1;
    public int TotalType34 { get; set; } = 2;
    public int TotalType41 { get; set; } = 2;
    public int TotalType43 { get; set; } = 2;
    public int TotalType44 { get; set; } = 2;
    public int TotalType45 { get; set; } = 2;
    public int TotalType46 { get; set; } = 2;
    public int TotalType47 { get; set; } = 2;
}

public class CertificationStepResultDto
{
    public int Index { get; set; }
    public string Ncf { get; set; } = string.Empty;
    public string Step { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string XmlFileName { get; set; } = string.Empty;
    public decimal? Amount { get; set; }
    public string SecurityCode { get; set; } = string.Empty;
    public string FechaFirma { get; set; } = string.Empty;
    public string FechaEmision { get; set; } = string.Empty;
    public string BuyerRnc { get; set; } = string.Empty;
}
