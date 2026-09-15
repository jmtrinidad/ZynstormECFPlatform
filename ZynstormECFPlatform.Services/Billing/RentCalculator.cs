using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record RentCalculationResult(
    decimal MonthlyFee,
    int MonthsCovered,
    decimal GrossAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total);

/// <summary>
/// Cálculo de la renta de un cliente con plan de tipo <see cref="PlanTypeEnum.Rent"/>.
/// Función pura: no toca base de datos ni configuración.
/// </summary>
public static class RentCalculator
{
    /// <summary>-1 en <c>Plan.MaxUsers</c> significa usuarios ilimitados.</summary>
    public const int UnlimitedUsers = -1;

    /// <summary>Días de anticipación para considerar una renta próxima a vencer.</summary>
    public const int DefaultWarningDays = 15;

    private const int MonthsInFullYear = 12;

    public static RentCalculationResult Calculate(decimal monthlyFee, bool paidFullYear, decimal discountPercent)
    {
        var months = paidFullYear ? MonthsInFullYear : 1;
        var gross = monthlyFee * months;
        var discount = Math.Round(gross * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new RentCalculationResult(
            monthlyFee,
            months,
            gross,
            discountPercent,
            discount,
            gross - discount);
    }

    /// <summary>
    /// Monto a facturar en el mes consultado. Si el próximo pago cae después de ese mes,
    /// el mes ya está cubierto y no se cobra nada; si cae en ese mes o antes (vencido),
    /// se cobra el ciclo completo.
    /// </summary>
    public static decimal GetAmountForMonth(RentCalculationResult result, DateTime? nextPaymentDate, int year, int month)
    {
        if (nextPaymentDate is not DateTime due)
            return result.Total;

        var firstDayAfterMonth = new DateTime(year, month, 1).AddMonths(1);
        return due.Date < firstDayAfterMonth ? result.Total : 0m;
    }

    /// <summary>
    /// Estado del próximo pago y días calendario que faltan (negativo si ya venció).
    /// <paramref name="today"/> solo se pasa en pruebas; por defecto usa la fecha en hora RD.
    /// </summary>
    public static (RentStatus Status, int? DaysToDue) GetRentStatus(
        DateTime? nextPaymentDate,
        int warningDays,
        DateTime? today = null)
    {
        if (nextPaymentDate is not DateTime due)
            return (RentStatus.NoDate, null);

        var reference = (today ?? DateTimeExtensions.DrNow).Date;
        var days = (due.Date - reference).Days;

        if (days < 0) return (RentStatus.Overdue, days);
        if (days <= warningDays) return (RentStatus.DueSoon, days);
        return (RentStatus.Current, days);
    }

    public static List<string> ValidateRentPlan(decimal monthlyFee, int? maxUsers, bool hasOverageTiers)
    {
        var errors = new List<string>();

        if (monthlyFee < 0)
            errors.Add("La renta mensual no puede ser negativa.");

        if (maxUsers is not int users || (users != UnlimitedUsers && users <= 0))
            errors.Add("Los usuarios permitidos deben ser mayor que 0, o -1 para ilimitado.");

        if (hasOverageTiers)
            errors.Add("Un plan de renta no lleva tramos de excedente.");

        return errors;
    }
}
