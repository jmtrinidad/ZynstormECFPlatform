using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class UseClient : IEntityMarker
{
    public int ClientId { get; set; }

    public string UserId { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}