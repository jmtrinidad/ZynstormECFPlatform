using System.Xml.Serialization;

namespace ZynstormECFPlatform.Core.Entities;

public class EcfGlobalAdjustment : BaseEntity
{
    [XmlIgnore]
    public int EcfGlobalAdjustmentId { get; set; }

    [XmlIgnore]
    public int EcfDocumentId { get; set; }

    [XmlElement("NumeroLinea")]
    public int LineNumber { get; set; }

    [XmlElement("TipoAjuste")]
    public string AdjustmentType { get; set; } = "D"; // D for Discount, R for Recargo

    [XmlElement("DescripcionDescuentooRecargo")]
    public string? Description { get; set; }

    [XmlElement("TipoValor")]
    public string ValueType { get; set; } = "$"; // $ or %

    [XmlElement("MontoDescuentooRecargo")]
    public decimal Amount { get; set; }

    [XmlIgnore]
    public virtual EcfDocument EcfDocument { get; set; } = null!;
}
