using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IEcfGeneratorService
{
    /// <summary>
    /// Generates a fully structured, unsigned XML string compliant with the DGII e-CF XSD schema
    /// corresponding to the TipoeCF derived from the NCF field in the DTO.
    /// </summary>
    /// <param name="dto">Invoice data including issuer, buyer, items and totals.</param>
    /// <returns>Unsigned XML string ready for digital signature.</returns>
    string GenerateUnsignedXml(EcfInvoiceRequestDto dto);

    /// <summary>
    /// Validates the generated XML string against the DGII XSD schema that matches
    /// the TipoeCF encoded in the NCF (e.g. e-CF 31 v.1.0.xsd for NCF starting with E31).
    /// </summary>
    /// <param name="xml">The XML string to validate.</param>
    /// <param name="ecfType">The TipoeCF code (e.g., 31, 32, 33).</param>
    /// <returns>List of XSD validation errors. Empty list means the XML is valid.</returns>
    List<string> ValidateXmlAgainstSchema(string xml, int ecfType);

    /// <summary>
    /// Validates the structure of the DTO against basic DGII business rules before generation.
    /// </summary>
    /// <param name="dto">Invoice data to validate.</param>
    /// <returns>List of validation errors. Empty list means all checks passed.</returns>
    List<string> ValidateDto(EcfInvoiceRequestDto dto);
}
