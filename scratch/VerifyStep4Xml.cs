using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services;
using System.IO;

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
services.AddTransient<IEcfGeneratorService, EcfGeneratorService>();
services.AddTransient<IClientService, DummyClientService>();
services.AddTransient<IApiKeyService, DummyApiKeyService>();

var provider = services.BuildServiceProvider();
var generator = provider.GetRequiredService<IEcfGeneratorService>();

// Simulate Index 25 (Type 32, < 250k, Step 4)
var dto = new EcfInvoiceRequestDto
{
    Ncf = "E320000000012",
    ManualMontoTotal = 47200,
    CustomerRnc = "0", // Placeholder to be cleaned
    CustomerName = "CLIENTE FINAL", // Placeholder to be cleaned
    IssueDate = DateTime.Now,
    Items = new List<EcfItemRequestDto> { new EcfItemRequestDto { Name = "Test", Quantity = 1, UnitPrice = 40000, TaxPercentage = 18 } }
};

Console.WriteLine("--- GENERATING XML FOR INDEX 25 (Step 4) ---");
string xml = generator.GenerateUnsignedXml(dto, isSummary: false);
Console.WriteLine(xml);

// Verify Comprador node
if (xml.Contains("<RNCComprador>") || xml.Contains("<RazonSocialComprador>"))
{
    Console.WriteLine("FAIL: Buyer placeholders were NOT cleaned!");
}
else
{
    Console.WriteLine("SUCCESS: Buyer placeholders cleaned and empty tags suppressed.");
}

if (xml.Contains("<ECF>") && !xml.Contains("<RFCE>"))
{
    Console.WriteLine("SUCCESS: <ECF> root used for Step 4.");
}
else
{
    Console.WriteLine("FAIL: Root element is incorrect.");
}

public class DummyClientService : IClientService { public Task<ClientDto> GetByAsync(System.Linq.Expressions.Expression<Func<ClientDto, bool>> predicate) => Task.FromResult(new ClientDto { ClientId = 1 }); public Task<ClientDto> GetByIdAsync(int id) => throw new NotImplementedException(); public Task<List<ClientDto>> GetAllAsync() => throw new NotImplementedException(); public Task<ClientDto> CreateAsync(ClientDto dto) => throw new NotImplementedException(); public Task<ClientDto> UpdateAsync(ClientDto dto) => throw new NotImplementedException(); public Task DeleteAsync(int id) => throw new NotImplementedException(); }
public class DummyApiKeyService : IApiKeyService { public Task<ApiKeyDto> GetByAsync(System.Linq.Expressions.Expression<Func<ApiKeyDto, bool>> predicate) => Task.FromResult(new ApiKeyDto { Key = "test" }); public Task<ApiKeyDto> GetByIdAsync(int id) => throw new NotImplementedException(); public Task<List<ApiKeyDto>> GetAllAsync() => throw new NotImplementedException(); public Task<ApiKeyDto> CreateAsync(ApiKeyDto dto) => throw new NotImplementedException(); public Task<ApiKeyDto> UpdateAsync(ApiKeyDto dto) => throw new NotImplementedException(); public Task DeleteAsync(int id) => throw new NotImplementedException(); }
