using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Signing;

/// <summary>
/// Firma real de un e-CF con un certificado de prueba fijo (Signing/Fixtures/test-signing.pfx, solo para tests).
/// RSA-SHA256 con PKCS#1 v1.5 es determinista: mismo XML y misma clave producen los mismos bytes, así que la
/// comparación con el archivo de referencia detecta cualquier cambio en la firma que recibe la DGII
/// (por ejemplo, al actualizar System.Security.Cryptography.Xml).
/// Para regenerar la referencia a propósito: RECORD_SIGNATURE_GOLDEN=1 dotnet test --filter XmlSignatureServiceTests
/// </summary>
public class XmlSignatureServiceTests
{
    private const string CertificatePassword = "test-only";
    private const string DsNamespace = "http://www.w3.org/2000/09/xmldsig#";
    private const string GoldenFileName = "E310000000402.signed-golden.xml";

    private static string FixturePath(params string[] parts) =>
        Path.Combine([AppContext.BaseDirectory, .. parts]);

    private static string CertificateBase64 =>
        Convert.ToBase64String(File.ReadAllBytes(FixturePath("Signing", "Fixtures", "test-signing.pfx")));

    private static string UnsignedEcf => File.ReadAllText(FixturePath("Ri", "Fixtures", "Paso_1_E310000000402.xml"));

    private static string Sign() => new XmlSignatureService().SignXml(UnsignedEcf, CertificateBase64, CertificatePassword);

    private static XmlDocument Load(string xml)
    {
        var doc = new XmlDocument { PreserveWhitespace = false };
        doc.LoadXml(xml);
        return doc;
    }

    private static XmlNamespaceManager Ns(XmlDocument doc)
    {
        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("ds", DsNamespace);
        return ns;
    }

    [Fact]
    public void SignXml_ProducesSignatureThatValidatesWithTheCertificate()
    {
        var doc = Load(Sign());
        var signatureNode = (XmlElement)doc.GetElementsByTagName("Signature", DsNamespace)[0]!;

        var signedXml = new SignedXml(doc);
        signedXml.LoadXml(signatureNode);

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(
            FixturePath("Signing", "Fixtures", "test-signing.pfx"), CertificatePassword);

        Assert.True(signedXml.CheckSignature(certificate, verifySignatureOnly: true));
    }

    [Fact]
    public void SignXml_UsesTheAlgorithmsRequiredByDgii()
    {
        var doc = Load(Sign());
        var ns = Ns(doc);

        Assert.Equal("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256",
            doc.SelectSingleNode("//ds:SignedInfo/ds:SignatureMethod/@Algorithm", ns)?.Value);
        Assert.Equal("http://www.w3.org/2001/04/xmlenc#sha256",
            doc.SelectSingleNode("//ds:Reference/ds:DigestMethod/@Algorithm", ns)?.Value);
        Assert.Equal("", doc.SelectSingleNode("//ds:Reference/@URI", ns)?.Value);
        Assert.Equal("http://www.w3.org/2000/09/xmldsig#enveloped-signature",
            doc.SelectSingleNode("//ds:Reference/ds:Transforms/ds:Transform/@Algorithm", ns)?.Value);
        Assert.NotNull(doc.SelectSingleNode("//ds:KeyInfo/ds:X509Data/ds:X509Certificate", ns));
    }

    [Fact]
    public void SignXml_ReplacesPlaceholderSignature_AndAppendsItToTheRoot()
    {
        var doc = Load(Sign());
        var signatures = doc.GetElementsByTagName("Signature", DsNamespace);

        Assert.Equal(1, signatures.Count);
        Assert.Same(doc.DocumentElement, signatures[0]!.ParentNode);
        Assert.Same(doc.DocumentElement!.LastChild, signatures[0]);
    }

    [Fact]
    public void SignXml_OutputIsByteForByteEqualToTheGoldenReference()
    {
        var signed = Sign();

        if (Environment.GetEnvironmentVariable("RECORD_SIGNATURE_GOLDEN") == "1")
        {
            var sourceDir = FindTestsProjectDirectory();
            File.WriteAllText(Path.Combine(sourceDir, "Signing", "Fixtures", GoldenFileName), signed);
            File.WriteAllText(FixturePath("Signing", "Fixtures", GoldenFileName), signed);
        }

        var golden = File.ReadAllText(FixturePath("Signing", "Fixtures", GoldenFileName));

        Assert.Equal(golden, signed);
    }

    [Fact]
    public void GetSignatureValue_ReturnsTheComputedSignature()
    {
        var service = new XmlSignatureService();
        var signed = Sign();
        var doc = Load(signed);

        var expected = doc.SelectSingleNode("//ds:SignatureValue", Ns(doc))!.InnerText.Trim();

        Assert.False(string.IsNullOrEmpty(expected));
        Assert.Equal(expected, service.GetSignatureValue(signed));
    }

    private static string FindTestsProjectDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ZynstormECFPlatform.Tests.csproj")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("No se encontró el proyecto de tests.");
    }
}
