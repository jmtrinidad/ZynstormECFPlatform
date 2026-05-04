using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<NotificationType> _notificationTypeRepository;
    private readonly IRepository<UserNotificationConfiguration> _userConfigRepository;

    public NotificationService(
        IRepository<NotificationType> notificationTypeRepository,
        IRepository<UserNotificationConfiguration> userConfigRepository)
    {
        _notificationTypeRepository = notificationTypeRepository;
        _userConfigRepository = userConfigRepository;
    }

    public async Task<IEnumerable<NotificationType>> GetNotificationTypesAsync()
    {
        return await _notificationTypeRepository.GetAllAsync();
    }

    public async Task<IEnumerable<UserNotificationConfiguration>> GetUserSettingsAsync(string userId)
    {
        return await _userConfigRepository.GetManyByAsync(x => x.UserId == userId);
    }

    public async Task UpdateUserSettingsAsync(string userId, IEnumerable<UserNotificationConfiguration> configurations)
    {
        // Get existing settings
        var existingSettings = await _userConfigRepository.GetManyByAsync(x => x.UserId == userId);

        foreach (var config in configurations)
        {
            var existing = existingSettings.FirstOrDefault(x => x.NotificationTypeId == config.NotificationTypeId);
            if (existing != null)
            {
                existing.IsEnabled = config.IsEnabled;
            }
            else
            {
                config.UserId = userId;
                config.GuidId = Guid.NewGuid().ToString();
                config.RegisteredAt = DateTime.UtcNow;
                _userConfigRepository.Add(config);
            }
        }

        await _userConfigRepository.SaveChangesAsync();
    }
}
