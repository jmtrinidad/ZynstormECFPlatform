using System;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ReceivedB2BMessage : BaseEntity
{
    public int ReceivedB2BMessageId { get; set; }

    public int ClientId { get; set; }

    public MessageType MessageType { get; set; }

    public string RncEmisor { get; set; } = string.Empty;

    public string RncComprador { get; set; } = string.Empty;

    public string ENcf { get; set; } = string.Empty;

    public string RawXml { get; set; } = string.Empty;

    public DateTime ReceivedAtUtc { get; set; }

    public virtual Client Client { get; set; } = null!;
}
