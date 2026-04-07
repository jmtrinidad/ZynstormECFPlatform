using System.Text.Json;
using System.Text.Json.Serialization;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Web.Api.Converters;

public class DrDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTime.Parse(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Convert to Dominican Republic time before writing to JSON
        writer.WriteStringValue(value.ToDrTime().ToString("yyyy-MM-ddTHH:mm:ss"));
    }
}

public class DrNullableDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (string.IsNullOrEmpty(s)) return null;
        return DateTime.Parse(s);
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToDrTime().ToString("yyyy-MM-ddTHH:mm:ss"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
