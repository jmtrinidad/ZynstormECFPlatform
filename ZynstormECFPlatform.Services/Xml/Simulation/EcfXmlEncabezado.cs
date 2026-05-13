using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml.Simulation;

/// <summary>
/// Maps to XSD &lt;Encabezado&gt; — document header containing all sub-sections.
/// </summary>
public class EcfXmlEncabezado
{
    [XmlElement("Version", Order = 1)]
    public decimal Version { get; set; } = 1.0m;

    [XmlElement("IdDoc", Order = 2)]
    public EcfXmlIdDoc IdDoc { get; set; } = null!;

    [XmlElement("Emisor", Order = 3)]
    public EcfXmlEmisor Emisor { get; set; } = null!;

    [XmlIgnore]
    public EcfXmlComprador? Comprador { get; set; }

    [XmlElement("Comprador", Order = 4)]
    public EcfXmlComprador? CompradorStandard
    {
        get => (Comprador != null && Comprador.EcfType != 43) ? Comprador : null;
        set { }
    }

    [XmlElement("InformacionesAdicionales", Order = 5)]
    public EcfXmlInformacionesAdicionales? InformacionesAdicionales { get; set; }
    public bool ShouldSerializeInformacionesAdicionales() => InformacionesAdicionales != null;

    [XmlElement("Transporte", Order = 6)]
    public EcfXmlTransporte? Transporte { get; set; }
    public bool ShouldSerializeTransporte() => Transporte != null;

    [XmlElement("Totales", Order = 7)]
    public EcfXmlTotales Totales { get; set; } = null!;

    [XmlElement("OtraMoneda", Order = 8)]
    public EcfXmlOtraMoneda? OtraMoneda { get; set; }
    public bool ShouldSerializeOtraMoneda() => OtraMoneda != null;
}

