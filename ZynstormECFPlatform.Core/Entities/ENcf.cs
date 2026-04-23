namespace ZynstormECFPlatform.Core.Entities;

public class ENcf : BaseEntity
{
    public int ENcfId { get; set; }

    public int NcfTypeId { get; set; }

    public int Sequence { get; set; }

    public virtual EcfType EcfType { get; set; } = null!;

    public virtual ICollection<CertificationDocument> CertificationDocuments { get; set; } = [];
}