using System.Linq.Expressions;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientCertificateService : IRepository<ClientCertificate>
{
    /// <summary>
    /// Obtiene el certificado vigente que cumpla el predicado: prefiere los no vencidos
    /// (ExpirationDateUtc nula o futura) y, entre ellos, el de vencimiento más lejano.
    /// Si todos están vencidos, retorna el de vencimiento más reciente como fallback.
    /// </summary>
    Task<ClientCertificate?> GetActiveCertificateAsync(Expression<Func<ClientCertificate, bool>> predicate, CancellationToken cancellationToken = default);
}
