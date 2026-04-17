using System.Collections.Generic;
using System.Threading.Tasks;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface ICertificationService
{
    Task<List<CertificationTestDto>> GetTestsAsync();
    Task<DgiiTransmissionResult> RunTestAsync(int index);
    Task<CertificationSummaryDto> GetSummaryAsync();
}
