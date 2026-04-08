using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Comprador&gt; — buyer data block.
/// </summary>
public class EcfXmlComprador
{
    [XmlElement("RNCComprador")]
    public string RncComprador { get; set; } = null!;

    [XmlElement("RazonSocialComprador")]
    public string RazonSocial { get; set; } = null!;

    public string? ContactoComprador { get; set; }
    public bool ShouldSerializeContactoComprador() => ContactoComprador != null;

    public string? CorreoComprador { get; set; }
    public bool ShouldSerializeCorreoComprador() => CorreoComprador != null;

    public string? DireccionComprador { get; set; }
    public bool ShouldSerializeDireccionComprador() => DireccionComprador != null;

    public string? MunicipioComprador { get; set; }
    public bool ShouldSerializeMunicipioComprador() => MunicipioComprador != null;

    public string? ProvinciaComprador { get; set; }
    public bool ShouldSerializeProvinciaComprador() => ProvinciaComprador != null;

    public string? TelefonoAdicional { get; set; }
    public bool ShouldSerializeTelefonoAdicional() => TelefonoAdicional != null;
}
