namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfStatus : BaseEntity
{
    public int EcfStatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];

    public virtual ICollection<EcfStatusHistory> EcfStatusHistories { get; set; } = [];

    public virtual ICollection<EcfTransmission> EcfTransmissions { get; set; } = [];
}