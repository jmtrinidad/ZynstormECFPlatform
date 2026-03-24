using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        //services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IEncryptedService, EncryptedService>();

        return services;
    }
}