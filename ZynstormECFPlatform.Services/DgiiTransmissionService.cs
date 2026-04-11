using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;
using Microsoft.Extensions.Configuration;

namespace ZynstormECFPlatform.Services;

public class DgiiTransmissionService : IDgiiTransmissionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public DgiiTransmissionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<DgiiTransmissionResult> SendEcfAsync(bool isProduction, string token, string signedXml, int ecfType, decimal totalAmount, string rncEmisor, string eNcf)
    {
        string envKey = isProduction ? "Production" : "Test";
        string baseUrl = _configuration[$"DgiiUrls:{envKey}"] 
            ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey} no fue encontrada en appsettings.json");
        
        // Determine endpoint based on logic:
        // "Resumen de Factura de Consumo para tipo 32 con monto <250,000 -> endpoint diferente"
        bool isResumenFacturaConsumo = (ecfType == 32 && totalAmount < 250000);
        string endpointUrl = isResumenFacturaConsumo 
            ? $"{baseUrl}/recepcionfc/api/recepcion/ecf" 
            : $"{baseUrl}/recepcion/api/facturaselectronicas";

        // Create a new request message to ensure we don't bleed headers between calls on the shared HttpClient
        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        // DGII requires multipart/form-data for the XML file payload
        string fileName = $"{rncEmisor}{eNcf}.xml";
        using var multipartContent = new MultipartFormDataContent();
        
        var xmlContent = new StringContent(signedXml, Encoding.UTF8, "application/xml");
        multipartContent.Add(xmlContent, "xml", fileName);
        
        request.Content = multipartContent;

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            var result = JsonSerializer.Deserialize<DgiiTransmissionResult>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result != null)
            {
                // In some edge cases DGII returns 200 OK but the JSON has an error prop.
                if (!response.IsSuccessStatusCode && string.IsNullOrEmpty(result.Error))
                {
                    result.Error = response.ReasonPhrase ?? "HTTP Error";
                }
                return result;
            }
        }
        catch
        {
            // JSON parsing failed, just return raw as error
        }

        return new DgiiTransmissionResult 
        { 
            Error = !response.IsSuccessStatusCode ? (response.ReasonPhrase ?? "Unknown HTTP Error") : "Des-serialization error",
            Mensaje = responseString
        };
    }
}
