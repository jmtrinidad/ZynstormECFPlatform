using ZynstormECFPlatform.Services.Ri.Html;
using Xunit;

namespace ZynstormECFPlatform.Tests.Ri.Html;

public class RiLogoValidatorTests
{
    // 1x1 PNG transparente.
    private const string Png =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";

    private static string DataUri(string mime, byte[] bytes) =>
        $"data:{mime};base64,{Convert.ToBase64String(bytes)}";

    [Fact]
    public void Validate_ValidPng_ReturnsNull() => Assert.Null(RiLogoValidator.Validate(Png));

    [Fact]
    public void Validate_ValidJpegAndWebp_ReturnsNull()
    {
        Assert.Null(RiLogoValidator.Validate(DataUri("image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0, 0])));
        Assert.Null(RiLogoValidator.Validate(DataUri("image/webp",
            [(byte)'R', (byte)'I', (byte)'F', (byte)'F', 0, 0, 0, 0, (byte)'W', (byte)'E', (byte)'B', (byte)'P'])));
    }

    [Fact]
    public void Validate_Svg_IsRejected() =>
        Assert.NotNull(RiLogoValidator.Validate(DataUri("image/svg+xml", "<svg onload=\"x()\"/>"u8.ToArray())));

    [Fact]
    public void Validate_NotADataUri_IsRejected()
    {
        Assert.NotNull(RiLogoValidator.Validate("http://evil/x.png"));
        Assert.NotNull(RiLogoValidator.Validate("data:text/html;base64,PHA+"));
    }

    [Fact]
    public void Validate_BadBase64_IsRejected() =>
        Assert.NotNull(RiLogoValidator.Validate("data:image/png;base64,@@@"));

    [Fact]
    public void Validate_ContentDoesNotMatchDeclaredType_IsRejected() =>
        Assert.NotNull(RiLogoValidator.Validate(DataUri("image/png", [0xFF, 0xD8, 0xFF, 0xE0])));

    [Fact]
    public void Validate_TooLarge_IsRejected()
    {
        var bytes = new byte[RiLogoValidator.MaxBytes + 1];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;

        var error = RiLogoValidator.Validate(DataUri("image/png", bytes));

        Assert.NotNull(error);
        Assert.Contains("500 KB", error);
    }
}
