using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

public interface IReceivedEcfProductionService
{
    Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750);
}

public class ReceivedEcfEmissionResultDto
{
    public bool Success { get; set; }
    public bool IsPending { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EcfDocumentId { get; set; }
    public int EcfType { get; set; }
    public string ENcf { get; set; } = string.Empty;
    public string TrackId { get; set; } = string.Empty;
    public string SecurityCode { get; set; } = string.Empty;
    public string SignatureDate { get; set; } = string.Empty;
    public string QrUrl { get; set; } = string.Empty;
    public string QrImageUrl { get; set; } = string.Empty;
    public string? HangfireJobId { get; set; }
    public string UnsignedXml { get; set; } = string.Empty;
    public string SignedXml { get; set; } = string.Empty;
    public List<string> DtoErrors { get; set; } = [];
    public List<string> XsdErrors { get; set; } = [];
    public List<string> XmlProdErrors { get; set; } = [];
    public DgiiTransmissionResult? Transmission { get; set; }
    public DgiiStatusResponse? Status { get; set; }
}
