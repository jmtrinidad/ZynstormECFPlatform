using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
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

namespace ZynstormECFPlatform.Services.Certification.OldSimulation;

public class OldCertificationSimulationService : IOldCertificationSimulationService
{
    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IEncryptedService _encryptedService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly IDgiiAuthService _authService;
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IOldEcfGeneratorService _generatorService;
    private readonly IXmlSignatureService _signerService;
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
    private async Task ProcessSimulacionEcfJobInternalAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath, Dictionary<string, string>? samples = null, Dictionary<string, int>? sampleIds = null, int? businessTypeId = null)
    {
        var accepted31Pool = new List<(string Ncf, DateTime IssueDate, string? CustomerRnc, OldEcfInvoiceRequestDto Dto)>();
        var rfcePool = new List<(string Ncf, string SecurityCode, OldEcfInvoiceRequestDto Dto)>();

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
                Console.WriteLine($"[Simulation] Old documents deleted.");
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
            status.SimulationStats.TotalType32Greater250k = matrix.Where(m => m.Type == 32 && !m.IsSummary).Sum(m => m.Count);
            status.SimulationStats.TotalType33 = matrix.Where(m => m.Type == 33).Sum(m => m.Count);
            status.SimulationStats.TotalType34 = matrix.Where(m => m.Type == 34).Sum(m => m.Count);
            status.SimulationStats.TotalType41 = matrix.Where(m => m.Type == 41).Sum(m => m.Count);
            status.SimulationStats.TotalType43 = matrix.Where(m => m.Type == 43).Sum(m => m.Count);
            status.SimulationStats.TotalType44 = matrix.Where(m => m.Type == 44).Sum(m => m.Count);
            status.SimulationStats.TotalType45 = matrix.Where(m => m.Type == 45).Sum(m => m.Count);
            status.SimulationStats.TotalType46 = matrix.Where(m => m.Type == 46).Sum(m => m.Count);
            status.SimulationStats.TotalType47 = matrix.Where(m => m.Type == 47).Sum(m => m.Count);

            var signatureDate = DateTimeExtensions.DrNow;
            var simulationXmls = new Dictionary<string, string>();

            foreach (var item in matrix)
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

                    // Add a placeholder log entry so the UI shows activity
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
                    OldEcfInvoiceRequestDto currentDto;
                    bool skipNcfConsumption = false;

                    if (item.IsManual && item.Type == 32 && item.MaxAmount < 250000 && rfcePool.Count > 0)
                    {
                        var pooled = rfcePool[i % rfcePool.Count];
                        currentDto = CloneDto(pooled.Dto)!;
                        currentDto.Ncf = pooled.Ncf;
                        currentDto.SecurityCodeOverride = pooled.SecurityCode;
                        skipNcfConsumption = true;
                    }
                    else
                    {
                        currentDto = CloneDto(dto)!;

                        // Try to use a specific sample for this type if available
                        if (samples != null && samples.TryGetValue(item.Type.ToString(), out var sampleJson))
                        {
                            var specificDto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(sampleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            if (specificDto != null)
                            {
                                currentDto = specificDto;
                                // Keep issuer info from the original request
                                currentDto.IssuerRnc = dto.IssuerRnc;
                                currentDto.IssuerName = dto.IssuerName;
                                currentDto.IssuerAddress = dto.IssuerAddress;
                                currentDto.ClientId = dto.ClientId;
                            }
                        }

                        currentDto.EcfType = item.Type;
                        currentDto.IncomeType ??= "01";
                        currentDto.PaymentType ??= 1;
                        foreach (var itm in currentDto.Items) itm.ItemType ??= 1;
                        currentDto.SequenceExpirationDate = new DateTime(DateTime.Now.Year + 2, 12, 31);

                        bool isNote = item.Type == 33 || item.Type == 34;

                        // Mixed Scenarios: Inject discounts for complex testing
                        if (!isNote && item.Type != 43 && currentDto.Items.Any())
                        {
                            // 1. Item-level discount (5% on the first item)
                            var firstItem = currentDto.Items[0];
                            decimal itemTotalBeforeDiscount = Math.Round(firstItem.UnitPrice * firstItem.Quantity, 2);
                            firstItem.Discount = Math.Round(itemTotalBeforeDiscount * 0.05m, 2);

                            // 2. Global discount (fixed amount)
                            currentDto.GlobalDiscountAmount = 100.00m;
                            currentDto.GlobalDiscountDescription = "Descuento por Certificación";
                        }

                        // Type 31 logic simplified as we now use validated samples
                        if (item.Type == 31 && currentDto.ManualIndicadorMontoGravado == null)
                        {
                            currentDto.ManualIndicadorMontoGravado = 0;
                        }

                        if (isNote)
                        {
                            int poolIndex = (item.Type == 33) ? i : (1 + i);
                            if (accepted31Pool.Count <= poolIndex)
                            {
                                status.CompletedSteps.Add(new CertificationStepResultDto { Index = status.CurrentStep, Ncf = $"E{item.Type}0000000000", Status = "Saltado", Message = $"No se encontró el e-CF tipo 31 (índice {poolIndex}) aceptado para referenciar." });
                                continue;
                            }
                            var reference = accepted31Pool[poolIndex];
                            var firstItem = CloneDto(reference.Dto)?.Items.FirstOrDefault();
                            currentDto.Items = firstItem != null ? new List<OldEcfItemRequestDto> { firstItem } : new List<OldEcfItemRequestDto>();
                            currentDto.CustomerRnc = reference.Dto.CustomerRnc;
                            currentDto.CustomerName = reference.Dto.CustomerName;
                            currentDto.ReferenceNcf = reference.Ncf;
                            currentDto.ReferenceIssueDate = reference.IssueDate;
                            currentDto.ReferenceReasonCode = 3;
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
                                case 32:
                                    currentDto.PaymentType = 1;
                                    if (item.MinAmount.HasValue && item.MinAmount.Value >= 250000 && string.IsNullOrEmpty(currentDto.CustomerRnc))
                                    {
                                        currentDto.CustomerRnc = "131793916";
                                        currentDto.CustomerName = "CLIENTE PRUEBA CERTIFICACION";
                                    }
                                    break;

                                case 41:
                                    currentDto.CustomerRnc = "131793916";
                                    currentDto.CustomerName = "PROVEEDOR DE SERVICIOS SRL";
                                    currentDto.ManualTotalITBISRetenido = 0;
                                    currentDto.ManualTotalISRRetencion = 0;
                                    foreach (var itm in currentDto.Items) { itm.ManualMontoITBISRetenido = 0; itm.ManualMontoISRRetenido = 0; }
                                    break;

                                case 43:
                                    currentDto.CustomerRnc = null;
                                    currentDto.CustomerName = null;
                                    break;

                                case 46:
                                    currentDto.CustomerRnc = null;
                                    currentDto.CustomerForeignId = currentDto.CustomerForeignId ?? $"EX{i + 1:D6}";
                                    currentDto.CustomerCountry = currentDto.CustomerCountry ?? "USA";
                                    foreach (var itm in currentDto.Items) { itm.BillingIndicator = 3; itm.TaxPercentage = 0; itm.ItbisAmount = 0; }
                                    break;

                                case 47:
                                    currentDto.CustomerRnc = null;
                                    currentDto.CustomerName = currentDto.CustomerName ?? "FOREIGN SERVICES PROVIDER";
                                    currentDto.CustomerForeignId = currentDto.CustomerForeignId ?? $"FOREIGN{i + 1:D6}";
                                    currentDto.ManualTotalITBISRetenido = 0;
                                    currentDto.ManualTotalISRRetencion = 0;
                                    foreach (var itm in currentDto.Items) { itm.BillingIndicator = 4; itm.TaxPercentage = 0; itm.ItbisAmount = 0; itm.ManualMontoITBISRetenido = 0; itm.ManualMontoISRRetenido = 0; }
                                    break;
                            }
                        }
                    }

                    if (item.IsSummary)
                    {
                        currentDto.ManualMontoGravadoTotal = currentDto.Items.Where(it => it.BillingIndicator == 1).Sum(it => it.Quantity * it.UnitPrice);
                        currentDto.ManualMontoExento = currentDto.Items.Where(it => it.BillingIndicator == 4).Sum(it => it.Quantity * it.UnitPrice);
                        currentDto.ManualTotalITBIS = currentDto.Items.Sum(it => it.ItbisAmount);
                        currentDto.ManualMontoTotal = currentDto.Items.Sum(it => (it.Quantity * it.UnitPrice) + it.ItbisAmount);
                        currentDto.CustomerRnc = null;
                        currentDto.CustomerName = "CONSUMIDOR FINAL";

                        try
                        {
                            var ecfTypeRecordForInd = await _context.Set<ZynstormECFPlatform.Core.Entities.EcfType>().FirstOrDefaultAsync(t => t.Code == item.Type.ToString());
                            var encfRecordForInd = await _context.Set<ENcf>().FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecordForInd!.EcfTypeId && e.ClientId == client.ClientId);
                            int seqForInd = encfRecordForInd?.Sequence ?? 1;

                            // Uniqueness loop for the peeked individual NCF
                            while (await _context.Set<CertificationDocument>().AnyAsync(d => d.ENcfSecuence == $"E{item.Type}{seqForInd:D10}" && d.CertificationProcess.ClientId == client.ClientId))
                                seqForInd++;

                            string realNcfForInd = $"E{item.Type}{seqForInd:D10}";
                            var indDto = CloneDto(currentDto)!;
                            indDto.Ncf = realNcfForInd;
                            indDto.SignatureDateOverride = signatureDate;
                            string indUnsigned = _generatorService.GenerateUnsignedXml(indDto, false);
                            string indSigned = _signerService.SignXml(indUnsigned, certBase64, certPass);

                            string tag = "<SignatureValue>";
                            var start = indSigned.IndexOf(tag);
                            if (start != -1)
                            {
                                var content = indSigned.Substring(start + tag.Length).TrimStart();
                                var realCode = content.Substring(0, 6);
                                currentDto.SecurityCodeOverride = realCode;
                                indDto.SecurityCodeOverride = realCode;
                                indDtoForPool = indDto;
                            }
                        }
                        catch { }
                    }

                    currentDto.SignatureDateOverride = signatureDate;

                    // XSD Validation BEFORE sequence management (to avoid burning NCFs)
                    string realNcfBeforeValidation = currentDto.Ncf;
                    currentDto.Ncf = $"E{item.Type}0000000000";
                    string unsignedXmlTemp = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);
                    currentDto.Ncf = realNcfBeforeValidation;

                    var xsdErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXmlTemp, item.Type);
                    if (xsdErrors.Any())
                        throw new Exception($"Error de validación XSD en Paso {status.CurrentStep}: {string.Join(" | ", xsdErrors.Take(2))}");

                    // --- BLOCKING REFERENCE VALIDATION ---
                    try
                    {
                        // Locate the Xml references folder relative to base directory
                        string schemasXmlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ZynstormECFPlatform.Schemas", "Xml"));

                        if (!Directory.Exists(schemasXmlPath))
                        {
                            // Fallback for different build structures
                            schemasXmlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Xml"));
                        }

                        if (Directory.Exists(schemasXmlPath))
                        {
                            var referenceFiles = Directory.GetFiles(schemasXmlPath, $"*E{item.Type}*.xml");
                            if (referenceFiles.Any())
                            {
                                // Validate against the first matching reference file
                                var refErrors = _generatorService.ValidateXmlAgainstReference(unsignedXmlTemp, item.Type, referenceFiles[0]);
                                if (refErrors.Any())
                                {
                                    throw new Exception($"Error de validación contra XML de referencia DGII en Paso {status.CurrentStep} (Tipo {item.Type}): {string.Join(" | ", refErrors)}");
                                }
                                Console.WriteLine($"[Simulation] Paso {status.CurrentStep} validado exitosamente contra referencia: {Path.GetFileName(referenceFiles[0])}");
                            }
                        }
                    }
                    catch (Exception ex) when (ex.Message.Contains("Error de validación contra XML de referencia"))
                    {
                        throw; // Rethrow structural mismatch errors as blocking
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Simulation] Warning: Error al intentar validar contra referencia: {ex.Message}");
                    }
                    // ------------------------------------

                    var ecfTypeRecord = await _context.Set<ZynstormECFPlatform.Core.Entities.EcfType>().FirstOrDefaultAsync(t => t.Code == item.Type.ToString());
                    ENcf? encfRecord = null;
                    if (!skipNcfConsumption)
                    {
                        encfRecord = await _context.Set<ENcf>().FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecord!.EcfTypeId && e.ClientId == client.ClientId);
                        if (encfRecord == null)
                        {
                            encfRecord = new ENcf { NcfTypeId = ecfTypeRecord!.EcfTypeId, ClientId = client.ClientId, Sequence = 1, RegisteredAt = DateTime.Now };
                            _context.Set<ENcf>().Add(encfRecord);
                            await _context.SaveChangesAsync();
                        }

                        // Robust sequence consumption with uniqueness check
                        int seqToUse = encfRecord.Sequence;
                        string ncfCandidate;
                        while (true)
                        {
                            ncfCandidate = $"E{item.Type}{seqToUse:D10}";
                            var exists = await _context.Set<CertificationDocument>().AnyAsync(d => d.ENcfSecuence == ncfCandidate && d.CertificationProcess.ClientId == client.ClientId);
                            if (!exists) break;
                            seqToUse++;
                        }

                        encfRecord.Sequence = seqToUse + 1;
                        currentDto.Ncf = ncfCandidate;
                        _context.Entry(encfRecord).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        if (item.IsSummary) rfcePool.Add((currentDto.Ncf, currentDto.SecurityCodeOverride ?? "", indDtoForPool ?? CloneDto(currentDto)!));
                    }

                    string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);
                    string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);
                    string securityCode = currentDto.SecurityCodeOverride ?? "";
                    if (string.IsNullOrEmpty(securityCode))
                    {
                        string tag = "<SignatureValue>";
                        int start = signedXml.IndexOf(tag);
                        if (start != -1)
                        {
                            securityCode = signedXml.Substring(start + tag.Length).TrimStart().Substring(0, 6);
                        }
                    }

                    string xmlFileName = (item.IsManual ? "SUBIR_DGII_" : "") + $"Paso_{status.CurrentStep}_{currentDto.Ncf}.xml";
                    simulationXmls[xmlFileName] = signedXml;

                    bool isAccepted = false;
                    string? trackId = null;
                    string? error = null;

                    decimal total = currentDto.ManualMontoTotal ?? currentDto.Items.Sum(itm => (itm.Quantity * itm.UnitPrice) - itm.Discount + itm.ItbisAmount);

                    if (item.IsManual) { isAccepted = true; trackId = "MANUAL"; }
                    else
                    {
                        Console.WriteLine($"[Simulation] Sending ECF Type {item.Type} - NCF {currentDto.Ncf} - Total {total}...");
                        var result = await _transmissionService.SendEcfAsync(DgiiEnvironment.CerteCF, token, signedXml, item.Type, total, dto.IssuerRnc, currentDto.Ncf, item.IsSummary);
                        if (result.Success)
                        {
                            if (!string.IsNullOrEmpty(result.TrackId))
                            {
                                await Task.Delay(2000);
                                var finalStatus = await PollDgiiStatusAsync(result.TrackId, dto.IssuerRnc);
                                isAccepted = finalStatus.Estado == "Aceptado" || (item.IsSummary && finalStatus.Estado == "Generado");
                                trackId = result.TrackId;
                                if (!isAccepted) error = $"DGII: {finalStatus.Estado}";
                            }
                            else if (item.IsSummary && result.Estado == "Aceptado") { isAccepted = true; trackId = "INMEDIATO"; }
                            else { isAccepted = false; error = "DGII: No se recibió TrackId."; }
                        }
                        else { isAccepted = false; error = result.Error; }
                        Console.WriteLine($"[Simulation] Result for NCF {currentDto.Ncf}: Success={result.Success}, Accepted={isAccepted}, TrackId={result.TrackId}, Error={error}");
                    }

                    // Save ALL documents for auditability
                    var certDoc = new CertificationDocument
                    {
                        CertificationProcessId = process.CertificationProcessId,
                        ENcfSecuence = currentDto.Ncf,
                        ENcfId = encfRecord?.ENcfId ?? (await _context.Set<ENcf>().FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecord!.EcfTypeId && e.ClientId == client.ClientId))?.ENcfId ?? 0,
                        EcfTypeId = ecfTypeRecord!.EcfTypeId,
                        XmlSent = signedXml,
                        TrackId = trackId,
                        Status = isAccepted ? DocumentStatus.Accepted : DocumentStatus.Rejected,
                        SentAt = DateTime.Now,
                        RegisteredAt = DateTime.Now
                    };
                    // --- AUTO-UPDATE / INSERT SAMPLES ---
                    if (isAccepted && businessTypeId.HasValue)
                    {
                        var ecfTypeCode = item.Type.ToString();
                        var dbSample = await _context.Set<BusinessSimulationSample>()
                            .FirstOrDefaultAsync(s => s.BusinessTypeId == businessTypeId.Value && s.EcfType == ecfTypeCode);

                        if (dbSample == null)
                        {
                            Console.WriteLine($"[Simulation] Inserting new sample for BusinessType {businessTypeId.Value}, Type {ecfTypeCode} (DGII Accepted).");
                            var newSample = new BusinessSimulationSample
                            {
                                BusinessTypeId = businessTypeId.Value,
                                EcfType = ecfTypeCode,
                                Name = $"{currentDto.IssuerName} - {ecfTypeCode} (Auto-generado)",
                                Description = $"Ejemplo autogenerado y aprobado por DGII para el tipo de negocio.",
                                JsonData = JsonSerializer.Serialize(currentDto),
                                IsDgiiApproved = true,
                                RegisteredAt = DateTime.Now,
                                GuidId = Guid.NewGuid().ToString()
                            };
                            _context.Set<BusinessSimulationSample>().Add(newSample);
                            await _context.SaveChangesAsync();

                            // Update sampleIds for this run
                            if (sampleIds != null) sampleIds[ecfTypeCode] = newSample.BusinessSimulationSampleId;
                        }
                        else if (!dbSample.IsDgiiApproved)
                        {
                            Console.WriteLine($"[Simulation] Updating existing sample {dbSample.BusinessSimulationSampleId} with DGII-accepted structure.");
                            dbSample.JsonData = JsonSerializer.Serialize(currentDto);
                            dbSample.IsDgiiApproved = true;
                            _context.Entry(dbSample).State = EntityState.Modified;
                            await _context.SaveChangesAsync();
                        }
                    }
                    // -------------------------------------

                    if (item.Type == 31 && isAccepted) accepted31Pool.Add((currentDto.Ncf, currentDto.IssueDate, currentDto.CustomerRnc, CloneDto(currentDto)!));

                    // Update or replace the placeholder log
                    var existingLog = status.CompletedSteps.FirstOrDefault(l => l.Index == status.CurrentStep);
                    if (existingLog != null) status.CompletedSteps.Remove(existingLog);

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = currentDto.Ncf,
                        Status = isAccepted ? "Aceptado" : "Rechazado",
                        Message = isAccepted ? (item.IsManual ? "Manual" : $"TrackId: {trackId}") : error,
                        Amount = total,
                        SecurityCode = securityCode,
                        FechaFirma = (currentDto.SignatureDateOverride ?? DateTimeExtensions.DrNow).ToString("dd-MM-yyyy hh:mm:ss tt"),
                        FechaEmision = currentDto.IssueDate.ToString("dd-MM-yyyy"),
                        BuyerRnc = currentDto.CustomerRnc,
                        XmlFileName = xmlFileName
                    });

                    if (!isAccepted)
                        throw new Exception($"Error en NCF {currentDto.Ncf}: {error}");
                }
            }

            process.Status = CertificationStatus.Approved;
            process.EndDate = DateTime.Now;
            status.Status = "Completed";

            if (simulationXmls.Any())
            {
                try
                {
                    string zipDir = Path.Combine(webRootPath, "certification_files");
                    if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);
                    string zipPath = Path.Combine(zipDir, $"simulacion_{jobId}.zip");
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
                    status.DownloadUrl = $"/certification_files/simulacion_{jobId}.zip";
                }
                catch { }
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

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessSimulacionEcfJobAsync(OldEcfInvoiceRequestDto dto, string jobId, string webRootPath)
    {
        await ProcessSimulacionEcfJobInternalAsync(dto, jobId, webRootPath);
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath)
    {
        Console.WriteLine($"[Simulation] Background job {jobId} started processing.");
        if (!_jobStatuses.TryGetValue(jobId, out var status))
        {
            status = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
            _jobStatuses[jobId] = status;
        }
        status.Status = "Processing";
        await NotifyUpdate(jobId, status);

        try
        {
            Console.WriteLine($"[Simulation] Loading BusinessType {businessTypeGuidId}...");
            var businessType = await _context.Set<BusinessType>()
                .Include(x => x.Samples)
                .FirstOrDefaultAsync(x => x.GuidId == businessTypeGuidId)
                ?? throw new Exception("El tipo de negocio seleccionado no existe en la base de datos.");

            Console.WriteLine($"[Simulation] Loading Client {clientGuidId}...");
            var client = await _clientService.GetByAsync(x => x.GuidId == clientGuidId)
                ?? throw new Exception("El cliente seleccionado no existe.");

            var sample = businessType.Samples.FirstOrDefault()
                ?? throw new Exception($"No se encontraron muestras de datos configuradas para el tipo de negocio '{businessType.Name}'.");

            Console.WriteLine($"[Simulation] Preparing invoice DTO from sample...");
            // Map sample JSON to OldEcfInvoiceRequestDto
            OldEcfInvoiceRequestDto invoiceDto;
            try
            {
                invoiceDto = JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(sample.JsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? throw new Exception("El resultado de la deserialización fue nulo.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al procesar la muestra de datos (JSON inválido o incompatible): {ex.Message}");
            }

            // Force client info from the selected client
            invoiceDto.ClientId = client.ClientId;
            invoiceDto.IssuerRnc = client.Rnc;
            invoiceDto.IssuerName = client.Name;

            var allSamples = businessType.Samples.ToDictionary(s => s.EcfType, s => s.JsonData);
            var sampleIds = businessType.Samples.ToDictionary(s => s.EcfType, s => s.BusinessSimulationSampleId);

            // Ensure required fields for the DTO are not null
            if (string.IsNullOrEmpty(invoiceDto.Ncf)) invoiceDto.Ncf = "E310000000000";
            if (string.IsNullOrEmpty(invoiceDto.ExternalReference)) invoiceDto.ExternalReference = $"SIM-{jobId}";
            if (invoiceDto.IssueDate == default) invoiceDto.IssueDate = DateTime.Now;

            Console.WriteLine($"[Simulation] Handing over to internal simulation logic...");
            // Continue with simulation logic
            await ProcessSimulacionEcfJobInternalAsync(invoiceDto, jobId, webRootPath, allSamples, sampleIds, businessType.BusinessTypeId);
            Console.WriteLine($"[Simulation] Background job {jobId} completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Simulation] Background job {jobId} FAILED: {ex.Message}");
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
            await NotifyUpdate(jobId, status);
        }
    }

    private async Task<DgiiStatusResponse> PollDgiiStatusAsync(string trackId, string rnc)
    {
        var client = await _clientService.GetByAsync(c => c.Rnc == rnc);
        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        var secret = _encryptedService.DecryptString(apiKey.SecretKey);
        var token = await _authService.GetTokenAsync(rnc, DgiiEnvironment.CerteCF, Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secret)), Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secret)));

        DgiiStatusResponse status;
        int attempts = 0;
        int maxAttempts = 30;
        do
        {
            attempts++;
            await Task.Delay(2000);
            status = await _transmissionService.GetStatusAsync(DgiiEnvironment.CerteCF, token, trackId);
            if (status.Estado == "Aceptado" || status.Estado == "Rechazado" || status.Estado == "Generado") break;
        } while (attempts < maxAttempts);
        return status;
    }

    private async Task NotifyUpdate(string jobId, CertificationJobStatusDto status)
    {
        await _hubContext.Clients.Group($"cert-job:{jobId}").SendAsync("ReceiveJobUpdate", status);
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

    private OldEcfInvoiceRequestDto? CloneDto(OldEcfInvoiceRequestDto source)
    {
        if (source == null) return null;
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<OldEcfInvoiceRequestDto>(json);
    }
}