using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class CurrencyCreateDto
{
    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public string Name { get; set; } = null!;
}

public class CurrencyUpdateDto : CurrencyCreateDto
{
    [Required]
    public int CurrencyId { get; set; }
}

public class CurrencyViewDto : CurrencyUpdateDto
{
}