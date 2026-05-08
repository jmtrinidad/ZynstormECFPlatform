using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Hangfire;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Data;
using Microsoft.AspNetCore.SignalR;
using ZynstormECFPlatform.Common.Hubs;
using System.Xml.Linq;

namespace ZynstormECFPlatform.Services.Certification;

public class CertificationSimulationService : ICertificationSimulationService
{
    private readonly ICertificationSimulationMappingService _mappingService;
    private readonly ICertificationSimulationGeneratorService _generatorService;
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
    private readonly IENcfService _encfService;
    private readonly StorageContext _context;
    private readonly IHubContext<CertificationHub> _hubContext;
    private readonly IBusinessTypeService _businessTypeService;
    private readonly IBusinessSimulationSampleService _sampleService;

    private static readonly ConcurrentDictionary<string, CertificationJobStatusDto> _jobStatuses = new();

    public CertificationSimulationService(
        ICertificationSimulationMappingService mappingService,
        ICertificationSimulationGeneratorService generatorService,
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
        IENcfService encfService,
        IBusinessTypeService businessTypeService,
        IBusinessSimulationSampleService sampleService,
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
        _encfService = encfService;
        _hubContext = hubContext;
        _businessTypeService = businessTypeService;
        _sampleService = sampleService;
        _context = context;
    }

    public async Task<IEnumerable<BusinessTypeDto>> GetBusinessTypesAsync()
    {
        return await _businessTypeService.Table
            .Select(x => new BusinessTypeDto
            {
                GuidId = x.GuidId,
                Name = x.Name,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task<string> EnqueueSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        BackgroundJob.Enqueue<ICertificationSimulationService>(x => x.ProcessSimulacionEcfJobAsync(dto, jobId, webRootPath));
        return jobId;
    }

    public async Task<string> EnqueueBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        BackgroundJob.Enqueue<ICertificationSimulationService>(x => x.ProcessBusinessSimulationJobAsync(businessTypeGuidId, clientGuidId, jobId, webRootPath));
        return jobId;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string jobId, string webRootPath)
    {
        var status = _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
        try
        {
            // Simulation Logic (Matrix)
            status.Status = "Completed";
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessBusinessSimulationJobAsync(string businessTypeGuidId, string clientGuidId, string jobId, string webRootPath)
    {
        if (!_jobStatuses.TryGetValue(jobId, out var status)) return;
        status.Status = "Processing";

        try
        {
            var businessType = await _businessTypeService.Table
                .Include(x => x.Samples)
                .FirstOrDefaultAsync(x => x.GuidId == businessTypeGuidId)
                ?? throw new Exception("Tipo de negocio no encontrado.");

            var client = await _clientService.GetByAsync(x => x.GuidId == clientGuidId)
                ?? throw new Exception("Cliente no encontrado.");

            // Credentials
            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("API Key no encontrada.");
            var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);
            var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                         ?? throw new Exception("Certificado no encontrado.");
            var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secretKey));
            var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secretKey));

            var samples = businessType.Samples.ToList();
            status.TotalSteps = samples.Count;
            status.CurrentStep = 0;

            foreach (var sample in samples)
            {
                status.CurrentStep++;
                var dto = JsonSerializer.Deserialize<EcfInvoiceRequestDto>(sample.JsonData);
                if (dto == null) continue;

                // Force Client RNC
                dto.ECF.Encabezado.Emisor.RNCEmisor = client.Rnc;

                try
                {
                    string token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);
                    string unsigned = _generatorService.GenerateUnsignedXml(dto, false);
                    string signed = _signerService.SignXml(unsigned, certBase64, certPass);

                    var result = await _transmissionService.SendEcfAsync(
                        DgiiEnvironment.CerteCF,
                        token,
                        signed,
                        int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF),
                        dto.ECF.Encabezado.Totales.MontoTotal ?? 0,
                        client.Rnc,
                        dto.ECF.Encabezado.IdDoc.eNCF,
                        false);

                    if (result.Success)
                    {
                        UpdateSimulationStats(status.SimulationStats, dto.ECF.Encabezado.IdDoc.TipoeCF, dto.ECF.Encabezado.Totales.MontoTotal ?? 0);
                    }

