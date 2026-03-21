using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ApiKey : IEntityMarker
{
    public int ApiKeyId { get; set; }

    public int ClientId { get; set; }

    public int? ClientBrancheId { get; set; }

    public string Apikey { get; set; } = null!;

    public string? SecretKey { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = [];

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];
}