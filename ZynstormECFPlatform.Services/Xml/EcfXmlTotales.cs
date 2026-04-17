using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Totales&gt; — invoice totals block.
/// All decimal? fields use ShouldSerialize to avoid emitting empty XML nodes.
/// </summary>
public class EcfXmlTotales
{
    [XmlIgnore]
    public int EcfType { get; set; }

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

    [XmlElement("MontoImpuestoAdicional")]
    public decimal? MontoImpuestoAdicional { get; set; }
    public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional > 0;

    [XmlElement("ImpuestosAdicionales")]
    public EcfXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0;

    // ── Grand total (required by XSD) ──────────────────────────────────────
    // This MUST precede MontoPeriodo, ValorPagar, and Retentions.

    [XmlElement("MontoTotal")]
    public decimal MontoTotal { get; set; }

    // ── Post-Total Sequence (Order is critical for XSD) ───────────────────

    [XmlElement("MontoNoFacturable")]
    public decimal? MontoNoFacturable { get; set; }
    public bool ShouldSerializeMontoNoFacturable() => MontoNoFacturable.HasValue;

    [XmlElement("MontoPeriodo")]
    public decimal? MontoPeriodo { get; set; }
    public bool ShouldSerializeMontoPeriodo() => MontoPeriodo.HasValue;

    [XmlElement("SaldoAnterior")]
    public decimal? SaldoAnterior { get; set; }
    public bool ShouldSerializeSaldoAnterior() => SaldoAnterior.HasValue;

    [XmlElement("MontoAvancePago")]
    public decimal? MontoAvancePago { get; set; }
    public bool ShouldSerializeMontoAvancePago() => MontoAvancePago.HasValue;

    [XmlElement("ValorPagar")]
    public decimal? ValorPagar { get; set; }
    public bool ShouldSerializeValorPagar() => ValorPagar.HasValue;

    // ── Retentions (ONLY for types allowing them, and MUST appear after ValorPagar)

    [XmlElement("TotalITBISRetenido")]
    public decimal? TotalITBISRetenido { get; set; }
    public bool ShouldSerializeTotalITBISRetenido() => TotalITBISRetenido.HasValue && EcfType != 32 && EcfType != 43;

    [XmlElement("TotalISRRetencion")]
    public decimal? TotalISRRetencion { get; set; }
    public bool ShouldSerializeTotalISRRetencion() => TotalISRRetencion.HasValue && EcfType != 32 && EcfType != 43;

    [XmlElement("TotalITBISPercepcion")]
    public decimal? TotalITBISPercepcion { get; set; }
    public bool ShouldSerializeTotalITBISPercepcion() => TotalITBISPercepcion.HasValue && EcfType != 32 && EcfType != 43;

    [XmlElement("TotalISRPercepcion")]
    public decimal? TotalISRPercepcion { get; set; }
    public bool ShouldSerializeTotalISRPercepcion() => TotalISRPercepcion.HasValue && EcfType != 32 && EcfType != 43;
}
