using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services;
using ZynstormECFPlatform.Services.Xml;

public class Program {
    public static void Main() {
        var s = new EcfGeneratorService();
        
        // CASE 1: Type 41 (Comprobante de Compras) 
        // Should DEFAULT PaymentType to 1 if null, because XSD 41 requires it.
        var d41 = new EcfInvoiceRequestDto {
            Ncf = "E410000000001",
            IssueDate = DateTime.Now,
            IssuerRnc = "101000001", IssuerName = "A", IssuerAddress = "A",
            CustomerRnc = "101000001", CustomerName = "A",
            Items = new List<EcfItemRequestDto> { new EcfItemRequestDto { Name = "Item", Quantity = 1, UnitPrice = 100 } }
        };
        Console.WriteLine("--- Type 41 (Should have TipoPago=1) ---");
        Console.WriteLine(s.GenerateUnsignedXml(d41));

        // CASE 2: Type 43 (Gastos Menores)
        // Should OMIT PaymentType and IncomeType if null, as per your request to fix certification.
        var d43 = new EcfInvoiceRequestDto {
            Ncf = "E430000000001",
            IssueDate = DateTime.Now,
            IssuerRnc = "101000001", IssuerName = "A", IssuerAddress = "A",
            CustomerRnc = "101000001", CustomerName = "A",
            Items = new List<EcfItemRequestDto> { new EcfItemRequestDto { Name = "Item", Quantity = 1, UnitPrice = 100 } }
        };
        Console.WriteLine("\n--- Type 43 (Should OMIT TipoPago and TipoIngresos) ---");
        Console.WriteLine(s.GenerateUnsignedXml(d43));
    }


}
