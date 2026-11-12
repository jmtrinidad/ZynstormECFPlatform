using System.Xml.Linq;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// El XML resumen (RFCE) de una factura de consumo &lt; 250,000 no incluye
/// <c>FechaHoraFirma</c> (su esquema no lo permite); esa fecha solo existe en el XML completo
/// ("SUBIR_DGII") del mismo e-NCF. Este helper completa el RFCE con esa fecha, solo para
/// renderizar la representación impresa (el XML guardado no se modifica).
/// </summary>
public static class RfceSignatureDate
{
    public static string Complete(string xml, IEnumerable<string?> siblingXmls)
    {
        XDocument doc;
        try { doc = XDocument.Parse(xml); }
        catch { return xml; }

        if (doc.Root is null || !string.Equals(doc.Root.Name.LocalName, "RFCE", StringComparison.OrdinalIgnoreCase))
            return xml;
        if (!string.IsNullOrWhiteSpace(FirmaOf(doc))) return xml;

        foreach (var sibling in siblingXmls)
        {
            if (string.IsNullOrWhiteSpace(sibling)) continue;
            string? fecha;
            try { fecha = FirmaOf(XDocument.Parse(sibling)); }
            catch { continue; }
            if (string.IsNullOrWhiteSpace(fecha)) continue;

            doc.Root.Add(new XElement(doc.Root.Name.Namespace + "FechaHoraFirma", fecha));
            return doc.ToString(SaveOptions.DisableFormatting);
        }

        return xml;
    }

    private static string? FirmaOf(XDocument doc) =>
        doc.Descendants().FirstOrDefault(d => d.Name.LocalName == "FechaHoraFirma")?.Value.Trim();
}
