using System;
using System.IO;
using System.Xml.Serialization;

[XmlRoot("Test")]
public class TestXml
{
    public decimal? Value1 { get; set; }
    public decimal? Value2 { get; set; }
}

class Program
{
    static void Main()
    {
        var obj = new TestXml {
            Value1 = 0m,
            Value2 = 0.00m
        };

        var serializer = new XmlSerializer(typeof(TestXml));
        using var writer = new StringWriter();
        serializer.Serialize(writer, obj);
        Console.WriteLine(writer.ToString());
    }
}
