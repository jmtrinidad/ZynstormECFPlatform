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
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IEcfGeneratorService, EcfGeneratorService>();
        
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();
        
        services.AddTransient<IXmlSignatureService, XmlSignatureService>();
        services.AddHttpClient<IDgiiAuthService, DgiiAuthService>();
        services.AddHttpClient<IDgiiTransmissionService, DgiiTransmissionService>();

        return services;
    }
}