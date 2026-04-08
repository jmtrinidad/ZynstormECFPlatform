using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

/// <summary>
/// Main DTO for creating an e-CF document. The TipoeCF is automatically derived from the NCF.
/// </summary>
public class EcfInvoiceRequestDto
{
    // ── Identification ─────────────────────────────────────────────────────────

    /// <summary>
    /// eNCF: 13-character electronic NCF (e.g. "E310000000001").
    /// The TipoeCF is extracted automatically from characters 1-2.
    /// </summary>
    [Required]
    public string Ncf { get; set; } = null!;

    /// <summary>Optional external reference / internal invoice number from the integrating system.</summary>
    [Required]
    public string ExternalReference { get; set; } = null!;

    /// <summary>Issue date of the document.</summary>
    [Required]
    public DateTime IssueDate { get; set; }

    /// <summary>Last valid date for the NCF sequence.</summary>
    public DateTime? SequenceExpirationDate { get; set; }

    // ── Issuer (Emisor) ────────────────────────────────────────────────────────

    /// <summary>RNC of the issuer (9 or 11 digits).</summary>
    [Required]
    public string IssuerRnc { get; set; } = null!;

    /// <summary>Legal name (Razón Social) of the issuer.</summary>
    [Required]
    public string IssuerName { get; set; } = null!;

    /// <summary>Street address of the issuer.</summary>
    [Required]
    public string IssuerAddress { get; set; } = null!;

    /// <summary>Commercial / trade name of the issuer (optional).</summary>
    public string? IssuerCommercialName { get; set; }

    /// <summary>DGII branch code (Sucursal), if applicable.</summary>
    public string? IssuerBranchCode { get; set; }

    /// <summary>Issuer phone number in format 000-000-0000 (optional).</summary>
    public string? IssuerPhone { get; set; }

    /// <summary>Issuer email address (optional).</summary>
    public string? IssuerEmail { get; set; }

    /// <summary>Issuer economic activity description (optional).</summary>
    public string? IssuerActivityCode { get; set; }

    /// <summary>Issuer website (optional).</summary>
    public string? IssuerWebSite { get; set; }

    /// <summary>Seller code (optional).</summary>
    public string? IssuerSellerCode { get; set; }

    /// <summary>Municipality code for issuer (6-digit DGII code, optional).</summary>
    public string? IssuerMunicipality { get; set; }

    /// <summary>Province code for issuer (6-digit DGII code, optional).</summary>
    public string? IssuerProvince { get; set; }

    // ── Buyer (Comprador) ──────────────────────────────────────────────────────

    /// <summary>RNC or Cédula of the buyer (9 or 11 digits).</summary>
    [Required]
    public string CustomerRnc { get; set; } = null!;

    /// <summary>Legal or full name of the buyer.</summary>
    [Required]
    public string CustomerName { get; set; } = null!;

    /// <summary>Buyer email (optional).</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>Buyer street address (optional).</summary>
    public string? CustomerAddress { get; set; }

    /// <summary>Buyer phone number (optional).</summary>
    public string? CustomerTelephone { get; set; }

    // ── Payment ────────────────────────────────────────────────────────────────

    /// <summary>1: Contado, 2: Crédito, 3: Gratuito.</summary>
    public int PaymentType { get; set; } = 1;

    /// <summary>Credit payment deadline date (required when PaymentType = 2).</summary>
    public DateTime? PaymentDeadline { get; set; }

    /// <summary>Payment terms description (optional).</summary>
    public string? PaymentTerms { get; set; }

    /// <summary>Income type code "01"-"06" per DGII catalog. Defaults to "01" (operational).</summary>
    public string IncomeType { get; set; } = "01";

    // ── Items ──────────────────────────────────────────────────────────────────

    [Required]
    public List<EcfItemRequestDto> Items { get; set; } = [];

    // ── Invoice-level adjustments ──────────────────────────────────────────────

    /// <summary>Global discount amount applied to the whole invoice.</summary>
    public decimal GlobalDiscountAmount { get; set; }

    /// <summary>Description of the global discount.</summary>
    public string? GlobalDiscountDescription { get; set; }
}

public class EcfItemRequestDto
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    /// <summary>Item-level discount amount (absolute value in currency).</summary>
    public decimal Discount { get; set; }

    /// <summary>ITBIS tax percentage: 18, 16, or 0.</summary>
    public decimal TaxPercentage { get; set; }

    /// <summary>Explicit ITBIS amount. If 0, it is calculated from TaxPercentage.</summary>
    public decimal ItbisAmount { get; set; }

    /// <summary>1: Bien (Good), 2: Servicio (Service).</summary>
    public int ItemType { get; set; } = 1;

    /// <summary>Unit of measure code per DGII catalog (1-62). Defaults to 43 (Unit).</summary>
    public int? UnitOfMeasure { get; set; }
}
