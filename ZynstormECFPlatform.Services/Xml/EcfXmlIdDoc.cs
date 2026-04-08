using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;IdDoc&gt; — document identification block.
/// </summary>
public class EcfXmlIdDoc
{
    [XmlElement("TipoeCF")]
    public int EcfType { get; set; }

    [XmlElement("eNCF")]
    public string Ncf { get; set; } = null!;

    [XmlElement("FechaVencimientoSecuencia")]
    public string SequenceExpirationDate { get; set; } = null!;

    // ── Optional indicators ────────────────────────────────────────────────

    public int? IndicadorEnvioDiferido { get; set; }
    public bool ShouldSerializeIndicadorEnvioDiferido() => IndicadorEnvioDiferido.HasValue;

    public int? IndicadorMontoGravado { get; set; }
    public bool ShouldSerializeIndicadorMontoGravado() => IndicadorMontoGravado.HasValue;

    [XmlElement("TipoIngresos")]
    public string IncomeType { get; set; } = "01";

    [XmlElement("TipoPago")]
    public int PaymentType { get; set; }

    public string? FechaLimitePago { get; set; }
    public bool ShouldSerializeFechaLimitePago() => FechaLimitePago != null;

    public string? TerminoPago { get; set; }
    public bool ShouldSerializeTerminoPago() => TerminoPago != null;
}
