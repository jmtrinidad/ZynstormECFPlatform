using System.Threading.Tasks;

namespace ZynstormECFPlatform.Abstractions.Services;

public class DgiiTransmissionResult
{
    public string TrackId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public bool Success => !string.IsNullOrEmpty(TrackId) && string.IsNullOrEmpty(Error);
}

/// <summary>
/// Service to handle transmission of e-CF documents to the DGII API.
/// </summary>
public interface IDgiiTransmissionService
{
    /// <summary>
    /// Sends a signed e-CF to the corresponding DGII endpoint depending on the document type and amount.
    /// </summary>
    Task<DgiiTransmissionResult> SendEcfAsync(bool isProduction, string token, string signedXml, int ecfType, decimal totalAmount, string rncEmisor, string eNcf);
}
