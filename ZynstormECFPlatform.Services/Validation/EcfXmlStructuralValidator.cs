using System.Xml;

namespace ZynstormECFPlatform.Services.Validation;

/// <summary>
/// Layer 1: Validates that the XML is well-formed and has the required structural elements
/// for any e-CF document before deeper validation can proceed.
/// </summary>
internal static class EcfXmlStructuralValidator
{
    private static readonly HashSet<int> ValidEcfTypes = [31, 32, 33, 34, 41, 43, 44, 45, 46, 47];

    /// <summary>
    /// Validates XML structure. Returns (errors, parsedDoc, ecfType, eNcf).
    /// If errors exist, parsedDoc may be null.
    /// </summary>
    public static (List<string> Errors, XmlDocument? Doc, int EcfType, string ENcf) Validate(string xml)
    {
        var errors = new List<string>();
        XmlDocument? doc = null;
        int ecfType = 0;
        string eNcf = string.Empty;

        // 1. Well-formed XML
        if (string.IsNullOrWhiteSpace(xml))
        {
            errors.Add("El contenido XML está vacío.");
            return (errors, null, 0, string.Empty);
        }

        try
        {
            doc = new XmlDocument();
            doc.LoadXml(xml);
        }
        catch (XmlException ex)
        {
            errors.Add($"El XML no está bien formado: {ex.Message}");
            return (errors, null, 0, string.Empty);
        }

        // 2. Root element must be <ECF>
        if (doc.DocumentElement == null)
        {
            errors.Add("El documento XML no tiene elemento raíz.");
            return (errors, null, 0, string.Empty);
        }

        var rootName = doc.DocumentElement.LocalName;
        if (!rootName.Equals("ECF", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"El elemento raíz debe ser <ECF>, pero se encontró <{rootName}>.");
        }

        // 3. Encabezado
        var encabezado = GetChild(doc.DocumentElement, "Encabezado");
        if (encabezado == null)
        {
            errors.Add("Falta el nodo obligatorio <Encabezado>.");
            return (errors, doc, 0, string.Empty);
        }

        // 4. IdDoc
        var idDoc = GetChild(encabezado, "IdDoc");
        if (idDoc == null)
        {
            errors.Add("Falta el nodo obligatorio <Encabezado><IdDoc>.");
            return (errors, doc, 0, string.Empty);
        }

        // 5. TipoeCF
        var tipoeCFNode = GetChild(idDoc, "TipoeCF");
        if (tipoeCFNode == null || string.IsNullOrWhiteSpace(tipoeCFNode.InnerText))
        {
            errors.Add("Falta el campo obligatorio <TipoeCF> dentro de <IdDoc>.");
        }
        else if (!int.TryParse(tipoeCFNode.InnerText.Trim(), out ecfType) || !ValidEcfTypes.Contains(ecfType))
        {
            errors.Add($"El TipoeCF '{tipoeCFNode.InnerText}' no es un valor válido. Valores permitidos: {string.Join(", ", ValidEcfTypes.Order())}.");
        }

        // 6. eNCF
        var eNcfNode = GetChild(idDoc, "eNCF");
        if (eNcfNode == null || string.IsNullOrWhiteSpace(eNcfNode.InnerText))
        {
            errors.Add("Falta el campo obligatorio <eNCF> dentro de <IdDoc>.");
        }
        else
        {
            eNcf = eNcfNode.InnerText.Trim();
            if (eNcf.Length != 13)
            {
                errors.Add($"El eNCF '{eNcf}' debe tener exactamente 13 caracteres (E + 2 dígitos tipo + 10 dígitos secuencia).");
            }
            else
            {
                // Validate eNCF type matches TipoeCF
                if (ecfType > 0 && eNcf.Length >= 3)
                {
                    var ncfTypeStr = eNcf.Substring(1, 2);
                    if (int.TryParse(ncfTypeStr, out int ncfType) && ncfType != ecfType)
                    {
                        errors.Add($"El tipo en el eNCF ({ncfType}) no coincide con el TipoeCF declarado ({ecfType}).");
                    }
                }
            }
        }

        // 7. Emisor
        var emisor = GetChild(encabezado, "Emisor");
        if (emisor == null)
        {
            errors.Add("Falta el nodo obligatorio <Emisor>.");
        }
        else
        {
            if (GetChildText(emisor, "RNCEmisor") == null)
                errors.Add("Falta el campo obligatorio <RNCEmisor> dentro de <Emisor>.");
            if (GetChildText(emisor, "RazonSocialEmisor") == null)
                errors.Add("Falta el campo obligatorio <RazonSocialEmisor> dentro de <Emisor>.");
            if (GetChildText(emisor, "FechaEmision") == null)
                errors.Add("Falta el campo obligatorio <FechaEmision> dentro de <Emisor>.");
        }

        // 8. DetallesItems
        var detalles = GetChild(doc.DocumentElement, "DetallesItems");
        if (detalles == null)
        {
            errors.Add("Falta el nodo obligatorio <DetallesItems>.");
        }
        else
        {
            var items = detalles.SelectNodes("Item");
            if (items == null || items.Count == 0)
            {
                errors.Add("El nodo <DetallesItems> debe contener al menos un <Item>.");
            }
            else
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i] as XmlElement;
                    if (item == null) continue;
                    int lineNum = i + 1;

                    if (GetChildText(item, "NumeroLinea") == null)
                        errors.Add($"Item #{lineNum}: Falta <NumeroLinea>.");
                    if (GetChildText(item, "IndicadorFacturacion") == null)
                        errors.Add($"Item #{lineNum}: Falta <IndicadorFacturacion>.");
                    if (GetChildText(item, "NombreItem") == null)
                        errors.Add($"Item #{lineNum}: Falta <NombreItem>.");
                    if (GetChildText(item, "CantidadItem") == null)
                        errors.Add($"Item #{lineNum}: Falta <CantidadItem>.");
                    if (GetChildText(item, "PrecioUnitarioItem") == null)
                        errors.Add($"Item #{lineNum}: Falta <PrecioUnitarioItem>.");
                    if (GetChildText(item, "MontoItem") == null)
                        errors.Add($"Item #{lineNum}: Falta <MontoItem>.");
                }
            }
        }

        // 9. Totales
        var totales = GetChild(encabezado, "Totales");
        if (totales == null)
        {
            errors.Add("Falta el nodo obligatorio <Totales> dentro de <Encabezado>.");
        }
        else
        {
            if (GetChildText(totales, "MontoTotal") == null)
                errors.Add("Falta el campo obligatorio <MontoTotal> dentro de <Totales>.");
        }

        return (errors, doc, ecfType, eNcf);
    }

    // ── Helpers ──

    internal static XmlElement? GetChild(XmlElement parent, string localName)
    {
        foreach (XmlNode child in parent.ChildNodes)
        {
            if (child is XmlElement el && el.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))
                return el;
        }
        return null;
    }

    internal static string? GetChildText(XmlElement parent, string localName)
    {
        var child = GetChild(parent, localName);
        return child != null && !string.IsNullOrWhiteSpace(child.InnerText) ? child.InnerText.Trim() : null;
    }

    internal static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0m;
        return decimal.TryParse(value.Trim(), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0m;
    }
}
