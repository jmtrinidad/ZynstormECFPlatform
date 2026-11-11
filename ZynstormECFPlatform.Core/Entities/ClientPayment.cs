namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Recibo de un pago recibido de un cliente suelto o de una cuenta de facturación (exactamente uno de los dos).
/// Sus líneas indican qué cubre.
/// </summary>
public partial class ClientPayment : BaseEntity
{
    public int ClientPaymentId { get; set; }

    /// <summary>Cliente suelto que pagó. Null cuando el pago es de una cuenta.</summary>
    public int? ClientId { get; set; }

    /// <summary>Cuenta de facturación que pagó. Null cuando el pago es de un cliente suelto.</summary>
    public int? BillingAccountId { get; set; }

    /// <summary>Fecha calendario en que se recibió el pago.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary><see cref="Enums.PaymentMethod"/>.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string? RegisteredByUserId { get; set; }

    public virtual Client? Client { get; set; }

    public virtual BillingAccount? BillingAccount { get; set; }

    public virtual ICollection<ClientPaymentItem> Items { get; set; } = [];
}
