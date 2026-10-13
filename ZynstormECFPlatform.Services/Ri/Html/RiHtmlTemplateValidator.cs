using System.Text;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace ZynstormECFPlatform.Services.Ri.Html;

public sealed record RiHtmlValidationResult(
    string SanitizedHtml,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

/// <summary>Sanea el HTML subido (sin scripts ni recursos externos) y valida los marcadores mínimos.</summary>
public static class RiHtmlTemplateValidator
{
    public const int MaxBytes = 2 * 1024 * 1024;

    private static readonly string[] RemovedTags = ["script", "iframe", "object", "embed", "base", "link", "frame", "frameset"];
    private static readonly string[] UrlAttributes = ["href", "src", "action", "formaction", "xlink:href", "data"];
    private static readonly Regex CssImport = new(@"@import[^;]*;?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static RiHtmlValidationResult Validate(string? html)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(html))
        {
            errors.Add("El HTML está vacío.");
            return new RiHtmlValidationResult(string.Empty, errors, warnings);
        }

        if (Encoding.UTF8.GetByteCount(html) > MaxBytes)
        {
            errors.Add("El HTML supera el tamaño máximo de 2 MB.");
            return new RiHtmlValidationResult(string.Empty, errors, warnings);
        }

        var document = new HtmlParser().ParseDocument(html);
        Sanitize(document);
        var sanitized = "<!DOCTYPE html>" + document.DocumentElement.OuterHtml;

        var markers = RiHtmlMarkers.Extract(sanitized).ToHashSet(StringComparer.Ordinal);

        if (!markers.Contains("Descripcion") && !markers.Contains("Monto"))
        {
            errors.Add("El formato debe incluir una fila de ítems con {{Descripcion}} o {{Monto}}.");
        }

        if (!markers.Contains("Total"))
        {
            errors.Add("El formato debe incluir {{Total}}.");
        }

        foreach (var unknown in markers.Where(m => !RiHtmlMarkers.Known.Contains(m)).OrderBy(m => m, StringComparer.Ordinal))
        {
            warnings.Add($"Marcador desconocido: {{{{{unknown}}}}}; se dejará tal cual.");
        }

        if (!markers.Contains("eNCF"))
        {
            warnings.Add("No incluye {{eNCF}}: el sistema agregará el bloque eNCF / VALIDO HASTA debajo del título.");
        }

        if (!markers.Contains("QR"))
        {
            warnings.Add("No incluye {{QR}}: el sistema agregará el bloque del QR al final.");
        }
        else
        {
            if (!markers.Contains("CodigoSeguridad"))
            {
                warnings.Add("Incluye {{QR}} pero no {{CodigoSeguridad}}.");
            }

            if (!markers.Contains("FechaFirma"))
            {
                warnings.Add("Incluye {{QR}} pero no {{FechaFirma}}.");
            }
        }

        return new RiHtmlValidationResult(sanitized, errors, warnings);
    }

    private static void Sanitize(IDocument document)
    {
        foreach (var tag in RemovedTags)
        {
            foreach (var element in document.QuerySelectorAll(tag).ToList())
            {
                element.Remove();
            }
        }

        foreach (var meta in document.QuerySelectorAll("meta[http-equiv]").ToList())
        {
            meta.Remove();
        }

        foreach (var element in document.All.ToList())
        {
            foreach (var attribute in element.Attributes.ToList())
            {
                var name = attribute.Name;
                if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
                {
                    element.RemoveAttribute(name);
                    continue;
                }

                if (UrlAttributes.Contains(name, StringComparer.OrdinalIgnoreCase) &&
                    attribute.Value.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                {
                    element.RemoveAttribute(name);
                }
            }
        }

        foreach (var style in document.QuerySelectorAll("style").ToList())
        {
            style.TextContent = CssImport.Replace(style.TextContent, string.Empty);
        }
    }
}
