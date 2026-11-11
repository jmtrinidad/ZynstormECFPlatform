using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>
/// Fila del reporte de consumo mensual para una cuenta de facturación, en el mismo formato que la de un cliente
/// para que la pantalla de consumo las muestre juntas. Función pura.
/// </summary>
public static class AccountUsageReportBuilder
{
    public static ClientMonthlyUsageDto Build(
        BillingAccount account,
        BillingAccountMonthlyUsage usage,
        IEnumerable<PlanOverageTier> tiers)
    {
        // El precio propio de la cuenta reemplaza la copia del precio del plan guardada al crear el mes.
        var fee = account.CustomMonthlyFee ?? usage.MonthlyFee;

        var calculation = BillingCalculator.Calculate(
            fee,
            usage.MonthlyDocumentLimit,
            tiers.Where(t => t.PlanId == usage.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
            usage.AcceptedDocuments);

        // El pago adelantado cubre solo la mensualidad; el excedente se cobra siempre.
        var payment = PaymentCalculator.Calculate(fee, account.PaidMonths, account.PrepaymentDiscountPercent);
        var feeCharged = PaymentCalculator.GetAmountForMonth(payment, account.NextPaymentDate, usage.Year, usage.Month);

        return new ClientMonthlyUsageDto
        {
            ClientGuidId = account.GuidId,
            ClientName = account.Name,
            ClientRnc = account.OwnerUser?.FullName ?? string.Empty,
            ClientInactive = false,
            IsAccount = true,
            Year = usage.Year,
            Month = usage.Month,
            PlanName = usage.PlanName,
            PlanTypeId = (int)PlanTypeEnum.Documents,
            MonthlyFee = calculation.MonthlyFee,
            MonthlyDocumentLimit = calculation.MonthlyDocumentLimit,
            AcceptedDocuments = calculation.AcceptedDocuments,
            OverageDocuments = calculation.OverageDocuments,
            OverageAmount = calculation.OverageAmount,
            MonthlyFeeCharged = feeCharged,
            MonthlyFeeCovered = PaymentCalculator.IsMonthCovered(account.NextPaymentDate, usage.Year, usage.Month),
            Total = feeCharged + calculation.OverageAmount,
            Tiers = calculation.Tiers.Select(t => new OverageTierChargeDto
            {
                FromUnit = t.FromUnit,
                ToUnit = t.ToUnit,
                UnitPrice = t.UnitPrice,
                Units = t.Units,
                Amount = t.Amount
            }).ToList()
        };
    }
}
