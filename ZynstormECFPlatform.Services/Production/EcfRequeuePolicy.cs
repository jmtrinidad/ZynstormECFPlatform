namespace ZynstormECFPlatform.Services.Production;

public enum EcfRequeueAction
{
    /// <summary>Volver a enviar a la DGII (primero se le pregunta si ya lo tiene).</summary>
    Transmit,

    /// <summary>Volver a consultar el estado en la DGII: ya la tiene, no se retransmite.</summary>
    Track
}

/// <summary>Lo que decide si un comprobante se puede re-encolar.</summary>
public sealed record EcfRequeueSnapshot(int EcfStatusId, bool HasSignedXml, bool HasTrackId, DateTime LastActivityUtc);

public static class EcfRequeuePolicy
{
    /// <summary>Si cambió hace menos de esto, puede haber un job en curso (o un doble clic).</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    /// <summary>El seguimiento original dura alrededor de 1 o 2 minutos; pasado esto ya terminó.</summary>
    public static readonly TimeSpan TrackIdle = TimeSpan.FromMinutes(2);

    public static EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot, DateTime nowUtc, TimeSpan staleInFlight)
    {
        if (!snapshot.HasSignedXml)
            return null;

        var idle = nowUtc - snapshot.LastActivityUtc;
        if (idle < Cooldown)
            return null;

        if (snapshot.HasTrackId)
        {
            return snapshot.EcfStatusId is 9 or 12 && idle >= TrackIdle
                ? EcfRequeueAction.Track
                : null;
        }

        return snapshot.EcfStatusId switch
        {
            12 => EcfRequeueAction.Transmit,
            7 or 8 or 9 when idle >= staleInFlight => EcfRequeueAction.Transmit,
            _ => null
        };
    }

    /// <summary>
    /// Número de intento del reenvío manual: al menos 2, para que el job siempre le pregunte
    /// primero a la DGII si ya recibió el comprobante (la comprobación aplica desde el intento 2).
    /// </summary>
    public static int NextAttemptNumber(int transmissions) => Math.Max(2, transmissions + 1);
}
