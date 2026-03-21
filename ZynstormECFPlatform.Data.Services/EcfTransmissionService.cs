using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfTransmissionService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfTransmission>(context, sqlGenerator), IEcfTransmissionService
{
}