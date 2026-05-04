using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class UserAccessLogService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<UserAccessLog>(context, sqlGenerator), IUserAccessLogService
{
}
