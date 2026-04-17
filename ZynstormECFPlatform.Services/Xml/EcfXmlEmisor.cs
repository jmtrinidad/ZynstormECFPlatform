using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Emisor&gt; — issuer data block.
/// The sequence of elements below is critical for XSD validation.
/// Order has been explicitly set to ensure compliance with DGII schemas.
/// </summary>
public class EcfXmlEmisor
{
    [XmlElement("RNCEmisor", Order = 1)]
    public string RncEmisor { get; set; } = null!;

    [XmlElement("RazonSocialEmisor", Order = 2)]
    public string RazonSocial { get; set; } = null!;

    [XmlElement("NombreComercial", Order = 3)]
    public string? NombreComercial { get; set; }
    public bool ShouldSerializeNombreComercial() => !string.IsNullOrWhiteSpace(NombreComercial);

    [XmlElement("Sucursal", Order = 4)]
    public string? Sucursal { get; set; }
    public bool ShouldSerializeSucursal() => !string.IsNullOrWhiteSpace(Sucursal);

    [XmlElement("DireccionEmisor", Order = 5)]
    public string Direccion { get; set; } = null!;

    [XmlElement("Municipio", Order = 6)]
    public string? Municipio { get; set; }
    public bool ShouldSerializeMunicipio() => !string.IsNullOrWhiteSpace(Municipio);

    [XmlElement("Provincia", Order = 7)]
    public string? Provincia { get; set; }
    public bool ShouldSerializeProvincia() => !string.IsNullOrWhiteSpace(Provincia);

    [XmlElement("TablaTelefonoEmisor", Order = 8)]
    public TablaTelefono? TelefonoTabla { get; set; }
    public bool ShouldSerializeTelefonoTabla() => TelefonoTabla != null && !string.IsNullOrWhiteSpace(TelefonoTabla.Telefono);

    public class TablaTelefono
    {
        [XmlElement("TelefonoEmisor")]
        public string Telefono { get; set; } = null!;
    }

    [XmlElement("CorreoEmisor", Order = 9)]
    public string? CorreoEmisor { get; set; }
    public bool ShouldSerializeCorreoEmisor() => !string.IsNullOrWhiteSpace(CorreoEmisor);

    [XmlElement("WebSite", Order = 10)]
    public string? WebSite { get; set; }
    public bool ShouldSerializeWebSite() => !string.IsNullOrWhiteSpace(WebSite);

    [XmlElement("ActividadEconomica", Order = 11)]
    public string? ActividadEconomica { get; set; }
    public bool ShouldSerializeActividadEconomica() => !string.IsNullOrWhiteSpace(ActividadEconomica);

    [XmlElement("CodigoVendedor", Order = 12)]
    public string? CodigoVendedor { get; set; }
    public bool ShouldSerializeCodigoVendedor() => !string.IsNullOrWhiteSpace(CodigoVendedor);

    [XmlElement("NumeroFacturaInterna", Order = 13)]
    public string? NumeroFacturaInterna { get; set; }
    public bool ShouldSerializeNumeroFacturaInterna() => !string.IsNullOrWhiteSpace(NumeroFacturaInterna);

    [XmlElement("NumeroPedidoInterno", Order = 14)]
    public string? NumeroPedidoInterno { get; set; }
    public bool ShouldSerializeNumeroPedidoInterno() => !string.IsNullOrWhiteSpace(NumeroPedidoInterno);

    [XmlElement("ZonaVenta", Order = 15)]
    public string? ZonaVenta { get; set; }
    public bool ShouldSerializeZonaVenta() => !string.IsNullOrWhiteSpace(ZonaVenta);

    /// <summary>FechaEmision must appear at the end of the Emisor block per XSD.</summary>
    [XmlElement("FechaEmision", Order = 16)]
    public string FechaEmision { get; set; } = null!;
}
