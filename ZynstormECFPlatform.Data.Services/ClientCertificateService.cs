using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientCertificateService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientCertificate>(context, sqlGenerator), IClientCertificateService
{
}