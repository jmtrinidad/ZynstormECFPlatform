using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class SystemLogService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<SystemLog>(context, sqlGenerator), ISystemLogService
{
}