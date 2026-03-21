using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfXmlDocument : IEntityMarker
{
    public int EcfXmlDocumentId { get; set; }

    public int EcfDocumentId { get; set; }

    public string XmlUnsigned { get; set; } = null!;

    public string XmlSigned { get; set; } = null!;

    public DateTime? CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}