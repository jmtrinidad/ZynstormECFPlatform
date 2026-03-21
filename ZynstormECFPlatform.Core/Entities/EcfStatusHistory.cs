using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfStatusHistory : IEntityMarker
{
    public int EcfStatusHistoryId { get; set; }

    public int EcfDocumentId { get; set; }

    public int EcfStatusId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;

    public virtual EcfStatus EcfStatus { get; set; } = null!;
}