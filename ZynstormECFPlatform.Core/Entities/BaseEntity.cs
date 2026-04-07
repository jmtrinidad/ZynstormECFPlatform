using System.ComponentModel.DataAnnotations;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public class BaseEntity : IEntityMarker
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    [ConcurrencyCheck]
    public DateTime? LastUpdateUtc { get; set; }

    public DateTime RegisteredAt { get; set; }
}