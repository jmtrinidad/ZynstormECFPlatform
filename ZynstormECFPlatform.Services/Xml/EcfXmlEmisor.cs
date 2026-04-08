using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Emisor&gt; — issuer data block.
/// </summary>
public class EcfXmlEmisor
{
    [XmlElement("RNCEmisor")]
    public string RncEmisor { get; set; } = null!;

    [XmlElement("RazonSocialEmisor")]
    public string RazonSocial { get; set; } = null!;

    public string? NombreComercial { get; set; }
    public bool ShouldSerializeNombreComercial() => NombreComercial != null;

    public string? Sucursal { get; set; }
    public bool ShouldSerializeSucursal() => Sucursal != null;

    [XmlElement("DireccionEmisor")]
    public string Direccion { get; set; } = null!;

    public string? Municipio { get; set; }
    public bool ShouldSerializeMunicipio() => Municipio != null;

    public string? Provincia { get; set; }
    public bool ShouldSerializeProvincia() => Provincia != null;

    [XmlElement("TablaTelefonoEmisor")]
    public TablaTelefono? TelefonoTabla { get; set; }
    public bool ShouldSerializeTelefonoTabla() => TelefonoTabla != null;

    public class TablaTelefono
    {
        [XmlElement("TelefonoEmisor")]
        public string Telefono { get; set; } = null!;
    }

    public string? CorreoEmisor { get; set; }
    public bool ShouldSerializeCorreoEmisor() => CorreoEmisor != null;

    public string? WebSite { get; set; }
    public bool ShouldSerializeWebSite() => WebSite != null;

    public string? ActividadEconomica { get; set; }
    public bool ShouldSerializeActividadEconomica() => ActividadEconomica != null;

    public string? CodigoVendedor { get; set; }
    public bool ShouldSerializeCodigoVendedor() => CodigoVendedor != null;

    /// <summary>FechaEmision maps to the invoice issue date.</summary>
    [XmlElement("FechaEmision")]
    public string FechaEmision { get; set; } = null!;
}
