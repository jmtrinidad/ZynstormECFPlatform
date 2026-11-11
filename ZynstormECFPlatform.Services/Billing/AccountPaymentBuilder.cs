using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record AccountPaymentDraft(ClientPayment Payment, DateTime? NewNextPaymentDate);

/// <summary>Arma el recibo de una cuenta a partir de lo que se paga. No modifica la cuenta ni toca la base de datos.</summary>
public static class AccountPaymentBuilder
{
    public static AccountPaymentDraft Build(
        BillingAccount account,
        RegisterPaymentRequestDto request,
        IReadOnlyCollection<AccountPendingOverageDto> selectedOverages,
        DateTime paymentDate,
        string? userId)
    {
        var date = paymentDate.Date;

        var payment = new ClientPayment
        {
            BillingAccountId = account.BillingAccountId,
            PaymentDate = date,
            PaymentMethod = request.PaymentMethod,
            Reference = Clean(request.Reference),
            Notes = Clean(request.Notes),
            RegisteredByUserId = userId
        };

        DateTime? newNext = null;

        if (request.IncludePlanFee)
        {
            var calculation = PaymentCalculator.CalculateCycle(BillingTerms.For(account), date);
            var previousNext = account.NextPaymentDate?.Date;
            newNext = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(previousNext, date, calculation.MonthsCovered);

            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.PlanFee,
                Amount = calculation.Total,
                PlanId = account.PlanId,
                PlanName = account.Plan.Name,
                MonthsCovered = calculation.MonthsCovered,
                GrossAmount = calculation.GrossAmount,
                DiscountPercent = calculation.DiscountPercent,
                DiscountAmount = calculation.DiscountAmount,
                OfferMonthsCovered = calculation.OfferMonthsCovered > 0 ? calculation.OfferMonthsCovered : null,
                OfferUnitPrice = calculation.OfferUnitPrice,
                PreviousNextPaymentDate = previousNext,
                NewNextPaymentDate = newNext
            });
        }

        foreach (var overage in selectedOverages.OrderBy(o => o.Year).ThenBy(o => o.Month))
        {
            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.Overage,
                Amount = overage.Amount,
                PlanId = overage.PlanId,
                PlanName = overage.PlanName,
                BillingAccountMonthlyUsageId = overage.BillingAccountMonthlyUsageId,
                Year = overage.Year,
                Month = overage.Month,
                OverageDocuments = overage.OverageDocuments
            });
        }

        payment.TotalAmount = payment.Items.Sum(i => i.Amount);

        return new AccountPaymentDraft(payment, newNext);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
