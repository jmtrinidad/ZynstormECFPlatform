using System.Xml.Serialization;

namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Stores the detail of each additional tax (ISC — Impuesto Selectivo al Consumo
/// and other additional taxes) associated with an e-CF document.
/// Maps to the XSD ImpuestosAdicionales/ImpuestoAdicional block inside &lt;Totales&gt;.
/// One row per TipoImpuesto per document (up to 20 per XSD).
/// </summary>
public class EcfDocumentAdditionalTax : BaseEntity
{
    [XmlIgnore]
    public int EcfDocumentAdditionalTaxId { get; set; }

    [XmlIgnore]
    public int EcfDocumentId { get; set; }

    /// <summary>
    /// 3-digit DGII code for the additional tax type (CodificacionTipoImpuestosType).
    /// Examples: "006" = Cerveza, "013" = Whisky, "014" = Ron, "019" = Cigarrillos.
    /// See the XSD CodificacionTipoImpuestosType enumeration for the full catalog.
    /// Maps to XSD element TipoImpuesto.
    /// </summary>
    [XmlElement("TipoImpuesto")]
    public string TaxTypeCode { get; set; } = null!;

    /// <summary>
    /// The additional tax rate percentage (TasaImpuestoAdicional).
    /// Required per XSD when the ImpuestoAdicional block is present.
    /// </summary>
    [XmlElement("TasaImpuestoAdicional")]
    public decimal TaxRate { get; set; }

    /// <summary>
    /// ISC Específico — fixed amount per unit summed across all items with this tax type.
    /// Maps to XSD element MontoImpuestoSelectivoConsumoEspecifico.
    /// </summary>
    [XmlElement("MontoImpuestoSelectivoConsumoEspecifico")]
    public decimal IscSpecificAmount { get; set; }

    /// <summary>
    /// ISC Ad-valorem — percentage-based amount summed across all items with this tax type.
    /// Maps to XSD element MontoImpuestoSelectivoConsumoAdvalorem.
    /// </summary>
    [XmlElement("MontoImpuestoSelectivoConsumoAdvalorem")]
    public decimal IscAdvaloremAmount { get; set; }

    /// <summary>
    /// Other additional taxes not covered by ISC Específico or Ad-valorem.
    /// Maps to XSD element OtrosImpuestosAdicionales.
    /// </summary>
    [XmlElement("OtrosImpuestosAdicionales")]
    public decimal OtherAdditionalTaxAmount { get; set; }

    [XmlIgnore]
    public virtual EcfDocument EcfDocument { get; set; } = null!;
}
