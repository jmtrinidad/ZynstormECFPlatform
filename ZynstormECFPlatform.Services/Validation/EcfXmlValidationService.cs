using System.Collections.Concurrent;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
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

    private static List<string> ValidateAgainstXsd(string xml, int ecfType)
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

    private static XmlSchemaSet? LoadSchemaSetForType(int ecfType)
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

    private static EcfVerificacionInfo ExtractVerificacionInfo(XmlDocument doc, int ecfType, string eNcf)
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
            // Use RNCComprador or IdentificadorExtranjero
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

        return info;
    }
}
