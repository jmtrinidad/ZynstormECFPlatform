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
        var typesToTest = new[] { 31, 32, 33, 34, 41, 43, 44, 45, 46, 47 };

        Console.WriteLine("===============================================================");
        Console.WriteLine("   VERIFICACIÓN INTEGRAL DE TODOS LOS TIPOS DE e-CF (XSD)");
        Console.WriteLine("===============================================================");

        foreach (var type in typesToTest)
        {
            Console.WriteLine($"\n--- Probando tipo e-CF {type} ---");
            TestEcfType(service, type);
        }
    }

    static void TestEcfType(EcfGeneratorService service, int type)
    {
        // 1. Prueba Estándar (Sin ISC)
        var standardDto = CreateBaseDto(type, false);
        Console.Write($" [Estándar] Validando... ");
        RunTest(service, standardDto, type);

        // 2. Prueba con ISC (Solo si es tipo 31, 32, 33, 34 que son los más comunes para este impuesto)
        // En teoría el XSD permite ISC en otros, pero validaremos principalmente estos.
        if (type <= 34)
        {
            var iscDto = CreateBaseDto(type, true);
            Console.Write($" [Con ISC]  Validando... ");
            RunTest(service, iscDto, type);
        }
    }

    static EcfInvoiceRequestDto CreateBaseDto(int type, bool withIsc)
    {
        var ncfPrefix = $"E{type}";
        var ncf = $"{ncfPrefix}0000000001";

        var dto = new EcfInvoiceRequestDto
        {
            Ncf = ncf,
            IssuerRnc = "101000001",
            IssuerName = "EMPRESA DE PRUEBA SAS",
            IssuerAddress = "Calle Central 45, Santo Domingo",
            IssuerPhone = "809-555-0000",
            CustomerRnc = "101683457",
            CustomerName = "CLIENTE FINAL PRUEBA",
            CustomerAddress = "Av. Winston Churchill",
            IssueDate = DateTime.UtcNow,
            IncomeType = "01",
            PaymentType = 1,
            Items = new List<EcfItemRequestDto>
            {
                new EcfItemRequestDto
                {
                    Name = "Item de Prueba",
                    Quantity = 2,
                    UnitPrice = 100,
                    TaxPercentage = (type == 43) ? 0 : 18 // Gasto menor suele ser exento
                }
            }
        };

        // ── Reference for NC / ND ──────────────────────────────────────────
        if (type == 33 || type == 34)
        {
            dto.ReferenceNcf = "E310000000005";
            dto.ReferenceIssueDate = DateTime.UtcNow.AddDays(-5);
            dto.ReferenceReasonCode = 3; // Corrige montos
        }

        if (withIsc)
        {
            var item = dto.Items[0];
            item.IscType = "006"; // Cerveza
            item.AdditionalTaxRate = 10.00m;
            item.IscSpecificAmount = 10.00m;
        }

        return dto;
    }


    static void RunTest(EcfGeneratorService service, EcfInvoiceRequestDto dto, int type)
    {
        try
        {
            var xml = service.GenerateUnsignedXml(dto);
            
            // Guardar para inspección
            var filename = $"temp_ecf_{type}.xml";
            if (dto.Items.Count > 0 && !string.IsNullOrWhiteSpace(dto.Items[0].IscType))
                filename = $"temp_ecf_{type}_isc.xml";
            
            File.WriteAllText(filename, xml);

            var xsdErrors = service.ValidateXmlAgainstSchema(xml, type);

            if (xsdErrors.Count == 0)
            {
                Console.WriteLine("✅ OK");
            }
            else
            {
                Console.WriteLine($"❌ ERROR (Ver {filename})");
                foreach (var err in xsdErrors)
                {
                    Console.WriteLine($"   - {err}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 EXCEPTION: {ex.Message}");
        }
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
