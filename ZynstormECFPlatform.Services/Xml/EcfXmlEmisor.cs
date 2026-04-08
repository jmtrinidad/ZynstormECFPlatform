using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Emisor&gt; — issuer data block.
/// The sequence of elements below is critical for XSD validation.
/// </summary>
public class EcfXmlEmisor
{
    [XmlElement("RNCEmisor")]
    public string RncEmisor { get; set; } = null!;

    [XmlElement("RazonSocialEmisor")]
    public string RazonSocial { get; set; } = null!;

    public string? NombreComercial { get; set; }
    public bool ShouldSerializeNombreComercial() => !string.IsNullOrWhiteSpace(NombreComercial);

    public string? Sucursal { get; set; }
    public bool ShouldSerializeSucursal() => !string.IsNullOrWhiteSpace(Sucursal);

    [XmlElement("DireccionEmisor")]
    public string Direccion { get; set; } = null!;

    public string? Municipio { get; set; }
    public bool ShouldSerializeMunicipio() => !string.IsNullOrWhiteSpace(Municipio);

    public string? Provincia { get; set; }
    public bool ShouldSerializeProvincia() => !string.IsNullOrWhiteSpace(Provincia);

    [XmlElement("TablaTelefonoEmisor")]
    public TablaTelefono? TelefonoTabla { get; set; }
    public bool ShouldSerializeTelefonoTabla() => TelefonoTabla != null && !string.IsNullOrWhiteSpace(TelefonoTabla.Telefono);

    public class TablaTelefono
    {
        [XmlElement("TelefonoEmisor")]
        public string Telefono { get; set; } = null!;
    }

    public string? CorreoEmisor { get; set; }
    public bool ShouldSerializeCorreoEmisor() => !string.IsNullOrWhiteSpace(CorreoEmisor);

    public string? WebSite { get; set; }
    public bool ShouldSerializeWebSite() => !string.IsNullOrWhiteSpace(WebSite);

    public string? ActividadEconomica { get; set; }
    public bool ShouldSerializeActividadEconomica() => !string.IsNullOrWhiteSpace(ActividadEconomica);

    public string? CodigoVendedor { get; set; }
    public bool ShouldSerializeCodigoVendedor() => !string.IsNullOrWhiteSpace(CodigoVendedor);

    /// <summary>FechaEmision must appear at the end of the Emisor block per XSD.</summary>
    [XmlElement("FechaEmision")]
    public string FechaEmision { get; set; } = null!;
}

