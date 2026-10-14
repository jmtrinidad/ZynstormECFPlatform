using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>Un documento emitido reducido a lo que hace falta para elegir cuál se entrega.</summary>
public sealed record SignedXmlCandidate(int EcfDocumentId, string ENcf, EcfLookupState State, DateTime RegisteredAt);

public sealed record SignedXmlItem(string ENcf, string Xml);

/// <summary>El archivo a entregar: un XML o un ZIP de XML.</summary>
public sealed record SignedXmlFile(byte[] Content, string FileName, string ContentType, int Count);

public enum SignedXmlExportKind
{
    Ok,
    NotFound,
    TooMany
}

public sealed record SignedXmlExportResult(SignedXmlExportKind Kind, SignedXmlFile? File, int Total, string Message)
{
    public static SignedXmlExportResult Success(SignedXmlFile file) =>
        new(SignedXmlExportKind.Ok, file, file.Count, string.Empty);

    public static SignedXmlExportResult Missing(string message) =>
        new(SignedXmlExportKind.NotFound, null, 0, message);

    public static SignedXmlExportResult Excess(int total, string message) =>
        new(SignedXmlExportKind.TooMany, null, total, message);
}

/// <summary>Reglas puras del export de XML firmados. Sin base de datos, para poder probarlas.</summary>
public static class SignedXmlExportLogic
{
    public const int MaxItems = 2000;

