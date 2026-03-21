using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientCertificate : IEntityMarker
{
    public int ClientCertificateId { get; set; }

    public int ClientId { get; set; }

    public string Certificate { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Thumbprint { get; set; }

    public DateTime? ExpirationDateUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual Client Client { get; set; } = null!;
}