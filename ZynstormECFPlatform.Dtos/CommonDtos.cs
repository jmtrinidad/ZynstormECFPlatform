using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfTypeDto
{
    [Required]
    public int EcfTypeId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class CurrencyDto
{
    [Required]
    public int CurrencyId { get; set; }

    [Required]
    [StringLength(3)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Symbol { get; set; }
}
