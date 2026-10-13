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
    public string? SignedXml { get; set; }

    /// <summary>
    /// No hubo respuesta de la DGII: timeout o red caída. Es lo único que se reintenta de forma
    /// automática, porque no se sabe si el documento llegó.
    /// </summary>
    public bool TransportFailure { get; set; }

    public bool Success => (!string.IsNullOrEmpty(TrackId) || string.Equals(Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || Codigo == 0) && string.IsNullOrEmpty(Error);
}

/// <summary>Un TrackId que la DGII devolvió para un eNCF recibido (servicio «Consulta de TrackId e-CF»).</summary>
public class DgiiTrackIdEntry
{
    public string TrackId { get; set; } = string.Empty;
    public string? Estado { get; set; }
    public string? FechaRecepcion { get; set; }
}

public class DgiiTrackIdsResult
{
    /// <summary>false cuando la consulta no existe en ese ambiente (CerteCF, validador interno): no es un error.</summary>
    public bool Available { get; set; } = true;

    /// <summary>Texto del fallo (HTTP, red) o del motivo de no estar disponible; null si la consulta se hizo bien.</summary>
    public string? Error { get; set; }

    public List<DgiiTrackIdEntry> Entries { get; set; } = [];
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
    Task<DgiiStatusResponse> GetStatusAsync(
        DgiiEnvironment environment,
        string token,
        string trackId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consulta de TrackId e-CF de la DGII: los TrackId que recibió para un eNCF del emisor.
    /// Sirve para recuperar el TrackId cuando la respuesta de la recepción se perdió.
    /// </summary>
    Task<DgiiTrackIdsResult> GetTrackIdsAsync(
        DgiiEnvironment environment,
        string token,
        string rncEmisor,
        string eNcf,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an Aprobación Comercial (ARECF) document to the specific DGII endpoint.
    /// </summary>
    Task<DgiiTransmissionResult> SendArecfAsync(DgiiEnvironment environment, string token, string signedXml, string rncEmisor, string eNcf);
}
