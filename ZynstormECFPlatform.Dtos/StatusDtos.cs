using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class StatusDto
{
    [Required]
    public int StatusId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public class EcfStatusDto
{
    [Required]
    public int EcfStatusId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}
