namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfXmlDocument : BaseEntity
{
    public int EcfXmlDocumentId { get; set; }

    public int EcfDocumentId { get; set; }

    public string XmlUnsigned { get; set; } = null!;

    public string XmlSigned { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}