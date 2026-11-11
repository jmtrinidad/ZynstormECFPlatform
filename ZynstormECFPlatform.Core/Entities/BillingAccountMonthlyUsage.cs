namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Total mensual de comprobantes aceptados de todos los clientes de una cuenta (el pool).</summary>
public partial class BillingAccountMonthlyUsage : BaseEntity
{
    public int BillingAccountMonthlyUsageId { get; set; }

    public int BillingAccountId { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int AcceptedDocuments { get; set; }

    // Snapshot del plan de la cuenta al crear la fila del mes
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    public decimal MonthlyFee { get; set; }

    public int MonthlyDocumentLimit { get; set; }

    public virtual BillingAccount BillingAccount { get; set; } = null!;

    public virtual Plan? Plan { get; set; }
}
