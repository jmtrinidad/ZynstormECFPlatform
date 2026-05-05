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

    [XmlIgnore]
    public EcfXmlComprador? Comprador { get; set; }

    [XmlElement("Comprador")]
    public EcfXmlComprador? CompradorStandard
    {
        get => (Comprador != null && Comprador.EcfType != 46 && Comprador.EcfType != 47 && Comprador.EcfType != 43) ? Comprador : null;
        set { }
    }

    [XmlElement("CompradorExp")]
    public EcfXmlCompradorExportacion? CompradorExportacion
    {
        get => (Comprador != null && (Comprador.EcfType == 46 || Comprador.EcfType == 47)) ? new EcfXmlCompradorExportacion(Comprador) : null;
        set { }
    }

    [XmlElement("InformacionesAdicionales")]
    public EcfXmlInformacionesAdicionales? InformacionesAdicionales { get; set; }
    public bool ShouldSerializeInformacionesAdicionales() => InformacionesAdicionales != null;

    [XmlElement("Transporte")]
    public EcfXmlTransporte? Transporte { get; set; }
    public bool ShouldSerializeTransporte() => Transporte != null;

    [XmlElement("Totales")]
    public EcfXmlTotales Totales { get; set; } = null!;
}
