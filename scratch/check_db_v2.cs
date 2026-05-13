using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Core.Entities;

class Program {
    static async Task Main(string[] args) {
        var config = new ConfigurationBuilder()
            .AddJsonFile("c:/Projects/ZynstormECFPlatform/ZynstormECFPlatform.Web.Api/appsettings.json")
            .Build();
        
        var optionsBuilder = new DbContextOptionsBuilder<StorageContext>();
        optionsBuilder.UseNpgsql(config.GetConnectionString("DefaultConnection"));
        
        using var context = new StorageContext(optionsBuilder.Options, null);
        
        var client = await context.Set<Client>().FirstOrDefaultAsync(c => c.Rnc == "132878191");
        if (client == null) {
            Console.WriteLine("Client 132878191 not found.");
            return;
        }
        
        Console.WriteLine($"ClientId: {client.ClientId}, GuidId: {client.GuidId}");
        
        var processes = await context.Set<CertificationProcess>()
            .Include(p => p.CertificationDocuments)
            .Where(p => p.ClientId == client.ClientId)
            .ToListAsync();
            
        Console.WriteLine($"Found {processes.Count} processes.");
        foreach (var p in processes) {
            Console.WriteLine($"ProcessId: {p.CertificationProcessId}, Step: {p.CurrentStepId}, Status: {p.Status}, Docs: {p.CertificationDocuments.Count}");
        }
    }
}
