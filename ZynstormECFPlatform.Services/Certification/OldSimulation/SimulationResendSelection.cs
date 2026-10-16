using System.Globalization;
using System.Xml.Linq;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

/// <summary>
/// Grupos de la simulación (una tarjeta del Paso 4 = un grupo) y reglas puras para reenviar
/// un grupo o un comprobante sin reiniciar la simulación completa.
/// </summary>
public static class SimulationResendSelection
{
    public const string Over250k32 = "32-250K";
    public const string Rfce32 = "32-RFCE";
    public const string Manual32 = "32-MANUAL";

    private static readonly HashSet<string> ResendableGroups = new(StringComparer.Ordinal)
    {
        "31", "33", "34", Over250k32, "41", "43", "44", "45", "46", "47", Rfce32
    };

    public static bool IsResendableGroup(string? group) => group != null && ResendableGroups.Contains(group);

    public static string GroupOfRow(int type, bool isSummary, bool isManual) =>
        type == 32 ? (isManual ? Manual32 : isSummary ? Rfce32 : Over250k32) : type.ToString();

    public static string GroupOfDocument(string? ecfTypeCode, string? trackId, string? xmlSent)
    {
        if (ecfTypeCode != "32") return ecfTypeCode ?? string.Empty;
        if (trackId == "MANUAL") return Manual32;
        return RootName(xmlSent) == "RFCE" ? Rfce32 : Over250k32;
    }

    // Reenviar Consumo (Resumen) arrastra a los manuales: se generan con los RFCE aceptados.
    public static IReadOnlySet<string> GroupsReplacedBy(string group) =>
        group == Rfce32 ? new HashSet<string> { Rfce32, Manual32 } : new HashSet<string> { group };

    /// <summary>null = simulación completa: corre todos los renglones de la matriz.</summary>
    public static bool RunsRow(string? selectedGroup, string rowGroup) =>
        selectedGroup == null || GroupsReplacedBy(selectedGroup).Contains(rowGroup);

    public static void AddCount(SimulationStatsDto stats, string group)
    {
        switch (group)
        {
            case "31": stats.Type31++; break;
            case "33": stats.Type33++; break;
            case "34": stats.Type34++; break;
            case Over250k32: stats.Type32Greater250k++; break;
            case Rfce32: stats.Type32Rfce++; break;
            case Manual32: stats.Type32Manual++; break;
            case "41": stats.Type41++; break;
            case "43": stats.Type43++; break;
            case "44": stats.Type44++; break;
            case "45": stats.Type45++; break;
            case "46": stats.Type46++; break;
            case "47": stats.Type47++; break;
        }
    }

    /// <summary>Datos de una factura 31 aceptada que necesita una nota 33/34 para referenciarla.</summary>
    public static Accepted31Reference? ParseAccepted31(string? xmlSent)
    {
        if (string.IsNullOrWhiteSpace(xmlSent)) return null;
        try
        {
            var doc = XDocument.Parse(xmlSent);
            string? Value(string name) => doc.Descendants().FirstOrDefault(e => e.Name.LocalName == name)?.Value;

            var ncf = Value("eNCF");
            if (string.IsNullOrWhiteSpace(ncf)) return null;
            if (!DateTime.TryParseExact(Value("FechaEmision"), "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var issueDate))
                return null;

            var firstPrice = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Item")?
                .Elements().FirstOrDefault(e => e.Name.LocalName == "PrecioUnitarioItem")?.Value;
            decimal? price = decimal.TryParse(firstPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) ? p : null;

            return new Accepted31Reference(ncf, issueDate, Value("RNCComprador"), Value("RazonSocialComprador"), price);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }

    private static string? RootName(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;
        try { return XDocument.Parse(xml).Root?.Name.LocalName; }
        catch (System.Xml.XmlException) { return null; }
    }
}

public sealed record Accepted31Reference(string Ncf, DateTime IssueDate, string? CustomerRnc, string? CustomerName, decimal? FirstItemUnitPrice);
