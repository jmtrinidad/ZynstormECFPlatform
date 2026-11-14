using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;

namespace ZynstormECFPlatform.Tests.Certification;

public class RentaVehiculosSeedTests
{
    private const int RentaId = 15;
    private const int ClinicaId = 14;

    private static readonly string[] EcfTypes = ["31", "32", "33", "34", "41", "43", "44", "45", "46", "47"];

    // Construir el modelo no abre conexión: la cadena solo selecciona el proveedor Npgsql.
    private static readonly IModel Model = BuildModel();

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<StorageContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x")
            .Options;
        using var context = new StorageContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static List<IDictionary<string, object?>> AllSamples() =>
        Model.FindEntityType(typeof(BusinessSimulationSample))!.GetSeedData().ToList();

    private static List<IDictionary<string, object?>> SamplesOf(int businessTypeId) =>
        AllSamples().Where(d => (int)d["BusinessTypeId"]! == businessTypeId).ToList();

    private static IDictionary<string, object?> RentaSample(string ecfType) =>
        SamplesOf(RentaId).Single(d => (string)d["EcfType"]! == ecfType);

    private record Item(string Name, decimal Quantity, decimal UnitPrice, int BillingIndicator, int ItemType);

    private static List<Item> ItemsOf(string ecfType)
    {
        using var doc = JsonDocument.Parse((string)RentaSample(ecfType)["JsonData"]!);
        return doc.RootElement.GetProperty("items").EnumerateArray()
            .Select(e => new Item(
                e.GetProperty("name").GetString()!,
                e.GetProperty("quantity").GetDecimal(),
                e.GetProperty("unitPrice").GetDecimal(),
                e.GetProperty("billingIndicator").GetInt32(),
                e.GetProperty("itemType").GetInt32()))
            .ToList();
    }

    [Fact]
    public void BusinessType15_IsRentaDeVehiculos()
    {
        var type = Model.FindEntityType(typeof(BusinessType))!.GetSeedData()
            .Single(d => (int)d["BusinessTypeId"]! == RentaId);

        Assert.Equal("Renta de Vehículos", type["Name"]);
        Assert.Equal("1d2e3f4a-5b6c-7d8e-9f0a-1b2c3d4e5f6a", type["GuidId"]);
        Assert.Equal("Alquiler de vehículos, motos y buggies por día.", type["Description"]);
    }

    [Fact]
    public void Renta_HasOneSamplePerEcfType_WithConsecutiveIds()
    {
        var samples = SamplesOf(RentaId);

        Assert.Equal(EcfTypes.OrderBy(t => t), samples.Select(s => (string)s["EcfType"]!).OrderBy(t => t));
        Assert.Equal(Enumerable.Range(142, 10), samples.Select(s => (int)s["BusinessSimulationSampleId"]!).OrderBy(i => i));
        Assert.All(samples, s =>
        {
            var ecf = (string)s["EcfType"]!;
            Assert.Equal($"Renta de Vehículos - Tipo {ecf}", s["Name"]);
            Assert.Equal($"00000000-0000-0000-0015-0000000000{ecf}", s["GuidId"]);
        });
    }

    [Fact]
    public void Clinica_KeepsItsSampleIds()
    {
        var ids = SamplesOf(ClinicaId).Select(s => (int)s["BusinessSimulationSampleId"]!).OrderBy(i => i);
        Assert.Equal(Enumerable.Range(132, 10), ids);
    }

    [Fact]
    public void AllSampleGuidIds_AreUnique()
    {
        var guids = AllSamples().Select(s => (string)s["GuidId"]!).ToList();
        Assert.Equal(guids.Count, guids.Distinct().Count());
    }

