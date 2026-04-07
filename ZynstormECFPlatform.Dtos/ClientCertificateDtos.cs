using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientCertificateCreateDto
{
    [Required]
    public int ClientId { get; set; }

    [Required]
    public string Certificate { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public string? Thumbprint { get; set; }

    public DateTime? ExpirationDateUtc { get; set; }
}

public class ClientCertificateUpdateDto : ClientCertificateCreateDto
{
    [Required]
    public int ClientCertificateId { get; set; }
}

public class ClientCertificateViewDto : ClientCertificateUpdateDto
{
    public DateTime RegisteredAt { get; set; }
}