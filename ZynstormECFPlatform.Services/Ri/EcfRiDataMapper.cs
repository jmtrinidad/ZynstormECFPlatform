using System.Globalization;
using System.Xml.Linq;
using ZynstormECFPlatform.Core.Ecf;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Maps a signed e-CF XML document (RFCE or full ECF) into the <see cref="RiData"/>
/// model consumed by the Ri (Representación Impresa) renderer. Ported from
/// MechanicalServ's PreviewController.BuildPreviewModel, adapted to RiData and to
/// build the QR URL via the canonical EcfQrUrlBuilder.
/// </summary>
public static class EcfRiDataMapper
{
    public static RiData Map(string signedXml, DgiiEnvironment environment)
    {
        var doc = XDocument.Parse(signedXml);

        var idDoc = First(doc, "IdDoc");
        var emisor = First(doc, "Emisor");
        var comprador = First(doc, "Comprador");
        var totales = First(doc, "Totales");

        var encf = Value(idDoc, "eNCF");
        var tipoeCF = Value(idDoc, "TipoeCF");
        var montoTotal = DecimalValue(totales, "MontoTotal");
        var fechaEmision = Value(emisor, "FechaEmision");
        var fechaFirma = Value(doc.Root, "FechaHoraFirma");
        var securityCode = GetSecurityCode(doc);
        var rncEmisor = Value(emisor, "RNCEmisor");
        var rncComprador = Value(comprador, "RNCComprador");

        var issuer = new Party
        {
            Name = Value(emisor, "RazonSocialEmisor"),
            Document = rncEmisor,
            Address = Value(emisor, "DireccionEmisor"),
            Phone = FirstNonEmpty(Value(emisor, "TelefonoEmisor"), Value(emisor, "TablaTelefonoEmisor"))
        };

        var buyer = new Party
        {
            Name = Value(comprador, "RazonSocialComprador"),
            Document = FirstNonEmpty(rncComprador, Value(comprador, "IdentificadorExtranjero")),
            Address = Value(comprador, "DireccionComprador"),
            Phone = FirstNonEmpty(Value(comprador, "TelefonoAdicional"), Value(comprador, "TelefonoComprador")),
            Email = NullIfEmpty(Value(comprador, "CorreoComprador")),
            Country = NullIfEmpty(Value(comprador, "PaisComprador"))
        };

        var qrUrl = EcfQrUrlBuilder.Build(
            environment,
            int.Parse(tipoeCF, CultureInfo.InvariantCulture),
            rncEmisor,
            rncComprador,
            encf,
            fechaEmision,
            montoTotal,
            fechaFirma,
            securityCode);

        return new RiData
        {
            Issuer = issuer,
            Buyer = buyer,
            ENcf = encf,
            TipoeCF = tipoeCF,
            FechaEmision = fechaEmision,
            FechaFirma = fechaFirma,
            SecurityCode = securityCode,
            QrUrl = qrUrl,
            Items = BuildItems(doc),
            Totals = new RiTotals
            {
                SubTotal = DecimalValue(totales, "MontoGravadoTotal"),
                Itbis = DecimalValue(totales, "TotalITBIS"),
                Exento = DecimalValue(totales, "MontoExento"),
                Gravado = DecimalValue(totales, "MontoGravadoTotal"),
                Total = montoTotal
            }
        };
    }

    private static List<RiItem> BuildItems(XDocument doc)
    {
        var totales = First(doc, "Totales");
        var itemElements = doc.Descendants().Where(d => d.Name.LocalName == "Item").ToList();

        if (itemElements.Count == 0)
        {
            return BuildConsumerSummaryItems(totales);
        }

        return itemElements.Select(item => new RiItem
        {
            Description = FirstNonEmpty(Value(item, "DescripcionItem"), Value(item, "NombreItem")),
            Quantity = DecimalValue(item, "CantidadItem"),
            Price = DecimalValue(item, "PrecioUnitarioItem"),
            Itbis = DecimalValue(item, "MontoITBIS"),
            Amount = DecimalValue(item, "MontoItem")
        }).ToList();
    }

    private static List<RiItem> BuildConsumerSummaryItems(XElement? totales)
    {
        var itbis = DecimalValue(totales, "TotalITBIS");
        var taxableAmount = FirstNonZero(
            DecimalValue(totales, "MontoGravadoTotal"),
            DecimalValue(totales, "MontoExento"),
            DecimalValue(totales, "MontoTotal") - itbis);

        if (taxableAmount <= 0m && itbis <= 0m)
        {
            return [];
        }

        return
        [
            new RiItem
            {
                Description = "Resumen factura consumo",
                Quantity = 1m,
                Price = taxableAmount,
                Itbis = itbis,
                Amount = taxableAmount
            }
        ];
    }

    private static string GetSecurityCode(XDocument doc)
    {
        var codigoSeguridad = Value(doc.Root, "CodigoSeguridadeCF");
        if (!string.IsNullOrEmpty(codigoSeguridad))
        {
            return codigoSeguridad;
        }

        var signatureValue = Value(doc.Root, "SignatureValue");
        return !string.IsNullOrEmpty(signatureValue) && signatureValue.Length >= 6
            ? signatureValue[..6]
            : signatureValue;
    }

    private static XElement? First(XDocument doc, string name) =>
        doc.Descendants().FirstOrDefault(d => d.Name.LocalName == name);

    private static string Value(XElement? element, string name) =>
        element?.Descendants().FirstOrDefault(d => d.Name.LocalName == name)?.Value.Trim() ?? string.Empty;

    private static decimal DecimalValue(XElement? element, string name) =>
        DecimalValue(Value(element, name));

    private static decimal DecimalValue(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0m;

    private static string FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static decimal FirstNonZero(params decimal[] values) =>
        values.FirstOrDefault(value => value != 0m);

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
