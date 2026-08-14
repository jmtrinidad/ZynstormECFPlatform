using System;
using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TokenRefreshLocks = new(StringComparer.Ordinal);
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

        var refreshLock = TokenRefreshLocks.GetOrAdd(cacheKey, static _ => new SemaphoreSlim(1, 1));
        await refreshLock.WaitAsync();

        try
        {
            cachedToken = _cacheService.Get<string>(cacheKey);
            if (!string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            string envKey = environment.ToString();
            string baseAuthUrl;
            string semillaUrl;
            string validacionUrl;

            bool useInternalValidator = _configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator");
            if (useInternalValidator)
            {
                string platformUrl = _configuration["AppSettings:PlatformUrl"] ?? "https://ecfstaging.zynstorm.com/api";
                baseAuthUrl = $"{platformUrl.TrimEnd('/')}/v1/Fe";
                semillaUrl = $"{baseAuthUrl}/autenticacion/api/Semilla";
                validacionUrl = $"{baseAuthUrl}/autenticacion/api/validacioncertificado";
            }
            else
            {
                baseAuthUrl = _configuration[$"DgiiUrls:{envKey}:Auth"]
                    ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey}:Auth no fue encontrada.");
                semillaUrl = $"{baseAuthUrl}/api/Autenticacion/Semilla";
                validacionUrl = $"{baseAuthUrl}/api/Autenticacion/ValidarSemilla";
            }

            // 1. Get Semilla
            _logger.LogError("[DgiiAuth] Ambiente={Env} | useInternalValidator={UseInternal} | Solicitando semilla en: {SemillaUrl}", environment, useInternalValidator, semillaUrl);

            HttpResponseMessage semillaResponse;
            try
            {
                semillaResponse = await _httpClient.GetAsync(semillaUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DgiiAuth] Fallo al conectar con DGII para obtener semilla. URL={SemillaUrl}", semillaUrl);
                throw;
            }

            semillaResponse.EnsureSuccessStatusCode();
            var semillaXml = await semillaResponse.Content.ReadAsStringAsync();

            // LOGGING LA SEMILLA ORIGINAL (SIN FIRMAR) DE DGII PARA ANALIZAR ESTRUCTURA
            //_logger.LogError("=== SEMILLA ORIGINAL (SIN FIRMAR) OBTENIDA DE DGII [{Env}] ===\n{Xml}", environment, semillaXml);

            // 2. Sign Semilla
            var signedSemillaXml = _xmlSignatureService.SignXml(semillaXml, certificateBase64, certificatePassword);

            // LOGGING LA SEMILLA FIRMADA QUE LE ENVIAREMOS A LA DGII
            //_logger.LogError("=== SEMILLA FIRMADA A ENVIAR A DGII [{Env}] ===\n{Xml}", environment, signedSemillaXml);

            // 3. Request Token
            // DGII Test, Production y CerteCF usan multipart/form-data para ValidarSemilla
            HttpResponseMessage tokenResponse;

            if (useInternalValidator)
            {
                using var requestContent = new StringContent(signedSemillaXml, Encoding.UTF8, "application/xml");
                tokenResponse = await _httpClient.PostAsync(validacionUrl, requestContent);
            }
            else
            {
                // All DGII environments (Test, Production, CerteCF) use multipart/form-data
                var multipartContent = new MultipartFormDataContent();
                var xmlFileContent = new StringContent(signedSemillaXml, Encoding.UTF8, "application/xml");
                multipartContent.Add(xmlFileContent, "xml", "semilla.xml");
                tokenResponse = await _httpClient.PostAsync(validacionUrl, multipartContent);
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
        finally
        {
            refreshLock.Release();
        }
    }
}