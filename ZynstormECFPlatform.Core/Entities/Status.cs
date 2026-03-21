using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Status : IEntityMarker
{
    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = [];

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}