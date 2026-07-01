using ZynstormECFPlatform.Dtos.Ri;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Orchestrates Ri (Representación Impresa) print templates: extraction of a layout
/// from a client-provided PDF sample, reference-RI generation, ecfType assignment,
/// confirmation, and rendering of the final RI for certification documents.
/// </summary>
public interface ICertificationRiModelService
{
    Task<RiModelDto> UploadAsync(string clientGuidId, string name, IReadOnlyList<string> ecfTypeCodes, byte[] sourcePdf, string fileName);

    Task<RiModelDto> UpdateAsync(string templateGuidId, string? name, IReadOnlyList<string>? ecfTypeCodes, bool? confirm, byte[]? newSourcePdf, string? fileName);

    Task<List<RiModelListItemDto>> ListByClientAsync(string clientGuidId);

    Task<byte[]?> GetReferenceRiAsync(string templateGuidId);

    Task DeleteAsync(string templateGuidId);

    Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf);

    Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath);
}
