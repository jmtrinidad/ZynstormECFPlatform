using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Data.Services;

public class ApiKeyService : Repository<ApiKey>, IApiKeyService
{
    public ApiKeyService(ZynstormEcfPlatformContext context) : base(context)
    {
    }

    public async Task<ApiKeyDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.ApiKeyId == id, cancellationToken);

        if (entity == null) return null;

        return MapToDto(entity);
    }

    public async Task<ApiKeyDto?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);

        if (entity == null) return null;

        return MapToDto(entity);
    }

    public async Task<IEnumerable<ApiKeyDto>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default)
    {
        var items = await _dbSet
            .Include(x => x.Status)
            .Where(x => x.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto);
    }

    public async Task<ApiKeyDto> CreateAsync(ApiKeyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new ApiKey
        {
            ClientId = dto.ClientId,
            Key = dto.Key,
            Description = dto.Description,
            IsActive = dto.IsActive,
            StatusId = dto.StatusId,
            CreatedAt = DateTime.UtcNow
        };

        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task<bool> UpdateAsync(ApiKeyDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { dto.ApiKeyId }, cancellationToken);
        if (entity == null) return false;

        entity.Key = dto.Key;
        entity.Description = dto.Description;
        entity.IsActive = dto.IsActive;
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

    private ApiKeyDto MapToDto(ApiKey entity)
    {
        return new ApiKeyDto
        {
            ApiKeyId = entity.ApiKeyId,
            ClientId = entity.ClientId,
            Key = entity.Key,
            Description = entity.Description,
            IsActive = entity.IsActive,
            StatusId = entity.StatusId,
            CreatedAt = entity.CreatedAt,
            Status = entity.Status != null ? new StatusDto { StatusId = entity.Status.StatusId, Name = entity.Status.Name } : null
        };
    }
}
