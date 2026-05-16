using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Dtos;

/// <summary>
/// Result of a full XML validation pipeline. Contains errors grouped by validation layer.
/// </summary>
public class EcfXmlValidationResult
{
    public bool IsValid => StructuralErrors.Count == 0
                           && XsdErrors.Count == 0
                           && BusinessRuleErrors.Count == 0
                           && ArithmeticErrors.Count == 0;

    public int EcfType { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public List<string> StructuralErrors { get; set; } = [];
    public List<string> XsdErrors { get; set; } = [];
    public List<string> BusinessRuleErrors { get; set; } = [];
    public List<string> ArithmeticErrors { get; set; } = [];

    /// <summary>
    /// Summary extracted from the XML for display (like DGII Verificación e-NCF).
    /// Only populated when the XML passes validation.
    /// </summary>
    public EcfVerificacionInfo? Verificacion { get; set; }
}

public class EcfXmlValidationReceipt
{
    public string TrackId { get; set; } = string.Empty;
    public string Estado { get; set; } = "Recibido";
    public string Mensaje { get; set; } = "XML recibido correctamente.";
    public DateTime RecibidoEnUtc { get; set; } = DateTime.UtcNow;
}

public class EcfXmlValidationTrackStatus
{
    public string TrackId { get; set; } = string.Empty;
    public string Estado { get; set; } = "Recibido";
    public string Mensaje { get; set; } = string.Empty;
    public DateTime RecibidoEnUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcesadoEnUtc { get; set; }
    public bool? IsValid { get; set; }
    public int EcfType { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public List<string> StructuralErrors { get; set; } = [];
    public List<string> XsdErrors { get; set; } = [];
    public List<string> BusinessRuleErrors { get; set; } = [];
    public List<string> ArithmeticErrors { get; set; } = [];
    public EcfVerificacionInfo? Verificacion { get; set; }
}

/// <summary>
/// Mirrors the DGII "Verificación e-NCF" screen data.
/// </summary>
public class EcfVerificacionInfo
{
    public string RncEmisor { get; set; } = string.Empty;
    public string RazonSocialEmisor { get; set; } = string.Empty;
    public string? RncComprador { get; set; }
    public string? RazonSocialComprador { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public string FechaEmision { get; set; } = string.Empty;
    public string? TotalItbis { get; set; }
    public string MontoTotal { get; set; } = string.Empty;
    public string Estado { get; set; } = "Aceptado";
    public int EcfType { get; set; }
    public string TipoDocumento { get; set; } = string.Empty;
    public DateTime ValidadoEnUtc { get; set; } = DateTime.UtcNow;

    // --- Campos de Validación DGII / QR ---
    public string? CodigoSeguridad { get; set; }
    public string? FechaFirma { get; set; }
    public string? VerificationUrl { get; set; }

    public string? UrlQr => VerificationUrl;

    [JsonIgnore]
    public string? QrCodeBase64 { get; set; }
}
