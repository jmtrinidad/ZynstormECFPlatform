using System.Threading.Tasks;

namespace ZynstormECFPlatform.Abstractions.Services;

/// <summary>
/// Service responsible for applying XML-DSig digital signatures to e-CF documents.
/// </summary>
public interface IXmlSignatureService
{
    /// <summary>
    /// Applies an XML-DSig signature using SHA-256 and preserveWhitespace=false.
    /// </summary>
    /// <param name="unsignedXml">The XML string to sign.</param>
    /// <param name="certificateBase64">The base64 encoded PKCS12 (.p12 / .pfx) certificate.</param>
    /// <param name="certificatePassword">The password for the certificate.</param>
    /// <returns>The signed XML string.</returns>
    string SignXml(string unsignedXml, string certificateBase64, string certificatePassword);

    /// <summary>
    /// Extracts the value of the SignatureValue element from a signed XML.
    /// </summary>
    string GetSignatureValue(string signedXml);
}
