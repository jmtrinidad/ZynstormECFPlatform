using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.Linq;

public class Program {
    public static void Main() {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try {
            var rows = MiniExcel.Query(path, true).Cast<IDictionary<string, object>>().ToList();
            var exists = rows.Any(r => r["ENCF"]?.ToString() == "E410000000001");
            Console.WriteLine($"ENCF E410000000001 exists: {exists}");
            
            var rowCase15 = rows.Where(r => r["ENCF"]?.ToString() == "E340000000015").FirstOrDefault();
            if (rowCase15 != null) {
                Console.WriteLine($"Case 15 Ref: '{rowCase15["NCFModificado"]}'");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
