using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Core.Entities;

public class UserAccessLog : BaseEntity
{
    public int UserAccessLogId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime AccessTimeUtc { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public virtual User User { get; set; } = null!;
}