using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientCallBackService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientCallBack>(context, sqlGenerator), IClientCallBackService
{
}