using System;
using System.Collections.Generic;
using System.Linq;
using ZynstormECFPlatform.Services;
using ZynstormECFPlatform.Services.Xml;
using ZynstormECFPlatform.Dtos;
using MiniExcelLibs;

namespace Scratch
{
    public class Program
    {
        public static void Main()
        {
            try {
                // I need the generator logic
                // Since I cannot easily instantiate everything, I'll simulate MapRowToRequest for Index 25
                string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
                var rfceRows = MiniExcel.Query(path, sheetName: "RFCE", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
                
                var row25 = rfceRows[0]; // NCF 14
                
                Console.WriteLine("NCF: " + (row25.ContainsKey("ENCF") ? row25["ENCF"] : "N/A"));
                Console.WriteLine("Has IndicadorMontoGravado: " + row25.ContainsKey("IndicadorMontoGravado"));
                
                // If I use the logic from CertificationService.cs
                var val = row25.ContainsKey("IndicadorMontoGravado") ? row25["IndicadorMontoGravado"]?.ToString() : null;
                int? img = int.TryParse(val, out int i) ? i : null;
                
                Console.WriteLine("ManualIndicadorMontoGravado: " + (img == null ? "null" : img.ToString()));

            } catch (Exception ex) {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
