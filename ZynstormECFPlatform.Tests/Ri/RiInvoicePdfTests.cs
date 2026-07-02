using System.Text;
using QuestPDF.Fluent;
using UglyToad.PdfPig;
using ZynstormECFPlatform.Services.Ri;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri;

public class RiInvoicePdfTests
{
    [Fact]
    public void GeneratePdf_ProducesPdf_WithNcfAndSecurityCode()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany
            {
                Name = "TRANSPORTE NJ, SRL",
                Rnc = "133009889",
                Address = "Ensanche Gregorio Luperon, Santiago de los Caballeros",
                Phone = "809-876-4046"
            },
            Client = new RiInvoiceClient
            {
                Name = "MORTEROS DE EUROPA",
                Rnc = "102620717",
                Address = "CARRETERA JANICO, LAS CHARCAS, SANTIAGO, R.D."
            },
            NcfNumber = "E310000000402",
            NcfTypeName = "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
            EcfType = 31,
            FechaEmision = "17-04-2026",
            FechaFirma = "24-04-2026 22:12:08",
            Items =
            [
                new RiInvoiceItem
                {
                    Description = "Servicio de Transporte de Carga",
                    Quantity = 1,
                    Price = 6001.00m,
                    Itbis = 0m,
                    Amount = 6001.00m
                }
            ],
            SubTotal = 6001.00m,
            Discount = 0m,
            Itbis = 0m,
            Total = 6001.00m,
            PaymentType = "CONTADO",
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=133009889&RncComprador=102620717&ENCF=E310000000402&FechaEmision=17-04-2026&MontoTotal=6001&FechaFirma=24-04-2026%2022:12:08&CodigoSeguridad=N4J8CY",
            SecurityCode = "N4J8CY"
        };

        var bytes = new RiInvoicePdf(model).GeneratePdf();

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));

        Assert.Contains(model.NcfNumber, text);
        Assert.Contains(model.SecurityCode, text);
    }
}
