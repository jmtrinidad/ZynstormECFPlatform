using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;IdDoc&gt; — document identification block.
/// 
/// Property declaration order is critical: XmlSerializer emits elements in
/// the order they are declared, and XSD validation is order-strict.
/// Order has been explicitly set to ensure compliance with DGII schemas.
/// </summary>
public class EcfXmlIdDoc
{
    [XmlElement("TipoeCF", Order = 1)]
    public int EcfType { get; set; }

    [XmlElement("eNCF", Order = 2)]
    public string Ncf { get; set; } = null!;

    // ── Position 3: either IndicadorNotaCredito (type 34) or FechaVencimientoSecuencia (all except 32 & 34) ──

    /// <summary>Type 34 only — required at position 3 in XSD 34, before any optional indicators.</summary>
    [XmlElement("IndicadorNotaCredito", Order = 3)]
    public int? IndicadorNotaCredito { get; set; }
    public bool ShouldSerializeIndicadorNotaCredito() => EcfType == 34;

    /// <summary>Present in all types EXCEPT 32 (no expiration) and 34 (IndicadorNotaCredito takes position 3).</summary>
    [XmlElement("FechaVencimientoSecuencia", Order = 4)]
    public string SequenceExpirationDate { get; set; } = null!;
    public bool ShouldSerializeSequenceExpirationDate() => EcfType != 32 && EcfType != 34;

    // ── Optional indicators (subset varies by type) ────────────────────────────────────
    
    /// <summary>Present in types 31, 32, 33. NOT in 41, 43, 44, 45, 46, 47.</summary>
    [XmlElement("IndicadorEnvioDiferido", Order = 5)]
    public int? IndicadorEnvioDiferido { get; set; }
    public bool ShouldSerializeIndicadorEnvioDiferido() => IndicadorEnvioDiferido.HasValue && 
        (EcfType == 31 || EcfType == 32 || EcfType == 33);

    /// <summary>Present in types 31, 32, 33, 34, 41, 45. NOT in 43, 44, 46, 47.</summary>
    [XmlElement("IndicadorMontoGravado", Order = 6)]
    public int? IndicadorMontoGravado { get; set; }
    public bool ShouldSerializeIndicadorMontoGravado() => IndicadorMontoGravado.HasValue && 
        (EcfType == 31 || EcfType == 32 || EcfType == 33 || EcfType == 34 || EcfType == 41 || EcfType == 45);

    /// <summary>Present in types 32, 33, 34, 44, 45. NOT in 31, 41, 43, 46, 47.</summary>
    [XmlElement("IndicadorServicioTodoIncluido", Order = 7)]
    public int? IndicadorServicioTodoIncluido { get; set; }
    public bool ShouldSerializeIndicadorServicioTodoIncluido() => IndicadorServicioTodoIncluido.HasValue && 
        (EcfType == 32 || EcfType == 33 || EcfType == 34 || EcfType == 44 || EcfType == 45);

    // ── TipoIngresos — present in 31, 32, 33, 34, 44, 45, 46. NOT in 41, 43, 47 ─────────
    [XmlElement("TipoIngresos", Order = 8)]
    public string? IncomeType { get; set; }
    public bool ShouldSerializeIncomeType() =>
        !string.IsNullOrWhiteSpace(IncomeType) &&
        (EcfType == 31 || EcfType == 32 || EcfType == 33 || EcfType == 34 ||
         EcfType == 44 || EcfType == 45 || EcfType == 46);


    // ── Payment ───────────────────────────────────────────────────────────────────────────
    [XmlElement("TipoPago", Order = 9)]
    public int? PaymentType { get; set; }
    public bool ShouldSerializePaymentType() => PaymentType.HasValue;


    [XmlElement("FechaLimitePago", Order = 10)]
    public string? FechaLimitePago { get; set; }
    public bool ShouldSerializeFechaLimitePago() => PaymentType == 2 && !string.IsNullOrWhiteSpace(FechaLimitePago);

    [XmlElement("TerminoPago", Order = 11)]
    public string? TerminoPago { get; set; }
    public bool ShouldSerializeTerminoPago() => !string.IsNullOrWhiteSpace(TerminoPago);

    [XmlElement("TotalPaginas", Order = 12)]
    public int? TotalPaginas { get; set; }
    public bool ShouldSerializeTotalPaginas() => TotalPaginas.HasValue;
}
