using System.Globalization;
using System.Net;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri.Html;

/// <summary>
/// Rellena un formato HTML de RI (marcadores {{...}}) con los datos de un e-CF firmado.
/// Camino aditivo: no toca las plantillas QuestPDF.
/// </summary>
public static class RiHtmlRenderer
{
    private static readonly CultureInfo Culture = new("es-DO");

    private static readonly HashSet<string> InlineTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "span", "b", "strong", "i", "em", "u", "a", "small", "font", "sup", "sub", "label"
    };

    private const string EncfBlock = """
<div class="ri-encf" style="display:flex;justify-content:space-between"><span>eNCF:</span><span>{{eNCF}}</span></div>
<div class="ri-valido" style="display:flex;justify-content:space-between"><span>VALIDO HASTA:</span><span>{{FechaVencimiento}}</span></div>
""";

    private const string QrBlock = """
<div class="ri-qr" style="text-align:center;margin-top:12px">
<div style="width:110px;height:110px;margin:0 auto">{{QR}}</div>
<div><b>Codigo seguridad:</b> {{CodigoSeguridad}}</div>
<div><b>Fecha firma digital:</b> {{FechaFirma}}</div>
<div><b>REPRESENTACION IMPRESA DEL e-CF</b></div>
</div>
""";

    public static string Render(string templateHtml, string signedXml, RiCompanyHeader? company, string? sampleDataJson)
    {
        var model = EcfRiTemplateMapper.MapInvoice(signedXml, DgiiEnvironment.CerteCF);
        if (company is not null)
        {
            model.Company = new RiInvoiceCompany
            {
                Name = company.Name,
                Rnc = company.Rnc,
                Address = company.Address,
                Phone = company.Phone,
                Whatsapp = company.Whatsapp,
            };
        }

        var values = BuildValues(model, ParseSamples(sampleDataJson));

        var document = new HtmlParser().ParseDocument(templateHtml);
        var body = document.Body ?? throw new InvalidOperationException("El formato HTML no tiene <body>.");

        EnsureCharset(document);
        InjectMissingBlocks(document, body);
        ExpandItemRows(document, body, model.Items);
        RemoveEmptyLines(body, values);
        SubstituteGlobalMarkers(document, body, values);

        return "<!DOCTYPE html>" + document.DocumentElement.OuterHtml;
    }

    // ---------------------------------------------------------------- values

    private static Dictionary<string, string> ParseSamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static Dictionary<string, string> BuildValues(RiInvoiceModel model, Dictionary<string, string> samples)
    {
        string Sample(string key) => samples.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;

        var hideValidUntil = model.EcfType is 32 or 34;
        var logo = Sample("EmisorLogo");

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["EmisorLogo"] = logo.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) ? logo : string.Empty,
            ["EmisorNombre"] = model.Company.Name,
            ["EmisorDireccion"] = model.Company.Address,
            ["EmisorRNC"] = model.Company.Rnc,
            ["EmisorTelefono"] = model.Company.Phone,
            ["TipoComprobante"] = model.NcfTypeName,
            ["eNCF"] = model.NcfNumber,
            ["FechaVencimiento"] = hideValidUntil ? string.Empty : model.ValidUntil,
            ["NoFactura"] = model.InternalInvoiceNumber,
            ["Fecha"] = model.FechaEmision,
            ["FormaPago"] = model.PaymentType,
            ["Nombre"] = model.Client.Name,
            ["QR"] = QrDataUri(model.Qr),
            ["CodigoSeguridad"] = string.IsNullOrEmpty(model.SecurityCode) ? "N/D" : model.SecurityCode,
            ["FechaFirma"] = string.IsNullOrEmpty(model.FechaFirma) ? "N/D" : model.FechaFirma,
            ["Subtotal"] = Money(model.SubTotal),
            ["Itbis"] = Money(model.Itbis),
            ["Total"] = Money(model.Total),
            ["TotalDescuento"] = Money(model.Discount),
            // El XML tiene prioridad sobre la muestra (la cédula del comprador).
            ["Cedula"] = string.IsNullOrEmpty(model.Client.Rnc) ? Sample("Cedula") : model.Client.Rnc,
        };

        foreach (var key in RiHtmlMarkers.SampleMarkers.Where(k => k != "Cedula"))
        {
            values[key] = Sample(key);
        }

        return values;
    }

    private static Dictionary<string, string> ItemValues(RiInvoiceItem item) => new(StringComparer.Ordinal)
    {
        ["Descripcion"] = item.Description,
        ["Cantidad"] = item.Quantity.ToString("0.##", CultureInfo.InvariantCulture),
        ["Unidad"] = item.Unit,
        ["Precio"] = Money(item.Price),
        ["ItemItbis"] = Money(item.Itbis),
        ["Monto"] = Money(item.Amount),
    };

    private static string Money(decimal value) => string.Format(Culture, "{0:C2}", value);

    private static string QrDataUri(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        try
        {
            using var generator = new QRCoder.QRCodeGenerator();
            using var data = generator.CreateQrCode(url, QRCoder.QRCodeGenerator.ECCLevel.M);
            using var png = new QRCoder.PngByteQRCode(data);
            return "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(5));
        }
        catch
        {
            return string.Empty;
        }
    }

    // ------------------------------------------------------------- DOM utils

    private static void EnsureCharset(IDocument document)
    {
        if (document.QuerySelector("meta[charset]") is not null || document.Head is null)
        {
            return;
        }

        var meta = document.CreateElement("meta");
        meta.SetAttribute("charset", "utf-8");
        document.Head.InsertBefore(meta, document.Head.FirstChild);
    }

    private static List<IText> TextNodesWithMarkers(IElement root, Func<string, bool> markerFilter) =>
        root.Descendants<IText>()
            .Where(t => RiHtmlMarkers.Extract(t.Data).Any(markerFilter))
            .ToList();

    private static bool HasStaticText(IElement element) =>
        element.Descendants<IText>()
            .Any(t => !string.IsNullOrWhiteSpace(RiHtmlMarkers.Pattern.Replace(t.Data, string.Empty)));

    private static IElement LowestBlock(IElement element, IElement body)
    {
        var current = element;
        while (current != body && InlineTags.Contains(current.LocalName) && current.ParentElement is not null)
        {
            current = current.ParentElement;
        }

        return current;
    }

    private static void InsertHtml(IDocument document, string html, IElement parent, INode? before)
    {
        var holder = document.CreateElement("div");
        holder.InnerHtml = html;
        foreach (var child in holder.ChildNodes.ToList())
        {
            parent.InsertBefore(child, before);
        }
    }

    // -------------------------------------------------------- injected blocks

    private static void InjectMissingBlocks(IDocument document, IElement body)
    {
        var markers = RiHtmlMarkers.Extract(document.DocumentElement.OuterHtml).ToHashSet(StringComparer.Ordinal);

        if (!markers.Contains("eNCF"))
        {
            var anchor = TextNodesWithMarkers(body, m => m == "TipoComprobante").FirstOrDefault()?.ParentElement
                         ?? body.QuerySelector("h1, h2, h3");
            anchor = anchor is null ? null : LowestBlock(anchor, body);

            if (anchor is null || anchor == body)
            {
                InsertHtml(document, EncfBlock, body, body.FirstChild);
            }
            else
            {
                InsertHtml(document, EncfBlock, anchor.ParentElement!, anchor.NextSibling);
            }
        }

        if (!markers.Contains("QR"))
        {
            InsertHtml(document, QrBlock, body, null);
        }
    }

    // ------------------------------------------------------------- item rows

    private static void ExpandItemRows(IDocument document, IElement body, IReadOnlyList<RiInvoiceItem> items)
    {
        var itemNodes = TextNodesWithMarkers(body, RiHtmlMarkers.ItemMarkers.Contains);
        if (itemNodes.Count == 0)
        {
            return;
        }

        var row = FindItemRow(itemNodes, body);

        foreach (var item in items)
        {
            var clone = (IElement)row.Clone(true);
            var values = ItemValues(item);
            foreach (var node in TextNodesWithMarkers(clone, values.ContainsKey))
            {
                ReplaceTextNode(document, node, name => values.TryGetValue(name, out var v) ? WebUtility.HtmlEncode(v) : null);
            }

            row.ParentElement!.InsertBefore(clone, row);
        }

        row.Remove();
    }

    private static IElement FindItemRow(List<IText> itemNodes, IElement body)
    {
        var common = itemNodes[0].ParentElement!;
        foreach (var node in itemNodes.Skip(1))
        {
            // Subir el ancestro común hasta que contenga también a este nodo.
            while (common != body && !common.Contains(node))
            {
                common = common.ParentElement ?? body;
            }
        }

        var row = LowestBlock(common, body);
        if (row.Closest("tr") is { } tr && body.Contains(tr))
        {
            row = tr;
        }

        // Si los marcadores están dispersos y el ancestro común es el body, repetir solo el bloque del primero.
        return row == body ? LowestBlock(itemNodes[0].ParentElement!, body) : row;
    }

    // ------------------------------------------------------------ empty lines

    private static void RemoveEmptyLines(IElement body, Dictionary<string, string> values)
    {
        bool IsEmpty(string marker) => values.TryGetValue(marker, out var v) && string.IsNullOrEmpty(v);

        bool AllKnownMarkersEmpty(IElement element)
        {
            var known = element.Descendants<IText>()
                .SelectMany(t => RiHtmlMarkers.Extract(t.Data))
                .Where(values.ContainsKey)
                .ToList();
            return known.Count > 0 && known.All(IsEmpty);
        }

        var targets = new HashSet<IElement>();

        foreach (var node in TextNodesWithMarkers(body, values.ContainsKey))
        {
            var nodeMarkers = RiHtmlMarkers.Extract(node.Data).Where(values.ContainsKey).ToList();
            if (nodeMarkers.Count == 0 || !nodeMarkers.All(IsEmpty))
            {
                continue;
            }

            var block = LowestBlock(node.ParentElement!, body);
            var candidate = block;
            while (candidate != body && !HasStaticText(candidate) && candidate.ParentElement is not null)
            {
                candidate = candidate.ParentElement;
            }

            if (candidate != body && AllKnownMarkersEmpty(candidate))
            {
                targets.Add(candidate);                       // fila con etiqueta y todos los valores vacíos
            }
            else if (block != body && !HasStaticText(block) && AllKnownMarkersEmpty(block))
            {
                targets.Add(block);                           // caja que solo contiene el marcador (logo, QR)
            }
        }

        foreach (var target in targets)
        {
            target.Remove();
        }
    }

    // ------------------------------------------------------- global markers

    private static void SubstituteGlobalMarkers(IDocument document, IElement body, Dictionary<string, string> values)
    {
        foreach (var node in TextNodesWithMarkers(body, values.ContainsKey))
        {
            ReplaceTextNode(document, node, name => values.TryGetValue(name, out var v) ? RenderTextValue(name, v) : null);
        }

        foreach (var element in document.All.ToList())
        {
            foreach (var attribute in element.Attributes.ToList())
            {
                if (!attribute.Value.Contains("{{", StringComparison.Ordinal))
                {
                    continue;
                }

                element.SetAttribute(attribute.Name, RiHtmlMarkers.Pattern.Replace(attribute.Value, m =>
                    values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value));
            }
        }
    }

    private static string RenderTextValue(string name, string value) => name switch
    {
        "EmisorLogo" when value.Length > 0 =>
            $"<img src=\"{WebUtility.HtmlEncode(value)}\" alt=\"\" style=\"max-width:100%;max-height:100%;object-fit:contain\">",
        "QR" when value.Length > 0 =>
            $"<img src=\"{WebUtility.HtmlEncode(value)}\" alt=\"QR\" style=\"width:110px;height:110px\">",
        _ => WebUtility.HtmlEncode(value)
    };

    /// <summary>
    /// Reemplaza los marcadores de un nodo de texto por HTML ya escapado. Si <paramref name="resolve"/>
    /// devuelve null el marcador se conserva literalmente (marcador desconocido).
    /// </summary>
    private static void ReplaceTextNode(IDocument document, IText node, Func<string, string?> resolve)
    {
        var parent = node.ParentElement;
        if (parent is null)
        {
            return;
        }

        var html = RiHtmlMarkers.Pattern.Replace(WebUtility.HtmlEncode(node.Data), m =>
            resolve(m.Groups[1].Value) ?? m.Value);

        InsertHtml(document, html, parent, node);
        parent.RemoveChild(node);
    }
}
