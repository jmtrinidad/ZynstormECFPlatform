using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class DGIIUnitCreateDto
{
    [Required]
    public int DGIICode { get; set; }

    [Required]
    public string Name { get; set; } = null!;
}

public class DGIIUnitUpdateDto : DGIIUnitCreateDto
{
    [Required]
    public int DGIIUnitId { get; set; }
}

public class DGIIUnitViewDto : DGIIUnitUpdateDto
{
}