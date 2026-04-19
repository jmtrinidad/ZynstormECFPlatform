using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Root XML model mapping to <RFCE> — the summary element defined in RFCE 32 v.1.0.xsd.
/// </summary>
[XmlRoot("RFCE")]
public class RfceXmlRoot
{
    [XmlElement("Encabezado")]
    public RfceXmlEncabezado Encabezado { get; set; } = null!;

    /// <summary>
    /// Placeholder for the digital signature. 
    /// Required by XSD as an xs:any element.
    /// </summary>
    [XmlAnyElement]
    public System.Xml.XmlElement? Signature { get; set; }
}
