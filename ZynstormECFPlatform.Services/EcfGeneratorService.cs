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
    private readonly XmlSerializer _rfceSerializer = new(typeof(RfceXmlRoot));
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
        // ── Step 1: Determine if this is an RFCE Summary ─────────────────────────
        // In this architecture, we treat Type 32 < 250k as RFCE summary by default
        // based on the NcfHelper providing the TipoeCF.
        var ecfType = NcfHelper.ExtractEcfType(dto.Ncf);
        bool isRfceSummary = (ecfType == 32 && (dto.ManualMontoTotal ?? 0) < 250000);
        
        // However, Step 4 is the SAME document but individual.
        // We need a way to force ECF or RFCE. We'll use a hack or check another property.
        // For now, let's look at if items are provided. RFCE summary has NO items in XSD.
        // Also, the caller (CertificationService) can indicate it. 
        // We'll check a custom field or just the context.
        
        // REFINED: We'll check if the ENCF starts with 'E' and is 13 chars.
        // The user's RFCE sheet rows have ENCF like 'E320000000014'.
        
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        string xml;
        using (var stringWriter = new Utf8StringWriter())
        {
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                // REFINED: If items are present, it MUST be an individual document (ECF), 
                // because RFCE summary (XSD) does not allow items.
                if (isRfceSummary && (dto.Items == null || dto.Items.Count == 0))
                {
                    // This is a summary (Step 3) - No items provided
                    var rfceRoot = MapToRfceXmlRoot(dto);
                    _rfceSerializer.Serialize(xmlWriter, rfceRoot, _noNamespaces);
                }
                else
                {
                    var root = MapToXmlRoot(dto);
                    _serializer.Serialize(xmlWriter, root, _noNamespaces);
                }
            }
            xml = stringWriter.ToString();
        }

        // ── NUCLEAR OPTION: Post-processing to ensure XSD compliance ────────
        // For type 44 (and others not in the allowed list), forcefully remove any <Retencion> block.
        // This is a safety measure against any serialization quirks.
        if (ecfType is not (41 or 47))
        {
            // Regex to remove <Retencion>...</Retencion> (including nested tags and content)
            xml = System.Text.RegularExpressions.Regex.Replace(
                xml, 
                @"<Retencion\b[^>]*>.*?</Retencion>", 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
        }


        // Add a diagnostic comment to verify the active version of the service
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");
        if (xml.Contains("<ECF>"))
        {
            xml = xml.Replace("<ECF>", $"<!-- Generator_Fix_V4_{timestamp} --><ECF>");
        }

        return xml;
    }


    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    /// <inheritdoc />

    public List<string> ValidateXmlAgainstSchema(string xml, int ecfType)
    {
        var errors = new List<string>();

        bool isRfce = xml.Contains("<RFCE", StringComparison.OrdinalIgnoreCase);
        var schemaSet = LoadSchemaSetForType(ecfType, isRfce);
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
        var issueDate = dto.IssueDate.ToString(DateFormat);
        var expirationDate = (dto.SequenceExpirationDate ?? dto.IssueDate.AddYears(1)).ToString(DateFormat);
        var signatureDateTime = DateTime.UtcNow.ToDrTime().ToString(DateTimeFormat);

        // ── Items + running totals ─────────────────────────────────────────────

        var xmlItems = new List<EcfXmlItem>();
        decimal totalBase = 0, totalItemDiscounts = 0, totalItbis = 0, totalExempt = 0, totalNoFacturable = 0;
        decimal taxableG1 = 0, taxableG2 = 0, taxableG3 = 0, itbisG1 = 0, itbisG2 = 0, itbisG3 = 0;

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

            var surchargeAmount = item.ManualRecargoMonto ?? 0;
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

            EcfXmlTablaSubRecargo? tablaSubRecargo = null;
            if (item.ManualSubRecargos.Count > 0)
            {
                tablaSubRecargo = new EcfXmlTablaSubRecargo
                {
                    SubRecargos = item.ManualSubRecargos.Select(s => new EcfXmlSubRecargo
                    {
                        TipoSubRecargo = s.TipoSubRecargo,
                        SubRecargoPorcentaje = s.SubRecargoPorcentaje,
                        MontoSubRecargo = s.MontoSubRecargo
                    }).ToList()
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
                UnidadMedida = item.UnitOfMeasure, // Remove hardcoded 43
                FechaElaboracion = item.FechaElaboracion,
                FechaVencimientoItem = item.FechaVencimientoItem,

                PrecioUnitarioItem = item.UnitPrice,
                DescuentoMonto = itemDiscountTotal > 0 ? itemDiscountTotal : null,
                TablaSubDescuento = tablaSubDescuento,
                RecargoMonto = surchargeAmount > 0 ? surchargeAmount : null,
                TablaSubRecargo = tablaSubRecargo,
                TablaImpuestoAdicional = tablaImpuesto,
                MontoItem = item.ManualMontoItem ?? (taxableAmount + itbisAmount + iscItemTotal + surchargeAmount),


            // ── Retentions handling (ONLY for Purchase 41 and Exportation 47)
            Retencion = (ecfType is 41 or 47) ? new EcfXmlItemRetencion
            {
                Indicador = 1, // Retención
                MontoITBISRetenido = (ecfType is 41) ? itbisRetenido : null,
                MontoISRRetenido = item.ManualMontoISRRetenido ?? (item.IsrRetentionAmount ?? 0)
            } : null

            });

            totalItemDiscounts += discountAmount;
            totalItbis += itbisAmount;


            switch (billingIndicator)
            {
                case 1:
                case 2:
                case 3:
                    totalBase += baseAmount; break;
                case 4:  // Exento
                    totalExempt += taxableAmount; break;
                case 0:  // No facturable
                    totalNoFacturable += taxableAmount; break;
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
            MontoNoFacturable = dto.ManualMontoNoFacturable ?? (totalNoFacturable > 0 ? totalNoFacturable : null),
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
                    IndicadorNotaCredito = dto.ManualIndicadorNotaCredito ?? (ecfType == 34 ? 1 : null),
                    IndicadorMontoGravado = dto.ManualIndicadorMontoGravado ?? (ecfType == 31 && taxableGravado > 0 ? 1 : null),

                    IncomeType = dto.IncomeType ?? ((ecfType is 31 or 32 or 33 or 44 or 45 or 46) ? "01" : null),


                    PaymentType = dto.PaymentType ?? ((ecfType is 31 or 32 or 33 or 34 or 41 or 44 or 45 or 46 or 47) ? 1 : null),
                    FechaLimitePago = dto.PaymentDeadline?.ToString(DateFormat),
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
                    FechaEntrega = dto.DeliveryDate?.ToString(DateFormat),
                    FechaOrdenCompra = dto.OrderDate?.ToString(DateFormat),
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
                    FechaNCFModificado = (dto.ReferenceIssueDate ?? DateTime.UtcNow).ToString(DateFormat),
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

    private static RfceXmlRoot MapToRfceXmlRoot(EcfInvoiceRequestDto dto)
    {
        var issueDate = dto.IssueDate.ToString(DateFormat);

        var root = new RfceXmlRoot
        {
            Encabezado = new RfceXmlEncabezado
            {
                Version = 1.0m,
                IdDoc = new RfceXmlIdDoc
                {
                    EcfType = 32,
                    Ncf = dto.Ncf,
                    TipoIngresos = dto.IncomeType ?? "01",
                    TipoPago = dto.PaymentType ?? 1
                },
                Emisor = new RfceXmlEmisor
                {
                    RncEmisor = dto.IssuerRnc,
                    RazonSocialEmisor = dto.IssuerName,
                    FechaEmision = issueDate
                },
                Comprador = new RfceXmlComprador
                {
                    RncComprador = string.IsNullOrEmpty(dto.CustomerRnc) ? null : dto.CustomerRnc,
                    IdentificadorExtranjero = dto.CustomerForeignId,
                    RazonSocialComprador = dto.CustomerName
                },
                Totales = new RfceXmlTotales
                {
                    MontoGravadoTotal = dto.ManualMontoGravadoTotal,
                    MontoGravadoI1 = dto.ManualMontoGravadoI1,
                    MontoGravadoI2 = dto.ManualMontoGravadoI2,
                    MontoGravadoI3 = dto.ManualMontoGravadoI3,
                    MontoExento = dto.ManualMontoExento,
                    TotalITBIS = dto.ManualTotalITBIS,
                    TotalITBIS1 = dto.ManualTotalITBIS1,
                    TotalITBIS2 = dto.ManualTotalITBIS2,
                    TotalITBIS3 = dto.ManualTotalITBIS3,
                    MontoImpuestoAdicional = dto.ManualMontoImpuestoAdicional,
                    MontoTotal = dto.ManualMontoTotal ?? 0,
                    MontoNoFacturable = dto.ManualMontoNoFacturable,
                    MontoPeriodo = dto.ManualMontoPeriodo
                },
                CodigoSeguridadeCF = "C8Y6N2" // Placeholder — in real life this comes from the original invoice hash
            }
        };

        return root;
    }

    /// <summary>
    /// Loads the XmlSchemaSet for the given TipoeCF from the embedded resources in the ZynstormECFPlatform.Schemas
    /// assembly. Resource name example: "ZynstormECFPlatform.Schemas.XSD.e-CF 31 v.1.0.xsd"
    /// </summary>
    private static XmlSchemaSet? LoadSchemaSetForType(int ecfType, bool isRfce = false)
    {
        string prefix = isRfce ? "RFCE" : "e-CF";
        var resourceName = _schemasAssembly
            .GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains(prefix, StringComparison.OrdinalIgnoreCase) && 
                                 r.Contains($" {ecfType} ", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null) return null;

        using var stream = _schemasAssembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, XmlReader.Create(stream));
        schemaSet.Compile();
        return schemaSet;
    }
}