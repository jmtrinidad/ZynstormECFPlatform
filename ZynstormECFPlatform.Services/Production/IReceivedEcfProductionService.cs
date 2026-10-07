using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Services.Production;

public interface IReceivedEcfProductionService
{
    Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750,
        CancellationToken cancellationToken = default,
        bool deferred = false);

    /// <summary>
    /// Transmite a la DGII un documento que <c>emit</c> dejó firmado y en cola (estado 7), y sigue
    /// su estado. Lo ejecuta <c>EcfTransmitJob</c>. Devuelve cuánto esperar para reintentar si
    /// hubo un fallo de transporte; sin espera, no queda nada por hacer.
    /// </summary>
    Task<DeferredTransmitOutcome> TransmitDeferredAsync(
        int ecfDocumentId,
        int attemptNumber,
        DgiiEnvironment environment,
        CancellationToken cancellationToken = default);
}

public sealed record DeferredTransmitOutcome(TimeSpan? RetryAfter);

public class ReceivedEcfEmissionResultDto
{
    public bool Success { get; set; }
    public bool IsPending { get; set; }
    public bool IsAcceptedConditional { get; set; }
    public bool RequiresCorrection { get; set; }
    public bool ClientInactive { get; set; }

    [JsonIgnore]
    public bool HasUnexpectedError { get; set; }

    public List<string> ConfigurationErrors { get; set; } = [];
    public string Message { get; set; } = string.Empty;
    public int EcfDocumentId { get; set; }
    public int EcfType { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string SecurityCode { get; set; } = string.Empty;
    public string SignatureDate { get; set; } = string.Empty;
    public string QrUrl { get; set; } = string.Empty;

    /// <summary>La plataforma no transmitió: devolvió lo que ya tenía guardado para ese eNCF.</summary>
    public bool Replayed { get; set; }

    /// <summary>Cantidad de documentos de la plataforma con este eNCF, contando este.</summary>
    public int Attempt { get; set; }

    [JsonIgnore]
    public string QrImageUrl { get; set; } = string.Empty;

    public string? HangfireJobId { get; set; }

    [JsonIgnore]
    public EcfXmlValidationResult? XmlValidation { get; set; }

    public string UnsignedXml { get; set; } = string.Empty;
    public string SignedXml { get; set; } = string.Empty;
    public List<string> DtoErrors { get; set; } = [];
    public List<string> XsdErrors { get; set; } = [];
    public List<string> XmlProdErrors { get; set; } = [];
    public DgiiTransmissionResult? Transmission { get; set; }
    public DgiiStatusResponse? Status { get; set; }
    public DgiiStatusResponse? DgiiResponse { get; set; }
}