                    lock (status.CompletedSteps)
                    {
                        var xDoc = XDocument.Parse(signed);
                        var securityCode = xDoc.Descendants().FirstOrDefault(d => d.Name.LocalName == "CodigoSeguridadeCF")?.Value;
                        var signatureValue = xDoc.Descendants().FirstOrDefault(d => d.Name.LocalName == "SignatureValue")?.Value;
                        var fechaFirma = xDoc.Descendants().FirstOrDefault(d => d.Name.LocalName == "FechaHoraFirma")?.Value;

                        if (string.IsNullOrEmpty(securityCode))
                        {
                            securityCode = !string.IsNullOrEmpty(signatureValue) && signatureValue.Length >= 6 
                                ? signatureValue.Substring(0, 6) 
                                : "ABC123";
                        }

                        status.CompletedSteps.Add(new CertificationStepResultDto
                        {
                            Ncf = dto.ECF.Encabezado.IdDoc.eNCF,
                            Step = dto.ECF.Encabezado.IdDoc.TipoeCF,
                            Status = result.Success ? "Aceptado" : "Rechazado",
                            Message = result.Success ? "Simulación exitosa" : result.Error,
                            TrackId = result.TrackId,
                            Amount = dto.ECF.Encabezado.Totales.MontoTotal,
                            SecurityCode = securityCode ?? "",
                            FechaFirma = fechaFirma ?? "",
                            BuyerRnc = dto.ECF.Encabezado.Comprador.RNCComprador ?? ""
                        });
                    }
                }
                catch (Exception ex)
                {
                    lock (status.CompletedSteps)
                    {
                        status.CompletedSteps.Add(new CertificationStepResultDto
                        {
                            Ncf = dto.ECF.Encabezado.IdDoc.eNCF,
                            Step = dto.ECF.Encabezado.IdDoc.TipoeCF,
                            Status = "Error",
                            Message = ex.Message
                        });
                    }
                }

                // Notify progress via SignalR
                await _hubContext.Clients.Group($"cert-job:{jobId}").SendAsync("ReceiveJobUpdate", status);
            }

            status.Status = "Completed";
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }
    }

    public async Task<string> ProcessSimulacionUnoAUnoAsync(EcfInvoiceRequestDto dto, string webRootPath)
    {
        var client = await _clientService.GetByAsync(c => c.Rnc == dto.ECF.Encabezado.Emisor.RNCEmisor);
        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);
        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secretKey));
        var certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secretKey));
        string token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);
        string unsigned = _generatorService.GenerateUnsignedXml(dto, false);
        string signed = _signerService.SignXml(unsigned, certBase64, certPass);
        var res = await _transmissionService.SendEcfAsync(DgiiEnvironment.CerteCF, token, signed, int.Parse(dto.ECF.Encabezado.IdDoc.TipoeCF), dto.ECF.Encabezado.Totales.MontoTotal ?? 0, client.Rnc, dto.ECF.Encabezado.IdDoc.eNCF, false);
        return JsonSerializer.Serialize(res);
    }

    public async Task InitializeSimulationSamplesAsync()
    {
        var businessTypes = await _businessTypeService.GetAllAsync();

        // DGII Requirements per Type
        var requirements = new Dictionary<string, (int count, decimal? amount)>
        {
            { "31", (4, null) },
            { "32_Greater", (2, 300000) }, // >= 250k
            { "32_Lower", (4, 1000) },    // < 250k (RFCE)
            { "33", (1, null) },
            { "34", (2, null) },
            { "41", (2, null) },
            { "43", (2, null) },
            { "44", (2, null) },
            { "45", (2, null) },
            { "46", (2, null) },
            { "47", (2, null) }
        };

        foreach (var type in businessTypes)
        {
            // Clear existing samples for this business type to ensure clean state
            var existingSamples = await _sampleService.GetManyByAsync(x => x.BusinessTypeId == type.BusinessTypeId);
            foreach (var s in existingSamples) await _sampleService.HardDeleteAsync(s);

            foreach (var req in requirements)
            {
                var ecfType = req.Key.Split('_')[0];
                for (int i = 1; i <= req.Value.count; i++)
                {
                    var json = SimulationSampleGenerator.GenerateJson(type.Name, ecfType, forceAmount: req.Value.amount);
                    await _sampleService.InsertAsync(new BusinessSimulationSample
                    {
                        BusinessTypeId = type.BusinessTypeId,
                        EcfType = ecfType,
                        JsonData = json,
                        GuidId = Guid.NewGuid().ToString(),
                        RegisteredAt = DateTime.UtcNow
                    });
                }
            }
        }
    }

    private void UpdateSimulationStats(SimulationStatsDto stats, string type, decimal amount)
    {
        switch (type)
        {
            case "31": stats.Type31++; break;
            case "32":
                if (amount >= 250000) stats.Type32Greater250k++;
                else stats.Type32Rfce++;
                break;

            case "33": stats.Type33++; break;
            case "34": stats.Type34++; break;
            case "41": stats.Type41++; break;
            case "43": stats.Type43++; break;
            case "44": stats.Type44++; break;
            case "45": stats.Type45++; break;
            case "46": stats.Type46++; break;
            case "47": stats.Type47++; break;
        }
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
}