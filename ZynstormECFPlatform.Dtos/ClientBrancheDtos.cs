using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ZynstormECFPlatform.Dtos;

public class ClientBrancheCreateDto
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address { get; set; }

    [Phone]
    public string? Phone { get; set; }

    public string Code { get; set; } = null!;

    [EmailAddress]
    public string? Email { get; set; }

    public bool IsMain { get; set; }

    public int StatusId { get; set; }
}

public class ClientBrancheUpdateDto : ClientBrancheCreateDto
{
    [Required]
    public int ClientBrancheId { get; set; }
}

public class ClientBrancheViewDto : ClientBrancheUpdateDto
{
    public DateTime CreatedAtUtc { get; set; }
}