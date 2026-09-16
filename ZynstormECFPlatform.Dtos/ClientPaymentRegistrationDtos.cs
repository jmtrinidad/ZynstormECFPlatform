using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Mes cerrado con excedente sin pagar.</summary>
public class PendingOverageDto
{
    public int ClientMonthlyUsageId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int OverageDocuments { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>Mensualidad que se registraría hoy con los meses y descuento del cliente.</summary>
public class PlanFeePreviewDto
{
    public string PlanName { get; set; } = string.Empty;
    public int PlanTypeId { get; set; }
    public decimal MonthlyFee { get; set; }
    public int MonthsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? CurrentNextPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NewNextPaymentDate { get; set; }
}

public class PaymentPreviewDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool PaymentSuspended { get; set; }

    /// <summary>Null si el plan no tiene mensualidad.</summary>
    public PlanFeePreviewDto? PlanFee { get; set; }

    public List<PendingOverageDto> PendingOverages { get; set; } = [];
}

public class RegisterPaymentRequestDto
{
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }

    /// <summary>1 Efectivo, 2 Transferencia, 3 Tarjeta, 4 Cheque, 5 Otro.</summary>
    public int PaymentMethod { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public bool IncludePlanFee { get; set; }
    public List<int> OverageUsageIds { get; set; } = [];
}

public class PaymentReceiptItemDto
{
    /// <summary>1 Mensualidad, 2 Excedente.</summary>
    public int ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int? MonthsCovered { get; set; }
    public decimal? DiscountAmount { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PreviousNextPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NewNextPaymentDate { get; set; }

    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? OverageDocuments { get; set; }
}

public class PaymentReceiptDto
{
    public int ClientPaymentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? PaymentDate { get; set; }

    public int PaymentMethod { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>Próximo pago tras este recibo; null si el recibo no incluyó mensualidad.</summary>
    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }

    /// <summary>Solo en la respuesta del registro: el cliente fue reactivado.</summary>
    public bool Reactivated { get; set; }

    /// <summary>Solo en la respuesta del registro: estaba suspendido y sigue suspendido.</summary>
    public bool StillSuspended { get; set; }

    public List<PaymentReceiptItemDto> Items { get; set; } = [];
}
