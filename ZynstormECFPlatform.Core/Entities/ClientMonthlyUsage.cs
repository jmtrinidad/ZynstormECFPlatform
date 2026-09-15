namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientMonthlyUsage : BaseEntity
{
    public int ClientMonthlyUsageId { get; set; }

    public int ClientId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int AcceptedDocuments { get; set; }

    // Snapshot del plan al crear la fila del mes
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal MonthlyFee { get; set; }

    public int MonthlyDocumentLimit { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Plan? Plan { get; set; }
}
