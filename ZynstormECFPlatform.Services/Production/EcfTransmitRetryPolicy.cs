using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Reglas del reintento de la transmisión diferida. Solo se reintentan los fallos de
/// transporte (timeout, red caída): un rechazo de la DGII no se reintenta, porque reenviar el
/// mismo XML volvería a ser rechazado.
/// </summary>
public static class EcfTransmitRetryPolicy
{
    public static readonly int[] DefaultDelaysSeconds = [5, 30, 120];

    private const int MaxDelaySeconds = 3600;

    /// <summary>
    /// Esperas entre intentos a partir de la configuración. Sin configuración (o vacía) se usan
    /// las de por defecto; los valores no positivos se descartan y el máximo es una hora.
    /// Una configuración con solo ceros significa "sin reintentos".
    /// </summary>
    public static IReadOnlyList<TimeSpan> Normalize(int[]? configuredSeconds)
    {
        var source = configuredSeconds is { Length: > 0 } ? configuredSeconds : DefaultDelaysSeconds;

        return source
            .Where(seconds => seconds > 0)
            .Select(seconds => TimeSpan.FromSeconds(Math.Min(seconds, MaxDelaySeconds)))
            .ToList();
    }

    /// <summary>
    /// Espera antes del próximo intento, dado cuántos intentos ya se hicieron (el primero es 1).
    /// Null cuando ya no quedan reintentos.
    /// </summary>
    public static TimeSpan? NextDelay(int attemptsMade, IReadOnlyList<TimeSpan> delays) =>
        attemptsMade >= 1 && attemptsMade <= delays.Count ? delays[attemptsMade - 1] : null;

    /// <summary>
    /// La DGII dijo "secuencia ya utilizada" (1209 o 75) en un reintento de un documento cuyos
    /// intentos anteriores nunca recibieron TrackId. Lo más probable es que el primer envío sí
    /// llegara y la respuesta se perdiera: no es un número quemado por otro comprobante. Sin
    /// intentos previos sin confirmar, el mismo mensaje sí es una secuencia quemada.
    /// </summary>
    public static bool IsOwnDuplicate(DgiiStatusResponse status, bool earlierAttemptsWithoutTrackId)
    {
        if (!earlierAttemptsWithoutTrackId)
            return false;

        if (status.Mensajes.Any(message => IsSequenceUsedCode(message.Codigo) || SaysAlreadyUsed(message.Valor)))
            return true;

        return SaysAlreadyUsed(status.Error) || SaysAlreadyUsed(status.Mensaje);
    }

    private static bool IsSequenceUsedCode(object? code) =>
        Convert.ToString(code, System.Globalization.CultureInfo.InvariantCulture) is "1209" or "75";

    private static bool SaysAlreadyUsed(string? text) =>
        text?.Contains("ya ha sido utilizado", StringComparison.OrdinalIgnoreCase) ?? false;
}
