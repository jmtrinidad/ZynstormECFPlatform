using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class UserClientService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<UserClient>(context, sqlGenerator), IUserClientService
{
}
