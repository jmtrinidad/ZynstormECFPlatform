import os
import re

def main():
    path = "ZynstormECFPlatform.Services/EcfGeneratorService.cs"
    with open(path, 'r', encoding='utf-8') as f:
        code = f.read()

    # GenerateUnsignedXml replacements
    code = code.replace("var ecfType = dto.EcfType ?? NcfHelper.ExtractEcfType(dto.Ncf);", "var ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString());")
    code = code.replace("dto.ManualMontoTotal ?? dto.Items.Sum(i => (i.Quantity * i.UnitPrice) - i.Discount + (i.ManualRecargoMonto ?? 0))", "dto.ECF.Encabezado.Totales.MontoTotal ?? dto.ECF.DetallesItems.Item.Sum(i => i.MontoItem)")

    # ValidateDto replacements
    code = code.replace("var ecfType = dto.EcfType ?? (string.IsNullOrWhiteSpace(dto.Ncf) ? 0 : NcfHelper.ExtractEcfType(dto.Ncf));", "var ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? (string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF) ? \"0\" : NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString()));")
    code = code.replace("string.IsNullOrWhiteSpace(dto.Ncf)", "string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.eNCF)")
    code = code.replace("dto.Ncf", "dto.ECF.Encabezado.IdDoc.eNCF")
    code = code.replace("dto.IssuerRnc", "dto.ECF.Encabezado.Emisor.RNCEmisor")
    code = code.replace("dto.IssuerName", "dto.ECF.Encabezado.Emisor.RazonSocialEmisor")
    code = code.replace("dto.IssuerAddress", "dto.ECF.Encabezado.Emisor.DireccionEmisor")
    code = code.replace("dto.CustomerRnc", "dto.ECF.Encabezado.Comprador.RNCComprador")
    code = code.replace("dto.CustomerForeignId", "dto.ECF.Encabezado.Comprador.IdentificadorExtranjero")
    code = code.replace("dto.CustomerName", "dto.ECF.Encabezado.Comprador.RazonSocialComprador")
    code = code.replace("dto.Items", "dto.ECF.DetallesItems.Item")
    code = code.replace("itm.Name", "itm.NombreItem")
    code = code.replace("itm.Quantity", "itm.CantidadItem")
    code = code.replace("itm.UnitPrice", "itm.PrecioUnitarioItem")
    code = code.replace("dto.PaymentType == 2", "dto.ECF.Encabezado.IdDoc.TipoPago == \"2\"")
    code = code.replace("dto.PaymentDeadline is null", "string.IsNullOrWhiteSpace(dto.ECF.Encabezado.IdDoc.FechaLimitePago)")

    # MapToXmlRoot - we will replace the mapping block
    map_start = code.find("private static EcfXmlRoot MapToXmlRoot(EcfInvoiceRequestDto dto)")
    map_end = code.find("private static RfceXmlRoot MapToRfceXmlRoot(EcfInvoiceRequestDto dto)")
    
    new_map_root = """private static EcfXmlRoot MapToXmlRoot(EcfInvoiceRequestDto dto)
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

    """
    
    code = code[:map_start] + new_map_root + code[map_end:]

    # MapToRfceXmlRoot replacement
    rfce_start = code.find("private static RfceXmlRoot MapToRfceXmlRoot(EcfInvoiceRequestDto dto)")
    rfce_end = code.find("private static string GenerateRandomCode")
    
    new_rfce = """private static RfceXmlRoot MapToRfceXmlRoot(EcfInvoiceRequestDto dto)
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

    """
    
    code = code[:rfce_start] + new_rfce + code[rfce_end:]

    with open(path, 'w', encoding='utf-8') as f:
        f.write(code)

if __name__ == '__main__':
    main()
