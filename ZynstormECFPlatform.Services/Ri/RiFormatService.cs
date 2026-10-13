using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Ri.Html;

namespace ZynstormECFPlatform.Services.Ri;

public class RiFormatService(StorageContext context) : IRiFormatService
{
    public async Task<IReadOnlyList<ClientTypeDto>> ListClientTypesAsync()
    {
        var types = await context.Set<ClientType>()
            .Include(t => t.RiHtmlTemplate)
            .OrderBy(t => t.Name)
            .ToListAsync();

        var counts = await context.Set<Client>()
            .Where(c => c.ClientTypeId != null)
            .GroupBy(c => c.ClientTypeId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        return types.ConvertAll(t => ToDto(t, counts.GetValueOrDefault(t.ClientTypeId)));
    }

    public async Task<ClientTypeDto> CreateClientTypeAsync(ClientTypeSaveDto dto)
    {
        var name = ValidateTypeDto(dto);
        if (await context.Set<ClientType>().AnyAsync(t => t.Name == name))
        {
            throw new InvalidOperationException($"Ya existe un tipo de cliente llamado '{name}'.");
        }

        var entity = new ClientType
        {
            GuidId = Guid.NewGuid().ToString(),
            RegisteredAt = DateTime.UtcNow,
            Name = name,
            Description = dto.Description,
            SampleDataJson = NormalizeSamples(dto.SampleDataJson),
            RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId),
        };

        context.Set<ClientType>().Add(entity);
        await context.SaveChangesAsync();

        return await GetTypeDtoAsync(entity.GuidId);
    }

