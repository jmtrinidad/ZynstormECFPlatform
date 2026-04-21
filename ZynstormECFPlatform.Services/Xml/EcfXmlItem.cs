using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

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
        get => (EcfType is 41 or 47) ? _retencion : null;
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

    [XmlElement("CantidadItem", Order = 7)]
    public decimal CantidadItem { get; set; }

    [XmlElement("UnidadMedida", Order = 8)]
    public int? UnidadMedida { get; set; }
    public bool ShouldSerializeUnidadMedida() => UnidadMedida.HasValue;

    [XmlElement("FechaElaboracion", Order = 9)]
    public string? FechaElaboracion { get; set; }
    public bool ShouldSerializeFechaElaboracion() => !string.IsNullOrEmpty(FechaElaboracion);

    [XmlElement("FechaVencimientoItem", Order = 10)]
    public string? FechaVencimientoItem { get; set; }
    public bool ShouldSerializeFechaVencimientoItem() => !string.IsNullOrEmpty(FechaVencimientoItem);

    [XmlElement("PrecioUnitarioItem", Order = 11)]
    public decimal PrecioUnitarioItem { get; set; }

    [XmlElement("DescuentoMonto", Order = 12)]
    public decimal? DescuentoMonto { get; set; }
    public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto > 0;

    [XmlElement("TablaSubDescuento", Order = 13)]
    public EcfXmlTablaSubDescuento? TablaSubDescuento { get; set; }
    public bool ShouldSerializeTablaSubDescuento() => TablaSubDescuento != null && TablaSubDescuento.SubDescuentos.Count > 0;

    [XmlElement("RecargoMonto", Order = 14)]
    public decimal? RecargoMonto { get; set; }
    public bool ShouldSerializeRecargoMonto() => RecargoMonto.HasValue && RecargoMonto > 0;

    [XmlElement("TablaSubRecargo", Order = 15)]
    public EcfXmlTablaSubRecargo? TablaSubRecargo { get; set; }
    public bool ShouldSerializeTablaSubRecargo() => TablaSubRecargo != null && TablaSubRecargo.SubRecargos.Count > 0;

    [XmlElement("TablaImpuestoAdicional", Order = 16)]
    public EcfXmlTablaImpuestoAdicionalItem? TablaImpuestoAdicional { get; set; }
    public bool ShouldSerializeTablaImpuestoAdicional() => TablaImpuestoAdicional != null && EcfType != 41 && EcfType != 43;

    [XmlElement("MontoItem", Order = 17)]
    public decimal MontoItem { get; set; }

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
    [XmlElement("IndicadorAgenteRetencionoPercepcion")]
    public int Indicador { get; set; } // 1=Retencion, 2=Percepcion

    [XmlElement("MontoITBISRetenido")]
    public decimal? MontoITBISRetenido { get; set; }
    public bool ShouldSerializeMontoITBISRetenido() => MontoITBISRetenido.HasValue;

    [XmlElement("MontoISRRetenido")]
    public decimal? MontoISRRetenido { get; set; }
    public bool ShouldSerializeMontoISRRetenido() => MontoISRRetenido.HasValue;
}
