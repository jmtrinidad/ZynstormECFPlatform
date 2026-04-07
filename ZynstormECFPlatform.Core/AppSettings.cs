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
}