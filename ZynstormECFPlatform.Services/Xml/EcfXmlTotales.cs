using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Totales&gt; — invoice totals block.
/// All decimal? fields use ShouldSerialize to avoid emitting empty XML nodes.
/// </summary>
public class EcfXmlTotales
{
    // ── Taxable amounts ────────────────────────────────────────────────────

    public decimal? MontoGravadoTotal { get; set; }
    public bool ShouldSerializeMontoGravadoTotal() => MontoGravadoTotal.HasValue;

    public decimal? MontoGravadoI1 { get; set; }
    public bool ShouldSerializeMontoGravadoI1() => MontoGravadoI1.HasValue;

    public decimal? MontoGravadoI2 { get; set; }
    public bool ShouldSerializeMontoGravadoI2() => MontoGravadoI2.HasValue;

    public decimal? MontoGravadoI3 { get; set; }
    public bool ShouldSerializeMontoGravadoI3() => MontoGravadoI3.HasValue;

    public decimal? MontoExento { get; set; }
    public bool ShouldSerializeMontoExento() => MontoExento.HasValue && MontoExento > 0;

    // ── ITBIS rates ────────────────────────────────────────────────────────

    public int? ITBIS1 { get; set; }
    public bool ShouldSerializeITBIS1() => ITBIS1.HasValue;

    public int? ITBIS2 { get; set; }
    public bool ShouldSerializeITBIS2() => ITBIS2.HasValue;

    public int? ITBIS3 { get; set; }
    public bool ShouldSerializeITBIS3() => ITBIS3.HasValue;

    // ── ITBIS totals ───────────────────────────────────────────────────────

    public decimal? TotalITBIS { get; set; }
    public bool ShouldSerializeTotalITBIS() => TotalITBIS.HasValue;

    public decimal? TotalITBIS1 { get; set; }
    public bool ShouldSerializeTotalITBIS1() => TotalITBIS1.HasValue;

    public decimal? TotalITBIS2 { get; set; }
    public bool ShouldSerializeTotalITBIS2() => TotalITBIS2.HasValue;

    public decimal? TotalITBIS3 { get; set; }
    public bool ShouldSerializeTotalITBIS3() => TotalITBIS3.HasValue;

    // ── Additional taxes (ISC — Impuesto Selectivo al Consumo) ────────────

    /// <summary>
    /// Total sum of all additional taxes (ISC Específico + Ad-valorem + Otros)
    /// across all items. Maps to XSD element <c>MontoImpuestoAdicional</c>.
    /// Only emitted when at least one item carries an ISC type.
    /// </summary>
    public decimal? MontoImpuestoAdicional { get; set; }
    public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional > 0;

    /// <summary>
    /// Detail of additional taxes grouped by TipoImpuesto.
    /// Maps to XSD element <c>ImpuestosAdicionales</c>.
    /// Only emitted when MontoImpuestoAdicional is present.
    /// </summary>
    public EcfXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0;

    // ── Grand total (required by XSD) ──────────────────────────────────────

    [XmlElement("MontoTotal")]
    public decimal MontoTotal { get; set; }

    // ── Retentions (optional) ──────────────────────────────────────────────

    public decimal? TotalITBISRetenido { get; set; }
    public bool ShouldSerializeTotalITBISRetenido() => TotalITBISRetenido.HasValue;

    public decimal? TotalISRRetencion { get; set; }
    public bool ShouldSerializeTotalISRRetencion() => TotalISRRetencion.HasValue;
}
