using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientCreateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string Rnc { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? DailyReportEmails { get; set; }

    [StringLength(500)]
    public string? WeeklyReportEmails { get; set; }

    //public int StatusId { get; set; }
}

public class ClientUpdateDto : ClientCreateDto
{
    [Required]
    public string GuidId { get; set; } = string.Empty;

    public int ClientId { get; set; }
}

public class ClientViewDto : ClientUpdateDto
{
    public bool IsCertified { get; set; }

    public DateTime RegisteredAt { get; set; }

    public string? ApiKey { get; set; }
}
