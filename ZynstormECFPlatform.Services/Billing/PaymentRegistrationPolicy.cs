using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas para registrar un pago de plan o de excedente. Funciones puras.</summary>
public static class PaymentRegistrationPolicy
{
    public const int ReferenceMaxLength = 100;
    public const int NotesMaxLength = 500;

    /// <summary>
    /// El ciclo avanza desde la fecha que vencía (no desde el día en que se pagó).
    /// Sin fecha previa, se cuenta desde la fecha del pago.
    /// </summary>
    public static DateTime CalculateNewNextPaymentDate(DateTime? currentNextPaymentDate, DateTime paymentDate, int paidMonths) =>
        (currentNextPaymentDate ?? paymentDate).Date.AddMonths(Math.Max(1, paidMonths));

    /// <summary>Solo se cobra el excedente de meses cerrados (anteriores al mes de <paramref name="today"/>).</summary>
    public static bool IsOverageMonthPayable(int year, int month, DateTime today) =>
        new DateTime(year, month, 1) < new DateTime(today.Year, today.Month, 1);

    public static bool ShouldReactivate(bool paymentSuspended, bool includesPlanFee, DateTime? newNextPaymentDate, DateTime today) =>
        paymentSuspended
        && includesPlanFee
        && newNextPaymentDate is DateTime next
        && next.Date > today.Date;

    public static List<string> ValidateRequest(
        DateTime? paymentDate,
        int paymentMethod,
        bool includePlanFee,
        int overageCount,
        string? reference,
        string? notes,
        DateTime today)
    {
        var errors = new List<string>();

        if (paymentDate is not DateTime date)
            errors.Add("La fecha del pago es requerida.");
        else if (date.Date > today.Date)
            errors.Add("La fecha del pago no puede ser futura.");

        if (!Enum.IsDefined(typeof(PaymentMethod), paymentMethod))
            errors.Add("El método de pago no es válido.");

        if (!includePlanFee && overageCount <= 0)
            errors.Add("Debe seleccionar la mensualidad o al menos un excedente.");

        if (reference?.Trim().Length > ReferenceMaxLength)
            errors.Add($"La referencia no puede exceder {ReferenceMaxLength} caracteres.");

        if (notes?.Trim().Length > NotesMaxLength)
            errors.Add($"La nota no puede exceder {NotesMaxLength} caracteres.");

        return errors;
    }

    public static string FormatReceiptNumber(int clientPaymentId) => $"REC-{clientPaymentId:D6}";
}
