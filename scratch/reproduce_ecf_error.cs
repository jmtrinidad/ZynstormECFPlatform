using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using ZynstormECFPlatform.Services.Xml;

public class Program
{
    public static void Main()
    {
        var item = new EcfXmlItem
        {
            EcfType = 44,
            NumeroLinea = 1,
            IndicadorFacturacion = 1,
            Name = "Item de Prueba",
            ItemType = 1,
            CantidadItem = 1,
            PrecioUnitarioItem = 100,
            MontoItem = 100,
            Retencion = new EcfXmlItemRetencion
            {
                Indicador = 1,
                MontoISRRetenido = 10
            }
        };

        var serializer = new XmlSerializer(typeof(EcfXmlItem));
        var settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true };
        using var stringWriter = new StringWriter();
        using (var writer = XmlWriter.Create(stringWriter, settings))
        {
            serializer.Serialize(writer, item);
        }

        Console.WriteLine("Serialized XML for EcfType 44 (Should NOT have Retencion):");
        Console.WriteLine(stringWriter.ToString());
    }
}
