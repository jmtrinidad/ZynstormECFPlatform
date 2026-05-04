using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public class User : IdentityUser, IEntityMarker
{
    [MaxLength(50)]
    public string FirstName { get; set; } = default!;

    [MaxLength(50)]
    public string LastName { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = Guid.NewGuid().ToString();

    [ConcurrencyCheck]
    public DateTime? LastUpdateUtc { get; set; }

    public DateTime RegisteredAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? LastAccessUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<UserClient> UserClients { get; set; } = [];

    public virtual ICollection<UserAccessLog> UserAccessLogs { get; set; } = [];

    public virtual ICollection<UserAuditLog> UserAuditLogs { get; set; } = [];

    public virtual ICollection<UserNotificationConfiguration> UserNotificationConfigurations { get; set; } = [];
}