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
    private const string Paragraph = @"style=""color: #334155; font-size: 15px; line-height: 1.6; margin: 0 0 16px 0;""";

    public static string FormatMoney(decimal amount) =>
        "RD$" + amount.ToString("N2", CultureInfo.InvariantCulture);

    public static string FormatDate(DateTime date) =>
        date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>"la renta" o "la mensualidad" según el tipo de plan.</summary>
    public static string Concept(int planTypeId) =>
        planTypeId == (int)PlanTypeEnum.Rent ? "la renta" : "la mensualidad";

    private static readonly string[] MonthNames =
    [
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    ];

    public static string DescribePaymentPeriod(DateTime dueDate, int paidMonths)
    {
        var months = Math.Max(1, paidMonths);
        var startMonth = MonthNames[dueDate.Month - 1];

        if (months == 1)
        {
            return $"Mes a pagar: {startMonth} {dueDate.Year}";
        }

        var endDate = dueDate.AddMonths(months - 1);
        var endMonth = MonthNames[endDate.Month - 1];

        if (dueDate.Year == endDate.Year)
        {
            return $"Meses a pagar: {startMonth} – {endMonth} {dueDate.Year} ({months} meses)";
        }

        return $"Meses a pagar: {startMonth} {dueDate.Year} – {endMonth} {endDate.Year} ({months} meses)";
    }

    private static string Details(PaymentReminderEmailData data, string deadlineLabel, string deadlineColor) => $@"
                <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 10px; padding: 22px; margin: 24px 0 20px 0; text-align: center;"">
                    <div style=""font-size: 12px; font-weight: 600; text-transform: uppercase; color: #64748b; letter-spacing: 0.5px;"">Monto a pagar</div>
                    <div style=""font-size: 32px; font-weight: 800; color: #0f172a; margin: 6px 0; letter-spacing: -0.5px;"">{FormatMoney(data.Amount)}</div>
                    <div style=""display: inline-block; font-size: 12px; color: #334155; background-color: #e2e8f0; padding: 4px 12px; border-radius: 9999px; font-weight: 600;"">
                        {DescribePaymentPeriod(data.DueDate, data.PaidMonths)}
                    </div>

                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin-top: 20px; border-top: 1px dashed #cbd5e1; padding-top: 14px; font-size: 13px; text-align: left; color: #334155;"">
                        <tr>
                            <td style=""padding: 6px 0; color: #64748b;"">Plan asignado:</td>
                            <td style=""padding: 6px 0; text-align: right; font-weight: 600; color: #0f172a;"">{WebUtility.HtmlEncode(data.PlanName)}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 6px 0; color: #64748b;"">Fecha de pago:</td>
                            <td style=""padding: 6px 0; text-align: right; font-weight: 500; color: #0f172a;"">{FormatDate(data.DueDate)}</td>
                        </tr>
                        <tr>
                            <td style=""padding: 6px 0; color: #64748b;"">{deadlineLabel}:</td>
                            <td style=""padding: 6px 0; text-align: right; font-weight: 700; color: {deadlineColor};"">{FormatDate(data.Deadline)}</td>
                        </tr>
                    </table>
                </div>";

    private const string HelpBox = @"
                <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 14px 18px; font-size: 13px; color: #64748b; line-height: 1.5; margin-top: 24px;"">
                    <strong style=""color: #334155;"">¿Necesita asistencia o realizar su reporte de pago?</strong><br>
                    Si ya realizó su transferencia o requiere asistencia con su facturación, puede responder directamente a este correo o comunicarse con nuestro equipo de atención.
                </div>";

    private const string Closing = @"
                <p style=""color: #64748b; font-size: 13px; line-height: 1.6; margin: 20px 0 10px 0;"">
                    Si ya realizó el pago, por favor ignore este mensaje. Ante cualquier duda, nuestro equipo de soporte con gusto le ayudará.
                </p>
                <p style=""color: #334155; font-size: 15px; font-weight: 500; line-height: 1.6; margin: 0;"">¡Muchas gracias por confiar en nosotros!</p>";

    public static (string Subject, string HtmlBody) BuildFirstReminder(PaymentReminderEmailData data)
    {
        var name = WebUtility.HtmlEncode(data.ClientName);
        var concept = Concept(data.PlanTypeId);

        var body = $@"
                <p {Paragraph}>¡Hola, <strong>{name}</strong>! Esperamos que se encuentre muy bien.</p>
                <p {Paragraph}>
                    Le escribimos para recordarle que llegó el día de pago de {concept} de su plan.
                </p>
                {Details(data, "Pagar antes del", "#0284c7")}
                <div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; color: #1e40af; padding: 14px 16px; border-radius: 6px; font-size: 14px; line-height: 1.5; margin: 20px 0;"">
                    Le agradecemos realizar su pago antes del <strong>{FormatDate(data.Deadline)}</strong>
                    para evitar cortes en el servicio y recargos.
                </div>
                {Closing}
                {HelpBox}";

        return ($"Recordatorio: llegó el día de pago de {concept} - Zynstorm",
            EmailLayout.Wrap("Recordatorio de pago", body, "Recordatorio", "#dbeafe", "#1e40af"));
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
                {Details(data, "Último día para pagar", "#b45309")}
                <div style=""background-color: #fffbeb; border-left: 4px solid #f59e0b; color: #92400e; padding: 14px 16px; border-radius: 6px; font-size: 14px; line-height: 1.5; margin: 20px 0;"">
                    Para evitar la suspensión del servicio y recargos, le pedimos realizar su pago hoy,
                    <strong>{FormatDate(data.Deadline)}</strong>.
                </div>
                {Closing}
                {HelpBox}";

        return ($"Hoy vence el plazo para pagar {concept} - Zynstorm",
            EmailLayout.Wrap("Último recordatorio de pago", body, "Último aviso", "#fef3c7", "#92400e"));
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
                {Details(data, "Plazo vencido el", "#dc2626")}
                <div style=""background-color: #fef2f2; border-left: 4px solid #ef4444; color: #991b1b; padding: 14px 16px; border-radius: 6px; font-size: 14px; line-height: 1.5; margin: 20px 0;"">
                    En cuanto registremos su pago, el servicio se reactivará. Si necesita ayuda o ya realizó el pago,
                    por favor contáctenos.
                </div>
                <p {Paragraph}>Gracias por su comprensión.</p>
                {HelpBox}";

        return ("Su servicio fue suspendido por falta de pago - Zynstorm",
            EmailLayout.Wrap("Servicio suspendido", body, "Suspendido", "#fee2e2", "#991b1b"));
    }

    public static (string Subject, string HtmlBody) BuildAdminSummary(IReadOnlyCollection<PaymentSummaryItem> items)
    {
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            var isRent = item.PlanTypeId == (int)PlanTypeEnum.Rent;
            var planBadgeColor = isRent ? "#0284c7" : "#0d9488";
            var planTypeLabel = isRent ? "Renta" : "Comprobantes";

            var overdueColor = item.DaysOverdue > 3 ? "#dc2626" : "#d97706";
            var statusBadge = item.Status.Contains("Suspendido", StringComparison.OrdinalIgnoreCase)
                ? @"<span style=""background-color: #fee2e2; color: #991b1b; padding: 2px 6px; border-radius: 4px; font-size: 11px; font-weight: 600;"">Suspendido</span>"
                : item.Status.Contains("Último", StringComparison.OrdinalIgnoreCase)
                    ? @"<span style=""background-color: #fef3c7; color: #92400e; padding: 2px 6px; border-radius: 4px; font-size: 11px; font-weight: 600;"">Último aviso</span>"
                    : @"<span style=""background-color: #dbeafe; color: #1e40af; padding: 2px 6px; border-radius: 4px; font-size: 11px; font-weight: 600;"">Primer aviso</span>";

            rows.Append($@"
                <tr style=""border-bottom: 1px solid #f1f5f9;"">
                    <td style=""padding: 10px 8px;""><strong>{WebUtility.HtmlEncode(item.ClientName)}</strong><br><span style=""font-family: monospace; font-size: 11px; color: #64748b;"">{WebUtility.HtmlEncode(item.ClientRnc)}</span></td>
                    <td style=""padding: 10px 8px;"">{WebUtility.HtmlEncode(item.PlanName)}<br><span style=""color: {planBadgeColor}; font-weight: 600; font-size: 11px;"">{planTypeLabel}</span></td>
                    <td style=""padding: 10px 8px; color: #475569;"">{FormatDate(item.DueDate)}</td>
                    <td style=""padding: 10px 8px; color: {overdueColor}; font-weight: 700;"">{item.DaysOverdue} día{(item.DaysOverdue == 1 ? "" : "s")}</td>
                    <td style=""padding: 10px 8px; text-align: right; font-weight: 600; color: #0f172a;"">{FormatMoney(item.Amount)}</td>
                    <td style=""padding: 10px 8px; color: #64748b;"">{FormatDate(item.Deadline)}</td>
                    <td style=""padding: 10px 8px; text-align: right;"">{statusBadge}</td>
                </tr>");
        }

        var total = items.Sum(i => i.Amount);
        var body = $@"
                <div style=""background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px 20px; margin-bottom: 20px;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                        <tr>
                            <td>
                                <span style=""font-size: 12px; color: #64748b; font-weight: 600; text-transform: uppercase;"">Clientes pendientes</span>
                                <div style=""font-size: 22px; font-weight: 700; color: #0f172a;"">{items.Count}</div>
                            </td>
                            <td align=""right"">
                                <span style=""font-size: 12px; color: #64748b; font-weight: 600; text-transform: uppercase;"">Total acumulado</span>
                                <div style=""font-size: 22px; font-weight: 800; color: #0f172a;"">{FormatMoney(total)}</div>
                            </td>
                        </tr>
                    </table>
                </div>

                <div style=""overflow-x: auto;"">
                    <table style=""width: 100%; border-collapse: collapse; font-size: 12px; color: #334155; border: 1px solid #e2e8f0; border-radius: 8px;"">
                        <thead>
                            <tr style=""background-color: #f8fafc; text-align: left; border-bottom: 2px solid #e2e8f0;"">
                                <th style=""padding: 10px 8px; font-weight: 600; color: #475569;"">Cliente</th>
                                <th style=""padding: 10px 8px; font-weight: 600; color: #475569;"">Plan</th>
                                <th style=""padding: 10px 8px; font-weight: 600; color: #475569;"">Fecha de pago</th>
                                <th style=""padding: 10px 8px; font-weight: 600; color: #475569;"">Vencido</th>
                                <th style=""padding: 10px 8px; text-align: right; font-weight: 600; color: #475569;"">Monto</th>
                                <th style=""padding: 10px 8px; font-weight: 600; color: #475569;"">Límite</th>
                                <th style=""padding: 10px 8px; text-align: right; font-weight: 600; color: #475569;"">Estado</th>
                            </tr>
                        </thead>
                        <tbody>{rows}</tbody>
                    </table>
                </div>

                <p style=""color: #64748b; font-size: 13px; margin-top: 20px; line-height: 1.5;"">
                    Al registrar el pago en el cliente, los avisos se detienen y, si estaba suspendido, se reactiva automáticamente.
                </p>";

        return ($"[Zynstorm] {items.Count} cliente(s) con pago pendiente",
            EmailLayout.Wrap("Pagos pendientes", body, "Reporte", "#f1f5f9", "#475569"));
    }
}
