namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Espera entre consultas de estado de la reconsulta manual: 5 s, luego 30 s y luego 2 min. Son 10
/// intentos (unos 10 minutos), más que el seguimiento original, para cubrir una DGII lenta.
/// </summary>
public static class EcfRetrackSchedule
{
    public const int MaxAttempts = 10;

    /// <summary>Espera antes del siguiente intento, dados los ya hechos (el primero es 1). Null al agotarse.</summary>
    public static TimeSpan? NextDelay(int attemptsMade) => attemptsMade switch
    {
        >= 1 and <= 3 => TimeSpan.FromSeconds(5),
        >= 4 and <= 6 => TimeSpan.FromSeconds(30),
        >= 7 and < MaxAttempts => TimeSpan.FromMinutes(2),
        _ => null
    };
}
