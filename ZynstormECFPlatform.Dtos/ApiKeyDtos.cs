using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ApiKeyDto
{
    [Required]
    public int ApiKeyId { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public StatusDto? Status { get; set; }
}
