using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientPaymentItemService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientPaymentItem>(context, sqlGenerator), IClientPaymentItemService
{
}
