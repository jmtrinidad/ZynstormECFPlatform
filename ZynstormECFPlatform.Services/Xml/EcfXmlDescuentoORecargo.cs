using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;DescuentoORecargo&gt; inside &lt;DescuentosORecargos&gt;.
/// </summary>
public class EcfXmlDescuentoORecargo
{
    [XmlElement("NumeroLinea")]
    public int NumeroLinea { get; set; }

    /// <summary>"D" = Descuento, "R" = Recargo.</summary>
    [XmlElement("TipoAjuste")]
    public string TipoAjuste { get; set; } = "D";

    public string? DescripcionDescuentooRecargo { get; set; }
    public bool ShouldSerializeDescripcionDescuentooRecargo() => DescripcionDescuentooRecargo != null;

    /// <summary>"$" (amount) or "%" (percentage).</summary>
    public string? TipoValor { get; set; }
    public bool ShouldSerializeTipoValor() => TipoValor != null;

    public decimal? MontoDescuentooRecargo { get; set; }
    public bool ShouldSerializeMontoDescuentooRecargo() => MontoDescuentooRecargo.HasValue;
}
