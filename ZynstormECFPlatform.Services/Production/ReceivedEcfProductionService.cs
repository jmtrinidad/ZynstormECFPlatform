using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ZynstormECFPlatform.Abstractions.Data;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Services.Billing;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IClientUsageService _clientUsageService;
    private readonly IEcfLookupService _lookupService;
    private readonly EcfReferenceCache _referenceCache;

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
        IUnitOfWork unitOfWork,
        IHttpClientFactory httpClientFactory,
        ICacheService cacheService,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IClientUsageService clientUsageService,
        IEcfLookupService lookupService,
        EcfReferenceCache referenceCache)
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
        _unitOfWork = unitOfWork;
        _httpClientFactory = httpClientFactory;
        _cacheService = cacheService;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _clientUsageService = clientUsageService;
        _lookupService = lookupService;
        _referenceCache = referenceCache;
    }

    public async Task<ReceivedEcfEmissionResultDto> ProcessAsync(
        EcfInvoiceRequestDto dto,
        DgiiEnvironment environment = DgiiEnvironment.Production,
        int statusDelayMilliseconds = 750,
        CancellationToken cancellationToken = default,
        bool deferred = false)
    {
        var resultDto = new ReceivedEcfEmissionResultDto();
        var timings = new EcfEmitTimings();

        var targetEnvironment = ResolveTargetEnvironment(environment);
        resultDto.TargetEnvironment = targetEnvironment.ToString();

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

        var client = await _clientService.GetByAsync(c => c.Rnc == issuerRnc);
        if (client == null)
            return FailConfiguration(resultDto, $"Cliente con RNC {issuerRnc} no encontrado.");

        if (client.ClientInactive)
            return BuildClientInactiveResult(resultDto);

        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        if (apiKey == null)
            return FailConfiguration(resultDto, "ApiKey no encontrada.");

        var clientBranch = await _referenceCache.GetOrLoadAsync(
            $"ecf-ref:branch:{client.ClientId}",
            async () => await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId && x.IsMain)
                ?? await _clientBrancheService.GetByAsync(x => x.ClientId == client.ClientId));
        var currency = await _referenceCache.GetOrLoadAsync(
            "ecf-ref:currency",
            async () => await _currencyService.GetByAsync(x => x.Code == "DOP")
                ?? await _currencyService.GetByAsync(x => x.CurrencyId > 0));
        if (currency == null)
            return FailConfiguration(resultDto, "No hay moneda configurada para registrar el e-CF.");

        var ecfTypeEntity = await _referenceCache.GetOrLoadAsync(
            $"ecf-ref:ecftype:{ecfType}",
            () => _ecfTypeService.GetByAsync(x => x.Code == ecfType.ToString()));
        if (ecfTypeEntity == null)
            return FailConfiguration(resultDto, $"TipoeCF {ecfType} no esta configurado.");

        dto.SignatureDateOverride ??= DateTime.Now.ToDrTime();
        dto.SequenceExpirationDate ??= new DateTime(DateTime.Now.Year + 2, 12, 31);
        dto.SecurityCodeOverride ??= GenerateSecurityCode();
        dto.ECF.Encabezado.IdDoc.TipoIngresos ??= "01";
        dto.ECF.Encabezado.IdDoc.TipoPago ??= "1";
        resultDto.SecurityCode = dto.SecurityCodeOverride;
        resultDto.SignatureDate = dto.ECF.FechaHoraFirma ?? dto.SignatureDateOverride.Value.ToString("dd-MM-yyyy HH:mm:ss");

        var claim = await ClaimDocumentAsync(
            client.ClientId, eNcf, dto, client, clientBranch, apiKey, currency, ecfTypeEntity, cancellationToken);

        // El eNCF ya tenía un documento aceptado o en proceso: no se transmite de nuevo.
        if (claim.Replay is not null)
            return claim.Replay;

        var ecfDocument = claim.Document!;
        resultDto.Attempt = claim.Attempt;
        resultDto.EcfDocumentId = ecfDocument.EcfDocumentId;
        timings.Mark("consultas_previas");

        try
        {
            var processed = await ContinueProcessingAsync(
                resultDto, dto, client, apiKey, ecfDocument, ecfType, targetEnvironment,
                issuerRnc, eNcf, statusDelayMilliseconds, timings, deferred, cancellationToken);

            await AddLogAsync(ecfDocument, client.ClientId, "Information", $"Tiempos de emit [{eNcf}]: {timings}");

            return processed;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            resultDto.Success = false;
            resultDto.HasUnexpectedError = true;
            resultDto.Message = $"Error inesperado durante la emision del e-CF: {ex.Message}";
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Error", resultDto.Message, ex.ToString());
            return resultDto;
        }
    }

    private sealed record EmitClaim(EcfDocument? Document, ReceivedEcfEmissionResultDto? Replay, int Attempt);

    /// <summary>
    /// Decide y reclama el documento en una transacción corta READ COMMITTED protegida por un
    /// candado de Postgres por (cliente, eNCF). La petición que llegue mientras otra decide
    /// espera el candado y, cuando lo obtiene, ya ve el documento que la otra creó: así dos
    /// emisiones simultáneas del mismo eNCF no crean dos envíos. No hay índice único porque
    /// producción ya tiene eNCF repetidos.
    /// </summary>
    private async Task<EmitClaim> ClaimDocumentAsync(
        int clientId,
        string eNcf,
        EcfInvoiceRequestDto dto,
        Client client,
        ClientBranche? clientBranch,
        ApiKey apiKey,
        Currency currency,
        Core.Entities.EcfType ecfType,
        CancellationToken cancellationToken)
    {
        var staleAfter = TimeSpan.FromMinutes(Math.Clamp(
            _configuration.GetValue<int?>("EcfIdempotency:StaleInFlightMinutes") ?? 5,
            1,
            120));

        return await _unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            await _ecfDocumentService.ExecuteAsync(
                "SELECT pg_advisory_xact_lock(@LockKey)",
                new { LockKey = EcfEmitDecision.LockKey(clientId, eNcf) },
                token);

            var existing = await _lookupService.ListExistingAsync(clientId, eNcf, token);
            var decision = EcfEmitDecision.Decide(existing, DateTime.UtcNow, staleAfter);

            if (decision.Action == EcfEmitAction.Replay)
            {
                var documentId = decision.ReplayDocumentId!.Value;
                var state = decision.ReplayState!.Value;

                var lookup = await _lookupService.DescribeDocumentAsync(clientId, documentId, state, existing.Count, token);
                var replay = EcfEmitReplay.ToResult(lookup, existing.Count);

                var current = existing.First(document => document.EcfDocumentId == documentId);
                var note = $"Reenvío ignorado: el eNCF {eNcf} ya tiene un documento en estado {state}; no se transmitió de nuevo.";

                await _systemLogService.InsertAsync(new SystemLog
                {
                    ClientId = clientId,
                    EcfDocumentId = documentId,
                    LogLevel = "Information",
                    Message = note,
                    CreateAtUtc = DateTime.UtcNow
                });

                await _ecfStatusHistoryService.InsertAsync(new EcfStatusHistory
                {
                    EcfDocumentId = documentId,
                    EcfStatusId = current.EcfStatusId,
                    Message = note
                });

                return new EmitClaim(null, replay, existing.Count);
            }

            var document = await CreateEcfDocumentAsync(dto, client, clientBranch, apiKey, currency, ecfType);

            return new EmitClaim(document, null, existing.Count + 1);
        }, IsolationLevel.ReadCommitted, cancellationToken);
    }

    public const string ClientInactiveMessage = "El cliente se encuentra desactivado. Por favor, comuníquese con soporte.";

    public static ReceivedEcfEmissionResultDto BuildClientInactiveResult(ReceivedEcfEmissionResultDto resultDto)
    {
        resultDto.Success = false;
        resultDto.ClientInactive = true;
        resultDto.Message = ClientInactiveMessage;
        return resultDto;
    }

    private static ReceivedEcfEmissionResultDto FailConfiguration(ReceivedEcfEmissionResultDto resultDto, string message)
    {
        resultDto.Success = false;
        resultDto.Message = message;
        resultDto.ConfigurationErrors.Add(message);
        return resultDto;
    }

    private async Task<ReceivedEcfEmissionResultDto> ContinueProcessingAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfInvoiceRequestDto dto,
        Client client,
        ApiKey apiKey,
        EcfDocument ecfDocument,
        int ecfType,
        DgiiEnvironment targetEnvironment,
        string issuerRnc,
        string eNcf,
        int statusDelayMilliseconds,
        EcfEmitTimings timings,
        bool deferred,
        CancellationToken cancellationToken)
    {
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = 1,
            Message = "Documento e-CF registrado."
        });
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = 2,
            Message = "Iniciando validacion y generacion del XML."
        });
        _systemLogService.Add(new SystemLog
        {
            ClientId = client.ClientId,
            EcfDocumentId = ecfDocument.EcfDocumentId,
            LogLevel = "Information",
            Message = "Proceso de emision e-CF iniciado.",
            CreateAtUtc = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        var total = CalculateTransmissionTotal(dto);
        var isSummary = ShouldSendAsB2cSummary(ecfType, total);

        var unsignedXml = _generatorService.GenerateUnsignedXml(dto, isSummary);
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

        //var xmlProdErrors = ValidateXmlAgainstProdReferences(unsignedXml, ecfType);
        //resultDto.XmlProdErrors = xmlProdErrors;

        //if (xmlProdErrors.Count > 0)
        //{
        //    resultDto.Message = "El XML generado no cumple con las referencias estructurales de XmlProd.";
        //    await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
        //    await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message, JsonSerializer.Serialize(xmlProdErrors));
        //    return resultDto;
        //}

        timings.Mark("generacion_xsd");

        var material = await LoadSigningMaterialAsync(client.ClientId, apiKey);
        if (material is null)
        {
            FailConfiguration(resultDto, "Certificado no encontrado.");
            await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
            await AddLogAsync(ecfDocument, client.ClientId, "Warning", resultDto.Message);
            return resultDto;
        }
        var (certBase64, certPass) = material.Value;

        var signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);
        resultDto.SignedXml = signedXml;
        ApplyQrMetadata(resultDto, dto, signedXml, ecfType, targetEnvironment);
        resultDto.XmlValidation = BuildAcceptedValidationResult(dto, signedXml, ecfType, resultDto.QrUrl, resultDto.SecurityCode, resultDto.SignatureDate);
        timings.Mark("firma");

        var useStagingValidation = ShouldUseStagingXmlValidation();
        // En modo diferido el token lo pide el job: no se gasta una llamada a la DGII en la respuesta.
        var tokenTask = useStagingValidation || deferred
            ? null
            : _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);

        _ecfXmlDocumentService.Add(new EcfXmlDocument
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            XmlUnsigned = unsignedXml,
            XmlSigned = signedXml
        });
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = 6,
            Message = "XML firmado y guardado."
        });

        if (deferred)
        {
            ecfDocument.EcfStatusId = 7;
            _ecfDocumentService.Modify(ecfDocument);
            _ecfStatusHistoryService.Add(new EcfStatusHistory
            {
                EcfDocumentId = ecfDocument.EcfDocumentId,
                EcfStatusId = 7,
                Message = "Firmado; la transmisión a la DGII quedó en cola."
            });
            _systemLogService.Add(new SystemLog
            {
                ClientId = client.ClientId,
                EcfDocumentId = ecfDocument.EcfDocumentId,
                LogLevel = "Information",
                Message = $"Modo diferido: se respondió al firmar y la transmisión corre en segundo plano ({targetEnvironment}).",
                CreateAtUtc = DateTime.UtcNow
            });
            await _unitOfWork.SaveChangesAsync();
            timings.Mark("guardado_previo");

            // No se guarda el id del job en el documento: UpdateAsync reescribe el documento
            // completo y podría pisar el estado 8 que el job ya escribió.
            var queuedJobId = BackgroundJob.Enqueue<EcfTransmitJob>(
                job => job.Execute(ecfDocument.EcfDocumentId, 1, targetEnvironment));

            resultDto.Success = false;
            resultDto.IsPending = true;
            resultDto.HangfireJobId = queuedJobId;
            resultDto.Message = "Firmado; la transmisión a la DGII está en curso.";

            return resultDto;
        }

        var sendingMessage = isSummary ? "Enviando resumen B2C a DGII." : "Enviando e-CF a DGII.";
        ecfDocument.EcfStatusId = 8;
        _ecfDocumentService.Modify(ecfDocument);
        _ecfStatusHistoryService.Add(new EcfStatusHistory
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            EcfStatusId = 8,
            Message = sendingMessage
        });
        _systemLogService.Add(new SystemLog
        {
            ClientId = client.ClientId,
            EcfDocumentId = ecfDocument.EcfDocumentId,
            LogLevel = "Information",
            Message = $"Ambiente DGII seleccionado para envio: {targetEnvironment}. Canal: {(isSummary ? "Resumen B2C" : "e-CF")}.",
            CreateAtUtc = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();
        timings.Mark("guardado_previo");

        await TransmitAndTrackAsync(
            resultDto,
            ecfDocument,
            client.ClientId,
            signedXml,
            ecfType,
            total,
            issuerRnc,
            eNcf,
            isSummary,
            targetEnvironment,
            certBase64,
            certPass,
            tokenTask,
            statusDelayMilliseconds,
            timings,
            attemptNumber: 1,
            isDeferred: false,
            earlierAttemptsWithoutTrackId: false,
            cancellationToken);

        return resultDto;
    }

    private enum TransmitStep { Completed, TransportFailure }

    /// <summary>
    /// Transmite el documento ya firmado a la DGII y sigue su estado hasta donde se pueda.
    /// Lo usan el modo síncrono de <c>emit</c> y, más adelante, el job de transmisión diferida.
    /// </summary>
    private async Task<TransmitStep> TransmitAndTrackAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        string signedXml,
        int ecfType,
        decimal total,
        string issuerRnc,
        string eNcf,
        bool isSummary,
        DgiiEnvironment targetEnvironment,
        string certBase64,
        string certPass,
        Task<string>? tokenTask,
        int statusDelayMilliseconds,
        EcfEmitTimings? timings,
        int attemptNumber,
        bool isDeferred,
        bool earlierAttemptsWithoutTrackId,
        CancellationToken cancellationToken)
    {
        if (ShouldUseStagingXmlValidation())
        {
            await ProcessWithStagingValidationAsync(
                resultDto,
                ecfDocument,
                clientId,
                signedXml,
                statusDelayMilliseconds,
                issuerRnc,
                targetEnvironment,
                certBase64,
                certPass);

            return TransmitStep.Completed;
        }

        var token = await tokenTask!;

        var transmission = await _transmissionService.SendEcfAsync(targetEnvironment, token, signedXml, ecfType, total, issuerRnc, eNcf, isSummary);
        await AddDgiiResponseLogAsync(ecfDocument, clientId, "recepcion", targetEnvironment, transmission);
        timings?.Mark("recepcion_dgii");

        resultDto.Transmission = transmission;
        resultDto.TrackId = transmission.TrackId;

        // Diferido: un fallo de transporte (no se sabe si llegó) no es un error del documento. Se
        // deja el rastro del intento y quien llama decide si reintenta.
        if (isDeferred && transmission.TransportFailure)
        {
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 12, signedXml, attemptNumber: attemptNumber);
            await AddLogAsync(
                ecfDocument,
                clientId,
                "Warning",
                $"Fallo de transporte hacia la DGII en el intento {attemptNumber}: {BuildDgiiTransmissionError(transmission)}",
                JsonSerializer.Serialize(transmission));

            return TransmitStep.TransportFailure;
        }

        if (!transmission.Success)
        {
            resultDto.Message = BuildDgiiTransmissionError(transmission);
            await SaveTransmissionAsync(ecfDocument, transmission, statusId: 12, signedXml);
            await MarkDocumentAsync(ecfDocument, 12, resultDto.Message);
            await AddLogAsync(ecfDocument, clientId, "Error", resultDto.Message, JsonSerializer.Serialize(transmission));
            return TransmitStep.Completed;
        }

        if (!string.IsNullOrWhiteSpace(transmission.TrackId))
        {
            var transmissionRecord = await BeginTransmissionAsync(ecfDocument, transmission, signedXml, attemptNumber);

            var status = await WaitForFinalDgiiStatusAsync(
                targetEnvironment, token, transmission.TrackId, cancellationToken);
            _cacheService.Set($"EcfStatus_{transmission.TrackId}", status, TimeSpan.FromHours(1));
            timings?.Mark("espera_estado_final");
            await AddDgiiStatusLogAsync(ecfDocument, clientId, targetEnvironment, transmission.TrackId, status);
            resultDto.Status = status;
            resultDto.DgiiResponse = status;
            resultDto.IsAcceptedConditional = IsAcceptedConditionalDgiiStatus(status);
            resultDto.RequiresCorrection = RequiresCorrectionDgiiStatus(status);
            resultDto.Success = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase);
            var statusId = MapDgiiStatusToEcfStatus(status);
            resultDto.Message = resultDto.Success ? $"TrackId: {transmission.TrackId}" : BuildDgiiStatusError(status);

            // 1209/75 en un reintento propio: el primer envío probablemente sí llegó y la
            // respuesta se perdió. No es una secuencia quemada ni un rechazo: se deja Pendiente
            // con un aviso, sin Status/DgiiResponse rechazados (la librería los clasificaría
            // como secuencia quemada) y sin pedir un NCF nuevo.
            if (EcfTransmitRetryPolicy.IsOwnDuplicate(status, earlierAttemptsWithoutTrackId))
            {
                statusId = 9;
                resultDto.Success = false;
                resultDto.IsAcceptedConditional = false;
                resultDto.RequiresCorrection = false;
                resultDto.IsPending = true;
                resultDto.Status = null;
                resultDto.DgiiResponse = null;
                resultDto.Message = "La DGII ya recibió este comprobante en un envío anterior; falta confirmar su TrackId.";

                await AddLogAsync(
                    ecfDocument,
                    clientId,
                    "Warning",
                    $"Reintento {attemptNumber}: la DGII ya tenía el eNCF {eNcf} de un envío anterior sin TrackId. No se trata como secuencia quemada; conciliar con la DGII.",
                    JsonSerializer.Serialize(status));
            }

            if (resultDto.Success)
                await AddLogAsync(ecfDocument, clientId, "Information", $"e-CF aprobado. TrackId: {transmission.TrackId}. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");

            await MarkDocumentAsync(ecfDocument, statusId, resultDto.Message);
            await CompleteTransmissionAsync(transmissionRecord, transmission, statusId, status);

            if (resultDto.Success || resultDto.IsAcceptedConditional)
                await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId, cancellationToken);

            if (IsPendingDgiiStatus(status))
            {
                resultDto.IsPending = true;
                resultDto.Success = false;
                resultDto.Message = string.IsNullOrWhiteSpace(status.Error)
                    ? $"DGII aun procesa el e-CF. TrackId: {transmission.TrackId}"
                    : status.Error;

                var trackingDelaySeconds = Math.Max(
                    1,
                    _configuration.GetValue<int?>("EcfPerformance:TrackingInitialDelaySeconds") ?? 1);
                var jobId = BackgroundJob.Schedule<EcfTrackingJob>(
                    j => j.Execute(transmission.TrackId, targetEnvironment, issuerRnc, certBase64, certPass, ecfDocument.EcfDocumentId, 1),
                    TimeSpan.FromSeconds(trackingDelaySeconds));

                ecfDocument.HangfireJobId = jobId;
                await _ecfDocumentService.UpdateAsync(ecfDocument);
                resultDto.HangfireJobId = jobId;
                await AddLogAsync(ecfDocument, clientId, "Information", $"DGII no retorno aceptacion inmediata. Job de seguimiento programado: {jobId}.");
            }

            return TransmitStep.Completed;
        }

        resultDto.Success = string.Equals(transmission.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || transmission.Codigo is 0 or 1;
        resultDto.Message = resultDto.Success ? $"DGII: {transmission.Estado ?? "Aceptado"}" : BuildDgiiTransmissionError(transmission);
        var finalStatusId = resultDto.Success ? 10 : 12;

        await SaveTransmissionAsync(ecfDocument, transmission, finalStatusId, signedXml);
        await MarkDocumentAsync(ecfDocument, finalStatusId, resultDto.Message);
        await AddLogAsync(ecfDocument, clientId, resultDto.Success ? "Information" : "Error", resultDto.Message, JsonSerializer.Serialize(transmission));

        if (resultDto.Success)
        {
            await AddLogAsync(ecfDocument, clientId, "Information", $"e-CF aprobado. CodigoSeguridad: {resultDto.SecurityCode}. FechaFirma: {resultDto.SignatureDate}. QR: {resultDto.QrUrl}.");
            await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId, cancellationToken);
        }

        return TransmitStep.Completed;
    }

    /// <summary>
    /// Certificado del cliente y su contraseña, descifrados con la clave de su API key. Null si el
    /// cliente no tiene un certificado activo. Se lee de la base de datos en cada uso: nunca se
    /// cachea ni se pasa como argumento de un job.
    /// </summary>
    private async Task<(string CertBase64, string CertPass)?> LoadSigningMaterialAsync(int clientId, ApiKey apiKey)
    {
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey ?? string.Empty);
        var certificate = await _clientCertificateService.GetActiveCertificateAsync(x => x.ClientId == clientId);

        if (certificate == null)
            return null;

        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

        return (Convert.ToBase64String(certificateBytes), Encoding.UTF8.GetString(passwordBytes));
    }

    public async Task<DeferredTransmitOutcome> TransmitDeferredAsync(
        int ecfDocumentId,
        int attemptNumber,
        DgiiEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var ecfDocument = await _ecfDocumentService.GetAsync(ecfDocumentId);

        if (ecfDocument is null)
            return new DeferredTransmitOutcome(null);

        // Solo se transmite lo que quedó en cola (7). Si otra ejecución ya lo pasó a 8 o ya
        // está aceptado, no se hace nada: así dos ejecuciones nunca transmiten el mismo documento.
        if (ecfDocument.EcfStatusId != 7)
        {
            await AddLogAsync(
                ecfDocument,
                ecfDocument.ClientId,
                "Information",
                $"Transmisión diferida omitida (intento {attemptNumber}): el documento está en estado {ecfDocument.EcfStatusId}, no en cola.");

            return new DeferredTransmitOutcome(null);
        }

        var clientId = ecfDocument.ClientId;
        await MarkDocumentAsync(ecfDocument, 8, $"Enviando e-CF a DGII (intento {attemptNumber}).");

        TransmitStep step;

        try
        {
            var client = await _clientService.GetAsync(clientId)
                ?? throw new InvalidOperationException($"No se encontró el cliente {clientId} del documento {ecfDocumentId}.");
            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == clientId)
                ?? throw new InvalidOperationException($"No hay API key para el cliente {clientId}.");

            var material = await LoadSigningMaterialAsync(clientId, apiKey);

            if (material is null)
            {
                await MarkDocumentAsync(ecfDocument, 3, "Certificado no encontrado.");
                await AddLogAsync(ecfDocument, clientId, "Warning", "Certificado no encontrado; no se pudo transmitir.");
                return new DeferredTransmitOutcome(null);
            }

            var (certBase64, certPass) = material.Value;

            var xml = await _ecfXmlDocumentService.GetByAsync(x => x.EcfDocumentId == ecfDocumentId)
                ?? throw new InvalidOperationException($"El documento {ecfDocumentId} no tiene XML firmado.");

            var ecfType = NcfHelper.ExtractEcfType(ecfDocument.Ncf);
            var total = ecfDocument.Total;
            var issuerRnc = client.Rnc;
            var targetEnvironment = ResolveTargetEnvironment(environment);

            var earlierAttemptsWithoutTrackId = attemptNumber > 1
                && !await _ecfTransmissionService.Table.AnyAsync(
                    t => t.EcfDocumentId == ecfDocumentId && t.TrackId != string.Empty,
                    cancellationToken);

            var tokenTask = ShouldUseStagingXmlValidation()
                ? null
                : _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);

            var resultDto = new ReceivedEcfEmissionResultDto
            {
                EcfDocumentId = ecfDocumentId,
                ENcf = ecfDocument.Ncf,
                EcfType = ecfType
            };

            step = await TransmitAndTrackAsync(
                resultDto,
                ecfDocument,
                clientId,
                xml.XmlSigned,
                ecfType,
                total,
                issuerRnc,
                ecfDocument.Ncf,
                ShouldSendAsB2cSummary(ecfType, total),
                targetEnvironment,
                certBase64,
                certPass,
                tokenTask,
                statusDelayMilliseconds: 750,
                timings: null,
                attemptNumber,
                isDeferred: true,
                earlierAttemptsWithoutTrackId,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un fallo inesperado (token, red, base de datos) se trata como transporte: se
            // reintenta con las mismas esperas y, agotadas, el documento queda en Error.
            await AddLogAsync(
                ecfDocument,
                clientId,
                "Error",
                $"Fallo inesperado al transmitir (intento {attemptNumber}): {ex.Message}",
                ex.ToString());

            step = TransmitStep.TransportFailure;
        }

        if (step == TransmitStep.Completed)
            return new DeferredTransmitOutcome(null);

        var delays = EcfTransmitRetryPolicy.Normalize(
            _configuration.GetSection("EcfTransmit:RetryDelaysSeconds").Get<int[]>());
        var delay = EcfTransmitRetryPolicy.NextDelay(attemptNumber, delays);

        if (delay is { } wait)
        {
            await MarkDocumentAsync(
                ecfDocument,
                7,
                $"La DGII no respondió en el intento {attemptNumber}; se reintentará en {wait.TotalSeconds:0} s.");

            return new DeferredTransmitOutcome(wait);
        }

        await MarkDocumentAsync(
            ecfDocument,
            12,
            $"Se agotaron los reintentos de transmisión a la DGII ({attemptNumber} intentos). El documento se puede reenviar con el mismo eNCF.");
        await AddLogAsync(ecfDocument, clientId, "Error", $"Se agotaron los reintentos de transmisión tras {attemptNumber} intentos.");

        return new DeferredTransmitOutcome(null);
    }

    private DgiiEnvironment ResolveTargetEnvironment(DgiiEnvironment requestedEnvironment)
    {
        var configuredEnvironment = _configuration["EcfXmlValidation:TargetDgiiEnvironment"];
        if (!string.IsNullOrWhiteSpace(configuredEnvironment) &&
            Enum.TryParse<DgiiEnvironment>(configuredEnvironment, true, out var parsedEnvironment))
        {
            return parsedEnvironment;
        }

        return requestedEnvironment;
    }

    private async Task<ReceivedEcfEmissionResultDto> ProcessWithStagingValidationAsync(
        ReceivedEcfEmissionResultDto resultDto,
        EcfDocument ecfDocument,
        int clientId,
        string signedXml,
        int statusDelayMilliseconds,
        string issuerRnc,
        DgiiEnvironment targetEnvironment,
        string certBase64,
        string certPass)
    {
        var validationUrl = _configuration["EcfXmlValidation:DevStagingUrl"]
            ?? "https://ecfstaging.zynstorm.com/api/v1/EcfXmlValidation/validate";

        await AddLogAsync(ecfDocument, clientId, "Information", $"Ambiente {_hostEnvironment.EnvironmentName}: obteniendo token de autenticación local.");

        string token = "";
        try
        {
            token = await _authService.GetTokenAsync(issuerRnc, targetEnvironment, certBase64, certPass);
            await AddLogAsync(ecfDocument, clientId, "Information", "Token de autenticación local obtenido correctamente.");
        }
        catch (Exception ex)
        {
            await AddLogAsync(ecfDocument, clientId, "Warning", $"No fue posible obtener el token local: {ex.Message}");
        }

        await AddLogAsync(ecfDocument, clientId, "Information", $"Ambiente {_hostEnvironment.EnvironmentName}: enviando XML al validador interno {validationUrl}.");

        try
        {
            var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, validationUrl);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            request.Content = new StringContent(signedXml, Encoding.UTF8, "application/xml");

            using var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            var receipt = DeserializeValidationReceipt(responseBody);

            if (receipt == null || string.IsNullOrWhiteSpace(receipt.TrackId))
            {
                resultDto.Success = false;
                resultDto.Message = $"El validador interno no retorno TrackId. HTTP {(int)response.StatusCode}.";
                await SaveValidationTransmissionAsync(ecfDocument, new DgiiTransmissionResult
                {
                    Estado = "ValidationReceiveFailed",
                    Error = resultDto.Message,
                    Mensaje = responseBody
                }, statusId: 3, signedXml, responseBody);
                await MarkDocumentAsync(ecfDocument, 3, resultDto.Message);
                await AddLogAsync(ecfDocument, clientId, "Warning", resultDto.Message, responseBody);
                return resultDto;
            }

            resultDto.TrackId = receipt.TrackId;
            await AddLogAsync(ecfDocument, clientId, "Information", $"XML recibido por validador interno. TrackId: {receipt.TrackId}.", responseBody);

            await Task.Delay(Math.Max(0, statusDelayMilliseconds));

            var statusUrl = BuildValidationStatusUrl(validationUrl, receipt.TrackId);
            using var statusRequest = new HttpRequestMessage(HttpMethod.Get, statusUrl);
            if (!string.IsNullOrEmpty(token))
            {
                statusRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
            using var statusResponse = await client.SendAsync(statusRequest);
            var statusBody = await statusResponse.Content.ReadAsStringAsync();
            var status = DeserializeValidationStatus(statusBody);

            if (status == null)
            {
                resultDto.Success = false;
                resultDto.IsPending = true;
                resultDto.Message = $"El validador interno recibio el XML, pero aun no retorno estado reconocible. TrackId: {receipt.TrackId}";
                await SaveValidationTransmissionAsync(ecfDocument, new DgiiTransmissionResult
                {
                    TrackId = receipt.TrackId,
                    Estado = "EnProceso",
                    Mensaje = statusBody
                }, statusId: 9, signedXml, statusBody);
                await MarkDocumentAsync(ecfDocument, 9, resultDto.Message);
                await AddLogAsync(ecfDocument, clientId, "Information", resultDto.Message, statusBody);
                return resultDto;
            }

            var validation = BuildValidationResult(status);
            resultDto.XmlValidation = validation;

            if (status.IsValid == true || string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase))
            {
                resultDto.Success = true;
                resultDto.IsPending = false;
                resultDto.Message = $"XML validado correctamente en {_hostEnvironment.EnvironmentName}. TrackId: {receipt.TrackId}";

                if (status.Verificacion != null)
                {
                    resultDto.SecurityCode = status.Verificacion.CodigoSeguridad ?? resultDto.SecurityCode;
                    resultDto.SignatureDate = status.Verificacion.FechaFirma ?? resultDto.SignatureDate;
                    resultDto.QrUrl = status.Verificacion.VerificationUrl ?? resultDto.QrUrl;
                    resultDto.QrImageUrl = string.Empty;
                }

                var transmission = new DgiiTransmissionResult
                {
                    TrackId = receipt.TrackId,
                    Estado = "Aceptado",
                    Mensaje = "Validado por Zynstorm XML Validation"
                };
                resultDto.Transmission = transmission;

                await SaveValidationTransmissionAsync(ecfDocument, transmission, statusId: 10, signedXml, statusBody);
                await MarkDocumentAsync(ecfDocument, 10, resultDto.Message);
                await AddLogAsync(ecfDocument, clientId, "Information", $"XML validado y aceptado por endpoint interno. QR: {resultDto.QrUrl}", statusBody);
                await _clientUsageService.RegisterAcceptedAsync(ecfDocument.EcfDocumentId, clientId);
                return resultDto;
            }

            resultDto.Success = false;
            resultDto.IsPending = string.Equals(status.Estado, "Recibido", StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(status.Estado, "EnProceso", StringComparison.OrdinalIgnoreCase);
            resultDto.Message = resultDto.IsPending
                ? $"El validador interno aun procesa el XML. TrackId: {receipt.TrackId}"
                : "El XML generado no paso la validacion interna de dev/staging.";
            resultDto.XsdErrors = status.XsdErrors;
            resultDto.XmlProdErrors = status.StructuralErrors
                .Concat(status.BusinessRuleErrors)
                .Concat(status.ArithmeticErrors)
                .ToList();

            await SaveValidationTransmissionAsync(ecfDocument, new DgiiTransmissionResult
            {
                TrackId = receipt.TrackId,
                Estado = status.Estado,
                Error = resultDto.IsPending ? null : resultDto.Message,
                Mensaje = statusBody
            }, statusId: resultDto.IsPending ? 9 : 3, signedXml, statusBody);
            await MarkDocumentAsync(ecfDocument, resultDto.IsPending ? 9 : 3, resultDto.Message);
            await AddLogAsync(ecfDocument, clientId, resultDto.IsPending ? "Information" : "Warning", resultDto.Message, statusBody);
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
        return _configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator");
    }

    private static EcfXmlValidationReceipt? DeserializeValidationReceipt(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;

        try
        {
            return JsonSerializer.Deserialize<EcfXmlValidationReceipt>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static EcfXmlValidationTrackStatus? DeserializeValidationStatus(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;

        try
        {
            return JsonSerializer.Deserialize<EcfXmlValidationTrackStatus>(
                responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static EcfXmlValidationResult BuildValidationResult(EcfXmlValidationTrackStatus status)
    {
        return new EcfXmlValidationResult
        {
            EcfType = status.EcfType,
            ENcf = status.ENcf,
            StructuralErrors = status.StructuralErrors,
            XsdErrors = status.XsdErrors,
            BusinessRuleErrors = status.BusinessRuleErrors,
            ArithmeticErrors = status.ArithmeticErrors,
            Verificacion = status.Verificacion
        };
    }

    private static string BuildValidationStatusUrl(string validationUrl, string trackId)
    {
        var baseUrl = validationUrl.TrimEnd('/');
        if (baseUrl.EndsWith("/validate", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^"/validate".Length];

        return $"{baseUrl}/estado/{Uri.EscapeDataString(trackId)}";
    }

    private async Task<EcfDocument> CreateEcfDocumentAsync(
        EcfInvoiceRequestDto dto,
        Client client,
        ClientBranche? clientBranch,
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
            ClientBrancheId = clientBranch?.ClientBrancheId,
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
        return ecfDocument;
    }

    private async Task SaveTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        int statusId,
        string signedXml,
        DgiiStatusResponse? status = null,
        int attemptNumber = 1)
    {
        await _ecfTransmissionService.InsertAsync(new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = attemptNumber,
            RequestPayload = signedXml,
            ResponsePayload = JsonSerializer.Serialize(new { transmission, status }),
            EcfStatusId = statusId,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = TrimTo(status?.Codigo ?? transmission.Codigo?.ToString() ?? string.Empty, 50),
            ResponseMessage = BuildTransmissionResponseMessage(transmission, status),
            Success = statusId == 10 || (status == null && transmission.Success)
        });
    }

    /// <summary>
    /// La DGII ya recibió el documento y devolvió su TrackId: se guarda ahora, antes de esperar
    /// el estado final (hasta 60 s). Si la petición se corta en esa espera, el TrackId ya está
    /// en la base de datos y el job de seguimiento y la consulta por eNCF lo encuentran.
    /// </summary>
    private async Task<EcfTransmission> BeginTransmissionAsync(
        EcfDocument ecfDocument,
        DgiiTransmissionResult transmission,
        string signedXml,
        int attemptNumber = 1)
    {
        var record = new EcfTransmission
        {
            EcfDocumentId = ecfDocument.EcfDocumentId,
            TrackId = transmission.TrackId ?? string.Empty,
            AttemptNumber = attemptNumber,
            RequestPayload = signedXml,
            ResponsePayload = JsonSerializer.Serialize(new { transmission, status = (DgiiStatusResponse?)null }),
            EcfStatusId = 9,
            SentAtUtc = DateTime.UtcNow,
            ResponseCode = TrimTo(transmission.Codigo?.ToString() ?? string.Empty, 50),
            ResponseMessage = BuildTransmissionResponseMessage(transmission, null),
            Success = false
        };

        await _ecfTransmissionService.InsertAsync(record);
        await MarkDocumentAsync(ecfDocument, 9, $"Recibido por la DGII. TrackId: {transmission.TrackId}");

        return record;
    }

    /// <summary>Actualiza con el estado final la transmisión que <see cref="BeginTransmissionAsync"/> guardó.</summary>
    private async Task CompleteTransmissionAsync(
        EcfTransmission record,
        DgiiTransmissionResult transmission,
        int statusId,
        DgiiStatusResponse status)
    {
        record.EcfStatusId = statusId;
        record.ResponsePayload = JsonSerializer.Serialize(new { transmission, status });
        record.ResponseCode = TrimTo(status.Codigo ?? transmission.Codigo?.ToString() ?? string.Empty, 50);
        record.ResponseMessage = BuildTransmissionResponseMessage(transmission, status);
        record.Success = statusId == 10;

        await _ecfTransmissionService.UpdateAsync(record);
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

    private async Task AddDgiiResponseLogAsync(
        EcfDocument ecfDocument,
        int clientId,
        string operation,
        DgiiEnvironment environment,
        DgiiTransmissionResult transmission)
    {
        var level = transmission.Success ? "Information" : "Error";
        var summary = BuildDgiiTransmissionError(transmission);
        await AddLogAsync(
            ecfDocument,
            clientId,
            level,
            $"Respuesta DGII {operation} ({environment}): {summary}",
            JsonSerializer.Serialize(transmission));
    }

    private async Task AddDgiiStatusLogAsync(
        EcfDocument ecfDocument,
        int clientId,
        DgiiEnvironment environment,
        string trackId,
        DgiiStatusResponse status)
    {
        var level = string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) ? "Information" : "Error";
        await AddLogAsync(
            ecfDocument,
            clientId,
            level,
            $"Respuesta DGII consulta estado ({environment}) TrackId {trackId}: {BuildDgiiStatusError(status)}",
            JsonSerializer.Serialize(status));
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

    public static int MapDgiiStatusToEcfStatus(DgiiStatusResponse status)
    {
        if (string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase)) return 10;
        if (IsAcceptedConditionalDgiiStatus(status)) return 11;
        if (string.Equals(status.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase)) return 11;
        if (string.Equals(status.Estado, "Error", StringComparison.OrdinalIgnoreCase)) return 12;
        return 7;
    }

    public static bool IsPendingDgiiStatus(DgiiStatusResponse status)
    {
        if (string.IsNullOrWhiteSpace(status.Estado)) return true;
        return status.Estado.Equals("Recibido", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("En Proceso", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("Procesando", StringComparison.OrdinalIgnoreCase)
            || status.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAcceptedConditionalDgiiStatus(DgiiStatusResponse status) =>
        string.Equals(status.Estado, "Aceptado Condicional", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status.Estado, "Aceptado Condicionalmente", StringComparison.OrdinalIgnoreCase)
        || (status.Estado != null && status.Estado.Contains("condicionalmente", StringComparison.OrdinalIgnoreCase))
        || string.Equals(status.Codigo, "4", StringComparison.OrdinalIgnoreCase);

    public static bool RequiresCorrectionDgiiStatus(DgiiStatusResponse status) =>
        IsAcceptedConditionalDgiiStatus(status)
        || string.Equals(status.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase)
        || string.Equals(status.Estado, "Error", StringComparison.OrdinalIgnoreCase);

    private async Task<DgiiStatusResponse> WaitForFinalDgiiStatusAsync(
        DgiiEnvironment environment,
        string token,
        string trackId,
        CancellationToken cancellationToken)
    {
        var timeoutSeconds = Math.Clamp(
            _configuration.GetValue<int?>("EcfPerformance:FinalStatusTimeoutSeconds") ?? 60,
            1,
            300);
        var pollingIntervalMilliseconds = Math.Clamp(
            _configuration.GetValue<int?>("EcfPerformance:StatusPollingIntervalMilliseconds") ?? 500,
            100,
            5000);

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var pollingToken = timeoutSource.Token;

        DgiiStatusResponse? lastStatus = null;
        try
        {
            await Task.Delay(Math.Min(250, pollingIntervalMilliseconds), pollingToken);
            while (true)
            {
                lastStatus = await _transmissionService.GetStatusAsync(
                    environment,
                    token,
                    trackId,
                    pollingToken);

                if (!IsPendingDgiiStatus(lastStatus))
                    return lastStatus;

                await Task.Delay(pollingIntervalMilliseconds, pollingToken);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DgiiStatusResponse
            {
                TrackId = trackId,
                Codigo = lastStatus?.Codigo ?? string.Empty,
                Estado = "Pendiente",
                ENcf = lastStatus?.ENcf ?? string.Empty,
                SecuenciaUtilizada = lastStatus?.SecuenciaUtilizada ?? false,
                FechaRecepcion = lastStatus?.FechaRecepcion ?? string.Empty,
                Error = $"DGII no emitio un estado final dentro de {timeoutSeconds} segundos.",
                Mensaje = lastStatus?.Mensaje ?? "La recepcion fue confirmada, pero el resultado final no estuvo disponible a tiempo.",
                Mensajes = lastStatus?.Mensajes ?? []
            };
        }
    }

    internal static QrMetadata BuildQrMetadata(EcfDocument ecfDocument, string signedXml, string rncEmisor, DgiiEnvironment environment = DgiiEnvironment.Production)
    {
        var securityCode = ExtractSecurityCode(signedXml);
        var signatureDate = ExtractXmlValue(signedXml, "FechaHoraFirma");

        if (string.IsNullOrWhiteSpace(securityCode))
            securityCode = GenerateSecurityCode();
        if (string.IsNullOrWhiteSpace(signatureDate))
            signatureDate = ecfDocument.SignatureDateTime?.ToString("dd-MM-yyyy HH:mm:ss") ?? DateTime.Now.ToDrTime().ToString("dd-MM-yyyy HH:mm:ss");

        var qrUrl = BuildQrUrl(
            environment,
            NcfHelper.ExtractEcfType(ecfDocument.Ncf),
            rncEmisorRaw: rncEmisor,
            rncCompradorRaw: ecfDocument.CustomerRnc,
            encf: ecfDocument.Ncf,
            fechaEmision: ecfDocument.IssueDateUtc.ToString("dd-MM-yyyy"),
            montoTotal: ecfDocument.Total,
            fechaFirma: signatureDate,
            securityCode: securityCode);

        return new QrMetadata(securityCode, signatureDate, qrUrl);
    }

    private static void ApplyQrMetadata(ReceivedEcfEmissionResultDto resultDto, EcfInvoiceRequestDto dto, string signedXml, int ecfType, DgiiEnvironment environment = DgiiEnvironment.Production)
    {
        var securityCode = ExtractSecurityCode(signedXml);
        var signatureDate = ExtractXmlValue(signedXml, "FechaHoraFirma");

        if (string.IsNullOrWhiteSpace(securityCode))
            securityCode = dto.SecurityCodeOverride ?? GenerateSecurityCode();
        if (string.IsNullOrWhiteSpace(signatureDate))
            signatureDate = dto.ECF.FechaHoraFirma ?? dto.SignatureDateOverride?.ToString("dd-MM-yyyy HH:mm:ss") ?? DateTime.Now.ToDrTime().ToString("dd-MM-yyyy HH:mm:ss");

        var qrUrl = BuildQrUrl(
            environment,
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
        resultDto.QrImageUrl = string.Empty;
    }

    private static EcfXmlValidationResult BuildAcceptedValidationResult(
        EcfInvoiceRequestDto dto,
        string signedXml,
        int ecfType,
        string qrUrl,
        string securityCode,
        string signatureDate)
    {
        var header = dto.ECF.Encabezado;

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
                VerificationUrl = qrUrl
            }
        };
    }

    private static string BuildQrUrl(
        DgiiEnvironment environment,
        int ecfType,
        string rncEmisorRaw,
        string rncCompradorRaw,
        string encf,
        string fechaEmision,
        decimal montoTotal,
        string fechaFirma,
        string securityCode)
        => ZynstormECFPlatform.Core.Ecf.EcfQrUrlBuilder.Build(
            environment, ecfType, rncEmisorRaw, rncCompradorRaw, encf, fechaEmision, montoTotal, fechaFirma, securityCode);

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

    internal static string ExtractXmlValue(string xml, string localName)
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

    internal static string ExtractSecurityCode(string xml)
    {
        var rfceSecurityCode = ExtractXmlValue(xml, "CodigoSeguridadeCF");
        if (!string.IsNullOrWhiteSpace(rfceSecurityCode))
            return rfceSecurityCode;

        // La DGII toma los primeros 6 caracteres RAW del <SignatureValue> (base64).
        // No se eliminan los chars especiales (+, /, =) porque eso produciría un
        // CodigoSeguridad diferente al que la DGII registra internamente.
        // Solo se hace Trim() para remover saltos de línea del base64 multilínea.
        var signatureValue = ExtractXmlValue(xml, "SignatureValue")
            .Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim();
        return signatureValue.Length >= 6 ? signatureValue[..6] : signatureValue;
    }

    private static string OnlyDigits(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());
    }

    private static string GenerateSecurityCode()
    {
        // Fallback cuando aún no hay SignatureValue (pre-firma).
        // Se usa el Guid tal cual, sin alterar el case, para mantener consistencia
        // con la política: el CodigoSeguridad siempre se toma RAW sin modificar case.
        return Guid.NewGuid().ToString("N")[..6];
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
            if (IsOptionalXmlProdPath(path)) continue;

            if (!generatedPaths.Contains(path))
                errors.Add($"Elemento '{path}' (presente en referencia XmlProd) no se encuentra en el XML generado.");
        }

        return errors;
    }

    private static bool IsOptionalXmlProdPath(string path)
    {
        return path.StartsWith("ECF/DescuentosORecargos", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("RFCE/DescuentosORecargos", StringComparison.OrdinalIgnoreCase);
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

    private static bool ShouldSendAsB2cSummary(int ecfType, decimal total)
    {
        return ecfType == 32 && total < 250000m;
    }

    private static string BuildDgiiStatusError(DgiiStatusResponse status)
    {
        if (status.Mensajes == null || !status.Mensajes.Any()) return $"DGII: {status.Estado}";
        return $"DGII: {status.Estado} | {string.Join(" | ", status.Mensajes.Select(m => m.Valor))}";
    }

    private static string BuildDgiiTransmissionError(DgiiTransmissionResult result)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.TrackId)) parts.Add($"TrackId: {result.TrackId}");
        if (!string.IsNullOrWhiteSpace(result.Estado)) parts.Add($"DGII: {result.Estado}");
        if (!string.IsNullOrWhiteSpace(result.Error)) parts.Add(result.Error);
        if (!string.IsNullOrWhiteSpace(result.Mensaje)) parts.Add(result.Mensaje);
        if (result.Mensajes != null && result.Mensajes.Any())
            parts.AddRange(result.Mensajes.Where(m => !string.IsNullOrWhiteSpace(m.Valor)).Select(m => m.Valor));

        return parts.Count == 0 ? "Error en transmision" : string.Join(" | ", parts);
    }
}

public record QrMetadata(string SecurityCode, string SignatureDate, string QrUrl);
