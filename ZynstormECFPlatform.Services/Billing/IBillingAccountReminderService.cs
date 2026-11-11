namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountReminderService
{
    /// <summary>Corrida diaria de avisos a los dueños de cuentas con pago vencido y resumen al correo administrativo.</summary>
    Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default);

    /// <summary>Suspende las cuentas cuyo período de gracia ya terminó. Al suspender una cuenta se bloquea la emisión de todos sus clientes.</summary>
    Task<int> SuspendExpiredAccountsAsync(CancellationToken cancellationToken = default);
}
