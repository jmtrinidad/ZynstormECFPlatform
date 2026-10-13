namespace ZynstormECFPlatform.Dtos;

/// <param name="RiHtmlTemplateGuidId">Formato predeterminado del tipo (null si no tiene).</param>
/// <param name="Formats">Todos los formatos disponibles del tipo, con el predeterminado marcado.</param>
public record ClientTypeDto(
    string GuidId,
    string Name,
    string? Description,
    string? RiHtmlTemplateGuidId,
    string? RiHtmlTemplateName,
    string? SampleDataJson,
    int ClientCount,
    IReadOnlyList<ClientTypeFormatDto> Formats);

public record ClientTypeFormatDto(string GuidId, string Name, bool IsDefault);

public record ClientTypeSaveDto(
    string Name,
    string? Description,
    string? RiHtmlTemplateGuidId,
    string? SampleDataJson);

public record RiHtmlTemplateDto(
    string GuidId,
    string Name,
    string FileName,
    DateTime RegisteredAt,
    int InUseCount);

public record RiHtmlTemplateUploadDto(string Name, string FileName, string Html);

public record RiHtmlTemplateUploadResultDto(
    RiHtmlTemplateDto? Template,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

/// <param name="DataUri">data:image/png|jpeg|webp;base64,... ; null/vacío = sin logo.</param>
public record ClientLogoDto(string? DataUri);

public record ClientRiFormatAssignDto(string? ClientTypeGuidId, string? RiHtmlTemplateGuidId);

/// <param name="Source">"Default" | "ClientType" | "Client".</param>
public record ResolvedRiFormatDto(
    string Source,
    string? ClientTypeName,
    string? TemplateGuidId,
    string? TemplateName);
