using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfStatusHistoryService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfStatusHistory>(context, sqlGenerator), IEcfStatusHistoryService
{
}