using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public class NotificationType : BaseEntity
{
    public int NotificationTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<UserNotificationConfiguration> UserConfigurations { get; set; } = [];
}
