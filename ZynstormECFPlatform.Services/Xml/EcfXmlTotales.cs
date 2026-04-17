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
    public bool ShouldSerializeMontoGravadoTotal() => MontoGravadoTotal.HasValue && EcfType <= 34;

    public decimal? MontoGravadoI1 { get; set; }
    public bool ShouldSerializeMontoGravadoI1() => MontoGravadoI1.HasValue && EcfType <= 34;

    public decimal? MontoGravadoI2 { get; set; }
    public bool ShouldSerializeMontoGravadoI2() => MontoGravadoI2.HasValue && EcfType <= 34;

    public decimal? MontoGravadoI3 { get; set; }
    public bool ShouldSerializeMontoGravadoI3() => MontoGravadoI3.HasValue && EcfType <= 34;

    public decimal? MontoExento { get; set; }
    public bool ShouldSerializeMontoExento() => MontoExento.HasValue && MontoExento > 0;

    // ── ITBIS rates ────────────────────────────────────────────────────────

    public int? ITBIS1 { get; set; }
    public bool ShouldSerializeITBIS1() => ITBIS1.HasValue && EcfType <= 34;

    public int? ITBIS2 { get; set; }
    public bool ShouldSerializeITBIS2() => ITBIS2.HasValue && EcfType <= 34;

    public int? ITBIS3 { get; set; }
    public bool ShouldSerializeITBIS3() => ITBIS3.HasValue && EcfType <= 34;

    // ── ITBIS totals ───────────────────────────────────────────────────────

    public decimal? TotalITBIS { get; set; }
    public bool ShouldSerializeTotalITBIS() => TotalITBIS.HasValue && EcfType <= 34;

    public decimal? TotalITBIS1 { get; set; }
    public bool ShouldSerializeTotalITBIS1() => TotalITBIS1.HasValue && EcfType <= 34;

    public decimal? TotalITBIS2 { get; set; }
    public bool ShouldSerializeTotalITBIS2() => TotalITBIS2.HasValue && EcfType <= 34;

    public decimal? TotalITBIS3 { get; set; }
    public bool ShouldSerializeTotalITBIS3() => TotalITBIS3.HasValue && EcfType <= 34;

    // ── Additional taxes (ISC — Impuesto Selectivo al Consumo) ────────────

    public decimal? MontoImpuestoAdicional { get; set; }
    public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional > 0 && EcfType <= 34;

    public EcfXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0 && EcfType <= 34;

    // ── Grand total (required by XSD) ──────────────────────────────────────

    [XmlElement("MontoTotal")]
    public decimal MontoTotal { get; set; }

    // ── Retentions (optional) ──────────────────────────────────────────────

    public decimal? TotalITBISRetenido { get; set; }
    public bool ShouldSerializeTotalITBISRetenido() => TotalITBISRetenido.HasValue && EcfType != 43;

    public decimal? TotalISRRetencion { get; set; }
    public bool ShouldSerializeTotalISRRetencion() => TotalISRRetencion.HasValue && EcfType != 43;

    // ── Additional fields for Type 32 ─────────────────────────────────────

    public decimal? MontoPeriodo { get; set; }
    public bool ShouldSerializeMontoPeriodo() => MontoPeriodo.HasValue && EcfType == 32;

    public decimal? ValorPagar { get; set; }
    public bool ShouldSerializeValorPagar() => ValorPagar.HasValue && EcfType == 32;
}