    // Sin BOM: el XML firmado se entrega tal cual, y un BOM no forma parte de lo firmado.
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static bool TryParseDate(string? raw, out DateTime date) =>
        DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    /// <summary>
    /// Días de RD (ambos inclusivos) a un intervalo UTC [desde, hasta). Es la misma conversión que
    /// usa la lista de facturas del panel, para que lo que se ve sea lo que se descarga.
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtcExclusive) ToUtcRange(DateTime fromDate, DateTime toDate) =>
        (fromDate.Date.ConvertDominicanLocalToUtc(), toDate.Date.AddDays(1).ConvertDominicanLocalToUtc());

    /// <summary>Solo documentos válidos, uno por eNCF (el mejor), ordenados por eNCF.</summary>
    public static IReadOnlyList<SignedXmlCandidate> SelectBest(IEnumerable<SignedXmlCandidate> candidates) =>
        candidates
            .Where(candidate => EcfLookupLogic.IsUsable(candidate.State))
            .GroupBy(candidate => candidate.ENcf)
            .Select(group =>
            {
                var best = EcfLookupLogic.PickBest(
                    group.Select(candidate => new EcfLookupCandidate(candidate.EcfDocumentId, candidate.State, candidate.RegisteredAt)))!;

                return group.First(candidate => candidate.EcfDocumentId == best.EcfDocumentId);
            })
            .OrderBy(candidate => candidate.ENcf, StringComparer.Ordinal)
            .ToList();

    public static SignedXmlFile Single(SignedXmlItem item) =>
        new(Utf8NoBom.GetBytes(item.Xml), $"{item.ENcf}.xml", "application/xml", 1);

    /// <summary>Un solo comprobante es el XML; más de uno, un ZIP con una entrada por eNCF.</summary>
    public static SignedXmlFile BuildFile(IReadOnlyList<SignedXmlItem> items, string issuerRnc, DateTime fromDate, DateTime toDate)
    {
        if (items.Count == 1)
        {
            return Single(items[0]);
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var item in items)
            {
                var entry = archive.CreateEntry($"{item.ENcf}.xml", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                var bytes = Utf8NoBom.GetBytes(item.Xml);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        var name = $"XML_firmados_{Digits(issuerRnc)}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.zip";
        return new SignedXmlFile(stream.ToArray(), name, "application/zip", items.Count);
    }

    public static string Digits(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : new string(value.Where(char.IsAsciiDigit).ToArray());
}

public interface ISignedXmlExportService
{
    /// <summary>XML firmado del mejor documento válido del cliente con ese eNCF (ya normalizado).</summary>
    Task<SignedXmlExportResult> GetByNcfAsync(int clientId, string normalizedEncf, CancellationToken cancellationToken = default);

    /// <summary>XML firmado de ese documento exacto, en cualquier estado.</summary>
    Task<SignedXmlExportResult> GetForDocumentAsync(int ecfDocumentId, CancellationToken cancellationToken = default);

    /// <summary>XML firmados de los documentos válidos del cliente registrados en el rango de días de RD.</summary>
    Task<SignedXmlExportResult> GetRangeAsync(int clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

public class SignedXmlExportService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients) : ISignedXmlExportService
{
    /// <summary>
    /// EcfStatusId que la plataforma comparte entre Rechazado y Aceptado condicional: para
    /// distinguirlos hay que leer la última transmisión (ver EcfLookupLogic.ResolveState).
    /// </summary>
    private const int RejectedOrConditionalStatusId = 11;

    private sealed record Row(int Id, string ENcf, int EcfStatusId, DateTime RegisteredAt);

    public async Task<SignedXmlExportResult> GetByNcfAsync(
        int clientId,
        string normalizedEncf,
        CancellationToken cancellationToken = default)
    {
        var rows = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, normalizedEncf))
            .Select(document => new Row(document.EcfDocumentId, document.Ncf, document.EcfStatusId, document.RegisteredAt))
            .ToListAsync(cancellationToken);

        var selected = SignedXmlExportLogic.SelectBest(await ToCandidatesAsync(rows, cancellationToken));
        var items = await LoadItemsAsync(selected, cancellationToken);

        return items.Count == 0
            ? SignedXmlExportResult.Missing("No hay un comprobante emitido y válido con ese eNCF que tenga XML firmado.")
            : SignedXmlExportResult.Success(SignedXmlExportLogic.Single(items[0]));
    }

    public async Task<SignedXmlExportResult> GetForDocumentAsync(
        int ecfDocumentId,
        CancellationToken cancellationToken = default)
    {
        var encf = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == ecfDocumentId)
            .Select(document => document.Ncf)
            .FirstOrDefaultAsync(cancellationToken);

        var xml = encf is null
            ? null
            : (await LoadSignedXmlAsync([ecfDocumentId], cancellationToken)).GetValueOrDefault(ecfDocumentId);

        return encf is null || xml is null
            ? SignedXmlExportResult.Missing("El comprobante no tiene XML firmado.")
            : SignedXmlExportResult.Success(SignedXmlExportLogic.Single(new SignedXmlItem(encf, xml)));
    }

    public async Task<SignedXmlExportResult> GetRangeAsync(
        int clientId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtcExclusive) = SignedXmlExportLogic.ToUtcRange(fromDate, toDate);

        var rows = await documents.Table
            .AsNoTracking()
            .Where(document => document.ClientId == clientId
                               && !document.IsDeleted
                               && document.RegisteredAt >= fromUtc
                               && document.RegisteredAt < toUtcExclusive)
            .Select(document => new Row(document.EcfDocumentId, document.Ncf, document.EcfStatusId, document.RegisteredAt))
            .ToListAsync(cancellationToken);

        var selected = SignedXmlExportLogic.SelectBest(await ToCandidatesAsync(rows, cancellationToken));

        if (selected.Count > SignedXmlExportLogic.MaxItems)
        {
            return SignedXmlExportResult.Excess(
                selected.Count,
                $"El rango tiene {selected.Count} comprobantes y el máximo por descarga es {SignedXmlExportLogic.MaxItems}. Reduzca el rango de fechas.");
        }

        var items = await LoadItemsAsync(selected, cancellationToken);

        if (items.Count == 0)
        {
            return SignedXmlExportResult.Missing("No hay comprobantes emitidos y válidos con XML firmado en ese rango de fechas.");
        }

        var rnc = await clients.Table
            .AsNoTracking()
            .Where(client => client.ClientId == clientId)
            .Select(client => client.Rnc)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        return SignedXmlExportResult.Success(SignedXmlExportLogic.BuildFile(items, rnc, fromDate, toDate));
    }

    /// <summary>Estado de cada documento; la última transmisión solo se lee donde el estado la necesita.</summary>
    private async Task<List<SignedXmlCandidate>> ToCandidatesAsync(List<Row> rows, CancellationToken cancellationToken)
    {
        var ambiguousIds = rows
            .Where(row => row.EcfStatusId == RejectedOrConditionalStatusId)
            .Select(row => row.Id)
            .ToList();

        var payloads = new Dictionary<int, string?>();

        if (ambiguousIds.Count > 0)
        {
            var sent = await transmissions.Table
                .AsNoTracking()
                .Where(transmission => ambiguousIds.Contains(transmission.EcfDocumentId))
                .Select(transmission => new { transmission.EcfDocumentId, transmission.ResponsePayload, transmission.SentAtUtc })
                .ToListAsync(cancellationToken);

            payloads = sent
                .GroupBy(transmission => transmission.EcfDocumentId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(transmission => transmission.SentAtUtc).First().ResponsePayload);
        }

        return rows
            .Select(row => new SignedXmlCandidate(
                row.Id,
                row.ENcf,
                EcfLookupLogic.ResolveState(row.EcfStatusId, payloads.GetValueOrDefault(row.Id)),
                row.RegisteredAt))
            .ToList();
    }

    /// <summary>XML firmado de cada documento elegido; los que no lo tienen se omiten.</summary>
    private async Task<List<SignedXmlItem>> LoadItemsAsync(
        IReadOnlyList<SignedXmlCandidate> selected,
        CancellationToken cancellationToken)
    {
        if (selected.Count == 0)
        {
            return [];
        }

        var signed = await LoadSignedXmlAsync(selected.Select(candidate => candidate.EcfDocumentId).ToList(), cancellationToken);

        return selected
            .Where(candidate => signed.ContainsKey(candidate.EcfDocumentId))
            .Select(candidate => new SignedXmlItem(candidate.ENcf, signed[candidate.EcfDocumentId]))
            .ToList();
    }

    /// <summary>El XmlSigned con texto de mayor id, por documento.</summary>
    private async Task<Dictionary<int, string>> LoadSignedXmlAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await xmlDocuments.Table
            .AsNoTracking()
            .Where(xml => documentIds.Contains(xml.EcfDocumentId) && xml.XmlSigned != null && xml.XmlSigned != "")
            .Select(xml => new { xml.EcfDocumentId, xml.EcfXmlDocumentId, xml.XmlSigned })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.EcfDocumentId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(row => row.EcfXmlDocumentId).First().XmlSigned);
    }
}
