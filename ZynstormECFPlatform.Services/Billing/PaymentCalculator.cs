using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentCalculationResult(
    decimal MonthlyFee,
    int MonthsCovered,
    decimal GrossAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total);

/// <summary>
/// Cálculo del pago por adelantado de un plan (renta o mensualidad de comprobantes).
/// Función pura: no toca base de datos ni configuración.
/// </summary>
public static class PaymentCalculator
{
    /// <summary>Días de anticipación para considerar un pago próximo a vencer.</summary>
    public const int DefaultWarningDays = 15;

    public static PaymentCalculationResult Calculate(decimal monthlyFee, int paidMonths, decimal discountPercent)
    {
        var months = Math.Max(1, paidMonths);
        var gross = monthlyFee * months;
        var discount = Math.Round(gross * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new PaymentCalculationResult(
            monthlyFee,
            months,
            gross,
            discountPercent,
            discount,
            gross - discount);
    }

    /// <summary>
    /// Monto de la mensualidad a facturar en el mes consultado. Si el próximo pago cae después de ese mes,
    /// el mes ya está cubierto y no se cobra nada; si cae en ese mes o antes (vencido), o no hay fecha,
    /// se cobra el ciclo completo.
    /// </summary>
    public static decimal GetAmountForMonth(PaymentCalculationResult result, DateTime? nextPaymentDate, int year, int month) =>
        IsMonthCovered(nextPaymentDate, year, month) ? 0m : result.Total;

    public static bool IsMonthCovered(DateTime? nextPaymentDate, int year, int month) =>
        nextPaymentDate is DateTime due && due.Date >= new DateTime(year, month, 1).AddMonths(1);

    /// <summary>
    /// Estado del próximo pago y días calendario que faltan (negativo si ya venció).
    /// <paramref name="today"/> solo se pasa en pruebas; por defecto usa la fecha en hora RD.
    /// </summary>
    public static (PaymentStatus Status, int? DaysToDue) GetPaymentStatus(
        DateTime? nextPaymentDate,
        int warningDays,
        DateTime? today = null)
    {
        if (nextPaymentDate is not DateTime due)
            return (PaymentStatus.NoDate, null);

        var reference = (today ?? DateTimeExtensions.DrNow).Date;
        var days = (due.Date - reference).Days;

        if (days < 0) return (PaymentStatus.Overdue, days);
        if (days <= warningDays) return (PaymentStatus.DueSoon, days);
        return (PaymentStatus.Current, days);
    }
}
