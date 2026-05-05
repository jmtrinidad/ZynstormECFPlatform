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
        _context = context;
    }

    public async Task<string> EnqueueSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };
        BackgroundJob.Enqueue<ICertificationSimulationService>(x => x.ProcessSimulacionEcfJobAsync(dto, jobId, webRootPath));
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
}
