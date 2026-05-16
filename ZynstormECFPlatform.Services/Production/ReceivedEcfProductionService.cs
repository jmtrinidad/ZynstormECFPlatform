using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using QRCoder;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Jobs;

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
    private readonly IClientBrancheService _clientBrancheService;
    private readonly ICurrencyService _currencyService;
    private readonly IEcfTypeService _ecfTypeService;
    private readonly IEcfDocumentService _ecfDocumentService;
    private readonly IEcfXmlDocumentService _ecfXmlDocumentService;
    private readonly IEcfTransmissionService _ecfTransmissionService;
    private readonly IEcfStatusHistoryService _ecfStatusHistoryService;
    private readonly ISystemLogService _systemLogService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    public ReceivedEcfProductionService(
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IClientCertificateService clientCertificateService,
        IDgiiAuthService authService,
        IDgiiTransmissionService transmissionService,
        IEcfProductionGeneratorService generatorService,
        IXmlSignatureService signerService,
        IClientBrancheService clientBrancheService,
        ICurrencyService currencyService,
        IEcfTypeService ecfTypeService,
        IEcfDocumentService ecfDocumentService,
        IEcfXmlDocumentService ecfXmlDocumentService,
        IEcfTransmissionService ecfTransmissionService,
        IEcfStatusHistoryService ecfStatusHistoryService,
        ISystemLogService systemLogService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment)
    {
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _encryptedService = encryptedService;
        _clientCertificateService = clientCertificateService;
        _authService = authService;
        _transmissionService = transmissionService;
        _generatorService = generatorService;
        _signerService = signerService;
        _clientBrancheService = clientBrancheService;
        _currencyService = currencyService;
        _ecfTypeService = ecfTypeService;
        _ecfDocumentService = ecfDocumentService;
        _ecfXmlDocumentService = ecfXmlDocumentService;
        _ecfTransmissionService = ecfTransmissionService;
        _ecfStatusHistoryService = ecfStatusHistoryService;
        _systemLogService = systemLogService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
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
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("ApiKey no encontrada.");
        var clientBranch = await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
            ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception($"El cliente con RNC {issuerRnc} no tiene sucursal configurada.");
        var currency = await _currencyService.GetByAsync(x => x.Code == "DOP")
            ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0)
            ?? throw new Exception("No hay moneda configurada para registrar el e-CF.");
        var ecfTypeEntity = await _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString())
            ?? throw new Exception($"TipoeCF {ecfType} no esta configurado.");

        dto.SignatureDateOverride ??= DateTime.Now.ToDrTime();
        dto.SequenceExpirationDate ??= new DateTime(DateTime.Now.Year + 2, 12, 31);
        dto.SecurityCodeOverride ??= GenerateSecurityCode();
        dto.ECF.Encabezado.IdDoc.TipoIngresos ??= "01";
        dto.ECF.Encabezado.IdDoc.TipoPago ??= "1";
        resultDto.SecurityCode = dto.SecurityCodeOverride;
        resultDto.SignatureDate = dto.ECF.FechaHoraFirma ?? dto.SignatureDateOverride.Value.ToString("dd-MM-yyyy HH:mm:ss");

        var ecfDocument = await CreateEcfDocumentAsync(dto, client, clientBranch, apiKey, currency, ecfTypeEntity);
        resultDto.EcfDocumentId = ecfDocument.EcfDocumentId;
        await AddHistoryAsync(ecfDocument, 2, "Iniciando validacion y generacion del XML.");
        await AddLogAsync(ecfDocument, client.ClientId, "Information", "Proceso de emision e-CF iniciado.");

        var unsignedXml = _generatorService.GenerateUnsignedXml(dto, isSummary: false);
        resultDto.UnsignedXml = unsignedXml;

        var xsdErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXml, ecfType);
        resultDto.XsdErrors = xsdErrors;
        if (xsdErrors.Count > 0)
        {
            resultDto.Message = "El XML generado no cumple con el esquema XSD de la DGII.";
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message, JsonSerializer.Serialize(xsdErrors));
            return resultDto;
        }

        var xmlProdErrors = ValidateXmlAgainstProdReferences(unsignedXml, ecfType);
        resultDto.XmlProdErrors = xmlProdErrors;

        if (xmlProdErrors.Count > 0)
        {
            resultDto.Message = "El XML generado no cumple con las referencias estructurales de XmlProd.";
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message, JsonSerializer.Serialize(xmlProdErrors));
            return resultDto;
        }

        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey ?? string.Empty);
        var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
            ?? throw new Exception("Certificado no encontrado.");
        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
        var certBase64 = Convert.ToBase64String(certificateBytes);
        var certPass = Encoding.UTF8.GetString(passwordBytes);

        var signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);
        resultDto.SignedXml = signedXml;
        ApplyQrMetadata(resultDto, dto, signedXml, ecfType);
        resultDto.XmlValidation = BuildAcceptedValidationResult(dto, signedXml, ecfType, resultDto.QrUrl, resultDto.SecurityCode, resultDto.SignatureDate, resultDto.QrImageUrl);

        await _ecfXmlDocumentService.InsertAsync(new EcfXmlDocument
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            XmlUnsigned = unsignedXml,
            XmlSigned = signedXml
        });
        await MarkDocumentAsync(ecfDocument, 6, "XML firmado y guardado.");

        if (ShouldUseStagingXmlValidation())
        {
            return await ProcessWithStagingValidationAsync(resultDto, ecfDocument, client.ClientId, signedXml);
        }

        var token = await _authService.GetTokenAsync(issuerRnc, environment, certBase64, certPass);
        var total = CalculateTransmissionTotal(dto);

        await MarkDocumentAsync(ecfDocument, 8, "Enviando e-CF a DGII.");

        var transmission = await _transmissionService.SendEcfAsync(environment, token, signedXml, ecfType, total, issuerRnc, eNcf, isSummary: false);

        resultDto.Transmission = transmission;
        resultDto.TrackId = transmission.TrackId;

        if (!transmission.Success)
        {
            resultDto.Message = BuildDgiiTransmissionError(transmission);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 12, signedXml);
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Error", resultDto.Message, JsonSerializer.Serialize(transmission));
            return resultDto;
        }

        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 9, signedXml);
            await MarkDocumentAsync(ecfDocument, 9, $"DGII recibio el e-CF. TrackId: {transmission.TrackId}");
            await Task.Delay(Math.Max(statusDelayMilliseconds, 0));
            var status = await _transmissionService.GetStatusAsync(environment, token, transmission.TrackId);
            resultDto.Status = status;
            resultDto.Success = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase);
            var statusId = MapDgiiStatusToEcfStatus(status);
            resultDto.Message = resultDto.Success ? $"TrackId: {transmission.TrackId}" : BuildDgiiStatusError(status);

            if (resultDto.Success)
                await AddLogAsync(ecfDocument, client.ClientId, "Information", $"e-CF aprobado. TrackId: {transmission.TrackId}. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");

            await MarkDocumentAsync(ecfDocument, statusId, resultDto.Message);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId, signedXml, status);
            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"Consulta DGII TrackId {transmission.TrackId}: {status.Estado}", JsonSerializer.Serialize(status));

            if (IsPendingDgiiStatus(status))
            {
                resultDto.IsPending = true;
                resultDto.Success = false;
                resultDto.Message = $"DGII aun procesa el e-CF. TrackId: {transmission.TrackId}";

                var jobId = BackgroundJob.Schedule<EcfTrackingJob>(
                    j => j.Execute(transmission.TrackId, environment, issuerRnc, certBase64, certPass, ecfDocument.EcfDocumentId, 1),
                    TimeSpan.FromSeconds(3));

                ecfDocument.HangfireJobId = jobId;
                await _ecfDocumentService.UpdateAsync(ecfDocument);
                resultDto.HangfireJobId = jobId;
                await AddLogAsync(ecfDocument, client.ClientId, "Information", $"DGII no retorno aceptacion inmediata. Job de seguimiento programado: {jobId}.");
            }

            return resultDto;
        }

        resultDto.Success = string.Equals(transmission.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || transmission.Codigo is 0 or 1;
        resultDto.Message = resultDto.Success ? $"DGII: {transmission.Estado ?? "Aceptado"}" : BuildDgiiTransmissionError(transmission);
        var finalStatusId = resultDto.Success ? 10 : 12;

        await SaveTransmissionAsync(ecfDocument, transmission, finalStatusId, signedXml);
        await MarkDocumentAsync(ecfDocument, finalStatusId, resultDto.Message);
        await AddLogAsync(ecfDocument, client.ClientId, resultDto.Success ? "Information" : "Error", resultDto.Message, JsonSerializer.Serialize(transmission));

        if (resultDto.Success)
            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"e-CF aprobado. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");

        return resultDto;
    }

    private async Task<ReceivedEcfEmissionResultDto> ProcessWithStagingValidationAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        string signedXml)
    {
        var validationUrl = _configuration["EcfXmlValidation:DevStagingUrl"]
            ?? "https://ecfstaging.zynstorm.com/api/v1/EcfXmlValidation/validate";

        await AddLogAsync(ecfDocument, clientId, "Information", $"Ambiente {_hostEnvironment.EnvironmentName}: enviando XML al validador interno {validationUrl}.");

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var content = new StringContent(signedXml, Encoding.UTF8, "application/xml");
            using var response = await client.PostAsync(validationUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            var validation = DeserializeValidationResult(responseBody);

            resultDto.XmlValidation = validation;

            if (validation?.IsValid == true)
            {
                resultDto.Success = true;
                resultDto.IsPending = false;
                resultDto.TrackId = $"VAL-{Guid.NewGuid():N}";
                resultDto.Message = $"XML validado correctamente en {_hostEnvironment.EnvironmentName}. TrackId: {resultDto.TrackId}";

                if (validation.Verificacion != null)
                {
                    resultDto.SecurityCode = validation.Verificacion.CodigoSeguridad ?? resultDto.SecurityCode;
                    resultDto.SignatureDate = validation.Verificacion.FechaFirma ?? resultDto.SignatureDate;
                    resultDto.QrUrl = validation.Verificacion.VerificationUrl ?? resultDto.QrUrl;
                    resultDto.QrImageUrl = !string.IsNullOrWhiteSpace(validation.Verificacion.QrCodeBase64)
                        ? $"data:image/png;base64,{validation.Verificacion.QrCodeBase64}"
                        : resultDto.QrImageUrl;
                }

                var transmission = new DgiiTransmissionResult
                {
                    TrackId = resultDto.TrackId,
                    Estado = "Aceptado",
                    Mensaje = "Validado por Zynstorm XML Validation"
                };
                resultDto.Transmission = transmission;

                await SaveValidationTransmissionAsync(ecfDocument, transmission, statusId: 10, signedXml, responseBody);
                await MarkDocumentAsync(ecfDocument, 10, resultDto.Message);
                await AddLogAsync(ecfDocument, clientId, "Information", $"XML validado y aceptado por endpoint interno. QR: {resultDto.QrUrl}", responseBody);
                return resultDto;
            }

            resultDto.Success = false;
            resultDto.Message = validation == null
                ? $"El validador interno retorno una respuesta no reconocida. HTTP {(int)response.StatusCode}."
                : "El XML generado no paso la validacion interna de dev/staging.";

            if (validation != null)
            {
                resultDto.XsdErrors = validation.XsdErrors;
                resultDto.XmlProdErrors = validation.StructuralErrors
                    .Concat(validation.BusinessRuleErrors)
                    .Concat(validation.ArithmeticErrors)
                    .ToList();
            }

            await SaveValidationTransmissionAsync(ecfDocument, new DgiiTransmissionResult
            {
                Estado = "ValidationFailed",
                Error = resultDto.Message,
                Mensaje = responseBody
            }, statusId: 3, signedXml, responseBody);
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, clientId, "Warning", resultDto.Message, responseBody);
            return resultDto;
        }
        catch (Exception ex)
        {
            resultDto.Success = false;
            resultDto.Message = $"No fue posible validar el XML contra el endpoint interno: {ex.Message}";
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, clientId, "Error", resultDto.Message, ex.ToString());
            return resultDto;
        }
    }

    private bool ShouldUseStagingXmlValidation()
    {
        return _hostEnvironment.IsDevelopment()
            || _hostEnvironment.IsStaging();
    }

    private static EcfXmlValidationResult? DeserializeValidationResult(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;

        try
        {
            return JsonSerializer.Deserialize<EcfXmlValidationResult>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private async Task<EcfDocument> CreateEcfDocumentAsync(
        EcfInvoiceRequestDto dto,
        Client client,
        ClientBranche clientBranch,
        ApiKey apiKey,
        Currency currency,
        Core.Entities.EcfType ecfType)
    {
        var header = dto.ECF.Encabezado;
        var totals = header.Totales;
        var issueDate = ParseDrDate(header.Emisor.FechaEmision) ?? DateTime.UtcNow;
        var sequenceExpiration = dto.SequenceExpirationDate
            ?? ParseDrDate(header.IdDoc.FechaVencimientoSecuencia)
            ?? DateTime.UtcNow.AddYears(2);

        var ecfDocument = new EcfDocument
        {
            ClientId = client.ClientId,
            ClientBrancheId = clientBranch.ClientBrancheId,
            ApiKeyId = apiKey.ApiKeyId,
            EcfTypeId = ecfType.EcfTypeId,
            ExternalReference = TrimTo(dto.ExternalReference ?? header.Emisor.NumeroFacturaInterna ?? header.IdDoc.eNCF, 70),
            Ncf = TrimTo(header.IdDoc.eNCF, 80),
            CustomerRnc = TrimTo(header.Comprador.RNCComprador ?? string.Empty, 50),
            CustomerForeignId = TrimTo(header.Comprador.IdentificadorExtranjero, 50),
            CustomerCountry = TrimTo(header.Comprador.PaisComprador, 60),
            CustomerName = TrimTo(header.Comprador.RazonSocialComprador ?? "Consumidor Final", 100),
            CustomerEmail = TrimTo(header.Comprador.CorreoComprador, 200),
            IssuerEmail = TrimTo(header.Emisor.CorreoEmisor, 80),
            CustomerAddress = TrimTo(header.Comprador.DireccionComprador, 300),
            IssueDateUtc = issueDate,
            CurrencyId = currency.CurrencyId,
            SubTotal = totals.MontoGravadoTotal ?? totals.MontoExento ?? 0m,
            Itbistotal = totals.TotalITBIS ?? 0m,
            Total = CalculateTransmissionTotal(dto),
            EcfStatusId = 1,
            Version = header.Version,
            SequenceExpirationDate = sequenceExpiration,
            IncomeType = header.IdDoc.TipoIngresos,
            PaymentType = int.TryParse(header.IdDoc.TipoPago, out var paymentType) ? paymentType : 1,
            IssuerCommercialName = TrimTo(header.Emisor.NombreComercial, 150),
            IssuerBranchCode = TrimTo(header.Emisor.Sucursal, 20),
            IssuerMunicipality = TrimTo(header.Emisor.Municipio, 6),
            IssuerProvince = TrimTo(header.Emisor.Provincia, 6),
            IssuerActivityCode = TrimTo(header.Emisor.ActividadEconomica, 10),
            IssuerSellerCode = TrimTo(header.Emisor.CodigoVendedor, 10),
            IssuerWebSite = TrimTo(header.Emisor.WebSite, 80),
            IssuerPhone = TrimTo(header.Emisor.Telefono, 12),
            CustomerContact = TrimTo(header.Comprador.ContactoComprador, 80),
            CustomerMunicipality = TrimTo(header.Comprador.MunicipioComprador, 6),
            CustomerProvince = TrimTo(header.Comprador.ProvinciaComprador, 6),
            CustomerTelephone = TrimTo(header.Comprador.TelefonoAdicional, 12),
            DeliveryDate = ParseDrDate(header.Comprador.FechaEntrega),
            DeliveryContact = TrimTo(header.Comprador.ContactoEntrega, 100),
            DeliveryAddress = TrimTo(header.Comprador.DireccionEntrega, 100),
            AdditionalPhone = TrimTo(header.Comprador.TelefonoAdicional, 12),
            PurchaseOrderDate = ParseDrDate(header.Comprador.FechaOrdenCompra),
            PurchaseOrderNumber = TrimTo(header.Comprador.NumeroOrdenCompra, 20),
            ModifiedNcf = TrimTo(dto.ECF.InformacionReferencia?.NCFModificado, 19),
            ReferenceCustomerRnc = TrimTo(dto.ECF.InformacionReferencia?.RNCOtroContribuyente, 25),
            ModifiedNcfDate = ParseDrDate(dto.ECF.InformacionReferencia?.FechaNCFModificado),
            ModificationCode = int.TryParse(dto.ECF.InformacionReferencia?.CodigoModificacion, out var modificationCode) ? modificationCode : null,
            ModificationReason = TrimTo(dto.ECF.InformacionReferencia?.RazonModificacion, 90),
            SignatureDateTime = dto.SignatureDateOverride
        };

        await _ecfDocumentService.InsertAsync(ecfDocument);
        await AddHistoryAsync(ecfDocument, 1, "Documento e-CF registrado.");
        return ecfDocument;
    }

    private async Task SaveTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        int statusId,
        string signedXml,
        DgiiStatusResponse? status = null)
    {
        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = 1,
            RequestPayload = signedXml,
            ResponsePayload = JsonSerializer.Serialize(new { transmission, status }),
            EcfStatusId = statusId,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = TrimTo(status?.Codigo ?? transmission.Codigo?.ToString() ?? string.Empty, 50),
            ResponseMessage = BuildTransmissionResponseMessage(transmission, status),
            Success = statusId == 10 || (status == null && transmission.Success)
        });
    }

    private async Task SaveValidationTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        int statusId,
        string signedXml,
        string responsePayload)
    {
        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = 1,
            RequestPayload = signedXml,
            ResponsePayload = responsePayload,
            EcfStatusId = statusId,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = statusId == 10 ? "VALID" : "INVALID",
            ResponseMessage = BuildDgiiTransmissionError(transmission),
            Success = statusId == 10
        });
    }

    private async Task MarkDocumentAsync(EcfDocument ecfDocument, int statusId, string message)
    {
        ecfDocument.EcfStatusId = statusId;
        await _ecfDocumentService.UpdateAsync(ecfDocument);
        await AddHistoryAsync(ecfDocument, statusId, message);
    }

    private async Task AddHistoryAsync(EcfDocument ecfDocument, int statusId, string message)
    {
        await _ecfStatusHistoryService.InsertAsync(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = statusId,
            Message = message
        });
    }

    private async Task AddLogAsync(EcfDocument ecfDocument, int clientId, string level, string message, string? exception = null)
    {
        await _systemLogService.InsertAsync(new SystemLog
        {
            ClientId = clientId,
            EcfDocumentId = ecfDocument.EcfDocumentId,
            LogLevel = TrimTo(level, 20),
            Message = message,
            Exception = exception,
            CreateAtUtc = DateTime.UtcNow
        });
    }

    internal static int MapDgiiStatusToEcfStatus(DgiiStatusResponse status)
    {
        if (string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase)) return 10;
        if (string.Equals(status.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase)) return 11;
        if (string.Equals(status.Estado, "Error", StringComparison.OrdinalIgnoreCase)) return 12;
        return 7;
    }

    internal static bool IsPendingDgiiStatus(DgiiStatusResponse status)
    {
        if (string.IsNullOrWhiteSpace(status.Estado)) return true;
        return status.Estado.Equals("Recibido", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("En Proceso", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("Procesando", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase);
    }

    internal static QrMetadata BuildQrMetadata(EcfDocument ecfDocument, string signedXml, string rncEmisor)
    {
        var securityCode = ExtractSecurityCode(signedXml);
        var signatureDate = ExtractXmlValue(signedXml, "FechaHoraFirma");

        if (string.IsNullOrWhiteSpace(securityCode))
            securityCode = GenerateSecurityCode();
        if (string.IsNullOrWhiteSpace(signatureDate))
            signatureDate = ecfDocument.SignatureDateTime?.ToString("dd-MM-yyyy HH:mm:ss") ?? DateTime.Now.ToDrTime().ToString("dd-MM-yyyy HH:mm:ss");

        var qrUrl = BuildQrUrl(
            NcfHelper.ExtractEcfType(ecfDocument.Ncf),
            rncEmisorRaw: rncEmisor,
            rncCompradorRaw: ecfDocument.CustomerRnc,
            encf: ecfDocument.Ncf,
            fechaEmision: ecfDocument.IssueDateUtc.ToString("dd-MM-yyyy"),
            montoTotal: ecfDocument.Total,
            fechaFirma: signatureDate,
            securityCode: securityCode);

        return new QrMetadata(securityCode, signatureDate, qrUrl, BuildQrImageUrl(qrUrl));
    }

    private static void ApplyQrMetadata(ReceivedEcfEmissionResultDto resultDto, EcfInvoiceRequestDto dto, string signedXml, int ecfType)
    {
        var securityCode = ExtractSecurityCode(signedXml);
        var signatureDate = ExtractXmlValue(signedXml, "FechaHoraFirma");

        if (string.IsNullOrWhiteSpace(securityCode))
            securityCode = dto.SecurityCodeOverride ?? GenerateSecurityCode();
        if (string.IsNullOrWhiteSpace(signatureDate))
            signatureDate = dto.ECF.FechaHoraFirma ?? dto.SignatureDateOverride?.ToString("dd-MM-yyyy HH:mm:ss") ?? DateTime.Now.ToDrTime().ToString("dd-MM-yyyy HH:mm:ss");

        var qrUrl = BuildQrUrl(
            ecfType,
            dto.ECF.Encabezado.Emisor.RNCEmisor,
            dto.ECF.Encabezado.Comprador.RNCComprador ?? string.Empty,
            dto.ECF.Encabezado.IdDoc.eNCF,
            dto.ECF.Encabezado.Emisor.FechaEmision,
            CalculateTransmissionTotal(dto),
            signatureDate,
            securityCode);

        resultDto.SecurityCode = securityCode;
        resultDto.SignatureDate = signatureDate;
        resultDto.QrUrl = qrUrl;
        resultDto.QrImageUrl = BuildQrImageUrl(qrUrl);
    }

    private static EcfXmlValidationResult BuildAcceptedValidationResult(
        EcfInvoiceRequestDto dto,
        string signedXml,
        int ecfType,
        string qrUrl,
        string securityCode,
        string signatureDate,
        string qrImageUrl)
    {
        var header = dto.ECF.Encabezado;
        var qrBase64 = qrImageUrl.StartsWith("data:image/png;base64,", StringComparison.OrdinalIgnoreCase)
            ? qrImageUrl["data:image/png;base64,".Length..]
            : qrImageUrl;

        return new EcfXmlValidationResult
        {
            EcfType = ecfType,
            ENcf = header.IdDoc.eNCF,
            Verificacion = new EcfVerificacionInfo
            {
                RncEmisor = OnlyDigits(header.Emisor.RNCEmisor),
                RazonSocialEmisor = header.Emisor.RazonSocialEmisor,
                RncComprador = OnlyDigits(header.Comprador.RNCComprador),
                RazonSocialComprador = header.Comprador.RazonSocialComprador,
                ENcf = header.IdDoc.eNCF,
                FechaEmision = header.Emisor.FechaEmision,
                TotalItbis = header.Totales.TotalITBIS?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                MontoTotal = CalculateTransmissionTotal(dto).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture),
                Estado = "Aceptado",
                EcfType = ecfType,
                TipoDocumento = GetEcfTypeName(ecfType),
                ValidadoEnUtc = DateTime.UtcNow,
                CodigoSeguridad = securityCode,
                FechaFirma = signatureDate,
                VerificationUrl = qrUrl,
                QrCodeBase64 = qrBase64
            }
        };
    }

    private static string BuildQrUrl(
        int ecfType,
        string rncEmisorRaw,
        string rncCompradorRaw,
        string encf,
        string fechaEmision,
        decimal montoTotal,
        string fechaFirma,
        string securityCode)
    {
        var rncEmisor = OnlyDigits(rncEmisorRaw);
        var rncComprador = OnlyDigits(rncCompradorRaw);
        var fechaEmisionUrl = !string.IsNullOrWhiteSpace(fechaEmision)
            ? fechaEmision.Split(' ')[0].Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy");
        var fechaFirmaUrl = (!string.IsNullOrWhiteSpace(fechaFirma)
            ? fechaFirma.Replace("/", "-")
            : DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")).Replace(" ", "%20");
        var montoTotalUrl = montoTotal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

        if (ecfType == 32 && montoTotal < 250000m)
        {
            return $"https://fc.dgii.gov.do/ConsultaTimbreFC?RncEmisor={rncEmisor}&ENCF={encf}&MontoTotal={montoTotalUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
        }

        if (string.IsNullOrEmpty(rncComprador))
        {
            return $"https://ecf.dgii.gov.do/ConsultaTimbre?RncEmisor={rncEmisor}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
        }

        return $"https://ecf.dgii.gov.do/ConsultaTimbre?RncEmisor={rncEmisor}&RncComprador={rncComprador}&ENCF={encf}&FechaEmision={fechaEmisionUrl}&MontoTotal={montoTotalUrl}&FechaFirma={fechaFirmaUrl}&CodigoSeguridad={Uri.EscapeDataString(securityCode)}";
    }

    private static string BuildQrImageUrl(string qrUrl)
    {
        var qrBase64 = GenerateQrCodeBase64(qrUrl);
        return string.IsNullOrWhiteSpace(qrBase64)
            ? string.Empty
            : $"data:image/png;base64,{qrBase64}";
    }

    private static string GenerateQrCodeBase64(string qrUrl)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20);
            return Convert.ToBase64String(qrCodeImage);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetEcfTypeName(int ecfType)
    {
        return ecfType switch
        {
            31 => "Factura de Credito Fiscal Electronica",
            32 => "Factura de Consumo Electronica",
            33 => "Nota de Credito Electronica",
            34 => "Nota de Debito Electronica",
            41 => "Comprobante de Compras Electronico",
            43 => "Gastos Menores Electronico",
            44 => "Regimenes Especiales Electronico",
            45 => "Gubernamental Electronico",
            46 => "Comprobante de Exportaciones Electronico",
            47 => "Comprobante para Pagos al Exterior Electronico",
            _ => $"Tipo {ecfType}"
        };
    }

    private static string ExtractXmlValue(string xml, string localName)
    {
        if (string.IsNullOrWhiteSpace(xml)) return string.Empty;
        try
        {
            var doc = XDocument.Parse(xml);
            return doc.Descendants().FirstOrDefault(x => x.Name.LocalName == localName)?.Value ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ExtractSecurityCode(string xml)
    {
        var rfceSecurityCode = ExtractXmlValue(xml, "CodigoSeguridadeCF");
        if (!string.IsNullOrWhiteSpace(rfceSecurityCode))
            return rfceSecurityCode;

        var signatureValue = ExtractXmlValue(xml, "SignatureValue");
        var normalizedSignature = new string(signatureValue.Where(char.IsLetterOrDigit).ToArray());
        return normalizedSignature.Length >= 6 ? normalizedSignature[..6].ToUpperInvariant() : string.Empty;
    }

    private static string OnlyDigits(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    private static string GenerateSecurityCode()
    {
        return Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
    }

    private static string BuildTransmissionResponseMessage(DgiiTransmissionResult transmission, DgiiStatusResponse? status)
    {
        if (status != null) return BuildDgiiStatusError(status);
        return BuildDgiiTransmissionError(transmission);
    }

    private static DateTime? ParseDrDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var formats = new[] { "dd-MM-yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "yyyy/MM/dd" };
        if (DateTime.TryParseExact(value, formats, null, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;
        return DateTime.TryParse(value, out parsed) ? parsed : null;
    }

    private static string TrimTo(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
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

public record QrMetadata(string SecurityCode, string SignatureDate, string QrUrl, string QrImageUrl);
