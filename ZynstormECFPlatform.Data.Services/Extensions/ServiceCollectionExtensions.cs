using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Data.Services;

namespace ZynstormECFPlatform.Data.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        // Business services
        services.AddScoped<IEcfDocumentService, EcfDocumentService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IClientSupportService, ClientSupportService>();
        
        return services;
    }
}
