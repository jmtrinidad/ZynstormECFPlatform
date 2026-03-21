using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Client
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string Rnc { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = new List<ClientBranche>();

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = new List<ClientCallBack>();

    public virtual ICollection<ClientCertificate> ClientCertificates { get; set; } = new List<ClientCertificate>();

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = new List<EcfDocument>();

    public virtual Status Status { get; set; } = null!;
}
