using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Tests.Certification;

/// <summary>
/// La DGII rechazó las muestras de Renta con "TotalITBIS no es válido" y "TotalITBIS1 no coincide
/// con MontoGravado1 x ITBIS tasa 1". Estas pruebas generan el XML real de cada muestra por el
/// mismo camino de la simulación y verifican la coherencia de los totales de ITBIS.
/// </summary>
public class RentaVehiculosXmlTotalsTests
{
    private const int RentaId = 15;

    private static readonly IModel Model = BuildModel();

    private static IModel BuildModel()
    {
        var options = new DbContextOptionsBuilder<StorageContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=x;Password=x")
            .Options;
        using var context = new StorageContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static string SampleJson(string ecfType) =>
        (string)Model.FindEntityType(typeof(BusinessSimulationSample))!.GetSeedData()
            .Single(d => (int)d["BusinessTypeId"]! == RentaId && (string)d["EcfType"]! == ecfType)["JsonData"]!;

    private static XElement GenerateTotales(string ecfType)
    {
        var dto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(
            SampleJson(ecfType), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // Mismos valores que aplica la simulación antes de generar el XML.
        dto.IssuerRnc = "133009889";
        dto.IssuerName = "EMISOR DE PRUEBA SRL";
        dto.IssuerAddress = "CALLE PRINCIPAL #1";
        dto.IssuerPhone = "809-555-1234";
        dto.IssuerActivityCode = "6201";
        dto.EcfType = int.Parse(ecfType);
        dto.IssueDate = new DateTime(2026, 11, 13);
        dto.SequenceExpirationDate = new DateTime(2028, 12, 31);
        dto.IncomeType ??= "01";
        dto.PaymentType ??= 1;
        if (dto.ReferenceNcf != null) dto.ReferenceIssueDate = new DateTime(2026, 11, 12);
        OldCertificationSimulationService.ApplyItemDefaults(dto.Items);

        var xml = new OldEcfGeneratorService().GenerateUnsignedXml(dto);
        return XDocument.Parse(xml).Descendants("Totales").First();
    }

    private static decimal? Value(XElement totales, string name) =>
        decimal.TryParse(totales.Element(name)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    [Theory]
    [InlineData("31")]
    [InlineData("32")]
    [InlineData("33")]
    [InlineData("34")]
    [InlineData("41")]
    [InlineData("43")]
    [InlineData("44")]
    [InlineData("45")]
    [InlineData("46")]
    [InlineData("47")]
    public void Totales_ItbisIsConsistentWithTheTaxableAmount(string ecfType)
    {
        AssertItbisConsistent(ecfType);
    }

    // Las muestras exentas no deben declarar monto gravado a la tasa 1 y sí su monto exento,
    // como las muestras de Transporte y Clínica que la DGII acepta.
    [Theory]
    [InlineData("31")]
    [InlineData("32")]
    [InlineData("33")]
    [InlineData("34")]
    [InlineData("41")]
    [InlineData("43")]
    [InlineData("44")]
    [InlineData("45")]
    [InlineData("47")]
    public void Totales_ExemptSamplesDeclareExemptAmountOnly(string ecfType)
    {
        var totales = GenerateTotales(ecfType);

        Assert.Null(Value(totales, "MontoGravadoI1"));
        Assert.True(Value(totales, "MontoExento") > 0);
    }

    private static void AssertItbisConsistent(string ecfType)
    {
        var totales = GenerateTotales(ecfType);
        var gravadoI1 = Value(totales, "MontoGravadoI1");

        if (gravadoI1 is null)
            return; // Sin monto gravado a la tasa 1 no hay ITBIS que cuadrar.

        var expected = Math.Round(gravadoI1.Value * 0.18m, 2);
        Assert.Equal(expected, Value(totales, "TotalITBIS1"));
        Assert.Equal(expected, Value(totales, "TotalITBIS"));
    }
}
