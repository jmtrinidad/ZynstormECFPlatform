import os
import re

def main():
    # We will just write out the complete replacements for the affected methods
    
    # 1. Update CertificationService.cs MapRowToRequest
    cert_path = "ZynstormECFPlatform.Services/CertificationService.cs"
    with open(cert_path, 'r', encoding='utf-8') as f:
        cert_code = f.read()
        
    map_row_start = cert_code.find("private EcfInvoiceRequestDto MapRowToRequest")
    map_row_end = cert_code.find("public async Task<CertificationSummaryDto> GetSummaryAsync()")
    
    new_map_row = """private EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int step, DateTime? fallbackDate = null)
    {
        var dto = new EcfInvoiceRequestDto
        {
            ExternalReference = GetStr(row, "NumeroFacturaInterna") ?? "",
            ECF = new EcfRequest
            {
                Encabezado = new EcfEncabezadoRequest
                {
                    IdDoc = new EcfIdDocRequest
                    {
                        TipoeCF = GetStr(row, "TipoeCF"),
                        eNCF = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "CasoPrueba") ?? "") ?? "",
                        FechaVencimientoSecuencia = GetStr(row, "FechaVencimientoSecuencia"),
                        IndicadorEnvioDiferido = GetStr(row, "IndicadorEnvioDiferido"),
                        IndicadorMontoGravado = GetStr(row, "IndicadorMontoGravado"),
                        TipoIngresos = GetStr(row, "TipoIngresos"),
                        TipoPago = GetStr(row, "TipoPago"),
                        FechaLimitePago = GetStr(row, "FechaLimitePago"),
                        IndicadorNotaCredito = GetStr(row, "IndicadorNotaCredito"),
                        TerminoPago = GetStr(row, "TerminoPago")
                    },
                    Emisor = new EcfEmisorRequest
                    {
                        RNCEmisor = GetStr(row, "RNCEmisor") ?? "",
                        RazonSocialEmisor = GetStr(row, "RazonSocialEmisor") ?? "",
                        NombreComercial = GetStr(row, "NombreComercial"),
                        Sucursal = GetStr(row, "Sucursal"),
                        DireccionEmisor = GetStr(row, "DireccionEmisor") ?? "",
                        Municipio = GetStr(row, "Municipio"),
                        Provincia = GetStr(row, "Provincia"),
                        Telefono = GetStr(row, "TelefonoEmisor[1]"),
                        CorreoEmisor = GetStr(row, "CorreoEmisor"),
                        WebSite = GetStr(row, "WebSite"),
                        ActividadEconomica = GetStr(row, "ActividadEconomica"),
                        CodigoVendedor = GetStr(row, "CodigoVendedor"),
                        NumeroFacturaInterna = GetStr(row, "NumeroFacturaInterna"),
                        NumeroPedidoInterno = GetStr(row, "NumeroPedidoInterno"),
                        ZonaVenta = GetStr(row, "ZonaVenta"),
                        FechaEmision = GetStr(row, "FechaEmision") ?? fallbackDate?.ToString("dd-MM-yyyy") ?? DateTime.Now.ToString("dd-MM-yyyy")
                    },
                    Comprador = new EcfCompradorRequest
                    {
                        RNCComprador = GetStr(row, "RNCComprador"),
                        IdentificadorExtranjero = GetStr(row, "IdentificadorExtranjero"),
                        RazonSocialComprador = GetStr(row, "RazonSocialComprador"),
                        ContactoComprador = GetStr(row, "ContactoComprador"),
                        CorreoComprador = GetStr(row, "CorreoComprador"),
                        DireccionComprador = GetStr(row, "DireccionComprador"),
                        PaisComprador = GetStr(row, "PaisComprador"),
                        TelefonoAdicional = GetStr(row, "TelefonoAdicional"),
                        MunicipioComprador = GetStr(row, "MunicipioComprador"),
                        ProvinciaComprador = GetStr(row, "ProvinciaComprador"),
                        FechaEntrega = GetStr(row, "FechaEntrega"),
                        FechaOrdenCompra = GetStr(row, "FechaOrdenCompra"),
                        NumeroOrdenCompra = GetStr(row, "NumeroOrdenCompra"),
                        CodigoInternoComprador = GetStr(row, "CodigoInternoComprador")
                    },
                    Totales = new EcfTotalesRequest
                    {
                        MontoGravadoTotal = GetDec(row, "MontoGravadoTotal"),
                        MontoGravadoI1 = GetDec(row, "MontoGravadoI1"),
                        MontoGravadoI2 = GetDec(row, "MontoGravadoI2"),
                        MontoGravadoI3 = GetDec(row, "MontoGravadoI3"),
                        MontoExento = GetDec(row, "MontoExento"),
                        TotalITBIS = GetDec(row, "TotalITBIS"),
                        TotalITBIS1 = GetDec(row, "TotalITBIS1"),
                        TotalITBIS2 = GetDec(row, "TotalITBIS2"),
                        TotalITBIS3 = GetDec(row, "TotalITBIS3"),
                        MontoTotal = GetDec(row, "MontoTotal"),
                        MontoNoFacturable = GetDec(row, "MontoNoFacturable"),
                        MontoPeriodo = GetDec(row, "MontoPeriodo"),
                        ValorPagar = GetDec(row, "ValorPagar"),
                        TotalITBISRetenido = GetDec(row, "TotalITBISRetenido"),
                        TotalISRRetencion = GetDec(row, "TotalISRRetencion"),
                        MontoImpuestoAdicional = GetDec(row, "MontoImpuestoAdicional")
                    }
                },
                InformacionReferencia = (GetStr(row, "TipoeCF") == "33" || GetStr(row, "TipoeCF") == "34") ? new EcfInformacionReferenciaRequest
                {
                    NCFModificado = CleanNcf(GetStr(row, "NCFModificado")),
                    RNCOtroContribuyente = GetStr(row, "RNCOtroContribuyente"),
                    FechaNCFModificado = GetStr(row, "FechaNCFModificado"),
                    CodigoModificacion = GetStr(row, "CodigoModificacion") ?? "3",
                    RazonModificacion = GetStr(row, "RazonModificacion") ?? "Ajuste parcial de montos"
                } : null
            }
        };

        for (int i = 1; i <= 50; i++)
        {
            var nombreKey = $"NombreItem[{i}]";
            var nombre = GetStr(row, nombreKey);
            if (nombre == null) continue;

            var item = new EcfItemRequestDto
            {
                NumeroLinea = i.ToString(),
                IndicadorFacturacion = GetStr(row, $"IndicadorFacturacion[{i}]"),
                NombreItem = nombre,
                DescripcionItem = GetStr(row, $"DescripcionItem[{i}]"),
                IndicadorBienoServicio = GetStr(row, $"IndicadorBienoServicio[{i}]"),
                CantidadItem = GetDec(row, $"CantidadItem[{i}]") ?? 1,
                UnidadMedida = GetStr(row, $"UnidadMedida[{i}]"),
                PrecioUnitarioItem = GetDec(row, $"PrecioUnitarioItem[{i}]") ?? 0,
                DescuentoMonto = GetDec(row, $"DescuentoMonto[{i}]"),
                MontoItem = GetDec(row, $"MontoItem[{i}]") ?? 0,
                RecargoMonto = GetDec(row, $"RecargoMonto[{i}]"),
                MontoITBISRetenido = GetDec(row, $"MontoITBISRetenido[{i}]"),
                MontoISRRetenido = GetDec(row, $"MontoISRRetenido[{i}]"),
                FechaElaboracion = GetStr(row, $"FechaElaboracion[{i}]"),
                FechaVencimientoItem = GetStr(row, $"FechaVencimientoItem[{i}]")
            };

            for (int k = 1; k <= 5; k++)
            {
                var subTipo = GetStr(row, $"TipoSubRecargo[{i}][{k}]");
                var subMonto = GetDec(row, $"MontosubRecargo[{i}][{k}]");
                if (subTipo != null || subMonto != null)
                {
                    item.TablaSubRecargo ??= new EcfTablaSubRecargoRequest();
                    item.TablaSubRecargo.SubRecargo.Add(new EcfSubRecargoRequest
                    {
                        TipoSubRecargo = subTipo ?? "$",
                        MontoSubRecargo = subMonto ?? 0,
                        SubRecargoPorcentaje = GetDec(row, $"SubRecargoPorcentaje[{i}][{k}]")
                    });
                }
            }

            dto.ECF.DetallesItems.Item.Add(item);

            if ((dto.ECF.Encabezado.IdDoc.TipoeCF == "33" || dto.ECF.Encabezado.IdDoc.TipoeCF == "34") && dto.ECF.DetallesItems.Item.Count >= 1)
                break;
        }

        return dto;
    }

    """
    
    cert_code = cert_code[:map_row_start] + new_map_row + cert_code[map_row_end:]
    
    # RunTestAsync fixes for the nested structure
    cert_code = cert_code.replace("requestDto.IssuerRnc", "requestDto.ECF.Encabezado.Emisor.RNCEmisor")
    cert_code = cert_code.replace("individualDto.IssuerRnc", "individualDto.ECF.Encabezado.Emisor.RNCEmisor")
    cert_code = cert_code.replace("individualDto.Ncf", "individualDto.ECF.Encabezado.IdDoc.eNCF")
    cert_code = cert_code.replace("individualDto.IssueDate:yyyy-MM-dd", "individualDto.ECF.Encabezado.Emisor.FechaEmision")
    cert_code = cert_code.replace("individualDto.ManualMontoTotal", "individualDto.ECF.Encabezado.Totales.MontoTotal")
    cert_code = cert_code.replace("requestDto.IssuerWebSite =", "requestDto.ECF.Encabezado.Emisor.WebSite =")
    cert_code = cert_code.replace("test.ENcf =", "test.ENcf = GetStr(row, \"ENCF\");\n            if(string.IsNullOrEmpty(test.ENcf)) test.ENcf = test.CaseNumber;\n            ")

    with open(cert_path, 'w', encoding='utf-8') as f:
        f.write(cert_code)
        
if __name__ == '__main__':
    main()
