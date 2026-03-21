using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class SystemLog : IEntityMarker
{
    public int SystemLogId { get; set; }

    public int ClientId { get; set; }

    public int EcfDocumentId { get; set; }

    public string LogLevel { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreateAtUtc { get; set; }

    public string? Exception { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}