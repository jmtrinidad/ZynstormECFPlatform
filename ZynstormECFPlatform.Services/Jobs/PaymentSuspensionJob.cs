using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Aplica el corte de acceso al iniciar el día posterior al período de gracia.
/// Los recordatorios siguen ejecutándose en horario laboral en PaymentReminderJob.
/// </summary>
public class PaymentSuspensionJob(IPaymentReminderService paymentReminderService, ILogger<PaymentSuspensionJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Payment Suspension Job...");
        var suspended = await paymentReminderService.SuspendExpiredClientsAsync(cancellationToken);
        logger.LogInformation("Payment Suspension Job finished: {Suspended} suspended.", suspended);
    }
}
