namespace ZynstormECFPlatform.Services.Billing;

public sealed record OverageTier(int FromUnit, int? ToUnit, decimal UnitPrice);

public sealed record OverageTierCharge(int FromUnit, int? ToUnit, decimal UnitPrice, int Units, decimal Amount);

public sealed record BillingCalculationResult(
    decimal MonthlyFee,
    int MonthlyDocumentLimit,
    int AcceptedDocuments,
    int OverageDocuments,
    IReadOnlyList<OverageTierCharge> Tiers,
    decimal OverageAmount,
    decimal Total);

public static class BillingCalculator
{
    public const int UnlimitedDocuments = -1;

    /// <summary>
    /// Calcula mensualidad + excedente escalonado. Cada tramo cubre las unidades de excedente
    /// dentro de [FromUnit, ToUnit] (ToUnit null = sin tope).
    /// </summary>
    public static BillingCalculationResult Calculate(
        decimal monthlyFee,
        int monthlyDocumentLimit,
        IEnumerable<OverageTier> tiers,
        int acceptedDocuments)
    {
        var overage = monthlyDocumentLimit == UnlimitedDocuments
            ? 0
            : Math.Max(0, acceptedDocuments - monthlyDocumentLimit);

        var charges = new List<OverageTierCharge>();
        foreach (var tier in tiers.OrderBy(t => t.FromUnit))
        {
            var upper = tier.ToUnit ?? int.MaxValue;
            var units = overage < tier.FromUnit ? 0 : Math.Min(overage, upper) - tier.FromUnit + 1;
            charges.Add(new OverageTierCharge(tier.FromUnit, tier.ToUnit, tier.UnitPrice, units, units * tier.UnitPrice));
        }

        var overageAmount = charges.Sum(c => c.Amount);

        return new BillingCalculationResult(
            monthlyFee,
            monthlyDocumentLimit,
            acceptedDocuments,
            overage,
            charges,
            overageAmount,
            monthlyFee + overageAmount);
    }

    public static List<string> ValidatePlan(int monthlyDocumentLimit, decimal monthlyFee, IEnumerable<OverageTier> tiers)
    {
        var errors = new List<string>();

        if (monthlyDocumentLimit != UnlimitedDocuments && monthlyDocumentLimit <= 0)
            errors.Add("El límite mensual debe ser mayor que 0, o -1 para ilimitado.");

        if (monthlyFee < 0)
            errors.Add("La mensualidad no puede ser negativa.");

        var ordered = tiers.OrderBy(t => t.FromUnit).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            var tier = ordered[i];
            var position = i + 1;

            if (i == 0 && tier.FromUnit != 1)
                errors.Add("El primer tramo debe iniciar en 1.");

            if (i > 0 && ordered[i - 1].ToUnit is int previousTo && tier.FromUnit != previousTo + 1)
                errors.Add($"El tramo {position} debe iniciar en {previousTo + 1}.");

            if (tier.ToUnit is null && i < ordered.Count - 1)
                errors.Add("Solo el último tramo puede no tener tope.");

            if (tier.ToUnit is int to && to < tier.FromUnit)
                errors.Add($"En el tramo {position} el 'hasta' no puede ser menor que el 'desde'.");

            if (tier.UnitPrice < 0)
                errors.Add($"El precio del tramo {position} no puede ser negativo.");
        }

        return errors;
    }
}
