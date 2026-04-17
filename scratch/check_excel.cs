using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using MiniExcelLibs;

class Program {
    static void Main() {
        string excelPath = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        if (!File.Exists(excelPath)) {
            Console.WriteLine("File not found");
            return;
        }
        
        var rows = MiniExcel.Query(excelPath).Cast<IDictionary<string, object>>().ToList();
        if (rows.Count == 0) {
            Console.WriteLine("No rows found");
            return;
        }
        
        var firstRow = rows[0];
        Console.WriteLine("Columns found:");
        foreach (var key in firstRow.Keys) {
            Console.WriteLine($"- {key}");
        }
        
        Console.WriteLine("\nFirst row values:");
        foreach (var kvp in firstRow) {
            Console.WriteLine($"{kvp.Key}: {kvp.Value}");
        }
    }
}
