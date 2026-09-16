namespace ZynstormECFPlatform.Core;

public class AppSettings
{
    public string Secret { get; set; } = string.Empty;

    public string Key { get; set; } = default!;

    public string IV { get; set; } = default!;

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = null!;
    public string SmtpAppPassword { get; set; } = null!;
    public string SmtpFromName { get; set; } = null!;

    public string PasswordChangeSuccessUrl { get; set; } = string.Empty;

    /// <summary>Correo que recibe el resumen diario de certificados por vencer.</summary>
    public string CertificateAlertEmail { get; set; } = string.Empty;

    /// <summary>Días de anticipación para considerar un certificado próximo a vencer.</summary>
    public int CertificateExpirationWarningDays { get; set; } = 20;

    /// <summary>Días de anticipación para considerar una renta próxima a vencer.</summary>
    public int PaymentWarningDays { get; set; } = 15;

    /// <summary>Correo que recibe el resumen diario de rentas pendientes. Si está vacío se usa CertificateAlertEmail.</summary>
    public string PaymentAlertEmail { get; set; } = string.Empty;
}