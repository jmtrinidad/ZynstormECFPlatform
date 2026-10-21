namespace ZynstormECFPlatform.Services.Billing;

public enum OfferStatus
{
    None = 0,
    Active = 1,
    Ended = 2
}

/// <summary>Condiciones de la oferta de un cliente: precio promocional, duración y fecha de inicio.</summary>
public sealed record OfferTerms(decimal OfferPrice, int OfferMonths, DateTime StartDate)
{
    /// <summary>Primer día en que se cobra el precio real.</summary>
    public DateTime EndDate => StartDate.Date.AddMonths(OfferMonths);
}

public sealed record OfferState(OfferStatus Status, DateTime? EndDate, int? DaysRemaining, int? MonthsRemaining);

/// <summary>Reglas de las ofertas de plan. Funciones puras: no tocan base de datos ni configuración.</summary>
public static class OfferPricing
{
    /// <summary>Días de anticipación para considerar una oferta próxima a vencer.</summary>
    public const int DefaultWarningDays = 30;

    public const int MaxOfferMonths = 120;

    public static bool PlanHasOffer(decimal? offerPrice, int? offerMonths) =>
        offerPrice.HasValue && offerMonths is > 0;

    /// <summary>Null si el plan no tiene oferta o el cliente no tiene fecha de inicio de oferta.</summary>
    public static OfferTerms? TermsFor(decimal? offerPrice, int? offerMonths, DateTime? startDate) =>
        PlanHasOffer(offerPrice, offerMonths) && startDate is DateTime start
            ? new OfferTerms(offerPrice!.Value, offerMonths!.Value, start.Date)
            : null;

    /// <summary>
    /// Estado de la oferta al día <paramref name="today"/>. Está activa hasta el día anterior a
    /// <see cref="OfferTerms.EndDate"/>; desde esa fecha se cobra el precio real.
    /// </summary>
    public static OfferState Evaluate(OfferTerms? terms, DateTime today)
    {
        if (terms is null)
            return new OfferState(OfferStatus.None, null, null, null);

        var end = terms.EndDate;
        var reference = today.Date;

        if (reference >= end)
            return new OfferState(OfferStatus.Ended, end, null, null);

        var months = 0;
        while (reference.AddMonths(months + 1) <= end) months++;

        return new OfferState(OfferStatus.Active, end, (end - reference).Days, months);
    }

    public static bool IsExpiringSoon(OfferTerms? terms, DateTime today, int warningDays = DefaultWarningDays)
    {
        var state = Evaluate(terms, today);
        return state.Status == OfferStatus.Active && state.DaysRemaining <= warningDays;
    }

    /// <summary>
    /// Fecha de inicio de la oferta tras guardar un cliente. La oferta arranca (o se reinicia) al asignar
    /// un plan distinto; con el mismo plan se conserva la fecha actual, así un plan que recibe oferta
    /// después no afecta retroactivamente a quienes ya lo tenían.
    /// </summary>
    public static DateTime? ResolveStartDate(
        int? previousPlanId,
        int? newPlanId,
        bool newPlanHasOffer,
        DateTime? currentStart,
        DateTime today)
    {
        if (!newPlanHasOffer) return null;

        return previousPlanId != newPlanId ? today.Date : currentStart;
    }

    public static List<string> ValidateOffer(decimal monthlyFee, decimal? offerPrice, int? offerMonths)
    {
        var errors = new List<string>();

        if (offerPrice is null && offerMonths is null)
            return errors;

        if (offerPrice is null || offerMonths is null)
        {
            errors.Add("La oferta requiere precio y meses de oferta.");
            return errors;
        }

        if (offerMonths < 1 || offerMonths > MaxOfferMonths)
            errors.Add($"Los meses de oferta deben estar entre 1 y {MaxOfferMonths}.");

        if (offerPrice < 0)
            errors.Add("El precio de oferta no puede ser negativo.");
        else if (offerPrice >= monthlyFee)
            errors.Add("El precio de oferta debe ser menor que la mensualidad.");

        return errors;
    }

    /// <summary>
    /// Precio mensual real que se cobró en un pago que mezcló meses de oferta y meses a precio real.
    /// Null si todos los meses fueron de oferta.
    /// </summary>
    public static decimal? GetFullPriceUnit(decimal grossAmount, int monthsCovered, int offerMonthsCovered, decimal offerUnitPrice)
    {
        var fullMonths = monthsCovered - offerMonthsCovered;
        if (fullMonths <= 0) return null;

        return Math.Round((grossAmount - offerMonthsCovered * offerUnitPrice) / fullMonths, 2, MidpointRounding.AwayFromZero);
    }
}
