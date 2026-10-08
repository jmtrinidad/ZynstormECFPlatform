using System.Xml.Serialization;
using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Services.Xml.Excel;

/// <summary>
/// Maps to XSD &lt;DescuentoORecargo&gt; inside &lt;DescuentosORecargos&gt;.
/// Element order follows the XSD; amounts are emitted verbatim from the Excel.
/// </summary>
public class EcfXmlDescuentoORecargo
{
    [XmlElement("NumeroLinea", Order = 1)]
    public int NumeroLinea { get; set; }

    /// <summary>"D" = Descuento, "R" = Recargo.</summary>
    [XmlElement("TipoAjuste", Order = 2)]
    public string TipoAjuste { get; set; } = "D";

    [XmlElement("DescripcionDescuentooRecargo", Order = 4)]
    public string? DescripcionDescuentooRecargo { get; set; }
    public bool ShouldSerializeDescripcionDescuentooRecargo() => !string.IsNullOrWhiteSpace(DescripcionDescuentooRecargo);

    /// <summary>"$" (amount) or "%" (percentage).</summary>
    [XmlElement("TipoValor", Order = 5)]
    public string? TipoValor { get; set; }
    public bool ShouldSerializeTipoValor() => !string.IsNullOrWhiteSpace(TipoValor);

    [XmlIgnore]
    public decimal? ValorDescuentooRecargo { get; set; }
    [XmlElement("ValorDescuentooRecargo", Order = 6)]
    public string? ValorDescuentooRecargoString
    {
        get => ExcelDecimal.Verbatim(ValorDescuentooRecargo);
        set => ValorDescuentooRecargo = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeValorDescuentooRecargoString() => ValorDescuentooRecargo.HasValue;

    [XmlIgnore]
    public decimal? MontoDescuentooRecargo { get; set; }
    [XmlElement("MontoDescuentooRecargo", Order = 7)]
    public string? MontoDescuentooRecargoString
    {
        get => ExcelDecimal.Verbatim(MontoDescuentooRecargo);
        set => MontoDescuentooRecargo = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeMontoDescuentooRecargoString() => MontoDescuentooRecargo.HasValue;

    [XmlElement("IndicadorFacturacionDescuentooRecargo", Order = 8)]
    public string? IndicadorFacturacionDescuentooRecargo { get; set; }
    public bool ShouldSerializeIndicadorFacturacionDescuentooRecargo() => !string.IsNullOrWhiteSpace(IndicadorFacturacionDescuentooRecargo);
}
