using System.Reflection;
using System.Text;
using System.Xml;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

public class ReceivedEcfProductionService : IReceivedEcfProductionService
{
    private static readonly Assembly SchemasAssembly =
        AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "ZynstormECFPlatform.Schemas")
        ?? Assembly.Load("ZynstormECFPlatform.Schemas");

    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IEncryptedService _encryptedService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly IDgiiAuthService _authService;
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IEcfProductionGeneratorService _generatorService;
    private readonly IXmlSignatureService _signerService;

    public ReceivedEcfProductionService(
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IClientCertificateService clientCertificateService,
        IDgiiAuthService authService,
        IDgiiTransmissionService transmissionService,
        IEcfProductionGeneratorService generatorService,
        IXmlSignatureService signerService)
    {
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _encryptedService = encryptedService;
        _clientCertificateService = clientCertificateService;
        _authService = authService;
        _transmissionService = transmissionService;
        _generatorService = generatorService;
        _signerService = signerService;
    }

    public async Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750)
    {
        var resultDto = new ReceivedEcfEmissionResultDto();

        var dtoErrors = _generatorService.ValidateDto(dto);
        if (dtoErrors.Count > 0)
        {
            resultDto.DtoErrors = dtoErrors;
            resultDto.Message = "Errores de validacion en los datos de entrada.";
            return resultDto;
        }

        var ecfType = int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF ?? NcfHelper.ExtractEcfType(dto.ECF.Encabezado.IdDoc.eNCF).ToString());
        resultDto.EcfType = ecfType;
        resultDto.ENcf = dto.ECF.Encabezado.IdDoc.eNCF;

        var issuerRnc = dto.ECF.Encabezado.Emisor.RNCEmisor;
        var eNcf = dto.ECF.Encabezado.IdDoc.eNCF;

        var client = await _clientService.GetByAsync(c => c.Rnc == issuerRnc)
            ?? throw new Exception($"Cliente con RNC {issuerRnc} no encontrado.");

        dto.SignatureDateOverride ??= DateTime.Now.ToDrTime();
        dto.SequenceExpirationDate ??= new DateTime(DateTime.Now.Year + 2, 12, 31);
        dto.ECF.Encabezado.IdDoc.TipoIngresos ??= "01";
        dto.ECF.Encabezado.IdDoc.TipoPago ??= "1";

        var unsignedXml = _generatorService.GenerateUnsignedXml(dto, isSummary: false);
        resultDto.UnsignedXml = unsignedXml;

        var xsdErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXml, ecfType);
        resultDto.XsdErrors = xsdErrors;
        if (xsdErrors.Count > 0)
        {
            resultDto.Message = "El XML generado no cumple con el esquema XSD de la DGII.";
            return resultDto;
        }

        var xmlProdErrors = ValidateXmlAgainstProdReferences(unsignedXml, ecfType);
        resultDto.XmlProdErrors = xmlProdErrors;
        if (xmlProdErrors.Count > 0)
        {
            resultDto.Message = "El XML generado no cumple con las referencias estructurales de XmlProd.";
            return resultDto;
        }

        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("ApiKey no encontrada.");
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("Certificado no encontrado.");
        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
        var certBase64 = Convert.ToBase64String(certificateBytes);
        var certPass = Encoding.UTF8.GetString(passwordBytes);

        var signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);
        resultDto.SignedXml = signedXml;

        var token = await _authService.GetTokenAsync(issuerRnc, environment, certBase64, certPass);
        var total = CalculateTransmissionTotal(dto);
        var transmission = await _transmissionService.SendEcfAsync(environment, token, signedXml, ecfType, total, issuerRnc, eNcf, isSummary: false);
        resultDto.Transmission = transmission;
        resultDto.TrackId = transmission.TrackId;

        if (!transmission.Success)
        {
            resultDto.Message = BuildDgiiTransmissionError(transmission);
            return resultDto;
        }

        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            await Task.Delay(Math.Max(statusDelayMilliseconds, 0));
            var status = await _transmissionService.GetStatusAsync(environment, token, transmission.TrackId);
            resultDto.Status = status;
            resultDto.Success = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase);
            resultDto.Message = resultDto.Success ? $"TrackId: {transmission.TrackId}" : BuildDgiiStatusError(status);
            return resultDto;
        }

        resultDto.Success = string.Equals(transmission.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || transmission.Codigo is 0 or 1;
        resultDto.Message = resultDto.Success ? $"DGII: {transmission.Estado ?? "Aceptado"}" : BuildDgiiTransmissionError(transmission);
        return resultDto;
    }

    private static List<string> ValidateXmlAgainstProdReferences(string xml, int ecfType)
    {
        var errors = new List<string>();
        try
        {
            var generatedDoc = new XmlDocument();
            generatedDoc.LoadXml(xml);

            var referenceResources = SchemasAssembly.GetManifestResourceNames()
                .Where(r => r.Contains(".XmlProd.", StringComparison.OrdinalIgnoreCase) &&
                            r.Contains($"E{ecfType}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r)
                .ToList();

            if (referenceResources.Count == 0)
            {
                errors.Add($"No se encontraron XML de referencia en XmlProd para TipoeCF {ecfType}.");
                return errors;
            }

            var bestErrors = new List<string>();
            var bestResource = referenceResources[0];
            var bestErrorCount = int.MaxValue;

            foreach (var resourceName in referenceResources)
            {
                using var stream = SchemasAssembly.GetManifestResourceStream(resourceName);
                if (stream == null) continue;

                var referenceDoc = new XmlDocument();
                referenceDoc.Load(stream);

                var referenceErrors = CompareXmlStructure(generatedDoc, referenceDoc);
                if (referenceErrors.Count == 0)
                    return errors;

                if (referenceErrors.Count < bestErrorCount)
                {
                    bestErrorCount = referenceErrors.Count;
                    bestErrors = referenceErrors;
                    bestResource = resourceName;
                }
            }

            errors.Add($"El XML generado no coincide con la estructura de referencia XmlProd para TipoeCF {ecfType}. Referencia mas cercana: {bestResource}.");
            errors.AddRange(bestErrors);
        }
        catch (Exception ex)
        {
            errors.Add($"Error validando contra XmlProd: {ex.Message}");
        }

        return errors;
    }

    private static List<string> CompareXmlStructure(XmlDocument generatedDoc, XmlDocument referenceDoc)
    {
        var errors = new List<string>();
        var referencePaths = GetUniqueElementPaths(referenceDoc);
        var generatedPaths = GetUniqueElementPaths(generatedDoc);

        var skippablePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ECF/Encabezado/Totales/MontoGravadoTotal",
            "ECF/Encabezado/Totales/MontoGravadoI1",
            "ECF/Encabezado/Totales/MontoGravadoI2",
            "ECF/Encabezado/Totales/MontoGravadoI3",
            "ECF/Encabezado/Totales/MontoExento",
            "ECF/Encabezado/Totales/TotalITBIS",
            "ECF/Encabezado/Totales/TotalITBIS1",
            "ECF/Encabezado/Totales/TotalITBIS2",
            "ECF/Encabezado/Totales/TotalITBIS3",
            "ECF/Encabezado/Totales/MontoTotal",
            "ECF/Encabezado/Totales/MontoNoFacturable",
            "ECF/Encabezado/Totales/MontoPeriodo",
            "ECF/Encabezado/Totales/ValorPagar",
            "ECF/Encabezado/Totales/TotalITBISRetenido",
            "ECF/Encabezado/Totales/TotalISRRetencion",
            "ECF/Encabezado/IdDoc/IndicadorMontoGravado",
            "ECF/Encabezado/IdDoc/TipoPago",
            "ECF/Encabezado/IdDoc/FechaLimitePago",
            "ECF/Encabezado/IdDoc/TerminoPago",
            "RFCE/Encabezado/Totales/MontoGravadoTotal",
            "RFCE/Encabezado/Totales/MontoExento",
            "RFCE/Encabezado/Totales/TotalITBIS",
            "RFCE/Encabezado/Totales/MontoTotal",
            "RFCE/Encabezado/Totales/MontoNoFacturable",
            "RFCE/Encabezado/Totales/MontoPeriodo",
            "RFCE/Encabezado/CodigoSeguridadeCF"
        };

        foreach (var path in referencePaths)
        {
            if (path.Contains("Signature") || path.Contains("FechaHoraFirma")) continue;
            if (skippablePaths.Contains(path)) continue;

            if (!generatedPaths.Contains(path))
                errors.Add($"Elemento '{path}' (presente en referencia XmlProd) no se encuentra en el XML generado.");
        }

        return errors;
    }

    private static HashSet<string> GetUniqueElementPaths(XmlDocument doc)
    {
        var paths = new HashSet<string>();
        if (doc.DocumentElement != null)
            GetPathsRecursive(doc.DocumentElement, "", paths);
        return paths;
    }

    private static void GetPathsRecursive(XmlElement element, string currentPath, HashSet<string> paths)
    {
        var path = string.IsNullOrEmpty(currentPath) ? element.Name : $"{currentPath}/{element.Name}";
        paths.Add(path);
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement)
                GetPathsRecursive(childElement, path, paths);
        }
    }

    private static decimal CalculateTransmissionTotal(EcfInvoiceRequestDto dto)
    {
        return dto.ECF.Encabezado.Totales.MontoTotal
            ?? dto.ECF.DetallesItems.Item.Sum(item => item.MontoItem);
    }

    private static string BuildDgiiStatusError(DgiiStatusResponse status)
    {
        if (status.Mensajes == null || !status.Mensajes.Any()) return $"DGII: {status.Estado}";
        return $"DGII: {status.Estado} | {string.Join(" | ", status.Mensajes.Select(m => m.Valor))}";
    }

    private static string BuildDgiiTransmissionError(DgiiTransmissionResult result)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.Estado)) parts.Add($"DGII: {result.Estado}");
        if (!string.IsNullOrWhiteSpace(result.Error)) parts.Add(result.Error);
        if (!string.IsNullOrWhiteSpace(result.Mensaje)) parts.Add(result.Mensaje);
        if (result.Mensajes != null && result.Mensajes.Any())
            parts.AddRange(result.Mensajes.Where(m => !string.IsNullOrWhiteSpace(m.Valor)).Select(m => m.Valor));

        return parts.Count == 0 ? "Error en transmision" : string.Join(" | ", parts);
    }
}
