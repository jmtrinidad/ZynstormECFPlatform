using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Excedentes de meses cerrados sin pagar. Función pura: recibe los datos ya leídos.</summary>
public static class PendingOverageCalculator
{
    public static Dictionary<int, List<AccountPendingOverageDto>> ForAccounts(
        IEnumerable<BillingAccountMonthlyUsage> usages,
        ISet<int> paidUsageIds,
        IEnumerable<PlanOverageTier> tiers,
        DateTime today)
    {
        var tierList = tiers.ToList();

        return usages
            .Where(u => !paidUsageIds.Contains(u.BillingAccountMonthlyUsageId)
                        && PaymentRegistrationPolicy.IsOverageMonthPayable(u.Year, u.Month, today))
            .Select(u =>
            {
                var calculation = BillingCalculator.Calculate(
                    u.MonthlyFee,
                    u.MonthlyDocumentLimit,
                    tierList.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                    u.AcceptedDocuments);

                return (u.BillingAccountId, Pending: new AccountPendingOverageDto
                {
                    BillingAccountMonthlyUsageId = u.BillingAccountMonthlyUsageId,
                    Year = u.Year,
                    Month = u.Month,
                    PlanId = u.PlanId,
                    PlanName = u.PlanName,
                    AcceptedDocuments = u.AcceptedDocuments,
                    MonthlyDocumentLimit = u.MonthlyDocumentLimit,
                    OverageDocuments = calculation.OverageDocuments,
                    Amount = calculation.OverageAmount
                });
            })
            .Where(x => x.Pending.Amount > 0)
            .GroupBy(x => x.BillingAccountId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Pending).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList());
    }
}
