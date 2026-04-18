using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.Linq;

public class Program {
    public static void Main() {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try {
            var sheetNames = MiniExcel.GetSheetNames(path);
            Console.WriteLine("START_SHEETS");
            foreach (var name in sheetNames) {
                Console.WriteLine(name);
            }
            Console.WriteLine("END_SHEETS");
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
