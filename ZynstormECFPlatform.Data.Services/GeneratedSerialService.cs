using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public sealed class GeneratedSerialService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<GeneratedSerial>(context, sqlGenerator), IGeneratedSerialService
{
}
