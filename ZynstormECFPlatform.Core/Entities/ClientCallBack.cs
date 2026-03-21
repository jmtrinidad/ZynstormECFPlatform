using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCallBack : IEntityMarker
{
    public int ClientCallBackId { get; set; }

    public int ClientId { get; set; }

    public int? ApiKeyId { get; set; }

    public int? ClientBrancheId { get; set; }

    public string Url { get; set; } = null!;

    public string? Secret { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual ApiKey? ApiKey { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ClientBranche? ClientBranche { get; set; }
}