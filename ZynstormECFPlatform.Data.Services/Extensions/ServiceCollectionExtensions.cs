using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Data.Services.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDataServices(this IServiceCollection services)
    {
        var collection = Assembly.GetExecutingAssembly().GetTypes()
            .Where(mytype => mytype.GetInterface(typeof(IRepository<>).Name) != null && mytype.IsClass);

        foreach (var service in collection)
        {
            var iService = Assembly.Load("ZynstormECFPlatform.Abstractions").GetTypes().FirstOrDefault(t => t.Name == "I" + service.Name);
            services.AddScoped(iService!, service);
        }

        services.AddScoped<IAccountService, AccountService>();

        return services;
    }
}