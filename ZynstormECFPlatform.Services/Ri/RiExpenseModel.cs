namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// View-model consumed by <see cref="RiExpensePdf"/> (e-CF tipo 43, Gastos Menores).
/// Contains only the fields the ported EasyInvoice <c>ExpensePdf</c> (rama IsInformal)
/// renders y que existen en el XML del 43, populated by
/// <see cref="EcfRiTemplateMapper.MapExpense"/>. ACREEDOR/CATEGORÍA/GASTO NO del diseño
/// original se omiten: no viajan en el e-CF 43 (que tampoco lleva comprador).
/// </summary>
public class RiExpenseModel
{
    public RiInvoiceCompany Company { get; set; } = new();

    public string NcfNumber { get; set; } = string.Empty;

    /// <summary>FechaVencimientoSecuencia formateada dd/MM/yyyy; vacío si no viene.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    /// <summary>"CONTADO"/"CRÉDITO" según TipoPago; vacío si no viene (fila omitida).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>Descripciones de los ítems unidas por "; ".</summary>
    public string Concept { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    /// <summary>"ITBIS 18%:" / "ITBIS 16%:" / "EXENTO:" según la tasa efectiva.</summary>
    public string ItbisLabel { get; set; } = string.Empty;

    /// <summary>DGII ConsultaTimbre URL, also used to render the QR image.</summary>
    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}
