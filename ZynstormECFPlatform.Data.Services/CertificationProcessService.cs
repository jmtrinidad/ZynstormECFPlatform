using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class CertificationProcessService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<CertificationProcess>(context, sqlGenerator), ICertificationProcessService
{
}
