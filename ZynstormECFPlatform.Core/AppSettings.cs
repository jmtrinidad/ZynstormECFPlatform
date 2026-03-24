namespace ZynstormECFPlatform.Core;

public class AppSettings
{
    public string Secret { get; set; } = string.Empty;

    public string Key { get; set; } = default!;

    public string IV { get; set; } = default!;
}