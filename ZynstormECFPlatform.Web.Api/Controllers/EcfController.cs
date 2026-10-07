using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Production;
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
        ICacheService cacheService,
        ILogger<EcfController> logger) : ControllerBase
    {
        private readonly IEcfProductionGeneratorService _ecfGeneratorService = ecfGeneratorService;
        private readonly IReceivedEcfProductionService _receivedEcfProductionService = receivedEcfProductionService;
        private readonly IEcfLookupService _ecfLookupService = ecfLookupService;
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

                // Un replay pendiente no es un fallo de la plataforma: devolvió lo guardado.
                if (result.IsPending && !result.Replayed)
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
