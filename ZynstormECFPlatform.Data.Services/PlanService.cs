using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class PlanService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<Plan>(context, sqlGenerator), IPlanService
{
}
