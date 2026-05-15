using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Standalone service that validates raw XML e-CF documents against DGII rules.
/// Completely decoupled from generation/transmission services.
/// </summary>
public interface IEcfXmlValidationService
{
    /// <summary>
    /// Validates a raw XML string through 4 layers: structural, XSD, business rules, arithmetic.
    /// If the XML passes all validations, it is cached and a verification info object is populated.
    /// </summary>
    EcfXmlValidationResult Validate(string xml);

    /// <summary>
    /// Retrieves cached verification info for a previously validated e-NCF.
    /// Returns null if the eNCF was never validated or has expired from cache.
    /// </summary>
    EcfVerificacionInfo? GetVerificacion(string eNcf);

    /// <summary>
    /// Returns all currently cached verification entries.
    /// </summary>
    List<EcfVerificacionInfo> GetAllVerificaciones();
}
