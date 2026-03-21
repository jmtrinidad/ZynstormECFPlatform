using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ApiKeyService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ApiKey>(context, sqlGenerator), IApiKeyService
{
}