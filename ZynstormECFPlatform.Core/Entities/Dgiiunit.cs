namespace ZynstormECFPlatform.Core.Entities;

public partial class DGIIUnit : BaseEntity
{
    public int DGIIUnitId { get; set; }

    public int DGIICode { get; set; }

    public string Name { get; set; } = null!;
}