using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BusinessSimulationSampleService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BusinessSimulationSample>(context, sqlGenerator), IBusinessSimulationSampleService
{
}
