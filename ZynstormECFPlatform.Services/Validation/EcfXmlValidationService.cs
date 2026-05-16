using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using static ZynstormECFPlatform.Services.Validation.EcfXmlStructuralValidator;

namespace ZynstormECFPlatform.Services.Validation;

/// <summary>
/// Standalone XML validation service. Completely decoupled from EcfController/ReceivedEcfProductionService.
/// Runs 4 validation layers and caches valid XMLs for DGII-style lookup.
/// </summary>
public class EcfXmlValidationService : IEcfXmlValidationService
{
    private readonly IConfiguration _configuration;

    public EcfXmlValidationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // ── Schema assembly (Schemas project) ──
    private static readonly Assembly SchemasAssembly =
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ZynstormECFPlatform.Schemas")
        ?? Assembly.Load("ZynstormECFPlatform.Schemas");

    /// <summary>
    /// In-memory store for validated e-NCFs. Thread-safe.
    /// Key = eNCF, Value = verification info.
    /// </summary>
    private static readonly ConcurrentDictionary<string, EcfVerificacionInfo> _verificacionCache = new();

    private static readonly ConcurrentDictionary<string, EcfXmlValidationTrackStatus> _trackStatusCache = new();

    /// <summary>
    /// Maps TipoeCF to document type name.
    /// </summary>
    private static readonly Dictionary<int, string> EcfTypeNames = new()
    {
        [31] = "Factura de Crédito Fiscal Electrónica",
        [32] = "Factura de Consumo Electrónica",
        [33] = "Nota de Crédito Electrónica",
        [34] = "Nota de Débito Electrónica",
        [41] = "Comprobante de Compras Electrónico",
        [43] = "Gastos Menores Electrónico",
        [44] = "Regímenes Especiales Electrónico",
        [45] = "Gubernamental Electrónico",
        [46] = "Comprobante de Exportaciones Electrónico",
        [47] = "Comprobante para Pagos al Exterior Electrónico"
    };

    // ═══════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public EcfXmlValidationResult Validate(string xml)
    {
        var result = new EcfXmlValidationResult();

        // ── Layer 1: Structural Validation ──
        var (structuralErrors, doc, ecfType, eNcf) = EcfXmlStructuralValidator.Validate(xml);
        result.StructuralErrors = structuralErrors;
        result.EcfType = ecfType;
        result.ENcf = eNcf;

        if (structuralErrors.Count > 0 || doc == null)
        {
            // Cannot proceed without a parseable document
            return result;
        }

        // ── Layer 2: XSD Validation ──
        result.XsdErrors = ValidateAgainstXsd(xml, ecfType);

        // ── Layer 3: Business Rule Validation ──
        result.BusinessRuleErrors = EcfXmlBusinessRuleValidator.Validate(doc, ecfType);

        // ── Layer 4: Arithmetic Validation ──
        result.ArithmeticErrors = EcfXmlArithmeticValidator.Validate(doc, ecfType);

        // ── Cache if valid ──
        if (result.IsValid)
        {
            var info = ExtractVerificacionInfo(doc, ecfType, eNcf);
            result.Verificacion = info;
            _verificacionCache[eNcf] = info;
        }

        return result;
    }

    /// <inheritdoc />
    public EcfXmlValidationReceipt RegisterReceived(string trackId)
    {
        var receipt = new EcfXmlValidationReceipt
        {
            TrackId = trackId,
            Estado = "Recibido",
            Mensaje = "XML recibido correctamente. Consulte el estado con el TrackId.",
            RecibidoEnUtc = DateTime.UtcNow
        };

        _trackStatusCache[trackId] = new EcfXmlValidationTrackStatus
        {
            TrackId = trackId,
            Estado = "Recibido",
            Mensaje = receipt.Mensaje,
            RecibidoEnUtc = receipt.RecibidoEnUtc
        };

        return receipt;
    }

