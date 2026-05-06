using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationExcelGeneratorService
{
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto, bool isSummary = false);
    List<string> ValidateXmlAgainstSchema(string xml, int ecfType);
}
