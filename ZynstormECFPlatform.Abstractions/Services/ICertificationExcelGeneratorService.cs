using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationExcelGeneratorService
{
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false);
}
