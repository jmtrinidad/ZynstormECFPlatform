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

    [XmlElement("NumeroLinea")]
    public int NumeroLinea { get; set; }

    [XmlElement("IndicadorFacturacion")]
    public int IndicadorFacturacion { get; set; }

    [XmlElement("Retencion")]
    public EcfXmlItemRetencion? Retencion { get; set; }
    public bool ShouldSerializeRetencion() => Retencion != null && (EcfType == 41 || EcfType == 43 || EcfType == 47 || EcfType <= 34);


    [XmlElement("NombreItem")]
    public string Name { get; set; } = null!;

    [XmlElement("IndicadorBienoServicio")]
    public int ItemType { get; set; }

    [XmlElement("DescripcionItem")]
    public string? DescripcionItem { get; set; }
    public bool ShouldSerializeDescripcionItem() => DescripcionItem != null;


    [XmlElement("CantidadItem")]
    public decimal CantidadItem { get; set; }

    public int? UnidadMedida { get; set; }
    public bool ShouldSerializeUnidadMedida() => UnidadMedida.HasValue;

    [XmlElement("PrecioUnitarioItem")]
    public decimal PrecioUnitarioItem { get; set; }

    [XmlElement("DescuentoMonto")]
    public decimal? DescuentoMonto { get; set; }
    public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto > 0;

    [XmlElement("TablaSubDescuento")]
    public EcfXmlTablaSubDescuento? TablaSubDescuento { get; set; }
    public bool ShouldSerializeTablaSubDescuento() => TablaSubDescuento != null && TablaSubDescuento.SubDescuentos.Count > 0;

    [XmlElement("TablaImpuestoAdicional")]
    public EcfXmlTablaImpuestoAdicionalItem? TablaImpuestoAdicional { get; set; }
    public bool ShouldSerializeTablaImpuestoAdicional() => TablaImpuestoAdicional != null && EcfType != 41 && EcfType != 43;

    [XmlElement("MontoItem")]
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

    [XmlElement("ValorSubDescuento")]
    public decimal ValorSubDescuento { get; set; }

    [XmlElement("MontoSubDescuento")]
    public decimal MontoSubDescuento { get; set; }
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
