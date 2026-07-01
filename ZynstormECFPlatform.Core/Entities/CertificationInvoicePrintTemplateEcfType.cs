namespace ZynstormECFPlatform.Core.Entities;

public class CertificationInvoicePrintTemplateEcfType
{
    public int CertificationInvoicePrintTemplateEcfTypeId { get; set; }
    public int CertificationInvoicePrintTemplateId { get; set; }
    public int EcfTypeId { get; set; }

    public virtual CertificationInvoicePrintTemplate Template { get; set; } = null!;
    public virtual EcfType EcfType { get; set; } = null!;
}
