using System.Net;
using System.Text;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Certificates;

public record ExpiringCertificateInfo(string ClientName, string ClientRnc, string? ClientEmail, DateTime ExpirationDateUtc, int DaysToExpire);

/// <summary>
/// Reglas y plantillas de correo para el aviso de vencimiento de certificados digitales.
/// </summary>
public static class CertificateExpirationHelper
{
    public const int DefaultWarningDays = 20;

    /// <summary>
    /// Misma regla que ClientCertificateService.GetActiveCertificateAsync: prefiere los no vencidos
    /// y, entre ellos, el de vencimiento más lejano.
    /// </summary>
    public static ClientCertificate? SelectActive(IEnumerable<ClientCertificate> certificates)
    {
        var now = DateTime.UtcNow;
        return certificates
            .OrderByDescending(c => c.ExpirationDateUtc is null || c.ExpirationDateUtc >= now)
            .ThenByDescending(c => c.ExpirationDateUtc ?? DateTime.MinValue)
            .FirstOrDefault();
    }

    /// <summary>Días calendario (hora RD) que faltan para el vencimiento; negativo si ya venció.</summary>
    public static int? GetDaysToExpire(DateTime? expirationDateUtc)
    {
        if (!expirationDateUtc.HasValue) return null;
        return (expirationDateUtc.Value.ToDrTime().Date - DateTime.UtcNow.ToDrTime().Date).Days;
    }

    public static bool IsExpiringSoon(int? daysToExpire, int warningDays) =>
        daysToExpire.HasValue && daysToExpire.Value <= warningDays;

    private static string DescribeDays(int days) => days switch
    {
        < 0 => $"venció hace {-days} día{(days == -1 ? "" : "s")}",
        0 => "vence hoy",
        1 => "vence mañana",
        _ => $"vence en {days} días"
    };

    private static string Wrap(string title, string innerHtml) => $@"
        <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 640px; margin: 0 auto; background-color: #f4f7f9; padding: 20px; border-radius: 8px;"">
            <div style=""background-color: #ffffff; padding: 32px; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">
                <h1 style=""color: #2c3e50; font-size: 22px; margin: 0 0 20px;"">{title}</h1>
                {innerHtml}
            </div>
            <div style=""text-align: center; margin-top: 20px; color: #95a5a6; font-size: 12px;"">
                &copy; {DateTime.UtcNow.Year} Zynstorm ECF Platform. Todos los derechos reservados.
            </div>
        </div>";

    public static (string Subject, string HtmlBody) BuildClientEmail(string clientName, DateTime expirationDateUtc, int daysToExpire)
    {
        var name = WebUtility.HtmlEncode(clientName);
        var date = expirationDateUtc.ToDrTime().ToString("dd/MM/yyyy");
        var status = DescribeDays(daysToExpire);

        var body = $@"
                <p style=""color: #34495e; font-size: 15px; line-height: 1.6;"">Estimado cliente <strong>{name}</strong>,</p>
                <p style=""color: #34495e; font-size: 15px; line-height: 1.6;"">
                    Le informamos que el <strong>certificado digital</strong> que utiliza para firmar sus comprobantes fiscales electrónicos (e-CF)
                    <strong>{status}</strong> (fecha de vencimiento: <strong>{date}</strong>).
                </p>
                <div style=""background-color: #fff3cd; border: 1px solid #ffeeba; color: #856404; padding: 15px; border-radius: 4px; font-size: 14px; margin: 24px 0;"">
                    <strong>⚠️ Acción requerida:</strong><br>
                    Una vez vencido el certificado no será posible firmar ni enviar comprobantes a la DGII.
                    Por favor, solicite un nuevo certificado digital a su entidad certificadora y compártalo con nosotros
                    para instalarlo antes de la fecha de vencimiento.
                </div>
                <p style=""color: #7f8c8d; font-size: 14px; line-height: 1.6;"">
                    Si ya realizó la renovación, puede ignorar este mensaje. Ante cualquier duda, contacte a nuestro equipo de soporte.
                </p>";

        return ($"Su certificado digital {status} - Zynstorm ECF", Wrap("Aviso de vencimiento de certificado digital", body));
    }

    public static (string Subject, string HtmlBody) BuildAdminSummaryEmail(IReadOnlyCollection<ExpiringCertificateInfo> items, int warningDays)
    {
        var rows = new StringBuilder();
        foreach (var item in items)
        {
            var color = item.DaysToExpire < 0 ? "#c0392b" : item.DaysToExpire <= 7 ? "#d35400" : "#b7950b";
            rows.Append($@"
                <tr>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{WebUtility.HtmlEncode(item.ClientName)}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1; font-family: monospace;"">{WebUtility.HtmlEncode(item.ClientRnc)}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1;"">{item.ExpirationDateUtc.ToDrTime():dd/MM/yyyy}</td>
                    <td style=""padding: 8px; border-bottom: 1px solid #ecf0f1; color: {color}; font-weight: bold;"">{DescribeDays(item.DaysToExpire)}</td>
                </tr>");
        }

        var body = $@"
                <p style=""color: #34495e; font-size: 15px; line-height: 1.6;"">
                    Se encontraron <strong>{items.Count}</strong> cliente(s) activos con el certificado vigente vencido o por vencer
                    en los próximos {warningDays} días.
                </p>
                <table style=""width: 100%; border-collapse: collapse; font-size: 14px; color: #34495e;"">
                    <thead>
                        <tr style=""background-color: #f8f9fa; text-align: left;"">
                            <th style=""padding: 8px;"">Cliente</th>
                            <th style=""padding: 8px;"">RNC</th>
                            <th style=""padding: 8px;"">Vencimiento</th>
                            <th style=""padding: 8px;"">Estado</th>
                        </tr>
                    </thead>
                    <tbody>{rows}</tbody>
                </table>
                <p style=""color: #7f8c8d; font-size: 13px; margin-top: 24px;"">
                    Puede notificar a cada cliente desde la pantalla de Clientes de la plataforma.
                </p>";

        return ($"[Zynstorm ECF] {items.Count} certificado(s) por vencer", Wrap("Certificados próximos a vencer", body));
    }
}
