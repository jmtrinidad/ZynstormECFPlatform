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
}

public class RiTotals
{
    public decimal SubTotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Exento { get; set; }

    public decimal Gravado { get; set; }

    public decimal Total { get; set; }
}
