using System.Xml.Serialization;

namespace ZynstormECFPlatform.Services.Xml
{
    public class EcfXmlInformacionReferencia
    {
        [XmlElement("NCFModificado")]
        public string NCFModificado { get; set; }

        [XmlElement("RNCOtroContribuyente")]
        public string? RNCOtroContribuyente { get; set; }

        [XmlElement("FechaNCFModificado")]
        public string FechaNCFModificado { get; set; }

        [XmlElement("CodigoModificacion")]
        public int CodigoModificacion { get; set; }

        [XmlElement("RazonModificacion")]
        public string? RazonModificacion { get; set; }
    }
}
