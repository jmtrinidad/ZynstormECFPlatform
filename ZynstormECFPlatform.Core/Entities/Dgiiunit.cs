using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class DGIIUnit : IEntityMarker
{
    public int DGIIUnitId { get; set; }

    public int DGIICode { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }
}