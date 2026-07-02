namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Renders the Ri (Representación Impresa) PDF for certification documents, either
/// individually or in bulk as a ZIP, dispatching by e-CF type.
/// </summary>
public interface ICertificationRiModelService
{
    Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf);

    Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath);
}
