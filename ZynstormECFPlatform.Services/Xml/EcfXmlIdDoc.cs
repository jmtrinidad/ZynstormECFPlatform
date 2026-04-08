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

    // ── Note Indicators (Types 33, 34) ───────────────────────────────────
    
    [XmlElement("IndicadorNotaDebito")]
    public int? IndicadorNotaDebito { get; set; }
    public bool ShouldSerializeIndicadorNotaDebito() => EcfType == 33;

    [XmlElement("IndicadorNotaCredito")]
    public int? IndicadorNotaCredito { get; set; }
    public bool ShouldSerializeIndicadorNotaCredito() => EcfType == 34;

    [XmlElement("FechaVencimientoSecuencia")]
    public string SequenceExpirationDate { get; set; } = null!;

    // ── Mandatory/Optional indicators in strict sequence ───────────────────

    public int? IndicadorEnvioDiferido { get; set; }
    public bool ShouldSerializeIndicadorEnvioDiferido() => IndicadorEnvioDiferido.HasValue;

    public int? IndicadorMontoGravado { get; set; }
    public bool ShouldSerializeIndicadorMontoGravado() => IndicadorMontoGravado.HasValue;

    [XmlElement("TipoIngresos")]
    public string IncomeType { get; set; } = "01";
    // Many XSDs (41, 43, 44, 45, 46) require this. Consumo (32) and Pagos Exterior (47) don't have it.
    public bool ShouldSerializeIncomeType() => EcfType != 32 && EcfType != 47;


    [XmlElement("TipoPago")]
    public int PaymentType { get; set; }

    [XmlElement("FechaLimitePago")]
    public string? FechaLimitePago { get; set; }
    public bool ShouldSerializeFechaLimitePago() => PaymentType == 2 && !string.IsNullOrEmpty(FechaLimitePago);

    [XmlElement("TerminoPago")]
    public string? TerminoPago { get; set; }
    public bool ShouldSerializeTerminoPago() => !string.IsNullOrEmpty(TerminoPago);


    [XmlElement("TotalPaginas")]
    public int? TotalPaginas { get; set; }
    public bool ShouldSerializeTotalPaginas() => TotalPaginas.HasValue;
}

