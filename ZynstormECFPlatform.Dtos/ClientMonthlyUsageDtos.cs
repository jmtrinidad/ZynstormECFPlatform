namespace ZynstormECFPlatform.Dtos;

public class OverageTierChargeDto
{
    public int FromUnit { get; set; }
    public int? ToUnit { get; set; }
    public decimal UnitPrice { get; set; }
    public int Units { get; set; }
    public decimal Amount { get; set; }
}

public class ClientMonthlyUsageDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public string ClientRnc { get; set; } = string.Empty;
    public bool ClientInactive { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string PlanName { get; set; } = string.Empty;
    /// <summary>1 = Comprobantes, 2 = Renta. En las filas de renta los campos de documentos van en cero.</summary>
    public int PlanTypeId { get; set; } = 1;
    public decimal MonthlyFee { get; set; }
    public int MonthlyDocumentLimit { get; set; }
    public int AcceptedDocuments { get; set; }
    public int OverageDocuments { get; set; }
    public decimal OverageAmount { get; set; }
    public decimal Total { get; set; }
    public List<OverageTierChargeDto> Tiers { get; set; } = [];
}
