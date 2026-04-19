using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniExcelLibs;
using Newtonsoft.Json;

namespace TestVerifier
{
    class Program
    {
        static void Main(string[] args)
        {
            string excelPath = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
            var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            
            // Simulating target Index 15 (which is E450000000003 as per my research)
            var targetENCF = "E450000000003";
            var row = rows.First(r => (r["ENCF"]?.ToString() ?? "").Contains(targetENCF));
            
            Console.WriteLine($"ENCF: {row["ENCF"]}");
            
            for (int i = 1; i <= 3; i++)
            {
                var elab = GetStr(row, $"FechaElaboracion[{i}]");
                var venc = GetStr(row, $"FechaVencimientoItem[{i}]");
                if (elab != null || venc != null)
                {
                    Console.WriteLine($"Item {i}: Elaboracion={elab}, Vencimiento={venc}");
                }
            }
        }

        private static string? GetStr(IDictionary<string, object> row, string key)
        {
            if (!row.ContainsKey(key)) return null;
            var val = row[key]?.ToString();
            return string.IsNullOrWhiteSpace(val) || val == "#e" ? null : val.Trim();
        }
    }
}
