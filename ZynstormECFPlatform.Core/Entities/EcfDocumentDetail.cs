using System.Xml.Serialization;
namespace ZynstormECFPlatform.Core.Entities;


public partial class EcfDocumentDetail : BaseEntity
{
    [XmlIgnore]
    public int EcfDocumentDetailId { get; set; }

    [XmlIgnore]
    public int EcfDocumentId { get; set; }

    [XmlElement("NumeroLinea")]
    public int LineNumber { get; set; }

    [XmlIgnore]
    public string Description { get; set; } = null!;

    [XmlElement("CantidadItem")]
    public decimal Quantity { get; set; }

    [XmlIgnore]
    public int UnitCode { get; set; }

    [XmlElement("PrecioUnitarioItem")]
    public decimal UnitPrice { get; set; }

    [XmlElement("DescuentoMonto")]
    public decimal Discount { get; set; }

    [XmlIgnore]
    public decimal SubTotal { get; set; }

    [XmlIgnore]
    public decimal ItbisPercentage { get; set; }

    [XmlIgnore]
    public decimal ItbisAmount { get; set; }

    [XmlIgnore]
    public decimal Total { get; set; }

    [XmlIgnore]
    public virtual EcfDocument EcfDocument { get; set; } = null!;

    #region e-CF Extended Properties

    [XmlElement("IndicadorFacturacion")]
    public int BillingIndicator { get; set; }

    [XmlElement("IndicadorBienoServicio")]
    public int ItemType { get; set; }

    [XmlElement("NombreItem")]
    public string ItemName { get; set; } = null!;

    [XmlElement("UnidadMedida")]
    public int? UnitOfMeasure { get; set; }

    [XmlElement("MontoITBISRetenido")]
    public decimal? WithholdingItbis { get; set; }

    [XmlElement("MontoISRRetenido")]
    public decimal? WithholdingIsr { get; set; }

    [XmlElement("MontoItem")]
    public decimal ItemAmount { get; set; }

    // ── ISC — Impuesto Selectivo al Consumo ──────────────────────────

    /// <summary>
    /// 3-digit DGII code identifying the additional tax type for this item.
    /// E.g. "006" = Cerveza, "013" = Whisky, "014" = Ron, "019" = Cigarrillos.
    /// Null when no ISC applies. Maps to TipoImpuesto inside TablaImpuestoAdicional.
    /// </summary>
    [XmlIgnore]
    public string? IscType { get; set; }

    /// <summary>
    /// Additional tax rate percentage (TasaImpuestoAdicional).
    /// Required when IscType is set.
    /// </summary>
    [XmlIgnore]
    public decimal AdditionalTaxRate { get; set; }

    /// <summary>
    /// ISC Específico: fixed amount per unit for this item line.
    /// Contributes to MontoImpuestoSelectivoConsumoEspecifico in totales.
    /// </summary>
    [XmlIgnore]
    public decimal IscSpecificAmount { get; set; }

    /// <summary>
    /// ISC Ad-valorem: percentage-based amount for this item line.
    /// Contributes to MontoImpuestoSelectivoConsumoAdvalorem in totales.
    /// </summary>
    [XmlIgnore]
    public decimal IscAdvaloremAmount { get; set; }

    /// <summary>
    /// Other additional tax amounts for this item line (OtrosImpuestosAdicionales).
    /// </summary>
    [XmlIgnore]
    public decimal OtherAdditionalTaxAmount { get; set; }

    #endregion
}