namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Línea de un recibo: mensualidad del plan o excedente de un mes.</summary>
public partial class ClientPaymentItem : BaseEntity
{
    public int ClientPaymentItemId { get; set; }

    public int ClientPaymentId { get; set; }

    /// <summary><see cref="Enums.ClientPaymentItemType"/>.</summary>
    public int ItemType { get; set; }

    public decimal Amount { get; set; }

    // Snapshot del plan al momento del pago
    public int? PlanId { get; set; }

    public string PlanName { get; set; } = null!;

    // Mensualidad
    public int? MonthsCovered { get; set; }

    public decimal? GrossAmount { get; set; }

    public decimal? DiscountPercent { get; set; }

    public decimal? DiscountAmount { get; set; }

    public DateTime? PreviousNextPaymentDate { get; set; }

    public DateTime? NewNextPaymentDate { get; set; }

    // Excedente
    public int? ClientMonthlyUsageId { get; set; }

    public int? Year { get; set; }

    public int? Month { get; set; }

    public int? OverageDocuments { get; set; }

    public virtual ClientPayment ClientPayment { get; set; } = null!;

    public virtual ClientMonthlyUsage? ClientMonthlyUsage { get; set; }
}
