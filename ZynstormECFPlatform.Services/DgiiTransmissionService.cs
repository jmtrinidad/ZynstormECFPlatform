using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Core.Enums;

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

    public async Task<DgiiTransmissionResult> SendEcfAsync(DgiiEnvironment environment, string token, string signedXml, int ecfType, decimal totalAmount, string rncEmisor, string eNcf, bool isSummary = false)
    {
        string envKey = environment.ToString();
        string baseUrl;
        string endpointUrl;

        if (environment == DgiiEnvironment.CerteCF)
        {
            bool isB2CSummaryChannel = isSummary || (ecfType == 32 && totalAmount < 250000);

            if (isB2CSummaryChannel) 
            {
                baseUrl = _configuration["DgiiUrls:CerteCF:RecepcionFC"] 
                    ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:RecepcionFC no fue encontrada.");
                
                // ── UNIFIED: Both Summary and Individual use the /ecf endpoint in RecepcionFC ──
                // DGII differentiates them by the XML root element (<RFCE> vs <ECF>)
                endpointUrl = $"{baseUrl}/api/recepcion/ecf";
            }
            else
            {
                baseUrl = _configuration["DgiiUrls:CerteCF:Recepcion"] 
                    ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:Recepcion no fue encontrada.");
                endpointUrl = $"{baseUrl}/api/facturaselectronicas";
            }
        }
        else
        {
            baseUrl = _configuration[$"DgiiUrls:{envKey}"] 
                ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey} no fue encontrada en appsettings.json");
            
            bool isResumenFacturaConsumo = (ecfType == 32 && totalAmount < 250000);
            endpointUrl = isResumenFacturaConsumo 
                ? $"{baseUrl}/recepcionfc/api/recepcion/ecf" 
                : $"{baseUrl}/recepcion/api/facturaselectronicas";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        if (environment == DgiiEnvironment.CerteCF)
        {
            // CerteCF requires multipart/form-data for transmission too
            string fileName = $"{rncEmisor}{eNcf}.xml";
            var multipartContent = new MultipartFormDataContent();
            var xmlFileContent = new StringContent(signedXml, Encoding.UTF8, "text/xml");
            multipartContent.Add(xmlFileContent, "xml", fileName);
            request.Content = multipartContent;
        }
        else
        {
            // Production/Test usually expects plain application/xml body
            request.Content = new StringContent(signedXml, Encoding.UTF8, "application/xml");
        }

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<DgiiTransmissionResult>(responseString, options);
            
            if (result != null)
            {
                // Mapping success based on RFCE fields if present
                if (result.Estado != null || result.Codigo.HasValue)
                {
                    bool isRfceSuccess = result.Estado == "Aceptado" || result.Codigo == 1 || result.Codigo == 0;
                    
                    if (!isRfceSuccess)
                    {
                        if (string.IsNullOrEmpty(result.Error))
                        {
                            var msgs = result.Mensajes?.Select(m => $"{m.Codigo}: {m.Valor}") ?? Enumerable.Empty<string>();
                            result.Error = $"DGII {result.Estado}: {string.Join(" | ", msgs)}";
                        }
                    }
                }
                else if (!response.IsSuccessStatusCode && string.IsNullOrEmpty(result.Error))
                {
                    result.Error = response.ReasonPhrase ?? "HTTP Error";
                }
                
                return result;
            }
        }
        catch (JsonException)
        {
            // If it's not JSON, might be a raw TrackId or an error message
            if (response.IsSuccessStatusCode && responseString.Length > 5 && responseString.Length < 50 && !responseString.Contains('<'))
            {
                return new DgiiTransmissionResult { TrackId = responseString.Trim('"') };
            }
        }

        return new DgiiTransmissionResult 
        { 
            Error = !response.IsSuccessStatusCode ? (response.ReasonPhrase ?? "Unknown HTTP Error") : "Des-serialization error",
            Mensaje = responseString
        };
    }

    public async Task<DgiiStatusResponse> GetStatusAsync(DgiiEnvironment environment, string token, string trackId)
    {
        string baseUrl;
        if (environment == DgiiEnvironment.CerteCF)
        {
            baseUrl = _configuration["DgiiUrls:CerteCF:Consulta"] 
                ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:Consulta no fue encontrada.");
        }
        else
        {
            string envKey = environment.ToString();
            baseUrl = _configuration[$"DgiiUrls:{envKey}"] 
                ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey} no fue encontrada.");
            
            // For Production/Test, the consultas are usually at /consultas
            baseUrl = $"{baseUrl}/consultas"; 
        }

        string url;
        if (environment == DgiiEnvironment.CerteCF)
        {
            url = $"{baseUrl}/api/Consultas/Estado?TrackId={trackId}";
        }
        else
        {
            url = $"{baseUrl}/api/Consultas/TrackId/{trackId}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<DgiiStatusResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result ?? new DgiiStatusResponse { Estado = "ParseError", TrackId = trackId };
        }

        return new DgiiStatusResponse { Estado = "Error", TrackId = trackId };
    }
}
