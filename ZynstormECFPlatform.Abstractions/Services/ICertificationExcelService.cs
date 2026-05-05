using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationExcelService
{
    Task<List<CertificationTestDto>> GetTestsAsync();
    Task<DgiiTransmissionResult> RunTestAsync(int index, string webRootPath);
    Task<CertificationSummaryDto> GetSummaryAsync();
    Task<string> EnqueueCertificationJobAsync(byte[] excelBytes, string fileName, string webRootPath);
    Task ProcessAutomationJobAsync(string tempFilePath, string jobId, string webRootPath);
    Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId);
    Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId);
    Task<List<DgiiTransmissionResult>> ProcessAprobacionComercialAsync(byte[] excelBytes);
    Task<(byte[] content, string fileName)> SignXmlAsync(Stream xmlStream, string rnc);
}
