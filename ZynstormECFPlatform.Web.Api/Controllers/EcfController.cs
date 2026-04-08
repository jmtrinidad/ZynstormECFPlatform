using Microsoft.AspNetCore.Mvc;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Web.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
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
                var ecfType = NcfHelper.ExtractEcfType(dto.Ncf);

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
                Ncf = "E310000000001",
                ExternalReference = "INV-001",
                IssueDate = DateTime.UtcNow,
                SequenceExpirationDate = DateTime.UtcNow.AddYears(1),
                IssuerRnc = "101000001",
                IssuerName = "EMPRESA DE PRUEBA SAS",
                IssuerAddress = "AV. PRINCIPAL 123",
                CustomerRnc = "101000002",
                CustomerName = "CLIENTE DE PRUEBA",
                IncomeType = "01",
                Items = new List<EcfItemRequestDto>
                {
                    new EcfItemRequestDto
                    {
                        Name = "PRODUCTO DE PRUEBA",
                        Quantity = 1,
                        UnitPrice = 100,
                        TaxPercentage = 18
                    }
                }
            });
        }
    }
}
