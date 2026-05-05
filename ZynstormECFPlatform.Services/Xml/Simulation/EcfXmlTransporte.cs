using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml.Simulation;

public class EcfXmlTransporte
{
    [XmlElement("ViaTransporte")]
    public string? ViaTransporte { get; set; }

    [XmlElement("PaisOrigen")]
    public string? PaisOrigen { get; set; }

    [XmlElement("DireccionDestino")]
    public string? DireccionDestino { get; set; }

    [XmlElement("PaisDestino")]
    public string? PaisDestino { get; set; }

    [XmlElement("RNCIdentificacionCompaniaTransportista")]
    public string? RncCompaniaTransportista { get; set; }

    [XmlElement("NombreCompaniaTransportista")]
    public string? NombreCompaniaTransportista { get; set; }

    [XmlElement("NumeroViaje")]
    public string? NumeroViaje { get; set; }

    [XmlElement("Conductor")]
    public string? Conductor { get; set; }

    [XmlElement("DocumentoTransporte")]
    public string? DocumentoTransporte { get; set; }

    [XmlElement("Ficha")]
    public string? Ficha { get; set; }

    [XmlElement("Placa")]
    public string? Placa { get; set; }

    [XmlElement("RutaTransporte")]
    public string? RutaTransporte { get; set; }

    [XmlElement("ZonaTransporte")]
    public string? ZonaTransporte { get; set; }

    [XmlElement("NumeroAlbaran")]
    public string? NumeroAlbaran { get; set; }
}

