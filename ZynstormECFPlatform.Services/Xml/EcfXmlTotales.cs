using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Totales&gt; — invoice totals block.
/// All decimal? fields use ShouldSerialize to avoid emitting empty XML nodes.
/// Order has been explicitly set to ensure compliance with DGII schemas.
/// </summary>
public class EcfXmlTotales
{
    [XmlIgnore]
    public int EcfType { get; set; }

    // ── Taxable amounts ────────────────────────────────────────────────────

    [XmlElement(Order = 1)]
    public decimal? MontoGravadoTotal { get; set; }
    public bool ShouldSerializeMontoGravadoTotal() => MontoGravadoTotal.HasValue;

    [XmlElement(Order = 2)]
    public decimal? MontoGravadoI1 { get; set; }
    public bool ShouldSerializeMontoGravadoI1() => MontoGravadoI1.HasValue;

    [XmlElement(Order = 3)]
    public decimal? MontoGravadoI2 { get; set; }
    public bool ShouldSerializeMontoGravadoI2() => MontoGravadoI2.HasValue;

    [XmlElement(Order = 4)]
    public decimal? MontoGravadoI3 { get; set; }
    public bool ShouldSerializeMontoGravadoI3() => MontoGravadoI3.HasValue;

    [XmlElement(Order = 5)]
    public decimal? MontoExento { get; set; }
    public bool ShouldSerializeMontoExento() => MontoExento.HasValue && MontoExento > 0;

    // ── ITBIS rates ────────────────────────────────────────────────────────

    [XmlElement(Order = 6)]
    public int? ITBIS1 { get; set; }
    public bool ShouldSerializeITBIS1() => ITBIS1.HasValue;

    [XmlElement(Order = 7)]
    public int? ITBIS2 { get; set; }
    public bool ShouldSerializeITBIS2() => ITBIS2.HasValue;

    [XmlElement(Order = 8)]
    public int? ITBIS3 { get; set; }
    public bool ShouldSerializeITBIS3() => ITBIS3.HasValue;

    // ── ITBIS totals ───────────────────────────────────────────────────────

    [XmlElement(Order = 9)]
    public decimal? TotalITBIS { get; set; }
    public bool ShouldSerializeTotalITBIS() => TotalITBIS.HasValue;

    [XmlElement(Order = 10)]
    public decimal? TotalITBIS1 { get; set; }
    public bool ShouldSerializeTotalITBIS1() => TotalITBIS1.HasValue;

    [XmlElement(Order = 11)]
    public decimal? TotalITBIS2 { get; set; }
    public bool ShouldSerializeTotalITBIS2() => TotalITBIS2.HasValue;

    [XmlElement(Order = 12)]
    public decimal? TotalITBIS3 { get; set; }
    public bool ShouldSerializeTotalITBIS3() => TotalITBIS3.HasValue;

    // ── Additional taxes (ISC — Impuesto Selectivo al Consumo) ────────────

    [XmlElement("MontoImpuestoAdicional", Order = 13)]
    public decimal? MontoImpuestoAdicional { get; set; }
    public bool ShouldSerializeMontoImpuestoAdicional() => MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional > 0;

    [XmlElement("ImpuestosAdicionales", Order = 14)]
    public EcfXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0;

    // ── Grand total (required by XSD) ──────────────────────────────────────
    // This MUST precede MontoPeriodo, ValorPagar, and Retentions.

    [XmlElement("MontoTotal", Order = 15)]
    public decimal MontoTotal { get; set; }

    // ── Post-Total Sequence (Order is critical for XSD) ───────────────────

    [XmlElement("MontoNoFacturable", Order = 16)]
    public decimal? MontoNoFacturable { get; set; }
    public bool ShouldSerializeMontoNoFacturable() => MontoNoFacturable.HasValue;

    [XmlElement("MontoPeriodo", Order = 17)]
    public decimal? MontoPeriodo { get; set; }
    public bool ShouldSerializeMontoPeriodo() => MontoPeriodo.HasValue;

    [XmlElement("SaldoAnterior", Order = 18)]
    public decimal? SaldoAnterior { get; set; }
    public bool ShouldSerializeSaldoAnterior() => SaldoAnterior.HasValue;

    [XmlElement("MontoAvancePago", Order = 19)]
    public decimal? MontoAvancePago { get; set; }
    public bool ShouldSerializeMontoAvancePago() => MontoAvancePago.HasValue;

    [XmlElement("ValorPagar", Order = 20)]
    public decimal? ValorPagar { get; set; }
    public bool ShouldSerializeValorPagar() => ValorPagar.HasValue;

    // ── Retentions (ONLY for types allowing them, and MUST appear after ValorPagar)

    [XmlElement("TotalITBISRetenido", Order = 21)]
    public decimal? TotalITBISRetenido { get; set; }
    public bool ShouldSerializeTotalITBISRetenido() => TotalITBISRetenido.HasValue && EcfType != 32;

    [XmlElement("TotalISRRetencion", Order = 22)]
    public decimal? TotalISRRetencion { get; set; }
    public bool ShouldSerializeTotalISRRetencion() => TotalISRRetencion.HasValue && EcfType != 32;

    [XmlElement("TotalITBISPercepcion", Order = 23)]
    public decimal? TotalITBISPercepcion { get; set; }
    public bool ShouldSerializeTotalITBISPercepcion() => TotalITBISPercepcion.HasValue && EcfType != 32;

    [XmlElement("TotalISRPercepcion", Order = 24)]
    public decimal? TotalISRPercepcion { get; set; }
    public bool ShouldSerializeTotalISRPercepcion() => TotalISRPercepcion.HasValue && EcfType != 32;
}
