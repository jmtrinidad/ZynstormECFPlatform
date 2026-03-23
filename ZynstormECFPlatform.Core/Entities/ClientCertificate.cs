using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCertificate : BaseEntity
{
    public int ClientCertificateId { get; set; }

    public int ClientId { get; set; }

    public string Certificate { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Thumbprint { get; set; }

    public DateTime? ExpirationDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Client Client { get; set; } = null!;
}