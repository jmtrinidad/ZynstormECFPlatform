using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
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

    // Igual que RegisterAccepted, pero para un cliente con cuenta: suma 1 en la fila del cliente
    // (solo detalle: sin plan, mensualidad 0 y límite -1) y 1 en la fila de la cuenta (el pool, que es
    // lo que se cobra). Las dos escrituras salen del mismo documento marcado y se ejecutan siempre:
    // PostgreSQL corre un CTE con INSERT aunque la consulta principal no lo lea.
    public const string RegisterAcceptedInAccount = """
        WITH marked AS (
            UPDATE "EcfDocument"
            SET "BillingCountedAtUtc" = @Now
            WHERE "EcfDocumentId" = @EcfDocumentId AND "BillingCountedAtUtc" IS NULL
            RETURNING "ClientId"
        ),
        client_usage AS (
            INSERT INTO "ClientMonthlyUsage"
                ("ClientId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
            SELECT "ClientId", @Year, @Month, 1, CAST(NULL AS integer), @PlanName, 0, -1, @Now
            FROM marked
            ON CONFLICT ("ClientId", "Year", "Month")
            DO UPDATE SET "AcceptedDocuments" = "ClientMonthlyUsage"."AcceptedDocuments" + 1,
                          "LastUpdateUtc" = EXCLUDED."LastUpdateUtc"
            RETURNING "ClientId"
        )
        INSERT INTO "BillingAccountMonthlyUsage"
            ("BillingAccountId", "Year", "Month", "AcceptedDocuments", "PlanId", "PlanName", "MonthlyFee", "MonthlyDocumentLimit", "LastUpdateUtc")
        SELECT @BillingAccountId, @Year, @Month, 1, @PlanId, @PlanName, @MonthlyFee, @MonthlyDocumentLimit, @Now
        FROM marked
        ON CONFLICT ("BillingAccountId", "Year", "Month")
        DO UPDATE SET "AcceptedDocuments" = "BillingAccountMonthlyUsage"."AcceptedDocuments" + 1,
                      "LastUpdateUtc" = EXCLUDED."LastUpdateUtc";
        """;
}

public class ClientUsageService(
    IClientService clientService,
    IPlanService planService,
    IBillingAccountService billingAccountService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    ILogger<ClientUsageService> logger) : IClientUsageService
{
    public async Task<bool> RegisterAcceptedAsync(int ecfDocumentId, int clientId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = await clientService.GetNoTrackingByAsync(c => c.ClientId == clientId, cancellationToken);
            if (client == null)
                return false;

            // Un cliente con cuenta factura contra el pool; su PlanId propio se ignora.
            if (client.BillingAccountId is int billingAccountId)
                return await RegisterInAccountAsync(ecfDocumentId, billingAccountId, cancellationToken);

            return await RegisterForClientAsync(ecfDocumentId, client, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registrando consumo mensual. EcfDocumentId {EcfDocumentId}, ClientId {ClientId}", ecfDocumentId, clientId);
            return false;
        }
    }

    private async Task<bool> RegisterForClientAsync(int ecfDocumentId, Client client, CancellationToken cancellationToken)
    {
        if (client.PlanId is not int planId)
            return false;

        var plan = await GetActiveAccruingPlanAsync(planId, cancellationToken);
        if (plan == null)
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

    private async Task<bool> RegisterInAccountAsync(int ecfDocumentId, int billingAccountId, CancellationToken cancellationToken)
    {
        var account = await billingAccountService.GetNoTrackingByAsync(
            a => a.BillingAccountId == billingAccountId && a.StatusId == (int)StatusEnum.Active, cancellationToken);
        if (account == null)
            return false;

        var plan = await GetActiveAccruingPlanAsync(account.PlanId, cancellationToken);
        if (plan == null)
            return false;

        var drNow = DateTimeExtensions.DrNow;
        var affected = await clientMonthlyUsageService.ExecuteAsync(ClientUsageSql.RegisterAcceptedInAccount, new
        {
            Now = DateTime.UtcNow,
            EcfDocumentId = ecfDocumentId,
            Year = drNow.Year,
            Month = drNow.Month,
            BillingAccountId = account.BillingAccountId,
            PlanId = plan.PlanId,
            PlanName = plan.Name,
            MonthlyFee = plan.MonthlyFee,
            MonthlyDocumentLimit = plan.MonthlyDocumentLimit
        }, cancellationToken);

        return affected > 0;
    }

    /// <summary>Plan activo que acumula consumo. Los planes de renta no acumulan consumo mensual ni excedente.</summary>
    private async Task<Plan?> GetActiveAccruingPlanAsync(int planId, CancellationToken cancellationToken)
    {
        var plan = await planService.GetNoTrackingByAsync(
            p => p.PlanId == planId && p.StatusId == (int)StatusEnum.Active, cancellationToken);

        return plan != null && BillingCalculator.AccruesDocumentUsage(plan.PlanTypeId) ? plan : null;
    }
}
