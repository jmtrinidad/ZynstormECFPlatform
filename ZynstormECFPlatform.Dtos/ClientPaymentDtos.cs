using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Fila del seguimiento de pagos. <see cref="Total"/> es el ciclo completo con descuento.</summary>
public class ClientPaymentDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }

    public string PlanName { get; set; } = string.Empty;

    /// <summary>1 = Comprobantes, 2 = Renta.</summary>
    public int PlanTypeId { get; set; }
    public decimal MonthlyFee { get; set; }

    /// <summary>Solo planes de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    public int ActiveUsersCount { get; set; }

    public int PaidMonths { get; set; }
    public decimal PrepaymentDiscountPercent { get; set; }

    public int MonthsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }

    /// <summary>0 = sin fecha, 1 = al día, 2 = por vencer, 3 = vencido.</summary>
    public int PaymentStatus { get; set; }

    /// <summary>Días calendario para el próximo pago; negativo si ya venció.</summary>
    public int? PaymentDaysToDue { get; set; }

    public int PaymentGraceDays { get; set; }

    /// <summary>Último día para pagar antes de la suspensión: próximo pago + días de gracia.</summary>
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>Ya se envió el primer aviso para la fecha de pago actual.</summary>
    public bool FirstReminderSent { get; set; }

    /// <summary>Ya se envió el último aviso para la fecha de pago actual.</summary>
    public bool FinalReminderSent { get; set; }

    /// <summary>Desactivado automáticamente por falta de pago.</summary>
    public bool PaymentSuspended { get; set; }

    public bool HasEmail { get; set; }

    /// <summary>Suma del excedente de meses cerrados sin pagar (solo planes de comprobantes).</summary>
    public decimal PendingOverageAmount { get; set; }

    public int PendingOverageMonths { get; set; }
}
