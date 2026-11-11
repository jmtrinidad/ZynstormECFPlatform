using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Dtos;

/// <summary>Alta y edición de una cuenta. <c>OwnerUserId</c> solo se usa al crear.</summary>
public class BillingAccountSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string? OwnerUserId { get; set; }
    public int PlanId { get; set; }
    public decimal? CustomMonthlyFee { get; set; }
    public int PaidMonths { get; set; } = 1;
    public decimal PrepaymentDiscountPercent { get; set; }
    public int PaymentGraceDays { get; set; } = 3;

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }
}

public class BillingAccountClientsDto
{
    public List<string> ClientGuidIds { get; set; } = [];
}

public class BillingAccountClientUsageDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int? QuotaDocuments { get; set; }
    public bool QuotaExceeded { get; set; }
}

public class BillingAccountViewDto
{
    public string GuidId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int MonthlyDocumentLimit { get; set; }
    public decimal MonthlyFee { get; set; }
    public decimal? CustomMonthlyFee { get; set; }
    public int StatusId { get; set; }
    public int ClientsCount { get; set; }

    // Pool del mes en curso
    public int CurrentMonthAcceptedDocuments { get; set; }
    public int CurrentMonthOverageDocuments { get; set; }
    public decimal CurrentMonthOverageAmount { get; set; }

    // Cobro
    public int PaidMonths { get; set; }
    public decimal PrepaymentDiscountPercent { get; set; }
    public int PaymentGraceDays { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? LastPaymentDate { get; set; }

    [JsonConverter(typeof(CalendarDateJsonConverter))]
    public DateTime? NextPaymentDate { get; set; }

    public int PaymentStatus { get; set; }
    public int? PaymentDaysToDue { get; set; }
    public bool PaymentSuspended { get; set; }
    public decimal PendingOverageAmount { get; set; }
    public int PendingOverageMonths { get; set; }
}

public class BillingAccountDetailDto : BillingAccountViewDto
{
    public List<BillingAccountClientUsageDto> Clients { get; set; } = [];
}
