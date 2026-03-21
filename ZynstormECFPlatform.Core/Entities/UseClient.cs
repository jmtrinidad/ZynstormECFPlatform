using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class UseClient
{
    public int ClientId { get; set; }

    public string UserId { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;
}
