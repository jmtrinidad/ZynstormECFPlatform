using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Aplica el corte de acceso al iniciar el día posterior al período de gracia, a clientes sueltos y a cuentas.
/// Los recordatorios siguen ejecutándose en horario laboral en PaymentReminderJob.
/// </summary>
public class PaymentSuspensionJob(
    IPaymentReminderService paymentReminderService,
    IBillingAccountReminderService accountReminderService,
    ILogger<PaymentSuspensionJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Payment Suspension Job...");
        var suspended = await paymentReminderService.SuspendExpiredClientsAsync(cancellationToken);
        var suspendedAccounts = await accountReminderService.SuspendExpiredAccountsAsync(cancellationToken);
        logger.LogInformation(
            "Payment Suspension Job finished: {Suspended} clients and {Accounts} accounts suspended.", suspended, suspendedAccounts);
    }
}
