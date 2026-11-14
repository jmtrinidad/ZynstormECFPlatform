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
/// Genera el XML real de las muestras de simulación sembradas, por el mismo camino de la
/// simulación (OldCertificationSimulationService + OldEcfGeneratorService), para poder revisar
/// los totales que la DGII valida.
/// </summary>
internal static class SeedSampleXml
{
    internal record Sample(int Id, int BusinessTypeId, string EcfType, string Json);

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

    internal static List<Sample> All() =>
        Model.FindEntityType(typeof(BusinessSimulationSample))!.GetSeedData()
            .Select(d => new Sample(
                (int)d["BusinessSimulationSampleId"]!,
                (int)d["BusinessTypeId"]!,
                (string)d["EcfType"]!,
                (string)d["JsonData"]!))
            .OrderBy(s => s.Id)
            .ToList();

    internal static Sample ById(int id) => All().Single(s => s.Id == id);

    internal static XElement GenerateTotales(Sample sample)
    {
        var dto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(
            sample.Json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        // Mismos valores que aplica la simulación antes de generar el XML.
        dto.IssuerRnc = "133009889";
        dto.IssuerName = "EMISOR DE PRUEBA SRL";
        dto.IssuerAddress = "CALLE PRINCIPAL #1";
        dto.IssuerPhone = "809-555-1234";
        dto.IssuerActivityCode = "6201";
        dto.EcfType = int.Parse(sample.EcfType);
        // La simulación siempre asigna el eNCF al reservar la secuencia; algunas muestras no lo traen.
        if (string.IsNullOrWhiteSpace(dto.Ncf)) dto.Ncf = $"E{sample.EcfType}0000000001";
        dto.IssueDate = new DateTime(2026, 11, 13);
        dto.SequenceExpirationDate = new DateTime(2028, 12, 31);
        dto.IncomeType ??= "01";
        dto.PaymentType ??= 1;
        if (dto.ReferenceNcf != null) dto.ReferenceIssueDate = new DateTime(2026, 11, 12);
        OldCertificationSimulationService.ApplyItemDefaults(dto.Items);

        var xml = new OldEcfGeneratorService().GenerateUnsignedXml(dto);
        return XDocument.Parse(xml).Descendants("Totales").First();
    }

    internal static decimal? Value(XElement totales, string name) =>
        decimal.TryParse(totales.Element(name)?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null;

    /// <summary>
    /// La DGII exige que TotalITBIS1 sea MontoGravadoI1 x 18% y que TotalITBIS exista y coincida.
    /// Si no hay monto gravado a la tasa 1, no hay ITBIS que cuadrar.
    /// </summary>
    internal static void AssertItbisConsistent(XElement totales)
    {
        var gravadoI1 = Value(totales, "MontoGravadoI1");
        if (gravadoI1 is null) return;

        var expected = Math.Round(gravadoI1.Value * 0.18m, 2);
        Assert.Equal(expected, Value(totales, "TotalITBIS1"));
        Assert.Equal(expected, Value(totales, "TotalITBIS"));
    }
}
