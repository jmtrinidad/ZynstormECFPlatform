using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCertificate
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
