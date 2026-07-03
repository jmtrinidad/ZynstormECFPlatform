using System.Text;
using QuestPDF.Fluent;
using UglyToad.PdfPig;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiPurchasePdfTests
{
    [Fact]
    public void GeneratePdf_ProducesPdf_WithNcfAndSecurityCode()
    {
        var model = new RiPurchaseModel
        {
            Company = new RiPurchaseCompany
            {
                Name = "TRANSPORTE NJ, SRL",
                Rnc = "133009889",
                Address = "Ensanche Gregorio Luperon, Santiago de los Caballeros",
                Phone = "809-876-4046"
            },
            Supplier = new RiPurchaseSupplier
            {
                Name = "SUPLIDOR INFORMAL DEL NORTE",
                Rnc = "102620717",
                Address = "CARRETERA JANICO, LAS CHARCAS, SANTIAGO, R.D."
            },
            NcfNumber = "E410000000123",
            FechaEmision = "17-04-2026",
            FechaFirma = "24-04-2026 22:12:08",
            Items =
            [
                new RiPurchaseItem
                {
                    Description = "Compra de materiales de construcción",
                    Quantity = 10,
                    Price = 500.00m,
                    Itbis = 900.00m,
                    Amount = 5900.00m
                }
            ],
            SubTotal = 5000.00m,
            Discount = 0m,
            Itbis = 900.00m,
            Total = 5900.00m,
            IsrRetentionRate = 0m,
            IsrRetentionAmount = 0m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=102620717&RncComprador=133009889&ENCF=E410000000123&FechaEmision=17-04-2026&MontoTotal=5900&FechaFirma=24-04-2026%2022:12:08&CodigoSeguridad=Q7K2MX",
            SecurityCode = "Q7K2MX"
        };

        var bytes = new RiPurchasePdf(model).GeneratePdf();

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));

        Assert.Contains(model.NcfNumber, text);
        Assert.Contains(model.SecurityCode, text);
    }

    [Fact]
    public void GeneratePdf_WithRetentions_ShowsRetentionRows_AndNetTotal()
    {
        var model = new RiPurchaseModel
        {
            Company = new RiPurchaseCompany { Name = "EMPRESA COMPRADORA", Rnc = "132293894", Address = "AVE. ISABEL AGUIAR NO. 269" },
            Supplier = new RiPurchaseSupplier { Name = "SUPLIDOR INFORMAL", Rnc = "533445861" },
            NcfNumber = "E410000000007",
            FechaEmision = "01-04-2020",
            Items = [new RiPurchaseItem { Description = "Servicio Profesional", Quantity = 15, Price = 385.00m, Itbis = 1049.90m, Amount = 5832.75m, ItbisRate = 18m }],
            SubTotal = 16064.05m,
            Itbis = 2891.53m,
            Total = 18955.58m,
            IsrRetentionAmount = 1606.41m,
            IsrRetentionRate = 10.0m,
            ItbisRetentionAmount = 2846.53m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E410000000007&CodigoSeguridad=lu69Mx",
            SecurityCode = "lu69Mx"
        };

        using var pdf = PdfDocument.Open(new RiPurchasePdf(model).GeneratePdf());
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));

        Assert.Contains("Retención ISR", text);
        Assert.Contains("Retención ITBIS", text);
        Assert.Contains("18%", text);         // ITBIS% de la línea
        Assert.Contains("14,502.64", text);   // Total Neto = 18955.58 - 2846.53 - 1606.41
    }
}
