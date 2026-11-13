using System.Linq;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Notifications;

/// <summary>
/// Decide si un usuario recibe un tipo de notificación por correo.
/// Es opt-in: sin fila de configuración guardada el usuario NO está suscrito,
/// igual que muestra la pantalla de Configuraciones > Notificaciones.
/// </summary>
public static class NotificationSubscriptionPolicy
{
    // Deben coincidir con el seed de NotificationType en StorageContext.
    public const int InvoiceAccepted = 1;
    public const int InvoiceRejected = 2;
    public const int DailyReport = 3;
    public const int WeeklyReport = 4;

    public static bool IsSubscribed(User user, int notificationTypeId)
    {
        var config = user.UserNotificationConfigurations
            .FirstOrDefault(c => c.NotificationTypeId == notificationTypeId);
        return config is { IsEnabled: true };
    }
}
