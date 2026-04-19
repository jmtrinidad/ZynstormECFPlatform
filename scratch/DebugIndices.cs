
using System;
using System.Collections.Generic;
using System.Linq;
using MiniExcelLibs;

public class DebugIndices
{
    public static void Main()
    {
        var excelPath = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        
        var ecfRows = MiniExcel.Query(excelPath, sheetName: "ECF", useHeaderRow: true)
            .Cast<IDictionary<string, object>>().ToList();
        var rfceRows = MiniExcel.Query(excelPath, sheetName: "RFCE", useHeaderRow: true)
            .Cast<IDictionary<string, object>>().ToList();

        int index = 0;
        
        var standardRows = ecfRows
            .Where(r => GetStr(r, "TipoeCF") != "32" || (GetDec(r, "MontoTotal") ?? 0) >= 250000)
            .ToList();
        
        Console.WriteLine($"Standard Rows: {standardRows.Count}");
        foreach(var r in standardRows) 
        {
            Console.WriteLine($"Index {index++}: {GetStr(r, "ENCF")} (Step 1/2)");
        }

        Console.WriteLine($"RFCE Rows: {rfceRows.Count}");
        foreach(var r in rfceRows)
        {
            Console.WriteLine($"Index {index++}: {GetStr(r, "ENCF")} (Step 3)");
        }

        var performanceRows = ecfRows
            .Where(r => GetStr(r, "TipoeCF") == "32" && (GetDec(r, "MontoTotal") ?? 0) < 250000)
            .ToList();
        Console.WriteLine($"Performance Rows: {performanceRows.Count}");
        foreach(var r in performanceRows)
        {
            Console.WriteLine($"Index {index++}: {GetStr(r, "ENCF")} (Step 4)");
        }
    }

    static string GetStr(IDictionary<string, object> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val == null) return null;
        string s = val.ToString().Trim();
        if (s.Equals("#e", StringComparison.OrdinalIgnoreCase) || s.Equals("#n/a", StringComparison.OrdinalIgnoreCase)) return null;
        return s;
    }

    static decimal? GetDec(IDictionary<string, object> row, string key)
    {
        if (!row.TryGetValue(key, out var val) || val == null) return null;
        string s = val.ToString().Trim();
        if (s.Equals("#e", StringComparison.OrdinalIgnoreCase) || s.Equals("#n/a", StringComparison.OrdinalIgnoreCase)) return null;
        if (decimal.TryParse(s, out decimal d)) return d;
        return null;
    }
}
