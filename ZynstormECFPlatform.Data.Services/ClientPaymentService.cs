using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientPaymentService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientPayment>(context, sqlGenerator), IClientPaymentService
{
}
