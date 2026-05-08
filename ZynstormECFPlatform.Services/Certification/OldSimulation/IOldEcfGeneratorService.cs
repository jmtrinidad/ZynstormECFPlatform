namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public interface IOldEcfGeneratorService
{
    string GenerateUnsignedXml(OldEcfInvoiceRequestDto dto, bool isSummary = false);
    List<string> ValidateXmlAgainstSchema(string xml, int ecfType);
    List<string> ValidateXmlAgainstReference(string xml, int ecfType, string referenceXmlPath);
    List<string> ValidateDto(OldEcfInvoiceRequestDto dto);
}
