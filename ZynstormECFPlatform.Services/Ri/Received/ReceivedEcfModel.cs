namespace ZynstormECFPlatform.Services.Ri.Received;

/// <summary>
/// View-model del PDF de un e-CF RECIBIDO de un tercero. A diferencia de los RI propios, el
/// emisor sale del XML (no del cliente) y el comprador es normalmente nuestro cliente.
/// </summary>
public class ReceivedEcfModel
{
    public int EcfType { get; set; }

    public string TypeName { get; set; } = string.Empty;

    public string ENcf { get; set; } = string.Empty;

    public Party Issuer { get; set; } = new();

    public Party Buyer { get; set; } = new();

    public string FechaEmision { get; set; } = string.Empty;

    public string FechaFirma { get; set; } = string.Empty;

    /// <summary>FechaVencimientoSecuencia como dd/MM/yyyy; vacío si el XML no la trae.</summary>
    public string ValidUntil { get; set; } = string.Empty;

    public string PaymentType { get; set; } = string.Empty;

    public string PaymentCondition { get; set; } = string.Empty;

    public string InternalInvoiceNumber { get; set; } = string.Empty;

    /// <summary>InformacionReferencia (notas 33/34).</summary>
    public string AffectedNcf { get; set; } = string.Empty;

    public string ModificationCode { get; set; } = string.Empty;

    public string ModificationReason { get; set; } = string.Empty;

    public List<ReceivedEcfItem> Items { get; set; } = [];

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Exento { get; set; }

    public decimal Itbis { get; set; }

    /// <summary>Tasa ITBIS principal de Totales (18, 16...); 0 si no hay.</summary>
    public decimal ItbisRate { get; set; }

    public decimal ItbisRetenido { get; set; }

    public decimal IsrRetencion { get; set; }

    public decimal Total { get; set; }

    public string Qr { get; set; } = string.Empty;

    public string SecurityCode { get; set; } = string.Empty;
}

public class ReceivedEcfItem
{
    public string Description { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Abreviatura de la unidad de medida; vacío si el XML no la trae.</summary>
    public string Unit { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Discount { get; set; }

    public decimal Itbis { get; set; }

    public decimal Amount { get; set; }
}
