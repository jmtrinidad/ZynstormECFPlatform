using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientBrancheService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientBranche>(context, sqlGenerator), IClientBrancheService
{
}