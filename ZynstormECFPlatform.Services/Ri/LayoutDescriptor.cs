namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Describes the visual layout of a printable Ri (Representación Impresa) document,
/// as produced by the PDF layout extractor and consumed by the QuestPDF renderer.
/// </summary>
public class LayoutDescriptor
{
    public PageInfo Page { get; set; } = new();

    public Dictionary<string, string> Palette { get; set; } = [];

    public LogoSlot? Logo { get; set; }

    public Dictionary<string, FieldSlot> FieldSlots { get; set; } = [];

    public ItemsTable Items { get; set; } = new();

    public List<TotalRow> Totals { get; set; } = [];

    public QrSlot Qr { get; set; } = new();

    public List<FixedText> FixedTexts { get; set; } = [];

    public Dictionary<string, string> Images { get; set; } = [];
}

public class PageInfo
{
    public double WidthPt { get; set; }

    public double HeightPt { get; set; }

    public double Margin { get; set; }
}

public class FieldSlot
{
    public double X { get; set; }

    public double Y { get; set; }

    public string? Label { get; set; }
}

public class ItemsTable
{
    public double TopY { get; set; }

    public List<ItemColumn> Columns { get; set; } = [];
}

public class ItemColumn
{
    public string Field { get; set; } = string.Empty;

    public double X { get; set; }

    public double W { get; set; }

    public string Align { get; set; } = string.Empty;
}

public class TotalRow
{
    public string Field { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public class QrSlot
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Size { get; set; }
}

public class FixedText
{
    public string Text { get; set; } = string.Empty;

    public double X { get; set; }

    public double Y { get; set; }

    public string Align { get; set; } = string.Empty;

    public double? FontSize { get; set; }
}

public class LogoSlot
{
    public double X { get; set; }

    public double Y { get; set; }

    public double W { get; set; }

    public double H { get; set; }

    public string ImageRef { get; set; } = string.Empty;
}
