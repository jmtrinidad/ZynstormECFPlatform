using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>Cuentas de facturación: un plan de comprobantes compartido por los clientes de un revendedor.</summary>
[Authorize(Roles = "SA")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class BillingAccountController(
    IBillingAccountService accountService,
    IBillingAccountPaymentService paymentService,
    IBillingAccountMonthlyUsageService usageService,
    IClientMonthlyUsageService clientUsageService,
    IClientService clientService,
    IPlanService planService,
    IPlanOverageTierService tierService,
    IAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ILogger<BillingAccountController> logger) : ControllerBase
{
    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private IQueryable<BillingAccount> AccountsWithDetails() =>
        accountService.Table.AsNoTracking().Include(a => a.Plan).Include(a => a.OwnerUser);

    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await AccountsWithDetails().OrderBy(a => a.Name).ToListAsync(cancellationToken);
            return Ok(await BuildViewsAsync(accounts, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error listando las cuentas de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}")]
    public async Task<IActionResult> GetByGuid(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await BuildDetailAsync(guid, cancellationToken);
            return detail == null ? NotFound(new { message = "Cuenta no encontrada." }) : Ok(detail);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPost("")]
    public async Task<IActionResult> Create([FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);

            var owner = string.IsNullOrWhiteSpace(dto.OwnerUserId) ? null : await userAccountService.GetUserByIdAsync(dto.OwnerUserId);
            var ownerIsAdmin = owner != null && await userAccountService.IsUserInRoleAsync(owner, "Admin");
            var ownerHasAccount = owner != null && await accountService.Table.AnyAsync(a => a.OwnerUserId == owner.Id, cancellationToken);
            errors.AddRange(BillingAccountPolicy.ValidateOwner(owner != null, ownerIsAdmin, ownerHasAccount));

            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var account = new BillingAccount
            {
                Name = dto.Name.Trim(),
                OwnerUserId = owner!.Id,
                PlanId = plan!.PlanId,
                StatusId = (int)StatusEnum.Active,
                CustomMonthlyFee = dto.CustomMonthlyFee,
                OfferStartDate = OfferPricing.ResolveStartDate(
                    null, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths), null, DateTimeExtensions.DrNow.Date),
                PaidMonths = dto.PaidMonths,
                PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent,
                PaymentGraceDays = dto.PaymentGraceDays,
                LastPaymentDate = dto.LastPaymentDate?.Date,
                NextPaymentDate = dto.NextPaymentDate?.Date
            };

            var inserted = await accountService.InsertAsync(account);
            return Ok(await BuildDetailAsync(inserted!.GuidId, cancellationToken));
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return Conflict(new { message = "El usuario ya tiene una cuenta de facturación." });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creando la cuenta de facturación");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("guid/{guid}")]
    public async Task<IActionResult> Update(string guid, [FromBody] BillingAccountSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.Include(a => a.Plan).FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var plan = await planService.GetNoTrackingByAsync(p => p.PlanId == dto.PlanId, cancellationToken);
            var errors = BillingAccountPolicy.ValidateAccount(
                dto.Name, plan, dto.CustomMonthlyFee, dto.PaidMonths, dto.PrepaymentDiscountPercent, dto.PaymentGraceDays);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            var previousPlanId = account.PlanId;
            account.Name = dto.Name.Trim();
            account.PlanId = plan!.PlanId;
            account.CustomMonthlyFee = dto.CustomMonthlyFee;
            account.OfferStartDate = OfferPricing.ResolveStartDate(
                previousPlanId, plan.PlanId, OfferPricing.PlanHasOffer(plan.OfferPrice, plan.OfferMonths),
                account.OfferStartDate, DateTimeExtensions.DrNow.Date);
            account.PaidMonths = dto.PaidMonths;
            account.PrepaymentDiscountPercent = dto.PrepaymentDiscountPercent;
            account.PaymentGraceDays = dto.PaymentGraceDays;
            account.LastPaymentDate = dto.LastPaymentDate?.Date;
            account.NextPaymentDate = dto.NextPaymentDate?.Date;

            // Como en un cliente: poner una fecha de pago futura reactiva la cuenta suspendida.
            if (account.PaymentSuspendedAtUtc != null && account.NextPaymentDate > DateTimeExtensions.DrNow.Date)
                account.PaymentSuspendedAtUtc = null;

            await accountService.UpdateAsync(account);
            return Ok(await BuildDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error actualizando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Deja como miembros exactamente los clientes indicados: asigna los nuevos y suelta a los que ya no figuran.
    /// El PlanId propio de un cliente asignado se conserva pero deja de cobrarse.
    /// </summary>
    [HttpPut("guid/{guid}/clients")]
    public async Task<IActionResult> SetClients(string guid, [FromBody] BillingAccountClientsDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var account = await accountService.Table.FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
            if (account == null) return NotFound(new { message = "Cuenta no encontrada." });

            var requested = dto.ClientGuidIds.Distinct().ToList();
            var affected = await clientService.Table
                .Where(c => requested.Contains(c.GuidId) || c.BillingAccountId == account.BillingAccountId)
                .ToListAsync(cancellationToken);

            var requestedClients = affected.Where(c => requested.Contains(c.GuidId)).ToList();
            var errors = BillingAccountPolicy.ValidateMembers(requestedClients, requested.Count, account.BillingAccountId);
            if (errors.Count > 0) return BadRequest(new { message = string.Join(" ", errors), errors });

            foreach (var client in affected)
                client.BillingAccountId = requested.Contains(client.GuidId) ? account.BillingAccountId : null;

            await unitOfWork.ExecuteInTransactionAsync(async _ => await clientService.UpdateAsync(affected), cancellationToken);

            return Ok(await BuildDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error asignando clientes a la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("guid/{guid}/payments/preview")]
    public async Task<IActionResult> GetPaymentPreview(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetPreviewAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el pago a registrar de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el pago a registrar." });
        }
    }

    [HttpPost("guid/{guid}/payments")]
    public async Task<IActionResult> RegisterPayment(string guid, [FromBody] RegisterPaymentRequestDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.RegisterAsync(guid, dto, CurrentUserId, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error registrando el pago de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo registrar el pago." });
        }
    }

    [HttpGet("guid/{guid}/payments")]
    public async Task<IActionResult> GetPaymentHistory(string guid, CancellationToken cancellationToken = default)
    {
        try
        {
            return ToActionResult(await paymentService.GetHistoryAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el historial de pagos de la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "No se pudo consultar el historial de pagos." });
        }
    }

    private IActionResult ToActionResult<T>(PaymentRegistrationResult<T> result) => result.Outcome switch
    {
        PaymentRegistrationOutcome.Ok => Ok(result.Value),
        PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
        PaymentRegistrationOutcome.Conflict => Conflict(new { message = string.Join(" ", result.Errors), errors = result.Errors }),
        _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
    };

    private async Task<BillingAccountDetailDto?> BuildDetailAsync(string guid, CancellationToken cancellationToken)
    {
        var account = await AccountsWithDetails().FirstOrDefaultAsync(a => a.GuidId == guid, cancellationToken);
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
