namespace ZynstormECFPlatform.Dtos;

public record ClientTypeDto(
    string GuidId,
    string Name,
    string? Description,
    string? RiHtmlTemplateGuidId,
    string? RiHtmlTemplateName,
    string? SampleDataJson,
    int ClientCount);

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

public record ClientRiFormatAssignDto(string? ClientTypeGuidId, string? RiHtmlTemplateGuidId);

/// <param name="Source">"Default" | "ClientType" | "Client".</param>
public record ResolvedRiFormatDto(
    string Source,
    string? ClientTypeName,
    string? TemplateGuidId,
    string? TemplateName);
