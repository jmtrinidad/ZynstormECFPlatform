using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfDocumentService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfDocument>(context, sqlGenerator), IEcfDocumentService
{
}