using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<Client>(context, sqlGenerator), IClientService
{
}