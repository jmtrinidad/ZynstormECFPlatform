using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class DGIIUnitService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<DGIIUnit>(context, sqlGenerator), IDGIIUnitService
{
}