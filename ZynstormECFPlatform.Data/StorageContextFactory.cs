using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ZynstormECFPlatform.Data
{
    public class StorageContextFactory() : IDesignTimeDbContextFactory<StorageContext>
    {
        //private readonly ConfigurationManager _configuration = configuration;

        public StorageContext CreateDbContext(string[] args)
        {
            // 1. Cargar la configuración desde el appsettings.json (opcional pero recomendado)
            var configuration = new ConfigurationManager();

            //.SetBasePath(Directory.GetCurrentDirectory())
            //.AddJsonFile("appsettings.json", optional: true)
            //.AddEnvironmentVariables()
            //.Build();

            var optionsBuilder = new DbContextOptionsBuilder<StorageContext>();

            // 2. Obtener la cadena de conexión
            // Si no tienes appsettings en este proyecto, puedes ponerla fija para la migración:
            //var connectionString = "Host=217.216.91.10;Port=8087;Database=ZynstormECFPlatform_dev_db;Username=easy_staging;Password=remigio135795#";

            optionsBuilder.UseNpgsql(configuration.GetConnectionString("DefaultConnection")!);

            return new StorageContext(optionsBuilder.Options);
        }
    }
}