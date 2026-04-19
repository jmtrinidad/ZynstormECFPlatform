using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public class DgiiTransmissionResult
{
    public string TrackId { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;

    // RFCE Specific fields
    public int? Codigo { get; set; }
    public string? Estado { get; set; }
    public List<RfceMensaje>? Mensajes { get; set; }
    public string? Encf { get; set; }
    public bool? SecuenciaUtilizada { get; set; }

    public bool Success => (!string.IsNullOrEmpty(TrackId) || Estado == "Aceptado" || Codigo == 0) && string.IsNullOrEmpty(Error);
}

public class RfceMensaje
{
    public string Codigo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}

/// <summary>
/// Service to handle transmission of e-CF documents to the DGII API.
/// </summary>
public interface IDgiiTransmissionService
{
    /// <summary>
    /// Sends a signed e-CF to the corresponding DGII endpoint depending on the document type and amount.
    /// </summary>
    Task<DgiiTransmissionResult> SendEcfAsync(DgiiEnvironment environment, string token, string signedXml, int ecfType, decimal totalAmount, string rncEmisor, string eNcf, bool isSummary = false);

    /// <summary>
    /// Queries the status of a document by its trackId from the DGII Consulta result endpoint.
    /// </summary>
    Task<DgiiStatusResponse> GetStatusAsync(DgiiEnvironment environment, string token, string trackId);
}
