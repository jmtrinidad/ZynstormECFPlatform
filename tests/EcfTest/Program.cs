using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services;

namespace EcfTest;

class Program
{
    static void Main(string[] args)
    {
        var service = new EcfGeneratorService();

        Console.WriteLine("=== Testing Standard Invoice (e-CF 31) ===");
        TestStandardInvoice(service);

        Console.WriteLine("\n=== Testing Invoice with ISC (e-CF 31) ===");
        TestIscInvoice(service);
    }

    static void TestStandardInvoice(EcfGeneratorService service)
    {
        var dto = new EcfInvoiceRequestDto
        {
            Ncf = "E310000000001",
            IssuerRnc = "101000001",
            IssuerName = "EMPRESA DE PRUEBA SAS",
            IssuerAddress = "Calle Central 45, Santo Domingo",
            IssuerProvince = "100000",
            IssuerMunicipality = "100100",
            IssuerPhone = "809-555-0000",
            CustomerRnc = "101000002",
            CustomerName = "JUAN PEREZ",
            CustomerAddress = "Av. Winston Churchill",
            IssueDate = DateTime.UtcNow,
            IncomeType = "01",
            PaymentType = 1,
            Items = new List<EcfItemRequestDto>
            {
                new EcfItemRequestDto
                {
                    Name = "Producto 1",
                    Quantity = 2,
                    UnitPrice = 100,
                    TaxPercentage = 18
                }
            }
        };

        RunTest(service, dto);
    }

    static void TestIscInvoice(EcfGeneratorService service)
    {
        var dto = new EcfInvoiceRequestDto
        {
            Ncf = "E310000012345",
            IssuerRnc = "101000001",
            IssuerName = "DISTRIBUIDORA DE BEBIDAS SAS",
            IssuerAddress = "Zona Industrial Haina",
            IssuerProvince = "100000",
            IssuerMunicipality = "100100",
            IssuerPhone = "809-555-1111",
            CustomerRnc = "101000002",
            CustomerName = "SUPERMERCADO NACIONAL",
            CustomerAddress = "Av. 27 de Febrero",
            CustomerTelephone = "809-222-3333", // New field check
            IssueDate = DateTime.UtcNow,
            IncomeType = "01",
            PaymentType = 1,
            Items = new List<EcfItemRequestDto>
            {
                new EcfItemRequestDto
                {
                    Name = "Cerveza Presidente 12oz",
                    Quantity = 24,
                    UnitPrice = 150.00m,
                    TaxPercentage = 18,
                    IscType = "006", // Cerveza
                    AdditionalTaxRate = 10.00m,
                    IscSpecificAmount = 120.00m, // 5.00 x 24
                    IscAdvaloremAmount = 0.00m
                },
                new EcfItemRequestDto
                {
                    Name = "Ron Barcelo Gran Añejo",
                    Quantity = 12,
                    UnitPrice = 800.00m,
                    TaxPercentage = 18,
                    IscType = "014", // Ron
                    AdditionalTaxRate = 10.00m,
                    IscSpecificAmount = 0.00m,
                    IscAdvaloremAmount = 960.00m // 10% de (12 * 800)
                }
            }
        };

        RunTest(service, dto);
    }


    static void RunTest(EcfGeneratorService service, EcfInvoiceRequestDto dto)
    {
        try
        {
            var xml = service.GenerateUnsignedXml(dto);
            // Console.WriteLine(xml); // Descomentar para ver el XML completo if needed

            var ecfType = 31; // E31
            var xsdErrors = service.ValidateXmlAgainstSchema(xml, ecfType);

            if (xsdErrors.Count == 0)
            {
                Console.WriteLine("SUCCESS: XML is valid against XSD.");
            }
            else
            {
                Console.WriteLine("FAILED: XML is NOT valid against XSD.");
                foreach (var err in xsdErrors)
                {
                    Console.WriteLine($" - {err}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EXCEPTION: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
