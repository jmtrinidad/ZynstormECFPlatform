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
    private readonly XmlSerializer _acecfSerializer = new(typeof(AcecfXmlRoot));
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
    public string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false)
    {
        // ── Step 1: Determine the ECF Type (Priority: explicit dto.ECF.Encabezado.IdDoc.TipoeCF > NCF extraction) ─────────────────────────
        var ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString());
        
        // Calculate actual total from items (do not rely on ManualMontoTotal which may be null)
        decimal actualTotal = dto.ECF.Encabezado.Totales.MontoTotal ?? dto.ECF.DetallesItems.Item.Sum(i => i.MontoItem);

        // For Type 32: route to RFCE only if explicitly a summary OR if actual amount is below threshold
        bool isRfceSummary = isSummary;

        // SPECIAL CASE: if isSummary flag is NOT set, always use ECF path (not RFCE)
        if (ecfType == 32 && !isSummary)
        {
            isRfceSummary = false;
        }

        // CLEANUP: Buyer cleanup removed to ensure Excel data is included.
        
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
                // REFINED: Individual vs Summary selection
                if (isRfceSummary)
                {
                    // This is a summary (Step 3 or real B2C workflow)
                    var rfceRoot = MapToRfceXmlRoot(dto);
                    _rfceSerializer.Serialize(xmlWriter, rfceRoot, _noNamespaces);
                }
                else
                {
                    // This is an individual invoice (Step 4 or B2B workflow)
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

        // Selective removal of retention totals to satisfy mandatory requirements for 41/47
        if (ecfType is not (41 or 47))
        {
            xml = System.Text.RegularExpressions.Regex.Replace(
                xml, 
                @"<(TotalISRRetencion|TotalITBISRetenido)\b[^>]*>.*?</\1>", 
                string.Empty, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        // [NEW] Suppression of ITBIS breakdown for export (46) and foreign payments (47)
        // More aggressive regex to handle prefixes, self-closing tags, and case variations.
        if (ecfType is 46 or 47)
        {
            // For 46/47, we need to strip ITBIS sub-totals that are forbidden by XSD.
            // Aggressively remove ITBIS breakdown fields for both types.
            // For 46, we preserve MontoGravadoTotal and MontoGravadoI3 to satisfy Tasa Cero items.
            var forbidden = new List<string> { 
                "MontoGravadoI1", "MontoGravadoI2", 
                "ITBIS1", "ITBIS2", "TotalITBIS1", "TotalITBIS2",
                "TotalITBISPercepcion", "TotalISRPercepcion"
            };

            // For Type 47, ITBIS3 and all ITBIS totals are forbidden.
            // For Type 46, we MUST keep ITBIS3, TotalITBIS3 and TotalITBIS (set to 0) as Tasa Cero indicators.
            if (ecfType == 47)
            {
                forbidden.AddRange(new[] { "ITBIS3", "TotalITBIS", "TotalITBIS3", "MontoGravadoTotal", "MontoGravadoI3", "TotalITBISRetenido" });
            }
            
            if (ecfType == 46)
            {
                forbidden.AddRange(new[] { "MontoExento", "TotalITBISRetenido", "TotalISRRetencion" });
            }

            foreach (var field in forbidden)
            {
                xml = System.Text.RegularExpressions.Regex.Replace(
                    xml, 
                    $@"<(?:[\w\-]+:)?{field}\b[^>]*>(?:.*?</(?:[\w\-]+:)?{field}>| />)", 
                    string.Empty, 
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
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
        var ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? "0" : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString()));

        // NCF format validation
        if (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF))
        {
            errors.Add("El eNCF es requerido.");
        }
        else if (!NcfHelper.TryExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF, out _))
        {
            errors.Add($"El eNCF '{dto.ECF.Encabezado.IdDoc.eNCF}' no tiene el formato correcto (E + 2 dígitos de tipo + 10 dígitos).");
        }

        if (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Emisor.RNCEmisor))
            errors.Add("El RNC del emisor es requerido.");

        if (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Emisor.RazonSocialEmisor))
            errors.Add("La razón social del emisor es requerida.");

        if (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Emisor.DireccionEmisor))
            errors.Add("La dirección del emisor es requerida.");

        // For type 47 (Pagos al Exterior): IdentificadorExtranjero replaces CustomerRnc
        // For type 32 (Consumo): buyer data is optional
        bool buyerRncRequired = ecfType != 47 && ecfType != 32;
        if (buyerRncRequired && string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Comprador.RNCComprador) && string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Comprador.IdentificadorExtranjero))
            errors.Add("El RNC/Cédula del comprador es requerido.");

        bool buyerNameRequired = ecfType != 32;
        if (buyerNameRequired && string.IsNullOrWhiteSpace(dto.ECF.Encabezado.Comprador.RazonSocialComprador))
            errors.Add("El nombre del comprador es requerido.");

        if (dto.ECF.DetallesItems.Item.Count == 0)
        {
            errors.Add("El documento debe contener al menos un ítem.");
        }
        else
        {
            for (int i = 0; i < dto.ECF.DetallesItems.Item.Count; i++)
            {
                var itm = dto.ECF.DetallesItems.Item[i];
                if (string.IsNullOrWhiteSpace(itm.NombreItem)) errors.Add($"Item {i + 1}: El nombre es requerido.");
                if (itm.CantidadItem <= 0) errors.Add($"Item {i + 1}: La cantidad debe ser mayor a cero.");
                if (itm.PrecioUnitarioItem < 0) errors.Add($"Item {i + 1}: El precio unitario no puede ser negativo.");

                // ISC validation
                if (!string.IsNullOrWhiteSpace(itm.IscType))
                {
                    if (itm.AdditionalTaxRate <= 0)
                        errors.Add($"Item {i + 1}: El campo AdditionalTaxRate es requerido cuando se especifica un IscType.");
                }
            }
        }

        if (dto.ECF.Encabezado.IdDoc.TipoPago == "2" && string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.FechaLimitePago))
            errors.Add("La fecha límite de pago es requerida cuando el tipo de pago es Crédito (2).");

        return errors;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // XML Mapping
    // ═══════════════════════════════════════════════════════════════════════════

    private static EcfXmlRoot MapToXmlRoot(EcfInvoiceRequestDto dto)
    {
        var e = dto.ECF.Encabezado;
        var ecfType = int.Parse(e.IdDoc.TipoeCF ?? NcfHelper.ExtractEcfType(e.IdDoc.eNCF).ToString());
        
        var signatureDate = dto.SignatureDateOverride ?? DateTime.UtcNow.ToDrTime();
        var signatureDateTime = dto.ECF.FechaHoraFirma ?? signatureDate.ToString(DateTimeFormat);

        var xmlItems = new List<EcfXmlItem>();
        int lineNo = 1;
        foreach (var item in dto.ECF.DetallesItems.Item)
        {
            EcfXmlTablaSubDescuento? tablaSubDescuento = null;
            if (item.TablaSubDescuento?.SubDescuento?.Any() == true)
            {
                tablaSubDescuento = new EcfXmlTablaSubDescuento
                {
                    SubDescuentos = item.TablaSubDescuento.SubDescuento.Select(s => new EcfXmlSubDescuento
                    {
                        TipoSubDescuento = s.TipoSubDescuento ?? "$",
                        MontoSubDescuento = s.MontoSubDescuento ?? 0
                    }).ToList()
                };
            }

            EcfXmlTablaSubRecargo? tablaSubRecargo = null;
            if (item.TablaSubRecargo?.SubRecargo?.Any() == true)
            {
                tablaSubRecargo = new EcfXmlTablaSubRecargo
                {
                    SubRecargos = item.TablaSubRecargo.SubRecargo.Select(s => new EcfXmlSubRecargo
                    {
                        TipoSubRecargo = s.TipoSubRecargo ?? "$",
                        SubRecargoPorcentaje = s.SubRecargoPorcentaje,
                        MontoSubRecargo = s.MontoSubRecargo ?? 0
                    }).ToList()
                };
            }

            EcfXmlTablaImpuestoAdicionalItem? tablaImpuesto = null;
            if (!string.IsNullOrWhiteSpace(item.IscType))
            {
                tablaImpuesto = new EcfXmlTablaImpuestoAdicionalItem
                {
                    ImpuestoAdicional = [new EcfXmlImpuestoAdicionalRef { TipoImpuesto = item.IscType }]
                };
            }

            xmlItems.Add(new EcfXmlItem
            {
                EcfType = ecfType,
                NumeroLinea = int.TryParse(item.NumeroLinea, out int nl) ? nl : lineNo++,
                IndicadorFacturacion = int.TryParse(item.IndicadorFacturacion, out int iFact) ? iFact : null,
                Name = item.NombreItem,
                ItemType = int.TryParse(item.IndicadorBienoServicio, out int bs) ? bs : null,
                DescripcionItem = item.DescripcionItem,
                CantidadItem = item.CantidadItem,
                UnidadMedida = int.TryParse(item.UnidadMedida, out int um) ? um : null,
                PrecioUnitarioItem = item.PrecioUnitarioItem,
                DescuentoMonto = item.DescuentoMonto,
                TablaSubDescuento = tablaSubDescuento,
                RecargoMonto = item.RecargoMonto,
                TablaSubRecargo = tablaSubRecargo,
                TablaImpuestoAdicional = tablaImpuesto,
                MontoItem = item.MontoItem,
                FechaElaboracion = item.FechaElaboracion,
                FechaVencimientoItem = item.FechaVencimientoItem,
                Retencion = (ecfType is 41 or 47) ? new EcfXmlItemRetencion
                {
                    Indicador = 1,
                    MontoITBISRetenido = item.MontoITBISRetenido ?? 0,
                    MontoISRRetenido = item.MontoISRRetenido ?? 0
                } : null
            });
        }

        EcfXmlImpuestosAdicionales? impuestosAdicionales = null;
        if (e.Totales.MontoImpuestoAdicional > 0)
        {
            // Just map if we have details, but DGII schema requires items. 
            // In the real XML generator we calculated them from items. We'll simplify.
        }

        var totales = new EcfXmlTotales
        {
            EcfType = ecfType,
            MontoGravadoTotal = e.Totales.MontoGravadoTotal,
            MontoGravadoI1 = e.Totales.MontoGravadoI1,
            MontoGravadoI2 = e.Totales.MontoGravadoI2,
            MontoGravadoI3 = e.Totales.MontoGravadoI3,
            MontoExento = e.Totales.MontoExento,
            ITBIS1 = e.Totales.ITBIS1,
            ITBIS2 = e.Totales.ITBIS2,
            ITBIS3 = e.Totales.ITBIS3,
            TotalITBIS = e.Totales.TotalITBIS,
            TotalITBIS1 = e.Totales.TotalITBIS1,
            TotalITBIS2 = e.Totales.TotalITBIS2,
            TotalITBIS3 = e.Totales.TotalITBIS3,
            MontoPeriodo = e.Totales.MontoPeriodo,
            ValorPagar = e.Totales.ValorPagar,
            TotalITBISRetenido = e.Totales.TotalITBISRetenido,
            TotalISRRetencion = e.Totales.TotalISRRetencion,
            MontoNoFacturable = e.Totales.MontoNoFacturable,
            MontoTotal = e.Totales.MontoTotal ?? 0
        };

        var root = new EcfXmlRoot
        {
            Encabezado = new EcfXmlEncabezado
            {
                Version = decimal.TryParse(e.Version, out decimal v) ? v : 1.0m,
                IdDoc = new EcfXmlIdDoc
                {
                    EcfType = ecfType,
                    Ncf = e.IdDoc.eNCF,
                    SequenceExpirationDate = e.IdDoc.FechaVencimientoSecuencia,
                    IndicadorNotaCredito = int.TryParse(e.IdDoc.IndicadorNotaCredito, out int inc) ? inc : null,
                    IndicadorMontoGravado = int.TryParse(e.IdDoc.IndicadorMontoGravado, out int img) ? img : null,
                    IncomeType = e.IdDoc.TipoIngresos,
                    PaymentType = int.TryParse(e.IdDoc.TipoPago, out int tp) ? tp : null,
                    FechaLimitePago = e.IdDoc.FechaLimitePago,
                    TerminoPago = e.IdDoc.TerminoPago
                },
                Emisor = new EcfXmlEmisor
                {
                    RncEmisor = e.Emisor.RNCEmisor,
                    RazonSocial = e.Emisor.RazonSocialEmisor,
                    NombreComercial = e.Emisor.NombreComercial,
                    Sucursal = e.Emisor.Sucursal,
                    Direccion = e.Emisor.DireccionEmisor,
                    Municipio = e.Emisor.Municipio,
                    Provincia = e.Emisor.Provincia,
                    TelefonoTabla = string.IsNullOrWhiteSpace(e.Emisor.Telefono) ? null : new EcfXmlEmisor.TablaTelefono { Telefono = e.Emisor.Telefono },
                    CorreoEmisor = e.Emisor.CorreoEmisor,
                    WebSite = e.Emisor.WebSite,
                    ActividadEconomica = e.Emisor.ActividadEconomica,
                    CodigoVendedor = e.Emisor.CodigoVendedor,
                    NumeroFacturaInterna = e.Emisor.NumeroFacturaInterna,
                    NumeroPedidoInterno = e.Emisor.NumeroPedidoInterno,
                    ZonaVenta = e.Emisor.ZonaVenta,
                    FechaEmision = e.Emisor.FechaEmision
                },
                Comprador = new EcfXmlComprador
                {
                    EcfType = ecfType,
                    RncComprador = e.Comprador.RNCComprador,
                    IdentificadorExtranjero = e.Comprador.IdentificadorExtranjero,
                    RazonSocial = e.Comprador.RazonSocialComprador,
                    ContactoComprador = e.Comprador.ContactoComprador,
                    CorreoComprador = e.Comprador.CorreoComprador,
                    DireccionComprador = e.Comprador.DireccionComprador,
                    PaisComprador = e.Comprador.PaisComprador,
                    TelefonoAdicional = e.Comprador.TelefonoAdicional,
                    MunicipioComprador = e.Comprador.MunicipioComprador,
                    ProvinciaComprador = e.Comprador.ProvinciaComprador,
                    FechaEntrega = e.Comprador.FechaEntrega,
                    FechaOrdenCompra = e.Comprador.FechaOrdenCompra,
                    NumeroOrdenCompra = e.Comprador.NumeroOrdenCompra,
                    CodigoInternoComprador = e.Comprador.CodigoInternoComprador
                },
                Totales = totales
            },
            Items = xmlItems,
            
            InformacionReferencia = dto.ECF.InformacionReferencia != null ? new EcfXmlInformacionReferencia
            {
                NCFModificado = dto.ECF.InformacionReferencia.NCFModificado,
                RNCOtroContribuyente = dto.ECF.InformacionReferencia.RNCOtroContribuyente,
                FechaNCFModificado = dto.ECF.InformacionReferencia.FechaNCFModificado,
                CodigoModificacion = int.TryParse(dto.ECF.InformacionReferencia.CodigoModificacion, out int cm) ? cm : 3,
                RazonModificacion = dto.ECF.InformacionReferencia.RazonModificacion
            } : null,
            FechaHoraFirma = signatureDateTime
        };

        var doc = new XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

        return root;
    }

    private static RfceXmlRoot MapToRfceXmlRoot(EcfInvoiceRequestDto dto)
    {
        var e = dto.ECF.Encabezado;
        
        var root = new RfceXmlRoot
        {
            Encabezado = new RfceXmlEncabezado
            {
                Version = decimal.TryParse(e.Version, out decimal v) ? v : 1.0m,
                IdDoc = new RfceXmlIdDoc
                {
                    EcfType = 32,
                    Ncf = e.IdDoc.eNCF,
                    TipoIngresos = e.IdDoc.TipoIngresos,
                    TipoPago = int.TryParse(e.IdDoc.TipoPago, out int tp) ? tp : null
                },
                Emisor = new RfceXmlEmisor
                {
                    RncEmisor = e.Emisor.RNCEmisor,
                    RazonSocialEmisor = e.Emisor.RazonSocialEmisor,
                    FechaEmision = e.Emisor.FechaEmision
                },
                Comprador = new RfceXmlComprador
                {
                    RncComprador = string.IsNullOrEmpty(e.Comprador.RNCComprador) ? null : e.Comprador.RNCComprador,
                    IdentificadorExtranjero = e.Comprador.IdentificadorExtranjero,
                    RazonSocialComprador = e.Comprador.RazonSocialComprador
                },
                Totales = new RfceXmlTotales
                {
                    MontoGravadoTotal = e.Totales.MontoGravadoTotal,
                    MontoGravadoI1 = e.Totales.MontoGravadoI1,
                    MontoGravadoI2 = e.Totales.MontoGravadoI2,
                    MontoGravadoI3 = e.Totales.MontoGravadoI3,
                    MontoExento = e.Totales.MontoExento,
                    TotalITBIS = e.Totales.TotalITBIS,
                    TotalITBIS1 = e.Totales.TotalITBIS1,
                    TotalITBIS2 = e.Totales.TotalITBIS2,
                    TotalITBIS3 = e.Totales.TotalITBIS3,
                    MontoImpuestoAdicional = e.Totales.MontoImpuestoAdicional,
                    MontoTotal = e.Totales.MontoTotal ?? 0,
                    MontoNoFacturable = e.Totales.MontoNoFacturable,
                    MontoPeriodo = e.Totales.MontoPeriodo
                },
                CodigoSeguridadeCF = dto.SecurityCodeOverride ?? GenerateRandomCode(6)
            }
        };

        var doc = new System.Xml.XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

        return root;
    }

    private static string GenerateRandomCode(int length)
    {
        return Tools.GenerateRandomCode(length);
    }

    /// <summary>
    /// Loads the XmlSchemaSet for the given TipoeCF from the embedded resources in the ZynstormECFPlatform.Schemas
    /// assembly. Resource name example: "ZynstormECFPlatform.Schemas.XSD.e-CF 31 v.1.0.xsd"
    /// </summary>
    private static XmlSchemaSet? LoadSchemaSetForType(int ecfType, bool isRfce = false)
    {
        // Special case for ACECF (Commercial Approval)
        if (ecfType == 0 && !isRfce)
        {
            var arecfResource = _schemasAssembly
                .GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains("ACECF", StringComparison.OrdinalIgnoreCase));
            
            if (arecfResource != null)
            {
                using var aecStream = _schemasAssembly.GetManifestResourceStream(arecfResource);
                if (aecStream != null)
                {
                    var aecSchemaSet = new XmlSchemaSet();
                    aecSchemaSet.Add(null, XmlReader.Create(aecStream));
                    aecSchemaSet.Compile();
                    return aecSchemaSet;
                }
            }
        }

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
    public string GenerateArecfXml(AcecfRequestDto dto)
    {
        var model = new AcecfXmlRoot
        {
            Detalle = new ArecfXmlDetalle
            {
                Version = dto.Version ?? "1.0",
                RNCEmisor = dto.RNCEmisor,
                ENcf = dto.ENcf,
                FechaEmision = dto.FechaEmision,
                MontoTotal = dto.MontoTotal,
                RNCComprador = dto.RNCComprador,
                Estado = dto.Estado,
                DetalleMotivoRechazo = dto.DetalleMotivoRechazo,
                FechaHoraAprobacionComercial = dto.FechaHoraAprobacionComercial
            }
        };

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = false
        };

        using (var stringWriter = new Utf8StringWriter())
        {
            using (var xmlWriter = XmlWriter.Create(stringWriter, settings))
            {
                _acecfSerializer.Serialize(xmlWriter, model, _noNamespaces);
            }
            return stringWriter.ToString();
        }
    }
}
