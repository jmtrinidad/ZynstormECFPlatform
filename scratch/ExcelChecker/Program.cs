
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
                
                var firstRow = rows.FirstOrDefault();
                if (firstRow != null)
                {
                    Console.WriteLine("--- RELEVANT COLUMNS FOR TOTALS ---");
                    var keys = firstRow.Keys.ToList();
                    string[] targets = { "Monto", "Total", "ITBIS", "Pagar", "Periodo", "Gravado", "Exento", "Indicador" };
                    foreach (var key in keys)
                    {
                        if (targets.Any(t => key.Contains(t, StringComparison.OrdinalIgnoreCase)))
                        {
                            Console.WriteLine(key);
                        }
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
