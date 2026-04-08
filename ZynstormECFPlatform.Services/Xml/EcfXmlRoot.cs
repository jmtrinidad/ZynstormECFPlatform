using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Root XML model mapping to &lt;ECF&gt; — the top-level element defined in all DGII e-CF XSD schemas.
/// Serialize this class with XmlSerializer to produce a valid unsigned e-CF XML document.
/// </summary>
[XmlRoot("ECF")]
public class EcfXmlRoot
{
    [XmlElement("Encabezado")]
    public EcfXmlEncabezado Encabezado { get; set; } = null!;

    [XmlArray("DetallesItems")]
    [XmlArrayItem("Item")]
    public List<EcfXmlItem> Items { get; set; } = [];

    // ── Optional sections ──────────────────────────────────────────────────

    [XmlArray("DescuentosORecargos")]
    [XmlArrayItem("DescuentoORecargo")]
    public List<EcfXmlDescuentoORecargo> Adjustments { get; set; } = [];
    public bool ShouldSerializeAdjustments() => Adjustments.Count > 0;

    [XmlElement("InformacionReferencia")]
    public EcfXmlInformacionReferencia? InformacionReferencia { get; set; }
    public bool ShouldSerializeInformacionReferencia() => InformacionReferencia != null;

    /// <summary>
    /// Signature date-time in DGII format "YYYY-MM-DDTHH:mm:ss".
    /// Required by XSD before the xs:any Signature element.
    /// Set to the moment the XML is generated (before actual signing).
    /// </summary>
    [XmlElement("FechaHoraFirma")]
    public string FechaHoraFirma { get; set; } = null!;


    /// <summary>
    /// Placeholder for the digital signature. 
    /// Required by XSD as an xs:any element.
    /// During validation of unsigned XML, we inject a dummy element.
    /// </summary>
    [XmlAnyElement]
    public System.Xml.XmlElement? Signature { get; set; }
}
