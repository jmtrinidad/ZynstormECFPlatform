namespace ZynstormECFPlatform.Core.Entities;

public class CertificationInvoicePrintTemplate : BaseEntity
{
    public int CertificationInvoicePrintTemplateId { get; set; }

    // Identificación
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Relación lógica
    public int ClientId { get; set; }

    public int EcfTypeId { get; set; }

    // Archivo (elige UNA estrategia principal)

    // Opción A: Guardar en base de datos
    public byte[]? FileData { get; set; }

    // Opción B: Guardar en filesystem / cloud
    public string? FileUrl { get; set; }

    // Metadata del archivo
    public string FileName { get; set; } = null!;

    public string ContentType { get; set; } = "application/pdf";

    public virtual Client Client { get; set; } = null!;

    public virtual EcfType EcfType { get; set; } = null!;
}