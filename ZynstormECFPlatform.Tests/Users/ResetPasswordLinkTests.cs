using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Tests.Users;

public class ResetPasswordLinkTests
{
    [Theory]
    [InlineData("https://ecfstaging.zynstorm.com/api")]
    [InlineData("https://ecfstaging.zynstorm.com/api/")]
    public void Build_keeps_the_api_prefix_of_the_platform_url(string platformUrl)
    {
        var link = ResetPasswordLink.Build(platformUrl, "dG9r+ZW4=", "ana@empresa.com");

        Assert.Equal(
            "https://ecfstaging.zynstorm.com/api/ResetPassword/reset-password?token=dG9r%2BZW4%3D&identifier=ana%40empresa.com",
            link);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Build_returns_null_without_platform_url(string? platformUrl)
    {
        Assert.Null(ResetPasswordLink.Build(platformUrl, "t", "ana"));
    }
}
