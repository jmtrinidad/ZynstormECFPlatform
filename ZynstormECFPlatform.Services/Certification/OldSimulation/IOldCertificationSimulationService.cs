using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public interface IOldCertificationSimulationService
{
    Task<string> EnqueueSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string webRootPath);
    Task<string> EnqueueBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string webRootPath);
    Task ProcessSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath);
    Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath);
    Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId);
    Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId);
}
