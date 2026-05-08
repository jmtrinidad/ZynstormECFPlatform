using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

/// <summary>
/// Main DTO for the old simulation logic. Isolated to avoid conflicts with the current EcfInvoiceRequestDto.
/// </summary>
public class OldEcfInvoiceRequestDto
{
    // ── Identification ─────────────────────────────────────────────────────────

    [Required]
    public string Ncf { get; set; } = null!;

    public int ClientId { get; set; }

    public int? EcfType { get; set; }

    [Required]
    public string ExternalReference { get; set; } = null!;

    [Required]
    public DateTime IssueDate { get; set; }

    public string? SecurityCodeOverride { get; set; }

    public DateTime? SignatureDateOverride { get; set; }

    public DateTime? SequenceExpirationDate { get; set; }

    // ── Issuer (Emisor) ────────────────────────────────────────────────────────

    [Required]
    public string IssuerRnc { get; set; } = null!;

    [Required]
    public string IssuerName { get; set; } = null!;

    [Required]
    public string IssuerAddress { get; set; } = null!;

    public string? IssuerCommercialName { get; set; }

    public string? IssuerBranchCode { get; set; }

    public string? IssuerPhone { get; set; }

    public string? IssuerEmail { get; set; }

    public string? IssuerActivityCode { get; set; }

    public string? IssuerWebSite { get; set; }

    public string? IssuerSellerCode { get; set; }

    public string? IssuerMunicipality { get; set; }

    public string? IssuerProvince { get; set; }

    // ── Buyer (Comprador) ──────────────────────────────────────────────────────

    [Required]
    public string CustomerRnc { get; set; } = null!;

    [Required]
    public string CustomerName { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public string? CustomerTelephone { get; set; }

    public string? CustomerContact { get; set; }

    public string? CustomerMunicipality { get; set; }

    public string? CustomerProvince { get; set; }

    public string? CustomerForeignId { get; set; }

    public string? CustomerCountry { get; set; }

    // ── Payment ────────────────────────────────────────────────────────────────

    public int? PaymentType { get; set; }

    public DateTime? PaymentDeadline { get; set; }

    public string? PaymentTerms { get; set; }

    public string? IncomeType { get; set; }

    // ── Items ──────────────────────────────────────────────────────────────────

    [Required]
    public List<OldEcfItemRequestDto> Items { get; set; } = [];

    // ── Invoice-level adjustments ──────────────────────────────────────────────

    public decimal GlobalDiscountAmount { get; set; }

    public string? GlobalDiscountDescription { get; set; }

    public string? InternalInvoiceNumber { get; set; }

    public string? InternalOrderNumber { get; set; }

    public string? SalesZone { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? OrderNumber { get; set; }

    public string? BuyerInternalCode { get; set; }

    public decimal? MontoNoFacturable { get; set; }

    // ── Reference Information (Required for NC/ND - Types 33, 34) ──────────────

    public string? ReferenceNcf { get; set; }

    public string? ReferenceCustomerRnc { get; set; }

    public DateTime? ReferenceIssueDate { get; set; }

    public int? ReferenceReasonCode { get; set; }

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
    public decimal? ManualMontoGravadoI1 { get; set; }
    public decimal? ManualMontoGravadoI2 { get; set; }
    public decimal? ManualMontoGravadoI3 { get; set; }
    public int? ManualIndicadorNotaCredito { get; set; }
    public decimal? ManualMontoNoFacturable { get; set; }
    public decimal? ManualMontoImpuestoAdicional { get; set; }

    // ── Exportation Information (Type 46) ──────────────────────────────────────

    public string? ExportFechaEmbarque { get; set; }
    public string? ExportNumeroEmbarque { get; set; }
    public string? ExportNumeroContenedor { get; set; }
    public string? ExportNumeroReferencia { get; set; }
    public string? ExportNombrePuertoEmbarque { get; set; }
    public string? ExportCondicionesEntrega { get; set; }
    public decimal? ExportTotalFob { get; set; }
    public decimal? ExportSeguro { get; set; }
    public decimal? ExportFlete { get; set; }
    public decimal? ExportOtrosGastos { get; set; }
    public decimal? ExportTotalCif { get; set; }
    public string? ExportRegimenAduanero { get; set; }
    public string? ExportNombrePuertoSalida { get; set; }
    public string? ExportNombrePuertoDesembarque { get; set; }
    public decimal? ExportPesoBruto { get; set; }
    public decimal? ExportPesoNeto { get; set; }
    public string? ExportUnidadPesoBruto { get; set; }
    public string? ExportUnidadPesoNeto { get; set; }
    public decimal? ExportCantidadBulto { get; set; }
    public string? ExportUnidadBulto { get; set; }
    public decimal? ExportVolumenBulto { get; set; }
    public string? ExportUnidadVolumen { get; set; }

    // ── Transport Information (Type 46) ────────────────────────────────────────

    public string? TranspViaTransporte { get; set; }
    public string? TranspPaisOrigen { get; set; }
    public string? TranspDireccionDestino { get; set; }
    public string? TranspPaisDestino { get; set; }
    public string? TranspRncCompaniaTransportista { get; set; }
    public string? TranspNombreCompaniaTransportista { get; set; }
    public string? TranspNumeroViaje { get; set; }
    public string? TranspConductor { get; set; }
    public string? TranspDocumentoTransporte { get; set; }
    public string? TranspFicha { get; set; }
    public string? TranspPlaca { get; set; }
    public string? TranspRutaTransporte { get; set; }
    public string? TranspZonaTransporte { get; set; }
    public string? TranspNumeroAlbaran { get; set; }

    // ── Other Currency (Type 47) ───────────────────────────────────────────────

    public string? CurrencyTipoMoneda { get; set; }
    public decimal? CurrencyTipoCambio { get; set; }
    public decimal? CurrencyMontoGravado { get; set; }
    public decimal? CurrencyMontoExento { get; set; }
    public decimal? CurrencyTotalITBIS { get; set; }
    public decimal? CurrencyMontoTotal { get; set; }
}

public class OldEcfItemRequestDto
{
    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    [Required]
    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal TaxPercentage { get; set; }

    public decimal ItbisAmount { get; set; }

    public int? ItemType { get; set; }

    public int? BillingIndicator { get; set; }

    public decimal? IsrRetentionAmount { get; set; }

    public int? UnitOfMeasure { get; set; }

    public string? IscType { get; set; }

    public decimal IscSpecificAmount { get; set; }

    public decimal IscAdvaloremAmount { get; set; }

    public decimal OtherAdditionalTaxAmount { get; set; }

    public decimal AdditionalTaxRate { get; set; }

    public decimal? ManualMontoItem { get; set; }
    public decimal? ManualDescuentoMonto { get; set; }
    public decimal? ManualRecargoMonto { get; set; }
    public decimal? ManualMontoITBISRetenido { get; set; }
    public decimal? ManualMontoISRRetenido { get; set; }
    public string? FechaElaboracion { get; set; }
    public string? FechaVencimientoItem { get; set; }
    public List<OldEcfSubRecargoDto> ManualSubRecargos { get; set; } = new();
}

public class OldEcfSubRecargoDto
{
    public string TipoSubRecargo { get; set; } = "$";
    public decimal? SubRecargoPorcentaje { get; set; }
    public decimal MontoSubRecargo { get; set; }
}
