namespace ZynstormECFPlatform.Dtos.Ri;

/// <summary>
/// Lightweight projection of a Ri model/template used for list views
/// (per-client template listing).
/// </summary>
public class RiModelListItemDto
{
    public string GuidId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> AssignedTypeCodes { get; set; } = [];

    public string Status { get; set; } = string.Empty;

    public bool HasReferenceRi { get; set; }
}
