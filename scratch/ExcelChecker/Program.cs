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
                var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
                var r = rows.FirstOrDefault(x => (x["ENCF"]?.ToString() ?? "") == "E410000000001");
                if (r != null)
                {
                    foreach (var kv in r)
                    {
                        Console.WriteLine($"{kv.Key}: {kv.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
}
