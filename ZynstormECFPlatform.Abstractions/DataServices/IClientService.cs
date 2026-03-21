using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IClientService
{
    Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IPagedCollection<ClientDto>> GetPagedAsync(int page, int perPage, CancellationToken cancellationToken = default);
    Task<ClientDto> CreateAsync(ClientDto clientDto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ClientDto clientDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
