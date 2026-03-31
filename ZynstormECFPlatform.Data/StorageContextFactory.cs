using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace ZynstormECFPlatform.Data
{
    public class StorageContextFactory : IDesignTimeDbContextFactory<StorageContext>
    {
        public StorageContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(basePath, "..", "ZynstormECFPlatform.Web.Api");

            // Verify if appsettings.json exists in the guessed Web.Api path, otherwise try current directory
            if (!File.Exists(Path.Combine(apiPath, "appsettings.json")))
            {
                apiPath = Path.Combine(basePath, "ZynstormECFPlatform.Web.Api");
            }

            if (!File.Exists(Path.Combine(apiPath, "appsettings.json")))
            {
                apiPath = basePath;
            }

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<StorageContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new StorageContext(optionsBuilder.Options);
        }
    }
}