using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public enum PaymentRegistrationOutcome
{
    Ok,
    NotFound,
    Invalid,
    Conflict
}

public sealed record PaymentRegistrationResult<T>(PaymentRegistrationOutcome Outcome, T? Value, List<string> Errors)
{
    public static PaymentRegistrationResult<T> Success(T value) => new(PaymentRegistrationOutcome.Ok, value, []);
    public static PaymentRegistrationResult<T> NotFound(string message) => new(PaymentRegistrationOutcome.NotFound, default, [message]);
    public static PaymentRegistrationResult<T> Invalid(List<string> errors) => new(PaymentRegistrationOutcome.Invalid, default, errors);
    public static PaymentRegistrationResult<T> Invalid(string message) => new(PaymentRegistrationOutcome.Invalid, default, [message]);
    public static PaymentRegistrationResult<T> Conflict(string message) => new(PaymentRegistrationOutcome.Conflict, default, [message]);
}

public interface IClientPaymentRegistrationService
{
    Task<PaymentRegistrationResult<PaymentPreviewDto>> GetPreviewAsync(string clientGuid, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string clientGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default);

    Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string clientGuid, CancellationToken cancellationToken = default);

    /// <summary>Excedentes de meses cerrados sin pagar, por ClientId.</summary>
    Task<Dictionary<int, List<PendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken = default);
}
