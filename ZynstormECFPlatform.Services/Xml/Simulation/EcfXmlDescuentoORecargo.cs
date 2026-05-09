using System.Xml.Serialization;
using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Services.Xml.Simulation;

/// <summary>
/// Maps to XSD <DescuentoORecargo> inside <DescuentosORecargos>.
/// Order is critical for XSD validation.
/// </summary>
public class EcfXmlDescuentoORecargo
{
    [XmlElement("NumeroLinea", Order = 1)]
    public int NumeroLinea { get; set; }

    /// <summary>"D" = Descuento, "R" = Recargo.</summary>
    [XmlElement("TipoAjuste", Order = 2)]
    public string TipoAjuste { get; set; } = "D";

    [XmlElement("DescripcionDescuentooRecargo", Order = 3)]
    public string? DescripcionDescuentooRecargo { get; set; }
    public bool ShouldSerializeDescripcionDescuentooRecargo() => !string.IsNullOrWhiteSpace(DescripcionDescuentooRecargo);

    /// <summary>"$" (amount) or "%" (percentage).</summary>
    [XmlElement("TipoValor", Order = 4)]
    public string? TipoValor { get; set; }
    public bool ShouldSerializeTipoValor() => !string.IsNullOrWhiteSpace(TipoValor);

    [XmlIgnore]
    public decimal? ValorDescuentooRecargo { get; set; }
    [XmlElement("ValorDescuentooRecargo", Order = 5)]
    public string? ValorDescuentooRecargoString
    {
        get => Tools.FormatDecimal(ValorDescuentooRecargo);
        set => ValorDescuentooRecargo = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeValorDescuentooRecargoString() => ValorDescuentooRecargo.HasValue;

    [XmlIgnore]
    public decimal? MontoDescuentooRecargo { get; set; }
    [XmlElement("MontoDescuentooRecargo", Order = 6)]
    public string? MontoDescuentooRecargoString
    {
        get => Tools.FormatDecimal(MontoDescuentooRecargo);
        set => MontoDescuentooRecargo = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeMontoDescuentooRecargoString() => MontoDescuentooRecargo.HasValue;

    [XmlIgnore]
    public decimal? MontoDescuentooRecargoOtraMoneda { get; set; }
    [XmlElement("MontoDescuentooRecargoOtraMoneda", Order = 7)]
    public string? MontoDescuentooRecargoOtraMonedaString
    {
        get => Tools.FormatDecimal(MontoDescuentooRecargoOtraMoneda);
        set => MontoDescuentooRecargoOtraMoneda = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeMontoDescuentooRecargoOtraMonedaString() => MontoDescuentooRecargoOtraMoneda.HasValue;

    [XmlElement("IndicadorFacturacionDescuentooRecargo", Order = 8)]
    public int IndicadorFacturacion { get; set; }
}
