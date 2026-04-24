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

    // ── Taxable amounts ────────────────

    [XmlIgnore]
    public decimal? MontoGravadoTotal { get; set; }
    [XmlElement("MontoGravadoTotal", Order = 1)]
    public string? MontoGravadoTotalString
    {
        get => MontoGravadoTotal?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoGravadoTotal = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoGravadoTotalString() => (EcfType == 46 || (EcfType != 46 && EcfType != 47)) && MontoGravadoTotal.HasValue;

    [XmlIgnore]
    public decimal? MontoGravadoI1 { get; set; }
    [XmlElement("MontoGravadoI1", Order = 2)]
    public string? MontoGravadoI1String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : MontoGravadoI1?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoGravadoI1 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoGravadoI1String() => EcfType != 46 && EcfType != 47 && MontoGravadoI1.HasValue;

    [XmlIgnore]
    public decimal? MontoGravadoI2 { get; set; }
    [XmlElement("MontoGravadoI2", Order = 3)]
    public string? MontoGravadoI2String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : MontoGravadoI2?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoGravadoI2 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoGravadoI2String() => EcfType != 46 && EcfType != 47 && MontoGravadoI2.HasValue;

    [XmlIgnore]
    public decimal? MontoGravadoI3 { get; set; }
    [XmlElement("MontoGravadoI3", Order = 4)]
    public string? MontoGravadoI3String
    {
        get => (EcfType == 47) ? null : MontoGravadoI3?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoGravadoI3 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoGravadoI3String() => (EcfType == 46 || (EcfType != 46 && EcfType != 47)) && MontoGravadoI3.HasValue;

    [XmlIgnore]
    public decimal? MontoExento { get; set; }
    [XmlElement("MontoExento", Order = 5)]
    public string? MontoExentoString
    {
        get => MontoExento?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoExento = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoExentoString() => EcfType != 46 && MontoExento.HasValue && MontoExento > 0;

    // ── ITBIS rates ────────────────────────────────────────────────────────

    [XmlIgnore]
    public int? ITBIS1 { get; set; }
    [XmlElement("ITBIS1", Order = 6)]
    public string? ITBIS1String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : ITBIS1?.ToString();
        set => ITBIS1 = string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }
    public bool ShouldSerializeITBIS1String() => ITBIS1.HasValue && EcfType != 46 && EcfType != 47;

    [XmlIgnore]
    public int? ITBIS2 { get; set; }
    [XmlElement("ITBIS2", Order = 7)]
    public string? ITBIS2String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : ITBIS2?.ToString();
        set => ITBIS2 = string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }
    public bool ShouldSerializeITBIS2String() => ITBIS2.HasValue && EcfType != 46 && EcfType != 47;

    [XmlIgnore]
    public int? ITBIS3 { get; set; }
    [XmlElement("ITBIS3", Order = 8)]
    public string? ITBIS3String
    {
        get => (EcfType == 47) ? null : ITBIS3?.ToString();
        set => ITBIS3 = string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }
    public bool ShouldSerializeITBIS3String() => EcfType != 47 && ITBIS3.HasValue;

    // ── ITBIS totals ───────────────────────────────────────────────────────

    [XmlIgnore]
    public decimal? TotalITBIS { get; set; }
    [XmlElement("TotalITBIS", Order = 9)]
    public string? TotalITBISString
    {
        get => (EcfType == 47) ? null : TotalITBIS?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBIS = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBISString() => EcfType != 47 && TotalITBIS.HasValue;

    [XmlIgnore]
    public decimal? TotalITBIS1 { get; set; }
    [XmlElement("TotalITBIS1", Order = 10)]
    public string? TotalITBIS1String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : TotalITBIS1?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBIS1 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBIS1String() => EcfType != 46 && EcfType != 47 && TotalITBIS1.HasValue;

    [XmlIgnore]
    public decimal? TotalITBIS2 { get; set; }
    [XmlElement("TotalITBIS2", Order = 11)]
    public string? TotalITBIS2String
    {
        get => (EcfType == 46 || EcfType == 47) ? null : TotalITBIS2?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBIS2 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBIS2String() => EcfType != 46 && EcfType != 47 && TotalITBIS2.HasValue;

    [XmlIgnore]
    public decimal? TotalITBIS3 { get; set; }
    [XmlElement("TotalITBIS3", Order = 12)]
    public string? TotalITBIS3String
    {
        get => (EcfType == 47) ? null : TotalITBIS3?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBIS3 = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBIS3String() => EcfType != 47 && TotalITBIS3.HasValue;

    // ── Additional taxes (ISC — Impuesto Selectivo al Consumo) ────────────

    [XmlIgnore]
    public decimal? MontoImpuestoAdicional { get; set; }
    [XmlElement("MontoImpuestoAdicional", Order = 13)]
    public string? MontoImpuestoAdicionalString
    {
        get => MontoImpuestoAdicional?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoImpuestoAdicional = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoImpuestoAdicionalString() => EcfType != 46 && EcfType != 47 && MontoImpuestoAdicional.HasValue && MontoImpuestoAdicional > 0;

    [XmlElement("ImpuestosAdicionales", Order = 14)]
    public EcfXmlImpuestosAdicionales? ImpuestosAdicionales { get; set; }
    public bool ShouldSerializeImpuestosAdicionales() => ImpuestosAdicionales != null && ImpuestosAdicionales.Items.Count > 0;

    // ── Grand total (required by XSD) ──────────────────────────────────────
    
    [XmlIgnore]
    public decimal MontoTotal { get; set; }
    [XmlElement("MontoTotal", Order = 15)]
    public string MontoTotalString
    {
        get => MontoTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoTotal = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }

    // ── Post-Total Sequence (Order is critical for XSD) ───────────────────

    [XmlIgnore]
    public decimal? MontoNoFacturable { get; set; }
    [XmlElement("MontoNoFacturable", Order = 16)]
    public string? MontoNoFacturableString
    {
        get => MontoNoFacturable?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoNoFacturable = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoNoFacturableString() => MontoNoFacturable.HasValue;

    [XmlIgnore]
    public decimal? MontoPeriodo { get; set; }
    [XmlElement("MontoPeriodo", Order = 17)]
    public string? MontoPeriodoString
    {
        get => MontoPeriodo?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoPeriodo = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoPeriodoString() => MontoPeriodo.HasValue;

    [XmlIgnore]
    public decimal? SaldoAnterior { get; set; }
    [XmlElement("SaldoAnterior", Order = 18)]
    public string? SaldoAnteriorString
    {
        get => SaldoAnterior?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => SaldoAnterior = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeSaldoAnteriorString() => SaldoAnterior.HasValue;

    [XmlIgnore]
    public decimal? MontoAvancePago { get; set; }
    [XmlElement("MontoAvancePago", Order = 19)]
    public string? MontoAvancePagoString
    {
        get => MontoAvancePago?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => MontoAvancePago = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeMontoAvancePagoString() => MontoAvancePago.HasValue;

    [XmlIgnore]
    public decimal? ValorPagar { get; set; }
    [XmlElement("ValorPagar", Order = 20)]
    public string? ValorPagarString
    {
        get => ValorPagar?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => ValorPagar = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeValorPagarString() => ValorPagar.HasValue;

    // ── Retentions ────────────

    [XmlIgnore]
    public decimal? TotalITBISRetenido { get; set; }
    [XmlElement("TotalITBISRetenido", Order = 21)]
    public string? TotalITBISRetenidoString
    {
        get => TotalITBISRetenido?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBISRetenido = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBISRetenidoString() => TotalITBISRetenido.HasValue && EcfType != 32 && EcfType != 43;

    [XmlIgnore]
    public decimal? TotalISRRetencion { get; set; }
    [XmlElement("TotalISRRetencion", Order = 22)]
    public string? TotalISRRetencionString
    {
        get => TotalISRRetencion?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalISRRetencion = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalISRRetencionString() => TotalISRRetencion.HasValue && TotalISRRetencion >= 0 && EcfType != 32 && EcfType != 43;

    [XmlIgnore]
    public decimal? TotalITBISPercepcion { get; set; }
    [XmlElement("TotalITBISPercepcion", Order = 23)]
    public string? TotalITBISPercepcionString
    {
        get => TotalITBISPercepcion?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalITBISPercepcion = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalITBISPercepcionString() => TotalITBISPercepcion.HasValue && EcfType != 32 && EcfType != 43;

    [XmlIgnore]
    public decimal? TotalISRPercepcion { get; set; }
    [XmlElement("TotalISRPercepcion", Order = 24)]
    public string? TotalISRPercepcionString
    {
        get => TotalISRPercepcion?.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        set => TotalISRPercepcion = string.IsNullOrEmpty(value) ? null : decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool ShouldSerializeTotalISRPercepcionString() => TotalISRPercepcion.HasValue && EcfType != 32 && EcfType != 43;
}
