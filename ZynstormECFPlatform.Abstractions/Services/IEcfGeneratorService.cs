using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEcfGeneratorService
{
    /// <summary>
    /// Generates the raw XML string for an e-CF document from the simplified DTO.
    /// </summary>
    /// <param name="dto">Universal invoice data.</param>
    /// <returns>Unsigned XML string compliant with DGII e-CF schemas.</returns>
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto);

    /// <summary>
    /// Validates the structure of the data against basic DGII rules before generation.
    /// </summary>
    /// <param name="dto">Universal invoice data.</param>
    /// <returns>List of validation errors, if any.</returns>
    List<string> ValidateDto(EcfInvoiceRequestDto dto);
}
