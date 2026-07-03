using QuestPDF.Fluent;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Datos del emisor para el encabezado de la RI, tomados del CLIENTE seleccionado
/// (no del XML), porque varían por cliente. WA reusa el mismo teléfono.
/// </summary>
public record RiCompanyHeader(string Name, string Rnc, string Address, string Phone, string Whatsapp);

/// <summary>
/// Renders a Ri (Representación Impresa) PDF for a signed e-CF XML by dispatching to the
/// QuestPDF template that matches the document's e-CF type: 41 (Compras) uses the
/// full-sheet <see cref="RiPurchasePdf"/>, 43 (Gastos Menores) uses
/// <see cref="RiExpensePdf"/>, and every other type uses the 80mm-receipt
/// <see cref="RiInvoicePdf"/>. The XML-to-model mapping is delegated to
/// <see cref="EcfRiTemplateMapper"/>.
/// </summary>
public static class RiPdfRenderer
{
    public static byte[] Render(int ecfType, string signedXml, RiCompanyHeader? company = null)
    {
        if (ecfType == 41)
        {
            var model = EcfRiTemplateMapper.MapPurchase(signedXml, DgiiEnvironment.CerteCF);
            if (company is not null)
            {
                model.Company = new RiPurchaseCompany
                {
                    Name = company.Name,
                    Rnc = company.Rnc,
                    Address = company.Address,
                    Phone = company.Phone,
                    Whatsapp = company.Whatsapp,
                };
            }
            return new RiPurchasePdf(model).GeneratePdf();
        }

        if (ecfType == 43)
        {
            var expense = EcfRiTemplateMapper.MapExpense(signedXml, DgiiEnvironment.CerteCF);
            if (company is not null)
            {
                expense.Company = new RiInvoiceCompany
                {
                    Name = company.Name,
                    Rnc = company.Rnc,
                    Address = company.Address,
                    Phone = company.Phone,
                    Whatsapp = company.Whatsapp,
                };
            }
            return new RiExpensePdf(expense).GeneratePdf();
        }

        var invoice = EcfRiTemplateMapper.MapInvoice(signedXml, DgiiEnvironment.CerteCF);
        if (company is not null)
        {
            invoice.Company = new RiInvoiceCompany
            {
                Name = company.Name,
                Rnc = company.Rnc,
                Address = company.Address,
                Phone = company.Phone,
                Whatsapp = company.Whatsapp,
            };
        }
        return new RiInvoicePdf(invoice).GeneratePdf();
    }
}
