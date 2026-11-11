using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BillingAccountService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BillingAccount>(context, sqlGenerator), IBillingAccountService
{
}
