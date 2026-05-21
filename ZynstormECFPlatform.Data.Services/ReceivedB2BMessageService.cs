using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ReceivedB2BMessageService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ReceivedB2BMessage>(context, sqlGenerator), IReceivedB2BMessageService
{
    public override async Task<ReceivedB2BMessage?> InsertAsync(ReceivedB2BMessage model)
    {
        return await base.InsertAsync(model);
    }
}