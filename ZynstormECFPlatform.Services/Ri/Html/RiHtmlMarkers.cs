using System.Text.RegularExpressions;

namespace ZynstormECFPlatform.Services.Ri.Html;

/// <summary>Marcadores {{Nombre}} soportados por los formatos HTML de RI.</summary>
public static class RiHtmlMarkers
{
    public static readonly Regex Pattern = new(@"\{\{\s*([A-Za-z][A-Za-z0-9_]*)\s*\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Marcadores que viven dentro de la fila repetible de ítems. <c>ITBIS</c> es el nombre que usa el diseñador
    /// y equivale a <c>ItemItbis</c>; <c>TotalITBIS</c> equivale al <c>Itbis</c> de totales. <c>Exento</c> vale «E»
    /// en los ítems exentos y vacío en los demás.
    /// </summary>
    public static readonly IReadOnlySet<string> ItemMarkers = new HashSet<string>(StringComparer.Ordinal)
    {
        "Descripcion", "Monto", "Cantidad", "Precio", "Unidad", "ItemItbis", "ITBIS", "Exento"
    };

    /// <summary>Marcadores que el XML no trae: salen de los datos de muestra del tipo de cliente.</summary>
    public static readonly IReadOnlySet<string> SampleMarkers = new HashSet<string>(StringComparer.Ordinal)
    {
        "Cedula", "Sexo", "FechaNac", "Edad", "SegMed", "NSS", "Devolucion", "Medico", "MontoPagado", "Restante"
    };

    private static readonly string[] DocumentMarkers =
    [
        "EmisorLogo", "EmisorNombre", "EmisorDireccion", "EmisorRNC", "EmisorTelefono",
        "TipoComprobante", "eNCF", "FechaVencimiento", "NoFactura", "Fecha", "FormaPago", "Nombre",
        "QR", "CodigoSeguridad", "FechaFirma", "Subtotal", "SubtotalGravado", "SubtotalExento", "Itbis", "TotalITBIS", "Total",
        "TotalDescuento", "NcfModificado", "CodigoModificacion"
    ];

    public static readonly IReadOnlySet<string> Known =
        new HashSet<string>(DocumentMarkers.Concat(ItemMarkers).Concat(SampleMarkers), StringComparer.Ordinal);

    /// <summary>Nombres de marcadores presentes en <paramref name="text"/>, en orden y con repeticiones.</summary>
    public static IEnumerable<string> Extract(string text) =>
        Pattern.Matches(text).Select(m => m.Groups[1].Value);
}
