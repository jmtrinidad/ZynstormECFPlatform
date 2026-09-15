using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class PlanOverageTierService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<PlanOverageTier>(context, sqlGenerator), IPlanOverageTierService
{
}
