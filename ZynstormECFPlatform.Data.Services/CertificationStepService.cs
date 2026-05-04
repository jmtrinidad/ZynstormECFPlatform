using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class CertificationStepService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<CertificationStep>(context, sqlGenerator), ICertificationStepService
{
}
