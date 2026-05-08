using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml.Simulation;

public class EcfXmlOtraMoneda
{
    [XmlElement("TipoMoneda")]
    public string? TipoMoneda { get; set; }

    [XmlElement("TipoCambio")]
    public decimal? TipoCambio { get; set; }
    public bool ShouldSerializeTipoCambio() => TipoCambio.HasValue;

    [XmlElement("MontoGravadoOtraMoneda")]
    public decimal? MontoGravadoOtraMoneda { get; set; }
    public bool ShouldSerializeMontoGravadoOtraMoneda() => MontoGravadoOtraMoneda.HasValue;

    [XmlElement("MontoExentoOtraMoneda")]
    public decimal? MontoExentoOtraMoneda { get; set; }
    public bool ShouldSerializeMontoExentoOtraMoneda() => MontoExentoOtraMoneda.HasValue;

    [XmlElement("TotalITBISOtraMoneda")]
    public decimal? TotalITBISOtraMoneda { get; set; }
    public bool ShouldSerializeTotalITBISOtraMoneda() => TotalITBISOtraMoneda.HasValue;

    [XmlElement("MontoTotalOtraMoneda")]
    public decimal? MontoTotalOtraMoneda { get; set; }
    public bool ShouldSerializeMontoTotalOtraMoneda() => MontoTotalOtraMoneda.HasValue;
}
