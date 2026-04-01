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

    #endregion
}