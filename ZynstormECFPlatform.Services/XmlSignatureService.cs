using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services;

public class XmlSignatureService : IXmlSignatureService
{
    public string SignXml(string unsignedXml, string certificateBase64, string certificatePassword)
    {
        // 1. Load the certificate
        var certBytes = Convert.FromBase64String(certificateBase64);
        using var cert = new X509Certificate2(certBytes, certificatePassword, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);

        // 2. Load XML Document
        var doc = new XmlDocument
        {
            PreserveWhitespace = false // Required by DGII
        };
        doc.LoadXml(unsignedXml);

        // Remove the placeholder Signature block if it exists (added just for XSD validation)
        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
        var placeholderNode = doc.SelectSingleNode("//ds:Signature", nsmgr);
        if (placeholderNode != null && placeholderNode.ParentNode != null)
        {
            placeholderNode.ParentNode.RemoveChild(placeholderNode);
        }

        // 3. Create SignedXml object
        var signedXml = new SignedXml(doc)
        {
            SigningKey = cert.GetRSAPrivateKey()
        };

        // 4. Create Reference to root element
        var reference = new Reference { Uri = "" };

        // Add EnvelopedSignatureTransform
        var env = new XmlDsigEnvelopedSignatureTransform();
        reference.AddTransform(env);

        // DGII requires SHA-256
        reference.DigestMethod = "http://www.w3.org/2001/04/xmlenc#sha256";
        signedXml.AddReference(reference);

        // 5. Add KeyInfo
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signedXml.KeyInfo = keyInfo;

        // DGII requires RSA-SHA256 signature method
        signedXml.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

        // 6. Compute signature
        signedXml.ComputeSignature();

        // 7. Append signature to standard root element
        var xmlDigitalSignature = signedXml.GetXml();
        doc.DocumentElement?.AppendChild(doc.ImportNode(xmlDigitalSignature, true));

        // 8. Return signed XML
        return doc.OuterXml;
    }

    public string GetSignatureValue(string signedXml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(signedXml);

        var nsmgr = new XmlNamespaceManager(doc.NameTable);
        nsmgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        var node = doc.SelectSingleNode("//ds:SignatureValue", nsmgr);
        return node?.InnerText.Trim() ?? string.Empty;
    }
}
