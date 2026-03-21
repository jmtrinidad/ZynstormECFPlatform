using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfTypeService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfType>(context, sqlGenerator), IEcfTypeService
{
}