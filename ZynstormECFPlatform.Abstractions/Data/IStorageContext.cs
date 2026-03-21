using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ZynstormECFPlatform.Abstractions.Data;

public interface IStorageContext : IDisposable
{
    DatabaseFacade Database { get; }
}