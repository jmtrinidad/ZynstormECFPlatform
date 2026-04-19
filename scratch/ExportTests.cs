using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniExcelLibs;
using Newtonsoft.Json;

namespace TestExporter
{
    class Program
    {
        static void Main(string[] args)
        {
            string excelPath = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
            var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            var rfceRows = MiniExcel.Query(excelPath, sheetName: "RFCE", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

            var referencedNcfs = rows
                .Select(r => r.ContainsKey("NCFModificado") ? r["NCFModificado"]?.ToString() : null)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var tests = new List<object>();
            int index = 0;

            foreach (var row in rows)
            {
                var ecfTypeStr = row.ContainsKey("TipoeCF") ? row["TipoeCF"]?.ToString() ?? "" : "";
                if (string.IsNullOrEmpty(ecfTypeStr)) continue;
                
                decimal total = 0;
                if (row.ContainsKey("MontoTotal")) decimal.TryParse(row["MontoTotal"]?.ToString(), out total);
                
                if (ecfTypeStr == "32" && total < 250000) continue;

                var encf = row.ContainsKey("ENCF") ? row["ENCF"]?.ToString() ?? "" : "";
                var test = new {
                    Index = index++,
                    Step = DetermineStep(ecfTypeStr, total, encf, referencedNcfs),
                    Type = ecfTypeStr,
                    ENCF = encf,
                    Case = row.ContainsKey("CasoPrueba") ? row["CasoPrueba"]?.ToString() ?? "" : ""
                };
                
                if ((int)test.GetType().GetProperty("Step").GetValue(test) is 1 or 2)
                    tests.Add(test);
            }

            foreach (var row in rfceRows)
            {
                var encf = row.ContainsKey("ENCF") ? row["ENCF"]?.ToString() ?? "" : "";
                var testCase = row.ContainsKey("CasoPrueba") ? row["CasoPrueba"]?.ToString() ?? "" : "";
                
                tests.Add(new { Index = index++, Step = 3, Type = "32", ENCF = encf, Case = testCase, Note = "Resumen" });
                tests.Add(new { Index = index++, Step = 4, Type = "32", ENCF = encf, Case = testCase, Note = "Individual" });
            }

            Console.WriteLine(JsonConvert.SerializeObject(tests, Formatting.Indented));
        }

        static int DetermineStep(string ecfTypeStr, decimal totalAmount, string encf, HashSet<string> referencedNcfs)
        {
            if (!int.TryParse(ecfTypeStr, out int type)) return 0;
            if (referencedNcfs.Contains(encf?.Trim())) return 1;
            if (type == 31 || type == 41 || (type >= 43 && type <= 47) || (type == 32 && totalAmount >= 250000)) return 1;
            if (type == 33 || type == 34) return 2;
            if (type == 32 && totalAmount < 250000) return 3;
            return 0;
        }
    }
}
