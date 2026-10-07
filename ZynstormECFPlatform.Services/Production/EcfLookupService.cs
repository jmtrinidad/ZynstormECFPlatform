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
}

public class EcfLookupService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients,
    IConfiguration configuration) : IEcfLookupService
{
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

        var ids = found.Select(document => document.EcfDocumentId).ToList();

        var sent = await transmissions.Table
            .AsNoTracking()
            .Where(transmission => ids.Contains(transmission.EcfDocumentId))
            .Select(transmission => new
            {
                transmission.EcfDocumentId,
                transmission.TrackId,
                transmission.ResponsePayload,
                transmission.SentAtUtc
            })
            .ToListAsync(cancellationToken);

        // Más reciente primero: el primer elemento es la última transmisión del documento.
        var transmissionsByDocument = sent
            .GroupBy(transmission => transmission.EcfDocumentId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(transmission => transmission.SentAtUtc).ToList());

        var candidates = found
            .Select(document => new EcfLookupCandidate(
                document.EcfDocumentId,
                EcfLookupLogic.ResolveState(
                    document.EcfStatusId,
                    transmissionsByDocument.TryGetValue(document.EcfDocumentId, out var list)
                        ? list[0].ResponsePayload
                        : null),
                document.RegisteredAt))
            .ToList();

        var best = EcfLookupLogic.PickBest(candidates)!;

        var detail = await documents.Table
            .AsNoTracking()
            .Where(document => document.EcfDocumentId == best.EcfDocumentId)
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
            .Where(xml => xml.EcfDocumentId == best.EcfDocumentId)
            .OrderByDescending(xml => xml.EcfXmlDocumentId)
            .Select(xml => xml.XmlSigned)
            .FirstOrDefaultAsync(cancellationToken);

        var trackId = transmissionsByDocument.TryGetValue(best.EcfDocumentId, out var bestList)
            ? bestList.FirstOrDefault(transmission => !string.IsNullOrWhiteSpace(transmission.TrackId))?.TrackId
            : null;

        var source = new EcfLookupSource(
            best.EcfDocumentId,
            detail.Ncf,
            detail.Total,
            detail.IssueDateUtc,
            detail.SignatureDateTime,
            issuerRnc,
            detail.CustomerRnc,
            trackId,
            signedXml);

        var environment = EcfLookupLogic.ResolveEnvironment(configuration["EcfXmlValidation:TargetDgiiEnvironment"]);

        return EcfLookupLogic.BuildResponse(source, best.State, found.Count, environment);
    }
}
