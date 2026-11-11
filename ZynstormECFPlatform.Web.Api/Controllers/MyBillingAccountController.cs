using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;

namespace ZynstormECFPlatform.Web.Api.Controllers;

/// <summary>
/// La cuenta de facturación del Admin que la dirige: consulta del pool y de los pagos (solo lectura) y cupo
/// informativo por cliente. La cuenta se resuelve siempre por el usuario autenticado.
/// </summary>
[Authorize(Roles = "Admin")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
public class MyBillingAccountController(
    IBillingAccountViewService viewService,
    IBillingAccountPaymentService paymentService,
    ILogger<MyBillingAccountController> logger) : ControllerBase
{
    private const string NoAccount = "No tiene una cuenta de facturación.";

    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private async Task<string?> OwnAccountGuidAsync(CancellationToken cancellationToken) =>
        string.IsNullOrEmpty(CurrentUserId) ? null : await viewService.FindGuidByOwnerAsync(CurrentUserId, cancellationToken);

    [HttpGet("")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var detail = await viewService.GetDetailAsync(guid, cancellationToken);
            return detail == null ? NotFound(new { message = NoAccount }) : Ok(detail);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando la cuenta de facturación propia");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpGet("payments")]
    public async Task<IActionResult> GetPayments(CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var result = await paymentService.GetHistoryAsync(guid, cancellationToken);
            return result.Outcome == PaymentRegistrationOutcome.Ok
                ? Ok(result.Value)
                : NotFound(new { message = result.Errors.FirstOrDefault() ?? NoAccount });
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error consultando el historial de pagos de la cuenta propia");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [HttpPut("clients/{clientGuid}/quota")]
    public async Task<IActionResult> SetClientQuota(string clientGuid, [FromBody] ClientQuotaDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            var guid = await OwnAccountGuidAsync(cancellationToken);
            if (guid == null) return NotFound(new { message = NoAccount });

            var result = await viewService.SetClientQuotaAsync(guid, clientGuid, dto.Quota, cancellationToken);

            return result.Outcome switch
            {
                PaymentRegistrationOutcome.Ok => Ok(await viewService.GetDetailAsync(guid, cancellationToken)),
                PaymentRegistrationOutcome.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
                _ => BadRequest(new { message = string.Join(" ", result.Errors), errors = result.Errors })
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error fijando el cupo del cliente {ClientGuid} en la cuenta propia", clientGuid);
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
