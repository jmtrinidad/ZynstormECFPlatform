using System.Text;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Production;
using ZynstormECFPlatform.Services.Ri.Received;
using ZynstormECFPlatform.Web.Api.Filters;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiKeyAuth]
    public class EcfController(
        IEcfProductionGeneratorService ecfGeneratorService,
        IReceivedEcfProductionService receivedEcfProductionService,
        IEcfLookupService ecfLookupService,
        IReceivedEcfService receivedEcfService,
        IConfiguration configuration,
        ICacheService cacheService,
        ILogger<EcfController> logger) : ControllerBase
    {
        private readonly IEcfProductionGeneratorService _ecfGeneratorService = ecfGeneratorService;
        private readonly IReceivedEcfProductionService _receivedEcfProductionService = receivedEcfProductionService;
        private readonly IEcfLookupService _ecfLookupService = ecfLookupService;
        private readonly IReceivedEcfService _receivedEcfService = receivedEcfService;
        private readonly IConfiguration _configuration = configuration;
        private readonly ICacheService _cacheService = cacheService;
        private readonly ILogger<EcfController> _logger = logger;

        /// <summary>
        /// Genera un XML de e-CF a partir del DTO de factura y lo valida contra el esquema XSD de la DGII.
        /// </summary>
        /// <param name="dto">Datos de la factura, emisor, comprador e ítems.</param>
        /// <returns>Resultado con el XML generado y errores de validación si existen.</returns>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GenerateXml([FromBody] EcfInvoiceRequestDto dto)
        {
            // 1. Validar DTO
            var dtoErrors = _ecfGeneratorService.ValidateDto(dto);

            if (dtoErrors.Count > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Errores de validación en los datos de entrada.",
                    dtoErrors
                });
            }

            try
            {
                // 2. Extraer TipoeCF para validación posterior
                var ecfType = NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF);

                // 3. Generar XML
                var xml = _ecfGeneratorService.GenerateUnsignedXml(dto);

                // 4. Validar XML contra Schema
                var xsdErrors = _ecfGeneratorService.ValidateXmlAgainstSchema(xml, ecfType);

                return Ok(new
                {
                    success = xsdErrors.Count == 0,
                    message = xsdErrors.Count == 0 ? "XML generado y validado con éxito." : "XML generado con errores de esquema.",
                    ecfType,
                    xml,
                    xsdErrors
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Error inesperado durante la generación del XML.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Recibe un e-CF con el modelo de simulacion legacy, genera el XML, valida XSD/XmlProd, firma, envia a DGII y
        /// consulta el estado inicial.
        /// </summary>
        [HttpPost("emit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> EmitEcf([FromBody] EcfInvoiceRequestDto dto, [FromQuery] DgiiEnvironment environment = DgiiEnvironment.Test)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Debe proporcionar el objeto e-CF." });

            try
            {
                var result = await _receivedEcfProductionService.ProcessAsync(
                    dto,
                    environment,
                    cancellationToken: HttpContext.RequestAborted);

                if (result.ClientInactive)
                    return StatusCode(StatusCodes.Status403Forbidden, result);

                if (result.HasUnexpectedError)
                    return StatusCode(StatusCodes.Status500InternalServerError, result);

                if (result.DtoErrors.Count > 0 || result.XsdErrors.Count > 0 || result.XmlProdErrors.Count > 0
                    || result.ConfigurationErrors.Count > 0 || result.XmlValidation?.IsValid == false)
                    return BadRequest(result);

                if (result.IsPending)
                    return StatusCode(StatusCodes.Status504GatewayTimeout, result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Error inesperado durante la emision del e-CF.",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Consulta el estado del envio a DGII por TrackId. Este endpoint usa el estado cacheado por el proceso inicial
        /// y por el job de seguimiento.
        /// </summary>
        [HttpGet("status/{trackId}")]
        [HttpGet("estado-envio/{trackId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetEmissionStatus(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                return BadRequest(new { success = false, message = "Debe proporcionar el TrackId." });

            var cacheKey = $"EcfStatus_{trackId.Trim()}";
            var status = _cacheService.Get<DgiiStatusResponse>(cacheKey);

            if (status == null)
            {
                return NotFound(new
                {
                    success = false,
                    isPending = true,
                    trackId,
                    message = "Estado no encontrado o expirado. Si el envio fue reciente, intente consultar nuevamente en unos segundos."
                });
            }

            return Ok(new
            {
                success = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase),
                isPending = ReceivedEcfProductionService.IsPendingDgiiStatus(status),
                isAcceptedConditional = ReceivedEcfProductionService.IsAcceptedConditionalDgiiStatus(status),
                requiresCorrection = ReceivedEcfProductionService.RequiresCorrectionDgiiStatus(status),
                trackId,
                dgiiResponse = status,
                status
            });
        }

        /// <summary>
        /// Consulta un e-CF ya recibido por su eNCF, dentro del cliente dueño de la API key.
        /// Existe para el integrador que perdió la respuesta de <c>emit</c> (timeout): devuelve
        /// el estado, el TrackId, el código de seguridad, la fecha de firma y el QR, de modo que
        /// no haga falta reenviar un comprobante que la DGII ya aceptó. No modifica nada.
        /// </summary>
        [HttpGet("by-ncf/{eNcf}")]
        [ProducesResponseType(typeof(EcfLookupResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetByNcf(string eNcf, CancellationToken cancellationToken)
        {
            // Sin ClientId no hay forma de acotar la consulta: ocurre con una sesión JWT de la
            // web, que no pasa por la API key. Nunca se consulta "sin cliente".
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    found = false,
                    message = "Esta consulta requiere una API key válida en el header 'X-Api-Key'."
                });
            }

            var normalized = EcfLookupLogic.NormalizeNcf(eNcf);

            if (normalized is null)
            {
                return BadRequest(new
                {
                    found = false,
                    message = "El eNCF no es válido: debe ser 'E' seguido de 12 dígitos (por ejemplo E310000000001)."
                });
            }

            var result = await _ecfLookupService.FindByNcfAsync(clientId, normalized, cancellationToken);

            if (result is null)
            {
                return NotFound(new
                {
                    found = false,
                    message = "La plataforma no tiene registrado ese eNCF."
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// Lista los e-CF que terceros le enviaron al cliente dueño de la API key. Pensado para
        /// consultarlo cada cierto tiempo: guarde el <c>receivedAtUtc</c> del último ítem y úselo
        /// como <c>since</c> en la próxima consulta. Se conservan 30 días.
        /// </summary>
        [HttpGet("received")]
        [ProducesResponseType(typeof(ReceivedEcfListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetReceived(
            [FromQuery] string? since,
            [FromQuery] int? limit,
            CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return ReceivedRequiresApiKey();
            }

            if (!ReceivedEcfLogic.TryParseSince(since, out var sinceUtc))
            {
                return BadRequest(new
                {
                    found = false,
                    message = "El parámetro 'since' no es una fecha válida. Use el formato ISO 8601, por ejemplo 2026-10-14T12:30:00Z."
                });
            }

            var result = await _receivedEcfService.ListAsync(
                clientId, sinceUtc, ReceivedEcfLogic.ClampLimit(limit), cancellationToken);

            return Ok(result);
        }

        /// <summary>Descarga el PDF de un e-CF recibido, identificado por su eNCF.</summary>
        [HttpGet("received/{eNcf}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/pdf")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> GetReceivedPdf(
            string eNcf,
            [FromQuery] string? rncEmisor,
            CancellationToken cancellationToken)
        {
            var (error, message) = await ResolveReceivedAsync(eNcf, rncEmisor, cancellationToken);

            if (error is not null)
            {
                return error;
            }

            try
            {
                var environment = ReceivedEcfLogic.ResolveEnvironment(_configuration["EcfXmlValidation:TargetDgiiEnvironment"]);
                var pdf = ReceivedEcfPdf.Render(message!.RawXml, environment);

                return File(pdf, "application/pdf", $"ECF_{message.ENcf}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo generar el PDF del e-CF recibido {ENcf}.", message!.ENcf);
                return StatusCode(StatusCodes.Status422UnprocessableEntity, new
                {
                    found = true,
                    message = "El XML recibido no se pudo interpretar para generar el PDF. Descargue el XML original."
                });
            }
        }

        /// <summary>Descarga el XML original de un e-CF recibido, identificado por su eNCF.</summary>
        [HttpGet("received/{eNcf}/xml")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/xml")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> GetReceivedXml(
            string eNcf,
            [FromQuery] string? rncEmisor,
            CancellationToken cancellationToken)
        {
            var (error, message) = await ResolveReceivedAsync(eNcf, rncEmisor, cancellationToken);

            return error ?? File(Encoding.UTF8.GetBytes(message!.RawXml), "application/xml", $"ECF_{message.ENcf}.xml");
        }

        private IActionResult ReceivedRequiresApiKey() =>
            StatusCode(StatusCodes.Status403Forbidden, new
            {
                found = false,
                message = "Esta consulta requiere una API key válida en el header 'X-Api-Key'."
            });

        /// <summary>
        /// Valida el eNCF y busca el recibido del cliente. Devuelve la respuesta de error lista
        /// (403, 400, 404 o 409) o el mensaje encontrado.
        /// </summary>
        private async Task<(IActionResult? Error, ReceivedB2BMessage? Message)> ResolveReceivedAsync(
            string eNcf,
            string? rncEmisor,
            CancellationToken cancellationToken)
        {
            if (HttpContext.Items["ClientId"] is not int clientId)
            {
                return (ReceivedRequiresApiKey(), null);
            }

            var normalized = EcfLookupLogic.NormalizeNcf(eNcf);

            if (normalized is null)
            {
                return (BadRequest(new
                {
                    found = false,
                    message = "El eNCF no es válido: debe ser 'E' seguido de 12 dígitos (por ejemplo E310000000001)."
                }), null);
            }

            var pick = await _receivedEcfService.FindAsync(clientId, normalized, rncEmisor, cancellationToken);

            return pick.Kind switch
            {
                ReceivedEcfPickKind.NotFound => (NotFound(new
                {
                    found = false,
                    message = "No hay un e-CF recibido con ese eNCF. Los recibidos se conservan 30 días."
                }), null),
                ReceivedEcfPickKind.Ambiguous => (Conflict(new
                {
                    found = true,
                    message = "Hay recibidos con ese eNCF de más de un emisor. Indique el RNC del emisor con el parámetro 'rncEmisor'.",
                    emisores = pick.Emisores
                }), null),
                _ => (null, pick.Message)
            };
        }

        /// <summary>
        /// Endpoint útil para ver un ejemplo vacío de la estructura que espera el DTO.
        /// </summary>
        [HttpGet("sample")]
        public ActionResult<EcfInvoiceRequestDto> GetSample()
        {
            return Ok(new EcfInvoiceRequestDto
            {
                ExternalReference = "INV-001",
                ECF = new EcfRequest
                {
                    Encabezado = new EcfEncabezadoRequest
                    {
                        IdDoc = new EcfIdDocRequest
                        {
                            eNCF = "E310000000001",
                            FechaVencimientoSecuencia = DateTime.UtcNow.AddYears(1).ToString("dd-MM-yyyy"),
                            TipoIngresos = "01"
                        },
                        Emisor = new EcfEmisorRequest
                        {
                            RNCEmisor = "101000001",
                            RazonSocialEmisor = "EMPRESA DE PRUEBA SAS",
                            DireccionEmisor = "AV. PRINCIPAL 123",
                            FechaEmision = DateTime.UtcNow.ToString("dd-MM-yyyy")
                        },
                        Comprador = new EcfCompradorRequest
                        {
                            RNCComprador = "101000002",
                            RazonSocialComprador = "CLIENTE DE PRUEBA"
                        }
                    },
                    DetallesItems = new EcfDetallesItemsRequest
                    {
                        Item = new List<EcfItemRequestDto>
                        {
                            new EcfItemRequestDto
                            {
                                NombreItem = "PRODUCTO DE PRUEBA",
                                CantidadItem = 1,
                                PrecioUnitarioItem = 100,
                                MontoItem = 100
                            }
                        }
                    }
                }
            });
        }
    }
}
