using ZynstormECFPlatform.Services.Certification;

namespace ZynstormECFPlatform.Tests.Certification;

public class CertificationExcelDescuentosORecargosTests
{
    // Fila real E320000000004 del Excel de certificación (DGII lo rechazó porque se omitían los recargos globales).
    private static Dictionary<string, object> Row() => new()
    {
        ["CasoPrueba"] = "05601434623E320000000004",
        ["Version"] = "1.0",
        ["TipoeCF"] = "32",
        ["ENCF"] = "E320000000004",
        ["IndicadorMontoGravado"] = "0",
        ["TipoIngresos"] = "01",
        ["TipoPago"] = "1",
        ["FormaPago[1]"] = "1",
        ["MontoPago[1]"] = "567375.00",
        ["RNCEmisor"] = "05601434623",
        ["RazonSocialEmisor"] = "DOCUMENTOS ELECTRONICOS DE 02",
        ["NombreComercial"] = "DOCUMENTOS ELECTRONICOS DE 02",
        ["DireccionEmisor"] = "AVE. ISABEL AGUIAR NO. 269, ZONA INDUSTRIAL DE HERRERA",
        ["Municipio"] = "010100",
        ["Provincia"] = "010000",
        ["TelefonoEmisor[1]"] = "809-472-7676",
        ["TelefonoEmisor[2]"] = "809-491-1918",
        ["CorreoEmisor"] = "DOCUMENTOSELECTRONICOSDE0612345678969789+9000000000000000000000000000001@123.COM",
        ["WebSite"] = "www.facturaelectronica.com",
        ["CodigoVendedor"] = "AA0000000100000000010000000002000000000300000000050000000006",
        ["NumeroFacturaInterna"] = "123456789016",
        ["NumeroPedidoInterno"] = "123456789016",
        ["ZonaVenta"] = "NORTE",
        ["FechaEmision"] = "01-04-2020",
        ["RNCComprador"] = "131880681",
        ["RazonSocialComprador"] = "DOCUMENTOS ELECTRONICOS DE 03",
        ["ContactoComprador"] = "MARCOS LATIPLOL",
        ["CorreoComprador"] = "MARCOSLATIPLOL@KKKK.COM",
        ["DireccionComprador"] = "CALLE JACINTO DE LA CONCHA FELIZ ESQUINA 27 DE FEBRERO,FRENTE A DOMINO",
        ["MunicipioComprador"] = "010100",
        ["ProvinciaComprador"] = "010000",
        ["FechaEntrega"] = "10-10-2020",
        ["FechaOrdenCompra"] = "10-11-2018",
        ["NumeroOrdenCompra"] = "4500352238",
        ["CodigoInternoComprador"] = "10633440",
        ["MontoGravadoTotal"] = "484250.00",
        ["MontoGravadoI1"] = "282250.00",
        ["MontoGravadoI2"] = "202000.00",
        ["ITBIS1"] = "18",
        ["ITBIS2"] = "16",
        ["TotalITBIS"] = "83125.00",
        ["TotalITBIS1"] = "50805.00",
        ["TotalITBIS2"] = "32320.00",
        ["MontoTotal"] = "567375.00",
        ["MontoPeriodo"] = "567375.00",
        ["ValorPagar"] = "567375.00",
        ["NumeroLinea[1]"] = "1",
        ["IndicadorFacturacion[1]"] = "1",
        ["NombreItem[1]"] = "BLOCK",
        ["IndicadorBienoServicio[1]"] = "1",
        ["CantidadItem[1]"] = "100.00",
        ["UnidadMedida[1]"] = "23",
        ["PrecioUnitarioItem[1]"] = "450.0000",
        ["MontoItem[1]"] = "45000.00",
        ["NumeroLinea[2]"] = "2",
        ["IndicadorFacturacion[2]"] = "1",
        ["NombreItem[2]"] = "VARILLAS",
        ["IndicadorBienoServicio[2]"] = "1",
        ["CantidadItem[2]"] = "300.00",
        ["UnidadMedida[2]"] = "23",
        ["PrecioUnitarioItem[2]"] = "350.0000",
        ["MontoItem[2]"] = "105000.00",
        ["NumeroLinea[3]"] = "3",
        ["IndicadorFacturacion[3]"] = "1",
        ["NombreItem[3]"] = "CINZ",
        ["IndicadorBienoServicio[3]"] = "1",
        ["CantidadItem[3]"] = "200.00",
        ["UnidadMedida[3]"] = "23",
        ["PrecioUnitarioItem[3]"] = "425.0000",
        ["MontoItem[3]"] = "85000.00",
        ["NumeroLinea[4]"] = "4",
        ["IndicadorFacturacion[4]"] = "1",
        ["NombreItem[4]"] = "CLAVOS DE MEDIA",
        ["IndicadorBienoServicio[4]"] = "1",
        ["CantidadItem[4]"] = "50.00",
        ["UnidadMedida[4]"] = "23",
        ["PrecioUnitarioItem[4]"] = "550.0000",
        ["MontoItem[4]"] = "27500.00",
        ["NumeroLinea[5]"] = "5",
        ["IndicadorFacturacion[5]"] = "1",
        ["NombreItem[5]"] = "CLAVOS DE CUARTA",
        ["IndicadorBienoServicio[5]"] = "1",
        ["CantidadItem[5]"] = "50.00",
        ["UnidadMedida[5]"] = "23",
        ["PrecioUnitarioItem[5]"] = "325.0000",
        ["MontoItem[5]"] = "16250.00",
        ["NumeroLinea[6]"] = "6",
        ["IndicadorFacturacion[6]"] = "2",
        ["NombreItem[6]"] = "ALAMBRE DULCE",
        ["IndicadorBienoServicio[6]"] = "1",
        ["CantidadItem[6]"] = "60.00",
        ["UnidadMedida[6]"] = "23",
        ["PrecioUnitarioItem[6]"] = "250.0000",
        ["MontoItem[6]"] = "15000.00",
        ["NumeroLinea[7]"] = "7",
        ["IndicadorFacturacion[7]"] = "2",
        ["NombreItem[7]"] = "CEMENTO BLANCO",
        ["IndicadorBienoServicio[7]"] = "1",
        ["CantidadItem[7]"] = "250.00",
        ["UnidadMedida[7]"] = "23",
        ["PrecioUnitarioItem[7]"] = "250.0000",
        ["MontoItem[7]"] = "62500.00",
        ["NumeroLinea[8]"] = "8",
        ["IndicadorFacturacion[8]"] = "2",
        ["NombreItem[8]"] = "CEMENTO GRIS",
        ["IndicadorBienoServicio[8]"] = "1",
        ["CantidadItem[8]"] = "400.00",
        ["UnidadMedida[8]"] = "23",
        ["PrecioUnitarioItem[8]"] = "250.0000",
        ["MontoItem[8]"] = "100000.00",
        ["NumeroLinea[9]"] = "9",
        ["IndicadorFacturacion[9]"] = "2",
        ["NombreItem[9]"] = "COLORANTE",
        ["IndicadorBienoServicio[9]"] = "1",
        ["CantidadItem[9]"] = "30.00",
        ["UnidadMedida[9]"] = "23",
        ["PrecioUnitarioItem[9]"] = "250.0000",
        ["MontoItem[9]"] = "7500.00",
        ["NumeroLinea[10]"] = "10",
        ["IndicadorFacturacion[10]"] = "2",
        ["NombreItem[10]"] = "TINER",
        ["IndicadorBienoServicio[10]"] = "1",
        ["CantidadItem[10]"] = "60.00",
        ["UnidadMedida[10]"] = "23",
        ["PrecioUnitarioItem[10]"] = "250.0000",
        ["MontoItem[10]"] = "15000.00",
        ["NumeroLineaDoR[1]"] = "1",
        ["TipoAjuste[1]"] = "R",
        ["DescripcionDescuentooRecargo[1]"] = "Pronto Pago",
        ["TipoValor[1]"] = "$",
        ["MontoDescuentooRecargo[1]"] = "3500.00",
        ["IndicadorFacturacionDescuentooRecargo[1]"] = "1",
        ["NumeroLineaDoR[2]"] = "2",
        ["TipoAjuste[2]"] = "R",
        ["DescripcionDescuentooRecargo[2]"] = "Pronto Pago",
        ["TipoValor[2]"] = "$",
        ["MontoDescuentooRecargo[2]"] = "2000.00",
        ["IndicadorFacturacionDescuentooRecargo[2]"] = "2",
    };

