using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Services.Notifications;

namespace ZynstormECFPlatform.Tests.Users;

public class NotificationSubscriptionPolicyTests
{
    private static User UserWith(params (int TypeId, bool Enabled)[] configs)
    {
        var user = new User();
        foreach (var (typeId, enabled) in configs)
        {
            user.UserNotificationConfigurations.Add(new UserNotificationConfiguration
            {
                NotificationTypeId = typeId,
                IsEnabled = enabled
            });
        }
        return user;
    }

    [Fact]
    public void UserWithoutConfiguration_IsNotSubscribed()
    {
        // La pantalla de Notificaciones muestra "apagado" cuando no hay fila guardada.
        Assert.False(NotificationSubscriptionPolicy.IsSubscribed(UserWith(), NotificationSubscriptionPolicy.DailyReport));
    }

    [Fact]
    public void UserWithDisabledConfiguration_IsNotSubscribed()
    {
        var user = UserWith((NotificationSubscriptionPolicy.DailyReport, false));
        Assert.False(NotificationSubscriptionPolicy.IsSubscribed(user, NotificationSubscriptionPolicy.DailyReport));
    }

    [Fact]
    public void UserWithEnabledConfiguration_IsSubscribed()
    {
        var user = UserWith((NotificationSubscriptionPolicy.DailyReport, true));
        Assert.True(NotificationSubscriptionPolicy.IsSubscribed(user, NotificationSubscriptionPolicy.DailyReport));
    }

    [Fact]
    public void EnabledConfigurationOfAnotherType_DoesNotSubscribe()
    {
        var user = UserWith((NotificationSubscriptionPolicy.WeeklyReport, true));
        Assert.False(NotificationSubscriptionPolicy.IsSubscribed(user, NotificationSubscriptionPolicy.DailyReport));
    }

    [Fact]
    public void TypeIdsMatchSeededNotificationTypes()
    {
        Assert.Equal(1, NotificationSubscriptionPolicy.InvoiceAccepted);
        Assert.Equal(2, NotificationSubscriptionPolicy.InvoiceRejected);
        Assert.Equal(3, NotificationSubscriptionPolicy.DailyReport);
        Assert.Equal(4, NotificationSubscriptionPolicy.WeeklyReport);
    }
}
