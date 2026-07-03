namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// View-model consumed by <see cref="RiInvoicePdf"/>. Contains only the fields the
/// ported EasyInvoice <c>InvoicePdf</c> template actually renders, populated by
/// <see cref="EcfRiTemplateMapper.MapInvoice"/> from a signed e-CF XML.
/// </summary>
public class RiInvoiceModel
{
    public RiInvoiceCompany Company { get; set; } = new();

    public RiInvoiceClient Client { get; set; } = new();

    public string NcfNumber { get; set; } = string.Empty;

    public string NcfTypeName { get; set; } = string.Empty;

    public int EcfType { get; set; }

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    public List<RiInvoiceItem> Items { get; set; } = [];

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    public string? Note { get; set; }

    public string PaymentType { get; set; } = string.Empty;

    /// <summary>FechaVencimientoSecuencia formateada dd/MM/yyyy; vacío si el XML no la trae.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    /// <summary>NumeroFacturaInterna del emisor; vacío si el XML no lo trae (la fila se omite).</summary>
    public string InternalInvoiceNumber { get; set; } = string.Empty;

    /// <summary>"CONTADO" / "N DÍAS" (TerminoPago del XML si viene); vacío si no aplica.</summary>
    public string PaymentCondition { get; set; } = string.Empty;

    /// <summary>true cuando TipoPago=2 (crédito): footer CRÉDITO + FIRMA REQUERIDA, sin recibido/cambio.</summary>
    public bool IsCredit { get; set; }

    public string Cashier { get; set; } = string.Empty;

    public decimal ReceivedAmount { get; set; }

    public decimal ChangeAmount { get; set; }

    /// <summary>InformacionReferencia/NCFModificado (solo notas 33/34).</summary>
    public string AffectedNcf { get; set; } = string.Empty;

    public string ModificationCode { get; set; } = string.Empty;

    public string ModificationReason { get; set; } = string.Empty;

    /// <summary>DGII ConsultaTimbre/ConsultaTimbreFC URL, also used to render the QR image.</summary>
    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}

public class RiInvoiceCompany
{
    public string Name { get; set; } = string.Empty;

    public string Rnc { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Whatsapp { get; set; } = string.Empty;
}

public class RiInvoiceClient
{
    public string Name { get; set; } = string.Empty;

    public string Rnc { get; set; } = string.Empty;

    public string? Address { get; set; }
}

public class RiInvoiceItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal Itbis { get; set; }

    public decimal Amount { get; set; }

    public decimal Discount { get; set; }

    /// <summary>Abreviatura de la unidad de medida DGII ("Und", "Caja", ...); vacío si el XML no la trae.</summary>
    public string Unit { get; set; } = string.Empty;
}
