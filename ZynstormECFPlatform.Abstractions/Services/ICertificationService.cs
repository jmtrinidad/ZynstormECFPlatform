using System.Collections.Generic;
using System.Threading.Tasks;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationService
{
    Task<List<CertificationTestDto>> GetTestsAsync();
    Task<DgiiTransmissionResult> RunTestAsync(int index, string webRootPath);
    Task<CertificationSummaryDto> GetSummaryAsync();
    
    // Automation Job Methods
    Task<string> EnqueueCertificationJobAsync(byte[] excelBytes, string fileName, string webRootPath);
    Task ProcessAutomationJobAsync(string tempFilePath, string jobId, string webRootPath);
    Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId);
    Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId);

    // AEC Processing
    Task<List<DgiiTransmissionResult>> ProcessAprobacionComercialAsync(byte[] excelBytes);
}
