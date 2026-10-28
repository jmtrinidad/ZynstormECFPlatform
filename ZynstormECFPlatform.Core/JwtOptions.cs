namespace ZynstormECFPlatform.Core;

public class JwtOptions
{
    public string Issuer { get; set; } = "EasyInvoice";
    public string Audience { get; set; } = "EasyInvoice.Clients";
    public int AccessTokenMinutes { get; set; } = 1440;
    public string Secret { get; set; } = string.Empty;
}
