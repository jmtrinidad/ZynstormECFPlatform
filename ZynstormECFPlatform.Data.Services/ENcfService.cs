using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ENcfService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ENcf>(context, sqlGenerator), IENcfService
{
}
