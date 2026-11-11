using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public interface IBillingAccountPaymentService
{
    Task<PaymentRegistrationResult<AccountPaymentPreviewDto>> GetPreviewAsync(string accountGuid, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string accountGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string accountGuid, CancellationToken cancellationToken = default);

    /// <summary>Excedentes del pool de meses cerrados sin pagar, por BillingAccountId.</summary>
    Task<Dictionary<int, List<AccountPendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default);
}
