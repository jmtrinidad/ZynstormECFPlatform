namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Recibo de un pago recibido de un cliente. Sus líneas indican qué cubre.</summary>
public partial class ClientPayment : BaseEntity
{
    public int ClientPaymentId { get; set; }

    public int ClientId { get; set; }

    /// <summary>Fecha calendario en que se recibió el pago.</summary>
    public DateTime PaymentDate { get; set; }

    /// <summary><see cref="Enums.PaymentMethod"/>.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public string? RegisteredByUserId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ClientPaymentItem> Items { get; set; } = [];
}
