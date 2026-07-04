using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Data.Services;

public class ClientCertificateService(
    StorageContext context,
    ISqlGenerator sqlGenerator) : Repository<ClientCertificate>(context, sqlGenerator), IClientCertificateService
{
    public async Task<ClientCertificate?> GetActiveCertificateAsync(Expression<Func<ClientCertificate, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var certificates = await Table.Where(predicate)
                                      .ToListAsync(cancellationToken)
                                      .ConfigureAwait(false);

        var now = DateTime.UtcNow;
        return certificates
            .OrderByDescending(c => c.ExpirationDateUtc is null || c.ExpirationDateUtc >= now)
            .ThenByDescending(c => c.ExpirationDateUtc ?? DateTime.MinValue)
            .FirstOrDefault();
    }
}