    [Fact]
    public void Mapper_ReadsGlobalDescuentosORecargosVerbatim()
    {
        var dto = new CertificationExcelMappingService().MapRowToRequest(Row(), 2);

        var ajustes = dto.ECF.DescuentosORecargos!.DescuentoORecargo;
        Assert.Equal(2, ajustes.Count);
        Assert.Equal("1", ajustes[0].NumeroLinea);
        Assert.Equal("R", ajustes[0].TipoAjuste);
        Assert.Equal("Pronto Pago", ajustes[0].DescripcionDescuentooRecargo);
        Assert.Equal("$", ajustes[0].TipoValor);
        Assert.Equal(3500.00m, ajustes[0].MontoDescuentooRecargo);
        Assert.Equal("1", ajustes[0].IndicadorFacturacionDescuentooRecargo);
        Assert.Equal(2000.00m, ajustes[1].MontoDescuentooRecargo);
        Assert.Equal("2", ajustes[1].IndicadorFacturacionDescuentooRecargo);
    }

    [Fact]
    public void Generator_EmitsDescuentosORecargosAndPassesXsd()
    {
        var dto = new CertificationExcelMappingService().MapRowToRequest(Row(), 2);
        var generator = new CertificationExcelGeneratorService();

        var xml = generator.GenerateUnsignedXml(dto);

        Assert.Contains("<DescuentosORecargos>", xml);
        Assert.Contains("<MontoDescuentooRecargo>3500.00</MontoDescuentooRecargo>", xml);
        Assert.Contains("<IndicadorFacturacionDescuentooRecargo>2</IndicadorFacturacionDescuentooRecargo>", xml);
        Assert.Contains("<MontoGravadoI1>282250.00</MontoGravadoI1>", xml);
        Assert.Empty(generator.ValidateXmlAgainstSchema(xml, 32));
    }
}
