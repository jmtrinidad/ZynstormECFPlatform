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

    private static string PdfText(byte[] bytes)
    {
        using var pdf = PdfDocument.Open(bytes);
        return string.Join(" ", pdf.GetPages().SelectMany(p => p.GetWords().Select(w => w.Text)));
    }

    [Fact]
    public void GeneratePdf_Contado_RendersFullHeaderAndFooter()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "MULTI SERVICE ICAAYSI SRL", Rnc = "132293894", Address = "C/Cristino Zeno & Duarte", Phone = "(809) 725 4440", Whatsapp = "(809) 725 4440" },
            Client = new RiInvoiceClient { Name = "TRANSPORTE NJ,SRL", Rnc = "133009889" },
            NcfNumber = "E310000000019",
            NcfTypeName = "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
            EcfType = 31,
            ValidUntil = "31/12/2028",
            InternalInvoiceNumber = "0019",
            PaymentType = "CONTADO",
            PaymentCondition = "CONTADO",
            IsCredit = false,
            Cashier = "PEDRO",
            FechaEmision = "02-07-2026",
            Items = [new RiInvoiceItem { Description = "BOLAZUL GRANDE 10/5", Quantity = 2, Price = 187.50m, Itbis = 67.50m, Amount = 442.50m, Unit = "Und" }],
            SubTotal = 375.00m,
            Itbis = 67.50m,
            Total = 442.50m,
            ReceivedAmount = 443.00m,
            ChangeAmount = 0.50m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?RncEmisor=132293894&ENCF=E310000000019&CodigoSeguridad=AbC123",
            SecurityCode = "AbC123"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.Contains("VALIDO HASTA:", text);
        Assert.Contains("31/12/2028", text);
        Assert.Contains("FACTURA:", text);
        Assert.Contains("0019", text);
        Assert.Contains("TIPO DE PAGO:", text);
        Assert.Contains("COND. PAGO:", text);
        Assert.Contains("CAJERO:", text);
        Assert.Contains("PEDRO", text);
        Assert.Contains("Und", text);
        Assert.Contains("ATENDIDO POR:", text);
        Assert.Contains("ARTÍCULOS:", text);
        Assert.Contains("TOTAL RECIBIDO:", text);
        Assert.Contains("SU CAMBIO:", text);
        Assert.DoesNotContain("FIRMA REQUERIDA", text);
    }

    [Fact]
    public void GeneratePdf_CreditNote34_HidesValidUntil_ShowsReferenceBlocks()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "EMPRESA X", Rnc = "132878191" },
            Client = new RiInvoiceClient { Name = "CLIENTE Y", Rnc = "131880681" },
            NcfNumber = "E340000000002",
            NcfTypeName = "NOTA DE CRÉDITO ELECTRÓNICA",
            EcfType = 34,
            ValidUntil = "31/12/2028", // aunque venga, para 34 no se muestra
            InternalInvoiceNumber = "123456789016",
            PaymentType = "CONTADO",
            IsCredit = false,
            Cashier = "PEDRO",
            FechaEmision = "02-04-2020",
            AffectedNcf = "E310000000034",
            ModificationCode = "3 - Corrige montos del NCF modificado",
            ModificationReason = "Error en monto",
            Items = [new RiInvoiceItem { Description = "BLOCK", Quantity = 1, Price = 480250.00m, Itbis = 86445.00m, Amount = 480250.00m }],
            SubTotal = 480250.00m,
            Itbis = 86445.00m,
            Total = 566695.00m,
            ReceivedAmount = 566695.00m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E340000000002&CodigoSeguridad=XyZ789",
            SecurityCode = "XyZ789"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.DoesNotContain("VALIDO HASTA:", text);
        Assert.Contains("NOTA CRÉDITO:", text);
        Assert.Contains("DATOS FACTURA AFECTADA", text);
        Assert.Contains("NCF MODIFICADO:", text);
        Assert.Contains("E310000000034", text);
        Assert.Contains("INFORMACIÓN DE MODIFICACIÓN", text);
        Assert.Contains("Error en monto", text);
        // Las notas no llevan TIPO/COND PAGO en la cabecera (paridad EasyInvoice).
        Assert.DoesNotContain("COND. PAGO:", text);
    }

    [Fact]
    public void GeneratePdf_Credito_ShowsFirmaRequerida_NoReceivedChange()
    {
        var model = new RiInvoiceModel
        {
            Company = new RiInvoiceCompany { Name = "TRANSPORTE NJ, SRL", Rnc = "133009889" },
            Client = new RiInvoiceClient { Name = "MORTEROS DE EUROPA", Rnc = "102620717" },
            NcfNumber = "E310000000402",
            NcfTypeName = "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
            EcfType = 31,
            ValidUntil = "31/12/2028",
            PaymentType = "CRÉDITO",
            PaymentCondition = "15 DIAS",
            IsCredit = true,
            Cashier = "PEDRO",
            FechaEmision = "17-04-2026",
            Items = [new RiInvoiceItem { Description = "Servicio de Transporte", Quantity = 1, Price = 6001.00m, Amount = 6001.00m }],
            SubTotal = 6001.00m,
            Total = 6001.00m,
            ReceivedAmount = 6001.00m,
            Qr = "https://ecf.dgii.gov.do/CerteCF/ConsultaTimbre?ENCF=E310000000402&CodigoSeguridad=N4J8CY",
            SecurityCode = "N4J8CY"
        };

        var text = PdfText(new RiInvoicePdf(model).GeneratePdf());

        Assert.Contains("FIRMA REQUERIDA", text);
        Assert.Contains("CRÉDITO:", text);
        Assert.DoesNotContain("TOTAL RECIBIDO:", text);
        Assert.DoesNotContain("SU CAMBIO:", text);
    }
}
