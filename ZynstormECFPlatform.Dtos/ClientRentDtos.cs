using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Fila del reporte de renta. <see cref="Total"/> es el ciclo completo con descuento.</summary>
public class ClientRentDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public decimal MonthlyFee { get; set; }

    /// <summary>-1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    public int ActiveUsersCount { get; set; }

    public bool RentPaidFullYear { get; set; }
    public decimal RentDiscountPercent { get; set; }

    public int MonthsCovered { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastRentPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextRentPaymentDate { get; set; }

    /// <summary>0 = sin fecha, 1 = al día, 2 = por vencer, 3 = vencido.</summary>
    public int RentStatus { get; set; }

    /// <summary>Días calendario para el próximo pago; negativo si ya venció.</summary>
    public int? RentDaysToDue { get; set; }
}
