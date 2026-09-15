using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Billing;

public static class ClientUsageSql
{
    // Una sola sentencia: marca el documento (solo si no estaba contado) y, únicamente si lo marcó,
    // inserta o incrementa la fila del mes. Es atómica e idempotente ante reintentos y concurrencia.
    public const string RegisterAccepted = """
        WITH marked AS (
            UPDATE "EcfDocument"
            SET "BillingCountedAtUtc" = @Now
            WHERE "EcfDocumentId" = @EcfDocumentId AND "BillingCountedAtUtc" IS NULL
            RETURNING "ClientId"
        )
        INSERT INTO "ClientMonthlyUsage"
            ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
        SELECT "ClientId", @Year, @Month, 1, @PlanId, @PlanName, @MonthlyFee, @MonthlyDocumentLimit, @Now
        FROM marked
        ON CONFLICT ("ClientId", "Year", "Month")
        DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                      "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";
        """;
}

public class ClientUsageService(
    IClientService clientService,
    IPlanService planService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    ILogger<ClientUsageService> logger) : IClientUsageService
{
    public async Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await clientService.GetNoTrackingByAsync(c => c.ClientId == clientId, cancellationToken);
            if (client?.PlanId is not int planId)
                return false;

            var plan = await planService.GetNoTrackingByAsync(
                p => p.PlanId == planId && p.StatusId == (int)StatusEnum.Active, cancellationToken);
            if (plan == null)
                return false;

            // Los planes de renta no acumulan consumo mensual ni excedente.
            if (!BillingCalculator.AccruesDocumentUsage(plan.PlanTypeId))
                return false;

            var drNow = DateTimeExtensions.DrNow;
            var affected = await clientMonthlyUsageService.ExecuteAsync(ClientUsageSql.RegisterAccepted, new
            {
                Now = DateTime.UtcNow,
                EcfDocumentId = ecfDocumentId,
                Year = drNow.Year,
                Month = drNow.Month,
                PlanId = plan.PlanId,
                PlanName = plan.Name,
                MonthlyFee = plan.MonthlyFee,
                MonthlyDocumentLimit = plan.MonthlyDocumentLimit
            }, cancellationToken);

            return affected > 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registrando consumo mensual. EcfDocumentId {EcfDocumentId}, ClientId {ClientId}", ecfDocumentId, clientId);
            return false;
        }
    }
}
