using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class UserPageAccessService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<UserPageAccess>(context, sqlGenerator), IUserPageAccessService
{
}
