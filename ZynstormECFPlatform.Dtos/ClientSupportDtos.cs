using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class ClientCallBackDto
{
    public int ClientCallBackId { get; set; }
    public int ClientId { get; set; }
    public string CallBackUrl { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class ClientCertificateDto
{
    public int ClientCertificateId { get; set; }
    public int ClientId { get; set; }
    public string CertificatePath { get; set; } = string.Empty;
    public string? Password { get; set; }
    public DateTime ExpiryDate { get; set; }
}
