using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public sealed class GenerateSerialRequestDto
{
    [MaxLength(200)]
    public string? Description { get; set; }
}

public sealed class GeneratedSerialDto
{
    public string GuidId { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public sealed class SerialValidationDto
{
    public bool IsValid { get; set; }
}
