using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class EcfXmlDocumentService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<EcfXmlDocument>(context, sqlGenerator), IEcfXmlDocumentService
{
}
