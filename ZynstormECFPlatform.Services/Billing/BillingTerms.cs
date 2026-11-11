using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Condiciones de cobro de quien paga un plan: un cliente suelto o una cuenta de facturación.</summary>
public sealed record BillingTerms(
    Plan Plan,
    decimal? CustomMonthlyFee,
    DateTime? OfferStartDate,
    int PaidMonths,
    decimal PrepaymentDiscountPercent,
    DateTime? NextPaymentDate)
{
    public static BillingTerms For(Client client) => new(
        client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client)),
        client.CustomMonthlyFee,
        client.OfferStartDate,
        client.PaidMonths,
        client.PrepaymentDiscountPercent,
        client.NextPaymentDate);

    public static BillingTerms For(BillingAccount account) => new(
        account.Plan ?? throw new ArgumentException("La cuenta no tiene el plan cargado.", nameof(account)),
        account.CustomMonthlyFee,
        account.OfferStartDate,
        account.PaidMonths,
        account.PrepaymentDiscountPercent,
        account.NextPaymentDate);
}
