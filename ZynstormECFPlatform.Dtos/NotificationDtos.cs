namespace ZynstormECFPlatform.Dtos;

public class NotificationTypeDto
{
    public int NotificationTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

public class UserNotificationConfigDto
{
    public int NotificationTypeId { get; set; }
    public bool IsEnabled { get; set; }
}

public class UpdateNotificationSettingsDto
{
    public List<UserNotificationConfigDto> Configurations { get; set; } = [];
}
