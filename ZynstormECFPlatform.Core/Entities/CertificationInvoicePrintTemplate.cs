namespace ZynstormECFPlatform.Core.Entities;

public class CertificationInvoicePrintTemplate : BaseEntity
{
    public int CertificationInvoicePrintTemplateId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public int ClientId { get; set; }

    // RI de referencia generada (PDF). El PDF fuente NO se persiste.
    public byte[]? FileData { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/pdf";

    // Descriptor de layout extraído (JSON) — se usa para renderizar cada RI.
    public string? LayoutJson { get; set; }

    public CertificationRiTemplateStatus Status { get; set; } = CertificationRiTemplateStatus.PendingExtraction;

    // Lista JSON de anclas no encontradas / avisos de extracción.
    public string? ExtractionWarnings { get; set; }

    public virtual Client Client { get; set; } = null!;
    public virtual ICollection<CertificationInvoicePrintTemplateEcfType> EcfTypes { get; set; } = [];
}
