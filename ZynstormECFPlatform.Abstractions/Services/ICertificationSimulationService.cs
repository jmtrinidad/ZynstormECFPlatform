using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationSimulationService
{
    Task<string> EnqueueSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string webRootPath);
    Task ProcessSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string jobId, string webRootPath);
    Task<string> ProcessSimulacionUnoAUnoAsync(EcfInvoiceRequestDto dto, string webRootPath);
}
