using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ZynstormECFPlatform.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZynstormECFPlatform.Dtos;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services;

public class DgiiTransmissionService : IDgiiTransmissionService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DgiiTransmissionService> _logger;

    public DgiiTransmissionService(HttpClient httpClient, IConfiguration configuration, ILogger<DgiiTransmissionService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<DgiiTransmissionResult> SendEcfAsync(DgiiEnvironment environment, string token, string signedXml, int ecfType, decimal totalAmount, string rncEmisor, string eNcf, bool isSummary = false)
    {
        string endpointUrl;

        bool useInternalValidator = _configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator");
        if (useInternalValidator)
        {
            string platformUrl = _configuration["AppSettings:PlatformUrl"] ?? "https://ecfstaging.zynstorm.com/api";
            endpointUrl = $"{platformUrl.TrimEnd('/')}/v1/Fe/recepcion/api/ecf";
        }
        else
        {
            string envKey = environment.ToString();

            if (environment == DgiiEnvironment.CerteCF)
            {
                if (isSummary)
                {
                    string baseUrl = _configuration["DgiiUrls:CerteCF:RecepcionFC"]
                        ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:RecepcionFC no fue encontrada.");
                    endpointUrl = $"{baseUrl}/api/recepcion/ecf";
                }
                else
                {
                    string baseUrl = _configuration["DgiiUrls:CerteCF:Recepcion"]
                        ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:Recepcion no fue encontrada.");
                    endpointUrl = $"{baseUrl}/api/FacturasElectronicas";
                }
            }
            else
            {
                if (isSummary)
                {
                    string baseUrl = _configuration[$"DgiiUrls:{envKey}:RecepcionFC"]
                        ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey}:RecepcionFC no fue encontrada.");
                    endpointUrl = $"{baseUrl}/api/recepcion/ecf";
                }
                else
                {
                    string baseUrl = _configuration[$"DgiiUrls:{envKey}:Recepcion"]
                        ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey}:Recepcion no fue encontrada.");
                    endpointUrl = $"{baseUrl}/api/FacturasElectronicas";
                }
            }
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (useInternalValidator)
        {
            // DEV env uses raw application/xml body
            request.Content = new StringContent(signedXml, Encoding.UTF8, "application/xml");
        }
        else
        {
            // DGII Test, Production y CerteCF usan multipart/form-data
            string fileName = $"{rncEmisor}{eNcf}.xml";
            var multipartContent = new MultipartFormDataContent();
            var utf8WithoutBom = new System.Text.UTF8Encoding(false);
            var xmlBytes = utf8WithoutBom.GetBytes(signedXml);
            var xmlFileContent = new ByteArrayContent(xmlBytes);
            xmlFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
            multipartContent.Add(xmlFileContent, "xml", fileName);
            request.Content = multipartContent;
        }

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "DGII SendEcf HTTP request failed. Environment={Environment} Endpoint={Endpoint} EcfType={EcfType} ENcf={ENcf} IsRfce={IsRfce}",
                environment,
                endpointUrl,
                ecfType,
                eNcf,
                isSummary);

            return new DgiiTransmissionResult
            {
                Error = $"No fue posible conectar con DGII para {(isSummary ? "Resumen B2C/RFCE" : "e-CF")}. Endpoint: {endpointUrl}. Detalle: {ex.Message}",
                Mensaje = ex.InnerException?.Message ?? ex.Message
            };
        }

        var responseString = await response.Content.ReadAsStringAsync();
        LogDgiiRawResponse("SendEcf", environment, endpointUrl, ecfType, eNcf, isSummary, response, responseString);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<DgiiTransmissionResult>(responseString, options);
            
            if (result != null)
            {
                LogDgiiParsedResponse("SendEcf", eNcf, isSummary, result);

                // Mapping success based on RFCE fields if present
                if (result.Estado != null || result.Codigo.HasValue)
                {
                    bool isRfceSuccess = string.Equals(result.Estado, "Aceptado", StringComparison.OrdinalIgnoreCase) || result.Codigo == 1 || result.Codigo == 0;
                    
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
            _logger.LogWarning(
                "DGII SendEcf parse failed. ENcf={ENcf} IsRfce={IsRfce} RawResponse={RawResponse}",
                eNcf,
                isSummary,
                responseString);

            // If it's not JSON, might be a raw TrackId or an error message
            if (response.IsSuccessStatusCode && responseString.Length > 5 && responseString.Length < 50 && !responseString.Contains('<'))
            {
                return new DgiiTransmissionResult { TrackId = responseString.Trim('"') };
            }
        }

        return new DgiiTransmissionResult 
        { 
            Error = !response.IsSuccessStatusCode ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseString}" : "Des-serialization error",
            Mensaje = responseString
        };
    }

    public async Task<DgiiStatusResponse> GetStatusAsync(DgiiEnvironment environment, string token, string trackId)
    {
        string url;
        string envKey = environment.ToString();

        bool useInternalValidator = _configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator");
        if (useInternalValidator)
        {
            string platformUrl = _configuration["AppSettings:PlatformUrl"] ?? "https://ecfstaging.zynstorm.com/api";
            string baseUrl = $"{platformUrl.TrimEnd('/')}/v1/Fe/consultaresultado";
            url = $"{baseUrl}/api/Consultas/Estado?TrackId={Uri.EscapeDataString(trackId)}";
        }
        else
        {
            string baseUrl = _configuration[$"DgiiUrls:{envKey}:Consulta"]
                ?? throw new InvalidOperationException($"La configuración DgiiUrls:{envKey}:Consulta no fue encontrada.");
            // Todos los ambientes DGII usan: /api/Consultas/Estado?TrackId=
            url = $"{baseUrl}/api/Consultas/Estado?TrackId={Uri.EscapeDataString(trackId)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        LogDgiiRawResponse("GetStatus", environment, url, ecfType: null, eNcf: trackId, isRfce: false, response, responseString);

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<DgiiStatusResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result != null)
            {
                _logger.LogInformation(
                    "DGII parsed status response. TrackId={TrackId} Codigo={Codigo} Estado={Estado} ENcf={ENcf} SecuenciaUtilizada={SecuenciaUtilizada} Error={Error} Mensaje={Mensaje} Mensajes={Mensajes}",
                    result.TrackId,
                    result.Codigo,
                    result.Estado,
                    result.ENcf,
                    result.SecuenciaUtilizada,
                    result.Error,
                    result.Mensaje,
                    result.Mensajes == null ? null : JsonSerializer.Serialize(result.Mensajes));
            }

            return result ?? new DgiiStatusResponse { Estado = "ParseError", TrackId = trackId, Error = "Error deserializing DGII response" };
        }

        return new DgiiStatusResponse 
        { 
            Estado = "Error", 
            TrackId = trackId, 
            Error = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
            Mensaje = responseString 
        };
    }

    public async Task<DgiiTransmissionResult> SendArecfAsync(DgiiEnvironment environment, string token, string signedXml, string rncEmisor, string eNcf)
    {
        string endpointUrl;
        bool useInternalValidator = _configuration.GetValue<bool>("EcfXmlValidation:UseInternalValidator");

        if (useInternalValidator)
        {
            string platformUrl = _configuration["AppSettings:PlatformUrl"] ?? "https://ecfstaging.zynstorm.com/api";
            endpointUrl = $"{platformUrl.TrimEnd('/')}/v1/Fe/aprobacioncomercial/api/ecf";
        }
        else
        {
            string baseUrl = _configuration["DgiiUrls:CerteCF:AprobacionComercial"] 
                ?? throw new InvalidOperationException("La configuración DgiiUrls:CerteCF:AprobacionComercial no fue encontrada.");
            endpointUrl = $"{baseUrl}/api/AprobacionComercial";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        string fileName = $"{rncEmisor}{eNcf}AEC.xml";
        var multipartContent = new MultipartFormDataContent();
        
        var utf8WithoutBom = new System.Text.UTF8Encoding(false);
        var xmlBytes = utf8WithoutBom.GetBytes(signedXml);
        var xmlFileContent = new ByteArrayContent(xmlBytes);
        xmlFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/xml");
        
        multipartContent.Add(xmlFileContent, "xml", fileName);
        request.Content = multipartContent;

        var response = await _httpClient.SendAsync(request);
        var responseString = await response.Content.ReadAsStringAsync();
        LogDgiiRawResponse("SendArecf", environment, endpointUrl, ecfType: null, eNcf, isRfce: false, response, responseString);

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aecResp = JsonSerializer.Deserialize<DgiiArecfResponse>(responseString, options);
            
            if (aecResp != null)
            {
                var result = new DgiiTransmissionResult
                {
                    Estado = aecResp.Estado,
                    Mensaje = aecResp.Mensaje != null ? string.Join(" | ", aecResp.Mensaje) : responseString
                };

                if (int.TryParse(aecResp.Codigo, out var codInt))
                {
                    result.Codigo = codInt;
                }

                LogDgiiParsedResponse("SendArecf", eNcf, isRfce: false, result);

                if (!response.IsSuccessStatusCode || (result.Codigo != 0 && result.Codigo != 1))
                {
                    result.Error = $"{result.Estado}: {result.Mensaje}";
                }

                return result;
            }
        }
        catch (JsonException)
        {
            _logger.LogWarning(
                "DGII SendArecf parse failed. ENcf={ENcf} RawResponse={RawResponse}",
                eNcf,
                responseString);

            if (response.IsSuccessStatusCode && responseString.Length > 5 && responseString.Length < 50 && !responseString.Contains('<'))
            {
                return new DgiiTransmissionResult { TrackId = responseString.Trim('"') };
            }
        }

        return new DgiiTransmissionResult 
        { 
            Error = !response.IsSuccessStatusCode ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseString}" : "Des-serialization error",
            Mensaje = responseString
        };
    }

    private class DgiiArecfResponse
    {
        public string? Codigo { get; set; }
        public string? Estado { get; set; }
        public List<string>? Mensaje { get; set; }
    }

    private void LogDgiiRawResponse(
        string operation,
        DgiiEnvironment environment,
        string endpointUrl,
        int? ecfType,
        string eNcf,
        bool isRfce,
        HttpResponseMessage response,
        string responseString)
    {
        _logger.LogInformation(
            "DGII raw response. Operation={Operation} Environment={Environment} Endpoint={Endpoint} EcfType={EcfType} ENcf={ENcf} IsRfce={IsRfce} HttpStatus={HttpStatus} Reason={Reason} RawResponse={RawResponse}",
            operation,
            environment,
            endpointUrl,
            ecfType,
            eNcf,
            isRfce,
            (int)response.StatusCode,
            response.ReasonPhrase,
            responseString);
    }

    private void LogDgiiParsedResponse(string operation, string eNcf, bool isRfce, DgiiTransmissionResult result)
    {
        _logger.LogInformation(
            "DGII parsed response. Operation={Operation} ENcf={ENcf} IsRfce={IsRfce} Success={Success} TrackId={TrackId} Codigo={Codigo} Estado={Estado} Error={Error} Mensaje={Mensaje} Mensajes={Mensajes}",
            operation,
            eNcf,
            isRfce,
            result.Success,
            result.TrackId,
            result.Codigo,
            result.Estado,
            result.Error,
            result.Mensaje,
            result.Mensajes == null ? null : JsonSerializer.Serialize(result.Mensajes));
    }
}
