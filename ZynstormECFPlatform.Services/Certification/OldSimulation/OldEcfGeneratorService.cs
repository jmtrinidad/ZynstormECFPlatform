using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Services.Xml.Simulation;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public class OldEcfGeneratorService : IOldEcfGeneratorService
{
    private const string DateFormat = "dd-MM-yyyy";
    private const string DateTimeFormat = "dd-MM-yyyy HH:mm:ss";

    private readonly XmlSerializer _serializer = new(typeof(EcfXmlRoot));
    private readonly XmlSerializer _rfceSerializer = new(typeof(RfceXmlRoot));
    private readonly XmlSerializer _acecfSerializer = new(typeof(AcecfXmlRoot));
    private static readonly XmlSerializerNamespaces _noNamespaces;

    private static readonly Assembly _schemasAssembly =
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ZynstormECFPlatform.Schemas")
            ?? Assembly.Load("ZynstormECFPlatform.Schemas");

    static OldEcfGeneratorService()
    {
        _noNamespaces = new XmlSerializerNamespaces();
        _noNamespaces.Add(string.Empty, string.Empty);
    }

    public string GenerateUnsignedXml(OldEcfInvoiceRequestDto dto, bool isSummary = false)
    {
        var ecfType = dto.EcfType ?? NcfHelper.ExtractEcfType(dto.Ncf);
        bool isRfceSummary = isSummary;

        if (ecfType == 32 && !isSummary)
        {
            isRfceSummary = false;
        }

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
                if (isRfceSummary)
                {
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

        // Nuclear Option Post-processing
        if (ecfType is not (41 or 47))
        {
            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"<Retencion\b[^>]*>.*?</Retencion>", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"<(TotalISRRetencion|TotalITBISRetenido)\b[^>]*>.*?</\1>", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        if (ecfType is 46 or 47)
        {
            var forbidden = new List<string> { 
                "MontoGravadoI1", "MontoGravadoI2", 
                "ITBIS1", "ITBIS2", "TotalITBIS1", "TotalITBIS2",
                "TotalITBISPercepcion", "TotalISRPercepcion"
            };

            if (ecfType == 47)
                forbidden.AddRange(new[] { "ITBIS3", "TotalITBIS", "TotalITBIS3", "MontoGravadoTotal", "MontoGravadoI3", "TotalITBISRetenido" });
            
            if (ecfType == 46)
                forbidden.AddRange(new[] { "MontoExento", "TotalITBISRetenido", "TotalISRRetencion" });

            foreach (var field in forbidden)
            {
                xml = System.Text.RegularExpressions.Regex.Replace(xml, $@"<(?:[\w\-]+:)?{field}\b[^>]*>(?:.*?</(?:[\w\-]+:)?{field}>| />)", string.Empty, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
        }

        return xml;
    }

    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

    public List<string> ValidateXmlAgainstSchema(string xml, int ecfType)
    {
        var errors = new List<string>();
        bool isRfce = xml.Contains("<RFCE", StringComparison.OrdinalIgnoreCase);
        var schemaSet = LoadSchemaSetForType(ecfType, isRfce);
        if (schemaSet is null)
        {
            errors.Add($"No se encontró el archivo XSD para TipoeCF {ecfType}.");
            return errors;
        }

        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints
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

    public List<string> ValidateXmlAgainstReference(string xml, int ecfType, string referenceXmlPath)
    {
        var errors = new List<string>();
        try
        {
            var generatedDoc = new XmlDocument();
            generatedDoc.LoadXml(xml);

            var referenceDoc = new XmlDocument();
            referenceDoc.Load(referenceXmlPath);

            var referencePaths = GetUniqueElementPaths(referenceDoc);
            var generatedPaths = GetUniqueElementPaths(generatedDoc);

            var skippablePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ECF/Encabezado/Totales/MontoGravadoTotal",
                "ECF/Encabezado/Totales/MontoGravadoI1",
                "ECF/Encabezado/Totales/MontoGravadoI2",
                "ECF/Encabezado/Totales/MontoGravadoI3",
                "ECF/Encabezado/Totales/MontoExento",
                "ECF/Encabezado/Totales/TotalITBIS",
                "ECF/Encabezado/Totales/TotalITBIS1",
                "ECF/Encabezado/Totales/TotalITBIS2",
                "ECF/Encabezado/Totales/TotalITBIS3",
                "ECF/Encabezado/Totales/MontoTotal",
                "ECF/Encabezado/Totales/MontoNoFacturable",
                "ECF/Encabezado/Totales/MontoPeriodo",
                "ECF/Encabezado/Totales/ValorPagar",
                "ECF/Encabezado/Totales/TotalITBISRetenido",
                "ECF/Encabezado/Totales/TotalISRRetencion",
                "ECF/Encabezado/IdDoc/IndicadorMontoGravado",
                "ECF/Encabezado/IdDoc/TipoPago",
                "ECF/Encabezado/IdDoc/FechaLimitePago",
                "ECF/Encabezado/IdDoc/TerminoPago"
            };

            foreach (var path in referencePaths)
            {
                if (path.Contains("Signature") || path.Contains("FechaHoraFirma")) continue;
                if (skippablePaths.Contains(path)) continue;

                if (!generatedPaths.Contains(path))
                {
                    errors.Add($"Elemento '{path}' (presente en referencia aprobada) no se encuentra en el XML generado.");
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error en comparación estructural: {ex.Message}");
        }
        return errors;
    }

    private HashSet<string> GetUniqueElementPaths(XmlDocument doc)
    {
        var paths = new HashSet<string>();
        if (doc.DocumentElement != null)
            GetPathsRecursive(doc.DocumentElement, "", paths);
        return paths;
    }

    private void GetPathsRecursive(XmlElement element, string currentPath, HashSet<string> paths)
    {
        string path = string.IsNullOrEmpty(currentPath) ? element.Name : $"{currentPath}/{element.Name}";
        paths.Add(path);
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement)
            {
                GetPathsRecursive(childElement, path, paths);
            }
        }
    }

    public List<string> ValidateDto(OldEcfInvoiceRequestDto dto)
    {
        var errors = new List<string>();
        var ecfType = dto.EcfType ?? (string.IsNullOrWhiteSpace(dto.Ncf) ? 0 : NcfHelper.ExtractEcfType(dto.Ncf));

        if (string.IsNullOrWhiteSpace(dto.Ncf)) errors.Add("El eNCF es requerido.");
        if (string.IsNullOrWhiteSpace(dto.IssuerRnc)) errors.Add("El RNC del emisor es requerido.");
        if (string.IsNullOrWhiteSpace(dto.IssuerName)) errors.Add("La razón social del emisor es requerida.");

        if (dto.Items.Count == 0) errors.Add("El documento debe contener al menos un ítem.");

        return errors;
    }

    private static EcfXmlRoot MapToXmlRoot(OldEcfInvoiceRequestDto dto)
    {
        var ecfType = NcfHelper.ExtractEcfType(dto.Ncf);
        var issueDate = dto.IssueDate.ToString(DateFormat);
        var expirationDate = (dto.SequenceExpirationDate ?? dto.IssueDate.AddYears(1)).ToString(DateFormat);
        var signatureDate = (dto.SignatureDateOverride ?? DateTime.UtcNow).ToDrTime();
        var signatureDateTime = signatureDate.ToString(DateTimeFormat);

        var xmlItems = new List<EcfXmlItem>();
        decimal totalBase = 0, totalItemDiscounts = 0, totalItbis = 0, totalExempt = 0, totalNoFacturable = 0;
        decimal taxableG1 = 0, taxableG2 = 0, taxableG3 = 0, itbisG1 = 0, itbisG2 = 0, itbisG3 = 0;

        var iscAccumulator = new Dictionary<string, EcfXmlImpuestoAdicional>(StringComparer.Ordinal);

        var lineNo = 1;
        foreach (var item in dto.Items)
        {
            var baseAmount = Math.Round(item.Quantity * item.UnitPrice, 2);
            var discountAmount = Math.Round(item.Discount, 2);
            var taxableAmount = baseAmount - discountAmount;

            var itbisAmount = item.ItbisAmount > 0 ? item.ItbisAmount : Math.Round(taxableAmount * (item.TaxPercentage / 100m), 2);
            var billingIndicator = item.BillingIndicator ?? item.TaxPercentage switch { 18m => 1, 16m => 2, 0m => 3, _ => 4 };

            if (billingIndicator is 4 or 0) itbisAmount = 0;

            EcfXmlTablaImpuestoAdicionalItem? tablaImpuesto = null;
            if (!string.IsNullOrWhiteSpace(item.IscType))
            {
                tablaImpuesto = new EcfXmlTablaImpuestoAdicionalItem { ImpuestoAdicional = [new EcfXmlImpuestoAdicionalRef { TipoImpuesto = item.IscType }] };
                if (!iscAccumulator.TryGetValue(item.IscType, out var entry))
                {
                    entry = new EcfXmlImpuestoAdicional { TipoImpuesto = item.IscType, TasaImpuestoAdicional = item.AdditionalTaxRate };
                    iscAccumulator[item.IscType] = entry;
                }
                if (item.IscSpecificAmount > 0) entry.MontoImpuestoSelectivoConsumoEspecifico = (entry.MontoImpuestoSelectivoConsumoEspecifico ?? 0) + Math.Round(item.IscSpecificAmount, 2);
                if (item.IscAdvaloremAmount > 0) entry.MontoImpuestoSelectivoConsumoAdvalorem = (entry.MontoImpuestoSelectivoConsumoAdvalorem ?? 0) + Math.Round(item.IscAdvaloremAmount, 2);
                if (item.OtherAdditionalTaxAmount > 0) entry.OtrosImpuestosAdicionales = (entry.OtrosImpuestosAdicionales ?? 0) + Math.Round(item.OtherAdditionalTaxAmount, 2);
            }

            var iscItemTotal = item.IscSpecificAmount + item.IscAdvaloremAmount + item.OtherAdditionalTaxAmount;
            var surchargeAmount = item.ManualRecargoMonto ?? 0;
            var itemDiscountTotal = item.ManualDescuentoMonto ?? (discountAmount > 0 ? discountAmount : 0);

            xmlItems.Add(new EcfXmlItem
            {
                EcfType = ecfType,
                NumeroLinea = lineNo++,
                IndicadorFacturacion = billingIndicator,
                Name = item.Name,
                ItemType = item.ItemType,
                DescripcionItem = item.Description,
                CantidadItem = item.Quantity,
                UnidadMedida = item.UnitOfMeasure,
                FechaElaboracion = item.FechaElaboracion,
                FechaVencimientoItem = item.FechaVencimientoItem,
                PrecioUnitarioItem = item.UnitPrice,
                DescuentoMonto = itemDiscountTotal > 0 ? itemDiscountTotal : null,
                RecargoMonto = surchargeAmount > 0 ? surchargeAmount : null,
                TablaImpuestoAdicional = tablaImpuesto,
                MontoItem = item.ManualMontoItem ?? (taxableAmount + iscItemTotal + surchargeAmount),
                Retencion = (ecfType is 41 or 47) ? new EcfXmlItemRetencion
                {
                    Indicador = 1,
                    MontoITBISRetenido = (ecfType == 41) ? (item.ManualMontoITBISRetenido ?? 0) : 0,
                    MontoISRRetenido = item.ManualMontoISRRetenido ?? item.IsrRetentionAmount ?? 0
                } : null
            });

            totalItbis += itbisAmount;
            switch (billingIndicator)
            {
                case 1: taxableG1 += taxableAmount; itbisG1 += itbisAmount; totalBase += baseAmount; totalItemDiscounts += discountAmount; break;
                case 2: taxableG2 += taxableAmount; itbisG2 += itbisAmount; totalBase += baseAmount; totalItemDiscounts += discountAmount; break;
                case 3: taxableG3 += taxableAmount; itbisG3 += itbisAmount; totalBase += baseAmount; totalItemDiscounts += discountAmount; break;
                case 4: totalExempt += (taxableAmount + surchargeAmount); break;
                case 0: totalNoFacturable += (taxableAmount + surchargeAmount); break;
            }
        }

        decimal totalIsc = 0;
        EcfXmlImpuestosAdicionales? impuestosAdicionales = null;
        if (iscAccumulator.Count > 0)
        {
            totalIsc = iscAccumulator.Values.Sum(e => (e.MontoImpuestoSelectivoConsumoEspecifico ?? 0) + (e.MontoImpuestoSelectivoConsumoAdvalorem ?? 0) + (e.OtrosImpuestosAdicionales ?? 0));
            impuestosAdicionales = new EcfXmlImpuestosAdicionales { Items = [.. iscAccumulator.Values] };
        }

        var adjustments = new List<EcfXmlDescuentoORecargo>();
        var adjustedTaxableG1 = taxableG1;
        var adjustedTaxableG2 = taxableG2;
        var adjustedTaxableG3 = taxableG3;
        var adjustedExempt = totalExempt;
        var adjustedNoFacturable = totalNoFacturable;
        var adjustedItbisG1 = itbisG1;
        var adjustedItbisG2 = itbisG2;
        var adjustedItbisG3 = itbisG3;
        var adjustedTotalItbis = totalItbis;

        if (dto.GlobalDiscountAmount > 0)
        {
            // Deriving indicator: if we have gravado, use 1, else use 4 (Exento)
            int discountIndicator = (taxableG1 > 0 || taxableG2 > 0 || taxableG3 > 0) ? 1 : 4;
            var globalDiscount = dto.GlobalDiscountAmount;

            if (taxableG1 > 0)
            {
                adjustedTaxableG1 = Math.Max(0, taxableG1 - globalDiscount);
                adjustedItbisG1 = Math.Round(adjustedTaxableG1 * 0.18m, 2);
            }
            else if (taxableG2 > 0)
            {
                adjustedTaxableG2 = Math.Max(0, taxableG2 - globalDiscount);
                adjustedItbisG2 = Math.Round(adjustedTaxableG2 * 0.16m, 2);
            }
            else if (taxableG3 > 0)
            {
                adjustedTaxableG3 = Math.Max(0, taxableG3 - globalDiscount);
                adjustedItbisG3 = 0;
            }
            else if (totalExempt > 0)
            {
                adjustedExempt = Math.Max(0, totalExempt - globalDiscount);
            }
            else if (totalNoFacturable > 0)
            {
                adjustedNoFacturable = Math.Max(0, totalNoFacturable - globalDiscount);
            }

            adjustedTotalItbis = adjustedItbisG1 + adjustedItbisG2 + adjustedItbisG3;

            adjustments.Add(new EcfXmlDescuentoORecargo 
            { 
                NumeroLinea = 1, 
                TipoAjuste = "D", 
                IndicadorFacturacion = discountIndicator,
                DescripcionDescuentooRecargo = dto.GlobalDiscountDescription ?? "Descuento Global", 
                TipoValor = "$", 
                ValorDescuentooRecargo = dto.GlobalDiscountAmount,
                MontoDescuentooRecargo = dto.GlobalDiscountAmount 
            });
        }

        var finalTotal = adjustedTaxableG1 + adjustedTaxableG2 + adjustedTaxableG3 + adjustedExempt + adjustedNoFacturable + adjustedTotalItbis + totalIsc;
        decimal taxableGravado = adjustedTaxableG1 + adjustedTaxableG2 + adjustedTaxableG3;

        var totales = new EcfXmlTotales
        {
            EcfType = ecfType,
            MontoGravadoTotal = dto.ManualMontoGravadoTotal ?? (taxableGravado > 0 ? taxableGravado : null),
            MontoGravadoI1 = dto.ManualMontoGravadoI1 ?? (adjustedTaxableG1 > 0 ? adjustedTaxableG1 : null),
            MontoGravadoI2 = dto.ManualMontoGravadoI2 ?? (adjustedTaxableG2 > 0 ? adjustedTaxableG2 : null),
            MontoGravadoI3 = dto.ManualMontoGravadoI3 ?? (adjustedTaxableG3 > 0 ? adjustedTaxableG3 : null),
            MontoExento = dto.ManualMontoExento ?? (adjustedExempt > 0 ? adjustedExempt : null),
            ITBIS1 = (ecfType is 46 or 47) ? null : ((taxableG1 > 0 || dto.ManualTotalITBIS1.HasValue) ? 18 : null),
            ITBIS2 = (ecfType is 46 or 47) ? null : ((taxableG2 > 0 || dto.ManualTotalITBIS2.HasValue) ? 16 : null),
            ITBIS3 = (ecfType is 47) ? null : ((taxableG3 > 0 || dto.ManualTotalITBIS3.HasValue) ? 0 : null),
            TotalITBIS = dto.ManualTotalITBIS ?? ((adjustedTotalItbis > 0.00m || (ecfType == 46 && adjustedTaxableG3 > 0)) ? adjustedTotalItbis : null),
            TotalITBIS1 = dto.ManualTotalITBIS1 ?? (adjustedTaxableG1 > 0.00m ? adjustedItbisG1 : null),
            TotalITBIS2 = dto.ManualTotalITBIS2 ?? (adjustedTaxableG2 > 0.00m ? adjustedItbisG2 : null),
            TotalITBIS3 = dto.ManualTotalITBIS3 ?? (adjustedTaxableG3 > 0.00m ? adjustedItbisG3 : null),
            MontoImpuestoAdicional = totalIsc > 0 ? totalIsc : null,
            ImpuestosAdicionales = impuestosAdicionales,
            MontoNoFacturable = dto.ManualMontoNoFacturable ?? (adjustedNoFacturable > 0 ? adjustedNoFacturable : null),
            MontoTotal = dto.ManualMontoTotal ?? finalTotal,
            MontoPeriodo = dto.ManualMontoPeriodo,
            ValorPagar = dto.ManualValorPagar,
            TotalITBISRetenido = dto.ManualTotalITBISRetenido,
            TotalISRRetencion = dto.ManualTotalISRRetencion
        };

        int? derivedIndicador = null;
        if (ecfType is 31 or 32 or 33 or 34 or 41 or 45 &&
            (totalBase > 0 || totalExempt > 0 || totalNoFacturable > 0))
        {
            derivedIndicador = 0;
        }

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
                    IndicadorMontoGravado = dto.ManualIndicadorMontoGravado ?? derivedIndicador,
                    IncomeType = dto.IncomeType,
                    PaymentType = dto.PaymentType,
                    FechaLimitePago = dto.PaymentDeadline?.ToString(DateFormat),
                    TerminoPago = dto.PaymentTerms,
                    NumeroCuentaPago = dto.PaymentAccountNumber,
                    BancoPago = dto.PaymentBank,
                    IndicadorNotaCredito = ecfType == 34 ? (dto.ManualIndicadorNotaCredito ?? 0) : null
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
            InformacionReferencia = (ecfType == 33 || ecfType == 34) && !string.IsNullOrWhiteSpace(dto.ReferenceNcf)
                ? new EcfXmlInformacionReferencia
                {
                    NCFModificado = dto.ReferenceNcf,
                    RNCOtroContribuyente = dto.ReferenceCustomerRnc,
                    FechaNCFModificado = (dto.ReferenceIssueDate ?? DateTime.UtcNow).ToString(DateFormat),
                    CodigoModificacion = dto.ReferenceReasonCode ?? 3,
                    RazonModificacion = string.IsNullOrWhiteSpace(dto.ReferenceReasonDescription)
                        ? "Ajuste parcial de montos"
                        : dto.ReferenceReasonDescription
                } : null,
            FechaHoraFirma = signatureDateTime
        };

        if (ecfType == 46)
        {
            root.Encabezado.InformacionesAdicionales = new EcfXmlInformacionesAdicionales
            {
                FechaEmbarque = dto.ExportFechaEmbarque,
                NumeroEmbarque = dto.ExportNumeroEmbarque,
                NumeroContenedor = dto.ExportNumeroContenedor,
                NumeroReferencia = dto.ExportNumeroReferencia,
                NombrePuertoEmbarque = dto.ExportNombrePuertoEmbarque,
                CondicionesEntrega = dto.ExportCondicionesEntrega,
                TotalFob = dto.ExportTotalFob,
                Seguro = dto.ExportSeguro,
                Flete = dto.ExportFlete,
                OtrosGastos = dto.ExportOtrosGastos,
                TotalCif = dto.ExportTotalCif,
                RegimenAduanero = dto.ExportRegimenAduanero,
                NombrePuertoSalida = dto.ExportNombrePuertoSalida,
                NombrePuertoDesembarque = dto.ExportNombrePuertoDesembarque,
                PesoBruto = dto.ExportPesoBruto,
                PesoNeto = dto.ExportPesoNeto,
                UnidadPesoBruto = dto.ExportUnidadPesoBruto,
                UnidadPesoNeto = dto.ExportUnidadPesoNeto,
                CantidadBulto = dto.ExportCantidadBulto,
                UnidadBulto = dto.ExportUnidadBulto,
                VolumenBulto = dto.ExportVolumenBulto,
                UnidadVolumen = dto.ExportUnidadVolumen
            };

            root.Encabezado.Transporte = new EcfXmlTransporte
            {
                ViaTransporte = dto.TranspViaTransporte,
                PaisOrigen = dto.TranspPaisOrigen,
                DireccionDestino = dto.TranspDireccionDestino,
                PaisDestino = dto.TranspPaisDestino,
                RncCompaniaTransportista = dto.TranspRncCompaniaTransportista,
                NombreCompaniaTransportista = dto.TranspNombreCompaniaTransportista,
                NumeroViaje = dto.TranspNumeroViaje,
                Conductor = dto.TranspConductor,
                DocumentoTransporte = dto.TranspDocumentoTransporte,
                Ficha = dto.TranspFicha,
                Placa = dto.TranspPlaca,
                RutaTransporte = dto.TranspRutaTransporte,
                ZonaTransporte = dto.TranspZonaTransporte,
                NumeroAlbaran = dto.TranspNumeroAlbaran
            };
        }

        if (ecfType == 47 && !string.IsNullOrWhiteSpace(dto.CurrencyTipoMoneda))
        {
            root.Encabezado.OtraMoneda = new EcfXmlOtraMoneda
            {
                TipoMoneda = dto.CurrencyTipoMoneda,
                TipoCambio = dto.CurrencyTipoCambio,
                MontoGravadoOtraMoneda = dto.CurrencyMontoGravado,
                MontoExentoOtraMoneda = dto.CurrencyMontoExento,
                TotalITBISOtraMoneda = dto.CurrencyTotalITBIS,
                MontoTotalOtraMoneda = dto.CurrencyMontoTotal
            };
        }

        var doc = new XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");

        return root;
    }

    private static RfceXmlRoot MapToRfceXmlRoot(OldEcfInvoiceRequestDto dto)
    {
        var root = new RfceXmlRoot
        {
            Encabezado = new RfceXmlEncabezado
            {
                IdDoc = new RfceXmlIdDoc { EcfType = 32, Ncf = dto.Ncf, TipoIngresos = dto.IncomeType, TipoPago = dto.PaymentType },
                Emisor = new RfceXmlEmisor { RncEmisor = dto.IssuerRnc, RazonSocialEmisor = dto.IssuerName, FechaEmision = dto.IssueDate.ToString(DateFormat) },
                Comprador = new RfceXmlComprador { RncComprador = string.IsNullOrEmpty(dto.CustomerRnc) ? null : dto.CustomerRnc, IdentificadorExtranjero = dto.CustomerForeignId, RazonSocialComprador = dto.CustomerName },
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
                CodigoSeguridadeCF = dto.SecurityCodeOverride ?? Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()
            }
        };
        var doc = new XmlDocument();
        root.Signature = doc.CreateElement("Signature", "http://www.w3.org/2000/09/xmldsig#");
        return root;
    }

    private static XmlSchemaSet? LoadSchemaSetForType(int ecfType, bool isRfce = false)
    {
        string prefix = isRfce ? "RFCE" : "e-CF";
        var resourceName = _schemasAssembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains(prefix, StringComparison.OrdinalIgnoreCase) && r.Contains($" {ecfType} ", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null) return null;
        using var stream = _schemasAssembly.GetManifestResourceStream(resourceName);
        if (stream is null) return null;

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, XmlReader.Create(stream));
        schemaSet.Compile();
        return schemaSet;
    }
}
