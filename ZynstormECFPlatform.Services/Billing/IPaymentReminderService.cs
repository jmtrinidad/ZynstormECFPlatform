namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentReminderResult(bool Success, string Message);

public sealed record PaymentReminderRunResult(int FirstReminders, int FinalReminders, int Suspended, int Pending);

public interface IPaymentReminderService
{
    /// <summary>Corrida diaria de avisos y resumen al correo administrativo. También recupera suspensiones pendientes.</summary>
    Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Suspende los clientes cuyo día de gracia ya terminó y que ya recibieron el último aviso.
    /// Se ejecuta separadamente de los correos para que el acceso se bloquee al comenzar el día de corte.
    /// </summary>
    Task<int> SuspendExpiredClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>Envío manual del aviso que corresponde a un cliente. Nunca suspende.</summary>
    Task<PaymentReminderResult> SendClientReminderAsync(string clientGuid, CancellationToken cancellationToken = default);

    /// <summary>Envío manual del resumen de pagos pendientes, sin ejecutar acciones.</summary>
    Task<PaymentReminderResult> SendAdminSummaryAsync(CancellationToken cancellationToken = default);
}
