using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientSupportService
{
    Task<IEnumerable<ClientCallBackDto>> GetCallBacksByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ClientCallBackDto> CreateCallBackAsync(ClientCallBackDto dto, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ClientCertificateDto>> GetCertificatesByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    Task<ClientCertificateDto> CreateCertificateAsync(ClientCertificateDto dto, CancellationToken cancellationToken = default);
}
