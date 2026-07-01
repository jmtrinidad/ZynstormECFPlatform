using System.Globalization;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Core.Ecf;

public static class EcfQrUrlBuilder
{
    public static string Build(
        DgiiEnvironment environment, int ecfType, string rncEmisorRaw, string rncCompradorRaw,
        string encf, string fechaEmision, decimal montoTotal, string fechaFirma, string securityCode)
    {
        var rncEmisor = OnlyDigits(rncEmisorRaw);
        var rncComprador = OnlyDigits(rncCompradorRaw);
        var fechaEmisionUrl = !string.IsNullOrWhiteSpace(fechaEmision)
            ? fechaEmision.Split(' ')[0].Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy");
        var fechaFirmaUrl = (!string.IsNullOrWhiteSpace(fechaFirma)
            ? fechaFirma.Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")).Replace(" ", "%20");
        var montoTotalUrl = montoTotal.ToString("0.##", CultureInfo.InvariantCulture);

        if (ecfType == 32 && montoTotal < 250000m)
        {
            string fcBase = environment == DgiiEnvironment.Production
                ? "https://fc.dgii.gov.do/ecf"
                : environment == DgiiEnvironment.Test
                    ? "https://fc.dgii.gov.do/testecf"
                    : "https://fc.dgii.gov.do/CerteCF";
            return $"{fcBase}/ConsultaTimbreFC?RncEmisor={rncEmisor}&ENCF={encf}&MontoTotal={montoTotalUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
        }

        string baseUrl = environment == DgiiEnvironment.Production
            ? "https://ecf.dgii.gov.do/ecf"
            : environment == DgiiEnvironment.Test
                ? "https://ecf.dgii.gov.do/TesteCF"
                : "https://ecf.dgii.gov.do/CerteCF";

        if (string.IsNullOrEmpty(rncComprador))
            return $"{baseUrl}/ConsultaTimbre?RncEmisor={rncEmisor}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";

        return $"{baseUrl}/ConsultaTimbre?RncEmisor={rncEmisor}&RncComprador={rncComprador}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
    }

    public static string OnlyDigits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : new string(value.Where(char.IsDigit).ToArray());
}
