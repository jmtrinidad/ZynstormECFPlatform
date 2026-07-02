namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Contenido posicionado extraído de un PDF modelo, clasificado en estático
/// (se conserva verbatim) y dinámico (valores que se reemplazan con datos del e-CF).
/// Coordenadas en puntos, origen arriba-izquierda (listo para dibujar con PDFsharp).
/// </summary>
public class PageModel
{
    public double WidthPt { get; set; }
    public double HeightPt { get; set; }

    /// <summary>Texto que se conserva tal cual (etiquetas, encabezado, cabeceras de tabla, pie, etc.).</summary>
    public List<TextRun> StaticRuns { get; set; } = [];

    /// <summary>Líneas / recuadros (bordes).</summary>
    public List<LineSeg> Lines { get; set; } = [];

    /// <summary>Imágenes (logo, etc.).</summary>
    public List<ImageEl> Images { get; set; } = [];

    /// <summary>Posiciones donde va cada valor dinámico del e-CF.</summary>
    public List<FieldSlot> Fields { get; set; } = [];

    /// <summary>Región de la tabla de ítems (columnas + banda de filas).</summary>
    public ItemsRegion Items { get; set; } = new();

    /// <summary>Posición del QR (por defecto inferior si el modelo no trae uno).</summary>
    public QrSlot Qr { get; set; } = new();

    public List<string> Warnings { get; set; } = [];
}

public class TextRun
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double FontSize { get; set; }
    public string? ColorHex { get; set; }
    public bool Bold { get; set; }
}

public class LineSeg
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Thickness { get; set; }
}

public class ImageEl
{
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    public string Base64 { get; set; } = string.Empty;
}

public class FieldSlot
{
    public string FieldKey { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double FontSize { get; set; }
    public string Align { get; set; } = "Left";
    public double MaxWidth { get; set; }
}

public class ItemsRegion
{
    public double TopY { get; set; }
    public double RowHeight { get; set; }
    public List<ItemColumn> Columns { get; set; } = [];
}

public class ItemColumn
{
    public string Field { get; set; } = string.Empty;
    public double X { get; set; }
    public double Width { get; set; }
    public string Align { get; set; } = "Left";
}

public class QrSlot
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
}
