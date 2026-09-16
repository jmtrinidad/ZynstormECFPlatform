using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public class PaymentReminderService(
    IRepository<Client> clientRepository,
    IEmailService emailService,
    IOptions<AppSettings> options,
    ILogger<PaymentReminderService> logger) : IPaymentReminderService
{
    private readonly AppSettings _settings = options.Value;

    private string AdminRecipient => !string.IsNullOrWhiteSpace(_settings.PaymentAlertEmail)
        ? _settings.PaymentAlertEmail
        : _settings.CertificateAlertEmail;

    /// <summary>Clientes con plan activo y fecha de próximo pago registrada.</summary>
    private IQueryable<Client> Candidates() =>
        clientRepository.Table
            .Include(c => c.Plan)
            .Where(c => c.Plan != null
                     && c.Plan.StatusId == (int)StatusEnum.Active
                     && c.NextPaymentDate != null);

    private static bool HasEmail(Client client) => !string.IsNullOrWhiteSpace(client.Email);

    private static bool IsPaymentSuspended(Client client) => client.PaymentSuspendedAtUtc != null;

    /// <summary>Desactivado a mano por otro motivo: no se le envían avisos ni se lista.</summary>
    private static bool IsManuallyInactive(Client client) => client.ClientInactive && !IsPaymentSuspended(client);

    private static PaymentReminderEmailData EmailData(Client client)
    {
        var due = client.NextPaymentDate!.Value.Date;
        var amount = PaymentCalculator.Calculate(client.Plan!.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent).Total;

        return new PaymentReminderEmailData(
            client.Name,
            client.Plan.Name,
            client.Plan.PlanTypeId,
            amount,
            Math.Max(1, client.PaidMonths),
            due,
            PaymentReminderPolicy.GetDeadline(due, client.PaymentGraceDays));
    }

    private static string DescribeStatus(Client client)
    {
        var due = client.NextPaymentDate!.Value.Date;

        if (IsPaymentSuspended(client)) return "Suspendido";
        if (!HasEmail(client)) return "Sin correo";
        if (client.FinalPaymentReminderSentFor?.Date == due) return "Último aviso enviado";
        if (client.FirstPaymentReminderSentFor?.Date == due) return "Primer aviso enviado";
        return "Pendiente de aviso";
    }

    private static PaymentSummaryItem SummaryItem(Client client, int daysOverdue, string status)
    {
        var data = EmailData(client);
        return new PaymentSummaryItem(
            client.Name, client.Rnc, data.PlanName, data.PlanTypeId, data.DueDate, daysOverdue, data.Amount, data.Deadline, status);
    }

    private async Task SendAsync(Client client, (string Subject, string HtmlBody) email, CancellationToken cancellationToken) =>
        await emailService.SendEmailAsync(client.Email!, email.Subject, email.HtmlBody, cancellationToken: cancellationToken);

    public async Task<PaymentReminderRunResult> RunDailyAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var clients = await Candidates().OrderBy(c => c.Name).ToListAsync(cancellationToken);

        var summary = new List<PaymentSummaryItem>();
        int first = 0, final = 0, suspended = 0;

        foreach (var client in clients)
        {
            if (IsManuallyInactive(client)) continue;

            var due = client.NextPaymentDate!.Value.Date;
            var overdue = PaymentReminderPolicy.GetDaysOverdue(due, today);
            if (overdue < 1) continue;

            var hasEmail = HasEmail(client);
            var action = PaymentReminderPolicy.Decide(
                due,
                client.PaymentGraceDays,
                client.FirstPaymentReminderSentFor,
                client.FinalPaymentReminderSentFor,
                hasEmail,
                IsPaymentSuspended(client),
                today);

            string status;
            try
            {
                switch (action)
                {
                    case PaymentReminderAction.FirstReminder when hasEmail:
                        await SendAsync(client, PaymentReminderEmails.BuildFirstReminder(EmailData(client)), cancellationToken);
                        client.FirstPaymentReminderSentFor = due;
                        await clientRepository.UpdateAsync(client);
                        first++;
                        status = "Primer aviso enviado hoy";
                        break;

                    case PaymentReminderAction.FinalReminder when hasEmail:
                        await SendAsync(client, PaymentReminderEmails.BuildFinalReminder(EmailData(client)), cancellationToken);
                        client.FinalPaymentReminderSentFor = due;
                        await clientRepository.UpdateAsync(client);
                        final++;
                        status = "Último aviso enviado hoy";
                        break;

                    case PaymentReminderAction.Suspend:
                        client.ClientInactive = true;
                        client.PaymentSuspendedAtUtc = DateTime.UtcNow;
                        await clientRepository.UpdateAsync(client);
                        suspended++;
                        status = "Suspendido hoy";

                        if (hasEmail)
                        {
                            try
                            {
                                await SendAsync(client, PaymentReminderEmails.BuildSuspension(EmailData(client)), cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Pago: cliente {ClientId} suspendido, pero no se pudo enviar el correo de suspensión.", client.ClientId);
                                status = "Suspendido hoy (correo no enviado)";
                            }
                        }
                        break;

                    default:
                        status = DescribeStatus(client);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pago: error procesando el aviso del cliente {ClientId}.", client.ClientId);
                status = "Error al enviar el aviso";
            }

            summary.Add(SummaryItem(client, overdue, status));
        }

        if (summary.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(AdminRecipient))
            {
                logger.LogWarning("Pago: {Count} pagos pendientes pero no hay PaymentAlertEmail ni CertificateAlertEmail configurado.", summary.Count);
            }
            else
            {
                var (subject, html) = PaymentReminderEmails.BuildAdminSummary(summary);
                await emailService.SendEmailAsync(AdminRecipient, subject, html, cancellationToken: cancellationToken);
            }
        }

        logger.LogInformation(
            "Pago: {Pending} pendientes, {First} primeros avisos, {Final} últimos avisos, {Suspended} suspensiones.",
            summary.Count, first, final, suspended);

        return new PaymentReminderRunResult(first, final, suspended, summary.Count);
    }

    public async Task<PaymentReminderResult> SendClientReminderAsync(string clientGuid, CancellationToken cancellationToken = default)
    {
        var client = await clientRepository.Table
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null)
            return new(false, "No se encontró el cliente.");
        if (client.Plan == null)
            return new(false, "El cliente no tiene un plan asignado.");
        if (client.NextPaymentDate is not DateTime nextPayment)
            return new(false, "El cliente no tiene una fecha de próximo pago registrada.");
        if (!HasEmail(client))
            return new(false, "El cliente no tiene un correo electrónico registrado.");

        var due = nextPayment.Date;
        var overdue = PaymentReminderPolicy.GetDaysOverdue(due, DateTimeExtensions.DrNow.Date);
        if (overdue < 0)
            return new(false, $"El pago de este cliente vence el {PaymentReminderEmails.FormatDate(due)}; todavía no corresponde enviar el recordatorio.");

        var data = EmailData(client);

        if (IsPaymentSuspended(client))
        {
            await SendAsync(client, PaymentReminderEmails.BuildSuspension(data), cancellationToken);
            return new(true, $"Aviso de suspensión enviado a {client.Email}.");
        }

        var isFinal = overdue >= PaymentReminderPolicy.NormalizeGraceDays(client.PaymentGraceDays);
        if (isFinal)
        {
            await SendAsync(client, PaymentReminderEmails.BuildFinalReminder(data), cancellationToken);
            client.FinalPaymentReminderSentFor = due;
        }
        else
        {
            await SendAsync(client, PaymentReminderEmails.BuildFirstReminder(data), cancellationToken);
            client.FirstPaymentReminderSentFor = due;
        }

        await clientRepository.UpdateAsync(client);

        return new(true, $"{(isFinal ? "Último aviso" : "Recordatorio")} de pago enviado a {client.Email}.");
    }

    public async Task<PaymentReminderResult> SendAdminSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var clients = await Candidates().AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken);

        var items = clients
            .Where(c => !IsManuallyInactive(c))
            .Select(c => (Client: c, Overdue: PaymentReminderPolicy.GetDaysOverdue(c.NextPaymentDate!.Value, today)))
            .Where(x => x.Overdue >= 1)
            .Select(x => SummaryItem(x.Client, x.Overdue, DescribeStatus(x.Client)))
            .ToList();

        if (items.Count == 0)
            return new(false, "No hay clientes con pagos vencidos.");
        if (string.IsNullOrWhiteSpace(AdminRecipient))
            return new(false, "No hay un correo administrativo configurado (AppSettings:PaymentAlertEmail).");

        var (subject, html) = PaymentReminderEmails.BuildAdminSummary(items);
        await emailService.SendEmailAsync(AdminRecipient, subject, html, cancellationToken: cancellationToken);

        return new(true, $"Resumen de {items.Count} pago(s) pendiente(s) enviado a {AdminRecipient}.");
    }
}
