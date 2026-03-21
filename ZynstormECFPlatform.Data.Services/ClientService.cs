using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class ClientService : Repository<Client>, IClientService
{
    public ClientService(ZynstormEcfPlatformContext context) : base(context)
    {
    }

    public async Task<ClientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .Include(x => x.Status)
            .Include(x => x.ClientBranches)
            .FirstOrDefaultAsync(x => x.ClientId == id, cancellationToken);

        if (entity == null) return null;

        return MapToDto(entity);
    }

    public async Task<IPagedCollection<ClientDto>> GetPagedAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        var count = await _dbSet.CountAsync(cancellationToken);
        var items = await _dbSet
            .Include(x => x.Status)
            .OrderBy(x => x.Name)
            .Skip((page - 1) * perPage)
            .Take(perPage)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedCollection<ClientDto>(dtos, count, page, perPage);
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

        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(ClientDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { dto.ClientId }, cancellationToken);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Rnc = dto.Rnc;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.StatusId = dto.StatusId;

        _dbSet.Update(entity);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null) return false;

        _dbSet.Remove(entity);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
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
