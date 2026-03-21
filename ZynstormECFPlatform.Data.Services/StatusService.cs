using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class StatusService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<Status>(context, sqlGenerator), IStatusService
{
}