    public async Task<ClientTypeDto> UpdateClientTypeAsync(string guidId, ClientTypeSaveDto dto)
    {
        var name = ValidateTypeDto(dto);
        var entity = await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Tipo de cliente no encontrado.");

        if (await context.Set<ClientType>().AnyAsync(t => t.Name == name && t.ClientTypeId != entity.ClientTypeId))
        {
            throw new InvalidOperationException($"Ya existe un tipo de cliente llamado '{name}'.");
        }

        entity.Name = name;
        entity.Description = dto.Description;
        entity.SampleDataJson = NormalizeSamples(dto.SampleDataJson);
        entity.RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId);
        entity.LastUpdateUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return await GetTypeDtoAsync(guidId);
    }

    public async Task DeleteClientTypeAsync(string guidId)
    {
        var entity = await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Tipo de cliente no encontrado.");

        var inUse = await context.Set<Client>().CountAsync(c => c.ClientTypeId == entity.ClientTypeId);
        if (inUse > 0)
        {
            throw new InvalidOperationException($"El tipo '{entity.Name}' lo usan {inUse} cliente(s); reasígnelos antes de borrarlo.");
        }

        context.Set<ClientType>().Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RiHtmlTemplateDto>> ListTemplatesAsync()
    {
        var templates = await context.Set<RiHtmlTemplate>().OrderBy(t => t.Name).ToListAsync();
        var result = new List<RiHtmlTemplateDto>();
        foreach (var template in templates)
        {
            result.Add(await ToDtoAsync(template));
        }

        return result;
    }

    public async Task<RiHtmlTemplateUploadResultDto> UploadTemplateAsync(RiHtmlTemplateUploadDto dto)
    {
        var validation = RiHtmlTemplateValidator.Validate(dto.Html);
        if (!validation.IsValid)
        {
            return new RiHtmlTemplateUploadResultDto(null, validation.Errors, validation.Warnings);
        }

        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return new RiHtmlTemplateUploadResultDto(null, ["El nombre del formato es obligatorio."], validation.Warnings);
        }

        var entity = new RiHtmlTemplate
        {
            GuidId = Guid.NewGuid().ToString(),
            RegisteredAt = DateTime.UtcNow,
            Name = name,
            FileName = string.IsNullOrWhiteSpace(dto.FileName) ? $"{name}.html" : dto.FileName.Trim(),
            Html = validation.SanitizedHtml,
        };

        context.Set<RiHtmlTemplate>().Add(entity);
        await context.SaveChangesAsync();

        return new RiHtmlTemplateUploadResultDto(await ToDtoAsync(entity), [], validation.Warnings);
    }

    public async Task DeleteTemplateAsync(string guidId)
    {
        var entity = await context.Set<RiHtmlTemplate>().FirstOrDefaultAsync(t => t.GuidId == guidId)
            ?? throw new InvalidOperationException("Formato no encontrado.");

        var inUse = await UsageCountAsync(entity.RiHtmlTemplateId);
        if (inUse > 0)
        {
            throw new InvalidOperationException($"El formato '{entity.Name}' está en uso por {inUse} tipo(s)/cliente(s); reasígnelos antes de borrarlo.");
        }

        context.Set<RiHtmlTemplate>().Remove(entity);
        await context.SaveChangesAsync();
    }

    public async Task AssignToClientAsync(string clientGuidId, ClientRiFormatAssignDto dto)
    {
        var client = await context.Set<Client>().FirstOrDefaultAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        client.ClientTypeId = string.IsNullOrWhiteSpace(dto.ClientTypeGuidId)
            ? null
            : (await context.Set<ClientType>().FirstOrDefaultAsync(t => t.GuidId == dto.ClientTypeGuidId)
                ?? throw new InvalidOperationException("Tipo de cliente no encontrado.")).ClientTypeId;

        client.RiHtmlTemplateId = await ResolveTemplateIdAsync(dto.RiHtmlTemplateGuidId);
        client.LastUpdateUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<ResolvedRiFormatDto> GetResolvedAsync(string clientGuidId)
    {
        var client = await LoadClientAsync(c => c.GuidId == clientGuidId)
            ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

        var resolved = RiFormatResolver.Resolve(client);
        return new ResolvedRiFormatDto(
            resolved.Source.ToString(),
            resolved.ClientTypeName,
            resolved.Template?.GuidId,
            resolved.Template?.Name);
    }

    public async Task<ClientLogoDto> GetClientLogoAsync(string clientGuidId)
    {
        var clientId = await ClientIdAsync(clientGuidId);
        return new ClientLogoDto(await GetLogoForClientAsync(clientId));
    }

    public async Task SetClientLogoAsync(string clientGuidId, ClientLogoDto dto)
    {
        var clientId = await ClientIdAsync(clientGuidId);
        var existing = await context.Set<ClientLogo>().FirstOrDefaultAsync(l => l.ClientId == clientId);

        if (string.IsNullOrWhiteSpace(dto.DataUri))
        {
            if (existing is not null)
            {
                context.Set<ClientLogo>().Remove(existing);
                await context.SaveChangesAsync();
            }

            return;
        }

        var error = RiLogoValidator.Validate(dto.DataUri);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        if (existing is null)
        {
            context.Set<ClientLogo>().Add(new ClientLogo
            {
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTime.UtcNow,
                ClientId = clientId,
                DataUri = dto.DataUri.Trim(),
            });
        }
        else
        {
            existing.DataUri = dto.DataUri.Trim();
            existing.LastUpdateUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>Logo del cliente o null (usado por <see cref="CertificationRiModelService"/>).</summary>
    public Task<string?> GetLogoForClientAsync(int clientId) =>
        context.Set<ClientLogo>()
            .Where(l => l.ClientId == clientId)
            .Select(l => (string?)l.DataUri)
            .FirstOrDefaultAsync();

    private async Task<int> ClientIdAsync(string clientGuidId) =>
        (await context.Set<Client>().Where(c => c.GuidId == clientGuidId).Select(c => (int?)c.ClientId).FirstOrDefaultAsync())
        ?? throw new InvalidOperationException($"Cliente {clientGuidId} no encontrado.");

    /// <summary>Formato efectivo de un cliente (usado por <see cref="CertificationRiModelService"/>).</summary>
    public async Task<ResolvedRiFormat> ResolveForClientAsync(int clientId)
    {
        var client = await LoadClientAsync(c => c.ClientId == clientId)
            ?? throw new InvalidOperationException("Cliente no encontrado.");
        return RiFormatResolver.Resolve(client);
    }

    // ---------------------------------------------------------------- helpers

    private Task<Client?> LoadClientAsync(Expression<Func<Client, bool>> predicate) =>
        context.Set<Client>()
            .Include(c => c.RiHtmlTemplate)
            .Include(c => c.ClientType).ThenInclude(t => t!.RiHtmlTemplate)
            .FirstOrDefaultAsync(predicate);

    private async Task<int?> ResolveTemplateIdAsync(string? templateGuidId)
    {
        if (string.IsNullOrWhiteSpace(templateGuidId))
        {
            return null;
        }

        var template = await context.Set<RiHtmlTemplate>().FirstOrDefaultAsync(t => t.GuidId == templateGuidId)
            ?? throw new InvalidOperationException("Formato no encontrado.");
        return template.RiHtmlTemplateId;
    }

    private async Task<int> UsageCountAsync(int templateId)
    {
        var types = await context.Set<ClientType>().CountAsync(t => t.RiHtmlTemplateId == templateId);
        var clients = await context.Set<Client>().CountAsync(c => c.RiHtmlTemplateId == templateId);
        return types + clients;
    }

    private async Task<RiHtmlTemplateDto> ToDtoAsync(RiHtmlTemplate t) =>
        new(t.GuidId, t.Name, t.FileName, t.RegisteredAt, await UsageCountAsync(t.RiHtmlTemplateId));

    private async Task<ClientTypeDto> GetTypeDtoAsync(string guidId)
    {
        var type = await context.Set<ClientType>().Include(t => t.RiHtmlTemplate).FirstAsync(t => t.GuidId == guidId);
        var count = await context.Set<Client>().CountAsync(c => c.ClientTypeId == type.ClientTypeId);
        return ToDto(type, count);
    }

    private static ClientTypeDto ToDto(ClientType t, int clientCount) =>
        new(t.GuidId, t.Name, t.Description, t.RiHtmlTemplate?.GuidId, t.RiHtmlTemplate?.Name, t.SampleDataJson, clientCount);

    private static string ValidateTypeDto(ClientTypeSaveDto dto)
    {
        var name = dto.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new InvalidOperationException("El nombre del tipo de cliente es obligatorio.");
        }

        return name;
    }

    /// <summary>Acepta null/vacío; si trae contenido debe ser un objeto JSON plano de strings.</summary>
    private static string? NormalizeSamples(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                         ?? throw new InvalidOperationException("Los datos de muestra deben ser un objeto JSON.");
            return JsonSerializer.Serialize(parsed);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Los datos de muestra deben ser un objeto JSON plano de texto, ej. {\"Medico\":\"Dr. X\"}.");
        }
    }
}
