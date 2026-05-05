using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml.Production;

/// <summary>
/// Specific Comprador structure for ECF Type 46 (Exportación) 
/// because DGII expects a completely different element sequence than the standard e-CF.
/// </summary>
public class EcfXmlCompradorExportacion
{
    public EcfXmlCompradorExportacion() { }

    public EcfXmlCompradorExportacion(EcfXmlComprador baseCmp)
    {
        EcfType = baseCmp.EcfType;
        RncComprador = baseCmp.RncComprador;
        IdentificadorExtranjero = baseCmp.IdentificadorExtranjero;
        RazonSocial = baseCmp.RazonSocial;
        ContactoComprador = baseCmp.ContactoComprador;
        CorreoComprador = baseCmp.CorreoComprador;
        DireccionComprador = baseCmp.DireccionComprador;
        MunicipioComprador = baseCmp.MunicipioComprador;
        ProvinciaComprador = baseCmp.ProvinciaComprador;
        PaisComprador = baseCmp.PaisComprador;
        TelefonoAdicional = baseCmp.TelefonoAdicional;
        FechaOrdenCompra = baseCmp.FechaOrdenCompra;
        NumeroOrdenCompra = baseCmp.NumeroOrdenCompra;
        CodigoInternoComprador = baseCmp.CodigoInternoComprador;
        ResponsablePago = baseCmp.ResponsablePago;
        InformacionAdicionalComprador = baseCmp.InformacionAdicionalComprador;
        FechaEntrega = baseCmp.FechaEntrega;
        ContactoEntrega = baseCmp.ContactoEntrega;
        DireccionEntrega = baseCmp.DireccionEntrega;
    }

    [XmlIgnore]
    public int EcfType { get; set; }

    [XmlElement("RNCComprador")]
    public string? RncComprador { get; set; }
    public bool ShouldSerializeRncComprador() => !string.IsNullOrWhiteSpace(RncComprador) && EcfType != 47;

    [XmlElement("IdentificadorExtranjero")]
    public string? IdentificadorExtranjero { get; set; }
    public bool ShouldSerializeIdentificadorExtranjero() => !string.IsNullOrWhiteSpace(IdentificadorExtranjero);

    [XmlElement("RazonSocialComprador")]
    public string? RazonSocial { get; set; }
    public bool ShouldSerializeRazonSocial() => !string.IsNullOrWhiteSpace(RazonSocial);

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

    [XmlElement("FechaEntrega")]
    public string? FechaEntrega { get; set; }
    public bool ShouldSerializeFechaEntrega() => !string.IsNullOrWhiteSpace(FechaEntrega) && EcfType != 47;

    [XmlElement("ContactoEntrega")]
    public string? ContactoEntrega { get; set; }
    public bool ShouldSerializeContactoEntrega() => !string.IsNullOrWhiteSpace(ContactoEntrega) && EcfType != 47;

    [XmlElement("DireccionEntrega")]
    public string? DireccionEntrega { get; set; }
    public bool ShouldSerializeDireccionEntrega() => !string.IsNullOrWhiteSpace(DireccionEntrega) && EcfType != 47;

    [XmlElement("TelefonoAdicional")]
    public string? TelefonoAdicional { get; set; }
    public bool ShouldSerializeTelefonoAdicional() => !string.IsNullOrWhiteSpace(TelefonoAdicional) && EcfType != 47;

    [XmlElement("FechaOrdenCompra")]
    public string? FechaOrdenCompra { get; set; }
    public bool ShouldSerializeFechaOrdenCompra() => !string.IsNullOrWhiteSpace(FechaOrdenCompra) && EcfType != 47;

    [XmlElement("NumeroOrdenCompra")]
    public string? NumeroOrdenCompra { get; set; }
    public bool ShouldSerializeNumeroOrdenCompra() => !string.IsNullOrWhiteSpace(NumeroOrdenCompra) && EcfType != 47;

    [XmlElement("CodigoInternoComprador")]
    public string? CodigoInternoComprador { get; set; }
    public bool ShouldSerializeCodigoInternoComprador() => !string.IsNullOrWhiteSpace(CodigoInternoComprador) && EcfType != 47;

    [XmlElement("ResponsablePago")]
    public string? ResponsablePago { get; set; }
    public bool ShouldSerializeResponsablePago() => !string.IsNullOrWhiteSpace(ResponsablePago) && EcfType != 47;

    [XmlElement("InformacionAdicionalComprador")]
    public string? InformacionAdicionalComprador { get; set; }
    public bool ShouldSerializeInformacionAdicionalComprador() => !string.IsNullOrWhiteSpace(InformacionAdicionalComprador) && EcfType != 47;
}

