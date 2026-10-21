namespace ZynstormECFPlatform.Services.Billing;

/// <summary>Reglas del precio especial por cliente. Funciones puras.</summary>
public static class ClientPricing
{
    /// <summary>Solo el rol SA fija o quita el precio propio; el resto conserva el valor actual.</summary>
    public static decimal? ResolveCustomFee(bool isSA, decimal? requested, decimal? current) =>
        isSA ? requested : current;
}
