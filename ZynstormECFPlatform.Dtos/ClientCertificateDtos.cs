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
    public string GuidId { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string? Thumbprint { get; set; }
    public DateTime? ExpirationDateUtc { get; set; }
    public DateTime? NotBeforeUtc { get; set; }
    public string? SerialNumber { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectIdentification { get; set; }
    public string? SubjectEmail { get; set; }
    public string? SubjectAddress { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationIdentifier { get; set; }
    public string? IssuerName { get; set; }
    public DateTime RegisteredAt { get; set; }
}

public class ClientCertificateRefreshDetailsResultDto
{
    public int Updated { get; set; }
    public int Failed { get; set; }
}