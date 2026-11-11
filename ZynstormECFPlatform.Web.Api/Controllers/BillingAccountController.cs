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
    IBillingAccountViewService viewService,
    IClientService clientService,
    IUserClientService userClientService,
    IPlanService planService,
    IAccountService userAccountService,
    IUnitOfWork unitOfWork,
    ILogger<BillingAccountController> logger) : ControllerBase
{
    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet("")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await viewService.GetAllAsync(cancellationToken));
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
            var detail = await viewService.GetDetailAsync(guid, cancellationToken);
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
            return Ok(await viewService.GetDetailAsync(inserted!.GuidId, cancellationToken));
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
            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error actualizando la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    /// <summary>
    /// Deja como miembros exactamente los clientes indicados: asigna los nuevos y suelta a los que ya no figuran.
    /// El PlanId propio de un cliente asignado se conserva pero deja de cobrarse. Al asignar, el dueño recibe
    /// el acceso (UserClient) al cliente; al soltar, el acceso del dueño no se quita.
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

            // El dueño debe ver a sus clientes en listados, documentos y dashboard: todo eso se rige por UserClient.
            var requestedIds = requestedClients.Select(c => c.ClientId).ToList();
            var ownerLinks = (await userClientService.GetManyByAsync(
                    x => x.UserId == account.OwnerUserId && requestedIds.Contains(x.ClientId), cancellationToken))
                .Select(x => x.ClientId)
                .ToHashSet();

            await unitOfWork.ExecuteInTransactionAsync(async _ =>
            {
                await clientService.UpdateAsync(affected);

                foreach (var clientId in requestedIds.Where(id => !ownerLinks.Contains(id)))
                    await userClientService.InsertAsync(new UserClient { UserId = account.OwnerUserId, ClientId = clientId });
            }, cancellationToken);

            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error asignando clientes a la cuenta {Guid}", guid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("guid/{guid}/clients/{clientGuid}/quota")]
    public async Task<IActionResult> SetClientQuota(string guid, string clientGuid, [FromBody] ClientQuotaDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await viewService.SetClientQuotaAsync(guid, clientGuid, dto.Quota, cancellationToken);
            if (result.Outcome != PaymentRegistrationOutcome.Ok) return ToActionResult(result);

            return Ok(await viewService.GetDetailAsync(guid, cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error fijando el cupo del cliente {ClientGuid} en la cuenta {Guid}", clientGuid, guid);
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
}
