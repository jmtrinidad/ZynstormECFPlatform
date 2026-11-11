namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Cuenta de un revendedor: tiene un plan de comprobantes cuyo total mensual comparten todos sus clientes.
/// Los campos de pago son los mismos que <see cref="Client"/> tiene para un cliente suelto.
/// </summary>
public partial class BillingAccount : BaseEntity
{
    public int BillingAccountId { get; set; }

    public string Name { get; set; } = null!;

    /// <summary>Usuario Admin dueño de la cuenta. Una cuenta por usuario.</summary>
    public string OwnerUserId { get; set; } = null!;

    public int PlanId { get; set; }

    public int StatusId { get; set; }

    /// <summary>Precio mensual pactado. Reemplaza el precio del plan y su oferta. Null = usa el precio del plan.</summary>
    public decimal? CustomMonthlyFee { get; set; }

    /// <summary>Fecha calendario en que empezó la oferta del plan para esta cuenta. Null = sin oferta.</summary>
    public DateTime? OfferStartDate { get; set; }

    public DateTime? LastPaymentDate { get; set; }

    public DateTime? NextPaymentDate { get; set; }

    /// <summary>Meses que cubre el pago registrado. Por defecto 1.</summary>
    public int PaidMonths { get; set; } = 1;

    /// <summary>Descuento por pago adelantado, en porcentaje (0–100).</summary>
    public decimal PrepaymentDiscountPercent { get; set; }

    /// <summary>Días después de la fecha de pago para pagar antes de la suspensión. Por defecto 3.</summary>
    public int PaymentGraceDays { get; set; } = 3;

    public DateTime? FirstPaymentReminderSentFor { get; set; }

    public DateTime? FinalPaymentReminderSentFor { get; set; }

    /// <summary>Momento en que la cuenta fue suspendida por falta de pago. Null = no suspendida.</summary>
    public DateTime? PaymentSuspendedAtUtc { get; set; }

    public virtual Plan Plan { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Client> Clients { get; set; } = [];

    public virtual ICollection<BillingAccountMonthlyUsage> MonthlyUsages { get; set; } = [];

    public virtual ICollection<ClientPayment> Payments { get; set; } = [];
}