    [Theory]
    [InlineData("31", "ALQUILER KIA PICANTO 2025 (DIA)")]
    [InlineData("32", "ALQUILER BUGGY CF MOTO UFORCE 600 (DIA)")]
    [InlineData("33", "AJUSTE POR DIAS DE ALQUILER")]
    [InlineData("34", "CARGO ADICIONAL POR KILOMETRAJE Y COMBUSTIBLE")]
    [InlineData("41", "MANTENIMIENTO Y LAVADO DE FLOTA")]
    [InlineData("43", "REEMBOLSO CAJA CHICA COMBUSTIBLE")]
    [InlineData("44", "ALQUILER SCOOTER TAURO DIO 110 (DIA)")]
    [InlineData("45", "ALQUILER SCOOTER SYM FNX 200CC (DIA)")]
    [InlineData("46", "ALQUILER DE VEHICULO A TURISTA EXTRANJERO")]
    [InlineData("47", "ALQUILER DE VEHICULO EN EL EXTERIOR")]
    public void MainItem_IsThemedRentalService(string ecfType, string expectedName)
    {
        var main = ItemsOf(ecfType)[0];
        Assert.Equal(expectedName, main.Name);
        Assert.Equal(2, main.ItemType);
    }

    [Theory]
    [InlineData("31", 4)]
    [InlineData("32", 3)]
    [InlineData("33", 1)]
    [InlineData("34", 1)]
    [InlineData("41", 1)]
    [InlineData("43", 1)]
    [InlineData("44", 3)]
    [InlineData("45", 3)]
    [InlineData("46", 1)]
    [InlineData("47", 1)]
    public void ItemCount_MatchesSpec(string ecfType, int expectedCount)
    {
        Assert.Equal(expectedCount, ItemsOf(ecfType).Count);
    }

    // Igual que las muestras que la DGII ya acepta (Transporte, Clínica, etc.): exentas (4) y la
    // de exportación (46) en 3. El simulador no rellena TaxPercentage, así que un ítem gravado (1)
    // produce TotalITBIS1 = 0 y la DGII rechaza el comprobante.
    [Theory]
    [InlineData("31", 4)]
    [InlineData("32", 4)]
    [InlineData("33", 4)]
    [InlineData("34", 4)]
    [InlineData("41", 4)]
    [InlineData("43", 4)]
    [InlineData("44", 4)]
    [InlineData("45", 4)]
    [InlineData("46", 3)]
    [InlineData("47", 4)]
    public void BillingIndicator_MatchesTheDgiiAcceptedSamples(string ecfType, int expectedIndicator)
    {
        Assert.All(ItemsOf(ecfType), i => Assert.Equal(expectedIndicator, i.BillingIndicator));
    }

    [Fact]
    public void Type31_HasExtraRentalItemsWithReferencePrices()
    {
        var items = ItemsOf("31");

        Assert.Equal(
            new (string, decimal)[]
            {
                ("ALQUILER KIA PICANTO 2025 (DIA)", 244.00m),
                ("ALQUILER NISSAN SENTRA SV (DIA)", 2100.00m),
                ("ALQUILER SCOOTER CFMOTO 150CC (DIA)", 900.00m),
                ("ENTREGA Y RECOGIDA A DOMICILIO", 500.00m),
            },
            items.Select(i => (i.Name, i.UnitPrice)).ToArray());
        Assert.All(items, i => Assert.Equal(2, i.ItemType));
    }

    [Fact]
    public void Type31_ItemsAreExemptServices()
    {
        Assert.All(ItemsOf("31"), i => Assert.Equal(4, i.BillingIndicator));
    }

    [Fact]
    public void Type44_ExtraItemsAreExemptServices()
    {
        var items = ItemsOf("44");

        Assert.Equal(
            new (string, decimal)[]
            {
                ("ALQUILER SCOOTER TAURO DIO 110 (DIA)", 29.50m),
                ("ALQUILER SYM FNX 200CC (DIA)", 900.00m),
                ("ENTREGA Y RECOGIDA A DOMICILIO", 500.00m),
            },
            items.Select(i => (i.Name, i.UnitPrice)).ToArray());
        Assert.All(items, i =>
        {
            Assert.Equal(4, i.BillingIndicator);
            Assert.Equal(2, i.ItemType);
        });
    }
}
