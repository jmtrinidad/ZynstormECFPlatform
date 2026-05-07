using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class BusinessTypeService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<BusinessType>(context, sqlGenerator), IBusinessTypeService
{
}
