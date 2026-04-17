using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class CertificationTestDto
{
    public int Index { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string EcfType { get; set; } = string.Empty;
    public string ENcf { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int Step { get; set; } // 1, 2, 3, 4
    public string Description { get; set; } = string.Empty;
    public TestStatus Status { get; set; } = TestStatus.Pending;
    public string? TrackId { get; set; }
    public string? Error { get; set; }
}

public enum TestStatus
{
    Pending,
    Running,
    Passed,
    Failed
}

public class CertificationSummaryDto
{
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int FailedTests { get; set; }
    public List<CertificationTestDto> Tests { get; set; } = new();
}
