using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.Linq;

public class Program {
    public static void Main() {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try {
            var rows = MiniExcel.Query(path, true);
            var case34 = rows.Where(r => {
                var d = (IDictionary<string, object>)r;
                return d["ENCF"]?.ToString() == "E340000000002" || d["ENCF"]?.ToString() == "E34000000002";
            }).FirstOrDefault() as IDictionary<string, object>;
            
            if (case34 != null) {
                Console.WriteLine("START_DATA");
                foreach (var kvp in case34) {
                   Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine("END_DATA");
            } else {
                Console.WriteLine("No case E340000000002 found.");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
