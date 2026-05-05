using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationSimulationMappingService
{
    EcfInvoiceRequestDto PrepareSimulationStep4Xml(EcfInvoiceRequestDto dto);
}
