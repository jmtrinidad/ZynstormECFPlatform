namespace ZynstormECFPlatform.Core.Entities;

public class BusinessSimulationSample : BaseEntity
{
    public int BusinessSimulationSampleId { get; set; }
    public int BusinessTypeId { get; set; }
    public string EcfType { get; set; } = string.Empty; // 31, 32, 33, 34, 41, 43, 44, 45, 46, 47
    public string JsonData { get; set; } = string.Empty; // JSON structure for EcfInvoiceRequestDto
    public virtual BusinessType BusinessType { get; set; } = null!;
}
