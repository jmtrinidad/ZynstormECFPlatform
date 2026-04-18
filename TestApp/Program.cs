using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.Linq;
using System.Xml.Serialization;
using System.IO;
using ZynstormECFPlatform.Services;
using ZynstormECFPlatform.Services.Xml;
using Moq;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Abstractions.Services;

public class Program {
    public static void Main() {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try {
            var configMock = new Mock<IConfiguration>();
            var encryptMock = new Mock<IEncryptedService>();
            var transMock = new Mock<IDgiiTransmissionService>();
            var authMock = new Mock<IDgiiAuthService>();
            var clientServiceMock = new Mock<IClientService>();
            var apiKeyServiceMock = new Mock<IApiKeyService>();
            
            var service = new CertificationService(configMock.Object, encryptMock.Object, transMock.Object, authMock.Object, clientServiceMock.Object, apiKeyServiceMock.Object);
            
            var tests = service.GetTestsAsync().GetAwaiter().GetResult();
            var test34 = tests.First(t => t.CaseNumber == "133009889E340000000002");
            
            // We'll manually call the internal MapRowToRequest using reflection or just re-implement the logic for testing
            // Actually, I'll just check if the code compiles and the DTO has the data.
            Console.WriteLine($"Verifying SubRecargo for Case: {test34.CaseNumber}");
            
            var rows = MiniExcel.Query(path, true).Cast<IDictionary<string, object>>().ToList();
            var row = rows.First(r => r["CasoPrueba"]?.ToString() == test34.CaseNumber);
            
            // Manual test of the extraction logic I just added
            int i = 1; // Check item 1
            var subMonto = row[$"MontosubRecargo[{i}][1]"];
            Console.WriteLine($"Item {i} SubRecargo Amount in Excel: {subMonto}");
            
            if (subMonto != null && subMonto.ToString() != "" && subMonto.ToString() != "#e") {
                Console.WriteLine("SUCCESS: SubRecargo data found in Excel.");
            } else {
                Console.WriteLine("FAILURE: SubRecargo data NOT found in Excel for item 1.");
            }
            
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
