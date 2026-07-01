namespace ZynstormECFPlatform.Dtos.Ri;

/// <summary>
/// Full detail of a Ri (Representación Impresa) model/template, returned after
/// upload/update operations and when loading a single template for edition.
/// </summary>
public class RiModelDto
{
    public string GuidId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> AssignedTypeCodes { get; set; } = [];

    public string Status { get; set; } = string.Empty;

    public List<string> Warnings { get; set; } = [];

    public bool HasReferenceRi { get; set; }
}
