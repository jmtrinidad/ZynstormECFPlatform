using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Services.Jobs;

/// <summary>
/// Corre diariamente: envía el primer y el último aviso de pago vencido, suspende a los clientes y cuentas que no pagaron
/// dentro de sus días de gracia y manda el resumen de pendientes al correo administrativo.
/// </summary>
public class PaymentReminderJob(
    IPaymentReminderService paymentReminderService,
    IBillingAccountReminderService accountReminderService,
    ILogger<PaymentReminderJob> logger)
{
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Payment Reminder Job...");
        var result = await paymentReminderService.RunDailyAsync(cancellationToken);
        logger.LogInformation(
            "Payment Reminder Job finished: {Pending} pending, {First} first, {Final} final, {Suspended} suspended.",
            result.Pending, result.FirstReminders, result.FinalReminders, result.Suspended);

        var accounts = await accountReminderService.RunDailyAsync(cancellationToken);
        logger.LogInformation(
            "Payment Reminder Job (accounts) finished: {Pending} pending, {First} first, {Final} final, {Suspended} suspended.",
            accounts.Pending, accounts.FirstReminders, accounts.FinalReminders, accounts.Suspended);
    }
}
