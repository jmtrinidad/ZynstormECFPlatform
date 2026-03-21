using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfType
{
    public int EcfTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = new List<EcfDocument>();
}
