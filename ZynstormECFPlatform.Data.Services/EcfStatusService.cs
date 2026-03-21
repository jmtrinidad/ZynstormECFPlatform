using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfStatusService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfStatus>(context, sqlGenerator), IEcfStatusService
{
}