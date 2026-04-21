using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MiniExcelLibs;

namespace Scratch
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var filePath = @"C:\Projects\ZynstormECFPlatform\Aprobacion comerciar.xlsx";
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Excel file not found.");
                return;
            }

            var rows = MiniExcel.Query(filePath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            if (rows.Any())
            {
                Console.WriteLine("Headers found:");
                foreach (var key in rows.First().Keys)
                {
                    Console.WriteLine($"- {key}");
                }
                
                Console.WriteLine("\nSample Row 1:");
                foreach (var kvp in rows.First())
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
            }
        }
    }
}
