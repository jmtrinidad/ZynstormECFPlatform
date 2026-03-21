using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfStatus : IEntityMarker
{
    public int EcfStatusId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];

    public virtual ICollection<EcfStatusHistory> EcfStatusHistories { get; set; } = [];

    public virtual ICollection<EcfTransmission> EcfTransmissions { get; set; } = [];
}