using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

// ─────────────────────────────────────────────────────────────────────────────
// Totales block — ImpuestosAdicionales
// Maps to XSD <ImpuestosAdicionales> inside <Totales>.
// Each entry represents one additional tax type (ISC, Propina, etc.)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Wrapper for <c>&lt;ImpuestosAdicionales&gt;</c> inside <c>&lt;Totales&gt;</c>.
/// Contains up to 20 <c>&lt;ImpuestoAdicional&gt;</c> entries.
/// </summary>
public class EcfXmlImpuestosAdicionales
{
    [XmlElement("ImpuestoAdicional")]
    public List<EcfXmlImpuestoAdicional> Items { get; set; } = [];
}

/// <summary>
/// A single additional tax entry inside <c>&lt;ImpuestosAdicionales&gt;</c>.
/// Represents one ISC category (e.g. Cerveza=006, Ron=014).
/// </summary>
public class EcfXmlImpuestoAdicional
{
    /// <summary>
    /// 3-digit DGII tax type code (CodificacionTipoImpuestosType).
    /// E.g. "006" = Cerveza, "013" = Whisky, "014" = Ron, "019" = Cigarrillos.
    /// Required.
    /// </summary>
    [XmlElement("TipoImpuesto")]
    public string TipoImpuesto { get; set; } = null!;

    /// <summary>
    /// Tax rate percentage (TasaImpuestoAdicional). Required.
    /// </summary>
    [XmlElement("TasaImpuestoAdicional")]
    public decimal TasaImpuestoAdicional { get; set; }

    /// <summary>
    /// ISC Específico — fixed amount per unit (optional).
    /// Maps to XSD element <c>MontoImpuestoSelectivoConsumoEspecifico</c>.
    /// </summary>
    public decimal? MontoImpuestoSelectivoConsumoEspecifico { get; set; }
    public bool ShouldSerializeMontoImpuestoSelectivoConsumoEspecifico()
        => MontoImpuestoSelectivoConsumoEspecifico.HasValue && MontoImpuestoSelectivoConsumoEspecifico > 0;

    /// <summary>
    /// ISC Ad-valorem — percentage-based amount (optional).
    /// Maps to XSD element <c>MontoImpuestoSelectivoConsumoAdvalorem</c>.
    /// </summary>
    public decimal? MontoImpuestoSelectivoConsumoAdvalorem { get; set; }
    public bool ShouldSerializeMontoImpuestoSelectivoConsumoAdvalorem()
        => MontoImpuestoSelectivoConsumoAdvalorem.HasValue && MontoImpuestoSelectivoConsumoAdvalorem > 0;

    /// <summary>
    /// Other additional taxes not covered by ISC Específico or Ad-valorem (optional).
    /// Maps to XSD element <c>OtrosImpuestosAdicionales</c>.
    /// </summary>
    public decimal? OtrosImpuestosAdicionales { get; set; }
    public bool ShouldSerializeOtrosImpuestosAdicionales()
        => OtrosImpuestosAdicionales.HasValue && OtrosImpuestosAdicionales > 0;
}

// ─────────────────────────────────────────────────────────────────────────────
// Item block — TablaImpuestoAdicional
// Maps to XSD <TablaImpuestoAdicional> inside each <Item>.
// References which tax types apply to that line item (max 2 per XSD).
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Wrapper for <c>&lt;TablaImpuestoAdicional&gt;</c> inside an <c>&lt;Item&gt;</c>.
/// Contains one or two <c>&lt;ImpuestoAdicional&gt;</c> entries referencing the tax type codes.
/// </summary>
public class EcfXmlTablaImpuestoAdicionalItem
{
    [XmlElement("ImpuestoAdicional")]
    public List<EcfXmlImpuestoAdicionalRef> ImpuestoAdicional { get; set; } = [];
}

/// <summary>
/// A reference to an additional tax type code inside an item's <c>&lt;TablaImpuestoAdicional&gt;</c>.
/// Only carries the <c>TipoImpuesto</c> code — amounts are aggregated at the <c>Totales</c> level.
/// </summary>
public class EcfXmlImpuestoAdicionalRef
{
    /// <summary>
    /// 3-digit DGII tax type code (same code used in Totales ImpuestosAdicionales).
    /// </summary>
    [XmlElement("TipoImpuesto")]
    public string TipoImpuesto { get; set; } = null!;
}
