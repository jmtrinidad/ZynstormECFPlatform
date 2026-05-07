using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationExcelMappingService
{
    EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int step, DateTime? fallbackDate = null);
    AcecfRequestDto MapRowToAcecfRequest(IDictionary<string, object> row, DateTime? fallbackDate = null);


}
