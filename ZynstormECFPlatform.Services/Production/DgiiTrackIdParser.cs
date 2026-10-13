using System.Text.Json;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>Lee la respuesta de la Consulta de TrackId: una lista, o un solo objeto (así lo muestra la guía de la DGII).</summary>
public static class DgiiTrackIdParser
{
    public static List<DgiiTrackIdEntry> Parse(string? body)
    {
        var entries = new List<DgiiTrackIdEntry>();
        if (string.IsNullOrWhiteSpace(body))
            return entries;

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                AddEntry(item, entries);
        }
        else
        {
            AddEntry(root, entries);
        }

        return entries;
    }

    private static void AddEntry(JsonElement element, List<DgiiTrackIdEntry> entries)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return;

        var trackId = Read(element, "trackId");
        if (string.IsNullOrWhiteSpace(trackId))
            return;

        entries.Add(new DgiiTrackIdEntry
        {
            TrackId = trackId,
            Estado = Read(element, "estado"),
            FechaRecepcion = Read(element, "fechaRecepcion")
        });
    }

    private static string? Read(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;

            return property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Null => null,
                _ => property.Value.ToString()
            };
        }

        return null;
    }
}
