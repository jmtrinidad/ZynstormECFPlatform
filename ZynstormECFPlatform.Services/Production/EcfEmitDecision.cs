using System.Security.Cryptography;
using System.Text;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfEmitAction
{
    /// <summary>No hay documento con ese eNCF: se crea uno y se emite.</summary>
    Create,

    /// <summary>Ya existe un documento aceptado o en proceso: no se transmite, se devuelve lo guardado.</summary>
    Replay,

    /// <summary>Los documentos existentes fallaron o quedaron colgados: se crea uno nuevo y se emite.</summary>
    Retry
}

/// <summary>Un documento ya existente con ese (cliente, eNCF), reducido a lo que decide.</summary>
public sealed record EcfEmitExisting(
    int EcfDocumentId,
    int EcfStatusId,
    EcfLookupState State,
    DateTime LastActivityUtc,
    bool HasTrackId);

public sealed record EcfEmitDecisionResult(
    EcfEmitAction Action,
    int? ReplayDocumentId,
    EcfLookupState? ReplayState);

public static class EcfEmitDecision
{
    public static EcfEmitDecisionResult Decide(
        IReadOnlyCollection<EcfEmitExisting> existing,
        DateTime nowUtc,
        TimeSpan staleAfter)
    {
        if (existing.Count == 0)
            return new EcfEmitDecisionResult(EcfEmitAction.Create, null, null);

        var accepted = existing
            .Where(document => document.State is EcfLookupState.Accepted or EcfLookupState.AcceptedConditional)
            .OrderBy(document => document.State == EcfLookupState.Accepted ? 0 : 1)
            .ThenByDescending(document => document.LastActivityUtc)
            .ThenByDescending(document => document.EcfDocumentId)
            .FirstOrDefault();

        if (accepted is not null)
            return new EcfEmitDecisionResult(EcfEmitAction.Replay, accepted.EcfDocumentId, accepted.State);

        var live = existing
            .Where(document => IsLive(document, nowUtc, staleAfter))
            .OrderByDescending(document => document.LastActivityUtc)
            .ThenByDescending(document => document.EcfDocumentId)
            .FirstOrDefault();

        if (live is not null)
            return new EcfEmitDecisionResult(EcfEmitAction.Replay, live.EcfDocumentId, EcfLookupState.Pending);

        return new EcfEmitDecisionResult(EcfEmitAction.Retry, null, null);
    }

    /// <summary>
    /// Clave de 64 bits para pg_advisory_xact_lock, estable para el mismo (cliente, eNCF) y
    /// distinta entre clientes y entre eNCF.
    /// </summary>
    public static long LockKey(int clientId, string eNcf)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"ecf-emit|{clientId}|{eNcf}"));
        return BitConverter.ToInt64(hash, 0);
    }

    /// <summary>
    /// En vuelo: creado, validando, listo para firmar, firmando, firmado, pendiente de envío,
    /// enviando o enviado (ids 1, 2, 4, 5, 6, 7, 8, 9). Sigue vivo mientras su última actividad
    /// sea reciente o la DGII ya lo tenga (TrackId): ese lo resuelve el job de seguimiento.
    /// </summary>
    private static bool IsLive(EcfEmitExisting document, DateTime nowUtc, TimeSpan staleAfter)
    {
        var inFlight = document.EcfStatusId is 1 or 2 or 4 or 5 or 6 or 7 or 8 or 9;

        if (!inFlight)
            return false;

        return document.HasTrackId || nowUtc - document.LastActivityUtc < staleAfter;
    }
}
