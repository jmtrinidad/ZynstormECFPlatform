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
    public EcfXmlComprador Comprador { get; set; } = null!;

    [XmlElement("Totales")]
    public EcfXmlTotales Totales { get; set; } = null!;
}
