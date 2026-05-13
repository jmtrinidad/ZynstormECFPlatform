using ZynstormECFPlatform.Dtos;
using Hangfire;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public interface IOldCertificationSimulationService
{
    Task<string> EnqueueSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string webRootPath);
    Task<string> EnqueueBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string webRootPath);

    [AutomaticRetry(Attempts = 0)]
    Task ProcessSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath);

    [AutomaticRetry(Attempts = 0)]
    Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath);
    Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId);
    Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId);
}
