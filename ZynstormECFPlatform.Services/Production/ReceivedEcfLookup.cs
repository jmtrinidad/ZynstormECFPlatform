using System.Globalization;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Production;

public sealed record ReceivedEcfListItem(string ENcf, string RncEmisor, int EcfType, decimal? Total, DateTime ReceivedAtUtc);

public sealed record ReceivedEcfListResponse(int Count, bool HasMore, IReadOnlyList<ReceivedEcfListItem> Items);

public enum ReceivedEcfPickKind
{
    Found,
    NotFound,
    Ambiguous
}

public sealed record ReceivedEcfPick(ReceivedEcfPickKind Kind, ReceivedB2BMessage? Message, IReadOnlyList<string> Emisores);

/// <summary>Reglas puras de la consulta de e-CF recibidos. Sin base de datos, para poder probarlas.</summary>
public static class ReceivedEcfLogic
{
    public const int DefaultLimit = 100;
    public const int MaxLimit = 200;

    /// <summary>Solo los dígitos del RNC; vacío si no hay.</summary>
    public static string NormalizeRnc(string? raw) =>
        string.IsNullOrWhiteSpace(raw) ? string.Empty : new string(raw.Where(char.IsAsciiDigit).ToArray());

    public static int ClampLimit(int? raw) =>
        raw is null or <= 0 ? DefaultLimit : Math.Min(raw.Value, MaxLimit);

    /// <summary>
    /// Elige el recibido para un eNCF. El eNCF solo es único por emisor, así que con candidatos de
    /// más de un RNC sin <paramref name="rncEmisor"/> el resultado es ambiguo. Dentro de un mismo
    /// emisor (reenvíos) gana el más reciente.
    /// </summary>
    public static ReceivedEcfPick Pick(IEnumerable<ReceivedB2BMessage> candidates, string? rncEmisor)
    {
        var list = candidates.ToList();
        var wanted = NormalizeRnc(rncEmisor);

        if (wanted.Length > 0)
        {
            list = list.Where(m => NormalizeRnc(m.RncEmisor) == wanted).ToList();
        }

        if (list.Count == 0)
        {
            return new ReceivedEcfPick(ReceivedEcfPickKind.NotFound, null, []);
        }

        var emisores = list
            .Select(m => NormalizeRnc(m.RncEmisor))
            .Distinct()
            .OrderBy(rnc => rnc, StringComparer.Ordinal)
            .ToList();

        if (emisores.Count > 1)
        {
            return new ReceivedEcfPick(ReceivedEcfPickKind.Ambiguous, null, emisores);
        }

        var latest = list.OrderByDescending(m => m.ReceivedAtUtc).First();
        return new ReceivedEcfPick(ReceivedEcfPickKind.Found, latest, emisores);
    }

    /// <summary>MontoTotal del XML, o null si el XML no se puede leer o no lo trae.</summary>
    public static decimal? ReadTotal(string rawXml)
    {
        try
        {
            var total = XDocument.Parse(rawXml).Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "MontoTotal")?.Value.Trim();

            return decimal.TryParse(total, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : null;
        }
        catch
        {
            return null;
        }
    }

    public static ReceivedEcfListItem ToListItem(ReceivedB2BMessage message) =>
        new(
            message.ENcf,
            NormalizeRnc(message.RncEmisor),
            NcfHelper.TryExtractEcfType(message.ENcf, out var type) ? type : 0,
            ReadTotal(message.RawXml),
            DateTime.SpecifyKind(message.ReceivedAtUtc, DateTimeKind.Utc));

    /// <summary>Ambiente del QR del PDF: el configurado, o Production.</summary>
    public static DgiiEnvironment ResolveEnvironment(string? configured) =>
        !string.IsNullOrWhiteSpace(configured) && Enum.TryParse<DgiiEnvironment>(configured, true, out var parsed)
            ? parsed
            : DgiiEnvironment.Production;

    /// <summary>
    /// Lee el parámetro <c>since</c>. Vacío es válido (sin filtro). Un valor sin zona horaria se
    /// toma como UTC.
    /// </summary>
    public static bool TryParseSince(string? raw, out DateTime? sinceUtc)
    {
        sinceUtc = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return true;
        }

        if (!DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return false;
        }

        sinceUtc = DateTime.SpecifyKind(parsed.UtcDateTime, DateTimeKind.Utc);
        return true;
    }
}

public interface IReceivedEcfService
{
    /// <summary>e-CF recibidos de un cliente, del más antiguo al más nuevo, posteriores a <paramref name="sinceUtc"/>.</summary>
    Task<ReceivedEcfListResponse> ListAsync(int clientId, DateTime? sinceUtc, int limit, CancellationToken cancellationToken = default);

    /// <summary>Recibido por eNCF ya normalizado, acotado a un cliente y opcionalmente a un emisor.</summary>
    Task<ReceivedEcfPick> FindAsync(int clientId, string normalizedEncf, string? rncEmisor, CancellationToken cancellationToken = default);
}

public class ReceivedEcfService(IReceivedB2BMessageService messages) : IReceivedEcfService
{
    public async Task<ReceivedEcfListResponse> ListAsync(
        int clientId,
        DateTime? sinceUtc,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = messages.Table
            .AsNoTracking()
            .Where(m => m.ClientId == clientId && m.MessageType == MessageType.Ecf);

        if (sinceUtc is not null)
        {
            query = query.Where(m => m.ReceivedAtUtc > sinceUtc.Value);
        }

        // Se pide uno de más para saber si hay más sin hacer un COUNT aparte.
        var page = await query
            .OrderBy(m => m.ReceivedAtUtc)
            .ThenBy(m => m.ReceivedB2BMessageId)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var items = page.Take(limit).Select(ReceivedEcfLogic.ToListItem).ToList();

        return new ReceivedEcfListResponse(items.Count, page.Count > limit, items);
    }

    public async Task<ReceivedEcfPick> FindAsync(
        int clientId,
        string normalizedEncf,
        string? rncEmisor,
        CancellationToken cancellationToken = default)
    {
        var candidates = await messages.Table
            .AsNoTracking()
            .Where(m => m.ClientId == clientId && m.MessageType == MessageType.Ecf && m.ENcf == normalizedEncf)
            .ToListAsync(cancellationToken);

        return ReceivedEcfLogic.Pick(candidates, rncEmisor);
    }
}
