using System.Text.Json;
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Dtos.Converters;

namespace ZynstormECFPlatform.Tests.Billing;

/// <summary>
/// Program.cs registra convertidores globales que aplican ToDrTime() (UTC-4) a todo DateTime.
/// Estas pruebas fijan que las fechas de renta salgan como fecha calendario sin corrimiento,
/// incluso con un convertidor global que sí desplazaría el día.
/// </summary>
public class CalendarDateJsonConverterTests
{
    /// <summary>Imita a DrNullableDateTimeConverter: resta 4 horas y escribe con hora.</summary>
    private class ShiftingDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null : DateTime.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.AddHours(-4).ToString("yyyy-MM-ddTHH:mm:ss"));
            else
                writer.WriteNullValue();
        }
    }

    private static JsonSerializerOptions OptionsWithGlobalShiftingConverter()
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        options.Converters.Add(new ShiftingDateTimeConverter());
        return options;
    }

    [Fact]
    public void ClientRentDto_RentDates_AreNotShiftedByTheGlobalConverter()
    {
        var dto = new ClientRentDto
        {
            LastRentPaymentDate = new DateTime(2026, 9, 1),
            NextRentPaymentDate = new DateTime(2027, 9, 1)
        };

        var json = JsonSerializer.Serialize(dto, OptionsWithGlobalShiftingConverter());

        Assert.Contains("\"lastRentPaymentDate\":\"2026-09-01\"", json);
        Assert.Contains("\"nextRentPaymentDate\":\"2027-09-01\"", json);
        // El día anterior es exactamente el síntoma que produciría el convertidor global.
        Assert.DoesNotContain("2026-08-31", json);
    }

    [Fact]
    public void ClientCreateDto_RentDates_AreNotShiftedByTheGlobalConverter()
    {
        var dto = new ClientCreateDto
        {
            Name = "Cliente",
            Rnc = "101010101",
            LastRentPaymentDate = new DateTime(2026, 1, 1),
            NextRentPaymentDate = new DateTime(2026, 2, 1)
        };

        var json = JsonSerializer.Serialize(dto, OptionsWithGlobalShiftingConverter());

        Assert.Contains("\"lastRentPaymentDate\":\"2026-01-01\"", json);
        Assert.Contains("\"nextRentPaymentDate\":\"2026-02-01\"", json);
        Assert.DoesNotContain("2025-12-31", json);
    }

    [Fact]
    public void ClientCreateDto_RentDates_RoundTrip()
    {
        var json = """
            {"name":"Cliente","rnc":"101010101","lastRentPaymentDate":"2026-09-01","nextRentPaymentDate":"2027-09-01"}
            """;

        var dto = JsonSerializer.Deserialize<ClientCreateDto>(json, OptionsWithGlobalShiftingConverter())!;

        Assert.Equal(new DateTime(2026, 9, 1), dto.LastRentPaymentDate);
        Assert.Equal(new DateTime(2027, 9, 1), dto.NextRentPaymentDate);
    }

    [Fact]
    public void NullDates_SerializeAsNull()
    {
        var json = JsonSerializer.Serialize(new ClientRentDto(), OptionsWithGlobalShiftingConverter());

        Assert.Contains("\"lastRentPaymentDate\":null", json);
        Assert.Contains("\"nextRentPaymentDate\":null", json);
    }

    [Fact]
    public void Converter_ReadsDateTimeWithTimePart_AsDateOnly()
    {
        var converter = new CalendarDateJsonConverter();
        var json = "\"2026-09-01T13:45:00\"";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read();

        var value = converter.Read(ref reader, typeof(DateTime?), JsonSerializerOptions.Default);

        Assert.Equal(new DateTime(2026, 9, 1), value);
    }
}
