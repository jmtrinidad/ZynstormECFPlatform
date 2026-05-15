using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddTransient<IJwtTokenService, JwtTokenService>();
        services.AddTransient<IEncryptedService, EncryptedService>();
        services.AddTransient<IEmailService, EmailService>();
        
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddTransient<IInboundEcfService, InboundEcfService>();
        
        services.AddTransient<IXmlSignatureService, XmlSignatureService>();
        services.AddHttpClient<IDgiiAuthService, DgiiAuthService>();
        services.AddHttpClient<IDgiiTransmissionService, DgiiTransmissionService>();
        

        // --- Certification: Excel Passthrough (Data Testing) ---
        services.AddTransient<ICertificationExcelMappingService, Certification.CertificationExcelMappingService>();
        services.AddTransient<ICertificationExcelGeneratorService, Certification.CertificationExcelGeneratorService>();
        services.AddTransient<ICertificationExcelService, Certification.CertificationExcelService>();

        // --- Certification: Simulation ---
        services.AddTransient<ICertificationSimulationMappingService, Certification.CertificationSimulationMappingService>();
        services.AddTransient<ICertificationSimulationGeneratorService, Certification.CertificationSimulationGeneratorService>();
        services.AddTransient<ICertificationSimulationService, Certification.CertificationSimulationService>();

        // --- Old Simulation (Legacy Matrix) ---
        services.AddTransient<Certification.OldSimulation.IOldEcfGeneratorService, Certification.OldSimulation.OldEcfGeneratorService>();
        services.AddTransient<Certification.OldSimulation.IOldCertificationSimulationService, Certification.OldSimulation.OldCertificationSimulationService>();

        // --- Production ---
        services.AddTransient<IEcfProductionGeneratorService, Production.EcfProductionGeneratorService>();
        services.AddTransient<IEcfProductionService, Production.EcfProductionService>();
        services.AddTransient<Production.IReceivedEcfProductionService, Production.ReceivedEcfProductionService>();

        // --- XML Validation (Standalone) ---
        // Singleton because it maintains an internal ConcurrentDictionary cache for Verificaciones
        services.AddSingleton<IEcfXmlValidationService, Validation.EcfXmlValidationService>();

        services.AddTransient<Jobs.EcfTrackingJob>();

        return services;
    }
}
