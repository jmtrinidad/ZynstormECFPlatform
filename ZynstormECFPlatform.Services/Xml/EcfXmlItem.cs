using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Item&gt; inside &lt;DetallesItems&gt;.
/// </summary>
public class EcfXmlItem
{
    [XmlElement("NumeroLinea")]
    public int NumeroLinea { get; set; }

    /// <summary>
    /// IndicadorFacturacion:
    /// 0 = No Facturable (18%), 1 = ITBIS 18%, 2 = ITBIS 16%, 3 = ITBIS 0%, 4 = Exento.
    /// </summary>
    [XmlElement("IndicadorFacturacion")]
    public int IndicadorFacturacion { get; set; }

    /// <summary>1 = Bien, 2 = Servicio.</summary>
    [XmlElement("NombreItem")]
    public string Name { get; set; } = null!;

    [XmlElement("IndicadorBienoServicio")]
    public int ItemType { get; set; }

    public string? DescripcionItem { get; set; }
    public bool ShouldSerializeDescripcionItem() => DescripcionItem != null;

    [XmlElement("CantidadItem")]
    public decimal CantidadItem { get; set; }

    public int? UnidadMedida { get; set; }
    public bool ShouldSerializeUnidadMedida() => UnidadMedida.HasValue;

    [XmlElement("PrecioUnitarioItem")]
    public decimal PrecioUnitarioItem { get; set; }

    public decimal? DescuentoMonto { get; set; }
    public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto > 0;

    [XmlElement("MontoItem")]
    public decimal MontoItem { get; set; }
}
