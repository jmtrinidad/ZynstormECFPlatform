using System;
using System.IO;
using System.Xml;
using System.Xml.Schema;

class TestProg
{
    static void Main()
    {
        string xsdPath = @"C:\Projects\ZynstormECFPlatform\ZynstormECFPlatform.Schemas\XSD\ARECF v1.0.xsd";
        string xmlContent = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ARECF><DetalleAcusedeRecibo><Version>1.0</Version><RNCEmisor>131880600</RNCEmisor><RNCComprador>132880600</RNCComprador><eNCF>E310000000001</eNCF><Estado>0</Estado><FechaHoraAcuseRecibo>29-04-2026 13:43:02</FechaHoraAcuseRecibo></DetalleAcusedeRecibo></ARECF>";

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas.Add("", xsdPath);
        settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings | XmlSchemaValidationFlags.ProcessIdentityConstraints;

        settings.ValidationEventHandler += (sender, args) =>
        {
            Console.WriteLine($"Error: {args.Message}");
        };

        using (StringReader sr = new StringReader(xmlContent))
        using (XmlReader reader = XmlReader.Create(sr, settings))
        {
            try
            {
                while (reader.Read()) { }
                Console.WriteLine("Validation finished.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}
