using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfXmlDocument
{
    public int EcfXmlDocumentId { get; set; }

    public int EcfDocumentId { get; set; }

    public string XmlUnsigned { get; set; } = null!;

    public string XmlSigned { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}
