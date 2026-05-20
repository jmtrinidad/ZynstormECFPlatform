using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Xml.Production;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Generates and validates unsigned e-CF XML documents compliant with DGII specifications. The XML structure is derived
/// from the official XSD schemas for each TipoeCF.
/// </summary>
public class EcfProductionGeneratorService : IEcfProductionGeneratorService
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

    static EcfProductionGeneratorService()
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

        // DGII requires Factura de Consumo below RD$250,000 to be sent through the B2C summary channel.
        bool isRfceSummary = isSummary || (ecfType == 32 && actualTotal < 250000m);

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



        xml = xml.Replace("<CompradorExp>", "<Comprador>")
                 .Replace("</CompradorExp>", "</Comprador>");

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
        var header = dto.ECF?.Encabezado;
        var idDoc = header?.IdDoc;
        var issuer = header?.Emisor;
        var buyer = header?.Comprador;
        var totals = header?.Totales;
        var items = dto.ECF?.DetallesItems?.Item;

        if (header == null)
        {
            errors.Add("El objeto ECF.Encabezado es requerido.");
            return errors;
        }

        if (idDoc == null)
        {
            errors.Add("El objeto ECF.Encabezado.IdDoc es requerido.");
            return errors;
        }

        if (issuer == null)
        {
            errors.Add("El objeto ECF.Encabezado.Emisor es requerido.");
            return errors;
        }

        if (totals == null)
        {
            errors.Add("El objeto ECF.Encabezado.Totales es requerido.");
            return errors;
        }

        var ecfType = 0;
        if (!string.IsNullOrWhiteSpace(idDoc.TipoeCF))
        {
            _ = int.TryParse(idDoc.TipoeCF, out ecfType);
        }
        else if (!string.IsNullOrWhiteSpace(idDoc.eNCF) && NcfHelper.TryExtractEcfType(idDoc.eNCF, out var extractedType))
        {
            ecfType = extractedType;
        }

        if (ecfType == 0)
            errors.Add("No se pudo determinar el TipoeCF. Verifique TipoeCF o eNCF.");

        if (string.IsNullOrWhiteSpace(idDoc.TipoeCF))
            errors.Add("El TipoeCF es requerido.");

        if (string.IsNullOrWhiteSpace(idDoc.eNCF))
        {
            errors.Add("El eNCF es requerido.");
        }
        else if (!NcfHelper.TryExtractEcfType(idDoc.eNCF, out _))
        {
            errors.Add($"El eNCF '{idDoc.eNCF}' no tiene el formato correcto (E + 2 digitos de tipo + 10 digitos).");
        }

        if (string.IsNullOrWhiteSpace(issuer.RNCEmisor))
            errors.Add("El RNC del emisor es requerido.");

        if (string.IsNullOrWhiteSpace(issuer.RazonSocialEmisor))
            errors.Add("La razon social del emisor es requerida.");

        if (string.IsNullOrWhiteSpace(issuer.DireccionEmisor))
            errors.Add("La direccion del emisor es requerida.");

        if (string.IsNullOrWhiteSpace(issuer.FechaEmision))
            errors.Add("La fecha de emision es requerida.");

        if (totals.MontoTotal == null)
            errors.Add("El monto total es requerido.");

        if (new[] { 31, 32, 33, 34, 44, 45, 46 }.Contains(ecfType) && string.IsNullOrWhiteSpace(idDoc.TipoIngresos))
            errors.Add($"El TipoIngresos es requerido para el comprobante tipo {ecfType}.");

        if (idDoc.TipoPago == "2" && string.IsNullOrWhiteSpace(idDoc.FechaLimitePago))
            errors.Add("La fecha limite de pago es requerida cuando el tipo de pago es Credito (2).");

        var formasPago = idDoc.TablaFormasPago?.FormaDePago;
        var hasFormaPagoShortcut = !string.IsNullOrWhiteSpace(idDoc.FormaPago);
        if (!string.IsNullOrWhiteSpace(idDoc.TipoPago) && formasPago?.Any() != true && !hasFormaPagoShortcut)
            errors.Add("Debe proveer al menos una FormaPago en IdDoc.TablaFormasPago.FormaDePago.");

        if (hasFormaPagoShortcut)
        {
            if (!IsValidPaymentForm(idDoc.FormaPago))
                errors.Add("FormaPago debe estar entre 1 y 8.");
            if (idDoc.MontoPago == null || idDoc.MontoPago < 0)
                errors.Add("MontoPago es requerido y no puede ser negativo cuando se envia FormaPago.");
        }

        if (formasPago?.Any() == true)
        {
            for (var i = 0; i < formasPago.Count; i++)
            {
                var formaPago = formasPago[i];
                if (!IsValidPaymentForm(formaPago.FormaPago))
                    errors.Add($"FormaDePago {i + 1}: FormaPago debe estar entre 1 y 8.");
                if (formaPago.MontoPago < 0)
                    errors.Add($"FormaDePago {i + 1}: MontoPago no puede ser negativo.");
            }
        }

        var buyerRncRequired = ecfType is 31 or 41 or 44 or 45;
        if (buyerRncRequired && string.IsNullOrWhiteSpace(buyer?.RNCComprador))
            errors.Add($"El RNC/Cedula del comprador es requerido para el comprobante tipo {ecfType}.");

        var buyerNameRequired = ecfType is 31 or 41 or 44 or 45;
        if (buyerNameRequired && string.IsNullOrWhiteSpace(buyer?.RazonSocialComprador))
            errors.Add($"El nombre del comprador es requerido para el comprobante tipo {ecfType}.");

        if (ecfType == 32 && totals.MontoTotal >= 250000m && string.IsNullOrWhiteSpace(buyer?.RNCComprador) && string.IsNullOrWhiteSpace(buyer?.IdentificadorExtranjero))
            errors.Add("Para Facturas de Consumo >= 250,000, debe especificar RNCComprador o IdentificadorExtranjero.");

        if (ecfType is 33 or 34)
        {
            if (dto.ECF?.InformacionReferencia == null)
            {
                errors.Add($"Para el comprobante tipo {ecfType}, el nodo InformacionReferencia es requerido.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.ECF.InformacionReferencia.NCFModificado))
                    errors.Add("Debe proveer el NCFModificado.");
                if (string.IsNullOrWhiteSpace(dto.ECF.InformacionReferencia.FechaNCFModificado))
                    errors.Add("Debe proveer la FechaNCFModificado.");
                if (string.IsNullOrWhiteSpace(dto.ECF.InformacionReferencia.CodigoModificacion))
                    errors.Add("Debe proveer el CodigoModificacion.");
            }
        }

        if (ecfType == 46)
        {
            if (string.IsNullOrWhiteSpace(buyer?.IdentificadorExtranjero) && string.IsNullOrWhiteSpace(buyer?.RNCComprador))
                errors.Add("Para Exportacion (46), debe proveer IdentificadorExtranjero o RNCComprador.");
            if (string.IsNullOrWhiteSpace(buyer?.RazonSocialComprador))
                errors.Add("Para Exportacion (46), el nombre del comprador es requerido.");
            if (string.IsNullOrWhiteSpace(buyer?.PaisComprador))
                errors.Add("Para Exportacion (46), el pais del comprador es requerido.");
        }

        if (ecfType == 47)
        {
            if (string.IsNullOrWhiteSpace(buyer?.IdentificadorExtranjero))
                errors.Add("Para Pagos al Exterior (47), el IdentificadorExtranjero es requerido.");
            if (string.IsNullOrWhiteSpace(buyer?.RazonSocialComprador))
                errors.Add("Para Pagos al Exterior (47), el nombre del comprador es requerido.");
        }

        if (items == null || items.Count == 0)
        {
            errors.Add("El documento debe contener al menos un item.");
        }
        else
        {
            for (int i = 0; i < items.Count; i++)
            {
                var itm = items[i];
                if (string.IsNullOrWhiteSpace(itm.NumeroLinea)) errors.Add($"Item {i + 1}: El numero de linea es requerido.");
                if (string.IsNullOrWhiteSpace(itm.IndicadorFacturacion)) errors.Add($"Item {i + 1}: El indicador de facturacion es requerido.");
                if (string.IsNullOrWhiteSpace(itm.NombreItem)) errors.Add($"Item {i + 1}: El nombre es requerido.");
                if (itm.CantidadItem <= 0) errors.Add($"Item {i + 1}: La cantidad debe ser mayor a cero.");
                if (string.IsNullOrWhiteSpace(itm.UnidadMedida)) errors.Add($"Item {i + 1}: La unidad de medida es requerida.");
                if (itm.PrecioUnitarioItem < 0) errors.Add($"Item {i + 1}: El precio unitario no puede ser negativo.");
                if (itm.MontoItem <= 0) errors.Add($"Item {i + 1}: El monto del item debe ser mayor a cero.");

                if (!string.IsNullOrWhiteSpace(itm.IscType) && itm.AdditionalTaxRate <= 0)
                    errors.Add($"Item {i + 1}: El campo AdditionalTaxRate es requerido cuando se especifica un IscType.");
            }
        }

        var adjustments = dto.ECF?.DescuentosORecargos?.DescuentoORecargo;
        if (adjustments?.Count > 0)
        {
            for (int i = 0; i < adjustments.Count; i++)
            {
                var adjustment = adjustments[i];
                if (string.IsNullOrWhiteSpace(adjustment.NumeroLinea)) errors.Add($"DescuentoORecargo {i + 1}: El numero de linea es requerido.");
                if (adjustment.TipoAjuste != "D" && adjustment.TipoAjuste != "R") errors.Add($"DescuentoORecargo {i + 1}: TipoAjuste debe ser D o R.");
                if (adjustment.TipoValor != null && adjustment.TipoValor != "$" && adjustment.TipoValor != "%") errors.Add($"DescuentoORecargo {i + 1}: TipoValor debe ser $ o %.");
                if (adjustment.MontoDescuentooRecargo < 0) errors.Add($"DescuentoORecargo {i + 1}: MontoDescuentooRecargo no puede ser negativo.");
                if (adjustment.IndicadorFacturacionDescuentooRecargo != null && !new[] { "1", "2", "3", "4" }.Contains(adjustment.IndicadorFacturacionDescuentooRecargo))
                    errors.Add($"DescuentoORecargo {i + 1}: IndicadorFacturacionDescuentooRecargo debe ser 1, 2, 3 o 4.");
            }
        }

        return errors;
    }

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
                        SubDescuentoPorcentaje = s.SubDescuentoPorcentaje,
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
                PrecioUnitarioItemDecimals = item.PrecioUnitarioItemDecimals,
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

        // Build ImpuestosAdicionales in Totales from items that have TablaImpuestoAdicional
        EcfXmlImpuestosAdicionales? impuestosAdicionales = null;
        var itemsWithTax = xmlItems.Where(i => i.TablaImpuestoAdicional?.ImpuestoAdicional?.Count > 0).ToList();
        if (itemsWithTax.Count > 0)
        {
            var taxGroups = itemsWithTax
                .SelectMany(i => i.TablaImpuestoAdicional!.ImpuestoAdicional.Select(ia => new { ia.TipoImpuesto }))
                .GroupBy(x => x.TipoImpuesto)
                .Select(g =>
                {
                    var dtoItems = dto.ECF.DetallesItems.Item
                        .Where(d => !string.IsNullOrWhiteSpace(d.IscType) && d.IscType == g.Key)
                        .ToList();
                    var rate = dtoItems.FirstOrDefault()?.AdditionalTaxRate;
                    var specificAmt = dtoItems.Sum(d => d.IscSpecificAmount ?? 0m);
                    var advaloremAmt = dtoItems.Sum(d => d.IscAdvaloremAmount ?? 0m);
                    var otherAmt = dtoItems.Sum(d => d.OtherAdditionalTaxAmount ?? 0m);
                    // Valid if we have a rate or any positive amount
                    bool isValid = rate.HasValue || specificAmt > 0 || advaloremAmt > 0 || otherAmt > 0;
                    return isValid ? new EcfXmlImpuestoAdicional
                    {
                        TipoImpuesto = g.Key,
                        TasaImpuestoAdicional = rate,
                        MontoImpuestoSelectivoConsumoEspecifico = specificAmt,
                        MontoImpuestoSelectivoConsumoAdvalorem = advaloremAmt,
                        OtrosImpuestosAdicionales = otherAmt
                    } : null;
                })
                .Where(x => x != null)
                .Cast<EcfXmlImpuestoAdicional>()
                .ToList();
            if (taxGroups.Count > 0)
                impuestosAdicionales = new EcfXmlImpuestosAdicionales { Items = taxGroups };

            // Remove TablaImpuestoAdicional from items whose TipoImpuesto is not registered in Totales
            var registeredTypes = taxGroups.Select(t => t.TipoImpuesto).ToHashSet();
            foreach (var item in xmlItems)
            {
                if (item.TablaImpuestoAdicional?.ImpuestoAdicional != null)
                {
                    item.TablaImpuestoAdicional.ImpuestoAdicional.RemoveAll(ia => !registeredTypes.Contains(ia.TipoImpuesto));
                    if (item.TablaImpuestoAdicional.ImpuestoAdicional.Count == 0)
                        item.TablaImpuestoAdicional = null;
                }
            }
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
            MontoImpuestoAdicional = e.Totales.MontoImpuestoAdicional,
            MontoNoFacturable = e.Totales.MontoNoFacturable,
            MontoTotal = e.Totales.MontoTotal ?? 0,
            ImpuestosAdicionales = impuestosAdicionales
        };

        var adjustments = dto.ECF.DescuentosORecargos?.DescuentoORecargo?
            .Select(a => new EcfXmlDescuentoORecargo
            {
                NumeroLinea = int.TryParse(a.NumeroLinea, out var numeroLinea) ? numeroLinea : 1,
                TipoAjuste = string.IsNullOrWhiteSpace(a.TipoAjuste) ? "D" : a.TipoAjuste,
                DescripcionDescuentooRecargo = a.DescripcionDescuentooRecargo,
                TipoValor = a.TipoValor,
                ValorDescuentooRecargo = a.ValorDescuentooRecargo,
                MontoDescuentooRecargo = a.MontoDescuentooRecargo,
                IndicadorFacturacionDescuentooRecargo = int.TryParse(a.IndicadorFacturacionDescuentooRecargo, out var indicadorDr) ? indicadorDr : null
            })
            .ToList() ?? [];

        var formasPago = BuildFormasPago(e.IdDoc);

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
                    TerminoPago = e.IdDoc.TerminoPago,
                    TablaFormasPago = formasPago.Count > 0
                        ? new EcfXmlTablaFormasPago { FormasDePago = formasPago }
                        : null,
                    TipoCuentaPago = e.IdDoc.TipoCuentaPago,
                    NumeroCuentaPago = e.IdDoc.NumeroCuentaPago,
                    BancoPago = e.IdDoc.BancoPago,
                    FechaDesde = e.IdDoc.FechaDesde,
                    FechaHasta = e.IdDoc.FechaHasta
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
                    TelefonoAdicional = string.IsNullOrWhiteSpace(e.Comprador.TelefonoAdicional)
                        ? e.Emisor.Telefono
                        : e.Comprador.TelefonoAdicional,
                    MunicipioComprador = e.Comprador.MunicipioComprador,
                    ProvinciaComprador = e.Comprador.ProvinciaComprador,
                    FechaEntrega = e.Comprador.FechaEntrega,
                    ContactoEntrega = e.Comprador.ContactoEntrega,
                    DireccionEntrega = e.Comprador.DireccionEntrega,
                    FechaOrdenCompra = e.Comprador.FechaOrdenCompra,
                    NumeroOrdenCompra = e.Comprador.NumeroOrdenCompra,
                    CodigoInternoComprador = e.Comprador.CodigoInternoComprador,
                    ResponsablePago = e.Comprador.ResponsablePago,
                    InformacionAdicionalComprador = e.Comprador.InformacionAdicionalComprador
                },
                Totales = totales
            },
            Items = xmlItems,
            Adjustments = adjustments,
            
            InformacionReferencia = dto.ECF.InformacionReferencia != null ? new EcfXmlInformacionReferencia
            {
                NCFModificado = dto.ECF.InformacionReferencia.NCFModificado!,
                RNCOtroContribuyente = dto.ECF.InformacionReferencia.RNCOtroContribuyente,
                FechaNCFModificado = dto.ECF.InformacionReferencia.FechaNCFModificado!,
                CodigoModificacion = int.TryParse(dto.ECF.InformacionReferencia.CodigoModificacion, out int cm) ? cm : null,
                RazonModificacion = dto.ECF.InformacionReferencia.RazonModificacion
            } : null,
            FechaHoraFirma = signatureDateTime
        };

        var doc = new XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

        return root;
    }

    private static List<EcfXmlFormaDePago> BuildFormasPago(EcfIdDocRequest idDoc)
    {
        if (idDoc.TablaFormasPago?.FormaDePago?.Any() == true)
        {
            return idDoc.TablaFormasPago.FormaDePago
                .Select(f => new EcfXmlFormaDePago
                {
                    FormaPago = int.TryParse(f.FormaPago, out var formaPago) ? formaPago : 0,
                    MontoPago = f.MontoPago
                })
                .Where(f => f.FormaPago > 0)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(idDoc.FormaPago) && idDoc.MontoPago.HasValue)
        {
            return
            [
                new EcfXmlFormaDePago
                {
                    FormaPago = int.TryParse(idDoc.FormaPago, out var formaPago) ? formaPago : 0,
                    MontoPago = idDoc.MontoPago.Value
                }
            ];
        }

        return [];
    }

    private static bool IsValidPaymentForm(string? value) =>
        int.TryParse(value, out var formaPago) && formaPago is >= 1 and <= 8;

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
                    TipoPago = int.TryParse(e.IdDoc.TipoPago, out int tp) ? tp : null,
                    TablaFormasPago = BuildRfceFormasPago(e.IdDoc)
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

    private static RfceXmlTablaFormasPago? BuildRfceFormasPago(EcfIdDocRequest idDoc)
    {
        var formasPago = BuildFormasPago(idDoc)
            .Select(f => new RfceXmlFormaDePago
            {
                FormaPago = f.FormaPago,
                MontoPago = f.MontoPago
            })
            .ToList();

        return formasPago.Count > 0
            ? new RfceXmlTablaFormasPago { FormasDePago = formasPago }
            : null;
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
