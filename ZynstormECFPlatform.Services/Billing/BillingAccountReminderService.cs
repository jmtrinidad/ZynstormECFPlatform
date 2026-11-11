using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountReminderService(
    IBillingAccountService accountService,
    IEmailService emailService,
    IOptions<AppSettings> options,
    ILogger<BillingAccountReminderService> logger) : IBillingAccountReminderService
{
    private readonly AppSettings _settings = options.Value;

    private string AdminRecipient => !string.IsNullOrWhiteSpace(_settings.PaymentAlertEmail)
        ? _settings.PaymentAlertEmail
        : _settings.CertificateAlertEmail;

    /// <summary>Cuentas activas con plan activo y fecha de próximo pago registrada.</summary>
    private IQueryable<BillingAccount> Candidates() =>
        accountService.Table
            .Include(a => a.Plan)
            .Include(a => a.OwnerUser)
            .Where(a => a.StatusId == (int)StatusEnum.Active
                     && a.Plan.StatusId == (int)StatusEnum.Active
                     && a.NextPaymentDate != null);

    private static string? Recipient(BillingAccount account) => account.OwnerUser?.Email;

    private static bool HasEmail(BillingAccount account) => !string.IsNullOrWhiteSpace(Recipient(account));

    private static bool IsSuspended(BillingAccount account) => account.PaymentSuspendedAtUtc != null;

    private static PaymentReminderEmailData EmailData(BillingAccount account)
    {
        var due = account.NextPaymentDate!.Value.Date;
        var amount = PaymentCalculator.CalculateCycle(BillingTerms.For(account), due).Total;

        return new PaymentReminderEmailData(
            account.Name,
            account.Plan.Name,
            account.Plan.PlanTypeId,
            amount,
            Math.Max(1, account.PaidMonths),
            due,
            PaymentReminderPolicy.GetDeadline(due, account.PaymentGraceDays));
    }

    private static string DescribeStatus(BillingAccount account)
    {
        var due = account.NextPaymentDate!.Value.Date;

        if (IsSuspended(account)) return "Suspendida";
        if (!HasEmail(account)) return "Sin correo";
        if (account.FinalPaymentReminderSentFor?.Date == due) return "Último aviso enviado";
        if (account.FirstPaymentReminderSentFor?.Date == due) return "Primer aviso enviado";
        return "Pendiente de aviso";
    }

    private static PaymentSummaryItem SummaryItem(BillingAccount account, int daysOverdue, string status)
    {
        var data = EmailData(account);
        return new PaymentSummaryItem(
            $"{account.Name} (cuenta)", "—", data.PlanName, data.PlanTypeId, data.DueDate, daysOverdue, data.Amount, data.Deadline, status);
    }

    private async Task SendAsync(BillingAccount account, (string Subject, string HtmlBody) email, CancellationToken cancellationToken) =>
        await emailService.SendEmailAsync(Recipient(account)!, email.Subject, email.HtmlBody, cancellationToken: cancellationToken);

    /// <summary>Persiste primero la suspensión para que un fallo del correo no deje a la cuenta con acceso activo.</summary>
    private async Task<string> SuspendAccountAsync(BillingAccount account, CancellationToken cancellationToken)
    {
        account.PaymentSuspendedAtUtc = DateTime.UtcNow;
        await accountService.UpdateAsync(account);

        if (!HasEmail(account))
            return "Suspendida hoy";

        try
        {
            await SendAsync(account, PaymentReminderEmails.BuildSuspension(EmailData(account)), cancellationToken);
            return "Suspendida hoy";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pago: cuenta {AccountId} suspendida, pero no se pudo enviar el correo de suspensión.", account.BillingAccountId);
            return "Suspendida hoy (correo no enviado)";
        }
    }

    private static PaymentReminderAction Decide(BillingAccount account, DateTime today) =>
        PaymentReminderPolicy.Decide(
            account.NextPaymentDate!.Value.Date,
            account.PaymentGraceDays,
            account.FirstPaymentReminderSentFor,
            account.FinalPaymentReminderSentFor,
            HasEmail(account),
            IsSuspended(account),
            today);

    public async Task<int> SuspendExpiredAccountsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var accounts = await Candidates().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        var suspended = 0;

        foreach (var account in accounts)
        {
            if (Decide(account, today) != PaymentReminderAction.Suspend) continue;

            try
            {
                await SuspendAccountAsync(account, cancellationToken);
                suspended++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pago: error suspendiendo la cuenta {AccountId} después del período de gracia.", account.BillingAccountId);
            }
        }

        logger.LogInformation("Pago: {Suspended} cuenta(s) suspendida(s) por vencimiento.", suspended);
        return suspended;
    }

    public async Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var accounts = await Candidates().OrderBy(a => a.Name).ToListAsync(cancellationToken);

        var summary = new List<PaymentSummaryItem>();
        int first = 0, final = 0, suspended = 0;

        foreach (var account in accounts)
        {
            var due = account.NextPaymentDate!.Value.Date;
            var overdue = PaymentReminderPolicy.GetDaysOverdue(due, today);
            if (overdue < 1) continue;

            var hasEmail = HasEmail(account);
            var action = Decide(account, today);

            string status;
            try
            {
                switch (action)
                {
                    case PaymentReminderAction.FirstReminder when hasEmail:
                        await SendAsync(account, PaymentReminderEmails.BuildFirstReminder(EmailData(account)), cancellationToken);
                        account.FirstPaymentReminderSentFor = due;
                        await accountService.UpdateAsync(account);
                        first++;
                        status = "Primer aviso enviado hoy";
                        break;

                    case PaymentReminderAction.FinalReminder when hasEmail:
                        await SendAsync(account, PaymentReminderEmails.BuildFinalReminder(EmailData(account)), cancellationToken);
                        account.FinalPaymentReminderSentFor = due;
                        await accountService.UpdateAsync(account);
                        final++;
                        status = "Último aviso enviado hoy";
                        break;

                    case PaymentReminderAction.Suspend:
                        status = await SuspendAccountAsync(account, cancellationToken);
                        suspended++;
                        break;

                    default:
                        status = DescribeStatus(account);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pago: error procesando el aviso de la cuenta {AccountId}.", account.BillingAccountId);
                status = "Error al enviar el aviso";
            }

            summary.Add(SummaryItem(account, overdue, status));
        }

        if (summary.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(AdminRecipient))
            {
                logger.LogWarning("Pago: {Count} cuentas con pagos pendientes pero no hay PaymentAlertEmail ni CertificateAlertEmail configurado.", summary.Count);
            }
            else
            {
                var (subject, html) = PaymentReminderEmails.BuildAdminSummary(summary);
                await emailService.SendEmailAsync(AdminRecipient, subject, html, cancellationToken: cancellationToken);
            }
        }

        logger.LogInformation(
            "Pago (cuentas): {Pending} pendientes, {First} primeros avisos, {Final} últimos avisos, {Suspended} suspensiones.",
            summary.Count, first, final, suspended);

        return new PaymentReminderRunResult(first, final, suspended, summary.Count);
    }
}
