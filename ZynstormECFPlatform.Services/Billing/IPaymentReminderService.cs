namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentReminderResult(bool Success, string Message);

public sealed record PaymentReminderRunResult(int FirstReminders, int FinalReminders, int Suspended, int Pending);

public interface IPaymentReminderService
{
    /// <summary>Corrida diaria: avisos, suspensiones y resumen al correo administrativo.</summary>
    Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default);

    /// <summary>Envío manual del aviso que corresponde a un cliente. Nunca suspende.</summary>
    Task<PaymentReminderResult> SendClientReminderAsync(string clientGuid, CancellationToken cancellationToken = default);

    /// <summary>Envío manual del resumen de pagos pendientes, sin ejecutar acciones.</summary>
    Task<PaymentReminderResult> SendAdminSummaryAsync(CancellationToken cancellationToken = default);
}
