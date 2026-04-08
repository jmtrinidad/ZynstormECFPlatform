using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Comprador&gt; — buyer data block.
/// The sequence of elements below is critical for XSD validation.
/// </summary>
public class EcfXmlComprador
{
    [XmlElement("RNCComprador")]
    public string RncComprador { get; set; } = null!;

    [XmlElement("RazonSocialComprador")]
    public string RazonSocial { get; set; } = null!;

    public string? ContactoComprador { get; set; }
    public bool ShouldSerializeContactoComprador() => !string.IsNullOrWhiteSpace(ContactoComprador);

    public string? CorreoComprador { get; set; }
    public bool ShouldSerializeCorreoComprador() => !string.IsNullOrWhiteSpace(CorreoComprador);

    public string? DireccionComprador { get; set; }
    public bool ShouldSerializeDireccionComprador() => !string.IsNullOrWhiteSpace(DireccionComprador);

    public string? MunicipioComprador { get; set; }
    public bool ShouldSerializeMunicipioComprador() => !string.IsNullOrWhiteSpace(MunicipioComprador);

    public string? ProvinciaComprador { get; set; }
    public bool ShouldSerializeProvinciaComprador() => !string.IsNullOrWhiteSpace(ProvinciaComprador);

    // Note: Other optional fields like FechaEntrega, ContactoEntrega belong here in XSD order.

    public string? TelefonoAdicional { get; set; }
    public bool ShouldSerializeTelefonoAdicional() => !string.IsNullOrWhiteSpace(TelefonoAdicional);
}

