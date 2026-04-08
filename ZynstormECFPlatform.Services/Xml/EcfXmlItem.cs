using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Maps to XSD &lt;Item&gt; inside &lt;DetallesItems&gt;.
/// </summary>
public class EcfXmlItem
{
    [XmlElement("NumeroLinea")]
    public int NumeroLinea { get; set; }

    /// <summary>
    /// IndicadorFacturacion:
    /// 0 = No Facturable (18%), 1 = ITBIS 18%, 2 = ITBIS 16%, 3 = ITBIS 0%, 4 = Exento.
    /// </summary>
    [XmlElement("IndicadorFacturacion")]
    public int IndicadorFacturacion { get; set; }

    /// <summary>1 = Bien, 2 = Servicio.</summary>
    [XmlElement("NombreItem")]
    public string Name { get; set; } = null!;

    [XmlElement("IndicadorBienoServicio")]
    public int ItemType { get; set; }

    public string? DescripcionItem { get; set; }
    public bool ShouldSerializeDescripcionItem() => DescripcionItem != null;

    [XmlElement("CantidadItem")]
    public decimal CantidadItem { get; set; }

    public int? UnidadMedida { get; set; }
    public bool ShouldSerializeUnidadMedida() => UnidadMedida.HasValue;

    [XmlElement("PrecioUnitarioItem")]
    public decimal PrecioUnitarioItem { get; set; }

    public decimal? DescuentoMonto { get; set; }
    public bool ShouldSerializeDescuentoMonto() => DescuentoMonto.HasValue && DescuentoMonto > 0;

    /// <summary>
    /// Additional tax type references for this item (TablaImpuestoAdicional).
    /// Populated when the item is subject to ISC or other additional taxes.
    /// References the TipoImpuesto codes; actual amounts are aggregated in Totales.
    /// XSD allows up to 2 entries per item.
    /// </summary>
    [XmlElement("TablaImpuestoAdicional")]
    public EcfXmlTablaImpuestoAdicionalItem? TablaImpuestoAdicional { get; set; }
    public bool ShouldSerializeTablaImpuestoAdicional() => TablaImpuestoAdicional != null;

    [XmlElement("MontoItem")]
    public decimal MontoItem { get; set; }
}
