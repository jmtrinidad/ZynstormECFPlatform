using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEcfProductionService
{
    Task<DgiiTransmissionResult> EmitEcfAsync(EcfInvoiceRequestDto dto);
}
