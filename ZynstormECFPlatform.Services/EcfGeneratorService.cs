using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Xml;

namespace ZynstormECFPlatform.Services;

/// <summary>
/// Generates and validates unsigned e-CF XML documents compliant with DGII specifications.
/// The XML structure is derived from the official XSD schemas for each TipoeCF.
/// </summary>
public class EcfGeneratorService : IEcfGeneratorService
{
    // ── Constants ──────────────────────────────────────────────────────────────

    private const string DateFormat = "dd-MM-yyyy";
    private const string DateTimeFormat = "dd-MM-yyyy HH:mm:ss";
    private const string XsdResourcePrefix = "ZynstormECFPlatform.Schemas.XSD.";

    // ── Cached serializer (thread-safe after first use) ────────────────────────

    private static readonly XmlSerializer _serializer = new(typeof(EcfXmlRoot));
    private static readonly XmlSerializerNamespaces _noNamespaces;

    // ── Schema assembly (Schemas project) ─────────────────────────────────────

    /// <summary>
    /// We locate the Schemas assembly reliably by its name. 
    /// If it's not currently loaded in the AppDomain, we load it explicitly.
    /// </summary>
    private static readonly Assembly _schemasAssembly = 
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ZynstormECFPlatform.Schemas")
            ?? Assembly.Load("ZynstormECFPlatform.Schemas");

    static EcfGeneratorService()
    {
        _noNamespaces = new XmlSerializerNamespaces();
        _noNamespaces.Add(string.Empty, string.Empty);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // Public API
    // ═══════════════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public string GenerateUnsignedXml(EcfInvoiceRequestDto dto)
    {
        var root = MapToXmlRoot(dto);

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            OmitXmlDeclaration = false,
            NewLineHandling = NewLineHandling.Replace
        };

        using var memoryStream = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(memoryStream, settings))
        {
            _serializer.Serialize(xmlWriter, root, _noNamespaces);
        }

