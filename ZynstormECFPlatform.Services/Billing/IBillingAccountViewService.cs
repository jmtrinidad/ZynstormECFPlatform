using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountViewService
{
    Task<List<BillingAccountViewDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<BillingAccountDetailDto?> GetDetailAsync(string accountGuid, CancellationToken cancellationToken = default);

    /// <summary>GUID de la cuenta de la que el usuario es dueño; null si no tiene.</summary>
    Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    /// <summary>Fija o quita el cupo informativo de un cliente que pertenece a la cuenta.</summary>
    Task<PaymentRegistrationResult<bool>> SetClientQuotaAsync(
        string accountGuid, string clientGuid, int? quota, CancellationToken cancellationToken = default);
}
