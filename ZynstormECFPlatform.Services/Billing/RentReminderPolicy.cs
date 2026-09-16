namespace ZynstormECFPlatform.Services.Billing;

public enum RentReminderAction
{
    None,
    FirstReminder,
    FinalReminder,
    Suspend
}

/// <summary>
/// Decide qué hacer con un cliente de renta cuyo pago no se ha registrado. Función pura.
/// Con pago el día D y g días de gracia: D+1 primer aviso, D+g último aviso, D+g+1 suspensión.
/// Cada aviso se envía una sola vez por fecha de pago y una corrida aplica una sola acción.
/// </summary>
public static class RentReminderPolicy
{
    public const int DefaultGraceDays = 3;
    public const int MaxGraceDays = 30;

    public static int NormalizeGraceDays(int graceDays) =>
        graceDays is >= 1 and <= MaxGraceDays ? graceDays : DefaultGraceDays;

    /// <summary>Días calendario desde la fecha de pago; negativo si todavía no vence.</summary>
    public static int GetDaysOverdue(DateTime nextPaymentDate, DateTime today) =>
        (today.Date - nextPaymentDate.Date).Days;

    public static DateTime GetDeadline(DateTime nextPaymentDate, int graceDays) =>
        nextPaymentDate.Date.AddDays(NormalizeGraceDays(graceDays));

    public static RentReminderAction Decide(
        DateTime? nextPaymentDate,
        int graceDays,
        DateTime? firstSentFor,
        DateTime? finalSentFor,
        bool hasEmail,
        bool isSuspended,
        DateTime today)
    {
        if (nextPaymentDate is not DateTime due || isSuspended)
            return RentReminderAction.None;

        var grace = NormalizeGraceDays(graceDays);
        var overdue = GetDaysOverdue(due, today);
        if (overdue < 1)
            return RentReminderAction.None;

        var firstSent = firstSentFor?.Date == due.Date;
        var finalSent = finalSentFor?.Date == due.Date;

        // Se suspende solo si el último aviso salió en una corrida anterior (o no hay a quién avisar).
        if (overdue > grace && (finalSent || !hasEmail))
            return RentReminderAction.Suspend;

        if (overdue >= grace)
            return finalSent ? RentReminderAction.None : RentReminderAction.FinalReminder;

        return firstSent || finalSent ? RentReminderAction.None : RentReminderAction.FirstReminder;
    }
}
