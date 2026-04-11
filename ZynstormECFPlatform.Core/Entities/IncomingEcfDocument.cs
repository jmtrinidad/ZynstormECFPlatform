using System;

namespace ZynstormECFPlatform.Core.Entities;

public partial class IncomingEcfDocument : BaseEntity
{
    public int IncomingEcfDocumentId { get; set; }

    public string RncEmisor { get; set; } = string.Empty;

    public string ENcf { get; set; } = string.Empty;

    public string TrackId { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }

    public string RawXml { get; set; } = string.Empty;

    public bool IsCommerciallyApproved { get; set; }
}
