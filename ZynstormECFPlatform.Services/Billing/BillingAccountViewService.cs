using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountViewService(
    IBillingAccountService accountService,
    IBillingAccountPaymentService paymentService,
    IBillingAccountMonthlyUsageService usageService,
    IClientMonthlyUsageService clientUsageService,
    IClientService clientService,
    IPlanOverageTierService tierService) : IBillingAccountViewService
{
    private IQueryable<BillingAccount> AccountsWithDetails() =>
        accountService.Table.AsNoTracking().Include(a => a.Plan).Include(a => a.OwnerUser);

    public async Task<List<BillingAccountViewDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await AccountsWithDetails().OrderBy(a => a.Name).ToListAsync(cancellationToken);
        return await BuildViewsAsync(accounts, cancellationToken);
    }

    public async Task<string?> FindGuidByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default) =>
        await accountService.Table.AsNoTracking()
            .Where(a => a.OwnerUserId == ownerUserId)
            .Select(a => a.GuidId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<BillingAccountDetailDto?> GetDetailAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await AccountsWithDetails().FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);
        if (account == null) return null;

        var view = (await BuildViewsAsync([account], cancellationToken)).Single();
        var today = DateTimeExtensions.DrNow;

        var clients = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId == account.BillingAccountId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var clientIds = clients.Select(c => c.ClientId).ToList();
        var accepted = await clientUsageService.Table.AsNoTracking()
            .Where(u => clientIds.Contains(u.ClientId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.ClientId, u => u.AcceptedDocuments, cancellationToken);

        var detail = new BillingAccountDetailDto();
        CopyView(view, detail);
        detail.Clients = clients.Select(c =>
        {
            var documents = accepted.GetValueOrDefault(c.ClientId);
            return new BillingAccountClientUsageDto
            {
                ClientGuidId = c.GuidId,
                ClientName = c.Name,
                ClientRnc = c.Rnc,
                AcceptedDocuments = documents,
                QuotaDocuments = c.QuotaDocuments,
                QuotaExceeded = BillingAccountPolicy.IsQuotaExceeded(documents, c.QuotaDocuments)
            };
        }).ToList();

        return detail;
    }

    public async Task<PaymentRegistrationResult<bool>> SetClientQuotaAsync(
        string accountGuid, string clientGuid, int? quota, CancellationToken cancellationToken = default)
    {
        var errors = BillingAccountPolicy.ValidateQuota(quota);
        if (errors.Count > 0) return PaymentRegistrationResult<bool>.Invalid(errors);

        var account = await accountService.Table.AsNoTracking()
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);
        if (account == null) return PaymentRegistrationResult<bool>.NotFound("Cuenta no encontrada.");

        var client = await clientService.Table
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid && c.BillingAccountId == account.BillingAccountId, cancellationToken);
        if (client == null) return PaymentRegistrationResult<bool>.NotFound("El cliente no pertenece a la cuenta.");

        client.QuotaDocuments = quota;
        await clientService.UpdateAsync(client);

        return PaymentRegistrationResult<bool>.Success(true);
    }

    private async Task<List<BillingAccountViewDto>> BuildViewsAsync(IReadOnlyList<BillingAccount> accounts, CancellationToken cancellationToken)
    {
        if (accounts.Count == 0) return [];

        var today = DateTimeExtensions.DrNow;
        var ids = accounts.Select(a => a.BillingAccountId).ToList();
        var planIds = accounts.Select(a => a.PlanId).Distinct().ToList();

        var poolNow = await usageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.BillingAccountId) && u.Year == today.Year && u.Month == today.Month)
            .ToDictionaryAsync(u => u.BillingAccountId, u => u.AcceptedDocuments, cancellationToken);

        var tiers = await tierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        var clientCounts = await clientService.Table.AsNoTracking()
            .Where(c => c.BillingAccountId != null && ids.Contains(c.BillingAccountId.Value))
            .GroupBy(c => c.BillingAccountId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var pending = await paymentService.GetPendingOveragesAsync(ids, cancellationToken);

        return accounts.Select(a =>
        {
            var terms = BillingTerms.For(a);
            var documents = poolNow.GetValueOrDefault(a.BillingAccountId);
            var calculation = BillingCalculator.Calculate(
                PaymentCalculator.GetMonthlyFee(terms),
                a.Plan.MonthlyDocumentLimit,
                tiers.Where(t => t.PlanId == a.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                documents);
            var (status, days) = PaymentCalculator.GetPaymentStatus(a.NextPaymentDate, PaymentCalculator.DefaultWarningDays);
            var accountPending = pending.GetValueOrDefault(a.BillingAccountId);

            return new BillingAccountViewDto
            {
                GuidId = a.GuidId,
                Name = a.Name,
                OwnerUserId = a.OwnerUserId,
                OwnerName = a.OwnerUser?.FullName ?? string.Empty,
                OwnerEmail = a.OwnerUser?.Email,
                PlanId = a.PlanId,
                PlanName = a.Plan.Name,
                MonthlyDocumentLimit = a.Plan.MonthlyDocumentLimit,
                MonthlyFee = PaymentCalculator.GetMonthlyFee(terms),
                CustomMonthlyFee = a.CustomMonthlyFee,
                StatusId = a.StatusId,
                ClientsCount = clientCounts.GetValueOrDefault(a.BillingAccountId),
                CurrentMonthAcceptedDocuments = documents,
                CurrentMonthOverageDocuments = calculation.OverageDocuments,
                CurrentMonthOverageAmount = calculation.OverageAmount,
                PaidMonths = a.PaidMonths,
                PrepaymentDiscountPercent = a.PrepaymentDiscountPercent,
                PaymentGraceDays = PaymentReminderPolicy.NormalizeGraceDays(a.PaymentGraceDays),
                LastPaymentDate = a.LastPaymentDate,
                NextPaymentDate = a.NextPaymentDate,
                PaymentStatus = (int)status,
                PaymentDaysToDue = days,
                PaymentSuspended = a.PaymentSuspendedAtUtc != null,
                PendingOverageAmount = accountPending?.Sum(p => p.Amount) ?? 0m,
                PendingOverageMonths = accountPending?.Count ?? 0
            };
        }).ToList();
    }

    private static void CopyView(BillingAccountViewDto from, BillingAccountViewDto to)
    {
        foreach (var property in typeof(BillingAccountViewDto).GetProperties().Where(p => p.CanRead && p.CanWrite))
            property.SetValue(to, property.GetValue(from));
    }
}
