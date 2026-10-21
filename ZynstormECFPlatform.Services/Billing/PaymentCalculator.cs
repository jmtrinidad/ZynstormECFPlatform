using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public sealed record PaymentCalculationResult(
    decimal MonthlyFee,
    int MonthsCovered,
    decimal GrossAmount,
    decimal DiscountPercent,
    decimal DiscountAmount,
    decimal Total,
    int OfferMonthsCovered = 0,
    decimal? OfferUnitPrice = null);

/// <summary>
/// Cálculo del pago por adelantado de un plan (renta o mensualidad de comprobantes).
/// Función pura: no toca base de datos ni configuración.
/// </summary>
public static class PaymentCalculator
{
    /// <summary>Días de anticipación para considerar un pago próximo a vencer.</summary>
    public const int DefaultWarningDays = 15;

    /// <summary>
    /// Con <paramref name="offer"/> y <paramref name="periodStart"/> cada mes cubierto se cobra a su precio:
    /// el mes k empieza en <c>periodStart + k meses</c> y usa el precio de oferta si esa fecha es anterior al
    /// fin de la oferta. Sin alguno de los dos se cobra el precio real en todos los meses.
    /// </summary>
    public static PaymentCalculationResult Calculate(
        decimal monthlyFee,
        int paidMonths,
        decimal discountPercent,
        OfferTerms? offer = null,
        DateTime? periodStart = null)
    {
        var months = Math.Max(1, paidMonths);
        var offerMonths = offer is not null && periodStart is DateTime start
            ? CountOfferMonths(offer, start, months)
            : 0;

        var gross = offerMonths * (offer?.OfferPrice ?? 0m) + (months - offerMonths) * monthlyFee;
        var discount = Math.Round(gross * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);

        return new PaymentCalculationResult(
            monthlyFee,
            months,
            gross,
            discountPercent,
            discount,
            gross - discount,
            offerMonths,
            offerMonths > 0 ? offer!.OfferPrice : null);
    }

    /// <summary>
    /// Precio mensual vigente del cliente: el propio si tiene uno y, si no, el de su plan.
    /// </summary>
    public static decimal GetMonthlyFee(Client client)
    {
        var plan = client.Plan ?? throw new ArgumentException("El cliente no tiene el plan cargado.", nameof(client));
        return client.CustomMonthlyFee ?? plan.MonthlyFee;
    }

    /// <summary>
    /// Ciclo de pago de un cliente con su precio vigente y su oferta. El primer mes cubierto empieza en el
    /// próximo pago, o en <paramref name="fallbackPeriodStart"/> si el cliente no tiene fecha. El precio
    /// propio reemplaza al del plan y también a su oferta.
    /// </summary>
    public static PaymentCalculationResult CalculateCycle(Client client, DateTime fallbackPeriodStart)
    {
        var monthlyFee = GetMonthlyFee(client);
        var offer = client.CustomMonthlyFee.HasValue
            ? null
            : OfferPricing.TermsFor(client.Plan!.OfferPrice, client.Plan.OfferMonths, client.OfferStartDate);

        return Calculate(
            monthlyFee,
            client.PaidMonths,
            client.PrepaymentDiscountPercent,
            offer,
            client.NextPaymentDate?.Date ?? fallbackPeriodStart.Date);
    }

    private static int CountOfferMonths(OfferTerms offer, DateTime periodStart, int months)
    {
        var count = 0;
        while (count < months && periodStart.Date.AddMonths(count) < offer.EndDate) count++;
        return count;
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
