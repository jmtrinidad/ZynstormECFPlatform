using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;IdDoc&gt; — document identification block.
/// 
/// Property declaration order is critical: XmlSerializer emits elements in
/// the order they are declared, and XSD validation is order-strict.
///
/// XSD element sequence by type (verified against each .xsd file):
///
///  Type 31: TipoeCF → eNCF → FechaVencimientoSecuencia → IndicadorEnvioDiferido? → IndicadorMontoGravado? → TipoIngresos → TipoPago → ...
///  Type 32: TipoeCF → eNCF → IndicadorEnvioDiferido? → IndicadorMontoGravado? → IndicadorServicioTodoIncluido? → TipoIngresos → TipoPago → ...
///  Type 33: TipoeCF → eNCF → FechaVencimientoSecuencia → IndicadorEnvioDiferido? → IndicadorMontoGravado? → IndicadorServicioTodoIncluido? → TipoIngresos → TipoPago → ...
///  Type 34: TipoeCF → eNCF → IndicadorNotaCredito → IndicadorEnvioDiferido? → IndicadorMontoGravado? → IndicadorServicioTodoIncluido? → TipoIngresos → TipoPago → ...
///  Type 41: TipoeCF → eNCF → FechaVencimientoSecuencia → IndicadorMontoGravado? → TipoPago? → ...
///  Type 43: TipoeCF → eNCF → FechaVencimientoSecuencia → TipoPago? → TotalPaginas?
///  Type 44: TipoeCF → eNCF → FechaVencimientoSecuencia → IndicadorServicioTodoIncluido? → TipoIngresos → TipoPago → ...
///  Type 45: TipoeCF → eNCF → FechaVencimientoSecuencia → IndicadorMontoGravado? → IndicadorServicioTodoIncluido? → TipoIngresos → TipoPago → ...
///  Type 46: TipoeCF → eNCF → FechaVencimientoSecuencia → TipoIngresos → TipoPago → ...
///  Type 47: TipoeCF → eNCF → FechaVencimientoSecuencia → TipoPago? → ...
///
/// Position 3 conflict: type 34 uses IndicadorNotaCredito at position 3, while all other types
/// use FechaVencimientoSecuencia (except type 32 which skips both).
/// Serialization: IndicadorNotaCredito emits for 34 (before FechaVencimientoSecuencia position),
/// FechaVencimientoSecuencia emits for all types except 32 and 34.
/// </summary>
public class EcfXmlIdDoc
{
    [XmlElement("TipoeCF")]
    public int EcfType { get; set; }

    [XmlElement("eNCF")]
    public string Ncf { get; set; } = null!;

    // ── Position 3: either IndicadorNotaCredito (type 34) or FechaVencimientoSecuencia (all except 32 & 34) ──

    /// <summary>Type 34 only — required at position 3 in XSD 34, before any optional indicators.</summary>
    [XmlElement("IndicadorNotaCredito")]
    public int? IndicadorNotaCredito { get; set; }
    public bool ShouldSerializeIndicadorNotaCredito() => EcfType == 34;

    /// <summary>Present in all types EXCEPT 32 (no expiration) and 34 (IndicadorNotaCredito takes position 3).</summary>
    [XmlElement("FechaVencimientoSecuencia")]
    public string SequenceExpirationDate { get; set; } = null!;
    public bool ShouldSerializeSequenceExpirationDate() => EcfType != 32 && EcfType != 34;

    // ── Optional indicators (subset varies by type) ────────────────────────────────────
    
    /// <summary>Present in types 31, 32, 33. NOT in 41, 43, 44, 45, 46, 47.</summary>
    [XmlElement("IndicadorEnvioDiferido")]
    public int? IndicadorEnvioDiferido { get; set; }
    public bool ShouldSerializeIndicadorEnvioDiferido() => IndicadorEnvioDiferido.HasValue;

    /// <summary>Present in types 31, 32, 33, 34, 41, 45. NOT in 43, 44, 46, 47.</summary>
    [XmlElement("IndicadorMontoGravado")]
    public int? IndicadorMontoGravado { get; set; }
    public bool ShouldSerializeIndicadorMontoGravado() => IndicadorMontoGravado.HasValue;

    /// <summary>Present in types 32, 33, 34, 44, 45. NOT in 31, 41, 43, 46, 47.</summary>
    [XmlElement("IndicadorServicioTodoIncluido")]
    public int? IndicadorServicioTodoIncluido { get; set; }
    public bool ShouldSerializeIndicadorServicioTodoIncluido() => IndicadorServicioTodoIncluido.HasValue;

    // ── TipoIngresos — present in 31, 32, 33, 34, 44, 45, 46. NOT in 41, 43, 47 ─────────
    [XmlElement("TipoIngresos")]
    public string IncomeType { get; set; } = "01";
    public bool ShouldSerializeIncomeType() =>
        EcfType == 31 || EcfType == 32 || EcfType == 33 || EcfType == 34 ||
        EcfType == 44 || EcfType == 45 || EcfType == 46;

    // ── Payment ───────────────────────────────────────────────────────────────────────────
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
