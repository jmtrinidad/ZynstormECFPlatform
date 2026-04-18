using System;
using System.Linq;
using MiniExcelLibs;

public class Program
{
    public static void Main()
    {
        string path = @"c:\Projects\ZynstormECFPlatform\133009889-16042026193727.xlsx";
        try 
        {
            var columns = MiniExcel.GetColumns(path);
            Console.WriteLine("Columns:");
            foreach (var col in columns)
            {
                Console.WriteLine($"- {col}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
