using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEcfProductionGeneratorService
{
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false);
    List<string> ValidateXmlAgainstSchema(string xml, int ecfType);
    List<string> ValidateDto(EcfInvoiceRequestDto dto);
}
