namespace ZynstormECFPlatform.Dtos;

/// <summary>Mes cerrado de una cuenta con excedente del pool sin pagar.</summary>
public class AccountPendingOverageDto
{
    public int BillingAccountMonthlyUsageId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int? PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int AcceptedDocuments { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int OverageDocuments { get; set; }
    public decimal Amount { get; set; }
}

public class AccountPaymentPreviewDto
{
    public string AccountGuidId { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool PaymentSuspended { get; set; }

    /// <summary>Null si el plan no tiene mensualidad.</summary>
    public PlanFeePreviewDto? PlanFee { get; set; }

    public List<AccountPendingOverageDto> PendingOverages { get; set; } = [];
}
