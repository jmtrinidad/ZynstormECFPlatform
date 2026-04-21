using System.Collections.Generic;
using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml;

/// <summary>
/// Root element for Aprobación Comercial de e-CF (AEC).
/// </summary>
[XmlRoot("ACECF")]
public class AcecfXmlRoot
{
    [XmlElement("DetalleAprobacionComercial")]
    public ArecfXmlDetalle Detalle { get; set; } = new();

    /// <summary>
    /// Placeholder for the digital signature.
    /// </summary>
    [XmlAnyElement]
    public System.Xml.XmlElement? Signature { get; set; }
}

public class ArecfXmlDetalle
{
    [XmlElement("Version")]
    public string Version { get; set; } = "1.0";

    [XmlElement("RNCEmisor")]
    public string RNCEmisor { get; set; }

    [XmlElement("eNCF")]
    public string ENcf { get; set; }

    [XmlElement("FechaEmision")]
    public string FechaEmision { get; set; }

    [XmlElement("MontoTotal")]
    public decimal MontoTotal { get; set; }

    [XmlElement("RNCComprador")]
    public string RNCComprador { get; set; }

    [XmlElement("Estado")]
    public int Estado { get; set; }

    [XmlElement("DetalleMotivoRechazo")]
    public string? DetalleMotivoRechazo { get; set; }
    public bool ShouldSerializeDetalleMotivoRechazo() => !string.IsNullOrWhiteSpace(DetalleMotivoRechazo);

    [XmlElement("FechaHoraAprobacionComercial")]
    public string FechaHoraAprobacionComercial { get; set; }
}
