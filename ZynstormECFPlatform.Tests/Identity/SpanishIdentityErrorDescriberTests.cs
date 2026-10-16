using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data.Identity;

namespace ZynstormECFPlatform.Tests.Identity;

public class SpanishIdentityErrorDescriberTests
{
    [Fact]
    public async Task Password_validator_reports_errors_in_spanish()
    {
        var options = new IdentityOptions();
        options.Password.RequiredLength = 8;
        var userManager = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), Options.Create(options), null!, null!, null!, null!, null!, null!, null!);
        var validator = new PasswordValidator<User>(new SpanishIdentityErrorDescriber());

        var result = await validator.ValidateAsync(userManager.Object, new User(), "abc");

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Description == "La contraseña debe tener al menos una letra mayúscula (A-Z).");
        Assert.Contains(result.Errors, e => e.Description == "La contraseña debe tener al menos 8 caracteres.");
    }
}
