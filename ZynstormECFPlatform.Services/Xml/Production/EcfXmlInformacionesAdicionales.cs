using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml.Production;

public class EcfXmlInformacionesAdicionales
{
    [XmlElement("FechaEmbarque")]
    public string? FechaEmbarque { get; set; }

    [XmlElement("NumeroEmbarque")]
    public string? NumeroEmbarque { get; set; }

    [XmlElement("NumeroContenedor")]
    public string? NumeroContenedor { get; set; }

    [XmlElement("NumeroReferencia")]
    public string? NumeroReferencia { get; set; }

    [XmlElement("NombrePuertoEmbarque")]
    public string? NombrePuertoEmbarque { get; set; }

    [XmlElement("CondicionesEntrega")]
    public string? CondicionesEntrega { get; set; }

    [XmlElement("TotalFob")]
    public decimal? TotalFob { get; set; }
    public bool ShouldSerializeTotalFob() => TotalFob.HasValue;

    [XmlElement("Seguro")]
    public decimal? Seguro { get; set; }
    public bool ShouldSerializeSeguro() => Seguro.HasValue;

    [XmlElement("Flete")]
    public decimal? Flete { get; set; }
    public bool ShouldSerializeFlete() => Flete.HasValue;

    [XmlElement("OtrosGastos")]
    public decimal? OtrosGastos { get; set; }
    public bool ShouldSerializeOtrosGastos() => OtrosGastos.HasValue;

    [XmlElement("TotalCif")]
    public decimal? TotalCif { get; set; }
    public bool ShouldSerializeTotalCif() => TotalCif.HasValue;

    [XmlElement("RegimenAduanero")]
    public string? RegimenAduanero { get; set; }

    [XmlElement("NombrePuertoSalida")]
    public string? NombrePuertoSalida { get; set; }

    [XmlElement("NombrePuertoDesembarque")]
    public string? NombrePuertoDesembarque { get; set; }

    [XmlElement("PesoBruto")]
    public decimal? PesoBruto { get; set; }
    public bool ShouldSerializePesoBruto() => PesoBruto.HasValue;

    [XmlElement("PesoNeto")]
    public decimal? PesoNeto { get; set; }
    public bool ShouldSerializePesoNeto() => PesoNeto.HasValue;

    [XmlElement("UnidadPesoBruto")]
    public string? UnidadPesoBruto { get; set; }

    [XmlElement("UnidadPesoNeto")]
    public string? UnidadPesoNeto { get; set; }

    [XmlElement("CantidadBulto")]
    public decimal? CantidadBulto { get; set; }
    public bool ShouldSerializeCantidadBulto() => CantidadBulto.HasValue;

    [XmlElement("UnidadBulto")]
    public string? UnidadBulto { get; set; }

    [XmlElement("VolumenBulto")]
    public decimal? VolumenBulto { get; set; }
    public bool ShouldSerializeVolumenBulto() => VolumenBulto.HasValue;

    [XmlElement("UnidadVolumen")]
    public string? UnidadVolumen { get; set; }
}

