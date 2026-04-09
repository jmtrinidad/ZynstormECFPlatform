using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Encabezado&gt; — document header containing all sub-sections.
/// </summary>
public class EcfXmlEncabezado
{
    [XmlElement("Version")]
    public decimal Version { get; set; } = 1.0m;

    [XmlElement("IdDoc")]
    public EcfXmlIdDoc IdDoc { get; set; } = null!;

    [XmlElement("Emisor")]
    public EcfXmlEmisor Emisor { get; set; } = null!;

    [XmlElement("Comprador")]
    public EcfXmlComprador? Comprador { get; set; }
    // Type 43 (Gastos Menores) does not include Comprador in its XSD — the buyer is anonymous.
    public bool ShouldSerializeComprador() => Comprador != null && IdDoc?.EcfType != 43;

    [XmlElement("Totales")]
    public EcfXmlTotales Totales { get; set; } = null!;
}
