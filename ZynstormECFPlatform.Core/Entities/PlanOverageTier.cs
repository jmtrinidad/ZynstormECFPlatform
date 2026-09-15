namespace ZynstormECFPlatform.Core.Entities;

public partial class PlanOverageTier : BaseEntity
{
    public int PlanOverageTierId { get; set; }

    public int PlanId { get; set; }

    public int FromUnit { get; set; }

    /// <summary>Null = sin tope superior.</summary>
    public int? ToUnit { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Plan Plan { get; set; } = null!;
}
