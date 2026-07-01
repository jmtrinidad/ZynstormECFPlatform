using System.Net;
using System.Net.Http;
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
        // DGII servers (Test & Production) do not support HTTP/2.
        // Forcing HTTP/1.1 prevents the 'ResponseEnded' premature connection drop.
        services.AddHttpClient<IDgiiAuthService, DgiiAuthService>(client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        });
        services.AddHttpClient<IDgiiTransmissionService, DgiiTransmissionService>(client =>
        {
            client.DefaultRequestVersion = HttpVersion.Version11;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        });
        

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

        // --- Certification: Representación Impresa (RI) ---
        services.AddTransient<ICertificationRiModelService, Ri.CertificationRiModelService>();

        // --- Production ---
        services.AddTransient<IEcfProductionGeneratorService, Production.EcfProductionGeneratorService>();
        services.AddTransient<IEcfProductionService, Production.EcfProductionService>();
        services.AddTransient<Production.IReceivedEcfProductionService, Production.ReceivedEcfProductionService>();

        // --- XML Validation (Standalone) ---
        // Singleton because it maintains an internal ConcurrentDictionary cache for Verificaciones
        services.AddSingleton<IEcfXmlValidationService, Validation.EcfXmlValidationService>();

        services.AddTransient<Jobs.EcfTrackingJob>();
        services.AddTransient<Jobs.AutomaticReportsJob>();
        services.AddTransient<Jobs.ReceivedB2BMessagesCleanupJob>();

        return services;
    }
}
