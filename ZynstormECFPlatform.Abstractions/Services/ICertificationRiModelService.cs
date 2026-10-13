namespace ZynstormECFPlatform.Abstractions.Services;

/// <param name="ContentType">"application/pdf" (formato por defecto) o "text/html" (formato HTML del cliente).</param>
public record RiRenderResult(byte[] Content, string ContentType, string FileName);

/// <summary>
/// Renders the Ri (Representación Impresa) for certification documents, either
/// individually or in bulk as a ZIP, dispatching by e-CF type and by the client's format.
/// </summary>
public interface ICertificationRiModelService
{
    Task<byte[]> RenderRiForDocumentAsync(string clientGuidId, string ncf);

    /// <summary>PDF por defecto, o HTML si el cliente (o su tipo) tiene un formato HTML.</summary>
    Task<RiRenderResult> RenderRiAsync(string clientGuidId, string ncf);

    Task<byte[]> RenderAllZipAsync(string clientGuidId, string webRootPath);
}
