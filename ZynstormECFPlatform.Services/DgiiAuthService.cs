using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Core.Enums;
using Microsoft.Extensions.Configuration;

namespace ZynstormECFPlatform.Services;

public class DgiiAuthService : IDgiiAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IXmlSignatureService _xmlSignatureService;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DgiiAuthService> _logger;

    public DgiiAuthService(
        HttpClient httpClient,
        IXmlSignatureService xmlSignatureService,
        ICacheService cacheService,
        IConfiguration configuration,
        ILogger<DgiiAuthService> logger)
    {
        _httpClient = httpClient;
        _xmlSignatureService = xmlSignatureService;
        _cacheService = cacheService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetTokenAsync(string rncEmisor, DgiiEnvironment environment, string certificateBase64, string certificatePassword)
    {
        string cacheKey = $"{rncEmisor}_{environment}_DgiiToken";

        string? cachedToken = _cacheService.Get<string>(cacheKey);

        if (!string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        string envKey = environment.ToString();
        string baseAuthUrl;

        if (environment == DgiiEnvironment.CerteCF)
        {
            baseAuthUrl = _configuration["DgiiUrls:CerteCF:Auth"]
                ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:Auth no fue encontrada.");
        }
        else
        {
            string baseUrl = _configuration[$"DgiiUrls:{envKey}"]
                ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey} no fue encontrada.");
            baseAuthUrl = $"{baseUrl}/autenticacion";
        }

        string semillaUrl;
        string validacionUrl;

        if (environment == DgiiEnvironment.CerteCF)
        {
            semillaUrl = $"{baseAuthUrl}/api/Autenticacion/Semilla";
            validacionUrl = $"{baseAuthUrl}/api/Autenticacion/ValidarSemilla";
        }
        else
        {
            semillaUrl = $"{baseAuthUrl}/api/semilla";
            validacionUrl = $"{baseAuthUrl}/api/validacioncertificado";
        }

        // 1. Get Semilla
        var semillaResponse = await _httpClient.GetAsync(semillaUrl);
        semillaResponse.EnsureSuccessStatusCode();
        var semillaXml = await semillaResponse.Content.ReadAsStringAsync();

        // LOGGING LA SEMILLA ORIGINAL (SIN FIRMAR) DE DGII PARA ANALIZAR ESTRUCTURA
        //_logger.LogError("=== SEMILLA ORIGINAL (SIN FIRMAR) OBTENIDA DE DGII [{Env}] ===\n{Xml}", environment, semillaXml);

        // 2. Sign Semilla
        var signedSemillaXml = _xmlSignatureService.SignXml(semillaXml, certificateBase64, certificatePassword);

        // LOGGING LA SEMILLA FIRMADA QUE LE ENVIAREMOS A LA DGII
        //_logger.LogError("=== SEMILLA FIRMADA A ENVIAR A DGII [{Env}] ===\n{Xml}", environment, signedSemillaXml);

        // 3. Request Token
        HttpResponseMessage tokenResponse;

        if (environment == DgiiEnvironment.CerteCF)
        {
            var multipartContent = new MultipartFormDataContent();
            var xmlFileContent = new StringContent(signedSemillaXml, Encoding.UTF8, "application/xml");
            multipartContent.Add(xmlFileContent, "xml", "semilla.xml");
            tokenResponse = await _httpClient.PostAsync(validacionUrl, multipartContent);
        }
        else
        {
            using var requestContent = new StringContent(signedSemillaXml, Encoding.UTF8, "application/xml");
            tokenResponse = await _httpClient.PostAsync(validacionUrl, requestContent);
        }

        tokenResponse.EnsureSuccessStatusCode();

        var tokenResponseData = await tokenResponse.Content.ReadAsStringAsync();

        string finalToken = tokenResponseData;

        try
        {
            using var doc = JsonDocument.Parse(tokenResponseData);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
            {
                finalToken = tokenProp.GetString() ?? tokenResponseData;
            }
        }
        catch (JsonException) { }

        // Cache the token for 55 minutes
        _cacheService.Set(cacheKey, finalToken, TimeSpan.FromMinutes(55));

        return finalToken;
    }
}