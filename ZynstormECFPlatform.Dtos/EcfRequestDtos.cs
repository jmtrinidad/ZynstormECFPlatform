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

    /// <summary>Buyer phone number in format 000-000-0000 (optional).</summary>
    public string? CustomerTelephone { get; set; }

    /// <summary>Buyer contact person name (optional).</summary>
    public string? CustomerContact { get; set; }

    /// <summary>Buyer municipality code — 6-digit DGII code (optional).</summary>
    public string? CustomerMunicipality { get; set; }

    /// <summary>Buyer province code — 6-digit DGII code (optional).</summary>
    public string? CustomerProvince { get; set; }

    /// <summary>Foreign identifier for customers outside DR (e.g. Passport, Tax ID) - Required for Type 46/47.</summary>
    public string? CustomerForeignId { get; set; }

    /// <summary>Country name for foreign customers (optional).</summary>
    public string? CustomerCountry { get; set; }


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

    /// <summary>Internal invoice number from the integrating system (optional).</summary>
    public string? InternalInvoiceNumber { get; set; }

    /// <summary>Internal order/purchase number from the integrating system (optional).</summary>
    public string? InternalOrderNumber { get; set; }

    /// <summary>Sales zone (optional).</summary>
    public string? SalesZone { get; set; }

    /// <summary>Delivery date (optional).</summary>
    public DateTime? DeliveryDate { get; set; }

    /// <summary>Purchase order date (optional).</summary>
    public DateTime? OrderDate { get; set; }

    /// <summary>Purchase order number (optional).</summary>
    public string? OrderNumber { get; set; }

    /// <summary>Buyer's internal code for the seller (optional).</summary>
    public string? BuyerInternalCode { get; set; }

    /// <summary>Amount not billable (non-taxed service included for info only, e.g. type 33 exento).</summary>
    public decimal? MontoNoFacturable { get; set; }

    // ── Reference Information (Required for NC/ND - Types 33, 34) ──────────────

    /// <summary>The original NCF being modified (e.g. "E310000000001").</summary>
    public string? ReferenceNcf { get; set; }

    /// <summary>RNC of the other taxpayer if the reference is not for the same customer (optional).</summary>
    public string? ReferenceCustomerRnc { get; set; }

    /// <summary>Issue date of the modified NCF.</summary>
    public DateTime? ReferenceIssueDate { get; set; }

    /// <summary>
    /// Modification reason code:
    /// 1 = Anula, 2 = Corrige texto, 3 = Corrige montos, 4 = Reemplazo contingencia.
    /// </summary>
    public int? ReferenceReasonCode { get; set; }

    /// <summary>Text description of the modification reason (optional).</summary>
    public string? ReferenceReasonDescription { get; set; }

    // ── Manual Overrides for Certification (Raw Excel Data) ──────────────────

    public decimal? ManualMontoGravadoTotal { get; set; }
    public decimal? ManualMontoExento { get; set; }
    public decimal? ManualMontoTotal { get; set; }
    public decimal? ManualTotalITBIS { get; set; }
    public decimal? ManualTotalITBIS1 { get; set; }
    public decimal? ManualTotalITBIS2 { get; set; }
    public decimal? ManualTotalITBIS3 { get; set; }
    public decimal? ManualMontoPeriodo { get; set; }
    public decimal? ManualValorPagar { get; set; }
    public int? ManualIndicadorMontoGravado { get; set; }
    public decimal? ManualTotalITBISRetenido { get; set; }
    public decimal? ManualTotalISRRetencion { get; set; }
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

    /// <summary>
    /// Explicit billing indicator from DGII catalog (IndicadorFacturacion).
    /// 1=ITBIS18%, 2=ITBIS16%, 3=ITBIS0%, 4=Exento, 0=NoFacturable.
    /// When set, overrides the TaxPercentage-derived billing indicator in the generator.
    /// </summary>
    public int? BillingIndicator { get; set; }

    /// <summary>ISR Retention amount (used in types 41, 47) (optional).</summary>
    public decimal? IsrRetentionAmount { get; set; }


    /// <summary>Unit of measure code per DGII catalog (1-62). Defaults to 43 (Unit).</summary>
    public int? UnitOfMeasure { get; set; }

    // ── ISC — Impuesto Selectivo al Consumo ─────────────────────────────────

    /// <summary>
    /// 3-digit DGII code identifying the additional tax type (ISC).
    /// Examples: "006"=Cerveza, "013"=Whisky, "014"=Ron, "019"=Cigarrillos.
    /// See CodificacionTipoImpuestosType in the XSD for the full catalog.
    /// Leave null if no ISC applies to this item.
    /// </summary>
    public string? IscType { get; set; }

    /// <summary>
    /// ISC Específico: fixed amount per unit (MontoImpuestoSelectivoConsumoEspecifico).
    /// Used when the ISC is a flat amount regardless of price.
    /// </summary>
    public decimal IscSpecificAmount { get; set; }

    /// <summary>
    /// ISC Ad-valorem: percentage-based amount (MontoImpuestoSelectivoConsumoAdvalorem).
    /// Used when the ISC is a percentage of the item price.
    /// </summary>
    public decimal IscAdvaloremAmount { get; set; }

    /// <summary>
    /// Other additional tax amounts (OtrosImpuestosAdicionales) not covered by ISC Específico or Ad-valorem.
    /// </summary>
    public decimal OtherAdditionalTaxAmount { get; set; }

    /// <summary>
    /// Additional tax rate (TasaImpuestoAdicional). Required when IscType is set.
    /// Represents the percentage rate of the additional tax (e.g. 10.00 for 10%).
    /// </summary>
    public decimal AdditionalTaxRate { get; set; }

    // ── Manual Overrides for Certification ──────────────────────────────────
    public decimal? ManualMontoItem { get; set; }
    public decimal? ManualMontoISRRetenido { get; set; }
}
