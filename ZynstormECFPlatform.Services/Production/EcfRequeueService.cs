using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Data;
using ZynstormECFPlatform.Services.Jobs;

namespace ZynstormECFPlatform.Services.Production;

public enum EcfRequeueOutcome { Ok, NotFound, Conflict }

public sealed record EcfRequeueResult(EcfRequeueOutcome Outcome, EcfRequeueAction? Action, string Message);

public interface IEcfRequeueService
{
    /// <summary>Qué se puede hacer con un comprobante ahora (null si nada). Misma regla que usa <see cref="RequeueAsync"/>.</summary>
    EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot);

    Task<EcfRequeueResult> RequeueAsync(
        string guid,
        string? userId,
        bool isSuperAdmin,
        string userName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Re-encola un comprobante atascado: lo reenvía a la DGII (<see cref="EcfTransmitJob"/>) o vuelve a
/// consultar su estado (<see cref="EcfRetrackJob"/>). La transición se reclama con un UPDATE
/// condicional, así que dos peticiones simultáneas no encolan dos veces.
/// </summary>
public class EcfRequeueService(StorageContext context, IConfiguration configuration) : IEcfRequeueService
{
    private TimeSpan StaleAfter => TimeSpan.FromMinutes(Math.Clamp(
        configuration.GetValue<int?>("EcfIdempotency:StaleInFlightMinutes") ?? 5,
        1,
        120));

    public EcfRequeueAction? Evaluate(EcfRequeueSnapshot snapshot) =>
        EcfRequeuePolicy.Evaluate(snapshot, DateTime.UtcNow, StaleAfter);

    public async Task<EcfRequeueResult> RequeueAsync(
        string guid,
        string? userId,
        bool isSuperAdmin,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<EcfDocument>().AsNoTracking().Where(d => d.GuidId == guid);

        if (!isSuperAdmin)
        {
            query = query.Where(d => d.Client.UserClients.Any(uc => uc.UserId == userId));
        }

        var row = await query
            .Select(d => new
            {
                d.EcfDocumentId,
                d.ClientId,
                d.EcfStatusId,
                d.RegisteredAt,
                d.LastUpdateUtc,
                HasSignedXml = d.EcfXmlDocuments.Any(x => x.XmlSigned != null && x.XmlSigned != ""),
                HasTrackId = d.EcfTransmissions.Any(t => t.TrackId != ""),
                Transmissions = d.EcfTransmissions.Count()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.NotFound, null, "No se encontró el comprobante o no tiene permisos sobre él.");
        }

        var observed = row.LastUpdateUtc ?? row.RegisteredAt;
        var action = Evaluate(new EcfRequeueSnapshot(row.EcfStatusId, row.HasSignedXml, row.HasTrackId, observed));

        if (action is null)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.Conflict, null, "Este comprobante ya no se puede re-encolar: cambió de estado o se re-encoló hace poco.");
        }

        var now = DateTime.UtcNow;
        var newStatus = action == EcfRequeueAction.Transmit ? 7 : row.EcfStatusId;

        // Reclamo atómico: solo gana quien todavía ve el documento como lo evaluó.
        var claimed = await context.Set<EcfDocument>()
            .Where(d => d.EcfDocumentId == row.EcfDocumentId
                        && d.EcfStatusId == row.EcfStatusId
                        && (d.LastUpdateUtc ?? d.RegisteredAt) == observed)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(d => d.EcfStatusId, newStatus)
                    .SetProperty(d => d.LastUpdateUtc, (DateTime?)now),
                cancellationToken);

        if (claimed == 0)
        {
            return new EcfRequeueResult(EcfRequeueOutcome.Conflict, null, "Otro usuario ya re-encoló este comprobante.");
        }

        var note = action == EcfRequeueAction.Transmit
            ? $"Re-encolado por {userName}: reenviar a la DGII."
            : $"Re-encolado por {userName}: reconsultar el estado a la DGII.";

        context.Set<EcfStatusHistory>().Add(new EcfStatusHistory
        {
            EcfDocumentId = row.EcfDocumentId,
            EcfStatusId = newStatus,
            Message = note
        });
        context.Set<SystemLog>().Add(new SystemLog
        {
            ClientId = row.ClientId,
            EcfDocumentId = row.EcfDocumentId,
            LogLevel = "Information",
            Message = note,
            CreateAtUtc = now
        });
        await context.SaveChangesAsync(cancellationToken);

        // El ambiente de la DGII lo impone EcfXmlValidation:TargetDgiiEnvironment.
        if (action == EcfRequeueAction.Transmit)
        {
            BackgroundJob.Enqueue<EcfTransmitJob>(job => job.Execute(
                row.EcfDocumentId,
                EcfRequeuePolicy.NextAttemptNumber(row.Transmissions),
                DgiiEnvironment.Production));
        }
        else
        {
            BackgroundJob.Enqueue<EcfRetrackJob>(job => job.Execute(row.EcfDocumentId, 1, DgiiEnvironment.Production));
        }

        var message = action == EcfRequeueAction.Transmit
            ? "Comprobante en cola para reenviarse a la DGII."
            : "Comprobante en cola para volver a consultar su estado a la DGII.";

        return new EcfRequeueResult(EcfRequeueOutcome.Ok, action, message);
    }
}
