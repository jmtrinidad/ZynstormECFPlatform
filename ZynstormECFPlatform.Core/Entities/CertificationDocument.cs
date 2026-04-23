using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Core.Entities;

public class CertificationDocument : BaseEntity
{
    public int CertificationDocumentId { get; set; }

    public int CertificationProcessId { get; set; }

    public string ENcfSecuence { get; set; } = null!;

    public int ENcfId { get; set; }

    public int EcfTypeId { get; set; }

    public string XmlSent { get; set; } = null!;

    public string? XmlResponse { get; set; }

    public string? TrackId { get; set; }

    public DocumentStatus Status { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public DateTime? ValidatedAt { get; set; }

    // Relaciones
    public virtual CertificationProcess CertificationProcess { get; set; } = null!;

    public virtual EcfType EcfType { get; set; } = null!;

    public virtual ENcf ENcf { get; set; } = null!;
}