using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers;

[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
[ApiController]
[Authorize(Roles = "SA")]
public sealed class SerialController(
    ISerialGeneratorService serialGeneratorService,
    ILogger<SerialController> logger) : ControllerBase
{
    [HttpPost("generate")]
    [ProducesResponseType<GeneratedSerialDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GeneratedSerialDto>> Generate(
        [FromBody] GenerateSerialRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await serialGeneratorService.GenerateAsync(request.Description, cancellationToken);
            var response = new GeneratedSerialDto
            {
                GuidId = result.Entity.GuidId,
                Serial = result.Serial,
                Description = result.Entity.Description,
                RegisteredAt = result.Entity.RegisteredAt
            };

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo generar el serial.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    [AllowAnonymous]
    [HttpGet("validate")]
    [EnableRateLimiting("serial-validation")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ProducesResponseType<SerialValidationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SerialValidationDto>> Validate(
        [FromHeader(Name = "X-Serial-Key")] string? serial,
        CancellationToken cancellationToken)
    {
        try
        {
            Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Vary = "X-Serial-Key";

            var isValid = !string.IsNullOrWhiteSpace(serial)
                && serial.Length <= 64
                && await serialGeneratorService.ValidateAndConsumeAsync(serial, cancellationToken);

            return Ok(new SerialValidationDto { IsValid = isValid });
        }
        catch (OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "No se pudo validar el serial.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
