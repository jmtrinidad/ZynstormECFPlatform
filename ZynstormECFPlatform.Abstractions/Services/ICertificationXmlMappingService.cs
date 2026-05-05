using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationXmlMappingService
{
    EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int step, DateTime? fallbackDate = null);
    EcfInvoiceRequestDto PrepareExcelCertificationXml(EcfInvoiceRequestDto dto);
    EcfInvoiceRequestDto PrepareSimulationStep4Xml(EcfInvoiceRequestDto dto);
}
