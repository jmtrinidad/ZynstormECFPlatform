using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Hangfire;
using MiniExcelLibs;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Data;
using Microsoft.AspNetCore.SignalR;
using ZynstormECFPlatform.Common.Hubs;

namespace ZynstormECFPlatform.Services.Certification;

public class CertificationExcelService : ICertificationExcelService
{
    private readonly ICertificationExcelMappingService _mappingService;
    private readonly ICertificationExcelGeneratorService _generatorService;
    private readonly IXmlSignatureService _signerService;
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IDgiiAuthService _authService;
    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly ICertificationStepService _stepService;
    private readonly ICertificationProcessService _processService;
    private readonly ICertificationDocumentService _documentService;
    private readonly IEncryptedService _encryptedService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly IENcfService _encfService;
    private readonly StorageContext _context;
    private readonly IHubContext<CertificationHub> _hubContext;

    private static readonly ConcurrentDictionary<string, CertificationJobStatusDto> _jobStatuses = new();

    public CertificationExcelService(
        ICertificationExcelMappingService mappingService,
        ICertificationExcelGeneratorService generatorService,
        IXmlSignatureService signerService,
        IDgiiTransmissionService transmissionService,
        IDgiiAuthService authService,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IClientCertificateService clientCertificateService,
        ICertificationStepService stepService,
        ICertificationProcessService processService,
        ICertificationDocumentService documentService,
        IEncryptedService encryptedService,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        ICacheService cacheService,
        IENcfService encfService,
        IHubContext<CertificationHub> hubContext,
        StorageContext context)
    {
        _mappingService = mappingService;
        _generatorService = generatorService;
        _signerService = signerService;
        _transmissionService = transmissionService;
        _authService = authService;
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _clientCertificateService = clientCertificateService;
        _stepService = stepService;
        _processService = processService;
        _documentService = documentService;
        _encryptedService = encryptedService;
        _configuration = configuration;
        _cacheService = cacheService;
        _encfService = encfService;
        _hubContext = hubContext;
        _context = context;
    }

    public async Task<List<CertificationTestDto>> GetTestsAsync()
    {
        string excelPath = Path.Combine(AppContext.BaseDirectory, "133009889-16042026193727.xlsx");
        return await GetTestsFromExcelAsync(excelPath);
    }

    private async Task<List<CertificationTestDto>> GetTestsFromExcelAsync(string excelPath)
    {
        if (!File.Exists(excelPath)) return new List<CertificationTestDto>();

        var ecfRows = MiniExcel.Query(excelPath, sheetName: "ECF", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
        var rfceRows = MiniExcel.Query(excelPath, sheetName: "RFCE", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var tests = new List<CertificationTestDto>();
        var referencedNcfs = ecfRows
            .Select(r => CleanNcf(GetStr(r, "NCFModificado")))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int targetIndex = 0;
        var rfceNcfs = rfceRows.Select(r => CleanNcf(GetStr(r, "ENCF") ?? "")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Filter ECF rows to exclude those that are in RFCE and take exactly 21 for Step 1
        var ecfRowsForStep1 = ecfRows.Where(r => !rfceNcfs.Contains(CleanNcf(GetStr(r, "ENCF") ?? ""))).Take(21).ToList();

        // Block 1: ECF Rows 0-20 -> Steps 1 & 2
        foreach (var row in ecfRowsForStep1)
        {
            var test = MapToTest(row, targetIndex++, referencedNcfs);
            tests.Add(test);
        }

        // Block 2: Step 3 -> NCFs from RFCE sheet
        foreach (var rfceRow in rfceRows)
        {
            var ncf = CleanNcf(GetStr(rfceRow, "ENCF") ?? "");
            var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf) ?? rfceRow;

            var test = MapToTest(ecfRow, targetIndex++, referencedNcfs);
            test.Step = 3;
            tests.Add(test);
        }
        // Block 3: Step 4 -> RFCE rows (mapping them as individual invoices for download)
        foreach (var rfceRow in rfceRows)
        {
            var test = MapToTest(rfceRow, targetIndex++, referencedNcfs);
            test.Step = 4;
            tests.Add(test);
        }

        return tests;
    }


    private CertificationTestDto MapToTest(IDictionary<string, object> row, int index, HashSet<string> referencedNcfs)
    {
        var ecfTypeStr = GetStr(row, "TipoeCF") ?? "31";
        var encf = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "CasoPrueba") ?? "") ?? "";
        var ncfModificado = CleanNcf(GetStr(row, "NCFModificado"));
        var testCase = GetStr(row, "CasoPrueba") ?? GetStr(row, "Caso Prueba") ?? GetStr(row, "Caso de Prueba") ?? GetStr(row, "Escenario") ?? GetStr(row, "Descripcion") ?? GetStr(row, "Descripción") ?? "";

        var test = new CertificationTestDto
        {
            Index = index,
            CaseNumber = testCase,
            EcfType = ecfTypeStr,
            ENcf = encf,
            TotalAmount = GetDec(row, "MontoTotal") ?? 0,
            Description = $"Caso: {testCase}",
            Status = TestStatus.Pending
        };
        test.Step = DetermineStep(test, referencedNcfs, ncfModificado);
        return test;
    }

