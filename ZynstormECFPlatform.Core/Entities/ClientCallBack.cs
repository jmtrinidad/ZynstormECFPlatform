using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCallBack
{
    public int ClientCallBackId { get; set; }

    public int ClientId { get; set; }

    public int? ApiKeyId { get; set; }

    public int? ClientBrancheId { get; set; }

    public string Url { get; set; } = null!;

    public string? Secret { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ApiKey? ApiKey { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ClientBranche? ClientBranche { get; set; }
}
