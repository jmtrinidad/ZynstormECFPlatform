
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniExcelLibs;

namespace ExcelInspector
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string excelPath = "c:\\Projects\\ZynstormECFPlatform\\133009889-16042026193727.xlsx";
                if (!File.Exists(excelPath))
                {
                    Console.WriteLine("File not found: " + excelPath);
                    return;
                }

                var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
                
                var expirationLog = new Dictionary<string, List<string>>();

                foreach (var row in rows)
                {
                    var type = row.ContainsKey("TipoeCF") ? row["TipoeCF"]?.ToString() : "Unknown";
                    var date = row.ContainsKey("FechaVencimientoSecuencia") ? row["FechaVencimientoSecuencia"]?.ToString() : "Missing";
                    
                    if (string.IsNullOrEmpty(type)) continue;

                    if (!expirationLog.ContainsKey(type)) expirationLog[type] = new List<string>();
                    expirationLog[type].Add(date ?? "null");
                }

                Console.WriteLine("Sequence Expiration Dates by EcfType:");
                foreach (var kvp in expirationLog)
                {
                    Console.WriteLine($"Type {kvp.Key}: {string.Join(", ", kvp.Value.Distinct())}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
