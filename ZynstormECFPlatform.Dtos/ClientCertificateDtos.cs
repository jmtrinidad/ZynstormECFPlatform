using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientCertificateCreateDto
{
    [Required]
    public string ClientGuidId { get; set; } = null!;

    [Required]
    public IFormFile Certificate { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class ClientCertificateViewDto
{
    public int ClientCertificateId { get; set; }
    public int ClientId { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
    public DateTime RegisteredAt { get; set; }
}