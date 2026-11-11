using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Mensualidad que se registraría hoy con las condiciones de cobro de un cliente o una cuenta.</summary>
public static class PlanFeePreviewBuilder
{
    public static PlanFeePreviewDto Build(BillingTerms terms, DateTime today)
    {
        var calculation = PaymentCalculator.CalculateCycle(terms, today);

        return new PlanFeePreviewDto
        {
            PlanName = terms.Plan.Name,
            PlanTypeId = terms.Plan.PlanTypeId,
            MonthlyFee = calculation.MonthlyFee,
            MonthsCovered = calculation.MonthsCovered,
            GrossAmount = calculation.GrossAmount,
            DiscountPercent = calculation.DiscountPercent,
            DiscountAmount = calculation.DiscountAmount,
            Total = calculation.Total,
            OfferMonthsCovered = calculation.OfferMonthsCovered,
            OfferUnitPrice = calculation.OfferUnitPrice,
            CurrentNextPaymentDate = terms.NextPaymentDate?.Date,
            NewNextPaymentDate = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(terms.NextPaymentDate, today, calculation.MonthsCovered)
        };
    }
}
