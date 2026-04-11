using System.Threading.Tasks;

namespace ZynstormECFPlatform.Abstractions.Services;

public interface IInboundEcfService
{
    /// <summary>
    /// Processes an incoming e-CF document XML string.
    /// Returns the assigned trackId to acknowledge reception.
    /// </summary>
    Task<string> ReceiveEcfAsync(string xmlContent);

    /// <summary>
    /// Processes an Aprobación Comercial XML for an e-CF we issued previously.
    /// Updates the corresponding document's status.
    /// </summary>
    Task ProcessCommercialApprovalAsync(string xmlContent);
}
