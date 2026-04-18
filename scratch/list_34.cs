using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.Linq;

public class Program {
    public static void Main() {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try {
            var rows = MiniExcel.Query(path, true);
            var cases34 = rows.Where(r => {
                var d = (IDictionary<string, object>)r;
                return d["TipoeCF"]?.ToString() == "34";
            }).ToList();
            
            Console.WriteLine("START_LIST");
            foreach (var r in cases34) {
                var d = (IDictionary<string, object>)r;
                Console.WriteLine($"ENCF: {d["ENCF"]}, NCFModificado: {d["NCFModificado"]}, FechaNCFModificado: {d["FechaNCFModificado"]}");
            }
            Console.WriteLine("END_LIST");
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
