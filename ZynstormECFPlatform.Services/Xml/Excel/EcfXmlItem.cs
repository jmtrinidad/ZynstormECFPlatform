using System.Collections.Generic;
using System.Xml.Serialization;
using ZynstormECFPlatform.Common.Utilities;

namespace ZynstormECFPlatform.Services.Xml.Excel;

/// <summary>
/// Maps to XSD &lt;Item&gt; inside &lt;DetallesItems&gt;.
/// </summary>
public class EcfXmlItem
{
    [XmlIgnore]
    public int EcfType { get; set; }

    [XmlElement("NumeroLinea", Order = 1)]
    public int NumeroLinea { get; set; }

    [XmlElement("IndicadorFacturacion", Order = 2)]
    public int? IndicadorFacturacion { get; set; }
    public bool ShouldSerializeIndicadorFacturacion() => IndicadorFacturacion.HasValue;

    private EcfXmlItemRetencion? _retencion;

    [XmlElement("Retencion", Order = 3)]
    public EcfXmlItemRetencion? Retencion
    {
        get
        {
            if (EcfType is 41 or 47 && _retencion != null)
            {
                _retencion.EcfType = EcfType;
                return _retencion;
            }
            return null;
        }
        set => _retencion = value;
    }


    public bool ShouldSerializeRetencion() => Retencion != null;


    [XmlElement("NombreItem", Order = 4)]
    public string Name { get; set; } = null!;

    [XmlElement("IndicadorBienoServicio", Order = 5)]
    public int? ItemType { get; set; }
    public bool ShouldSerializeItemType() => ItemType.HasValue;

    [XmlElement("DescripcionItem", Order = 6)]
    public string? DescripcionItem { get; set; }
    public bool ShouldSerializeDescripcionItem() => DescripcionItem != null;

    [XmlIgnore]
    public decimal CantidadItem { get; set; }

    [XmlElement("CantidadItem", Order = 7)]
    public string CantidadItemString
    {
        get => ExcelDecimal.Verbatim(CantidadItem) ?? "0";
        set => CantidadItem = Tools.ParseDecimal(value) ?? 0m;
    }

    [XmlElement("UnidadMedida", Order = 8)]
    public int? UnidadMedida { get; set; }
    public bool ShouldSerializeUnidadMedida() => UnidadMedida.HasValue;

    // ── Reference fields for regulated goods (ISC / alcohol). Verbatim from Excel.
    //    XSD order: after UnidadMedida, before FechaElaboracion. ────────────────────

