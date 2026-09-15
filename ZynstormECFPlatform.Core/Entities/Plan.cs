namespace ZynstormECFPlatform.Core.Entities;

public partial class Plan : BaseEntity
{
    public int PlanId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal MonthlyFee { get; set; }

    /// <summary>Cantidad de comprobantes incluidos al mes. -1 = ilimitado.</summary>
    public int MonthlyDocumentLimit { get; set; }

    /// <summary>Tipo de plan: 1 = Comprobantes, 2 = Renta (<see cref="Enums.PlanTypeEnum"/>).</summary>
    public int PlanTypeId { get; set; } = (int)Enums.PlanTypeEnum.Documents;

    /// <summary>Usuarios permitidos al mismo tiempo. Solo planes de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    public int StatusId { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<PlanOverageTier> OverageTiers { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}
