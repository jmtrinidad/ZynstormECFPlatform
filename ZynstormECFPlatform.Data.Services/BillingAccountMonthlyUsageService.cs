using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BillingAccountMonthlyUsageService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BillingAccountMonthlyUsage>(context, sqlGenerator), IBillingAccountMonthlyUsageService
{
}
