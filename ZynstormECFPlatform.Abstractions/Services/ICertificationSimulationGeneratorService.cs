using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationSimulationGeneratorService
{
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false);
}
