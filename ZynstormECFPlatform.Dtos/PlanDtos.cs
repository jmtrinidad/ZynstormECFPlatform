using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class PlanOverageTierDto
{
    [Range(1, int.MaxValue)]
    public int FromUnit { get; set; }

    public int? ToUnit { get; set; }

    [Range(0, 9999999)]
    public decimal UnitPrice { get; set; }
}

public class PlanCreateDto
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    [Range(0, 99999999)]
    public decimal MonthlyFee { get; set; }

    /// <summary>-1 = ilimitado.</summary>
    public int MonthlyDocumentLimit { get; set; }

    /// <summary>1 = Comprobantes, 2 = Renta.</summary>
    [Range(1, 2)]
    public int PlanTypeId { get; set; } = 1;

    /// <summary>Usuarios permitidos al mismo tiempo. Solo planes de renta. -1 = ilimitado.</summary>
    public int? MaxUsers { get; set; }

    public bool IsActive { get; set; } = true;

    public List<PlanOverageTierDto> OverageTiers { get; set; } = [];
}

public class PlanUpdateDto : PlanCreateDto
{
    [Required]
    public string GuidId { get; set; } = string.Empty;
}

public class PlanViewDto : PlanUpdateDto
{
    public int PlanId { get; set; }

    public int ClientsCount { get; set; }

    public DateTime RegisteredAt { get; set; }
}
