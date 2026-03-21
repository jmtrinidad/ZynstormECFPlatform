using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Common;

public interface IEntityMarker
{
    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; }

    [ConcurrencyCheck]
    public DateTime? LastUpdateUtc { get; set; }
}