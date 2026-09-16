using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Billing;

public class ClientPaymentRegistrationService(
    IClientService clientService,
    IClientPaymentService clientPaymentService,
    IClientPaymentItemService clientPaymentItemService,
    IClientMonthlyUsageService clientMonthlyUsageService,
    IPlanOverageTierService planOverageTierService,
    IUnitOfWork unitOfWork) : IClientPaymentRegistrationService
{
    private const string ClientNotFound = "Cliente no encontrado.";
    private const string NoPlan = "El cliente no tiene un plan asignado.";
    private static readonly CultureInfo Es = CultureInfo.GetCultureInfo("es-DO");

    public async Task<PaymentRegistrationResult<PaymentPreviewDto>> GetPreviewAsync(string clientGuid, CancellationToken cancellationToken = default)
    {
        var client = await clientService.Table.AsNoTracking()
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<PaymentPreviewDto>.NotFound(ClientNotFound);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentPreviewDto>.Invalid(NoPlan);

        var today = DateTimeExtensions.DrNow.Date;
        var pending = await GetPendingOveragesAsync([client.ClientId], cancellationToken);

        return PaymentRegistrationResult<PaymentPreviewDto>.Success(new PaymentPreviewDto
        {
            ClientGuidId = client.GuidId,
            ClientName = client.Name,
            ClientRnc = client.Rnc,
            PaymentSuspended = client.PaymentSuspendedAtUtc != null,
            PlanFee = client.Plan.MonthlyFee > 0 ? BuildPlanFeePreview(client, today) : null,
            PendingOverages = pending.GetValueOrDefault(client.ClientId) ?? []
        });
    }

    public async Task<PaymentRegistrationResult<PaymentReceiptDto>> RegisterAsync(
        string clientGuid, RegisterPaymentRequestDto request, string? userId, CancellationToken cancellationToken = default)
    {
        var today = DateTimeExtensions.DrNow.Date;
        var overageIds = request.OverageUsageIds.Distinct().ToList();

        var errors = PaymentRegistrationPolicy.ValidateRequest(
            request.PaymentDate, request.PaymentMethod, request.IncludePlanFee, overageIds.Count,
            request.Reference, request.Notes, today);
        if (errors.Count > 0) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(errors);

        var client = await clientService.Table
            .Include(c => c.Plan)
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<PaymentReceiptDto>.NotFound(ClientNotFound);
        if (client.Plan == null) return PaymentRegistrationResult<PaymentReceiptDto>.Invalid(NoPlan);
        if (request.IncludePlanFee && client.Plan.MonthlyFee <= 0)
            return PaymentRegistrationResult<PaymentReceiptDto>.Invalid("El plan del cliente no tiene mensualidad a pagar.");

        var pending = (await GetPendingOveragesAsync([client.ClientId], cancellationToken))
            .GetValueOrDefault(client.ClientId) ?? [];
        var selectedOverages = pending.Where(p => overageIds.Contains(p.ClientMonthlyUsageId)).ToList();
        if (selectedOverages.Count != overageIds.Count)
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(
                "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.");

        var paymentDate = request.PaymentDate!.Value.Date;
        var wasSuspended = client.PaymentSuspendedAtUtc != null;

        var payment = new ClientPayment
        {
            ClientId = client.ClientId,
            PaymentDate = paymentDate,
            PaymentMethod = request.PaymentMethod,
            Reference = string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            RegisteredByUserId = userId
        };

        if (request.IncludePlanFee)
        {
            var calculation = PaymentCalculator.Calculate(client.Plan.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);
            var previousNext = client.NextPaymentDate?.Date;
            var newNext = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(previousNext, paymentDate, calculation.MonthsCovered);

            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.PlanFee,
                Amount = calculation.Total,
                PlanId = client.PlanId,
                PlanName = client.Plan.Name,
                MonthsCovered = calculation.MonthsCovered,
                GrossAmount = calculation.GrossAmount,
                DiscountPercent = calculation.DiscountPercent,
                DiscountAmount = calculation.DiscountAmount,
                PreviousNextPaymentDate = previousNext,
                NewNextPaymentDate = newNext
            });

            client.LastPaymentDate = paymentDate;
            client.NextPaymentDate = newNext;
        }

        foreach (var overage in selectedOverages.OrderBy(o => o.Year).ThenBy(o => o.Month))
        {
            payment.Items.Add(new ClientPaymentItem
            {
                ItemType = (int)ClientPaymentItemType.Overage,
                Amount = overage.Amount,
                PlanId = overage.PlanId,
                PlanName = overage.PlanName,
                ClientMonthlyUsageId = overage.ClientMonthlyUsageId,
                Year = overage.Year,
                Month = overage.Month,
                OverageDocuments = overage.OverageDocuments
            });
        }

        payment.TotalAmount = payment.Items.Sum(i => i.Amount);

        var reactivated = PaymentRegistrationPolicy.ShouldReactivate(wasSuspended, request.IncludePlanFee, client.NextPaymentDate, today);
        if (reactivated)
        {
            client.ClientInactive = false;
            client.PaymentSuspendedAtUtc = null;
        }

        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientPaymentService.InsertAsync(payment);
                await clientService.UpdateAsync(client);
            }, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // El índice único de excedente se disparó por un registro concurrente.
            return PaymentRegistrationResult<PaymentReceiptDto>.Conflict(
                "Alguno de los excedentes seleccionados ya fue pagado o no está disponible para pago.");
        }

        var receipt = ToReceipt(payment, client);
        receipt.Reactivated = reactivated;
        receipt.StillSuspended = wasSuspended && !reactivated;

        return PaymentRegistrationResult<PaymentReceiptDto>.Success(receipt);
    }

    public async Task<PaymentRegistrationResult<List<PaymentReceiptDto>>> GetHistoryAsync(string clientGuid, CancellationToken cancellationToken = default)
    {
        var client = await clientService.Table.AsNoTracking()
            .FirstOrDefaultAsync(c => c.GuidId == clientGuid, cancellationToken);

        if (client == null) return PaymentRegistrationResult<List<PaymentReceiptDto>>.NotFound(ClientNotFound);

        var payments = await clientPaymentService.Table.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.ClientId == client.ClientId)
            .OrderByDescending(p => p.PaymentDate)
            .ThenByDescending(p => p.ClientPaymentId)
            .ToListAsync(cancellationToken);

        return PaymentRegistrationResult<List<PaymentReceiptDto>>.Success(payments.Select(p => ToReceipt(p, client)).ToList());
    }

    public async Task<Dictionary<int, List<PendingOverageDto>>> GetPendingOveragesAsync(
        IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0) return [];

        var today = DateTimeExtensions.DrNow.Date;
        var ids = clientIds.ToList();

        var usages = await clientMonthlyUsageService.Table.AsNoTracking()
            .Where(u => ids.Contains(u.ClientId)
                        && (u.Year < today.Year || (u.Year == today.Year && u.Month < today.Month)))
            .ToListAsync(cancellationToken);

        if (usages.Count == 0) return [];

        var usageIds = usages.Select(u => u.ClientMonthlyUsageId).ToList();
        var paidUsageIds = (await clientPaymentItemService.Table.AsNoTracking()
                .Where(i => i.ItemType == (int)ClientPaymentItemType.Overage
                            && i.ClientMonthlyUsageId != null
                            && usageIds.Contains(i.ClientMonthlyUsageId.Value))
                .Select(i => i.ClientMonthlyUsageId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var planIds = usages.Where(u => u.PlanId.HasValue).Select(u => u.PlanId!.Value).Distinct().ToList();
        var tiers = await planOverageTierService.Table.AsNoTracking()
            .Where(t => planIds.Contains(t.PlanId))
            .ToListAsync(cancellationToken);

        return usages
            .Where(u => !paidUsageIds.Contains(u.ClientMonthlyUsageId)
                        && PaymentRegistrationPolicy.IsOverageMonthPayable(u.Year, u.Month, today))
            .Select(u =>
            {
                var calculation = BillingCalculator.Calculate(
                    u.MonthlyFee,
                    u.MonthlyDocumentLimit,
                    tiers.Where(t => t.PlanId == u.PlanId).Select(t => new OverageTier(t.FromUnit, t.ToUnit, t.UnitPrice)),
                    u.AcceptedDocuments);

                return (u.ClientId, Pending: new PendingOverageDto
                {
                    ClientMonthlyUsageId = u.ClientMonthlyUsageId,
                    Year = u.Year,
                    Month = u.Month,
                    PlanId = u.PlanId,
                    PlanName = u.PlanName,
                    AcceptedDocuments = u.AcceptedDocuments,
                    MonthlyDocumentLimit = u.MonthlyDocumentLimit,
                    OverageDocuments = calculation.OverageDocuments,
                    Amount = calculation.OverageAmount
                });
            })
            .Where(x => x.Pending.Amount > 0)
            .GroupBy(x => x.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Pending).OrderBy(p => p.Year).ThenBy(p => p.Month).ToList());
    }

    private static PlanFeePreviewDto BuildPlanFeePreview(Client client, DateTime today)
    {
        var calculation = PaymentCalculator.Calculate(client.Plan!.MonthlyFee, client.PaidMonths, client.PrepaymentDiscountPercent);

        return new PlanFeePreviewDto
        {
            PlanName = client.Plan.Name,
            PlanTypeId = client.Plan.PlanTypeId,
            MonthlyFee = calculation.MonthlyFee,
            MonthsCovered = calculation.MonthsCovered,
            GrossAmount = calculation.GrossAmount,
            DiscountPercent = calculation.DiscountPercent,
            DiscountAmount = calculation.DiscountAmount,
            Total = calculation.Total,
            CurrentNextPaymentDate = client.NextPaymentDate?.Date,
            NewNextPaymentDate = PaymentRegistrationPolicy.CalculateNewNextPaymentDate(client.NextPaymentDate, today, calculation.MonthsCovered)
        };
    }

    private static PaymentReceiptDto ToReceipt(ClientPayment payment, Client client) => new()
    {
        ClientPaymentId = payment.ClientPaymentId,
        ReceiptNumber = PaymentRegistrationPolicy.FormatReceiptNumber(payment.ClientPaymentId),
        ClientGuidId = client.GuidId,
        ClientName = client.Name,
        PaymentDate = payment.PaymentDate,
        PaymentMethod = payment.PaymentMethod,
        Reference = payment.Reference,
        Notes = payment.Notes,
        TotalAmount = payment.TotalAmount,
        NextPaymentDate = payment.Items
            .FirstOrDefault(i => i.ItemType == (int)ClientPaymentItemType.PlanFee)?.NewNextPaymentDate,
        Items = payment.Items
            .OrderBy(i => i.ItemType)
            .ThenBy(i => i.Year)
            .ThenBy(i => i.Month)
            .Select(i => new PaymentReceiptItemDto
            {
                ItemType = i.ItemType,
                Description = DescribeItem(i),
                Amount = i.Amount,
                MonthsCovered = i.MonthsCovered,
                DiscountAmount = i.DiscountAmount,
                PreviousNextPaymentDate = i.PreviousNextPaymentDate,
                NewNextPaymentDate = i.NewNextPaymentDate,
                Year = i.Year,
                Month = i.Month,
                OverageDocuments = i.OverageDocuments
            })
            .ToList()
    };

    private static string DescribeItem(ClientPaymentItem item)
    {
        if (item.ItemType == (int)ClientPaymentItemType.PlanFee)
        {
            var months = item.MonthsCovered == 1 ? "1 mes" : $"{item.MonthsCovered} meses";
            return $"Mensualidad {item.PlanName} ({months})";
        }

        var period = item.Year is int year && item.Month is int month
            ? Es.TextInfo.ToTitleCase(new DateTime(year, month, 1).ToString("MMMM yyyy", Es))
            : string.Empty;
        return $"Excedente {period} ({item.OverageDocuments} comprobantes)";
    }
}
