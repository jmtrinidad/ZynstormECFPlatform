using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface INotificationService
{
    Task<IEnumerable<NotificationType>> GetNotificationTypesAsync();
    Task<IEnumerable<UserNotificationConfiguration>> GetUserSettingsAsync(string userId);
    Task UpdateUserSettingsAsync(string userId, IEnumerable<UserNotificationConfiguration> configurations);
}
