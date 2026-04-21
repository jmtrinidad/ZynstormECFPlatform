using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

public class RfceXmlIdDoc
{
    [XmlElement("TipoeCF", Order = 1)]
    public int EcfType { get; set; } = 32;

    [XmlElement("eNCF", Order = 2)]
    public string Ncf { get; set; } = null!;

    [XmlElement("TipoIngresos", Order = 3)]
    public string? TipoIngresos { get; set; }

    [XmlElement("TipoPago", Order = 4)]
    public int? TipoPago { get; set; }

    [XmlElement("TablaFormasPago", Order = 5)]
    public RfceXmlTablaFormasPago? TablaFormasPago { get; set; }
    public bool ShouldSerializeTablaFormasPago() => TablaFormasPago != null && TablaFormasPago.FormasDePago.Count > 0;
}

public class RfceXmlTablaFormasPago
{
    [XmlElement("FormaDePago")]
    public List<RfceXmlFormaDePago> FormasDePago { get; set; } = new();
}

public class RfceXmlFormaDePago
{
    [XmlElement("FormaPago")]
    public int FormaPago { get; set; }

    [XmlElement("MontoPago")]
    public decimal MontoPago { get; set; }
}
