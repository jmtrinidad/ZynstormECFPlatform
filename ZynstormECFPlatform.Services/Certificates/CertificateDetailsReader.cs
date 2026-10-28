using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using ZynstormECFPlatform.Core.Entities;

namespace ZynstormECFPlatform.Services.Certificates;

/// <summary>
/// Extrae del certificado X.509 los datos del titular y del emisor. Cada autoridad (DigiFirma, Avansi, etc.)
/// arma el Subject a su manera, por lo que todos los campos son opcionales.
/// </summary>
public static partial class CertificateDetailsReader
{
    private const string CommonNameOid = "2.5.4.3";
    private const string SurnameOid = "2.5.4.4";
    private const string SerialNumberOid = "2.5.4.5";
    private const string LocalityOid = "2.5.4.7";
    private const string StateOid = "2.5.4.8";
    private const string StreetOid = "2.5.4.9";
    private const string OrganizationOid = "2.5.4.10";
    private const string GivenNameOid = "2.5.4.42";
    private const string OrganizationIdentifierOid = "2.5.4.97";

    public static void Apply(X509Certificate2 certificate, ClientCertificate target)
    {
        var subject = ReadAttributes(certificate.SubjectName);
        var issuer = ReadAttributes(certificate.IssuerName);

        target.Thumbprint = certificate.Thumbprint;
        target.ExpirationDateUtc = certificate.NotAfter.ToUniversalTime();
        target.NotBeforeUtc = certificate.NotBefore.ToUniversalTime();
        target.SerialNumber = Truncate(certificate.SerialNumber, 100);

        var fullName = Get(subject, CommonNameOid)
                       ?? JoinNonEmpty(" ", Get(subject, GivenNameOid), Get(subject, SurnameOid));
        target.SubjectName = Truncate(fullName, 250);
        target.SubjectIdentification = Truncate(StripSemanticsPrefix(Get(subject, SerialNumberOid)), 50);
        target.SubjectEmail = Truncate(NullIfEmpty(certificate.GetNameInfo(X509NameType.EmailName, false)), 250);
        target.SubjectAddress = Truncate(JoinNonEmpty(", ", Get(subject, StreetOid), Get(subject, LocalityOid), Get(subject, StateOid)), 500);
        target.OrganizationName = Truncate(Get(subject, OrganizationOid), 250);
        target.OrganizationIdentifier = Truncate(StripSemanticsPrefix(Get(subject, OrganizationIdentifierOid)), 50);
        target.IssuerName = Truncate(Get(issuer, OrganizationOid) ?? Get(issuer, CommonNameOid), 250);
    }

    private static Dictionary<string, string> ReadAttributes(X500DistinguishedName name)
    {
        var attributes = new Dictionary<string, string>();

        foreach (var rdn in name.EnumerateRelativeDistinguishedNames())
        {
            if (rdn.HasMultipleElements) continue;

            var oid = rdn.GetSingleElementType().Value;
            var value = rdn.GetSingleElementValue();

            if (oid != null && !string.IsNullOrWhiteSpace(value))
                attributes.TryAdd(oid, value.Trim());
        }

        return attributes;
    }

    private static string? Get(Dictionary<string, string> attributes, string oid) =>
        attributes.TryGetValue(oid, out var value) ? value : null;

    /// <summary>
    /// ETSI EN 319 412-1: "IDCDO-00112345678" (cédula) o "VATDO-131234567" (RNC) → solo el número.
    /// </summary>
    private static string? StripSemanticsPrefix(string? value) =>
        value == null ? null : SemanticsPrefixRegex().Replace(value, string.Empty);

    private static string? JoinNonEmpty(string separator, params string?[] parts)
    {
        var joined = string.Join(separator, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return NullIfEmpty(joined);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? Truncate(string? value, int maxLength) =>
        value == null || value.Length <= maxLength ? value : value[..maxLength];

    [GeneratedRegex("^[A-Z]{3}[A-Z]{2}-")]
    private static partial Regex SemanticsPrefixRegex();
}
