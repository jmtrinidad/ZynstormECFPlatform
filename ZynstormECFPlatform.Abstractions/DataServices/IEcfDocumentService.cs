using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Abstractions.Data;

namespace ZynstormECFPlatform.Abstractions.DataServices;

public interface IEcfDocumentService
{
    Task<EcfDocumentViewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IPagedCollection<EcfDocumentViewDto>> GetPagedAsync(int page, int perPage, CancellationToken cancellationToken = default);
    Task<EcfDocumentViewDto> CreateAsync(EcfDocumentCreateDto createDto, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(EcfDocumentUpdateDto updateDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
