namespace ZynstormECFPlatform.Dtos.Ri;

/// <summary>
/// Request payload shared by the upload/update endpoints for Ri models/templates.
/// The source PDF (when present) is only used to (re)extract the layout; it is
/// never persisted.
/// </summary>
public class SaveRiModelRequestDto
{
    public string? GuidId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> EcfTypeCodes { get; set; } = [];

    public bool? Confirm { get; set; }

    public byte[]? SourcePdf { get; set; }

    public string? FileName { get; set; }
}
