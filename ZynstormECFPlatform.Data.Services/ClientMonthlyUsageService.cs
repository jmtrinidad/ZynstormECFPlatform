using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientMonthlyUsageService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientMonthlyUsage>(context, sqlGenerator), IClientMonthlyUsageService
{
}
