using System.Globalization;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Elige, entre los TrackId que la DGII tiene para un eNCF, el que debe seguirse: prefiere el
/// aceptado (también el condicional), luego el que sigue en proceso y por último el rechazado; a
/// igual estado, el recibido más recientemente. Descarta «No encontrado» y los sin TrackId.
/// </summary>
public static class DgiiTrackIdPicker
{
    public static DgiiTrackIdEntry? Pick(IEnumerable<DgiiTrackIdEntry> entries, string? excludeTrackId = null) =>
        entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.TrackId))
            .Where(entry => !string.Equals(entry.TrackId, excludeTrackId, StringComparison.OrdinalIgnoreCase))
            .Where(entry => Normalize(entry.Estado) != "noencontrado")
            .Select((entry, index) => (Entry: entry, Index: index, Rank: Rank(entry.Estado), Date: ParseDate(entry.FechaRecepcion)))
            .OrderBy(item => item.Rank)
            .ThenByDescending(item => item.Date ?? DateTime.MinValue)
            .ThenByDescending(item => item.Index)
            .Select(item => item.Entry)
            .FirstOrDefault();

    private static int Rank(string? estado) => Normalize(estado) switch
    {
        "aceptado" or "aceptadocondicional" => 0,
        "enproceso" => 1,
        "rechazado" => 2,
        _ => 3
    };

    private static string Normalize(string? value) =>
        new string((value ?? string.Empty).Where(char.IsLetter).ToArray()).ToLowerInvariant();

    private static DateTime? ParseDate(string? value)
    {
        if (DateTime.TryParse(value, CultureInfo.GetCultureInfo("es-DO"), DateTimeStyles.None, out var local))
            return local;

        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var invariant)
            ? invariant
            : null;
    }
}
