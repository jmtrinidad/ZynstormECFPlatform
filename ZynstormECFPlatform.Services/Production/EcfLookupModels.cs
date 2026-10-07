using System.Text.Json.Serialization;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfLookupState
{
    Accepted,
    AcceptedConditional,
    Pending,
    Rejected,
    Error,
    NotSent
}

/// <summary>Un documento de la plataforma con ese eNCF, reducido a lo que hace falta para elegir.</summary>
public sealed record EcfLookupCandidate(int EcfDocumentId, EcfLookupState State, DateTime RegisteredAt);

/// <summary>Los datos ya leídos de la base con los que se arma la respuesta.</summary>
public sealed record EcfLookupSource(
    int EcfDocumentId,
    string ENcf,
    decimal Total,
    DateTime IssueDateUtc,
    DateTime? SignatureDateTime,
    string IssuerRnc,
    string CustomerRnc,
    string? TrackId,
    string? SignedXml);

public sealed record EcfLookupResponse
{
    public bool Found { get; init; } = true;

    public string ENcf { get; init; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EcfLookupState State { get; init; }

    public bool IsUsable { get; init; }

    public string? TrackId { get; init; }

    public string? SecurityCode { get; init; }

    public string? SignatureDate { get; init; }

    public string? QrUrl { get; init; }

    public int EcfDocumentId { get; init; }

    public decimal Total { get; init; }

    public DateTime IssueDate { get; init; }

    /// <summary>Cantidad de documentos de la plataforma con ese eNCF (envíos y reenvíos).</summary>
    public int Attempts { get; init; }

    public string Message { get; init; } = string.Empty;
}