    /// <inheritdoc />
    public Task ProcessValidationJobAsync(string trackId, string xml)
    {
        _trackStatusCache.AddOrUpdate(
            trackId,
            _ => new EcfXmlValidationTrackStatus
            {
                TrackId = trackId,
                Estado = "EnProceso",
                Mensaje = "Validacion en proceso.",
                RecibidoEnUtc = DateTime.UtcNow
            },
            (_, current) =>
            {
                current.Estado = "EnProceso";
                current.Mensaje = "Validacion en proceso.";
                return current;
            });

        try
        {
            var result = Validate(xml);
            var status = new EcfXmlValidationTrackStatus
            {
                TrackId = trackId,
                Estado = result.IsValid ? "Aceptado" : "Rechazado",
                Mensaje = result.IsValid
                    ? "XML validado correctamente."
                    : "El XML no paso la validacion.",
                RecibidoEnUtc = _trackStatusCache.TryGetValue(trackId, out var current)
                    ? current.RecibidoEnUtc
                    : DateTime.UtcNow,
                ProcesadoEnUtc = DateTime.UtcNow,
                IsValid = result.IsValid,
                EcfType = result.EcfType,
                ENcf = result.ENcf,
                StructuralErrors = result.StructuralErrors,
                XsdErrors = result.XsdErrors,
                BusinessRuleErrors = result.BusinessRuleErrors,
                ArithmeticErrors = result.ArithmeticErrors,
                Verificacion = result.Verificacion
            };

            _trackStatusCache[trackId] = status;
        }
        catch (Exception ex)
        {
            _trackStatusCache.AddOrUpdate(
                trackId,
                _ => new EcfXmlValidationTrackStatus
                {
                    TrackId = trackId,
                    Estado = "Error",
                    Mensaje = $"Error interno durante la validacion: {ex.Message}",
                    RecibidoEnUtc = DateTime.UtcNow,
                    ProcesadoEnUtc = DateTime.UtcNow,
                    IsValid = false
                },
                (_, current) =>
                {
                    current.Estado = "Error";
                    current.Mensaje = $"Error interno durante la validacion: {ex.Message}";
                    current.ProcesadoEnUtc = DateTime.UtcNow;
                    current.IsValid = false;
                    return current;
                });
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public EcfXmlValidationTrackStatus? GetStatus(string trackId)
    {
        _trackStatusCache.TryGetValue(trackId, out var status);
        return status;
    }

    /// <inheritdoc />
    public EcfVerificacionInfo? GetVerificacion(string eNcf)
    {
        _verificacionCache.TryGetValue(eNcf, out var info);
        return info;
    }

    /// <inheritdoc />
    public List<EcfVerificacionInfo> GetAllVerificaciones()
    {
        return _verificacionCache.Values
            .OrderByDescending(v => v.ValidadoEnUtc)
            .ToList();
    }

    // ═══════════════════════════════════════════════════════════════
    // XSD Validation (Layer 2)
    // ═══════════════════════════════════════════════════════════════

    private List<string> ValidateAgainstXsd(string xml, int ecfType)
    {
        var errors = new List<string>();

        var schemaSet = LoadSchemaSetForType(ecfType);
        if (schemaSet == null)
        {
            errors.Add($"No se encontró el archivo XSD para TipoeCF {ecfType}. Verifique que el recurso esté embebido en el proyecto Schemas.");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
            ValidationFlags =
                XmlSchemaValidationFlags.ReportValidationWarnings |
                XmlSchemaValidationFlags.ProcessIdentityConstraints
        };

        settings.ValidationEventHandler += (_, e) =>
        {
            var severity = e.Severity == XmlSeverityType.Error ? "ERROR" : "WARNING";
            errors.Add($"[{severity}] Línea {e.Exception?.LineNumber}, Pos {e.Exception?.LinePosition}: {e.Message}");
        };

        using var reader = XmlReader.Create(new StringReader(xml), settings);
        try
        {
            while (reader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add($"[XML malformado] {ex.Message}");
        }

        return errors;
    }

    private XmlSchemaSet? LoadSchemaSetForType(int ecfType)
    {
        var resourceName = SchemasAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains("e-CF", StringComparison.OrdinalIgnoreCase) &&
                                 r.Contains($" {ecfType} ", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null) return null;

        using var stream = SchemasAssembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, XmlReader.Create(stream));
        schemaSet.Compile();
        return schemaSet;
    }

    // ═══════════════════════════════════════════════════════════════
    // Extract Verification Info (for caching)
    // ═══════════════════════════════════════════════════════════════

    private EcfVerificacionInfo ExtractVerificacionInfo(XmlDocument doc, int ecfType, string eNcf)
    {
        var root = doc.DocumentElement!;
        var encabezado = GetChild(root, "Encabezado")!;
        var emisor = GetChild(encabezado, "Emisor");
        var comprador = GetChild(encabezado, "Comprador");
        var totales = GetChild(encabezado, "Totales");

        var info = new EcfVerificacionInfo
        {
            ENcf = eNcf,
            EcfType = ecfType,
            TipoDocumento = EcfTypeNames.GetValueOrDefault(ecfType, $"Tipo {ecfType}"),
            Estado = "Aceptado",
            ValidadoEnUtc = DateTime.UtcNow
        };

        if (emisor != null)
        {
            info.RncEmisor = GetChildText(emisor, "RNCEmisor") ?? string.Empty;
            info.RazonSocialEmisor = GetChildText(emisor, "RazonSocialEmisor") ?? string.Empty;
            info.FechaEmision = GetChildText(emisor, "FechaEmision") ?? string.Empty;
        }

        if (comprador != null)
        {
            info.RncComprador = GetChildText(comprador, "RNCComprador")
                                ?? GetChildText(comprador, "IdentificadorExtranjero");
            info.RazonSocialComprador = GetChildText(comprador, "RazonSocialComprador");
        }

        if (totales != null)
        {
            var totalItbis = GetChildText(totales, "TotalITBIS");
            info.TotalItbis = totalItbis;
            info.MontoTotal = GetChildText(totales, "MontoTotal") ?? "0.00";
        }

        // --- Extraer Datos de Firma para el QR ---
        var nsManager = new XmlNamespaceManager(doc.NameTable);
        nsManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
        var sigValueNode = doc.SelectSingleNode("//ds:SignatureValue", nsManager);
        var signingTimeNode = doc.SelectSingleNode("//*[local-name()='SigningTime']", nsManager);
        
        string? sigValue = sigValueNode?.InnerText?.Trim();
        info.FechaFirma = signingTimeNode?.InnerText?.Trim() ?? DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        ApplyQrUrlMetadata(info, sigValue);

        return info;
    }

    private void ApplyQrUrlMetadata(EcfVerificacionInfo info, string? signatureValue)
    {
        try
        {
            // 1. Codigo de Seguridad (6 chars del SignatureValue o fallback SHA256)
            string codigoSeguridad = "";
            if (!string.IsNullOrEmpty(signatureValue))
            {
                codigoSeguridad = signatureValue.Length >= 6 ? signatureValue.Substring(0, 6) : signatureValue;
            }
            else
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(info.ENcf));
                codigoSeguridad = BitConverter.ToString(hash).Replace("-", "").Substring(0, 6);
            }
            info.CodigoSeguridad = codigoSeguridad;

            // 2. Determinar URL Base (Nuestra URL en lugar de la DGII)
            string platformUrl = _configuration["AppSettings:PlatformUrl"] ?? "http://localhost:5000";
            string baseUrl = $"{platformUrl.TrimEnd('/')}/api/v1/EcfXmlValidation/verificacion?";

            // 3. Construir Query String (Replicando parámetros DGII pero a nuestra URL)
            var queryParams = new List<string>
            {
                $"rncEmisor={info.RncEmisor}",
                $"eNcf={info.ENcf}",
                $"montoTotal={info.MontoTotal}",
                $"codigoSeguridad={Uri.EscapeDataString(codigoSeguridad)}"
            };

            // Solo agregamos opcionales si existen para mantener URL limpia
            if (!string.IsNullOrEmpty(info.RncComprador)) queryParams.Add($"rncComprador={info.RncComprador}");
            if (!string.IsNullOrEmpty(info.FechaEmision)) queryParams.Add($"fechaEmision={info.FechaEmision}");
            if (!string.IsNullOrEmpty(info.FechaFirma)) queryParams.Add($"fechaFirma={Uri.EscapeDataString(info.FechaFirma).Replace("%3A", ":")}");

            string fullUrl = baseUrl + string.Join("&", queryParams);
            info.VerificationUrl = fullUrl;
        }
        catch (Exception)
        {
            info.VerificationUrl = string.Empty;
        }
    }
}
