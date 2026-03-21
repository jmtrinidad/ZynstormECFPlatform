using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfStatus
{
    public int EcfStatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = new List<EcfDocument>();

    public virtual ICollection<EcfStatusHistory> EcfStatusHistories { get; set; } = new List<EcfStatusHistory>();

    public virtual ICollection<EcfTransmission> EcfTransmissions { get; set; } = new List<EcfTransmission>();
}
