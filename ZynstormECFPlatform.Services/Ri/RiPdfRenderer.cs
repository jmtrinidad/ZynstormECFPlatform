using QuestPDF.Fluent;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Renders a Ri (Representación Impresa) PDF for a signed e-CF XML by dispatching to the
/// QuestPDF template that matches the document's e-CF type: type 41 (Comprobante de
/// Compras) uses the full-sheet <see cref="RiPurchasePdf"/>; every other type uses the
/// 80mm-receipt <see cref="RiInvoicePdf"/>. The XML-to-model mapping is delegated to
/// <see cref="EcfRiTemplateMapper"/>.
/// </summary>
public static class RiPdfRenderer
{
    public static byte[] Render(int ecfType, string signedXml)
    {
        if (ecfType == 41)
        {
            return new RiPurchasePdf(EcfRiTemplateMapper.MapPurchase(signedXml, DgiiEnvironment.CerteCF)).GeneratePdf();
        }

        return new RiInvoicePdf(EcfRiTemplateMapper.MapInvoice(signedXml, DgiiEnvironment.CerteCF)).GeneratePdf();
    }
}
