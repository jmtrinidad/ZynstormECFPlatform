using System.Globalization;
using System.Net;
using System.Text;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Certificates;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentReminderEmailData(
    string ClientName,
    string PlanName,
    int PlanTypeId,
    decimal Amount,
    int PaidMonths,
    DateTime DueDate,
    DateTime Deadline);

public sealed record PaymentSummaryItem(
    string ClientName,
    string ClientRnc,
    string PlanName,
    int PlanTypeId,
    DateTime DueDate,
    int DaysOverdue,
    decimal Amount,
    DateTime Deadline,
    string Status);

/// <summary>Plantillas de los correos de pago pendiente. Tono amigable hacia el cliente.</summary>
public static class PaymentReminderEmails
{
    private const string Paragraph = @"style=""color: #34495e; font-size: 15px; line-height: 1.6;""";

    public static string FormatMoney(decimal amount) =>
        "RD$" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string FormatDate(DateTime date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>"la renta" o "la mensualidad" según el tipo de plan.</summary>
    public static string Concept(int planTypeId) =>
        planTypeId == (int)PlanTypeEnum.Rent ? "la renta" : "la mensualidad";

    private static string DescribeMonths(int months) => months == 1 ? "1 mes" : $"{months} meses";

    private static string Details(PaymentReminderEmailData data, string deadlineLabel) => $@"
                <table style=""width: 100%; border-collapse: collapse; font-size: 14px; color: #34495e; background-color: #f8f9fa; border-radius: 6px; margin: 20px 0;"">
                    <tr><td style=""padding: 10px 14px;"">Plan</td><td style=""padding: 10px 14px; text-align: right;""><strong>{WebUtility.HtmlEncode(data.PlanName)}</strong></td></tr>
                    <tr><td style=""padding: 10px 14px;"">Monto a pagar</td><td style=""padding: 10px 14px; text-align: right;""><strong>{FormatMoney(data.Amount)}</strong> ({DescribeMonths(data.PaidMonths)})</td></tr>
                    <tr><td style=""padding: 10px 14px;"">Fecha de pago</td><td style=""padding: 10px 14px; text-align: right;"">{FormatDate(data.DueDate)}</td></tr>
                    <tr><td style=""padding: 10px 14px;"">{deadlineLabel}</td><td style=""padding: 10px 14px; text-align: right;""><strong>{FormatDate(data.Deadline)}</strong></td></tr>
                </table>";

    private const string Closing = @"
                <p style=""color: #7f8c8d; font-size: 14px; line-height: 1.6;"">
                    Si ya realizó el pago, por favor ignore este mensaje. Ante cualquier duda, nuestro equipo de soporte con gusto le ayudará.
                </p>
                <p style=""color: #34495e; font-size: 15px; line-height: 1.6;"">¡Muchas gracias por confiar en nosotros!</p>";

    public static (string Subject, string HtmlBody) BuildFirstReminder(PaymentReminderEmailData data)
    {
        var name = WebUtility.HtmlEncode(data.ClientName);
        var concept = Concept(data.PlanTypeId);

        var body = $@"
                <p {Paragraph}>¡Hola, <strong>{name}</strong>! Esperamos que se encuentre muy bien.</p>
                <p {Paragraph}>
                    Le escribimos para recordarle que llegó el día de pago de {concept} de su plan.
                </p>
                {Details(data, "Pagar antes del")}
                <div style=""background-color: #e8f4fd; border: 1px solid #b6dcf7; color: #1b4f72; padding: 15px; border-radius: 4px; font-size: 14px; margin: 24px 0;"">
                    Le agradecemos realizar su pago antes del <strong>{FormatDate(data.Deadline)}</strong>
                    para evitar cortes en el servicio y recargos.
                </div>
                {Closing}";

        return ($"Recordatorio: llegó el día de pago de {concept} - Zynstorm ECF",
            EmailLayout.Wrap("Recordatorio de pago", body));
    }

    public static (string Subject, string HtmlBody) BuildFinalReminder(PaymentReminderEmailData data)
    {
        var name = WebUtility.HtmlEncode(data.ClientName);
        var concept = Concept(data.PlanTypeId);

        var body = $@"
                <p {Paragraph}>¡Hola, <strong>{name}</strong>!</p>
                <p {Paragraph}>
                    Todavía no hemos recibido el pago de {concept} de su plan y <strong>hoy vence el plazo</strong> para realizarlo.
                </p>
                {Details(data, "Último día para pagar")}
                <div style=""background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 4px; font-size: 14px; margin: 24px 0;"">
                    Para evitar la suspensión del servicio y recargos, le pedimos realizar su pago hoy,
                    <strong>{FormatDate(data.Deadline)}</strong>.
                </div>
                {Closing}";

        return ($"Hoy vence el plazo para pagar {concept} - Zynstorm ECF",
            EmailLayout.Wrap("Último recordatorio de pago", body));
    }

    public static (string Subject, string HtmlBody) BuildSuspension(PaymentReminderEmailData data)
    {
        var name = WebUtility.HtmlEncode(data.ClientName);
        var concept = Concept(data.PlanTypeId);

        var body = $@"
                <p {Paragraph}>Hola, <strong>{name}</strong>.</p>
                <p {Paragraph}>
                    No hemos recibido el pago de {concept} correspondiente al <strong>{FormatDate(data.DueDate)}</strong>,
                    por lo que su servicio fue <strong>suspendido temporalmente</strong>. Mientras tanto no será posible emitir comprobantes.
                </p>
                {Details(data, "Plazo vencido el")}
                <div style=""background-color: #fdecea; border: 1px solid #f5c6cb; color: #721c24; padding: 15px; border-radius: 4px; font-size: 14px; margin: 24px 0;"">
                    En cuanto registremos su pago, el servicio se reactivará. Si necesita ayuda o ya realizó el pago,
                    por favor contáctenos.
                </div>
                <p style=""color: #34495e; font-size: 15px; line-height: 1.6;"">Gracias por su comprensión.</p>";

        return ("Su servicio fue suspendido por falta de pago - Zynstorm ECF",
            EmailLayout.Wrap("Servicio suspendido", body));
    }

    public static (string Subject, string HtmlBody) BuildAdminSummary(IReadOnlyCollection<PaymentSummaryItem> items)
    {
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            var color = item.DaysOverdue > 3 ? "#c0392b" : "#d35400";
            rows.Append($@"
                <tr>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{WebUtility.HtmlEncode(item.ClientName)}<br><span style=""font-family: monospace; color: #7f8c8d;"">{WebUtility.HtmlEncode(item.ClientRnc)}</span></td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{WebUtility.HtmlEncode(item.PlanName)}<br><span style=""color: #7f8c8d;"">{(item.PlanTypeId == (int)PlanTypeEnum.Rent ? "Renta" : "Comprobantes")}</span></td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{FormatDate(item.DueDate)}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1; color: {color}; font-weight: bold;"">{item.DaysOverdue} día{(item.DaysOverdue == 1 ? "" : "s")}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1; text-align: right;"">{FormatMoney(item.Amount)}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{FormatDate(item.Deadline)}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{WebUtility.HtmlEncode(item.Status)}</td>
                </tr>");
        }

        var total = items.Sum(i => i.Amount);
        var body = $@"
                <p {Paragraph}>
                    Hay <strong>{items.Count}</strong> cliente(s) con el pago del plan vencido, por un total de <strong>{FormatMoney(total)}</strong>.
                </p>
                <table style=""width: 100%; border-collapse: collapse; font-size: 13px; color: #34495e;"">
                    <thead>
                        <tr style=""background-color: #f8f9fa; text-align: left;"">
                            <th style=""padding: 8px;"">Cliente</th>
                            <th style=""padding: 8px;"">Plan</th>
                            <th style=""padding: 8px;"">Fecha de pago</th>
                            <th style=""padding: 8px;"">Vencido</th>
                            <th style=""padding: 8px; text-align: right;"">Monto</th>
                            <th style=""padding: 8px;"">Límite</th>
                            <th style=""padding: 8px;"">Estado</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
                <p style=""color: #7f8c8d; font-size: 13px; margin-top: 24px;"">
                    Al registrar el pago en el cliente, los avisos se detienen y, si estaba suspendido, se reactiva automáticamente.
                </p>";

        return ($"[Zynstorm ECF] {items.Count} cliente(s) con pago pendiente", EmailLayout.Wrap("Pagos pendientes", body));
    }
}
