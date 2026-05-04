using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public class UserNotificationConfiguration : BaseEntity
{
    public int UserNotificationConfigurationId { get; set; }

    public string UserId { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public int NotificationTypeId { get; set; }

    public virtual NotificationType NotificationType { get; set; } = null!;

    public bool IsEnabled { get; set; } = true;
}