using System.Text.Json;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Certification;

namespace ZynstormECFPlatform.Tests.Certification;

public class RentaVehiculosGeneratorTests
{
    private static EcfInvoiceRequestDto Generate(string ecfType = "31") =>
        JsonSerializer.Deserialize<EcfInvoiceRequestDto>(
            SimulationSampleGenerator.GenerateJson("Renta de Vehículos", ecfType))!;

    [Fact]
    public void GenerateJson_BuildsRentalServiceItems()
    {
        var items = Generate().ECF.DetallesItems.Item;

        Assert.Equal(5, items.Count);
        Assert.Contains(items, i => i.NombreItem.StartsWith("Alquiler"));
        Assert.All(items, i =>
        {
            Assert.Equal("1", i.IndicadorFacturacion);      // gravado ITBIS 18%
            Assert.Equal("2", i.IndicadorBienoServicio);    // servicio
        });
    }

    [Fact]
    public void GenerateJson_TotalsMatchTheRentalItems()
    {
        var totals = Generate().ECF.Encabezado.Totales;

        Assert.Equal(7400m, totals.MontoGravadoTotal);
        Assert.Equal(1332.00m, totals.TotalITBIS);
        Assert.Equal(8732.00m, totals.MontoTotal);
    }

    [Fact]
    public void GenerateJson_DoesNotFallBackToGenericMerchandise()
    {
        var items = Generate().ECF.DetallesItems.Item;
        Assert.DoesNotContain(items, i => i.NombreItem == "Venta de Mercancía General");
    }
}
