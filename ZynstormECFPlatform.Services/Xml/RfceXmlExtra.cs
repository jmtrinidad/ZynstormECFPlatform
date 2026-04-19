using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

public class RfceXmlEmisor
{
    [XmlElement("RNCEmisor")]
    public string RncEmisor { get; set; } = null!;

    [XmlElement("RazonSocialEmisor")]
    public string RazonSocialEmisor { get; set; } = null!;

    [XmlElement("FechaEmision")]
    public string FechaEmision { get; set; } = null!;
}

public class RfceXmlComprador
{
    [XmlElement("RNCComprador")]
    public string? RncComprador { get; set; }

    [XmlElement("IdentificadorExtranjero")]
    public string? IdentificadorExtranjero { get; set; }

    [XmlElement("RazonSocialComprador")]
    public string? RazonSocialComprador { get; set; }
}

public class RfceXmlTotales
{
    [XmlElement("MontoGravadoTotal", Order = 1)]
    public decimal? MontoGravadoTotal { get; set; }
    public bool ShouldSerializeMontoGravadoTotal() => MontoGravadoTotal.HasValue;

    [XmlElement("MontoGravadoI1", Order = 2)]
    public decimal? MontoGravadoI1 { get; set; }
    public bool ShouldSerializeMontoGravadoI1() => MontoGravadoI1.HasValue;

    [XmlElement("MontoGravadoI2", Order = 3)]
    public decimal? MontoGravadoI2 { get; set; }
    public bool ShouldSerializeMontoGravadoI2() => MontoGravadoI2.HasValue;

    [XmlElement("MontoGravadoI3", Order = 4)]
    public decimal? MontoGravadoI3 { get; set; }
    public bool ShouldSerializeMontoGravadoI3() => MontoGravadoI3.HasValue;

    [XmlElement("MontoExento", Order = 5)]
    public decimal? MontoExento { get; set; }
    public bool ShouldSerializeMontoExento() => MontoExento.HasValue;

    [XmlElement("TotalITBIS", Order = 6)]
    public decimal? TotalITBIS { get; set; }
    public bool ShouldSerializeTotalITBIS() => TotalITBIS.HasValue;

    [XmlElement("TotalITBIS1", Order = 7)]
    public decimal? TotalITBIS1 { get; set; }
    public bool ShouldSerializeTotalITBIS1() => TotalITBIS1.HasValue;

    [XmlElement("TotalITBIS2", Order = 8)]
    public decimal? TotalITBIS2 { get; set; }
    public bool ShouldSerializeTotalITBIS2() => TotalITBIS2.HasValue;

    [XmlElement("TotalITBIS3", Order = 9)]
    public decimal? TotalITBIS3 { get; set; }
    public bool ShouldSerializeTotalITBIS3() => TotalITBIS3.HasValue;

    [XmlElement("MontoImpuestoAdicional", Order = 10)]
    public decimal? MontoImpuestoAdicional { get; set; }
    public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue;

    [XmlElement("ImpuestosAdicionales", Order = 11)]
    public RfceXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0;

    [XmlElement("MontoTotal", Order = 12)]
    public decimal MontoTotal { get; set; }

    [XmlElement("MontoNoFacturable", Order = 13)]
    public decimal? MontoNoFacturable { get; set; }
    public bool ShouldSerializeMontoNoFacturable() => MontoNoFacturable.HasValue;

    [XmlElement("MontoPeriodo", Order = 14)]
    public decimal? MontoPeriodo { get; set; }
    public bool ShouldSerializeMontoPeriodo() => MontoPeriodo.HasValue;
}

public class RfceXmlImpuestosAdicionales
{
    [XmlElement("ImpuestoAdicional")]
    public List<RfceXmlImpuestoAdicional> Items { get; set; } = new();
}

public class RfceXmlImpuestoAdicional
{
    [XmlElement("TipoImpuesto")]
    public string? TipoImpuesto { get; set; }

    [XmlElement("MontoImpuestoSelectivoConsumoEspecifico")]
    public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; }

    [XmlElement("MontoImpuestoSelectivoConsumoAdvalorem")]
    public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }

    [XmlElement("OtrosImpuestosAdicionales")]
    public decimal? OtrosImpuestosAdicionales { get; set; }
}