        return Encoding.UTF8.GetString(memoryStream.ToArray());
    }

    /// <inheritdoc />
    public List<string> ValidateXmlAgainstSchema(string xml, int ecfType)
    {
        var errors = new List<string>();

        var schemaSet = LoadSchemaSetForType(ecfType);
        if (schemaSet is null)
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
            while (reader.Read()) { /* Consume the entire document to trigger validation */ }
        }
        catch (XmlException ex)
        {
            errors.Add($"[XML malformado] {ex.Message}");
        }

        return errors;
    }

    /// <inheritdoc />
    public List<string> ValidateDto(EcfInvoiceRequestDto dto)
    {
        var errors = new List<string>();

        // NCF format validation
        if (string.IsNullOrWhiteSpace(dto.Ncf))
        {
            errors.Add("El eNCF es requerido.");
        }
        else if (!NcfHelper.TryExtractEcfType(dto.Ncf, out _))
        {
            errors.Add($"El eNCF '{dto.Ncf}' no tiene el formato correcto (E + 2 dígitos de tipo + 10 dígitos).");
        }

        if (string.IsNullOrWhiteSpace(dto.IssuerRnc))
            errors.Add("El RNC del emisor es requerido.");

        if (string.IsNullOrWhiteSpace(dto.IssuerName))
            errors.Add("La razón social del emisor es requerida.");

        if (string.IsNullOrWhiteSpace(dto.IssuerAddress))
            errors.Add("La dirección del emisor es requerida.");

        if (string.IsNullOrWhiteSpace(dto.CustomerRnc))
            errors.Add("El RNC/Cédula del comprador es requerido.");

        if (string.IsNullOrWhiteSpace(dto.CustomerName))
            errors.Add("El nombre del comprador es requerido.");

        if (dto.Items.Count == 0)
        {
            errors.Add("El documento debe contener al menos un ítem.");
        }
        else
        {
            for (int i = 0; i < dto.Items.Count; i++)
            {
                var itm = dto.Items[i];
                if (string.IsNullOrWhiteSpace(itm.Name)) errors.Add($"Item {i + 1}: El nombre es requerido.");
                if (itm.Quantity <= 0) errors.Add($"Item {i + 1}: La cantidad debe ser mayor a cero.");
                if (itm.UnitPrice < 0) errors.Add($"Item {i + 1}: El precio unitario no puede ser negativo.");

                // ISC validation
                if (!string.IsNullOrWhiteSpace(itm.IscType))
                {
                    if (itm.AdditionalTaxRate <= 0)
                        errors.Add($"Item {i + 1}: El campo AdditionalTaxRate es requerido cuando se especifica un IscType.");
                }
            }
        }

        if (dto.PaymentType == 2 && dto.PaymentDeadline is null)
            errors.Add("La fecha límite de pago es requerida cuando el tipo de pago es Crédito (2).");

        return errors;
    }


    // ═══════════════════════════════════════════════════════════════════════════
    // XML Mapping
    // ═══════════════════════════════════════════════════════════════════════════

    private static EcfXmlRoot MapToXmlRoot(EcfInvoiceRequestDto dto)
    {
        var ecfType = NcfHelper.ExtractEcfType(dto.Ncf);
        var issueDate = dto.IssueDate.ToDrTime().ToString(DateFormat);
        var expirationDate = (dto.SequenceExpirationDate ?? dto.IssueDate.AddYears(1)).ToDrTime().ToString(DateFormat);
        var signatureDateTime = DateTime.UtcNow.ToDrTime().ToString(DateTimeFormat);

        // ── Items + running totals ─────────────────────────────────────────────

        var xmlItems = new List<EcfXmlItem>();
        decimal totalBase = 0, totalItemDiscounts = 0, totalItbis = 0, totalExempt = 0;
        decimal taxableG1 = 0, taxableG2 = 0, taxableG3 = 0;
        decimal itbisG1 = 0, itbisG2 = 0, itbisG3 = 0;

        // ISC accumulator: key = TipoImpuesto code, value = accumulated amounts
        var iscAccumulator = new Dictionary<string, EcfXmlImpuestoAdicional>(StringComparer.Ordinal);

        var lineNo = 1;
        foreach (var item in dto.Items)
        {
            var baseAmount = Math.Round(item.Quantity * item.UnitPrice, 2);
            var discountAmount = Math.Round(item.Discount, 2);
            var taxableAmount = baseAmount - discountAmount;

            var itbisAmount = item.ItbisAmount > 0
                ? item.ItbisAmount
                : Math.Round(taxableAmount * (item.TaxPercentage / 100m), 2);

            var billingIndicator = item.TaxPercentage switch
            {
                18m => 1,
                16m => 2,
                 0m => 3,
                  _ => 4  // Exento
            };

            // ── ISC / Additional Tax handling ──────────────────────────────────
            EcfXmlTablaImpuestoAdicionalItem? tablaImpuesto = null;
            if (!string.IsNullOrWhiteSpace(item.IscType))
            {
                tablaImpuesto = new EcfXmlTablaImpuestoAdicionalItem
                {
                    ImpuestoAdicional = [new EcfXmlImpuestoAdicionalRef { TipoImpuesto = item.IscType }]
                };

                if (!iscAccumulator.TryGetValue(item.IscType, out var entry))
                {
                    entry = new EcfXmlImpuestoAdicional
                    {
                        TipoImpuesto = item.IscType,
                        TasaImpuestoAdicional = item.AdditionalTaxRate
                    };
                    iscAccumulator[item.IscType] = entry;
                }

                if (item.IscSpecificAmount > 0)
                    entry.MontoImpuestoSelectivoConsumoEspecifico =
                        (entry.MontoImpuestoSelectivoConsumoEspecifico ?? 0) + Math.Round(item.IscSpecificAmount, 2);

                if (item.IscAdvaloremAmount > 0)
                    entry.MontoImpuestoSelectivoConsumoAdvalorem =
                        (entry.MontoImpuestoSelectivoConsumoAdvalorem ?? 0) + Math.Round(item.IscAdvaloremAmount, 2);

                if (item.OtherAdditionalTaxAmount > 0)
                    entry.OtrosImpuestosAdicionales =
                        (entry.OtrosImpuestosAdicionales ?? 0) + Math.Round(item.OtherAdditionalTaxAmount, 2);
            }

            // ISC total per item to include in MontoItem
            var iscItemTotal = item.IscSpecificAmount + item.IscAdvaloremAmount + item.OtherAdditionalTaxAmount;

            xmlItems.Add(new EcfXmlItem
            {
                NumeroLinea          = lineNo++,
                IndicadorFacturacion = billingIndicator,
                Name                 = item.Name,
                ItemType             = item.ItemType,
                DescripcionItem      = item.Description,
                CantidadItem         = item.Quantity,
                UnidadMedida         = item.UnitOfMeasure ?? 43, // 43 = Unidad
                PrecioUnitarioItem   = item.UnitPrice,
                DescuentoMonto       = discountAmount > 0 ? discountAmount : null,
                TablaImpuestoAdicional = tablaImpuesto,
                MontoItem            = taxableAmount + itbisAmount + iscItemTotal
            });

            totalBase          += baseAmount;
            totalItemDiscounts += discountAmount;
            totalItbis         += itbisAmount;

            switch (item.TaxPercentage)
            {
                case 0m:  totalExempt += taxableAmount; taxableG3 += taxableAmount; break;
                case 18m: taxableG1   += taxableAmount; itbisG1   += itbisAmount;    break;
                case 16m: taxableG2   += taxableAmount; itbisG2   += itbisAmount;    break;
            }
        }

        // ── ISC Totales ────────────────────────────────────────────────────────

        decimal totalIsc = 0;
        EcfXmlImpuestosAdicionales? impuestosAdicionales = null;

        if (iscAccumulator.Count > 0)
        {
            totalIsc = iscAccumulator.Values.Sum(e =>
                (e.MontoImpuestoSelectivoConsumoEspecifico ?? 0) +
                (e.MontoImpuestoSelectivoConsumoAdvalorem ?? 0) +
                (e.OtrosImpuestosAdicionales ?? 0));

            impuestosAdicionales = new EcfXmlImpuestosAdicionales
            {
                Items = [.. iscAccumulator.Values]
            };
        }

        // ── Global adjustments ─────────────────────────────────────────────────

        var adjustments = new List<EcfXmlDescuentoORecargo>();
        if (dto.GlobalDiscountAmount > 0)
        {
            adjustments.Add(new EcfXmlDescuentoORecargo
            {
                NumeroLinea              = 1,
                TipoAjuste               = "D",
                DescripcionDescuentooRecargo = dto.GlobalDiscountDescription ?? "Descuento Global",
                TipoValor                = "$",
                MontoDescuentooRecargo   = dto.GlobalDiscountAmount
            });
        }

        var finalTotal = (totalBase - totalItemDiscounts + totalItbis + totalIsc) - dto.GlobalDiscountAmount;

        // ── Totales block ──────────────────────────────────────────────────────

        decimal taxableGravado = totalBase - totalItemDiscounts - totalExempt;

        var totales = new EcfXmlTotales
        {
            MontoGravadoTotal = taxableGravado > 0 ? taxableGravado : null,
            MontoGravadoI1    = taxableG1 > 0 ? taxableG1 : null,
            MontoGravadoI2    = taxableG2 > 0 ? taxableG2 : null,
            MontoGravadoI3    = taxableG3 > 0 ? taxableG3 : null,
            MontoExento       = totalExempt > 0 ? totalExempt : null,

            ITBIS1 = taxableG1 > 0 ? 18 : null,
            ITBIS2 = taxableG2 > 0 ? 16 : null,
            ITBIS3 = taxableG3 > 0 ? 0  : null,

            TotalITBIS  = totalItbis > 0 ? totalItbis : null,
            TotalITBIS1 = itbisG1 > 0 ? itbisG1 : null,
            TotalITBIS2 = itbisG2 > 0 ? itbisG2 : null,
            TotalITBIS3 = itbisG3 > 0 ? itbisG3 : null,

            MontoImpuestoAdicional = totalIsc > 0 ? totalIsc : null,
            ImpuestosAdicionales   = impuestosAdicionales,

            MontoTotal = finalTotal
        };

        var root = new EcfXmlRoot
        {
            Encabezado = new EcfXmlEncabezado
            {
                Version = 1.0m,
                IdDoc = new EcfXmlIdDoc
                {
                    EcfType                = ecfType,
                    Ncf                    = dto.Ncf,
                    SequenceExpirationDate = expirationDate,
                    IncomeType             = dto.IncomeType,
                    PaymentType            = dto.PaymentType,
                    FechaLimitePago        = dto.PaymentDeadline?.ToDrTime().ToString(DateFormat),
                    TerminoPago            = dto.PaymentTerms
                },
                Emisor = new EcfXmlEmisor
                {
                    RncEmisor          = dto.IssuerRnc,
                    RazonSocial        = dto.IssuerName,
                    NombreComercial    = dto.IssuerCommercialName,
                    Sucursal           = dto.IssuerBranchCode,
                    Direccion          = dto.IssuerAddress,
                    Municipio          = dto.IssuerMunicipality,
                    Provincia          = dto.IssuerProvince,
                    TelefonoTabla      = string.IsNullOrWhiteSpace(dto.IssuerPhone) ? null : new EcfXmlEmisor.TablaTelefono { Telefono = dto.IssuerPhone },
                    CorreoEmisor       = dto.IssuerEmail,
                    WebSite            = dto.IssuerWebSite,
                    ActividadEconomica = dto.IssuerActivityCode,
                    CodigoVendedor     = dto.IssuerSellerCode,
                    FechaEmision       = issueDate
                },
                Comprador = new EcfXmlComprador
                {
                    RncComprador       = dto.CustomerRnc,
                    RazonSocial        = dto.CustomerName,
                    ContactoComprador  = dto.CustomerContact,
                    CorreoComprador    = dto.CustomerEmail,
                    DireccionComprador = dto.CustomerAddress,
                    TelefonoAdicional  = dto.CustomerTelephone,
                    MunicipioComprador = dto.CustomerMunicipality,
                    ProvinciaComprador = dto.CustomerProvince
                },
                Totales = totales
            },
            Items           = xmlItems,
            Adjustments     = adjustments,
            FechaHoraFirma  = signatureDateTime
        };

        // ── Placeholder Signature (Required for XSD structural validation only) ──

        var doc = new XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

        return root;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XSD Schema Loading
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Loads the XmlSchemaSet for the given TipoeCF from the embedded resources
    /// in the ZynstormECFPlatform.Schemas assembly.
    /// Resource name example: "ZynstormECFPlatform.Schemas.XSD.e-CF 31 v.1.0.xsd"
    /// </summary>
    private static XmlSchemaSet? LoadSchemaSetForType(int ecfType)
    {
        var resourceName = _schemasAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains($" {ecfType} ", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null) return null;

        using var stream = _schemasAssembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, XmlReader.Create(stream));
        schemaSet.Compile();
        return schemaSet;
    }
}
