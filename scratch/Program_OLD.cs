using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services;

var generator = new EcfGeneratorService();

var dto = new EcfInvoiceRequestDto
{
    EcfType = 46,
    Ncf = "E460000000001",
    IssuerRnc = "101000001",
    IssuerName = "EMPRESA EXPORTADORA",
    IssuerAddress = "Calle 1, SD",
    CustomerRnc = null,
    CustomerName = "FOREIGN BUYER",
    CustomerForeignId = "ID123",
    CustomerCountry = "USA",
    IncomeType = "01",
    PaymentType = 1,
    Items = new List<EcfItemRequestDto>
    {
        new EcfItemRequestDto
        {
            Name = "Item 1",
            Quantity = 1,
            UnitPrice = 100,
            TaxPercentage = 0,
            BillingIndicator = 3
        }
    }
};

string xml = generator.GenerateUnsignedXml(dto);
Console.WriteLine(xml);
