using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

public class RfceXmlEncabezado
{
    [XmlElement("Version", Order = 1)]
    public decimal Version { get; set; } = 1.0m;

    [XmlElement("IdDoc", Order = 2)]
    public RfceXmlIdDoc IdDoc { get; set; } = null!;

    [XmlElement("Emisor", Order = 3)]
    public RfceXmlEmisor Emisor { get; set; } = null!;

    [XmlElement("Comprador", Order = 4)]
    public RfceXmlComprador Comprador { get; set; } = null!;

    [XmlElement("Totales", Order = 5)]
    public RfceXmlTotales Totales { get; set; } = null!;

    [XmlElement("CodigoSeguridadeCF", Order = 6)]
    public string CodigoSeguridadeCF { get; set; } = null!;
}
