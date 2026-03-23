namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfType : BaseEntity
{
    public int EcfTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];
}