    private int DetermineStep(CertificationTestDto test, HashSet<string> referencedNcfs, string? ncfModificado)
    {
        if (!int.TryParse(test.EcfType, out int type)) return 0;
        string desc = test.Description ?? "";

        // 1. Keyword Detection (Step 4 - Simulation/Manual)
        if (desc.Contains("Paso 4", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Etapa 4", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Paso IV", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Etapa IV", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Simulacion", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Simulación", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Manual", StringComparison.OrdinalIgnoreCase) ||
            desc.Contains("Especial", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        // 3. Any document that IS REFERENCED by someone else MUST be in Step 1

        if (referencedNcfs.Contains(test.ENcf?.Trim()))
            return 1;

        // 4. Standard Document Types
        if (type == 31 || type == 41 || (type >= 43 && type <= 47) || (type == 32 && test.TotalAmount >= 250000))
            return 1;

        if (type == 33 || type == 34)
            return 2;

        // Step 3 Summaries (Type 32 for B2C)
        if (type == 32 && test.TotalAmount < 250000)
            return 3;

        return 0;
    }

    public async Task<DgiiTransmissionResult> RunTestAsync(int index, string webRootPath)
    {
        var tests = await GetTestsAsync();
        var test = tests.FirstOrDefault(t => t.Index == index) ?? throw new Exception("Test not found");
        string excelPath = Path.Combine(AppContext.BaseDirectory, "133009889-16042026193727.xlsx");
        var rows = MiniExcel.Query(excelPath, sheetName: "ECF", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
        var row = rows[index];
        var requestDto = _mappingService.MapRowToRequest(row, 1);

        var client = await _clientService.GetByAsync(x => x.Rnc == requestDto.ECF.Encabezado.Emisor.RNCEmisor);
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secretKey));
        var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secretKey));
        string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, false);
        string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);
        string token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);
        return await _transmissionService.SendEcfAsync(DgiiEnvironment.CerteCF, token, signedXml, int.Parse(test.EcfType), test.TotalAmount, client.Rnc, test.ENcf, false);
    }

    public async Task<CertificationSummaryDto> GetSummaryAsync() => new CertificationSummaryDto { Tests = await GetTestsAsync() };

    public async Task<CertificationJobStatusDto> EnqueueCertificationJobAsync(byte[] excelBytes, string fileName, string webRootPath, string clientGuidId)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        string path = System.IO.Path.Combine(webRootPath, "certification_files", $"suite_{jobId}.xlsx");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        await System.IO.File.WriteAllBytesAsync(path, excelBytes);

        // Parse Excel to get initial tests immediately using the existing helper method
        var tests = await GetTestsFromExcelAsync(path);

        // CertificationAutomation Job Progress Initial State
        var status = new CertificationJobStatusDto
        {
            JobId = jobId,
            Status = "Pending",
            TotalSteps = tests.Count,
            TotalComprobantes = tests.Count(t => t.Step is 1 or 2),
            TotalResumenes = tests.Count(t => t.Step == 3)
        };

        foreach (var t in tests)
        {
            status.CompletedSteps.Add(new CertificationStepResultDto
            {
                Index = t.Index,
                Ncf = t.ENcf,
                Step = t.Step.ToString(),
                Status = "Pendiente",
                Message = "Esperando procesamiento..."
            });
        }

        _jobStatuses[jobId] = status;

        Console.WriteLine($"[DEBUG-JOB] Enqueued automation job {jobId}. Total tests: {tests.Count}. WebRootPath: {webRootPath}");

        try
        {
            BackgroundJob.Enqueue<ICertificationExcelService>(x => x.ProcessAutomationJobAsync(path, jobId, webRootPath, clientGuidId));
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    public async Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId) => _jobStatuses.TryGetValue(jobId, out var s) ? s : new CertificationJobStatusDto { JobId = jobId, Status = "NotFound" };

    public async Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId)
    {
        if (_jobStatuses.TryGetValue(jobId, out var s))
        {
            lock (s.CompletedSteps)
            {
                return s.CompletedSteps.ToList();
            }
        }
        return new List<CertificationStepResultDto>();
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAutomationJobAsync(string tempFilePath, string jobId, string webRootPath, string clientGuidId)
    {
        if (!_jobStatuses.TryGetValue(jobId, out var status)) return;
        status.Status = "Processing";

        try
        {
            var client = await _clientService.GetByAsync(x => x.GuidId == clientGuidId)
                         ?? throw new Exception("Cliente no encontrado.");

            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("API Key no encontrada.");
            var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);

            var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("Certificado no encontrado.");
            var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secretKey));
            var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secretKey));

            var ecfRows = MiniExcel.Query(tempFilePath, sheetName: "ECF", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            var rfceRows = MiniExcel.Query(tempFilePath, sheetName: "RFCE", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

            if (ecfRows.Count < 1) throw new Exception("El archivo Excel está vacío o no contiene datos en la hoja ECF.");

            var tests = await GetTestsFromExcelAsync(tempFilePath);
            status.TotalSteps = tests.Count;
            status.CurrentStep = 0;

            var jobStartTime = DateTime.Now;

            // Create a 'Virtual' collection for data mapping
            var virtualRows = new List<IDictionary<string, object>>();
            var rfceNcfSet = rfceRows.Select(r => CleanNcf(GetStr(r, "ENCF") ?? "")).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Re-apply Take(21) just in case
            var step1Rows = ecfRows.Where(r => !rfceNcfSet.Contains(CleanNcf(GetStr(r, "ENCF") ?? ""))).Take(21).ToList();

            virtualRows.AddRange(step1Rows); // Steps 1 & 2

            foreach (var rfceRow in rfceRows)
            {
                var ncf = CleanNcf(GetStr(rfceRow, "ENCF") ?? "");
                var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf) ?? rfceRow;
                virtualRows.Add(ecfRow); // Step 3
            }

            foreach (var rfceRow in rfceRows)
            {
                var ncf = CleanNcf(GetStr(rfceRow, "ENCF") ?? "");
                var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf) ?? rfceRow;
                virtualRows.Add(ecfRow); // Step 4
            }

            status.TotalComprobantes = step1Rows.Count;
            status.TotalResumenes = rfceRows.Count;

            await NotifyJobUpdateAsync(jobId, status);

            string jobDir = Path.Combine(webRootPath, "certification_files", jobId);
            if (Directory.Exists(jobDir)) Directory.Delete(jobDir, true);
            Directory.CreateDirectory(jobDir);

            var ncfSecurityCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var globalUsedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            int lastStep = 0;
            foreach (var test in tests.OrderBy(t => t.Step).ThenBy(t => t.Index))
            {
                if (lastStep != 0 && lastStep != test.Step)
                {
                    // Larger delay when switching steps (especially from 1 to 2)
                    Console.WriteLine($"[DEBUG-JOB] Switching from Step {lastStep} to Step {test.Step}. Waiting 5s...");
                    await Task.Delay(5000);
                }
                lastStep = test.Step;

                status.CurrentStep++;
                status.CurrentNcf = test.ENcf;
                string generatedXmlName = "";

                Console.WriteLine($"[DEBUG-JOB] Processing Step {test.Step}, Index {test.Index}, NCF {test.ENcf}, Type {test.EcfType}");

                // Standard delay for stability
                await Task.Delay(2000);

                if (test.Step == 4)
                {
                    // Generate individual XML with synchronized security code
                    ncfSecurityCodes.TryGetValue(test.ENcf, out var sharedCode);

                    try
                    {
                        var row = virtualRows[test.Index];
                        var requestDto = _mappingService.MapRowToRequest(row, 4, jobStartTime);
                        requestDto.SecurityCodeOverride = sharedCode;
                        requestDto.SignatureDateOverride = jobStartTime;

                        string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, false);
                        string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                        string xmlFileName = $"{requestDto.ECF.Encabezado.Emisor.RNCEmisor}{requestDto.ECF.Encabezado.IdDoc.eNCF}.xml";
                        await File.WriteAllTextAsync(Path.Combine(jobDir, xmlFileName), signedXml);

                        lock (status.CompletedSteps)
                        {
                            status.CompletedSteps.Add(new CertificationStepResultDto
                            {
                                Index = test.Index,
                                Ncf = test.ENcf,
                                Step = "4",
                                Status = "Generado",
                                Message = string.IsNullOrEmpty(sharedCode) ? "XML generado (aleatorio)" : $"XML generado con código sincronizado: {sharedCode}",
                                XmlFileName = xmlFileName
                            });
                        }

                        if (test.Step > status.HighestCompletedStep) status.HighestCompletedStep = test.Step;

                        await NotifyJobUpdateAsync(jobId, status);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG-JOB] Error in Step 4 Index {test.Index}: {ex.Message}");
                    }
                }
                else
                {
                    string forcedCode = "";

                    if (test.Step == 3)
                    {
                        // Pre-calculate security code from individual XML
                        try
                        {
                            var row = virtualRows[test.Index];
                            var individualDto = _mappingService.MapRowToRequest(row, 4, jobStartTime);
                            individualDto.SignatureDateOverride = jobStartTime;

                            string unsignedInd = _generatorService.GenerateUnsignedXml(individualDto, false);
                            string signedInd = _signerService.SignXml(unsignedInd, certBase64, certPass);

                            string tag = "<SignatureValue>";
                            var startIdx = signedInd.IndexOf(tag);
                            if (startIdx != -1)
                            {
                                var content = signedInd.Substring(startIdx + tag.Length).TrimStart();
                                forcedCode = content.Substring(0, 6);
                            }
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(forcedCode))
                    {
                        int safety = 0;
                        do { forcedCode = Tools.GenerateRandomCode(6); safety++; } while (globalUsedCodes.Contains(forcedCode) && safety < 100);
                    }
                    globalUsedCodes.Add(forcedCode);

                    try
                    {
                        var row = virtualRows[test.Index];
                        var requestDto = _mappingService.MapRowToRequest(row, test.Step, jobStartTime);
                        requestDto.SecurityCodeOverride = forcedCode;
                        requestDto.SignatureDateOverride = jobStartTime;

                        bool isSummary = (test.Step == 3);
                        string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, isSummary);
                        string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                        var validationErrors = _generatorService.ValidateXmlAgainstSchema(signedXml, int.Parse(test.EcfType));

                        if (validationErrors.Any(e => e.StartsWith("[ERROR]")))
                        {
                            var errorMsg = "Error de esquema (XSD): " + string.Join(" | ", validationErrors);
                            lock (status.CompletedSteps)
                            {
                                status.CompletedSteps.Add(new CertificationStepResultDto
                                {
                                    Index = test.Index,
                                    Ncf = test.ENcf,
                                    Step = test.Step.ToString(),
                                    Status = "Rechazado",
                                    Message = errorMsg
                                });
                            }

                            if (test.Step == 1 || test.Step == 2)
                            {
                                status.ErrorMessage = $"Proceso detenido: El documento {test.ENcf} (Paso {test.Step}) falló XSD. Mensaje: {errorMsg}";
                                status.Status = "Failed";
                                Console.WriteLine($"[DEBUG-JOB] XSD Validation Failed for {test.ENcf}: {errorMsg}");
                                return; // Stop execution
                            }
                            continue;
                        }

                        string token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);

                        var result = await _transmissionService.SendEcfAsync(
                            DgiiEnvironment.CerteCF,
                            token,
                            signedXml,
                            int.Parse(test.EcfType),
                            test.TotalAmount,
                            client.Rnc,
                            test.ENcf,
                            isSummary);

                        var finalResult = result;

                        DgiiStatusResponse pollStatus = null;
                        if (result.Success && !string.IsNullOrEmpty(result.TrackId))
                        {
                            Console.WriteLine($"[DEBUG-JOB] Document sent. TrackId: {result.TrackId}. Polling for status...");
                            pollStatus = await PollDgiiStatusAsync(result.TrackId, client.Rnc);
                            bool isAceptado = string.Equals(pollStatus.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase);
                            bool isGenerado = string.Equals(pollStatus.Estado, "Generado", StringComparison.OrdinalIgnoreCase);

                            if (isAceptado || (test.Step == 3 && isGenerado))
                            {
                                ncfSecurityCodes[test.ENcf] = forcedCode;
                                if (test.Step > status.HighestCompletedStep) status.HighestCompletedStep = test.Step;

                                string xmlFileName = $"{requestDto.ECF.Encabezado.Emisor.RNCEmisor}{requestDto.ECF.Encabezado.IdDoc.eNCF}{(isSummary ? "RFCE" : "")}.xml";
                                await File.WriteAllTextAsync(Path.Combine(jobDir, xmlFileName), signedXml);
                                generatedXmlName = xmlFileName;
                            }
                            else
                            {
                                finalResult = new DgiiTransmissionResult
                                {
                                    TrackId = result.TrackId,
                                    Error = $"DGII Rechazado: {pollStatus.Estado}. {string.Join(", ", pollStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "" })}"
                                };
                            }
                        }

                        lock (status.CompletedSteps)
                        {
                            status.CompletedSteps.Add(new CertificationStepResultDto
                            {
                                Index = test.Index,
                                Ncf = test.ENcf,
                                Step = test.Step.ToString(),
                                Status = finalResult.Success ? "Aceptado" : "Rechazado",
                                Message = finalResult.Success ? $"Paso exitoso (Code: {forcedCode})" : finalResult.Error,
                                TrackId = finalResult.TrackId,
                                XmlFileName = generatedXmlName
                            });
                        }

                        Console.WriteLine($"[DEBUG-JOB] Step {test.Step} completed for {test.ENcf}. Status: {(finalResult.Success ? "Aceptado" : "Rechazado")}");

                        // Notify listeners via SignalR
                        await NotifyJobUpdateAsync(jobId, status);

                        if (!finalResult.Success && (test.Step == 1 || test.Step == 2))
                        {
                            status.ErrorMessage = $"Proceso detenido: El documento {test.ENcf} (Paso {test.Step}) fue rechazado por DGII. Mensaje: {finalResult.Error}";
                            status.Status = "Failed";
                            Console.WriteLine($"[DEBUG-JOB] Job aborted for Step 1/2 failure on {test.ENcf}: {finalResult.Error}");
                            return; // STOP execution to prevent cascading errors
                        }

                        if (!finalResult.Success)
                        {
                            status.Status = "Failed";
                            status.ErrorMessage = $"Error en Index {test.Index} (NCF: {test.ENcf}): {finalResult.Error}";
                            Console.WriteLine($"[DEBUG-JOB] Transmission Failed for {test.ENcf}: {finalResult.Error}");
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG-JOB] Error in Step {test.Step} Index {test.Index}: {ex.Message}");
                        status.Status = "Failed";
                        status.ErrorMessage = ex.Message;
                        break;
                    }
                }
            }

            if (status.Status != "Failed")
            {
                status.Status = "Generating Zip File";
                string zipPath = Path.Combine(webRootPath, "certification_files", $"cert_step4_{jobId}.zip");
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(jobDir, zipPath);
                status.DownloadUrl = zipPath; // Store PHYSICAL path for ReadAllBytes in Controller
                status.Status = "Completed";
                Console.WriteLine($"[DEBUG-JOB] Job {jobId} completed. ZIP created at: {zipPath}");
            }
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }
        finally
        {
            // Always push the terminal state (Completed/Failed) so listeners stop waiting.
            await NotifyJobUpdateAsync(jobId, status);
        }
    }

    public async Task<CertificationJobStatusDto> EnqueueAprobacionComercialJobAsync(byte[] excelBytes, string fileName, string webRootPath, string clientGuidId)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        string path = System.IO.Path.Combine(webRootPath, "certification_files", $"aprobacion_{jobId}.xlsx");
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        await System.IO.File.WriteAllBytesAsync(path, excelBytes);

        var rows = MiniExcel.Query(path, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var status = new CertificationJobStatusDto
        {
            JobId = jobId,
            Status = "Pending",
            TotalSteps = rows.Count,
            TotalComprobantes = 0,
            TotalResumenes = rows.Count
        };

        foreach (var row in rows)
        {
            status.CompletedSteps.Add(new CertificationStepResultDto
            {
                Ncf = GetStr(row, "ENCF") ?? GetStr(row, "eNCF") ?? "N/A",
                Step = "3",
                Status = "Pendiente",
                Message = "Esperando procesamiento..."
            });
        }

        _jobStatuses[jobId] = status;

        try
        {
            BackgroundJob.Enqueue<ICertificationExcelService>(x => x.ProcessAprobacionComercialJobAsync(path, jobId, webRootPath, clientGuidId));
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }

        return status;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAprobacionComercialJobAsync(string tempFilePath, string jobId, string webRootPath, string clientGuidId)
    {
        if (!_jobStatuses.TryGetValue(jobId, out var status)) return;
        status.Status = "Processing";

        try
        {
            var client = await _clientService.GetByAsync(x => x.GuidId == clientGuidId)
                         ?? throw new Exception("Cliente no encontrado.");

            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("API Key no encontrada.");
            var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);

            var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("Certificado no encontrado.");
            var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secretKey));
            var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secretKey));

            var rows = MiniExcel.Query(tempFilePath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            status.TotalSteps = rows.Count;
            status.CurrentStep = 0;
            var jobStartTime = DateTime.Now;

            foreach (var row in rows)
            {
                status.CurrentStep++;
                var requestDto = _mappingService.MapRowToAcecfRequest(row, jobStartTime);
                status.CurrentNcf = requestDto.ENcf;

                await Task.Delay(2000); // Stability delay

                try
                {
                    string signedXml = _signerService.SignXml(_generatorService.GenerateArecfXml(requestDto), certBase64, certPass);
                    string token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);

                    var result = await _transmissionService.SendArecfAsync(DgiiEnvironment.CerteCF, token, signedXml, client.Rnc, requestDto.ENcf);

                    Console.WriteLine($"[DEBUG-AC] Sent AC for {requestDto.ENcf}. Success: {result.Success}");

                    lock (status.CompletedSteps)
                    {
                        var stepResult = status.CompletedSteps.FirstOrDefault(s => s.Ncf == requestDto.ENcf)
                                        ?? new CertificationStepResultDto { Ncf = requestDto.ENcf };

                        stepResult.Status = result.Success ? "Aceptado" : "Rechazado";
                        stepResult.Message = result.Success ? "Aprobación Comercial exitosa" : result.Error;
                        stepResult.Step = "3";

                        if (!status.CompletedSteps.Contains(stepResult)) status.CompletedSteps.Add(stepResult);
                    }

                    // Notify listeners via SignalR
                    await NotifyJobUpdateAsync(jobId, status);

                    if (!result.Success)
                    {
                        // In Step 3, we might continue or stop based on requirements.
                        // User said "replicar paso 2", and step 2 stops on error for Step 1/2.
                        // We'll just continue for Step 3 unless it's a critical infrastructure error.
                    }
                }
                catch (Exception ex)
                {
                    status.Status = "Failed";
                    status.ErrorMessage = $"Error en NCF {requestDto.ENcf}: {ex.Message}";
                    break;
                }
            }

            if (status.Status != "Failed")
            {
                status.Status = "Completed";
            }
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }
        finally
        {
            // Always push the terminal state (Completed/Failed) so listeners stop waiting.
            await NotifyJobUpdateAsync(jobId, status);
        }
    }

    public async Task<List<DgiiTransmissionResult>> ProcessAprobacionComercialAsync(byte[] excelBytes) => new List<DgiiTransmissionResult>();

    public async Task<(byte[] content, string fileName)> SignXmlAsync(Stream xmlStream, string rnc)
    {
        // 1. Leer el contenido del XML desde el stream
        string xmlContent;
        using (var reader = new StreamReader(xmlStream, Encoding.UTF8))
        {
            xmlContent = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(xmlContent))
            throw new Exception("El archivo XML está vacío.");

        // 2. Buscar al cliente por RNC
        var client = await _clientService.GetByAsync(x => x.Rnc == rnc)
                     ?? throw new Exception($"No se encontró ningún cliente registrado con el RNC '{rnc}'.");

        // 3. Obtener la ApiKey activa del cliente para acceder a la SecretKey
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                     ?? throw new Exception($"El cliente con RNC '{rnc}' no tiene una ApiKey activa configurada.");

        if (string.IsNullOrEmpty(apiKey.SecretKey))
            throw new Exception($"El cliente con RNC '{rnc}' no tiene SecretKey configurada.");

        // 4. Desencriptar la SecretKey del cliente
        var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        if (string.IsNullOrEmpty(decryptedSecretKey))
            throw new Exception("No se pudo desencriptar la SecretKey del cliente.");

        // 5. Obtener el certificado digital del cliente
        var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                          ?? throw new Exception($"El cliente con RNC '{rnc}' no tiene un certificado digital registrado.");

        // 6. Desencriptar certificado y password
        var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
        var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

        if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
            throw new Exception("No se pudo desencriptar el certificado del cliente.");

        var certBase64 = Convert.ToBase64String(certificateBytes);
        var certPassword = Encoding.UTF8.GetString(passwordBytes);

        // 7. Firmar el XML
        string signedXml = _signerService.SignXml(xmlContent, certBase64, certPassword);

        // 8. Retornar el XML firmado como bytes
        var signedBytes = Encoding.UTF8.GetBytes(signedXml);
        var fileName = $"{rnc}_signed.xml";

        return (signedBytes, fileName);
    }

    private async Task NotifyJobUpdateAsync(string jobId, CertificationJobStatusDto status)
    {
        try
        {
            await _hubContext.Clients.Group($"cert-job:{jobId}").SendAsync("ReceiveJobUpdate", status);
        }
        catch (Exception ex)
        {
            // Never let a notification failure break the certification flow.
            Console.WriteLine($"[DEBUG-JOB] SignalR notify failed for job {jobId}: {ex.Message}");
        }
    }

    private async Task<DgiiStatusResponse> PollDgiiStatusAsync(string trackId, string rnc)
    {
        var client = await _clientService.GetByAsync(c => c.Rnc == rnc);
        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        var secret = _encryptedService.DecryptString(apiKey.SecretKey);

        var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secret));
        var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secret));

        var token = await _authService.GetTokenAsync(rnc, DgiiEnvironment.CerteCF, certBase64, certPass);

        DgiiStatusResponse status;
        int attempts = 0;
        int maxAttempts = 60;

        do
        {
            attempts++;
            await Task.Delay(2500); // Wait 2.5s between polls
            status = await _transmissionService.GetStatusAsync(DgiiEnvironment.CerteCF, token, trackId);

            if (string.Equals(status.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status.Estado, "Generado", StringComparison.OrdinalIgnoreCase))
                break;
        } while (attempts < maxAttempts);

        return status;
    }

    private static string? CleanNcf(string? raw) => raw?.Trim();

    private static string? GetStr(IDictionary<string, object> row, string key) => row.TryGetValue(key, out var v) ? v?.ToString() : null;

    private static decimal? GetDec(IDictionary<string, object> row, string key) => row.TryGetValue(key, out var v) && decimal.TryParse(v?.ToString(), out var d) ? d : null;
}