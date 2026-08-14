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
        CancellationToken cancellationToken = default);
}

public class ReceivedEcfEmissionResultDto
{
    public bool Success { get; set; }
    public bool IsPending { get; set; }
    public bool IsAcceptedConditional { get; set; }
    public bool RequiresCorrection { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EcfDocumentId { get; set; }
    public int EcfType { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string TargetEnvironment { get; set; } = string.Empty;
    public string SecurityCode { get; set; } = string.Empty;
    public string SignatureDate { get; set; } = string.Empty;
    public string QrUrl { get; set; } = string.Empty;

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
