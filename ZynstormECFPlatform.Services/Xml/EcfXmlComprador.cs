using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Comprador&gt; — buyer data block.
/// The sequence of elements below is critical for XSD validation.
/// </summary>
public class EcfXmlComprador
{
    [XmlIgnore]
    public int EcfType { get; set; }

    [XmlElement("RNCComprador")]
    public string? RncComprador { get; set; }

    public bool ShouldSerializeRncComprador() => !string.IsNullOrEmpty(RncComprador) && EcfType != 47;


    [XmlElement("IdentificadorExtranjero")]
    public string? IdentificadorExtranjero { get; set; }
    public bool ShouldSerializeIdentificadorExtranjero() => !string.IsNullOrEmpty(IdentificadorExtranjero);

    [XmlElement("RazonSocialComprador")]
    public string RazonSocial { get; set; } = null!;

    [XmlElement("ContactoComprador")]
    public string? ContactoComprador { get; set; }
    public bool ShouldSerializeContactoComprador() => !string.IsNullOrWhiteSpace(ContactoComprador) && EcfType != 47;

    [XmlElement("CorreoComprador")]
    public string? CorreoComprador { get; set; }
    public bool ShouldSerializeCorreoComprador() => !string.IsNullOrWhiteSpace(CorreoComprador) && EcfType != 47;

    [XmlElement("DireccionComprador")]
    public string? DireccionComprador { get; set; }
    public bool ShouldSerializeDireccionComprador() => !string.IsNullOrWhiteSpace(DireccionComprador) && EcfType != 47;

    [XmlElement("MunicipioComprador")]
    public string? MunicipioComprador { get; set; }
    public bool ShouldSerializeMunicipioComprador() => !string.IsNullOrWhiteSpace(MunicipioComprador) && EcfType != 47;

    [XmlElement("ProvinciaComprador")]
    public string? ProvinciaComprador { get; set; }
    public bool ShouldSerializeProvinciaComprador() => !string.IsNullOrWhiteSpace(ProvinciaComprador) && EcfType != 47;

    [XmlElement("PaisComprador")]
    public string? PaisComprador { get; set; }
    public bool ShouldSerializePaisComprador() => !string.IsNullOrWhiteSpace(PaisComprador) && EcfType != 47;

    [XmlElement("TelefonoAdicional")]
    public string? TelefonoAdicional { get; set; }
    public bool ShouldSerializeTelefonoAdicional() => !string.IsNullOrWhiteSpace(TelefonoAdicional) && EcfType != 47;

    [XmlElement("FechaEntrega")]
    public string? FechaEntrega { get; set; }
    public bool ShouldSerializeFechaEntrega() => !string.IsNullOrWhiteSpace(FechaEntrega);

    [XmlElement("FechaOrdenCompra")]
    public string? FechaOrdenCompra { get; set; }
    public bool ShouldSerializeFechaOrdenCompra() => !string.IsNullOrWhiteSpace(FechaOrdenCompra);

    [XmlElement("NumeroOrdenCompra")]
    public string? NumeroOrdenCompra { get; set; }
    public bool ShouldSerializeNumeroOrdenCompra() => !string.IsNullOrWhiteSpace(NumeroOrdenCompra);

    [XmlElement("CodigoInternoComprador")]
    public string? CodigoInternoComprador { get; set; }
    public bool ShouldSerializeCodigoInternoComprador() => !string.IsNullOrWhiteSpace(CodigoInternoComprador);
}

