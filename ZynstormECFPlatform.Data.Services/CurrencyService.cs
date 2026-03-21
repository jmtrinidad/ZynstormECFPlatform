using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class CurrencyService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<Currency>(context, sqlGenerator), ICurrencyService
{
}