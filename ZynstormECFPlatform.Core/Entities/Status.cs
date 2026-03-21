using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Status
{
    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = new List<ClientBranche>();

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
