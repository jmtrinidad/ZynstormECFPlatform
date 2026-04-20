using System;
using System.Collections.Generic;
using System.Linq;
using MiniExcelLibs;

namespace InspectExcel
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string filePath = @"c:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Web.Api\wwwroot\certification_files\suite_f3813f14.xlsx";
                var rows = MiniExcel.Query(filePath).Cast<IDictionary<string, object>>().ToList();
                
                Console.WriteLine($"Total rows: {rows.Count}");
                foreach(var row in rows)
                {
                    string idx = GetStr(row, "Indice") ?? GetStr(row, "Índice") ?? GetStr(row, "Index") ?? "?";
                    string caso = GetStr(row, "CasoPrueba") ?? GetStr(row, "Caso Prueba") ?? GetStr(row, "Caso de Prueba") ?? GetStr(row, "Escenario") ?? "?";
                    string type = GetStr(row, "TipoeCF") ?? "?";
                    
                    Console.WriteLine($"Idx: {idx} | Type: {type} | Caso: {caso}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static string GetStr(IDictionary<string, object> row, string key)
        {
            if (row.TryGetValue(key, out var val) && val != null) return val.ToString().Trim();
            return null;
        }
    }
}
