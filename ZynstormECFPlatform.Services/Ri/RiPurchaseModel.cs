namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// View-model consumed by <see cref="RiPurchasePdf"/>. Contains only the fields the
/// ported EasyInvoice <c>PurchasePdf</c> template actually renders, plus the QR/security
/// fields added for the e-CF tipo 41 (Comprobante de Compras) Ri, populated by
/// <see cref="EcfRiTemplateMapper.MapPurchase"/> from a signed e-CF XML.
/// </summary>
public class RiPurchaseModel
{
    public RiPurchaseCompany Company { get; set; } = new();

    public RiPurchaseSupplier Supplier { get; set; } = new();

    public string NcfNumber { get; set; } = string.Empty;

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    public List<RiPurchaseItem> Items { get; set; } = [];

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    /// <summary>ISR retention rate (percentage, e.g. 2.0 for 2%). Defaults to 0.</summary>
    public decimal IsrRetentionRate { get; set; }

    /// <summary>ISR retention amount. Defaults to 0.</summary>
    public decimal IsrRetentionAmount { get; set; }

    /// <summary>ITBIS retenido (Totales/TotalITBISRetenido). Defaults to 0.</summary>
    public decimal ItbisRetentionAmount { get; set; }

    public string? Note { get; set; }

    /// <summary>DGII ConsultaTimbre/ConsultaTimbreFC URL, also used to render the QR image.</summary>
    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}

public class RiPurchaseCompany
{
    public string Name { get; set; } = string.Empty;

    public string Rnc { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Whatsapp { get; set; } = string.Empty;
}

public class RiPurchaseSupplier
{
    public string Name { get; set; } = string.Empty;

    public string Rnc { get; set; } = string.Empty;

    public string? Address { get; set; }
}

public class RiPurchaseItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Itbis { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Tasa ITBIS de la línea según IndicadorFacturacion (ej. 18); 0 = exento.</summary>
    public decimal ItbisRate { get; set; }
}
