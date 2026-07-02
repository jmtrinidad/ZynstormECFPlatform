using System.Globalization;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Maps a signed e-CF XML document into the <see cref="RiInvoiceModel"/> consumed by
/// <see cref="RiInvoicePdf"/> (the QuestPDF template ported from EasyInvoice's
/// InvoicePdf). Reuses <see cref="EcfRiDataMapper"/> for the emisor/comprador/items/
/// totals/security-code extraction and QR URL construction, so both renderers stay
/// consistent about how those values are derived from the XML.
/// </summary>
public static class EcfRiTemplateMapper
{
    public static RiInvoiceModel MapInvoice(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);
        var ecfType = int.TryParse(data.TipoeCF, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        return new RiInvoiceModel
        {
            Company = new RiInvoiceCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            Client = new RiInvoiceClient
            {
                Name = data.Buyer.Name,
                Rnc = data.Buyer.Document,
                Address = string.IsNullOrWhiteSpace(data.Buyer.Address) ? null : data.Buyer.Address
            },
            NcfNumber = data.ENcf,
            NcfTypeName = NcfTypeName(ecfType),
            EcfType = ecfType,
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            Items = data.Items.ConvertAll(item => new RiInvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Price = item.Price,
                Itbis = item.Itbis,
                Amount = item.Amount
            }),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>
    /// Maps a signed e-CF type 41 (Comprobante de Compras) XML into the
    /// <see cref="RiPurchaseModel"/> consumed by <see cref="RiPurchasePdf"/> (the
    /// full-sheet QuestPDF template ported from EasyInvoice's PurchasePdf). Emisor maps
    /// to Company and Comprador maps to Supplier, since in a compras e-CF the buyer
    /// (comprador) is the one issuing the document to record a purchase from the
    /// supplier identified in the emisor node.
    /// </summary>
    public static RiPurchaseModel MapPurchase(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        return new RiPurchaseModel
        {
            Company = new RiPurchaseCompany
            {
                Name = data.Buyer.Name,
                Rnc = data.Buyer.Document,
                Address = data.Buyer.Address,
                Phone = data.Buyer.Phone
            },
            Supplier = new RiPurchaseSupplier
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = string.IsNullOrWhiteSpace(data.Issuer.Address) ? null : data.Issuer.Address
            },
            NcfNumber = data.ENcf,
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            Items = data.Items.ConvertAll(item => new RiPurchaseItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Price = item.Price,
                Itbis = item.Itbis,
                Amount = item.Amount
            }),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>DGII e-CF type code to display title, mirroring EasyInvoice's InvoicePdf label logic.</summary>
    private static string NcfTypeName(int ecfType) => ecfType switch
    {
        31 => "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
        32 => "FACTURA DE CONSUMO ELECTRÓNICA",
        33 => "NOTA DE CRÉDITO ELECTRÓNICA",
        34 => "NOTA DE DÉBITO ELECTRÓNICA",
        41 => "COMPRAS ELECTRÓNICO",
        43 => "GASTO MENOR ELECTRÓNICO",
        44 => "REGÍMENES ESPECIALES ELECTRÓNICO",
        45 => "GUBERNAMENTAL ELECTRÓNICO",
        46 => "EXPORTACIONES ELECTRÓNICO",
        47 => "PAGOS AL EXTERIOR ELECTRÓNICO",
        _ => "COMPROBANTE FISCAL ELECTRÓNICO"
    };
}
