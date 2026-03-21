using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class ClientService : Repository<Client>, IClientService
{
    public ClientService(StorageContext context, ISqlGenerator sqlGenerator) : base(context, sqlGenerator)
    {
    }

    public async Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Table
            .Include(x => x.Status)
            .Include(x => x.ClientBranches)
            .FirstOrDefaultAsync(x => x.ClientId == id, cancellationToken);

        if (entity == null) return null;

        return MapToDto(entity);
    }

    public async Task<IPagedCollection<ClientDto>> GetPagedAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var pagedItems = await GetPagedAsync(page, perPage);
        var dtos = pagedItems.Select(MapToDto).ToList();
        return new PagedCollection<ClientDto>(dtos, page, perPage, pagedItems.TotalItemCount);
    }

    public async Task<ClientDto> CreateAsync(ClientDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new Client
        {
            Name = dto.Name,
            Rnc = dto.Rnc,
            Email = dto.Email,
            Phone = dto.Phone,
            StatusId = dto.StatusId,
            CreatedAt = DateTime.UtcNow
        };

        Add(entity);
        await SaveChangesAsync();

        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(ClientDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await GetAsync(dto.ClientId, cancellationToken);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Rnc = dto.Rnc;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.StatusId = dto.StatusId;

        Update(entity);
        return await SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await SoftDeleteAsync(id);
    }

    private ClientDto MapToDto(Client entity)
    {
        return new ClientDto
        {
            ClientId = entity.ClientId,
            Name = entity.Name,
            Rnc = entity.Rnc,
            Email = entity.Email,
            Phone = entity.Phone,
            StatusId = entity.StatusId,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status != null ? new StatusDto { StatusId = entity.Status.StatusId, Name = entity.Status.Name } : null,
            Branches = entity.ClientBranches.Select(b => new ClientBrancheDto
            {
                ClientBrancheId = b.ClientBrancheId,
                ClientId = b.ClientId,
                Name = b.Name,
                Address = b.Address,
                Phone = b.Phone
            }).ToList()
        };
    }
}
