using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;

namespace ZynstormECFPlatform.Services;

public class DgiiAuthService : IDgiiAuthService
{
    private readonly HttpClient _httpClient;
    private readonly IXmlSignatureService _xmlSignatureService;

    public DgiiAuthService(HttpClient httpClient, IXmlSignatureService xmlSignatureService)
    {
        _httpClient = httpClient;
        _xmlSignatureService = xmlSignatureService;
    }

    public async Task<string> GetTokenAsync(bool isProduction, string certificateBase64, string certificatePassword)
    {
        string baseUrl = isProduction ? "https://ecf.dgii.gov.do/autenticacion/api" : "https://ecf.dgii.gov.do/testecf/autenticacion/api";
        string semillaUrl = $"{baseUrl}/semilla";
        string validacionUrl = $"{baseUrl}/validacioncertificado";

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
        
        // DGII optionally returns pure string token or JSON. Usually {"token": "..."}
        try
        {
            using var doc = JsonDocument.Parse(tokenResponseData);
            if (doc.RootElement.TryGetProperty("token", out var tokenProp))
            {
                return tokenProp.GetString() ?? tokenResponseData;
            }
        }
        catch (JsonException)
        {
            // It might be a raw string format instead of JSON
        }

        return tokenResponseData;
    }
}
