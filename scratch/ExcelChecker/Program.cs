using System;

namespace ExcelInspector
{
    class Program
    {
        static void Main(string[] args)
        {
            string val = "0.00";
            if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal d))
            {
                Console.WriteLine($"Parsed: {d}");
                Console.WriteLine($"ToString(): {d.ToString()}");
                var obj = new TestXml { Value2 = d };
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(TestXml));
                using var writer = new System.IO.StringWriter();
                serializer.Serialize(writer, obj);
                Console.WriteLine(writer.ToString());
            }
        }
    }

    [System.Xml.Serialization.XmlRoot("Test")]
    public class TestXml
    {
        public decimal? Value2 { get; set; }
    }
}