    [XmlIgnore]
    public decimal? CantidadReferencia { get; set; }
    [XmlElement("CantidadReferencia", Order = 9)]
    public string? CantidadReferenciaString
    {
        get => ExcelDecimal.Verbatim(CantidadReferencia);
        set => CantidadReferencia = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeCantidadReferenciaString() => CantidadReferencia.HasValue;

    [XmlElement("UnidadReferencia", Order = 10)]
    public int? UnidadReferencia { get; set; }
    public bool ShouldSerializeUnidadReferencia() => UnidadReferencia.HasValue;

    [XmlElement("TablaSubcantidad", Order = 11)]
    public EcfXmlTablaSubcantidad? TablaSubcantidad { get; set; }
    public bool ShouldSerializeTablaSubcantidad() => TablaSubcantidad != null && TablaSubcantidad.SubcantidadItem.Count > 0;

    [XmlIgnore]
    public decimal? GradosAlcohol { get; set; }
    [XmlElement("GradosAlcohol", Order = 12)]
    public string? GradosAlcoholString
    {
        get => ExcelDecimal.Verbatim(GradosAlcohol);
        set => GradosAlcohol = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeGradosAlcoholString() => GradosAlcohol.HasValue;

    [XmlIgnore]
    public decimal? PrecioUnitarioReferencia { get; set; }
    [XmlElement("PrecioUnitarioReferencia", Order = 13)]
    public string? PrecioUnitarioReferenciaString
    {
        get => ExcelDecimal.Verbatim(PrecioUnitarioReferencia);
        set => PrecioUnitarioReferencia = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializePrecioUnitarioReferenciaString() => PrecioUnitarioReferencia.HasValue;

    [XmlElement("FechaElaboracion", Order = 14)]
    public string? FechaElaboracion { get; set; }
    public bool ShouldSerializeFechaElaboracion() => !string.IsNullOrEmpty(FechaElaboracion);

    [XmlElement("FechaVencimientoItem", Order = 15)]
    public string? FechaVencimientoItem { get; set; }
    public bool ShouldSerializeFechaVencimientoItem() => !string.IsNullOrEmpty(FechaVencimientoItem);

    [XmlIgnore]
    public decimal PrecioUnitarioItem { get; set; }

    [XmlIgnore]
    public int? PrecioUnitarioItemDecimals { get; set; }

    [XmlElement("PrecioUnitarioItem", Order = 16)]
    public string PrecioUnitarioItemString
    {
        get
        {
            var decimals = PrecioUnitarioItemDecimals ?? 4;
            if (decimals < 0) decimals = 0;
            if (decimals > 4) decimals = 4;
            return Tools.FormatDecimal(PrecioUnitarioItem, decimals) ?? "0.0000";
        }
        set => PrecioUnitarioItem = Tools.ParseDecimal(value) ?? 0m;
    }

    [XmlElement("DescuentoMonto", Order = 17)]
    public decimal? DescuentoMonto { get; set; }
    public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto > 0;

    [XmlElement("TablaSubDescuento", Order = 18)]
    public EcfXmlTablaSubDescuento? TablaSubDescuento { get; set; }
    public bool ShouldSerializeTablaSubDescuento() => TablaSubDescuento != null && TablaSubDescuento.SubDescuentos.Count > 0;

    [XmlElement("RecargoMonto", Order = 19)]
    public decimal? RecargoMonto { get; set; }
    public bool ShouldSerializeRecargoMonto() => RecargoMonto.HasValue && RecargoMonto > 0;

    [XmlElement("TablaSubRecargo", Order = 20)]
    public EcfXmlTablaSubRecargo? TablaSubRecargo { get; set; }
    public bool ShouldSerializeTablaSubRecargo() => TablaSubRecargo != null && TablaSubRecargo.SubRecargos.Count > 0;

    [XmlElement("TablaImpuestoAdicional", Order = 21)]
    public EcfXmlTablaImpuestoAdicionalItem? TablaImpuestoAdicional { get; set; }
    public bool ShouldSerializeTablaImpuestoAdicional() => TablaImpuestoAdicional != null && EcfType != 41 && EcfType != 43;

    [XmlIgnore]
    public decimal MontoItem { get; set; }

    [XmlElement("MontoItem", Order = 22)]
    public string MontoItemString
    {
        get => ExcelDecimal.Verbatim(MontoItem) ?? "0";
        set => MontoItem = Tools.ParseDecimal(value) ?? 0m;
    }

}

public class EcfXmlTablaSubcantidad
{
    [XmlElement("SubcantidadItem")]
    public List<EcfXmlSubcantidadItem> SubcantidadItem { get; set; } = new();
}

public class EcfXmlSubcantidadItem
{
    [XmlIgnore]
    public decimal? Subcantidad { get; set; }
    [XmlElement("Subcantidad", Order = 1)]
    public string? SubcantidadString
    {
        get => ExcelDecimal.Verbatim(Subcantidad);
        set => Subcantidad = Tools.ParseDecimal(value);
    }
    public bool ShouldSerializeSubcantidadString() => Subcantidad.HasValue;

    [XmlElement("CodigoSubcantidad", Order = 2)]
    public int? CodigoSubcantidad { get; set; }
    public bool ShouldSerializeCodigoSubcantidad() => CodigoSubcantidad.HasValue;
}

public class EcfXmlTablaSubDescuento
{
    [XmlElement("SubDescuento")]
    public List<EcfXmlSubDescuento> SubDescuentos { get; set; } = new();
}

public class EcfXmlSubDescuento
{
    [XmlElement("TipoSubDescuento")]
    public string TipoSubDescuento { get; set; } = "$"; // "$" for amount, "%" for percentage

    [XmlElement("SubDescuentoPorcentaje")]
    public decimal? SubDescuentoPorcentaje { get; set; }
    public bool ShouldSerializeSubDescuentoPorcentaje() => SubDescuentoPorcentaje.HasValue;

    [XmlElement("MontoSubDescuento")]
    public decimal MontoSubDescuento { get; set; }
}

public class EcfXmlTablaSubRecargo
{
    [XmlElement("SubRecargo")]
    public List<EcfXmlSubRecargo> SubRecargos { get; set; } = new();
}

public class EcfXmlSubRecargo
{
    [XmlElement("TipoSubRecargo")]
    public string TipoSubRecargo { get; set; } = "$"; // "$" or "%"

    [XmlElement("SubRecargoPorcentaje")]
    public decimal? SubRecargoPorcentaje { get; set; }
    public bool ShouldSerializeSubRecargoPorcentaje() => SubRecargoPorcentaje.HasValue;

    [XmlElement("MontoSubRecargo")]
    public decimal MontoSubRecargo { get; set; }
}

public class EcfXmlItemRetencion
{
    [XmlIgnore]
    public int EcfType { get; set; }

    [XmlElement("IndicadorAgenteRetencionoPercepcion")]
    public int Indicador { get; set; } // 1=Retencion, 2=Percepcion

    [XmlElement("MontoITBISRetenido")]
    public decimal? MontoITBISRetenido { get; set; }
    public bool ShouldSerializeMontoITBISRetenido() => MontoITBISRetenido.HasValue && EcfType != 47;

    [XmlElement("MontoISRRetenido")]
    public decimal? MontoISRRetenido { get; set; }
    public bool ShouldSerializeMontoISRRetenido() => MontoISRRetenido.HasValue;
}

