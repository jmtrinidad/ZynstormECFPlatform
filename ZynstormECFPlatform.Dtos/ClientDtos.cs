using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

public class ClientCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string Rnc { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? DailyReportEmails { get; set; }

    [StringLength(500)]
    public string? WeeklyReportEmails { get; set; }

    public int? PlanId { get; set; }

    public bool ClientInactive { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastRentPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextRentPaymentDate { get; set; }

    public bool RentPaidFullYear { get; set; }

    /// <summary>Descuento por pago adelantado, en porcentaje. Por defecto 0.</summary>
    [Range(0, 100)]
    public decimal RentDiscountPercent { get; set; }

    //public int StatusId { get; set; }
}

public class ClientUpdateDto : ClientCreateDto
{
    [Required]
    public string GuidId { get; set; } = string.Empty;

    public int ClientId { get; set; }
}

public class ClientViewDto : ClientUpdateDto
{
    public bool IsCertified { get; set; }

    public DateTime RegisteredAt { get; set; }

    public string? ApiKey { get; set; }

    public string? PlanName { get; set; }

    public decimal? PlanMonthlyFee { get; set; }

    /// <summary>1 = Comprobantes, 2 = Renta. Null si el cliente no tiene plan.</summary>
    public int? PlanTypeId { get; set; }

    /// <summary>Usuarios permitidos por el plan de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    /// <summary>Usuarios activos y no eliminados asignados al cliente.</summary>
    public int ActiveUsersCount { get; set; }

    /// <summary>0 = sin fecha, 1 = al día, 2 = por vencer, 3 = vencido. Solo planes de renta.</summary>
    public int? RentStatus { get; set; }

    public int? RentDaysToDue { get; set; }

    /// <summary>Ciclo completo de renta con descuento aplicado. Solo planes de renta.</summary>
    public decimal? RentCycleAmount { get; set; }

    /// <summary>Vencimiento del certificado vigente (UTC).</summary>
    public DateTime? CertificateExpirationDateUtc { get; set; }

    /// <summary>Días calendario para el vencimiento; negativo si ya venció.</summary>
    public int? CertificateDaysToExpire { get; set; }

    public bool CertificateExpiringSoon { get; set; }
}
