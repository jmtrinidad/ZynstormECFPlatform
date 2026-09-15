using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Dtos.Converters;

/// <summary>
/// Fechas calendario (sin hora): se leen y escriben como "yyyy-MM-dd", sin desplazamiento de zona.
/// Necesario porque Program.cs registra DrDateTimeConverter / DrNullableDateTimeConverter de forma
/// global, que aplican ToDrTime() (UTC-4) a todo DateTime y moverían un día hacia atrás una fecha
/// guardada a las 00:00. Un [JsonConverter] a nivel de propiedad tiene precedencia sobre esos
/// convertidores globales.
/// </summary>
public class CalendarDateJsonConverter : JsonConverter<DateTime?>
{
    private const string Format = "yyyy-MM-dd";

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        var text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.None).Date;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            writer.WriteStringValue(value.Value.ToString(Format, CultureInfo.InvariantCulture));
        else
            writer.WriteNullValue();
    }
}
