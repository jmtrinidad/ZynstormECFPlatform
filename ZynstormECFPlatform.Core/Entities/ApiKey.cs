using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ApiKey
{
    public int ApiKeyId { get; set; }

    public int ClientId { get; set; }

    public int? ClientBrancheId { get; set; }

    public string Apikey { get; set; } = null!;

    public string? SecretKey { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = new List<ClientCallBack>();

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = new List<EcfDocument>();

    public virtual Status Status { get; set; } = null!;
}