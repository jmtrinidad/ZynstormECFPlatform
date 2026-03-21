using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Data.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        // Registro manual de servicios principales por ahora, 
        // o implementar escaneo de ensamblado similar a EasyInvoice
        services.AddScoped<IEcfDocumentService, EcfDocumentService>();
        services.AddScoped<IClientService, ClientService>();
        
        return services;
    }
}
