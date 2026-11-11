using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class BillingAccountPaymentService(
    IBillingAccountService billingAccountService,
    IClientPaymentService clientPaymentService,
    IClientPaymentItemService clientPaymentItemService,
    IBillingAccountMonthlyUsageService usageService,
    IPlanOverageTierService planOverageTierService,
    IUnitOfWork unitOfWork) : IBillingAccountPaymentService
{
    private const string AccountNotFound = "Cuenta no encontrada.";
    private const string OveragesUnavailable = "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.";

    public async Task<PaymentRegistrationResult<AccountPaymentPreviewDto>> GetPreviewAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await billingAccountService.Table.AsNoTracking()
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<AccountPaymentPreviewDto>.NotFound(AccountNotFound);

        var today = DateTimeExtensions.DrNow.Date;
        var terms = BillingTerms.For(account);
        var pending = await GetPendingOveragesAsync([account.BillingAccountId], cancellationToken);

        return PaymentRegistrationResult<AccountPaymentPreviewDto>.Success(new AccountPaymentPreviewDto
        {
            AccountGuidId = account.GuidId,
            AccountName = account.Name,
            PaymentSuspended = account.PaymentSuspendedAtUtc != null,
            PlanFee = PaymentCalculator.GetMonthlyFee(terms) > 0 ? PlanFeePreviewBuilder.Build(terms, today) : null,
            PendingOverages = pending.GetValueOrDefault(account.BillingAccountId) ?? []
        });
    }

    public async Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string accountGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var overageIds = request.OverageUsageIds.Distinct().ToList();

        var errors = PaymentRegistrationPolicy.ValidateRequest(
            request.PaymentDate, request.PaymentMethod, request.IncludePlanFee, overageIds.Count,
            request.Reference, request.Notes, today);
        if (errors.Count > 0) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(errors);

        var account = await billingAccountService.Table
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<PaymentReceiptDto>.NotFound(AccountNotFound);
        if (request.IncludePlanFee && PaymentCalculator.GetMonthlyFee(BillingTerms.For(account)) <= 0)
            return PaymentRegistrationResult<PaymentReceiptDto>.Invalid("El plan de la cuenta no tiene mensualidad a pagar.");

        var pending = (await GetPendingOveragesAsync([account.BillingAccountId], cancellationToken))
            .GetValueOrDefault(account.BillingAccountId) ?? [];
        var selected = pending.Where(p => overageIds.Contains(p.BillingAccountMonthlyUsageId)).ToList();
        if (selected.Count != overageIds.Count)
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(OveragesUnavailable);

        var paymentDate = request.PaymentDate!.Value.Date;
        var wasSuspended = account.PaymentSuspendedAtUtc != null;

        // Se arma antes de mover las fechas de la cuenta: el ciclo parte del vencimiento actual.
        var draft = AccountPaymentBuilder.Build(account, request, selected, paymentDate, userId);

        if (request.IncludePlanFee)
        {
            account.LastPaymentDate = paymentDate;
            account.NextPaymentDate = draft.NewNextPaymentDate;
        }

        var reactivated = PaymentRegistrationPolicy.ShouldReactivate(wasSuspended, request.IncludePlanFee, account.NextPaymentDate, today);
        if (reactivated)
            account.PaymentSuspendedAtUtc = null;

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientPaymentService.InsertAsync(draft.Payment);
                await billingAccountService.UpdateAsync(account);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // El índice único de excedente se disparó por un registro concurrente.
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(OveragesUnavailable);
        }

        var receipt = PaymentReceiptMapper.ToReceipt(draft.Payment, account.GuidId, account.Name, isAccount: true);
        receipt.Reactivated = reactivated;
        receipt.StillSuspended = wasSuspended && !reactivated;

        return PaymentRegistrationResult<PaymentReceiptDto>.Success(receipt);
    }

    public async Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string accountGuid, CancellationToken cancellationToken = default)
    {
        var account = await billingAccountService.Table.AsNoTracking()
            .FirstOrDefaultAsync(a => a.GuidId == accountGuid, cancellationToken);

        if (account == null) return PaymentRegistrationResult<List<PaymentReceiptDto>>.NotFound(AccountNotFound);

        var payments = await clientPaymentService.Table.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.BillingAccountId == account.BillingAccountId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ClientPaymentId)
            .ToListAsync(cancellationToken);

        return PaymentRegistrationResult<List<PaymentReceiptDto>>.Success(
            payments.Select(p => PaymentReceiptMapper.ToReceipt(p, account.GuidId, account.Name, isAccount: true)).ToList());
    }

    public async Task<Dictionary<int, List<AccountPendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> accountIds, CancellationToken cancellationToken = default)
    {
        if (accountIds.Count == 0) return [];

        var today = DateTimeExtensions.DrNow.Date;
        var ids = accountIds.ToList();

        var usages = await usageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.BillingAccountId)
                        && (u.Year < today.Year || (u.Year == today.Year && u.Month < today.Month)))
            .ToListAsync(cancellationToken);

        if (usages.Count == 0) return [];

        var usageIds = usages.Select(u => u.BillingAccountMonthlyUsageId).ToList();
        var paidUsageIds = (await clientPaymentItemService.Table.AsNoTracking()
                .Where(i => i.ItemType == (int)ClientPaymentItemType.Overage
                            && i.BillingAccountMonthlyUsageId != null
                            && usageIds.Contains(i.BillingAccountMonthlyUsageId.Value))
                .Select(i => i.BillingAccountMonthlyUsageId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
        var tiers = await planOverageTierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        return PendingOverageCalculator.ForAccounts(usages, paidUsageIds, tiers, today);
    }
}
