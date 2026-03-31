namespace ZynstormECFPlatform.Core.Entities;

public partial class Currency : BaseEntity
{
    public int CurrencyId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];
}