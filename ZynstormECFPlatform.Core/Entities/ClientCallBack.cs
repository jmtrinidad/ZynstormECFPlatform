using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCallBack : BaseEntity
{
    public int ClientCallBackId { get; set; }

    public int ClientId { get; set; }

    public int? ApiKeyId { get; set; }

    public int? ClientBrancheId { get; set; }

    public string Url { get; set; } = null!;

    public string? Secret { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual ApiKey? ApiKey { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual ClientBranche? ClientBranche { get; set; }
}