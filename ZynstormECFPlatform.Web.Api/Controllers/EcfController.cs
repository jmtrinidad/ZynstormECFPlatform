using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Web.Api.Filters;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiKeyAuth]
    public class EcfController(IEcfGeneratorService ecfGeneratorService) : ControllerBase
    {
        private readonly IEcfGeneratorService _ecfGeneratorService = ecfGeneratorService;

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
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Error inesperado durante la generación del XML.",
                    detail = ex.Message
                });
            }
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
