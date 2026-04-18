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
/// Generates and validates unsigned e-CF XML documents compliant with DGII specifications. The XML structure is derived
/// from the official XSD schemas for each TipoeCF.
/// </summary>
public class EcfGeneratorService : IEcfGeneratorService
{
    // ── Constants ──────────────────────────────────────────────────────────────

    private const string DateFormat = "dd-MM-yyyy";
    private const string DateTimeFormat = "dd-MM-yyyy HH:mm:ss";
    private const string XsdResourcePrefix = "ZynstormECFPlatform.Schemas.XSD.";

    // ── Cached serializer (thread-safe after first use) ────────────────────────

    private readonly XmlSerializer _serializer = new(typeof(EcfXmlRoot));
    private static readonly XmlSerializerNamespaces _noNamespaces;

    // ── Schema assembly (Schemas project) ─────────────────────────────────────

    /// <summary>
    /// We locate the Schemas assembly reliably by its name. If it's not currently loaded in the AppDomain, we load it
    /// explicitly.
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
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using var stringWriter = new Utf8StringWriter();
        using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
        {
            _serializer.Serialize(xmlWriter, root, _noNamespaces);
        }

        return stringWriter.ToString();
    }

    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
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

            var itbisRetenido = item.ManualMontoITBISRetenido ?? 0;

            // Use explicit BillingIndicator from DTO if provided (e.g. from Excel certification data),
            // otherwise derive it from TaxPercentage as before.
            var billingIndicator = item.BillingIndicator ?? item.TaxPercentage switch
            {
                18m => 1,
                16m => 2,
                0m => 3,
                _ => 4
            };

            // For exento (4) and no-facturable (0), force ITBIS to 0 regardless of TaxPercentage.
            if (billingIndicator is 4 or 0)
                itbisAmount = 0;

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

            var itemDiscountTotal = item.ManualDescuentoMonto ?? (discountAmount > 0 ? discountAmount : 0);
            EcfXmlTablaSubDescuento? tablaSubDescuento = null;

            if (itemDiscountTotal > 0)
            {
                tablaSubDescuento = new EcfXmlTablaSubDescuento
                {
                    SubDescuentos = new List<EcfXmlSubDescuento>
                    {
                        new EcfXmlSubDescuento
                        {
                            TipoSubDescuento = "$",
                            MontoSubDescuento = itemDiscountTotal
                        }
                    }
                };
            }

            xmlItems.Add(new EcfXmlItem
            {
                EcfType = ecfType,
                NumeroLinea = lineNo++,
                IndicadorFacturacion = billingIndicator,
                Name = item.Name,
                ItemType = item.ItemType,
                DescripcionItem = item.Description,
                CantidadItem = item.Quantity,
                UnidadMedida = item.UnitOfMeasure ?? 43, // 43 = Unidad
                PrecioUnitarioItem = item.UnitPrice,
                DescuentoMonto = itemDiscountTotal > 0 ? itemDiscountTotal : null,
                TablaSubDescuento = tablaSubDescuento,
                TablaImpuestoAdicional = tablaImpuesto,
                MontoItem = item.ManualMontoItem ?? (taxableAmount + itbisAmount + iscItemTotal),

                // ── Retentions handling (For Purchase 41, Gastos Menores 43 and International Payment 47)
                Retencion = (ecfType == 41 || ecfType == 43 || ecfType == 47) ? new EcfXmlItemRetencion
                {
                    Indicador = 1, // Retención
                    MontoITBISRetenido = (ecfType == 41 || ecfType == 43) ? itbisRetenido : null,
                    MontoISRRetenido = item.ManualMontoISRRetenido ?? (item.IsrRetentionAmount ?? 0)
                } : null
            });

            totalBase += baseAmount;
            totalItemDiscounts += discountAmount;
            totalItbis += itbisAmount;

            switch (billingIndicator)
            {
                case 4:  // Exento
                case 0:  // No facturable
                    totalExempt += taxableAmount; break;
                case 3:  // ITBIS 0% (Gravado 0%)
                    taxableG3 += taxableAmount; break;
                case 1: taxableG1 += taxableAmount; itbisG1 += itbisAmount; break;
                case 2: taxableG2 += taxableAmount; itbisG2 += itbisAmount; break;
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
                NumeroLinea = 1,
                TipoAjuste = "D",
                DescripcionDescuentooRecargo = dto.GlobalDiscountDescription ?? "Descuento Global",
                TipoValor = "$",
                MontoDescuentooRecargo = dto.GlobalDiscountAmount
            });
        }

        var finalTotal = (totalBase - totalItemDiscounts + totalItbis + totalIsc) - dto.GlobalDiscountAmount;

        // ── Totales block ──────────────────────────────────────────────────────

        decimal taxableGravado = totalBase - totalItemDiscounts - totalExempt;

        var totales = new EcfXmlTotales
        {
            EcfType = ecfType,
            MontoGravadoTotal = dto.ManualMontoGravadoTotal ?? (taxableGravado > 0 ? taxableGravado : null),
            MontoGravadoI1 = dto.ManualMontoGravadoI1 ?? (taxableG1 > 0 ? taxableG1 : null),
            MontoGravadoI2 = dto.ManualMontoGravadoI2 ?? (taxableG2 > 0 ? taxableG2 : null),
            MontoGravadoI3 = dto.ManualMontoGravadoI3 ?? (taxableG3 > 0 ? taxableG3 : null),
            MontoExento = dto.ManualMontoExento ?? (totalExempt > 0 ? totalExempt : null),

            ITBIS1 = (taxableG1 > 0 || dto.ManualTotalITBIS1.HasValue) ? 18 : null,
            ITBIS2 = (taxableG2 > 0 || dto.ManualTotalITBIS2.HasValue) ? 16 : null,
            ITBIS3 = (taxableG3 > 0 || dto.ManualTotalITBIS3.HasValue) ? 0 : null,

            TotalITBIS = dto.ManualTotalITBIS ?? (totalItbis > 0.00m ? totalItbis : null),
            TotalITBIS1 = dto.ManualTotalITBIS1 ?? (taxableG1 > 0.00m ? itbisG1 : null),
            TotalITBIS2 = dto.ManualTotalITBIS2 ?? (taxableG2 > 0.00m ? itbisG2 : null),
            TotalITBIS3 = dto.ManualTotalITBIS3 ?? (taxableG3 > 0.00m ? itbisG3 : null),

            MontoPeriodo = dto.ManualMontoPeriodo,
            ValorPagar = dto.ManualValorPagar,

            MontoImpuestoAdicional = totalIsc > 0 ? totalIsc : null,
            ImpuestosAdicionales = impuestosAdicionales,

            TotalITBISRetenido = dto.ManualTotalITBISRetenido,
            TotalISRRetencion = dto.ManualTotalISRRetencion,

            MontoTotal = dto.ManualMontoTotal ?? finalTotal
        };

        var root = new EcfXmlRoot
        {
            Encabezado = new EcfXmlEncabezado
            {
                Version = 1.0m,
                IdDoc = new EcfXmlIdDoc
                {
                    EcfType = ecfType,
                    Ncf = dto.Ncf,
                    SequenceExpirationDate = expirationDate,
                    IndicadorNotaCredito = ecfType == 34 ? 1 : null,
                    IndicadorMontoGravado = dto.ManualIndicadorMontoGravado ?? ((totalBase - totalExempt > 0) ? 1 : 0),
                    IncomeType = dto.IncomeType,
                    PaymentType = dto.PaymentType,
                    FechaLimitePago = dto.PaymentDeadline?.ToDrTime().ToString(DateFormat),
                    TerminoPago = dto.PaymentTerms
                },
                Emisor = new EcfXmlEmisor
                {
                    RncEmisor = dto.IssuerRnc,
                    RazonSocial = dto.IssuerName,
                    NombreComercial = dto.IssuerCommercialName,
                    Sucursal = dto.IssuerBranchCode,
                    Direccion = dto.IssuerAddress,
                    Municipio = dto.IssuerMunicipality,
                    Provincia = dto.IssuerProvince,
                    TelefonoTabla = string.IsNullOrWhiteSpace(dto.IssuerPhone) ? null : new EcfXmlEmisor.TablaTelefono { Telefono = dto.IssuerPhone },
                    CorreoEmisor = dto.IssuerEmail,
                    WebSite = dto.IssuerWebSite,
                    ActividadEconomica = dto.IssuerActivityCode,
                    CodigoVendedor = dto.IssuerSellerCode,
                    NumeroFacturaInterna = dto.InternalInvoiceNumber,
                    NumeroPedidoInterno = dto.InternalOrderNumber,
                    ZonaVenta = dto.SalesZone,
                    FechaEmision = issueDate
                },
                Comprador = new EcfXmlComprador
                {
                    EcfType = ecfType,
                    RncComprador = dto.CustomerRnc,
                    IdentificadorExtranjero = dto.CustomerForeignId,
                    RazonSocial = dto.CustomerName,
                    ContactoComprador = dto.CustomerContact,
                    CorreoComprador = dto.CustomerEmail,
                    DireccionComprador = dto.CustomerAddress,
                    PaisComprador = dto.CustomerCountry,
                    TelefonoAdicional = dto.CustomerTelephone,
                    MunicipioComprador = dto.CustomerMunicipality,
                    ProvinciaComprador = dto.CustomerProvince,
                    FechaEntrega = dto.DeliveryDate?.ToDrTime().ToString(DateFormat),
                    FechaOrdenCompra = dto.OrderDate?.ToDrTime().ToString(DateFormat),
                    NumeroOrdenCompra = dto.OrderNumber,
                    CodigoInternoComprador = dto.BuyerInternalCode
                },

                Totales = totales
            },
            Items = xmlItems,
            Adjustments = adjustments,

            // ── Reference Information (33, 34) ──────────────────────────────────
            InformacionReferencia = (ecfType == 33 || ecfType == 34) && !string.IsNullOrWhiteSpace(dto.ReferenceNcf)
                ? new EcfXmlInformacionReferencia
                {
                    NCFModificado = dto.ReferenceNcf,
                    RNCOtroContribuyente = dto.ReferenceCustomerRnc,
                    FechaNCFModificado = (dto.ReferenceIssueDate ?? DateTime.UtcNow).ToDrTime().ToString(DateFormat),
                    CodigoModificacion = dto.ReferenceReasonCode ?? 1,
                    RazonModificacion = dto.ReferenceReasonDescription
                } : null,

            FechaHoraFirma = signatureDateTime
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
    /// Loads the XmlSchemaSet for the given TipoeCF from the embedded resources in the ZynstormECFPlatform.Schemas
    /// assembly. Resource name example: "ZynstormECFPlatform.Schemas.XSD.e-CF 31 v.1.0.xsd"
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