using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDbContextData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<IStorageContext, StorageContext>(options =>
            options.UseNpgsql(connectionString,
                builder =>
                {
                    builder.CommandTimeout(600);
                    builder.EnableRetryOnFailure();
                    builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                }

            ).EnableSensitiveDataLogging());

        services.AddIdentityCore<User>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;
            options.Tokens.PasswordResetTokenProvider = "PasswordResetTokenProvider";
        }).AddRoles<Role>()
          .AddSignInManager()
          .AddTokenProvider<PasswordResetTokenProvider<User>>("PasswordResetTokenProvider")
          .AddDefaultTokenProviders()
          .AddEntityFrameworkStores<StorageContext>();

        services.Configure<PasswordResetTokenProviderOptions>(options =>
        {
            options.TokenLifespan = TimeSpan.FromMinutes(30);
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISqlGenerator, SqlGenerator>();
        //services.AddScoped<SeedDb>();//New Way for inject Default Data

        return services;
    }
}