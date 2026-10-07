using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Abstractions.DataServices;

namespace ZynstormECFPlatform.Services.Production;

public interface IEcfLookupService
{
    /// <summary>
    /// Estado de un e-CF ya recibido, por eNCF y dentro de un solo cliente. Null si la
    /// plataforma nunca recibió ese eNCF. Solo lee: no modifica nada ni llama a la DGII.
    /// </summary>
    Task<EcfLookupResponse?> FindByNcfAsync(int clientId, string eNcf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Los documentos de ese cliente con ese eNCF, reducidos a lo que necesita
    /// <see cref="EcfEmitDecision"/>. Lista vacía si no hay ninguno.
    /// </summary>
    Task<IReadOnlyList<EcfEmitExisting>> ListExistingAsync(int clientId, string eNcf, CancellationToken cancellationToken = default);

    /// <summary>
    /// La respuesta (estado, TrackId, código de seguridad, fecha de firma y QR) de un
    /// documento concreto, con el estado que decidió quien llama.
    /// </summary>
    Task<EcfLookupResponse> DescribeDocumentAsync(
        int clientId,
        int ecfDocumentId,
        EcfLookupState state,
        int attempts,
        CancellationToken cancellationToken = default);
}

public class EcfLookupService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients,
    IConfiguration configuration) : IEcfLookupService
{
    private sealed record TransmissionRow(int EcfDocumentId, string TrackId, string? ResponsePayload, DateTime SentAtUtc);

    public async Task<EcfLookupResponse?> FindByNcfAsync(
        int clientId,
        string eNcf,
        CancellationToken cancellationToken = default)
    {
        var found = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, eNcf))
            .Select(document => new { document.EcfDocumentId, document.EcfStatusId, document.RegisteredAt })
            .ToListAsync(cancellationToken);

        if (found.Count == 0)
            return null;

        var byDocument = await LoadTransmissionsAsync(found.Select(document => document.EcfDocumentId).ToList(), cancellationToken);

        var candidates = found
            .Select(document => new EcfLookupCandidate(
                document.EcfDocumentId,
                EcfLookupLogic.ResolveState(document.EcfStatusId, LastPayload(byDocument, document.EcfDocumentId)),
                document.RegisteredAt))
            .ToList();

        var best = EcfLookupLogic.PickBest(candidates)!;

        return await DescribeDocumentAsync(clientId, best.EcfDocumentId, best.State, found.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<EcfEmitExisting>> ListExistingAsync(
        int clientId,
        string eNcf,
        CancellationToken cancellationToken = default)
    {
        var found = await documents.Table
            .AsNoTracking()
            .Where(EcfLookupLogic.ForClient(clientId, eNcf))
            .Select(document => new
            {
                document.EcfDocumentId,
                document.EcfStatusId,
                document.RegisteredAt,
                document.LastUpdateUtc
            })
            .ToListAsync(cancellationToken);

        if (found.Count == 0)
            return [];

        var byDocument = await LoadTransmissionsAsync(found.Select(document => document.EcfDocumentId).ToList(), cancellationToken);

        return found
            .Select(document => new EcfEmitExisting(
                document.EcfDocumentId,
                document.EcfStatusId,
                EcfLookupLogic.ResolveState(document.EcfStatusId, LastPayload(byDocument, document.EcfDocumentId)),
                document.LastUpdateUtc ?? document.RegisteredAt,
                byDocument.TryGetValue(document.EcfDocumentId, out var list)
                    && list.Any(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))))
            .ToList();
    }

    public async Task<EcfLookupResponse> DescribeDocumentAsync(
        int clientId,
        int ecfDocumentId,
        EcfLookupState state,
        int attempts,
        CancellationToken cancellationToken = default)
    {
        var detail = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == ecfDocumentId && document.ClientId == clientId)
            .Select(document => new
            {
                document.Ncf,
                document.Total,
                document.IssueDateUtc,
                document.SignatureDateTime,
                document.CustomerRnc
            })
            .FirstAsync(cancellationToken);

        var issuerRnc = await clients.Table
            .AsNoTracking()
            .Where(client => client.ClientId == clientId)
            .Select(client => client.Rnc)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var signedXml = await xmlDocuments.Table
            .AsNoTracking()
            .Where(xml => xml.EcfDocumentId == ecfDocumentId)
            .OrderByDescending(xml => xml.EcfXmlDocumentId)
            .Select(xml => xml.XmlSigned)
            .FirstOrDefaultAsync(cancellationToken);

        var byDocument = await LoadTransmissionsAsync([ecfDocumentId], cancellationToken);

        var trackId = byDocument.TryGetValue(ecfDocumentId, out var list)
            ? list.FirstOrDefault(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))?.TrackId
            : null;

        var source = new EcfLookupSource(
            ecfDocumentId,
            detail.Ncf,
            detail.Total,
            detail.IssueDateUtc,
            detail.SignatureDateTime,
            issuerRnc,
            detail.CustomerRnc,
            trackId,
            signedXml);

        var environment = EcfLookupLogic.ResolveEnvironment(configuration["EcfXmlValidation:TargetDgiiEnvironment"]);

        return EcfLookupLogic.BuildResponse(source, state, attempts, environment);
    }

    /// <summary>Transmisiones de esos documentos, la más reciente primero dentro de cada uno.</summary>
    private async Task<Dictionary<int, List<TransmissionRow>>> LoadTransmissionsAsync(
        List<int> documentIds,
        CancellationToken cancellationToken)
    {
        var rows = await transmissions.Table
            .AsNoTracking()
            .Where(transmission => documentIds.Contains(transmission.EcfDocumentId))
            .Select(transmission => new TransmissionRow(
                transmission.EcfDocumentId,
                transmission.TrackId,
                transmission.ResponsePayload,
                transmission.SentAtUtc))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.EcfDocumentId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(row => row.SentAtUtc).ToList());
    }

    private static string? LastPayload(Dictionary<int, List<TransmissionRow>> byDocument, int documentId) =>
        byDocument.TryGetValue(documentId, out var list) ? list[0].ResponsePayload : null;
}
