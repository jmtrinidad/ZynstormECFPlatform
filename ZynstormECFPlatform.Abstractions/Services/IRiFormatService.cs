using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>Biblioteca de formatos HTML de RI, tipos de cliente y su asignación a clientes.</summary>
public interface IRiFormatService
{
    Task<IReadOnlyList<ClientTypeDto>> ListClientTypesAsync();

    Task<ClientTypeDto> CreateClientTypeAsync(ClientTypeSaveDto dto);

    Task<ClientTypeDto> UpdateClientTypeAsync(string guidId, ClientTypeSaveDto dto);

    Task DeleteClientTypeAsync(string guidId);

    /// <summary>Agrega un formato de la biblioteca a los disponibles del tipo; si el tipo no tenía predeterminado, queda como tal.</summary>
    Task<ClientTypeDto> AddTemplateToClientTypeAsync(string clientTypeGuidId, string templateGuidId);

    /// <summary>Quita un formato de los disponibles del tipo. No se puede quitar el predeterminado.</summary>
    Task<ClientTypeDto> RemoveTemplateFromClientTypeAsync(string clientTypeGuidId, string templateGuidId);

    Task<IReadOnlyList<RiHtmlTemplateDto>> ListTemplatesAsync();

    Task<RiHtmlTemplateUploadResultDto> UploadTemplateAsync(RiHtmlTemplateUploadDto dto);

    Task DeleteTemplateAsync(string guidId);

    Task AssignToClientAsync(string clientGuidId, ClientRiFormatAssignDto dto);

    Task<ResolvedRiFormatDto> GetResolvedAsync(string clientGuidId);

    /// <summary>Logo del cliente (data URI) o null si no tiene.</summary>
    Task<ClientLogoDto> GetClientLogoAsync(string clientGuidId);

    /// <summary>Guarda el logo del cliente; null o vacío lo quita. Lanza InvalidOperationException si no es válido.</summary>
    Task SetClientLogoAsync(string clientGuidId, ClientLogoDto dto);
}
