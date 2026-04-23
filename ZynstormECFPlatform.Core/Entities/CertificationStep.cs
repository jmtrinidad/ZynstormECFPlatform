namespace ZynstormECFPlatform.Core.Entities;

public class CertificationStep : BaseEntity
{
    public int CertificationStepId { get; set; }

    public string Name { get; set; } = null!;

    public int Order { get; set; }

    public bool IsRequired { get; set; }

    public ICollection<CertificationProcess> CertificationProcesses { get; set; } = [];
}