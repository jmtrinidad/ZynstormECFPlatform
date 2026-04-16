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

    //public int StatusId { get; set; }
}

public class ClientUpdateDto : ClientCreateDto
{
    [Required]
    public int ClientId { get; set; }
}

public class ClientViewDto : ClientUpdateDto
{
    public DateTime RegisteredAt { get; set; }
}