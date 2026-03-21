using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class EcfDocumentService : Repository<EcfDocument>, IEcfDocumentService
{
    public EcfDocumentService(StorageContext context, ISqlGenerator sqlGenerator) : base(context, sqlGenerator)
    {
    }

    public async Task<EcfDocumentViewDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Table
            .Include(x => x.EcfStatus)
            .Include(x => x.EcfType)
            .Include(x => x.Client)
            .Include(x => x.Currency)
            .Include(x => x.EcfDocumentDetails)
            .Include(x => x.EcfDocumentTotals)
            .FirstOrDefaultAsync(x => x.EcfDocumentId == id, cancellationToken);

        if (entity == null) return null;

        return MapToViewDto(entity);
    }

    public async Task<IPagedCollection<EcfDocumentViewDto>> GetPagedAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var pagedItems = await GetPagedAsync(page, perPage);
        var dtos = pagedItems.Select(MapToViewDto).ToList();
        return new PagedCollection<EcfDocumentViewDto>(dtos, page, perPage, pagedItems.TotalItemCount);
    }

    public async Task<EcfDocumentViewDto> CreateAsync(EcfDocumentCreateDto createDto, CancellationToken cancellationToken = default)
    {
        var entity = new EcfDocument
        {
            ClientId = createDto.ClientId,
            ClientBrancheId = createDto.ClientBrancheId,
            ApiKeyId = createDto.ApiKeyId,
            EcfTypeId = createDto.EcfTypeId,
            ExternalReference = createDto.ExternalReference,
            Ncf = createDto.Ncf ?? string.Empty,
            CustomerRnc = createDto.CustomerRnc,
            CustomerName = createDto.CustomerName,
            CustomerEmail = createDto.CustomerEmail,
            CustomerAddress = createDto.CustomerAddress,
            IssueDate = createDto.IssueDate,
            CurrencyId = createDto.CurrencyId,
            SubTotal = createDto.SubTotal,
            Itbistotal = createDto.Itbistotal,
            Total = createDto.Total,
            EcfStatusId = createDto.EcfStatusId,
            CreatedAtUtc = DateTime.UtcNow
        };

        Add(entity);
        await SaveChangesAsync();
        
        return MapToViewDto(entity);
    }

    public async Task<bool> UpdateAsync(EcfDocumentUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(updateDto.EcfDocumentId, cancellationToken);
        if (entity == null) return false;

        entity.EcfStatusId = updateDto.EcfStatusId;
        entity.CustomerEmail = updateDto.CustomerEmail;

        Update(entity);
        return await SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await SoftDeleteAsync(id);
    }

    private EcfDocumentViewDto MapToViewDto(EcfDocument entity)
    {
        return new EcfDocumentViewDto
        {
            EcfDocumentId = entity.EcfDocumentId,
            ClientId = entity.ClientId,
            EcfStatusId = entity.EcfStatusId,
            Total = entity.Total,
            CreatedAt = entity.CreatedAtUtc,
            Ncf = entity.Ncf,
            Status = entity.EcfStatus != null ? new EcfStatusDto { EcfStatusId = entity.EcfStatus.EcfStatusId, Name = entity.EcfStatus.Name } : null
        };
    }
}
