using System;
using System.Collections.Generic;
using System.Linq;
using MiniExcelLibs;

namespace Scratch
{
    public class Program
    {
        public static void Main()
        {
            try {
                string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
                var ecfRows = MiniExcel.Query(path, sheetName: "ECF", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
                
                // Print IndicadorMontoGravado for all rows in ECF
                for(int i=0; i<ecfRows.Count; i++) {
                    var row = ecfRows[i];
                    var ncf = row.ContainsKey("ENCF") ? row["ENCF"]?.ToString() : "";
                    var val = row.ContainsKey("IndicadorMontoGravado") ? row["IndicadorMontoGravado"] : "N/A";
                    var caseNo = row.ContainsKey("CasoPrueba") ? row["CasoPrueba"]?.ToString() : "N/A";
                    
                    Console.WriteLine($"Row: {i}, Case: {caseNo}, NCF: {ncf}, IndicadorMontoGravado: '{val}'");
                }
            } catch (Exception ex) {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
    }
}
