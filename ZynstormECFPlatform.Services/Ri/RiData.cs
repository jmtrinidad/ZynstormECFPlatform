namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Data required to render a Ri (Representación Impresa), as produced by the
/// XML mapper and consumed by the QuestPDF renderer.
/// </summary>
public class RiData
{
    public Party Issuer { get; set; } = new();

    public Party Buyer { get; set; } = new();

    public string ENcf { get; set; } = string.Empty;

    public string TipoeCF { get; set; } = string.Empty;

    public string FechaVencimientoSecuencia { get; set; } = string.Empty;

    /// <summary>1=Contado, 2=Crédito, 0=ausente.</summary>
    public int TipoPago { get; set; }

    public string FechaLimitePago { get; set; } = string.Empty;

    public string TerminoPago { get; set; } = string.Empty;

    public string NumeroFacturaInterna { get; set; } = string.Empty;

    public string NcfModificado { get; set; } = string.Empty;

    public string CodigoModificacion { get; set; } = string.Empty;

    public string RazonModificacion { get; set; } = string.Empty;

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;

    public string QrUrl { get; set; } = string.Empty;

    public List<RiItem> Items { get; set; } = [];

    public RiTotals Totals { get; set; } = new();
}

public class Party
{
    public string Name { get; set; } = string.Empty;

    public string Document { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Country { get; set; }
}

public class RiItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Itbis { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Código DGII de unidad de medida (1-62); 0 si el XML no lo trae.</summary>
    public int UnidadMedida { get; set; }

    public int IndicadorFacturacion { get; set; }

    public decimal Discount { get; set; }
}

public class RiTotals
{
    public decimal SubTotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Exento { get; set; }

    public decimal Gravado { get; set; }

    public decimal Total { get; set; }

    public decimal ItbisRetenido { get; set; }

    public decimal IsrRetencion { get; set; }

    /// <summary>Tasas ITBIS de Totales (ej. 18, 16, 0).</summary>
    public decimal Itbis1Rate { get; set; }

    public decimal Itbis2Rate { get; set; }

    public decimal Itbis3Rate { get; set; }
}
