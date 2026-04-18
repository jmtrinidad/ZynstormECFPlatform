using System;
using System.Collections.Generic;
using MiniExcelLibs;
using System.IO;

public class Program
{
    public static void Main()
    {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try 
        {
            var columns = MiniExcel.GetColumns(path);
            Console.WriteLine("START_COLUMNS");
            foreach (var col in columns)
            {
                Console.WriteLine(col);
            }
            Console.WriteLine("END_COLUMNS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
