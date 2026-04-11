using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;
using Microsoft.Extensions.Configuration;

namespace ZynstormECFPlatform.Services;

public class DgiiAuthService : IDgiiAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IXmlSignatureService _xmlSignatureService;
    private readonly ICacheService _cacheService;
    private readonly IConfiguration _configuration;

    public DgiiAuthService(HttpClient httpClient, IXmlSignatureService xmlSignatureService, ICacheService cacheService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _xmlSignatureService = xmlSignatureService;
        _cacheService = cacheService;
        _configuration = configuration;
    }

    public async Task<string> GetTokenAsync(string rncEmisor, bool isProduction, string certificateBase64, string certificatePassword)
    {
        string cacheKey = $"{rncEmisor}_DgiiToken";

        string? cachedToken = _cacheService.Get<string>(cacheKey);
        if (!string.IsNullOrEmpty(cachedToken))
        {
            return cachedToken;
        }

        string envKey = isProduction ? "Production" : "Test";
        string baseUrl = _configuration[$"DgiiUrls:{envKey}"] 
            ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey} no fue encontrada en appsettings.json");

        string semillaUrl = $"{baseUrl}/autenticacion/api/semilla";
        string validacionUrl = $"{baseUrl}/autenticacion/api/validacioncertificado";

        // 1. Get Semilla
        var semillaResponse = await _httpClient.GetAsync(semillaUrl);
        semillaResponse.EnsureSuccessStatusCode();
        var semillaXml = await semillaResponse.Content.ReadAsStringAsync();

        // 2. Sign Semilla
        var signedSemillaXml = _xmlSignatureService.SignXml(semillaXml, certificateBase64, certificatePassword);

        // 3. Request Token
        using var requestContent = new StringContent(signedSemillaXml, Encoding.UTF8, "application/xml");
        var tokenResponse = await _httpClient.PostAsync(validacionUrl, requestContent);
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

