using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Hangfire;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Data;
using Microsoft.AspNetCore.SignalR;
using ZynstormECFPlatform.Common.Hubs;
using ZynstormECFPlatform.Abstractions.DataServices;
using System.Text.Json;
using ZynstormECFPlatform.Common;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using EcfTypeEntity = ZynstormECFPlatform.Core.Entities.EcfType;

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public class OldCertificationSimulationService : IOldCertificationSimulationService
{
    private const string CertificationBuyerRnc = "133009889";
    private const string CertificationBuyerName = "TRANSPORTE NJ SRL";

    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IEncryptedService _encryptedService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly IDgiiAuthService _authService;
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IOldEcfGeneratorService _generatorService;
    private readonly IXmlSignatureService _signerService;
    private readonly IEcfTypeService _ecfTypeService;
    private readonly IENcfService _eNcfService;
    private readonly StorageContext _context;
    private readonly IHubContext<CertificationHub> _hubContext;

    private static readonly ConcurrentDictionary<string, CertificationJobStatusDto> _jobStatuses = new();

    public OldCertificationSimulationService(
        IClientService clientService,
        IApiKeyService apiKeyService,
        IEncryptedService encryptedService,
        IClientCertificateService clientCertificateService,
        IDgiiAuthService authService,
        IDgiiTransmissionService transmissionService,
        IOldEcfGeneratorService generatorService,
        IXmlSignatureService signerService,
        IEcfTypeService ecfTypeService,
        IENcfService eNcfService,
        IHubContext<CertificationHub> hubContext,
        StorageContext context)
    {
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _encryptedService = encryptedService;
        _clientCertificateService = clientCertificateService;
        _authService = authService;
        _transmissionService = transmissionService;
        _generatorService = generatorService;
        _signerService = signerService;
        _ecfTypeService = ecfTypeService;
        _eNcfService = eNcfService;
        _hubContext = hubContext;
        _context = context;
    }

    public async Task<string> EnqueueSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        BackgroundJob.Enqueue<IOldCertificationSimulationService>(x => x.ProcessSimulacionEcfJobAsync(dto, jobId, webRootPath));
        return jobId;
    }

    public async Task<string> EnqueueBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        Console.WriteLine($"[Simulation] Enqueueing job {jobId} for businessType {businessTypeGuidId}");
        BackgroundJob.Enqueue<IOldCertificationSimulationService>(x => x.ProcessBusinessSimulationJobAsync(businessTypeGuidId, clientGuidId, jobId, webRootPath));
        return jobId;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath)
    {
        await ProcessSimulacionEcfJobInternalAsync(dto, jobId, webRootPath);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath)
    {
        var client = await _clientService.GetByAsync(c => c.GuidId == clientGuidId) ?? throw new Exception("Cliente no encontrado.");
        var businessType = await _context.Set<BusinessType>().FirstOrDefaultAsync(b => b.GuidId == businessTypeGuidId) ?? throw new Exception("Tipo de negocio no encontrado.");

        var samples = await _context.Set<BusinessSimulationSample>()
            .Where(s => s.BusinessTypeId == businessType.BusinessTypeId)
            .ToDictionaryAsync(s => s.EcfType.ToString(), s => s.JsonData);

        var sampleIds = await _context.Set<BusinessSimulationSample>()
            .Where(s => s.BusinessTypeId == businessType.BusinessTypeId)
            .ToDictionaryAsync(s => s.EcfType.ToString(), s => s.BusinessSimulationSampleId);

        var mainBranch = await _context.Set<ClientBranche>().FirstOrDefaultAsync(b => b.ClientId == client.ClientId && b.IsMain);

        var initialDto = new OldEcfInvoiceRequestDto
        {
            IssuerRnc = client.Rnc,
            IssuerName = client.Name,
            IssuerAddress = mainBranch?.Address ?? "CALLE PRINCIPAL #1",
            ClientId = client.ClientId
        };

        await ProcessSimulacionEcfJobInternalAsync(initialDto, jobId, webRootPath, samples, sampleIds, businessType.BusinessTypeId);
    }

    [AutomaticRetry(Attempts = 0)]
    private async Task ProcessSimulacionEcfJobInternalAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath, Dictionary<string, string>? samples = null, Dictionary<string, int>? sampleIds = null, int? businessTypeId = null)
    {
        var accepted31Pool = new List<(string Ncf, DateTime IssueDate, string? CustomerRnc, OldEcfInvoiceRequestDto Dto)>();
        var rfcePool = new List<(string Ncf, string SecurityCode, OldEcfInvoiceRequestDto Dto, string SignedXml)>();

        if (!_jobStatuses.TryGetValue(jobId, out var status))
        {
            status = _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
        }
        status.Status = "Processing";
        await NotifyUpdate(jobId, status);

        try
        {
            var client = await _clientService.GetByAsync(c => c.Rnc == dto.IssuerRnc)
                ?? throw new Exception($"Cliente con RNC {dto.IssuerRnc} no encontrado.");

            var step4 = await _context.Set<CertificationStep>().FirstOrDefaultAsync(s => s.Order == 4);
            if (step4 == null)
            {
                step4 = new CertificationStep { Name = "Pruebas Simulación e-CF", Order = 4, IsRequired = true, RegisteredAt = DateTime.Now };
                _context.Set<CertificationStep>().Add(step4);
                await _context.SaveChangesAsync();
            }

            var process = await _context.Set<CertificationProcess>()
                .OrderByDescending(p => p.RegisteredAt)
                .FirstOrDefaultAsync(p => p.ClientId == client.ClientId &&
                                          (p.Status == CertificationStatus.Pending || p.Status == CertificationStatus.InProgress));

            if (process != null)
            {
                var docsToDelete = await _context.Set<CertificationDocument>()
                    .Where(d => d.CertificationProcessId == process.CertificationProcessId)
                    .ToListAsync();

                Console.WriteLine($"[Simulation] Deleting {docsToDelete.Count} old documents for process {process.CertificationProcessId}");
                if (docsToDelete.Any())
                {
                    _context.Set<CertificationDocument>().RemoveRange(docsToDelete);
                    await _context.SaveChangesAsync();
                }
                Console.WriteLine($"[Simulation] Old documents deleted. Next eNCF will be reserved from ENcf.Sequence.");
            }
            else
            {
                process = new CertificationProcess
                {
                    ClientId = client.ClientId,
                    Environment = DgiiEnvironment.CerteCF,
                    Status = CertificationStatus.InProgress,
                    StartDate = DateTime.Now,
                    CurrentStepId = step4.CertificationStepId,
                    RegisteredAt = DateTime.Now
                };
                _context.Set<CertificationProcess>().Add(process);
                await _context.SaveChangesAsync();
            }

            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId) ?? throw new Exception("ApiKey no encontrada.");
            var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
            var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId) ?? throw new Exception("Certificado no encontrado.");
            var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
            var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
            var certBase64 = Convert.ToBase64String(certificateBytes);
            var certPass = Encoding.UTF8.GetString(passwordBytes);

            Console.WriteLine($"[Simulation] Requesting DGII token for RNC {dto.IssuerRnc}...");
            string token = await _authService.GetTokenAsync(dto.IssuerRnc, DgiiEnvironment.CerteCF, certBase64, certPass);
            Console.WriteLine($"[Simulation] Token obtained successfully.");
            var matrix = new (int Type, int Count, bool IsSummary, bool IsManual, decimal? MinAmount, decimal? MaxAmount)[] {
                (31, 4, false, false, null, null),
                (33, 1, false, false, null, null),
                (34, 2, false, false, null, null),
                (32, 2, false, false, 250000, null),
                (41, 2, false, false, null, null),
                (43, 2, false, false, null, null),
                (44, 2, false, false, null, null),
                (45, 2, false, false, null, null),
                (46, 2, false, false, null, null),
                (47, 2, false, false, null, null),
                (32, 4, true, false, 10, 249000),
                (32, 4, false, true, 10, 249000)
            };

            Console.WriteLine($"[Simulation] Starting simulation loop with {matrix.Sum(m => m.Count)} documents...");

            status.TotalSteps = matrix.Sum(m => m.Count);
            status.CurrentStep = 0;

            // Initialize totals in stats
            status.SimulationStats.TotalType31 = matrix.Where(m => m.Type == 31).Sum(m => m.Count);
            status.SimulationStats.TotalType32Rfce = matrix.Where(m => m.Type == 32 && m.IsSummary).Sum(m => m.Count);
            status.SimulationStats.TotalType32Greater250k = matrix.Where(m => m.Type == 32 && !m.IsSummary && !m.IsManual).Sum(m => m.Count);
            status.SimulationStats.TotalType32Manual = matrix.Where(m => m.Type == 32 && m.IsManual).Sum(m => m.Count);
            status.SimulationStats.TotalType33 = matrix.Where(m => m.Type == 33).Sum(m => m.Count);
            status.SimulationStats.TotalType34 = matrix.Where(m => m.Type == 34).Sum(m => m.Count);
            status.SimulationStats.TotalType41 = matrix.Where(m => m.Type == 41).Sum(m => m.Count);
            status.SimulationStats.TotalType43 = matrix.Where(m => m.Type == 43).Sum(m => m.Count);
            status.SimulationStats.TotalType44 = matrix.Where(m => m.Type == 44).Sum(m => m.Count);
            status.SimulationStats.TotalType45 = matrix.Where(m => m.Type == 45).Sum(m => m.Count);
            status.SimulationStats.TotalType46 = matrix.Where(m => m.Type == 46).Sum(m => m.Count);
            status.SimulationStats.TotalType47 = matrix.Where(m => m.Type == 47).Sum(m => m.Count);

            var signatureDate = DateTimeExtensions.DrNow;
            var approvedXmls = new Dictionary<string, string>();
            var manualUploadXmls = new Dictionary<string, string>();
            var hasRejectedDocuments = false;
            var stopRequested = false;

            // 1. PROCESS AUTOMATIC ITEMS (Non-manual)
            foreach (var item in matrix.Where(m => !m.IsManual))
            {
                for (int i = 0; i < item.Count; i++)
                {
                    status.CurrentStep++;
                    Console.WriteLine($"[Simulation] Processing Step {status.CurrentStep}/{status.TotalSteps} - Type {item.Type} (Iteration {i + 1}/{item.Count})");

                    // Increment specific type counter
                    if (item.Type == 31) status.SimulationStats.Type31++;
                    else if (item.Type == 32)
                    {
                        if (item.IsSummary) status.SimulationStats.Type32Rfce++;
                        else status.SimulationStats.Type32Greater250k++;
                    }
                    else if (item.Type == 33) status.SimulationStats.Type33++;
                    else if (item.Type == 34) status.SimulationStats.Type34++;
                    else if (item.Type == 41) status.SimulationStats.Type41++;
                    else if (item.Type == 43) status.SimulationStats.Type43++;
                    else if (item.Type == 44) status.SimulationStats.Type44++;
                    else if (item.Type == 45) status.SimulationStats.Type45++;
                    else if (item.Type == 46) status.SimulationStats.Type46++;
                    else if (item.Type == 47) status.SimulationStats.Type47++;

                    var placeholderLog = new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = "Procesando...",
                        Status = "Enviando",
                        Message = $"Iniciando paso {status.CurrentStep} de {status.TotalSteps} (Tipo {item.Type})..."
                    };
                    status.CompletedSteps.Add(placeholderLog);
                    await NotifyUpdate(jobId, status);

                    OldEcfInvoiceRequestDto? indDtoForPool = null;
                    OldEcfInvoiceRequestDto currentDto = CloneDto(dto)!;

                    if (samples != null && samples.TryGetValue(item.Type.ToString(), out var sampleJson))
                    {
                        var specificDto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(sampleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (specificDto != null)
                        {
                            currentDto = specificDto;
                            currentDto.IssuerRnc = dto.IssuerRnc;
                            currentDto.IssuerName = dto.IssuerName;
                            currentDto.IssuerAddress = dto.IssuerAddress;
                            currentDto.ClientId = dto.ClientId;
                        }
                    }

                    currentDto.EcfType = item.Type;
                    currentDto.IssueDate = DateTime.Now.ToDrTime();
                    currentDto.IncomeType ??= "01";

                    if (item.Type == 31) currentDto.PaymentType ??= 2;
                    else currentDto.PaymentType ??= 1;

                    if (currentDto.PaymentType == 2)
                    {
                        currentDto.PaymentDeadline ??= DateTime.Now.AddDays(30);
                        currentDto.PaymentTerms ??= "30 DIAS";
                    }

                    currentDto.IssuerPhone ??= "809-555-1234";
                    currentDto.IssuerActivityCode ??= "6201";
                    currentDto.IssuerAddress ??= dto.IssuerAddress ?? "CALLE PRINCIPAL #1, SANTO DOMINGO";

                    if (item.Type is 31 or 32 or 33 or 34 or 41 or 44 or 45)
                    {
                        ApplyCertificationBuyer(currentDto);
                        if (string.IsNullOrEmpty(currentDto.CustomerAddress)) currentDto.CustomerAddress = "AV. CASANDRA DAMIRON #80";
                        if (string.IsNullOrEmpty(currentDto.CustomerTelephone)) currentDto.CustomerTelephone = "809-233-6060";
                    }

                    foreach (var itm in currentDto.Items)
                    {
                        // DGII acepta IndicadorBienoServicio = 2 (servicio) para todos los tipos
                        // en certificación; el tipo 47 además lo exige obligatoriamente.
                        itm.ItemType ??= 2;
                        itm.Description ??= itm.Name;
                        itm.UnitOfMeasure ??= 43;
                    }

                    currentDto.SequenceExpirationDate = new DateTime(DateTime.Now.Year + 2, 12, 31);
                    bool isNote = item.Type == 33 || item.Type == 34;

                    if (isNote)
                    {
                        int poolIndex = (item.Type == 33) ? i : (1 + i);
                        if (accepted31Pool.Count <= poolIndex) continue;
                        var reference = accepted31Pool[poolIndex];
                        currentDto.Items = CloneDto(reference.Dto)?.Items.Take(1).ToList() ?? new List<OldEcfItemRequestDto>();
                        foreach (var itm in currentDto.Items) { itm.Discount = 0; itm.ManualDescuentoMonto = null; }
                        currentDto.CustomerRnc = reference.Dto.CustomerRnc;
                        currentDto.CustomerName = reference.Dto.CustomerName;
                        currentDto.ReferenceNcf = reference.Ncf;
                        currentDto.ReferenceIssueDate = reference.IssueDate;
                        currentDto.ReferenceReasonCode = 3;
                        currentDto.ReferenceReasonDescription = "Ajuste parcial de montos";

                        // Las notas (33/34) NO llevan TerminoPago en IdDoc: el esquema XSD
                        // solo admite FechaDesde/FechaHasta/TotalPaginas. Forzamos contado
                        // y limpiamos los términos de pago que pudo traer la muestra.
                        currentDto.PaymentType = 1;
                        currentDto.PaymentDeadline = null;
                        currentDto.PaymentTerms = null;
                    }
                    else
                    {
                        decimal itemsTotal = currentDto.Items.Sum(itm => (itm.Quantity * itm.UnitPrice) - itm.Discount);
                        if (item.MinAmount.HasValue && itemsTotal < item.MinAmount.Value)
                        {
                            decimal scaleFactor = item.MinAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                            foreach (var itm in currentDto.Items) itm.UnitPrice = Math.Round(itm.UnitPrice * scaleFactor, 2);
                        }
                        else if (item.MaxAmount.HasValue && itemsTotal > item.MaxAmount.Value)
                        {
                            decimal scaleFactor = item.MaxAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                            foreach (var itm in currentDto.Items) itm.UnitPrice = Math.Round(itm.UnitPrice * scaleFactor, 2);
                        }

                        if (currentDto.Items.Any()) currentDto.Items[0].UnitPrice += status.CurrentStep;

                        switch (item.Type)
                        {
                            case 31: ApplyCertificationBuyer(currentDto); break;
                            case 32: ApplyCertificationBuyer(currentDto); currentDto.PaymentType = 1; break;
                            case 41: ApplyBuyer(currentDto, "00100325067", "ENRIQUE CAMILO SANTOS TAVAREZ"); break;
                            case 43: ApplyBuyer(currentDto, null, null); break;
                            case 44: ApplyBuyer(currentDto, "131098843", "ZONA FRANCA 6 DE NOVIEMBRE SRL"); break;
                            case 45: ApplyBuyer(currentDto, "401506459", "PLAN DE ASISTENCIA SOCIAL DE LA PRESIDENCIA"); break;
                            case 46: ApplyExportDefaults(currentDto, i); break;
                            case 47: ApplyForeignPaymentDefaults(currentDto, i); break;
                        }
                    }

                    if (item.IsSummary)
                    {
                        currentDto.CustomerRnc = null;
                        currentDto.CustomerName = "CONSUMIDOR FINAL";
                    }

                    currentDto.SignatureDateOverride = signatureDate;
                    var ecfTypeRecord = await _ecfTypeService.GetByAsync(t => t.Code == item.Type.ToString());
                    (ENcf encfRecord, string reservedNcf) = await ReserveNextNcfAsync(client.ClientId, ecfTypeRecord!, item.Type);
                    currentDto.Ncf = reservedNcf;

                    if (item.IsSummary)
                    {
                        NormalizeRfceIndividualDto(currentDto);
                    }

                    bool isAccepted = false;
                    string? trackId = null;
                    string signedXml = string.Empty;
                    string securityCode = string.Empty;
                    string xmlFileName = $"Paso_{status.CurrentStep}_{currentDto.Ncf}.xml";
                    decimal total = currentDto.ManualMontoTotal ?? CalculateTransmissionTotal(currentDto);
                    string resultMessage = "Error en transmision";
                    string signedIndividualXmlForPool = string.Empty;

                    // Reintento por "secuencia ya utilizada": si DGII rechaza el eNCF por estar
                    // duplicado, avanzamos la secuencia y reenviamos el MISMO comprobante.
                    // Se intenta 3 veces en total (envío inicial + 2 reintentos). Si aún falla,
                    // queda rechazado y el flujo se detiene notificando el error.
                    const int MaxSequenceAttempts = 3;
                    int sequenceAttempt = 1;
                    bool retryWithNewSequence;

                    do
                    {
                        retryWithNewSequence = false;
                        isAccepted = false;
                        trackId = null;
                        securityCode = string.Empty;
                        signedIndividualXmlForPool = string.Empty;
                        xmlFileName = $"Paso_{status.CurrentStep}_{currentDto.Ncf}.xml";

                        if (item.IsSummary)
                        {
                            indDtoForPool = PrepareRfceIndividualDto(currentDto, signatureDate, certBase64, certPass);
                            string indUnsignedXml = _generatorService.GenerateUnsignedXml(indDtoForPool, false);
                            signedIndividualXmlForPool = _signerService.SignXml(indUnsignedXml, certBase64, certPass);
                            securityCode = ExtractSecurityCodeFromSignature(signedIndividualXmlForPool);
                            currentDto.SecurityCodeOverride = securityCode;
                            indDtoForPool.SecurityCodeOverride = securityCode;
                        }

                        string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);
                        LogGeneratedXmlDiagnostics(jobId, status.CurrentStep, currentDto.Ncf, item.Type, xmlFileName, unsignedXml, item.IsSummary);
                        signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                        if (string.IsNullOrWhiteSpace(securityCode))
                        {
                            securityCode = ExtractSecurityCodeFromSignature(signedXml);
                        }

                        // Validación XSD contra los esquemas de la DGII ANTES de enviar.
                        // Un XML inválido por esquema no se resuelve reintentando: se detiene.
                        var xsdErrors = _generatorService.ValidateXmlAgainstSchema(signedXml, item.Type);
                        if (xsdErrors.Any(e => e.StartsWith("[ERROR]") || e.StartsWith("[XML malformado]")))
                        {
                            resultMessage = "El formato del XML no es válido (XSD): " + string.Join(" | ", xsdErrors);
                            Console.WriteLine($"[Simulation] XSD inválido en paso {status.CurrentStep} ({currentDto.Ncf}): {resultMessage}");
                            break;
                        }

                        var result = await _transmissionService.SendEcfAsync(DgiiEnvironment.CerteCF, token, signedXml, item.Type, total, dto.IssuerRnc, currentDto.Ncf, item.IsSummary);
                        trackId = result.TrackId;

                        if (result.Success)
                        {
                            if (!string.IsNullOrEmpty(result.TrackId))
                            {
                                await Task.Delay(2000);
                                var finalStatus = await PollDgiiStatusAsync(result.TrackId, dto.IssuerRnc);
                                isAccepted = finalStatus.Estado == "Aceptado" || (item.IsSummary && finalStatus.Estado == "Generado");
                                resultMessage = isAccepted ? $"TrackId: {trackId}" : BuildDgiiStatusError(finalStatus);
                            }
                            else
                            {
                                isAccepted = string.Equals(result.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase)
                                    || (item.IsSummary && string.Equals(result.Estado, "Generado", StringComparison.OrdinalIgnoreCase))
                                    || result.Codigo is 0 or 1;
                                resultMessage = isAccepted
                                    ? $"DGII: {result.Estado ?? "Aceptado"}"
                                    : BuildDgiiTransmissionError(result);
                            }
                        }
                        else
                        {
                            resultMessage = BuildDgiiTransmissionError(result);
                        }

                        // Secuencia ya utilizada -> avanzar secuencia y reenviar el mismo comprobante.
                        if (!isAccepted && IsSequenceAlreadyUsed(resultMessage) && sequenceAttempt < MaxSequenceAttempts)
                        {
                            sequenceAttempt++;
                            (encfRecord, reservedNcf) = await ReserveNextNcfAsync(client.ClientId, ecfTypeRecord!, item.Type);
                            currentDto.Ncf = reservedNcf;
                            Console.WriteLine($"[Simulation] Secuencia ya utilizada en paso {status.CurrentStep}; intento {sequenceAttempt}/{MaxSequenceAttempts} con nuevo eNCF {reservedNcf}.");
                            retryWithNewSequence = true;
                        }
                    }
                    while (retryWithNewSequence);

                    if (isAccepted)
                    {
                        approvedXmls[xmlFileName] = signedXml;
                        if (item.Type == 31) accepted31Pool.Add((currentDto.Ncf, currentDto.IssueDate, currentDto.CustomerRnc, CloneDto(currentDto)!));
                        if (item.IsSummary) rfcePool.Add((currentDto.Ncf, securityCode, indDtoForPool ?? CloneDto(currentDto)!, signedIndividualXmlForPool));
                    }
                    else
                    {
                        hasRejectedDocuments = true;
                    }

                    // PERSISTENCE: Save to CertificationDocument table
                    var certDoc = new CertificationDocument
                    {
                        CertificationProcessId = process.CertificationProcessId,
                        ENcfSecuence = currentDto.Ncf,
                        ENcfId = encfRecord.ENcfId,
                        EcfTypeId = ecfTypeRecord.EcfTypeId,
                        XmlSent = signedXml,
                        TrackId = trackId,
                        Status = isAccepted ? DocumentStatus.Accepted : DocumentStatus.Rejected,
                        SentAt = DateTimeExtensions.DrNow,
                        RegisteredAt = DateTimeExtensions.DrNow,
                        GuidId = Guid.NewGuid().ToString()
                    };
                    _context.Set<CertificationDocument>().Add(certDoc);
                    await _context.SaveChangesAsync();

                    var existingLog = status.CompletedSteps.FirstOrDefault(l => l.Index == status.CurrentStep);
                    if (existingLog != null) status.CompletedSteps.Remove(existingLog);

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = currentDto.Ncf,
                        Step = item.Type.ToString(),
                        Status = isAccepted ? "Aceptado" : "Rechazado",
                        Message = resultMessage,
                        Amount = total,
                        SecurityCode = securityCode,
                        FechaFirma = signatureDate.ToString("dd-MM-yyyy hh:mm:ss tt"),
                        FechaEmision = currentDto.IssueDate.ToString("dd-MM-yyyy"),
                        BuyerRnc = currentDto.CustomerRnc,
                        XmlFileName = xmlFileName
                    });

                    if (approvedXmls.Any())
                    {
                        await CreateSimulationZipAsync(jobId, webRootPath, approvedXmls, status, "aprobados");
                    }

                    await NotifyUpdate(jobId, status);

                    // Validar que fue aceptado para continuar: si se rechazó (XSD o DGII),
                    // detenemos el proceso en vez de seguir enviando los demás comprobantes.
                    if (!isAccepted)
                    {
                        stopRequested = true;
                        break;
                    }
                }

                if (stopRequested) break;
            }

            // 2. CREATE INTERMEDIATE ZIP (Ready for user to download while manual ones run)
            if (approvedXmls.Any())
            {
                await CreateSimulationZipAsync(jobId, webRootPath, approvedXmls, status, "aprobados");
                await NotifyUpdate(jobId, status);
            }

            // 3. PROCESS MANUAL ITEMS (Step 4 documents that must be uploaded manually)
            foreach (var item in matrix.Where(m => m.IsManual))
            {
                if (stopRequested) break;
                for (int i = 0; i < item.Count; i++)
                {
                    status.CurrentStep++;
                    status.SimulationStats.Type32Manual++;

                    var placeholderLog = new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = "Generando...",
                        Status = "Procesando",
                        Message = $"Iniciando paso {status.CurrentStep} de {status.TotalSteps} (Tipo {item.Type} Manual)..."
                    };
                    status.CompletedSteps.Add(placeholderLog);
                    await NotifyUpdate(jobId, status);

                    if (rfcePool.Count == 0)
                    {
                        var existingManualLog = status.CompletedSteps.FirstOrDefault(l => l.Index == status.CurrentStep);
                        if (existingManualLog != null) status.CompletedSteps.Remove(existingManualLog);

                        status.CompletedSteps.Add(new CertificationStepResultDto
                        {
                            Index = status.CurrentStep,
                            Ncf = string.Empty,
                            Status = "Pendiente",
                            Message = "No hay RFCE aceptados por DGII para generar este XML manual."
                        });

                        hasRejectedDocuments = true;
                        await NotifyUpdate(jobId, status);
                        continue;
                    }

                    var pooled = rfcePool[i % rfcePool.Count];
                    var currentDto = CloneDto(pooled.Dto)!;
                    currentDto.Ncf = pooled.Ncf;
                    currentDto.SecurityCodeOverride = pooled.SecurityCode;
                    currentDto.SignatureDateOverride = signatureDate;

                    string xmlFileName = $"SUBIR_DGII_Paso_{status.CurrentStep}_{currentDto.Ncf}.xml";
                    string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, false);
                    LogGeneratedXmlDiagnostics(jobId, status.CurrentStep, currentDto.Ncf, item.Type, xmlFileName, unsignedXml, false);
                    string signedXml = string.IsNullOrWhiteSpace(pooled.SignedXml)
                        ? _signerService.SignXml(unsignedXml, certBase64, certPass)
                        : pooled.SignedXml;

                    manualUploadXmls[xmlFileName] = signedXml;

                    // PERSISTENCE: Save manual documents too
                    var ecfTypeRecordManual = await _ecfTypeService.GetByAsync(t => t.Code == item.Type.ToString());
                    var encfRecordManual = await _context.Set<ENcf>().FirstOrDefaultAsync(e => e.ClientId == client.ClientId && e.NcfTypeId == ecfTypeRecordManual!.EcfTypeId);

                    var certDocManual = new CertificationDocument
                    {
                        CertificationProcessId = process.CertificationProcessId,
                        ENcfSecuence = currentDto.Ncf,
                        ENcfId = encfRecordManual?.ENcfId ?? 0,
                        EcfTypeId = ecfTypeRecordManual?.EcfTypeId ?? 0,
                        XmlSent = signedXml,
                        TrackId = "MANUAL",
                        Status = DocumentStatus.Accepted,
                        SentAt = DateTimeExtensions.DrNow,
                        RegisteredAt = DateTimeExtensions.DrNow,
                        GuidId = Guid.NewGuid().ToString()
                    };
                    _context.Set<CertificationDocument>().Add(certDocManual);
                    await _context.SaveChangesAsync();

                    var existingLog = status.CompletedSteps.FirstOrDefault(l => l.Index == status.CurrentStep);
                    if (existingLog != null) status.CompletedSteps.Remove(existingLog);

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = currentDto.Ncf,
                        Step = item.Type.ToString(),
                        Status = "GeneradoManual",
                        Message = "XML generado para subir directo a DGII",
                        Amount = CalculateTransmissionTotal(currentDto),
                        SecurityCode = pooled.SecurityCode,
                        FechaFirma = signatureDate.ToString("dd-MM-yyyy hh:mm:ss tt"),
                        FechaEmision = currentDto.IssueDate.ToString("dd-MM-yyyy"),
                        BuyerRnc = currentDto.CustomerRnc,
                        XmlFileName = xmlFileName
                    });

                    await CreateSimulationZipAsync(jobId, webRootPath, manualUploadXmls, status, "subir_dgii", setAsPrimaryDownload: false);
                    await NotifyUpdate(jobId, status);
                }
            }

            process.Status = CertificationStatus.InProgress;
            process.CurrentStepId = step4.CertificationStepId;
            process.EndDate = null;
            status.Status = hasRejectedDocuments ? "CompletedWithErrors" : "Completed";

            if (approvedXmls.Any())
            {
                await CreateSimulationZipAsync(jobId, webRootPath, approvedXmls, status, "aprobados");
            }

            await _context.SaveChangesAsync();
            await NotifyUpdate(jobId, status);
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
            await NotifyUpdate(jobId, status);
            throw;
        }
    }

    private async Task CreateSimulationZipAsync(string jobId, string webRootPath, Dictionary<string, string> simulationXmls, CertificationJobStatusDto status, string suffix, bool setAsPrimaryDownload = true)
    {
        try
        {
            string zipDir = Path.Combine(webRootPath, "certification_files");
            if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);
            string zipFileName = $"simulacion_{suffix}_{jobId}.zip";
            string zipPath = Path.Combine(zipDir, zipFileName);
            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                foreach (var entry in simulationXmls)
                {
                    var zipEntry = archive.CreateEntry(entry.Key, System.IO.Compression.CompressionLevel.Optimal);
                    using (var entryStream = zipEntry.Open())
                    using (var writer = new StreamWriter(entryStream)) writer.Write(entry.Value);
                }
            }
            if (setAsPrimaryDownload)
            {
                status.DownloadUrl = $"/certification_files/{zipFileName}";
                status.ApprovedXmlZipUrl = status.DownloadUrl;
            }
            else
            {
                status.JsonDownloadUrl = $"/certification_files/{zipFileName}";
                status.ManualXmlZipUrl = status.JsonDownloadUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Simulation] Error creating ZIP for job {jobId}: {ex.Message}");
        }
    }

    public async Task<CertificationJobStatusDto> GetLastSimulationResultsByClientAsync(string clientGuidId)
    {
        var client = await _clientService.GetByAsync(c => c.GuidId == clientGuidId);
        if (client == null)
        {
            Console.WriteLine($"[Simulation] Client not found: {clientGuidId}");
            return new CertificationJobStatusDto { Status = "NotFound" };
        }

        Console.WriteLine($"[Simulation] Searching last results for ClientId: {client.ClientId}");

        var processes = await _context.Set<CertificationProcess>()
            .Include(p => p.CertificationDocuments)
                .ThenInclude(d => d.EcfType)
            .Where(p => p.ClientId == client.ClientId)
            .OrderByDescending(p => p.RegisteredAt)
            .ToListAsync();

        Console.WriteLine($"[Simulation] Found {processes.Count} processes for client.");

        var process = processes.FirstOrDefault(p => p.CertificationDocuments.Any());

        if (process == null)
        {
            Console.WriteLine("[Simulation] No process found with documents.");
            return new CertificationJobStatusDto { Status = "NotFound" };
        }

        Console.WriteLine($"[Simulation] Using process {process.CertificationProcessId} (Step {process.CurrentStepId}) with {process.CertificationDocuments.Count} documents.");

        var completedSteps = new List<CertificationStepResultDto>();
        foreach (var d in process.CertificationDocuments)
        {
            try
            {
                var doc = XDocument.Parse(d.XmlSent);
                var total = GetDecimal(doc, "Encabezado", "Totales", "MontoTotal") ?? 0m;
                var securityCode = ExtractSecurityCodeFromSignature(d.XmlSent);
                var signatureDateStr = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "FechaHoraFirma")?.Value;
                var issueDateStr = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "FechaEmision")?.Value;
                var buyerRnc = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "RNCComprador")?.Value;

                completedSteps.Add(new CertificationStepResultDto
                {
                    Index = completedSteps.Count + 1,
                    Ncf = d.ENcfSecuence,
                    Step = d.EcfType?.Code ?? "Unknown",
                    Status = d.TrackId == "MANUAL" ? "GeneradoManual" : (d.Status == DocumentStatus.Accepted ? "Aceptado" : "Rechazado"),
                    Message = d.TrackId == "MANUAL" ? "Manual" : $"TrackId: {d.TrackId}",
                    XmlFileName = d.TrackId == "MANUAL" ? $"SUBIR_DGII_{d.ENcfSecuence}.xml" : $"{d.ENcfSecuence}.xml",
                    Amount = total,
                    SecurityCode = securityCode,
                    FechaFirma = signatureDateStr,
                    FechaEmision = issueDateStr,
                    BuyerRnc = buyerRnc
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Simulation] Error parsing historical document {d.ENcfSecuence}: {ex.Message}");
            }
        }

        if (!completedSteps.Any())
            return new CertificationJobStatusDto { Status = "NotFound" };

        var status = new CertificationJobStatusDto
        {
            JobId = $"DB_{process.CertificationProcessId}",
            Status = "Completed", // If it has documents in DB, we consider it completed or at least partially done
            TotalSteps = 29,
            CurrentStep = completedSteps.Count,
            CompletedSteps = completedSteps.OrderBy(x => x.Index).ToList(),
            SimulationStats = new SimulationStatsDto
            {
                Type31 = completedSteps.Count(x => x.Step == "31"),
                Type32Greater250k = completedSteps.Count(x => x.Step == "32" && x.Amount >= 250000),
                Type32Rfce = completedSteps.Count(x => x.Step == "32" && x.Amount < 250000 && x.Message != "Manual"),
                Type32Manual = completedSteps.Count(x => x.Step == "32" && x.Message == "Manual"),
                Type33 = completedSteps.Count(x => x.Step == "33"),
                Type34 = completedSteps.Count(x => x.Step == "34"),
                Type41 = completedSteps.Count(x => x.Step == "41"),
                Type43 = completedSteps.Count(x => x.Step == "43"),
                Type44 = completedSteps.Count(x => x.Step == "44"),
                Type45 = completedSteps.Count(x => x.Step == "45"),
                Type46 = completedSteps.Count(x => x.Step == "46"),
                Type47 = completedSteps.Count(x => x.Step == "47")
            }
        };

        return status;
    }

    public async Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId)
    {
        if (_jobStatuses.TryGetValue(jobId, out var status)) return status;
        return new CertificationJobStatusDto { JobId = jobId, Status = "NotFound" };
    }

    public async Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId)
    {
        if (_jobStatuses.TryGetValue(jobId, out var status)) return status.CompletedSteps;
        return new List<CertificationStepResultDto>();
    }

    private async Task NotifyUpdate(string jobId, CertificationJobStatusDto status)
    {
        await _hubContext.Clients.Group($"cert-job:{jobId}").SendAsync("ReceiveJobUpdate", status);
        await _hubContext.Clients.Group($"cert-job:{jobId}").SendAsync("SimulationJobUpdate", status);
        await _hubContext.Clients.Group(jobId).SendAsync("ReceiveJobUpdate", status);
        await _hubContext.Clients.Group(jobId).SendAsync("SimulationJobUpdate", status);
    }

    private static void LogGeneratedXmlDiagnostics(string jobId, int step, string ncf, int ecfType, string xmlFileName, string unsignedXml, bool isRfce)
    {
        try
        {
            var doc = XDocument.Parse(unsignedXml);
            var rootName = doc.Root?.Name.LocalName ?? "Unknown";
            var headerMontoExento = GetDecimal(doc, "Encabezado", "Totales", "MontoExento");
            var headerMontoGravadoTotal = GetDecimal(doc, "Encabezado", "Totales", "MontoGravadoTotal");
            var headerMontoTotal = GetDecimal(doc, "Encabezado", "Totales", "MontoTotal") ?? 0m;

            var exemptItemsTotal = doc.Descendants()
                .Where(x => x.Name.LocalName == "Item" &&
                            GetInt(x, "IndicadorFacturacion") == 4)
                .Sum(item =>
                {
                    var quantity = GetDecimal(item, "CantidadItem") ?? 0m;
                    var unitPrice = GetDecimal(item, "PrecioUnitarioItem") ?? 0m;
                    var discount = GetDecimal(item, "DescuentoMonto") ?? 0m;
                    var surcharge = GetDecimal(item, "RecargoMonto") ?? 0m;
                    return Math.Round((quantity * unitPrice) - discount + surcharge, 2);
                });

            foreach (var adjustment in doc.Descendants().Where(x => x.Name.LocalName == "DescuentoORecargo" &&
                                                                    GetInt(x, "IndicadorFacturacionDescuentooRecargo") == 4))
            {
                var amount = GetDecimal(adjustment, "MontoDescuentooRecargo") ?? 0m;
                var type = adjustment.Elements().FirstOrDefault(x => x.Name.LocalName == "TipoAjuste")?.Value;
                exemptItemsTotal += string.Equals(type, "R", StringComparison.OrdinalIgnoreCase) ? amount : -amount;
            }

            exemptItemsTotal = Math.Round(exemptItemsTotal, 2);
            var headerExempt = Math.Round(headerMontoExento ?? 0m, 2);
            var isExemptMismatch = (headerMontoExento.HasValue || exemptItemsTotal > 0m) && headerExempt != exemptItemsTotal;

            Console.WriteLine(
                "[Simulation][XML Totals] Job={0} Step={1} File={2} Root={3} Type={4} NCF={5} IsRfce={6} MontoExentoHeader={7} MontoExentoDetalle={8} MontoGravadoTotal={9} MontoTotal={10} ExemptMismatch={11}",
                jobId,
                step,
                xmlFileName,
                rootName,
                ecfType,
                ncf,
                isRfce,
                headerMontoExento?.ToString("0.00", CultureInfo.InvariantCulture) ?? "(null)",
                exemptItemsTotal.ToString("0.00", CultureInfo.InvariantCulture),
                headerMontoGravadoTotal?.ToString("0.00", CultureInfo.InvariantCulture) ?? "(null)",
                headerMontoTotal.ToString("0.00", CultureInfo.InvariantCulture),
                isExemptMismatch);

            if (isExemptMismatch)
            {
                Console.WriteLine(
                    "[Simulation][XML Totals][ERROR] Job={0} Step={1} File={2} NCF={3} Campo=MontoExento Header={4} DetalleEsperado={5}",
                    jobId,
                    step,
                    xmlFileName,
                    ncf,
                    headerExempt.ToString("0.00", CultureInfo.InvariantCulture),
                    exemptItemsTotal.ToString("0.00", CultureInfo.InvariantCulture));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Simulation][XML Totals] Error validating generated XML for job {jobId}, step {step}, NCF {ncf}: {ex.Message}");
        }
    }

    private static decimal? GetDecimal(XContainer container, params string[] path)
    {
        var element = FindElement(container, path);
        return ParseDecimal(element?.Value);
    }

    private static int? GetInt(XContainer container, string localName)
    {
        var value = container.Descendants().FirstOrDefault(x => x.Name.LocalName == localName)?.Value;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private static XElement? FindElement(XContainer container, params string[] path)
    {
        XElement? current = container switch
        {
            XDocument doc => doc.Root,
            XElement element => element,
            _ => null
        };

        foreach (var segment in path)
        {
            current = current?.Elements().FirstOrDefault(x => x.Name.LocalName == segment);
            if (current == null) return null;
        }

        return current;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    private T? CloneDto<T>(T obj)
    {
        if (obj == null) return default;
        var json = JsonSerializer.Serialize(obj);
        return JsonSerializer.Deserialize<T>(json);
    }

    private void ApplyCertificationBuyer(OldEcfInvoiceRequestDto dto)
    {
        dto.CustomerRnc = CertificationBuyerRnc;
        dto.CustomerName = CertificationBuyerName;
    }

    private void ApplyBuyer(OldEcfInvoiceRequestDto dto, string? rnc, string? name)
    {
        dto.CustomerRnc = rnc;
        dto.CustomerName = name;
    }

    private void ApplyCertificationDiscount(OldEcfInvoiceRequestDto dto)
    {
        dto.GlobalDiscountAmount = 100;
        dto.GlobalDiscountDescription = "DESCUENTO DE PRUEBA";
    }

    private void ApplyExportDefaults(OldEcfInvoiceRequestDto dto, int iteration)
    {
        dto.CustomerRnc = null;
        dto.CustomerName = iteration == 0 ? "GLOBAL LOGISTICS INC" : "EURO TRADE GMBH";
        dto.TranspPaisDestino = iteration == 0 ? "USA" : "DEU";
    }

    private void ApplyForeignPaymentDefaults(OldEcfInvoiceRequestDto dto, int iteration)
    {
        dto.CustomerRnc = null;
        dto.CustomerName = iteration == 0 ? "SOFTWARE SOLUTIONS LTD" : "CLOUD SERVICES CORP";
        dto.CustomerForeignId ??= iteration == 0 ? "533445888" : "778899001";
        dto.CustomerCountry ??= iteration == 0 ? "USA" : "CAN";
        dto.CurrencyTipoMoneda ??= "USD";
        dto.CurrencyTipoCambio ??= 60.0000m;
        dto.GlobalDiscountAmount = 0;
        dto.GlobalDiscountDescription = null;

        foreach (var item in dto.Items)
        {
            // Tipo 47 (Pagos al Exterior): DGII solo permite IndicadorBienoServicio = 2 (servicio).
            item.ItemType = 2;
            item.BillingIndicator = 4;
            item.TaxPercentage = 0;
            item.ItbisAmount = 0;
            item.Discount = Math.Round(item.Discount, 2);
            item.ManualDescuentoMonto = null;
            item.ManualMontoItem = Math.Round((item.Quantity * item.UnitPrice) - item.Discount + (item.ManualRecargoMonto ?? 0m), 2);
            item.ManualMontoITBISRetenido = null;
        }

        var exemptTotal = Math.Round(dto.Items.Sum(item =>
            (item.Quantity * item.UnitPrice) - item.Discount + (item.ManualRecargoMonto ?? 0m)), 2);

        dto.ManualMontoGravadoTotal = null;
        dto.ManualMontoGravadoI1 = null;
        dto.ManualMontoGravadoI2 = null;
        dto.ManualMontoGravadoI3 = null;
        dto.ManualMontoExento = exemptTotal > 0m ? exemptTotal : null;
        dto.ManualMontoNoFacturable = null;
        dto.ManualTotalITBIS = null;
        dto.ManualTotalITBIS1 = null;
        dto.ManualTotalITBIS2 = null;
        dto.ManualTotalITBIS3 = null;
        dto.ManualMontoImpuestoAdicional = null;
        dto.ManualMontoTotal = exemptTotal;
        dto.ManualMontoPeriodo = null;
        dto.ManualValorPagar = null;
        dto.ManualIndicadorMontoGravado = null;
        dto.ManualTotalITBISRetenido = null;

        if (dto.CurrencyTipoCambio is > 0m)
        {
            dto.CurrencyMontoGravado = null;
            dto.CurrencyMontoExento = Math.Round(exemptTotal / dto.CurrencyTipoCambio.Value, 2);
            dto.CurrencyTotalITBIS = null;
            dto.CurrencyMontoTotal = dto.CurrencyMontoExento;
        }
    }

    private void ApplyForeignPaymentRetentionAndCurrency(OldEcfInvoiceRequestDto dto)
    {
        ApplyForeignPaymentDefaults(dto, 0);
        var retentionBase = dto.ManualMontoTotal ?? 0m;
        dto.ManualTotalISRRetencion = Math.Round(retentionBase * 0.10m, 2);

        foreach (var item in dto.Items)
        {
            var lineBase = Math.Round((item.Quantity * item.UnitPrice) - item.Discount + (item.ManualRecargoMonto ?? 0m), 2);
            item.ManualMontoISRRetenido = Math.Round(lineBase * 0.10m, 2);
        }
    }

    private decimal CalculateTransmissionTotal(OldEcfInvoiceRequestDto dto)
    {
        return dto.Items.Sum(itm => (itm.Quantity * itm.UnitPrice) - itm.Discount + itm.ItbisAmount);
    }

    private static string ExtractSecurityCodeFromSignature(string signedXml)
    {
        const string tag = "<SignatureValue>";
        int start = signedXml.IndexOf(tag, StringComparison.Ordinal);
        if (start == -1) return string.Empty;

        // CodigoSeguridad: primeros 6 chars RAW del base64 (case-sensitive, sin ToUpper).
        // El base64 puede ser multilínea — se limpian todos los whitespace antes de cortar.
        var content = signedXml[(start + tag.Length)..]
            .Replace("\n", "").Replace("\r", "").Replace(" ", "").Trim();
        return content.Length >= 6 ? content[..6] : string.Empty;
    }

    private bool IsDuplicateSequenceError(string? error)
    {
        if (string.IsNullOrEmpty(error)) return false;
        return error.Contains("ya ha sido utilizado") || error.Contains("1701");
    }

    // Detecta el rechazo de DGII por eNCF duplicado ("Este número de secuencia ya ha sido utilizado").
    private static bool IsSequenceAlreadyUsed(string? message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var m = message.ToLowerInvariant();
        return m.Contains("secuencia") && m.Contains("utilizad");
    }

    private string BuildDgiiStatusError(DgiiStatusResponse status)
    {
        if (status.Mensajes == null || !status.Mensajes.Any()) return $"DGII: {status.Estado}";
        return $"DGII: {status.Estado} | {string.Join(" | ", status.Mensajes.Select(m => m.Valor))}";
    }

    private string BuildDgiiTransmissionError(DgiiTransmissionResult result)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(result.Estado)) parts.Add($"DGII: {result.Estado}");
        if (!string.IsNullOrWhiteSpace(result.Error)) parts.Add(result.Error);
        if (!string.IsNullOrWhiteSpace(result.Mensaje)) parts.Add(result.Mensaje);
        if (result.Mensajes != null && result.Mensajes.Any())
            parts.AddRange(result.Mensajes.Where(m => !string.IsNullOrWhiteSpace(m.Valor)).Select(m => m.Valor));

        return parts.Count == 0 ? "Error en transmision" : string.Join(" | ", parts);
    }

    private async Task<DgiiStatusResponse> PollDgiiStatusAsync(string trackId, string rncEmisor)
    {
        var apiKey = await _apiKeyService.GetByAsync(x => x.Client.Rnc == rncEmisor) ?? throw new Exception("ApiKey no encontrada para poll.");
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        var certificate = await _clientCertificateService.GetByAsync(x => x.Client.Rnc == rncEmisor) ?? throw new Exception("Certificado no encontrado para poll.");
        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
        var certBase64 = Convert.ToBase64String(certificateBytes);
        var certPass = Encoding.UTF8.GetString(passwordBytes);

        string token = await _authService.GetTokenAsync(rncEmisor, DgiiEnvironment.CerteCF, certBase64, certPass);
        return await _transmissionService.GetStatusAsync(DgiiEnvironment.CerteCF, token, trackId);
    }

    private async Task<(ENcf Record, string Ncf)> ReserveNextNcfAsync(int clientId, EcfTypeEntity type, int typeCode, int jump = 0)
    {
        const int MinSequence = 1;
        const int MaxSequence = 10_000_000;

        var encf = await _context.Set<ENcf>()
            .FirstOrDefaultAsync(e => e.ClientId == clientId && e.NcfTypeId == type.EcfTypeId);

        // Si el cliente no tiene secuencia configurada para este tipo, la creamos
        // iniciando desde 1 en vez de fallar. Los comprobantes van de 1 a 10,000,000.
        if (encf == null)
        {
            encf = new ENcf
            {
                ClientId = clientId,
                NcfTypeId = type.EcfTypeId,
                Sequence = MinSequence,
                GuidId = Guid.NewGuid().ToString(),
                RegisteredAt = DateTimeExtensions.DrNow
            };
            await _context.Set<ENcf>().AddAsync(encf);
        }

        if (jump > 0) encf.Sequence += jump;

        // Nunca reutilizar un eNCF: la secuencia siempre arranca por encima del mayor
        // número ya registrado para este cliente y tipo (incluye los consumidos por otros
        // flujos como el de Excel). Cada comprobante enviado quema su número de forma durable.
        var prefix = $"E{typeCode}";
        var usedSequences = await (
            from d in _context.Set<CertificationDocument>()
            join p in _context.Set<CertificationProcess>() on d.CertificationProcessId equals p.CertificationProcessId
            where p.ClientId == clientId && d.ENcfSecuence.StartsWith(prefix)
            select d.ENcfSecuence
        ).ToListAsync();

        long maxUsed = 0;
        foreach (var s in usedSequences)
        {
            if (!string.IsNullOrEmpty(s) && s.Length > prefix.Length
                && long.TryParse(s.Substring(prefix.Length), out var n))
                maxUsed = Math.Max(maxUsed, n);
        }
        if (encf.Sequence <= maxUsed) encf.Sequence = (int)(maxUsed + 1);

        // Normalizar al rango válido [1, 10,000,000].
        if (encf.Sequence < MinSequence) encf.Sequence = MinSequence;
        if (encf.Sequence > MaxSequence)
            throw new Exception($"Se agotó la secuencia e-NCF para el tipo {typeCode} (máximo {MaxSequence}).");

        string ncf = $"E{typeCode}{encf.Sequence.ToString().PadLeft(10, '0')}";
        encf.Sequence++;
        await _context.SaveChangesAsync();

        return (encf, ncf);
    }

    private OldEcfInvoiceRequestDto PrepareRfceIndividualDto(OldEcfInvoiceRequestDto summaryDto, DateTime signatureDate, string certBase64, string certPass)
    {
        var ind = CloneDto(summaryDto)!;
        NormalizeRfceIndividualDto(ind);
        ind.SignatureDateOverride = signatureDate;
        return ind;
    }

    private static void NormalizeRfceIndividualDto(OldEcfInvoiceRequestDto dto)
    {
        const decimal taxableAmount = 1000m;
        const decimal itbisAmount = 180m;

        dto.EcfType = 32;
        dto.PaymentType = 1;
        dto.CustomerRnc = null;
        dto.CustomerName = "CONSUMIDOR FINAL";
        dto.CustomerForeignId = null;
        dto.GlobalDiscountAmount = 0;
        dto.GlobalDiscountDescription = null;

        var sourceItem = dto.Items.FirstOrDefault();
        dto.Items =
        [
            new OldEcfItemRequestDto
            {
                Name = sourceItem?.Name ?? "Servicio de certificacion",
                Description = sourceItem?.Description ?? sourceItem?.Name ?? "Servicio de certificacion",
                Quantity = 1,
                UnitPrice = taxableAmount,
                Discount = 0,
                TaxPercentage = 18,
                ItbisAmount = itbisAmount,
                ItemType = sourceItem?.ItemType ?? 2,
                UnitOfMeasure = sourceItem?.UnitOfMeasure ?? 43,
                BillingIndicator = 1,
                ManualMontoItem = taxableAmount
            }
        ];

        dto.ManualMontoGravadoTotal = taxableAmount;
        dto.ManualMontoGravadoI1 = taxableAmount;
        dto.ManualMontoGravadoI2 = null;
        dto.ManualMontoGravadoI3 = null;
        dto.ManualMontoExento = null;
        dto.ManualMontoNoFacturable = null;
        dto.ManualTotalITBIS = itbisAmount;
        dto.ManualTotalITBIS1 = itbisAmount;
        dto.ManualTotalITBIS2 = null;
        dto.ManualTotalITBIS3 = null;
        dto.ManualMontoImpuestoAdicional = null;
        dto.ManualMontoTotal = taxableAmount + itbisAmount;
        dto.ManualMontoPeriodo = null;
        dto.ManualValorPagar = null;
        dto.ManualIndicadorMontoGravado = 0;
        dto.ManualTotalITBISRetenido = null;
        dto.ManualTotalISRRetencion = null;
    }
}