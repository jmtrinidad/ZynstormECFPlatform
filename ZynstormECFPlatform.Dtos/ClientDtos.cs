using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientDto
{
    [Required]
    public int ClientId { get; set; }

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

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public StatusDto? Status { get; set; }

    public ICollection<ClientBrancheDto> Branches { get; set; } = new List<ClientBrancheDto>();
}

public class ClientBrancheDto
{
    [Required]
    public int ClientBrancheId { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [Phone]
    public string? Phone { get; set; }
}
