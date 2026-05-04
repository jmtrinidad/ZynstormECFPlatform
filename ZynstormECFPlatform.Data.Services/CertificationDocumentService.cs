using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class CertificationDocumentService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<CertificationDocument>(context, sqlGenerator), ICertificationDocumentService
{
}
