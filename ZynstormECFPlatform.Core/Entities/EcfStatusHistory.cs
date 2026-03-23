namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfStatusHistory : BaseEntity
{
    public int EcfStatusHistoryId { get; set; }

    public int EcfDocumentId { get; set; }

    public int EcfStatusId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;

    public virtual EcfStatus EcfStatus { get; set; } = null!;
}