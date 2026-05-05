using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEcfProductionGeneratorService
{
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false);
}
