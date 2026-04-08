using System.Xml.Serialization;
namespace ZynstormECFPlatform.Core.Entities;


public partial class EcfDocumentTotal : BaseEntity
{
    [XmlIgnore]
    public int EcfDocumentTotalId { get; set; }

    [XmlIgnore]
    public int EcfDocumentId { get; set; }

    [XmlIgnore] // redundant with TaxableAmount
    public decimal TaxableTotal { get; set; }

    [XmlElement("MontoExento")]
    public decimal ExemptTotal { get; set; }

    [XmlIgnore]
    public decimal DiscountTotal { get; set; }

    [XmlElement("TotalITBIS")]
    public decimal ITBISTotal { get; set; }

    [XmlElement("MontoTotal")]
    public decimal Total { get; set; }

    [XmlIgnore]
    public virtual EcfDocument EcfDocument { get; set; } = null!;

    #region e-CF Extended Properties

    [XmlElement("MontoGravadoTotal")]
    public decimal? TaxableAmount { get; set; }

    [XmlElement("MontoGravadoI1")]
    public decimal? TaxableAmountG1 { get; set; } // 18%

    [XmlElement("MontoGravadoI2")]
    public decimal? TaxableAmountG2 { get; set; } // 16%

    [XmlElement("MontoGravadoI3")]
    public decimal? TaxableAmountG3 { get; set; } // 0%

    [XmlElement("ITBIS1")]
    public int? TaxRate1 { get; set; }

    [XmlElement("ITBIS2")]
    public int? TaxRate2 { get; set; }

    [XmlElement("ITBIS3")]
    public int? TaxRate3 { get; set; }

    [XmlElement("TotalITBIS1")]
    public decimal? TaxAmount1 { get; set; }

    [XmlElement("TotalITBIS2")]
    public decimal? TaxAmount2 { get; set; }

    [XmlElement("TotalITBIS3")]
    public decimal? TaxAmount3 { get; set; }

    [XmlElement("TotalITBISRetenido")]
    public decimal? TotalWithheldItbis { get; set; }

    [XmlElement("TotalISRRetencion")]
    public decimal? TotalWithheldIsr { get; set; }

    /// <summary>
    /// Total additional taxes (ISC sum) for the document.
    /// Maps to XSD element <c>MontoImpuestoAdicional</c> in Totales.
    /// </summary>
    [XmlElement("MontoImpuestoAdicional")]
    public decimal? AdditionalTaxTotal { get; set; }

    #endregion
}