using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MiniExcelLibs;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using Hangfire;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Common.Utilities;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

using System.IO.Compression;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Data;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json;

namespace ZynstormECFPlatform.Services;

public class CertificationService : ICertificationService
{
    private readonly IConfiguration _configuration;
    private readonly IEcfGeneratorService _generatorService;
    private readonly IXmlSignatureService _signerService;
    private readonly IDgiiAuthService _authService;
    private readonly IDgiiTransmissionService _transmissionService;
    private readonly IClientService _clientService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IClientCertificateService _clientCertificateService;
    private readonly IEncryptedService _encryptedService;
    private readonly StorageContext _context;

    // In-memory store for certification state
    private static List<CertificationTestDto>? _cachedTests;

    private static Dictionary<string, DateTime>? _typeExpirationDates;
    private static TimeSpan? _dateOffset;

    // Track job status in memory (simplified for now)
    private static readonly Dictionary<string, CertificationJobStatusDto> _jobStatuses = new();

    public CertificationService(
        IConfiguration configuration,
        IEcfGeneratorService generatorService,
        IXmlSignatureService signerService,
        IDgiiAuthService authService,
        IDgiiTransmissionService transmissionService,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IClientCertificateService clientCertificateService,
        IEncryptedService encryptedService,
        StorageContext context)
    {
        _configuration = configuration;
        _generatorService = generatorService;
        _signerService = signerService;
        _authService = authService;
        _transmissionService = transmissionService;
        _clientService = clientService;
        _apiKeyService = apiKeyService;
        _clientCertificateService = clientCertificateService;
        _encryptedService = encryptedService;
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

        // Filter ECF rows to exclude those and take exactly 21 for Step 1
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

        _cachedTests = tests;
        return _cachedTests;
    }

    private CertificationTestDto MapToTest(IDictionary<string, object> row, int index, HashSet<string> referencedNcfs)
    {
        var ecfTypeStr = GetStr(row, "TipoeCF") ?? "31";
        var encf = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "CasoPrueba") ?? "") ?? "";
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
        test.Step = DetermineStep(test, referencedNcfs);
        return test;
    }

    private int DetermineStep(CertificationTestDto test, HashSet<string> referencedNcfs)
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

        // 2. Referenced NCFs (Step 1 priority)
        if (referencedNcfs.Contains(test.ENcf?.Trim()))
            return 1;

        // 3. Document Types
        if (type == 31 || type == 41 || (type >= 43 && type <= 47) || (type == 32 && test.TotalAmount >= 250000))
            return 1;

        if (type == 33 || type == 34)
            return 2;

        // Step 3 Summaries (Type 32 for B2C)
        if (type == 32 && test.TotalAmount < 250000)
            return 3;

        return 0;
    }

    private IDictionary<string, object>? FindRowForTest(List<IDictionary<string, object>> allRows, CertificationTestDto test)
    {
        return allRows.FirstOrDefault(r =>
        {
            string rowIdx = GetStr(r, "Indice") ?? GetStr(r, "Índice") ?? GetStr(r, "Index");
            string rowNcf = CleanNcf(GetStr(r, "ENCF") ?? GetStr(r, "CasoPrueba") ?? "");

            return rowIdx == test.Index.ToString() || rowNcf == test.ENcf;
        });
    }

    public async Task<DgiiTransmissionResult> RunTestAsync(int index, string webRootPath)
    {
        var allTests = await GetTestsAsync();
        var test = allTests.FirstOrDefault(t => t.Index == index)
            ?? throw new ArgumentException("Test no encontrado.");

        test.Status = TestStatus.Running;

        try
        {
            string excelPath = Path.Combine(AppContext.BaseDirectory, "133009889-16042026193727.xlsx");

            string sheetName = (test.Step == 3) ? "RFCE" : "ECF";
            var rows = MiniExcel.Query(excelPath, sheetName: sheetName, useHeaderRow: true)
                .Cast<IDictionary<string, object>>().ToList();

            IDictionary<string, object> row;
            if (test.Step == 3)
            {
                row = rows.First(r => GetStr(r, "ENCF") == test.ENcf);
            }
            else
            {
                row = rows.FirstOrDefault(r => GetStr(r, "ENCF") == test.ENcf)
                      ?? rows.First(r => GetStr(r, "CasoPrueba") == test.CaseNumber);
            }

            test.ENcf = GetStr(row, "ENCF");
            if(string.IsNullOrEmpty(test.ENcf)) test.ENcf = test.CaseNumber;
             GetStr(row, "ENCF");

            var requestDto = MapRowToRequest(row, test.Step);

            string issuerRnc = requestDto.ECF.Encabezado.Emisor.RNCEmisor;
            if (string.IsNullOrEmpty(issuerRnc))
                issuerRnc = _configuration["Certification:IssuerRnc"] ?? "133009889";

            var client = await _clientService.GetByAsync(x => x.Rnc == issuerRnc)
                ?? throw new InvalidOperationException($"No se encontró ningún cliente registrado con el RNC '{issuerRnc}'.");

            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new InvalidOperationException($"El cliente RNC '{issuerRnc}' no tiene una ApiKey activa.");

            var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
            if (string.IsNullOrEmpty(decryptedSecretKey))
                throw new InvalidOperationException("No se pudo desencriptar la SecretKey del cliente.");

            var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new InvalidOperationException($"El cliente RNC '{issuerRnc}' no tiene un certificado digital registrado.");

            var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
            var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);

            if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
                throw new InvalidOperationException("No se pudo desencriptar el certificado del cliente.");

            var certBase64 = Convert.ToBase64String(certificateBytes);
            var certPass = Encoding.UTF8.GetString(passwordBytes);

            if (test.Step == 3)
            {
                try
                {
                    var ecfRows = MiniExcel.Query(excelPath, sheetName: "ECF", useHeaderRow: true)
                        .Cast<IDictionary<string, object>>().ToList();
                    var individualRow = ecfRows.FirstOrDefault(r => GetStr(r, "ENCF") == test.ENcf);
                    if (individualRow != null)
                    {
                        var individualDto = MapRowToRequest(individualRow, 4);
                        string individualUnsigned = _generatorService.GenerateUnsignedXml(individualDto, false);
                        string individualSigned = _signerService.SignXml(individualUnsigned, certBase64, certPass);

                        string tag = "<SignatureValue>";
                        var start = individualSigned.IndexOf(tag);
                        if (start != -1)
                        {
                            var content = individualSigned.Substring(start + tag.Length);
                            var realCode = content.TrimStart().Substring(0, 6);
                            requestDto.SecurityCodeOverride = realCode;
                            requestDto.ECF.Encabezado.Emisor.WebSite = $"[DEBUG-PRECALC] RNC={individualDto.ECF.Encabezado.Emisor.RNCEmisor}, NCF={individualDto.ECF.Encabezado.IdDoc.eNCF}, Date={individualDto.ECF.Encabezado.Emisor.FechaEmision}, Total={individualDto.ECF.Encabezado.Totales.MontoTotal}";
                        }
                    }
                }
                catch { }
            }

            string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, test.Step == 3);
            string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

            try
            {
                string scratchFolder = Path.Combine(webRootPath, "certification_files");
                if (!Directory.Exists(scratchFolder)) Directory.CreateDirectory(scratchFolder);
                string exportPath = Path.Combine(scratchFolder, $"cert_test_{test.Index}.xml");
                File.WriteAllText(exportPath, signedXml);
            }
            catch { }

            var validationErrors = _generatorService.ValidateXmlAgainstSchema(signedXml, int.Parse(test.EcfType));

            if (validationErrors.Any(e => e.StartsWith("[ERROR]")))
            {
                var errorMsg = "Error de esquema (XSD): " + string.Join(" | ", validationErrors);
                test.Status = TestStatus.Failed;
                test.Error = errorMsg;
                return new DgiiTransmissionResult { Error = errorMsg };
            }

            string token = await _authService.GetTokenAsync(issuerRnc, DgiiEnvironment.CerteCF, certBase64, certPass);

            bool isSummary = (test.Step == 3);
            var result = await _transmissionService.SendEcfAsync(
                DgiiEnvironment.CerteCF,
                token,
                signedXml,
                int.Parse(test.EcfType),
                test.TotalAmount,
                issuerRnc,
                test.ENcf,
                isSummary);

            if (result.Success)
            {
                test.Status = TestStatus.Passed;
                test.TrackId = result.TrackId;

                BackgroundJob.Enqueue<Jobs.EcfTrackingJob>(j => j.Execute(
                    result.TrackId,
                    DgiiEnvironment.CerteCF,
                    issuerRnc,
                    certBase64,
                    certPass));
            }
            else
            {
                test.Status = TestStatus.Failed;
                test.Error = result.Error;
            }

            return result;
        }
        catch (Exception ex)
        {
            test.Status = TestStatus.Failed;
            test.Error = ex.Message;
            return new DgiiTransmissionResult { Error = ex.Message };
        }
    }

    private static string? CleanNcf(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        // Find the first letter (NCF start) and return everything from there
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"[A-Za-z].*");
        return match.Success ? match.Value : raw;
    }

    private static object? GetVal(IDictionary<string, object> row, string key)
    {
        if (row == null || string.IsNullOrEmpty(key)) return null;
        if (row.TryGetValue(key, out var val) && val != null) return val;

        var normalizedKey = NormalizeKey(key);
        foreach (var k in row.Keys)
        {
            if (NormalizeKey(k) == normalizedKey)
                return row[k];
        }

        return null;
    }

    private static object? FindValueBroadly(IDictionary<string, object> row, params string[] keywords)
    {
        if (row == null || keywords == null || keywords.Length == 0) return null;

        // 1. Exact match first (normalized)
        foreach (var kw in keywords)
        {
            var val = GetVal(row, kw);
            if (val != null) return val;
        }

        // 2. Partial match (contains all keywords)
        var normalizedKeywords = keywords.Select(k => NormalizeKey(k)).ToList();
        foreach (var key in row.Keys)
        {
            var normalizedKey = NormalizeKey(key);
            if (normalizedKeywords.All(kw => normalizedKey.Contains(kw)))
                return row[key];
        }

        // 3. Fallback: Contains FIRST keyword at least
        var firstKw = normalizedKeywords[0];
        foreach (var key in row.Keys)
        {
            if (NormalizeKey(key).Contains(firstKw))
                return row[key];
        }

        return null;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        // Remove spaces, dots, slashes, dashes
        var normalized = key.Replace(" ", "").Replace(".", "").Replace("/", "").Replace("-", "").Replace("_", "").ToLowerInvariant();
        // Remove accents
        normalized = normalized.Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");
        return normalized;
    }

    private static string? GetStr(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return null;

        var val = valObj.ToString();
        if (string.IsNullOrWhiteSpace(val)) return null;

        var clean = val.Trim();
        var lowered = clean.ToLowerInvariant();

        if (lowered == "#e" || lowered == "#n/a")
            return null;

        return clean;
    }

    private static decimal? GetDec(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return null;
        if (valObj is decimal d) return d;
        if (valObj is double db) return (decimal)db;
        if (valObj is int i) return (decimal)i;
        if (valObj is long l) return (decimal)l;
        if (valObj is float f) return (decimal)f;

        var val = valObj.ToString();
        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsed))
            return parsed;
        return null;
    }

    private static int GetInt(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return 0;
        if (valObj is int i) return i;
        if (valObj is decimal d) return (int)d;
        if (valObj is double db) return (int)db;
        if (valObj is long l) return (int)l;

        var val = valObj.ToString();
        if (int.TryParse(val, out int parsed))
            return parsed;
        return 0;
    }

    private static DateTime? GetDateTime(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        return ObjectToDateTime(valObj);
    }

    private static DateTime? ObjectToDateTime(object? valObj)
    {
        if (valObj == null) return null;
        if (valObj is DateTime dt) return dt;

        var val = valObj.ToString();
        if (string.IsNullOrWhiteSpace(val) || val == "#e") return null;

        // Exhaustive list based on DGII Excel screenshot and previous errors
        string[] formats = [
            "dd-MM-yyyy HH:mm:ss",
            "dd-MM-yyyy HH:mm",
            "dd-MM-yyyy",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm",
            "MM/dd/yyyy",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "M/d/yyyy h:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "d/M/yyyy h:mm:ss tt",
            "dd/MM/yyyy h:mm:ss tt",
            "yyyy-MM-dd h:mm:ss tt",
            "dd-MM-yyyy h:mm:ss tt",
            "yyyy/MM/dd HH:mm:ss",
            "yyyy/MM/dd"
        ];

        if (DateTime.TryParseExact(val, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDt))
            return parsedDt;

        if (DateTime.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDt))
            return parsedDt;

        if (DateTime.TryParse(val, out parsedDt))
            return parsedDt;

        return null;
    }

    private static string CleanRnc(string? rnc) => rnc?.Trim().Replace("-", "") ?? "";

    private static DateTime? ParseDgiiDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "#e") return null;
        if (DateTime.TryParseExact(raw, "dd-MM-yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out dt))
            return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
        return null;
    }

    private static DateTime? ParseDgiiDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    private static DateTime ApplyDateOffset(DateTime date)
    {
        return _dateOffset.HasValue ? date.Add(_dateOffset.Value) : date;
    }

    private static DateTime? ApplyDateOffset(DateTime? date)
    {
        if (!date.HasValue) return null;
        return _dateOffset.HasValue ? date.Value.Add(_dateOffset.Value) : date;
    }

    private EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int step, DateTime? fallbackDate = null)
    {
        var dto = new EcfInvoiceRequestDto
        {
            ExternalReference = GetStr(row, "NumeroFacturaInterna") ?? "",
            ECF = new EcfRequest
            {
                Encabezado = new EcfEncabezadoRequest
                {
                    IdDoc = new EcfIdDocRequest
                    {
                        TipoeCF = GetStr(row, "TipoeCF"),
                        eNCF = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "CasoPrueba") ?? "") ?? "",
                        FechaVencimientoSecuencia = GetStr(row, "FechaVencimientoSecuencia"),
                        IndicadorEnvioDiferido = GetStr(row, "IndicadorEnvioDiferido"),
                        IndicadorMontoGravado = GetStr(row, "IndicadorMontoGravado"),
                        TipoIngresos = GetStr(row, "TipoIngresos"),
                        TipoPago = GetStr(row, "TipoPago"),
                        FechaLimitePago = GetStr(row, "FechaLimitePago"),
                        IndicadorNotaCredito = GetStr(row, "IndicadorNotaCredito"),
                        TerminoPago = GetStr(row, "TerminoPago")
                    },
                    Emisor = new EcfEmisorRequest
                    {
                        RNCEmisor = GetStr(row, "RNCEmisor") ?? "",
                        RazonSocialEmisor = GetStr(row, "RazonSocialEmisor") ?? "",
                        NombreComercial = GetStr(row, "NombreComercial"),
                        Sucursal = GetStr(row, "Sucursal"),
                        DireccionEmisor = GetStr(row, "DireccionEmisor") ?? "",
                        Municipio = GetStr(row, "Municipio"),
                        Provincia = GetStr(row, "Provincia"),
                        Telefono = GetStr(row, "TelefonoEmisor[1]"),
                        CorreoEmisor = GetStr(row, "CorreoEmisor"),
                        WebSite = GetStr(row, "WebSite"),
                        ActividadEconomica = GetStr(row, "ActividadEconomica"),
                        CodigoVendedor = GetStr(row, "CodigoVendedor"),
                        NumeroFacturaInterna = GetStr(row, "NumeroFacturaInterna"),
                        NumeroPedidoInterno = GetStr(row, "NumeroPedidoInterno"),
                        ZonaVenta = GetStr(row, "ZonaVenta"),
                        FechaEmision = GetStr(row, "FechaEmision") ?? fallbackDate?.ToString("dd-MM-yyyy") ?? DateTime.Now.ToString("dd-MM-yyyy")
                    },
                    Comprador = new EcfCompradorRequest
                    {
                        RNCComprador = GetStr(row, "RNCComprador"),
                        IdentificadorExtranjero = GetStr(row, "IdentificadorExtranjero"),
                        RazonSocialComprador = GetStr(row, "RazonSocialComprador"),
                        ContactoComprador = GetStr(row, "ContactoComprador"),
                        CorreoComprador = GetStr(row, "CorreoComprador"),
                        DireccionComprador = GetStr(row, "DireccionComprador"),
                        PaisComprador = GetStr(row, "PaisComprador"),
                        TelefonoAdicional = GetStr(row, "TelefonoAdicional"),
                        MunicipioComprador = GetStr(row, "MunicipioComprador"),
                        ProvinciaComprador = GetStr(row, "ProvinciaComprador"),
                        FechaEntrega = GetStr(row, "FechaEntrega"),
                        FechaOrdenCompra = GetStr(row, "FechaOrdenCompra"),
                        NumeroOrdenCompra = GetStr(row, "NumeroOrdenCompra"),
                        CodigoInternoComprador = GetStr(row, "CodigoInternoComprador")
                    },
                    Totales = new EcfTotalesRequest
                    {
                        MontoGravadoTotal = GetDec(row, "MontoGravadoTotal"),
                        MontoGravadoI1 = GetDec(row, "MontoGravadoI1"),
                        MontoGravadoI2 = GetDec(row, "MontoGravadoI2"),
                        MontoGravadoI3 = GetDec(row, "MontoGravadoI3"),
                        MontoExento = GetDec(row, "MontoExento"),
                        TotalITBIS = GetDec(row, "TotalITBIS"),
                        TotalITBIS1 = GetDec(row, "TotalITBIS1"),
                        TotalITBIS2 = GetDec(row, "TotalITBIS2"),
                        TotalITBIS3 = GetDec(row, "TotalITBIS3"),
                        MontoTotal = GetDec(row, "MontoTotal"),
                        MontoNoFacturable = GetDec(row, "MontoNoFacturable"),
                        MontoPeriodo = GetDec(row, "MontoPeriodo"),
                        ValorPagar = GetDec(row, "ValorPagar"),
                        TotalITBISRetenido = GetDec(row, "TotalITBISRetenido"),
                        TotalISRRetencion = GetDec(row, "TotalISRRetencion"),
                        MontoImpuestoAdicional = GetDec(row, "MontoImpuestoAdicional")
                    }
                },
                InformacionReferencia = (GetStr(row, "TipoeCF") == "33" || GetStr(row, "TipoeCF") == "34") ? new EcfInformacionReferenciaRequest
                {
                    NCFModificado = CleanNcf(GetStr(row, "NCFModificado")),
                    RNCOtroContribuyente = GetStr(row, "RNCOtroContribuyente"),
                    FechaNCFModificado = GetStr(row, "FechaNCFModificado"),
                    CodigoModificacion = GetStr(row, "CodigoModificacion") ?? "3",
                    RazonModificacion = GetStr(row, "RazonModificacion") ?? "Ajuste parcial de montos"
                } : null
            }
        };

        for (int i = 1; i <= 50; i++)
        {
            var nombreKey = $"NombreItem[{i}]";
            var nombre = GetStr(row, nombreKey);
            if (nombre == null) continue;

            var item = new EcfItemRequestDto
            {
                NumeroLinea = i.ToString(),
                IndicadorFacturacion = GetStr(row, $"IndicadorFacturacion[{i}]"),
                NombreItem = nombre,
                DescripcionItem = GetStr(row, $"DescripcionItem[{i}]"),
                IndicadorBienoServicio = GetStr(row, $"IndicadorBienoServicio[{i}]"),
                CantidadItem = GetDec(row, $"CantidadItem[{i}]") ?? 1,
                UnidadMedida = GetStr(row, $"UnidadMedida[{i}]"),
                PrecioUnitarioItem = GetDec(row, $"PrecioUnitarioItem[{i}]") ?? 0,
                DescuentoMonto = GetDec(row, $"DescuentoMonto[{i}]"),
                MontoItem = GetDec(row, $"MontoItem[{i}]") ?? 0,
                RecargoMonto = GetDec(row, $"RecargoMonto[{i}]"),
                MontoITBISRetenido = GetDec(row, $"MontoITBISRetenido[{i}]"),
                MontoISRRetenido = GetDec(row, $"MontoISRRetenido[{i}]"),
                FechaElaboracion = GetStr(row, $"FechaElaboracion[{i}]"),
                FechaVencimientoItem = GetStr(row, $"FechaVencimientoItem[{i}]")
            };

            for (int k = 1; k <= 5; k++)
            {
                var subTipo = GetStr(row, $"TipoSubRecargo[{i}][{k}]");
                var subMonto = GetDec(row, $"MontosubRecargo[{i}][{k}]");
                if (subTipo != null || subMonto != null)
                {
                    item.TablaSubRecargo ??= new EcfTablaSubRecargoRequest();
                    item.TablaSubRecargo.SubRecargo.Add(new EcfSubRecargoRequest
                    {
                        TipoSubRecargo = subTipo ?? "$",
                        MontoSubRecargo = subMonto ?? 0,
                        SubRecargoPorcentaje = GetDec(row, $"SubRecargoPorcentaje[{i}][{k}]")
                    });
                }
            }

            dto.ECF.DetallesItems.Item.Add(item);

            if ((dto.ECF.Encabezado.IdDoc.TipoeCF == "33" || dto.ECF.Encabezado.IdDoc.TipoeCF == "34") && dto.ECF.DetallesItems.Item.Count >= 1)
                break;
        }

        return dto;
    }

    public async Task<CertificationSummaryDto> GetSummaryAsync()
    {
        var tests = await GetTestsAsync();

        // Find latest job to enrich clinical data
        var latestJob = _jobStatuses.Values
            .OrderByDescending(j => j.JobId) // GUID-based but GUIDs are random, better use a timestamp if we had one.
            .FirstOrDefault();

        if (latestJob != null)
        {
            foreach (var test in tests)
            {
                var matchingStep = latestJob.CompletedSteps.FirstOrDefault(s => s.Index == test.Index);
                if (matchingStep != null)
                {
                    test.Status = matchingStep.Status == "Aceptado" ? TestStatus.Passed :
                                  matchingStep.Status == "Rechazado" ? TestStatus.Failed :
                                  TestStatus.Pending;
                    // We can't easily change the DTO in place if it doesn't have these fields,
                    // but we can put the TrackID in the TestCase or similar if needed.
                    // For now, at least the Status will be correct.
                }
            }
        }

        return new CertificationSummaryDto
        {
            TotalTests = tests.Count,
            PassedTests = tests.Count(t => t.Status == TestStatus.Passed),
            FailedTests = tests.Count(t => t.Status == TestStatus.Failed),
            Tests = tests
        };
    }

    #region Automation & Hangfire

    public async Task<string> EnqueueCertificationJobAsync(byte[] excelBytes, string fileName, string webRootPath)
    {
        string scratchFolder = Path.Combine(webRootPath, "certification_files");
        if (!Directory.Exists(scratchFolder)) Directory.CreateDirectory(scratchFolder);

        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        string tempPath = Path.Combine(scratchFolder, $"suite_{jobId}.xlsx");
        await File.WriteAllBytesAsync(tempPath, excelBytes);

        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };

        BackgroundJob.Enqueue<ICertificationService>(x => x.ProcessAutomationJobAsync(tempPath, jobId, webRootPath));

        return jobId;
    }

    public async Task<CertificationJobStatusDto> GetJobStatusAsync(string jobId)
    {
        if (_jobStatuses.TryGetValue(jobId, out var status))
        {
            return await Task.FromResult(status);
        }
        return await Task.FromResult(new CertificationJobStatusDto { JobId = jobId, Status = "NotFound" });
    }

    public async Task<List<CertificationStepResultDto>> GetJobLogsAsync(string jobId)
    {
        if (_jobStatuses.TryGetValue(jobId, out var status))
        {
            return await Task.FromResult(status.CompletedSteps);
        }
        return await Task.FromResult(new List<CertificationStepResultDto>());
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessAutomationJobAsync(string tempFilePath, string jobId, string webRootPath)
    {
        if (!_jobStatuses.ContainsKey(jobId))
        {
            _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
        }

        var status = _jobStatuses[jobId];
        status.Status = "Processing";

        try
        {
            var ecfRows = MiniExcel.Query(tempFilePath, sheetName: "ECF", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            var rfceRows = MiniExcel.Query(tempFilePath, sheetName: "RFCE", useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

            if (ecfRows.Count < 1) throw new Exception("Excel file is empty or missing data row.");

            var firstRow = ecfRows[0];
            string? rncEmisor = GetStr(firstRow, "RNCEmisor");

            if (string.IsNullOrEmpty(rncEmisor))
            {
                var casoPrueba = GetStr(firstRow, "CasoPrueba") ?? GetStr(firstRow, "ENCF");
                if (!string.IsNullOrEmpty(casoPrueba))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(casoPrueba, @"^\d+");
                    if (match.Success) rncEmisor = match.Value;
                }
            }

            if (string.IsNullOrEmpty(rncEmisor)) throw new Exception("Could not find RNCEmisor in the excel.");

            var client = await _clientService.GetByAsync(c => c.Rnc == rncEmisor);

            if (client == null) throw new Exception($"Client with RNC {rncEmisor} not found in the database.");

            var tests = await GetTestsFromExcelAsync(tempFilePath);
            status.TotalSteps = tests.Count;
            status.CurrentStep = 0;

            var jobStartTime = DateTime.Now; // Stabilize all signatures in this job

            // Create a 'Virtual' collection that exactly mirrors the tests mapping logic
            var virtualRows = new List<IDictionary<string, object>>();
            var rfceNcfSet = rfceRows.Select(r => CleanNcf(GetStr(r, "ENCF") ?? "")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var step1Rows = ecfRows.Where(r => !rfceNcfSet.Contains(CleanNcf(GetStr(r, "ENCF") ?? ""))).Take(21).ToList();

            virtualRows.AddRange(step1Rows); // 0-20

            // For Step 3 (21-24)
            foreach (var rfceRow in rfceRows)
            {
                var ncf = CleanNcf(GetStr(rfceRow, "ENCF") ?? "");
                var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf) ?? rfceRow;
                virtualRows.Add(ecfRow);
            }

            // For Step 4 (25-28)
            foreach (var rfceRow in rfceRows)
            {
                var ncf = CleanNcf(GetStr(rfceRow, "ENCF") ?? "");
                var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf) ?? rfceRow;
                virtualRows.Add(ecfRow);
            }

            var step4Xmls = new Dictionary<string, string>();
            var ncfSecurityCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var globalUsedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var test in tests.OrderBy(t => t.Step).ThenBy(t => t.Index))
            {
                status.CurrentStep++;
                status.CurrentNcf = test.ENcf;

                Console.WriteLine($"[DEBUG-JOB] Processing Step {test.Step}, Index {test.Index}, NCF {test.ENcf}, Type {test.EcfType}");

                bool isNote = test.EcfType == "33" || test.EcfType == "34";
                await Task.Delay(isNote ? 5000 : 2000);

                if (test.Step == 4)
                {
                    // For Step 4, we MUST use the same security code that was used in Step 3
                    string? sharedCode = null;
                    ncfSecurityCodes.TryGetValue(test.ENcf, out sharedCode);

                    var genResult = await GenerateSignedXmlForTestAsync(test, client, virtualRows, sharedCode, jobStartTime);
                    step4Xmls[$"cert_test_{test.Index}_{test.ENcf}.xml"] = genResult;

                    if (test.Step > status.HighestCompletedStep)
                        status.HighestCompletedStep = test.Step;

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = test.Index,
                        Ncf = test.ENcf,
                        Step = "4",
                        Status = "Generado",
                        Message = string.IsNullOrEmpty(sharedCode) ? "XML generado (aleatorio)" : $"XML generado con código sincronizado: {sharedCode}"
                    });
                }
                else
                {
                    // For Steps 1, 2, 3: Generate a UNIQUE code
                    string forcedCode = "";

                    if (test.Step == 3)
                    {
                        try
                        {
                            var ncf = CleanNcf(test.ENcf);
                            var ecfRow = ecfRows.FirstOrDefault(r => CleanNcf(GetStr(r, "ENCF") ?? "") == ncf);
                            if (ecfRow != null)
                            {
                                var individualDto = MapRowToRequest(ecfRow, 4, jobStartTime);
                                individualDto.SignatureDateOverride = jobStartTime;

                                var activeCert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
                                var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
                                var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);

                                string individualUnsigned = _generatorService.GenerateUnsignedXml(individualDto, false);
                                string certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(activeCert.Certificate, secretKey));
                                string certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(activeCert.Password, secretKey));

                                string signed = _signerService.SignXml(individualUnsigned, certBase64, certPass);
                                string tag = "<SignatureValue>";
                                var start = signed.IndexOf(tag);
                                if (start != -1)
                                {
                                    var content = signed.Substring(start + tag.Length).TrimStart();
                                    forcedCode = content.Substring(0, 6);
                                }
                            }
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(forcedCode))
                    {
                        int safetyIter = 0;
                        do
                        {
                            forcedCode = GenerateRandomCode(6);
                            safetyIter++;
                        } while (globalUsedCodes.Contains(forcedCode) && safetyIter < 100);
                    }

                    globalUsedCodes.Add(forcedCode);

                    var result = await RunTestInternalAsync(test, client, virtualRows, forcedCode, jobStartTime);

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = test.Index,
                        Ncf = test.ENcf,
                        Step = test.Step.ToString(),
                        TrackId = result.TrackId,
                        Status = result.Success ? "Aceptado" : "Rechazado",
                        Message = result.Success ? $"Paso exitoso (Code: {forcedCode})" : result.Error
                    });

                    if (result.Success)
                    {
                        ncfSecurityCodes[test.ENcf] = forcedCode; // Track/Overwrite code for this NCF for Step 4
                        if (test.Step > status.HighestCompletedStep)
                            status.HighestCompletedStep = test.Step;
                    }
                    else
                    {
                        status.Status = "Failed";
                        status.ErrorMessage = $"Error en Index {test.Index} (NCF: {test.ENcf}): {result.Error}";
                        break;
                    }
                }
            }

            if (step4Xmls.Any())
            {
                string zipPath = Path.Combine(Path.GetDirectoryName(tempFilePath)!, $"cert_step4_results_{jobId}.zip");
                using (var zipStream = new FileStream(zipPath, FileMode.Create))
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var entry in step4Xmls)
                    {
                        var zipEntry = archive.CreateEntry(entry.Key, CompressionLevel.Optimal);
                        using (var entryStream = zipEntry.Open())
                        using (var writer = new StreamWriter(entryStream))
                        {
                            writer.Write(entry.Value);
                        }
                    }
                }
                status.DownloadUrl = zipPath;
            }

            if (status.Status != "Failed")
                status.Status = "Completed";
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
        }
    }

    private async Task<string> GenerateSignedXmlForTestAsync(CertificationTestDto test, Client client, List<IDictionary<string, object>> virtualRows, string? forcedCode = null, DateTime? fallbackDate = null)
    {
        var row = test.Index < virtualRows.Count ? virtualRows[test.Index] : virtualRows.Last();

        var requestDto = MapRowToRequest(row, test.Step, fallbackDate);
        requestDto.SignatureDateOverride = fallbackDate;

        if (!string.IsNullOrEmpty(forcedCode))
        {
            requestDto.SecurityCodeOverride = forcedCode;
        }

        string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, test.Step == 3);

        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        if (cert == null) throw new Exception("Client has no active certificate.");

        var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);
        if (apiKey == null) throw new Exception("Client has no api key.");

        string secret = _encryptedService.DecryptString(apiKey.SecretKey);
        string certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secret));
        string certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secret));

        return _signerService.SignXml(unsignedXml, certBase64, certPass);
    }

    private async Task<DgiiTransmissionResult> RunTestInternalAsync(CertificationTestDto test, Client client, List<IDictionary<string, object>> virtualRows, string? forcedCode = null, DateTime? fallbackDate = null)
    {
        var row = virtualRows[test.Index];
        var requestDto = MapRowToRequest(row, test.Step, fallbackDate);
        requestDto.SignatureDateOverride = fallbackDate;

        // ALWAYS ensure randomization/forced code to avoid 'Already used' errors
        requestDto.SecurityCodeOverride = forcedCode;

        var activeCert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var secretKey = _encryptedService.DecryptString((await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)).SecretKey);

        string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto, test.Step == 3);
        string signedXml = _signerService.SignXml(unsignedXml, Convert.ToBase64String(_encryptedService.DecryptWithSecret(activeCert.Certificate, secretKey)), Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(activeCert.Password, secretKey)));

        var token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, Convert.ToBase64String(_encryptedService.DecryptWithSecret(activeCert.Certificate, secretKey)), Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(activeCert.Password, secretKey)));

        var sendResult = await _transmissionService.SendEcfAsync(
            DgiiEnvironment.CerteCF,
            token,
            signedXml,
            int.Parse(test.EcfType),
            test.TotalAmount,
            client.Rnc,
            test.ENcf,
            test.Step == 3);

        if (!sendResult.Success || string.IsNullOrEmpty(sendResult.TrackId))
            return sendResult;

        // Poll until Aceptado or Rechazado
        var finalStatus = await PollDgiiStatusAsync(sendResult.TrackId, client.Rnc);

        if (finalStatus.Estado == "Aceptado" || (test.Step == 3 && finalStatus.Estado == "Generado"))
        {
            return new DgiiTransmissionResult { TrackId = sendResult.TrackId, SignedXml = signedXml };
        }

        return new DgiiTransmissionResult
        {
            Error = $"DGII Rejected: {finalStatus.Estado}. Mensajes: {string.Join(", ", finalStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "Sin mensaje" })}"
        };
    }

    private async Task<DgiiStatusResponse> PollDgiiStatusAsync(string trackId, string rnc)
    {
        var client = await _clientService.GetByAsync(c => c.Rnc == rnc);
        var cert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
        var secret = _encryptedService.DecryptString((await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)).SecretKey);
        var token = await _authService.GetTokenAsync(rnc, DgiiEnvironment.CerteCF, Convert.ToBase64String(_encryptedService.DecryptWithSecret(cert.Certificate, secret)), Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(cert.Password, secret)));

        DgiiStatusResponse status;
        int attempts = 0;
        int maxAttempts = 60;

        do
        {
            attempts++;
            await Task.Delay(1000);
            status = await _transmissionService.GetStatusAsync(DgiiEnvironment.CerteCF, token, trackId);

            if (status.Estado == "Aceptado" || status.Estado == "Rechazado" || status.Estado == "Generado")
                break;
        } while (attempts < maxAttempts);

        return status;
    }

    #endregion Automation & Hangfire

    #region AEC Processing

    public async Task<List<DgiiTransmissionResult>> ProcessAprobacionComercialAsync(byte[] excelBytes)
    {
        var results = new List<DgiiTransmissionResult>();
        using var ms = new MemoryStream(excelBytes);
        var rows = MiniExcel.Query(ms, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var processStartTime = DateTime.Now;

        foreach (var row in rows)
        {
            try
            {
                var dto = MapRowToArecfRequest(row, processStartTime);

                // Identify the client who needs to sign.
                // We check both RNCComprador and RNCEmisor, prioritization matches the certification target.
                var client = await _clientService.GetByAsync(x => x.Rnc == dto.RNCComprador)
                          ?? await _clientService.GetByAsync(x => x.Rnc == dto.RNCEmisor);

                if (client == null)
                {
                    results.Add(new DgiiTransmissionResult { Error = $"No se encontró un cliente registrado para RNC {dto.RNCComprador} o {dto.RNCEmisor}. Verifique que el cliente esté dado de alta en el sistema." });
                    continue;
                }

                // 1. Get Security Data
                var activeCert = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId);
                var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId);

                if (activeCert == null || apiKey == null)
                {
                    results.Add(new DgiiTransmissionResult { Error = $"Datos de seguridad (Certificado/API Key) incompletos para el cliente {client.Rnc}." });
                    continue;
                }

                var secretKey = _encryptedService.DecryptString(apiKey.SecretKey);
                string certBase64 = Convert.ToBase64String(_encryptedService.DecryptWithSecret(activeCert.Certificate, secretKey));
                string certPass = Encoding.UTF8.GetString(_encryptedService.DecryptWithSecret(activeCert.Password, secretKey));

                // 2. Generate Unsigned XML
                var unsignedXml = _generatorService.GenerateArecfXml(dto);

                // 3. Validate against XSD (Pattern "Lo hicimos en la certificación")
                var validationErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXml, 0);

                if (validationErrors.Any())
                {
                    results.Add(new DgiiTransmissionResult
                    {
                        Error = $"XML AEC generado para {dto.ENcf} no cumple con el XSD: {string.Join(" | ", validationErrors)}"
                    });
                    // continue; // User might decide to continue or stop. Usually better to stop if invalid.
                }

                // 4. Authentication (Semilla -> Token)
                var token = await _authService.GetTokenAsync(client.Rnc, DgiiEnvironment.CerteCF, certBase64, certPass);

                // 5. Digital Signature
                var signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                // 6. Transmission
                var transmission = await _transmissionService.SendArecfAsync(DgiiEnvironment.CerteCF, token, signedXml, client.Rnc, dto.ENcf);
                transmission.Encf = dto.ENcf;
                transmission.SignedXml = signedXml;

                results.Add(transmission);
            }
            catch (Exception ex)
            {
                results.Add(new DgiiTransmissionResult { Error = $"Error procesando AEC: {ex.Message}" });
            }
        }

        return results;
    }

    private AcecfRequestDto MapRowToArecfRequest(IDictionary<string, object> row, DateTime fallbackDate)
    {
        // Broad Matching to ensure we catch columns with variations (Accents, Case, Connector words)
        var version = GetStr(row, "Version") ?? "1.0";
        var rncEmisor = GetStr(row, "RNCEmisor") ?? GetStr(row, "RNC Emisor") ?? (FindValueBroadly(row, "RNC", "Emisor")?.ToString() ?? "");
        var encf = (FindValueBroadly(row, "eNCF") ?? FindValueBroadly(row, "NCF") ?? "").ToString() ?? "";

        var fechaEmisionRaw = ObjectToDateTime(FindValueBroadly(row, "Fecha", "Emision"));
        var fechaEmision = fechaEmisionRaw ?? fallbackDate;

        var montoTotalVal = FindValueBroadly(row, "Monto", "Total") ?? FindValueBroadly(row, "Total");
        decimal montoTotal = 0;
        if (montoTotalVal != null) decimal.TryParse(montoTotalVal.ToString(), out montoTotal);

        var rncComprador = GetStr(row, "RNCComprador") ?? GetStr(row, "RNC Comprador") ?? (FindValueBroadly(row, "RNC", "Comprador")?.ToString() ?? "");
        var estadoVal = FindValueBroadly(row, "Estado");
        int estado = 1;
        if (estadoVal != null) int.TryParse(estadoVal.ToString(), out estado);

        var motivo = GetStr(row, "DetalleMotivoRechazo") ?? (FindValueBroadly(row, "Motivo")?.ToString() ?? "");

        var fechaHoraAproRaw = ObjectToDateTime(FindValueBroadly(row, "Fecha", "Hora", "Aprobacion"))
                            ?? ObjectToDateTime(FindValueBroadly(row, "Fecha", "Aprobacion"))
                            ?? ObjectToDateTime(FindValueBroadly(row, "Fecha", "Aprobacion", "Comercial"));

        var fechaHoraApro = fechaHoraAproRaw ?? fallbackDate;

        return new AcecfRequestDto
        {
            Version = version,
            RNCEmisor = rncEmisor,
            ENcf = encf,
            FechaEmision = fechaEmision.ToString("dd-MM-yyyy"),
            MontoTotal = montoTotal,
            RNCComprador = rncComprador,
            Estado = estado,
            DetalleMotivoRechazo = motivo,
            FechaHoraAprobacionComercial = fechaHoraApro.ToString("dd-MM-yyyy HH:mm:ss")
        };
    }

    #endregion AEC Processing

    public async Task<string> EnqueueSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string webRootPath)
    {
        string jobId = Guid.NewGuid().ToString("N").Substring(0, 8);
        _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Pending" };

        BackgroundJob.Enqueue<ICertificationService>(x => x.ProcessSimulacionEcfJobAsync(dto, jobId, webRootPath));

        return jobId;
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ProcessSimulacionEcfJobAsync(EcfInvoiceRequestDto dto, string jobId, string webRootPath)
    {
        // Pool to track accepted Type 31 invoices to use as references for notes
        var accepted31Pool = new List<(string Ncf, DateTime IssueDate, string? CustomerRnc, EcfInvoiceRequestDto Dto)>();

        // [NEW] Pool to track RFCE summaries to synchronize with Step 4 manual individuals
        var rfcePool = new List<(string Ncf, string SecurityCode, EcfInvoiceRequestDto Dto)>();

        if (!_jobStatuses.ContainsKey(jobId))
        {
            _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
        }

        var status = _jobStatuses[jobId];
        status.Status = "Processing";

        try
        {
            // 1. Validate Client
            var client = await _clientService.GetByAsync(c => c.Rnc == dto.ECF.Encabezado.Emisor.RNCEmisor)
                ?? throw new Exception($"Cliente con RNC {dto.ECF.Encabezado.Emisor.RNCEmisor} no encontrado.");

            // 2. Manage Certification Process (Reuse if ongoing)
            var step4 = await _context.CertificationSteps.FirstOrDefaultAsync(s => s.Order == 4);
            if (step4 == null)
            {
                step4 = new CertificationStep
                {
                    Name = "Pruebas Simulación e-CF",
                    Order = 4,
                    IsRequired = true,
                    RegisteredAt = DateTime.Now
                };
                _context.CertificationSteps.Add(step4);
                await _context.SaveChangesAsync();
            }

            var process = await _context.CertificationProcesses
                .OrderByDescending(p => p.RegisteredAt)
                .FirstOrDefaultAsync(p => p.ClientId == client.ClientId &&
                                          (p.Status == CertificationStatus.Pending || p.Status == CertificationStatus.InProgress));

            if (process != null)
            {
                // When the simulation restarts, delete all previously sent documents for this process
                // to start from a clean slate as requested.
                var docsToDelete = await _context.CertificationDocuments
                    .Where(d => d.CertificationProcessId == process.CertificationProcessId)
                    .ToListAsync();

                if (docsToDelete.Any())
                {
                    _context.CertificationDocuments.RemoveRange(docsToDelete);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[INFO] Limpieza de base de datos: {docsToDelete.Count} documentos eliminados para reiniciar simulación.");
                }
            }
            else
            {
                process = new CertificationProcess
                {
                    ClientId = client.ClientId,
                    Environment = DgiiEnvironment.CerteCF,
                    Status = CertificationStatus.InProgress,
                    StartDate = DateTime.Now,
                    CurrentStepId = step4.CertificationStepId,
                    RegisteredAt = DateTime.Now
                };
                _context.CertificationProcesses.Add(process);
                await _context.SaveChangesAsync();
            }

            // 3. Prepare Credentials
            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new Exception("ApiKey no encontrada.");
            var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
            var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new Exception("Certificado no encontrado.");
            var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
            var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
            var certBase64 = Convert.ToBase64String(certificateBytes);
            var certPass = Encoding.UTF8.GetString(passwordBytes);

            string token = await _authService.GetTokenAsync(dto.ECF.Encabezado.Emisor.RNCEmisor, DgiiEnvironment.CerteCF, certBase64, certPass);

            // 4. Define Simulation Matrix (Full Matrix in DGII Order)
            // IMPORTANT: Type 33/34 must come immediately after Type 31 to ensure the
            // referenced NCF is still valid (not invalidated by a DGII reset from another type).
            var matrix = new (int Type, int Count, bool IsSummary, bool IsManual, decimal? MinAmount, decimal? MaxAmount)[] {
                // Paso 1: Crédito Fiscal (4 requeridos)
                (31, 4, false, false, null, null),
                // Paso 2: Notas (1 de Crédito, 2 de Débito)
                (33, 1, false, false, null, null),
                (34, 2, false, false, null, null),
                // Paso 3: Tipos especiales (2 de cada uno)
                (32, 2, false, false, 250000, null),
                (41, 2, false, false, null, null),
                (43, 2, false, false, null, null),
                (44, 2, false, false, null, null),
                (45, 2, false, false, null, null),
                (46, 2, false, false, null, null),
                (47, 2, false, false, null, null),
                // Paso 4: RFCE (Resúmenes B2C < 250k - 4 requeridos)
                (32, 4, true, false, 10, 249000),
                // Paso 5: Consumo Individual B2C < 250k (Manual upload - 4 requeridos)
                (32, 4, false, true, 10, 249000)
            };

            status.TotalSteps = matrix.Sum(m => m.Count);
            status.CurrentStep = 0;
            var signatureDate = DateTime.Now;

            // In-memory list to track documents sent in THIS run (for DB persistence)
            var sentDocsThisRun = new List<CertificationDocument>();
            var simulationXmls = new Dictionary<string, string>();
            var simulationJsons = new Dictionary<string, string>();

            // [FIX] Define dtoRows correctly
            var dtoRows = dto.ECF.DetallesItems.Item.Select(it => new { Items = new List<EcfItemRequestDto> { it }, Total = it.CantidadItem * it.PrecioUnitarioItem }).ToList();

            foreach (var item in matrix)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    status.CurrentStep++;
                    EcfInvoiceRequestDto? indDtoForPool = null;

                    // Prepare DTO for this step
                    EcfInvoiceRequestDto currentDto;
                    bool skipNcfConsumption = false;

                    // [NEW] Synchronization for Step 4: If this is a manual individual document (<250k)
                    // and we have a corresponding summary in the pool, REUSE it.
                    if (item.IsManual && item.Type == 32 && item.MaxAmount < 250000 && rfcePool.Count > 0)
                    {
                        var pooled = rfcePool[i % rfcePool.Count]; // Match by index within the count
                        currentDto = CloneDto(pooled.Dto)!;
                        currentDto.ECF.Encabezado.IdDoc.eNCF = pooled.Ncf;
                        currentDto.SecurityCodeOverride = pooled.SecurityCode;
                        skipNcfConsumption = true;
                        Console.WriteLine($"[SYNC] Reutilizando NCF {currentDto.ECF.Encabezado.IdDoc.eNCF} para documento individual Step 4.");
                    }
                    else
                    {
                        var row = i < dtoRows.Count ? dtoRows[i] : dtoRows.Last();
                        currentDto = CloneDto(dto)!;
                        // Map items from the source JSON row
                        currentDto.ECF.DetallesItems.Item = row.Items;
                        currentDto.ECF.Encabezado.Totales.MontoTotal = row.Total;
                        currentDto.ECF.Encabezado.IdDoc.TipoeCF = item.Type.ToString();
                        currentDto.SequenceExpirationDate = new DateTime(2028, 12, 31);

                        bool isNote = item.Type == 33 || item.Type == 34;

                        // ── Step 1: Force exempt ONLY for Type 31 (Standard Invoices) to pass initial DGII steps
                        if (item.Type == 31)
                        {
                            foreach (var itm in currentDto.ECF.DetallesItems.Item)
                            {
                                itm.IndicadorFacturacion = "4"; // Exento
                                // itm.TaxPercentage = 0;
                                // 0m = 0;
                                itm.MontoItem = 0; // Let generator recalculate
                            }
                        }

                        // ── Step 2: Clear manual totals so the generator calculates from items
                        currentDto.ECF.Encabezado.Totales.MontoGravadoTotal = null;
                        currentDto.ManualMontoGravadoI1 = null;
                        currentDto.ManualMontoGravadoI2 = null;
                        currentDto.ManualMontoGravadoI3 = null;
                        currentDto.ECF.Encabezado.Totales.MontoExento = null;
                        currentDto.ECF.Encabezado.Totales.TotalITBIS = null;
                        currentDto.ECF.Encabezado.Totales.TotalITBIS1 = null;
                        currentDto.ECF.Encabezado.Totales.TotalITBIS2 = null;
                        currentDto.ECF.Encabezado.Totales.TotalITBIS3 = null;
                        currentDto.ECF.Encabezado.Totales.TotalISRRetencion = null;
                        currentDto.ECF.Encabezado.Totales.TotalITBISRetenido = null;
                        currentDto.ECF.Encabezado.Totales.MontoTotal = null;

                        if (isNote)
                        {
                            // Use references from the pool
                            // Type 33 -> uses 1st accepted 31 (index 0)
                            // Type 34 -> uses 2nd and 3rd accepted 31 (index 1, 2, ...)
                            int poolIndex = (item.Type == 33) ? i : (1 + i);

                            if (accepted31Pool.Count <= poolIndex)
                            {
                                Console.WriteLine($"[SKIP] Saltando Paso {status.CurrentStep} (Tipo {item.Type}): No hay e-CF 31 aceptado en el índice {poolIndex}.");
                                status.CompletedSteps.Add(new CertificationStepResultDto
                                {
                                    Index = status.CurrentStep,
                                    Ncf = $"E{item.Type}0000000000",
                                    Status = "Saltado",
                                    Message = $"No se encontró el e-CF tipo 31 (índice {poolIndex}) aceptado para referenciar."
                                });
                                continue;
                            }

                            var reference = accepted31Pool[poolIndex];

                            // [MODIFIED] Take only the FIRST item to avoid total annulment
                            var firstItem = CloneDto(reference.Dto)?.Items.FirstOrDefault();
                            currentDto.ECF.DetallesItems.Item = firstItem != null ? new List<EcfItemRequestDto> { firstItem } : new List<EcfItemRequestDto>();

                            currentDto.ECF.Encabezado.Comprador.RNCComprador = reference.Dto.CustomerRnc;
                            currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = reference.Dto.CustomerName;
                            currentDto.ECF.Encabezado.Comprador.DireccionComprador = reference.Dto.CustomerAddress;

                            // Set reference fields
                            currentDto.ReferenceNcf = reference.Ncf;
                            currentDto.ReferenceIssueDate = reference.IssueDate;
                            currentDto.ECF.InformacionReferencia.CodigoModificacion = "3"; // [MODIFIED] 3 = Correction of amounts (Partial)
                            currentDto.ECF.InformacionReferencia.RNCOtroContribuyente = reference.CustomerRnc == currentDto.ECF.Encabezado.Comprador.RNCComprador ? null : reference.CustomerRnc;
                            currentDto.ECF.InformacionReferencia.RazonModificacion = "Ajuste parcial de montos";
                            currentDto.ECF.Encabezado.IdDoc.TipoIngresos = currentDto.ECF.Encabezado.IdDoc.TipoIngresos ?? "01";

                            if (item.Type == 34)
                                currentDto.ECF.Encabezado.IdDoc.IndicadorNotaCredito = "0"; // [MODIFIED] 0 = <= 30 days (Correct for simulation)

                            // Cash payment for notes
                            currentDto.ECF.Encabezado.IdDoc.TipoPago = "1";
                            currentDto.ECF.Encabezado.IdDoc.FechaLimitePago = null;
                            currentDto.ECF.Encabezado.IdDoc.TerminoPago = null;
                        }
                        else
                        {
                            // ── Step 3: Calculate current total from items (base for adjustments)
                            decimal itemsTotal = currentDto.ECF.DetallesItems.Item.Sum(itm => (itm.CantidadItem * itm.PrecioUnitarioItem) - (itm.DescuentoMonto ?? 0));

                            // ── Step 4: Scale item prices proportionally if min/max constraints apply
                            if (item.MinAmount.HasValue && itemsTotal < item.MinAmount.Value)
                            {
                                decimal scaleFactor = item.MinAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                                foreach (var itm in currentDto.ECF.DetallesItems.Item)
                                    itm.PrecioUnitarioItem = Math.Round(itm.PrecioUnitarioItem * scaleFactor, 2);
                            }
                            else if (item.MaxAmount.HasValue && itemsTotal > item.MaxAmount.Value)
                            {
                                decimal scaleFactor = item.MaxAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                                foreach (var itm in currentDto.ECF.DetallesItems.Item)
                                    itm.PrecioUnitarioItem = Math.Round(itm.PrecioUnitarioItem * scaleFactor, 2);
                            }

                            // ── Step 5: Add variety (small offset per step to avoid duplicate rejection)
                            if (currentDto.ECF.DetallesItems.Item.Any())
                            {
                                currentDto.ECF.DetallesItems.Item[0].PrecioUnitarioItem += status.CurrentStep;
                                currentDto.ECF.DetallesItems.Item[0].MontoItem = 0;
                            }

                            // ── Step 6: Type-specific header field adjustments only
                            switch (item.Type)
                            {
                                case 31: break;

                                case 32:
                                    currentDto.ECF.Encabezado.IdDoc.TipoPago = "1";
                                    currentDto.ECF.Encabezado.IdDoc.FechaLimitePago = null;
                                    currentDto.ECF.Encabezado.IdDoc.TerminoPago = null;
                                    if (item.MinAmount.HasValue && item.MinAmount.Value >= 250000)
                                    {
                                        currentDto.ECF.Encabezado.IdDoc.TipoIngresos = currentDto.ECF.Encabezado.IdDoc.TipoIngresos ?? "01";
                                        // Ensure we have a valid buyer RNC for large Type 32
                                        if (string.IsNullOrEmpty(currentDto.ECF.Encabezado.Comprador.RNCComprador))
                                        {
                                            currentDto.ECF.Encabezado.Comprador.RNCComprador = "131793916";
                                            currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = "CLIENTE PRUEBA CERTIFICACION";
                                        }
                                    }
                                    break;

                                case 41:
                                    // Type 41 (Compras): RNCComprador is REQUIRED (minOccurs=1)
                                    // The supplier/seller we're buying from — use a valid company RNC
                                    currentDto.ECF.Encabezado.Comprador.RNCComprador = "131793916"; // Valid 9-digit test RNC
                                    currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = "PROVEEDOR DE SERVICIOS SRL";
                                    currentDto.ECF.Encabezado.IdDoc.TipoIngresos = null; // Not in type 41 XSD
                                    currentDto.ECF.Encabezado.IdDoc.TipoPago = "1";
                                    currentDto.ECF.Encabezado.IdDoc.FechaLimitePago = null;
                                    currentDto.ECF.Encabezado.IdDoc.TerminoPago = null;

                                    // [NEW] satisfy XSD 41: <Retencion> is MANDATORY
                                    currentDto.ECF.Encabezado.Totales.TotalITBISRetenido = 0;
                                    currentDto.ECF.Encabezado.Totales.TotalISRRetencion = 0;
                                    foreach (var itm in currentDto.ECF.DetallesItems.Item)
                                    {
                                        itm.MontoITBISRetenido = 0;
                                        itm.MontoISRRetenido = 0;
                                    }
                                    break;

                                case 43:
                                    // Type 43 (Gastos Menores): Comprador is forbidden in XSD.
                                    currentDto.ECF.Encabezado.Comprador.RNCComprador = null;
                                    currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = null;
                                    currentDto.ECF.Encabezado.IdDoc.TipoIngresos = null;
                                    currentDto.ECF.Encabezado.IdDoc.TipoPago = "1";
                                    currentDto.ECF.Encabezado.IdDoc.FechaLimitePago = null;
                                    currentDto.ECF.Encabezado.IdDoc.TerminoPago = null;
                                    break;

                                case 44: break;
                                case 45: break;

                                case 46:
                                    // Type 46 (Exportaciones): TipoPago & TipoIngresos are REQUIRED.
                                    // IMPORTANT: MontoExento and ITBIS sub-totals are FORBIDDEN in Type 46 XSD.
                                    // We must ensure items use BillingIndicator 3 (Gravado 0%).
                                    currentDto.ECF.Encabezado.IdDoc.TipoIngresos = currentDto.ECF.Encabezado.IdDoc.TipoIngresos ?? "01";
                                    currentDto.ECF.Encabezado.IdDoc.TipoPago = currentDto.ECF.Encabezado.IdDoc.TipoPago ?? "1";

                                    // For exports, use Foreign ID and Country instead of local RNC.
                                    currentDto.ECF.Encabezado.Comprador.RNCComprador = null;
                                    currentDto.ECF.Encabezado.Comprador.IdentificadorExtranjero = currentDto.ECF.Encabezado.Comprador.IdentificadorExtranjero ?? $"EX{i + 1:D6}";
                                    currentDto.ECF.Encabezado.Comprador.PaisComprador = currentDto.ECF.Encabezado.Comprador.PaisComprador ?? "USA";

                                    foreach (var itm in currentDto.ECF.DetallesItems.Item)
                                    {
                                        itm.IndicadorFacturacion = "3"; // Type 46 MUST use Tasa Cero (3)
                                        // itm.TaxPercentage = 0;
                                        // 0m = 0;
                                    }
                                    break;

                                case 47:
                                    // Type 47 (Pagos al Exterior): Comprador is optional (minOccurs=0)
                                    // No RNC, no Dominican fields — just foreign identifier
                                    currentDto.ECF.Encabezado.Comprador.RNCComprador = null;
                                    currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = currentDto.ECF.Encabezado.Comprador.RazonSocialComprador ?? "FOREIGN SERVICES PROVIDER";
                                    currentDto.ECF.Encabezado.Comprador.IdentificadorExtranjero = currentDto.ECF.Encabezado.Comprador.IdentificadorExtranjero ?? $"FOREIGN{i + 1:D6}";
                                    currentDto.ECF.Encabezado.Comprador.PaisComprador = null; // PaisComprador excluded for 47 in serializer
                                    currentDto.ECF.Encabezado.Comprador.DireccionComprador = null;
                                    currentDto.ECF.Encabezado.IdDoc.TipoIngresos = null; // Not in type 47 XSD
                                    currentDto.ECF.Encabezado.IdDoc.TipoPago = "1";
                                    currentDto.ECF.Encabezado.IdDoc.FechaLimitePago = null;
                                    currentDto.ECF.Encabezado.IdDoc.TerminoPago = null;

                                    // [NEW] satisfy XSD 47: <Retencion> is MANDATORY
                                    currentDto.ECF.Encabezado.Totales.TotalITBISRetenido = 0;
                                    currentDto.ECF.Encabezado.Totales.TotalISRRetencion = 0;
                                    foreach (var itm in currentDto.ECF.DetallesItems.Item)
                                    {
                                        itm.IndicadorFacturacion = "4"; // Force Exento for 47
                                        // itm.TaxPercentage = 0;
                                        // 0m = 0;
                                        itm.MontoITBISRetenido = 0;
                                        itm.MontoISRRetenido = 0;
                                        // itm.IsrRetentionAmount = 0;
                                    }
                                    break;
                            }
                        }
                    }

                    // [NEW] For RFCE (Summary), we need manual totals because it doesn't have Items to calculate from
                    if (item.IsSummary)
                    {
                        // Calculate totals from the current items before they are ignored by summary generator
                        currentDto.ECF.Encabezado.Totales.MontoGravadoTotal = currentDto.ECF.DetallesItems.Item.Where(it => it.IndicadorFacturacion == "1").Sum(it => it.CantidadItem * it.PrecioUnitarioItem);
                        currentDto.ECF.Encabezado.Totales.MontoExento = currentDto.ECF.DetallesItems.Item.Where(it => it.IndicadorFacturacion == "4").Sum(it => it.CantidadItem * it.PrecioUnitarioItem);
                        currentDto.ECF.Encabezado.Totales.TotalITBIS = currentDto.ECF.DetallesItems.Item.Sum(it => 0m);
                        currentDto.ECF.Encabezado.Totales.MontoTotal = currentDto.ECF.DetallesItems.Item.Sum(it => (it.CantidadItem * it.PrecioUnitarioItem) );

                        // [NEW] For RFCE B2C simulation, clear customer to ensure anonymous
                        currentDto.ECF.Encabezado.Comprador.RNCComprador = null;
                        currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = "CONSUMIDOR FINAL";
                                     // [NEW] Synchronize Security Code with the signature of the upcoming individual document
                        try
                        {
                            // Peek at the sequence to know the NCF the individual will use
                            var ecfTypeRecordForInd = await _context.Set<Core.Entities.EcfType>().FirstOrDefaultAsync(t => t.Code == item.Type.ToString());
                            var encfRecordForInd = await _context.ENcfs.FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecordForInd!.EcfTypeId && e.ClientId == client.ClientId);
                            int seqForInd = encfRecordForInd?.Sequence ?? 1;
                            string realNcfForInd = $"E{item.Type}{seqForInd:D10}";

                            var indDto = CloneDto(currentDto)!;
                            indDto.ECF.Encabezado.IdDoc.eNCF = realNcfForInd;
                            indDto.SignatureDateOverride = signatureDate; // [FIX] Lock signature date for consistency
                            // Ensure the dry-run uses individual mode
                            string indUnsigned = _generatorService.GenerateUnsignedXml(indDto, false);
                            string indSigned = _signerService.SignXml(indUnsigned, certBase64, certPass);

                            string tag = "<SignatureValue>";
                            var start = indSigned.IndexOf(tag);
                            if (start != -1)
                            {
                                var content = indSigned.Substring(start + tag.Length).TrimStart();
                                var realCode = content.Substring(0, 6);
                                currentDto.SecurityCodeOverride = realCode;
                                indDto.SecurityCodeOverride = realCode;
                                indDtoForPool = indDto; // [NEW] Store the EXACT DTO that was signed
                                Console.WriteLine($"[RFCE] Sincronizando Código de Seguridad: {realCode} (NCF: {realNcfForInd})");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARN] Error sincronizando código RFCE: {ex.Message}");
                        }
                    }

                    // Apply global signature date override to ensure consistency across all steps
                    currentDto.SignatureDateOverride = signatureDate;

                    // D. Generate with temp NCF for XSD validation BEFORE consuming sequence
                    string realNcfBeforeValidation = currentDto.ECF.Encabezado.IdDoc.eNCF;
                    currentDto.ECF.Encabezado.IdDoc.eNCF = $"E{item.Type}0000000000"; // Temp NCF for validation only
                    string unsignedXmlTemp = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);
                    
                    // Restore NCF immediately after generating the temp XML for validation
                    currentDto.ECF.Encabezado.IdDoc.eNCF = realNcfBeforeValidation;

                    // XSD Validation BEFORE sequence management (to avoid burning NCFs)
                    var xsdErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXmlTemp, item.Type);
                    if (xsdErrors.Any())
                    {
                        string msg = string.Join(" | ", xsdErrors.Take(3));
                        Console.WriteLine($"[ERR] Error XSD en Paso {status.CurrentStep} (Tipo {item.Type}): {msg}");
                        status.CompletedSteps.Add(new CertificationStepResultDto
                        {
                            Index = status.CurrentStep,
                            Ncf = currentDto.ECF.Encabezado.IdDoc.eNCF,
                            Status = "Error XSD",
                            Message = msg
                        });
                        // DO NOT consume sequence - skip without incrementing
                        continue;
                    }

                    // B. Sequence Management (AFTER XSD validation passes)
                    var ecfTypeRecord = await _context.Set<Core.Entities.EcfType>().FirstOrDefaultAsync(t => t.Code == item.Type.ToString());
                    if (ecfTypeRecord == null) throw new Exception($"Tipo de e-CF {item.Type} no soportado en la base de datos.");

                    ENcf? encfRecord = null;
                    if (!skipNcfConsumption)
                    {
                        encfRecord = await _context.ENcfs.FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecord.EcfTypeId && e.ClientId == client.ClientId);
                        if (encfRecord == null)
                        {
                            encfRecord = new ENcf { NcfTypeId = ecfTypeRecord.EcfTypeId, ClientId = client.ClientId, Sequence = 1, RegisteredAt = DateTime.Now };
                            _context.ENcfs.Add(encfRecord);
                            await _context.SaveChangesAsync();
                        }

                        int seq = encfRecord.Sequence++;
                        currentDto.ECF.Encabezado.IdDoc.eNCF = $"E{item.Type}{seq:D10}";
                        _context.Entry(encfRecord).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        // [NEW] If this is a summary, store it in the pool AFTER we have the real NCF
                        if (item.IsSummary)
                        {
                            // [NEW] Store the individual DTO that was used for the dry-run signature
                            rfcePool.Add((currentDto.ECF.Encabezado.IdDoc.eNCF, currentDto.SecurityCodeOverride ?? "", indDtoForPool ?? CloneDto(currentDto)!));
                        }
                    }

                    // Re-generate with the real NCF
                    string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);


                    string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                    // [NEW] Collect for final ZIP with differentiation
                    string zipName = (item.IsManual ? "SUBIR_DGII_" : "") + $"Paso_{status.CurrentStep}_{currentDto.ECF.Encabezado.IdDoc.eNCF}.xml";
                    simulationXmls[zipName] = signedXml;

                    // [NEW] Capture the DTO JSON payload for example documentation
                    string jsonZipName = (item.IsManual ? "SUBIR_DGII_" : "") + $"Paso_{status.CurrentStep}_{currentDto.ECF.Encabezado.IdDoc.eNCF}.json";
                    simulationJsons[jsonZipName] = System.Text.Json.JsonSerializer.Serialize(currentDto, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                    // E. Transmission
                    // [NEW] Console Logging [TX]
                    Console.WriteLine($"[TX] Enviando e-CF {currentDto.ECF.Encabezado.IdDoc.eNCF} tipo {item.Type} (Paso {status.CurrentStep}/{status.TotalSteps})...");

                    decimal actualTransmissionTotal = currentDto.ECF.Encabezado.Totales.MontoTotal ?? 0;
                    if (actualTransmissionTotal == 0 && currentDto.ECF.DetallesItems.Item.Any())
                    {
                        actualTransmissionTotal = currentDto.ECF.DetallesItems.Item.Sum(itm => 
                            (itm.CantidadItem * itm.PrecioUnitarioItem) 
                            - (itm.DescuentoMonto ?? 0) 
                            + 0m 
                            + (itm.RecargoMonto ?? 0)
                            + ((itm.IscSpecificAmount ?? 0) + (itm.IscAdvaloremAmount ?? 0) + (itm.OtherAdditionalTaxAmount ?? 0)));
                    }

                    bool isAccepted = false;
                    string? trackId = null;
                    string? error = null;
                    string? downloadUrl = null;

                    if (item.IsManual)
                    {
                        // For manual steps, just mark as generated and save
                        isAccepted = true;
                        trackId = "MANUAL";
                        Console.WriteLine($"[TX] Paso MANUAL {currentDto.ECF.Encabezado.IdDoc.eNCF} tipo {item.Type} - Marcado como generado.");
                    }
                    else
                    {
                        var result = await _transmissionService.SendEcfAsync(
                            DgiiEnvironment.CerteCF,
                            token,
                            signedXml,
                            item.Type,
                            actualTransmissionTotal,
                            dto.ECF.Encabezado.Emisor.RNCEmisor,
                            currentDto.ECF.Encabezado.IdDoc.eNCF,
                            item.IsSummary);

                        if (result.Success)
                        {
                            if (!string.IsNullOrEmpty(result.TrackId))
                            {
                                // [NEW] Stability wait of 2 seconds as requested before polling
                                await Task.Delay(2000);

                                // Poll DGII to get actual acceptance/rejection
                                var finalStatus = await PollDgiiStatusAsync(result.TrackId, dto.ECF.Encabezado.Emisor.RNCEmisor);
                                isAccepted = finalStatus.Estado == "Aceptado" || (item.IsSummary && finalStatus.Estado == "Generado");
                                trackId = result.TrackId;

                                // [NEW] Console Logging [RX]
                                Console.WriteLine($"[RX] Resultado e-CF {currentDto.ECF.Encabezado.IdDoc.eNCF}: {finalStatus.Estado}");

                                if (!isAccepted)
                                    error = $"DGII: {finalStatus.Estado} - {string.Join(" | ", finalStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "Sin mensaje" })}";
                            }
                            else if (item.IsSummary && result.Estado == "Aceptado")
                            {
                                // Immediate acceptance for RFCE (no TrackId)
                                isAccepted = true;
                                trackId = "INMEDIATO";
                                Console.WriteLine($"[RX] Resultado e-CF {currentDto.ECF.Encabezado.IdDoc.eNCF}: Aceptado (Inmediato)");
                            }
                            else
                            {
                                isAccepted = false;
                                error = "DGII: No se recibió TrackId ni estado de aceptación inmediata.";
                            }
                        }
                        else
                        {
                            isAccepted = false;
                            error = string.IsNullOrEmpty(result.Error) ? $"DGII {result.Estado}: {result.Mensaje}" : result.Error;
                            Console.WriteLine($"[RX] Error en envío e-CF {currentDto.ECF.Encabezado.IdDoc.eNCF}: {error}");
                        }
                    }

                    if (item.IsManual)
                    {
                        // Manual documents for Step 4 download
                        string fileName = $"cert_test_{status.CurrentStep}_{currentDto.ECF.Encabezado.IdDoc.eNCF}.xml";
                        string fullPath = Path.Combine(webRootPath, "certification_files", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                        await File.WriteAllTextAsync(fullPath, signedXml);
                        downloadUrl = $"/certification_files/{fileName}";
                    }

                    // Save ALL sent documents (accepted or rejected) to allow audit trail
                    var doc = new CertificationDocument
                    {
                        CertificationProcessId = process.CertificationProcessId,
                        ENcfSecuence = currentDto.ECF.Encabezado.IdDoc.eNCF,
                        ENcfId = encfRecord?.ENcfId ?? (await _context.ENcfs.FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecord.EcfTypeId && e.ClientId == client.ClientId))?.ENcfId ?? 0,
                        EcfTypeId = ecfTypeRecord.EcfTypeId,
                        XmlSent = signedXml,
                        TrackId = trackId,
                        Status = isAccepted ? DocumentStatus.Accepted : DocumentStatus.Rejected,
                        SentAt = DateTime.Now,
                        RegisteredAt = DateTime.Now
                    };
                    _context.CertificationDocuments.Add(doc);
                    sentDocsThisRun.Add(doc);
                    await _context.SaveChangesAsync();

                    // Track Type 31 references for notes (33/34)
                    if (item.Type == 31 && isAccepted && currentDto != null && accepted31Pool != null)
                    {
                        var cloned = CloneDto(currentDto);
                        if (cloned != null)
                        {
                            accepted31Pool.Add((currentDto.ECF.Encabezado.IdDoc.eNCF, DateTime.Parse(currentDto.ECF.Encabezado.Emisor.FechaEmision), currentDto.ECF.Encabezado.Comprador.RNCComprador, cloned));
                        }
                    }

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = currentDto.ECF.Encabezado.IdDoc.eNCF,
                        Status = isAccepted ? "Aceptado" : "Rechazado",
                        Message = isAccepted ? (item.IsManual ? $"Manual: {downloadUrl}" : $"TrackId: {trackId}") : error
                    });

                    // [NEW] Stop simulation on first error as requested
                    if (!isAccepted)
                    {
                        status.Status = "Failed";
                        status.ErrorMessage = $"Error en NCF {currentDto.ECF.Encabezado.IdDoc.eNCF}: {error}";
                        goto EndOfJob;
                    }
                }
            }

        EndOfJob:
            if (status.Status != "Failed")
            {
                process.Status = CertificationStatus.Approved;
                process.EndDate = DateTime.Now;
                status.Status = "Completed";
                Console.WriteLine($"[INFO] Simulación finalizada exitosamente para {dto.ECF.Encabezado.Emisor.RNCEmisor}.");
            }
            else
            {
                process.Status = CertificationStatus.Rejected;
                process.EndDate = DateTime.Now;
                Console.WriteLine($"[ERROR] Simulación detenida por error en {dto.ECF.Encabezado.Emisor.RNCEmisor}.");
            }

            await _context.SaveChangesAsync();

            // [NEW] Generate ZIP results for the simulation
            if (simulationXmls.Any())
            {
                try
                {
                    string zipDir = Path.Combine(webRootPath, "certification_files");
                    if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);

                    string zipPath = Path.Combine(zipDir, $"simulacion_{jobId}.zip");
                    using (var zipStream = new FileStream(zipPath, FileMode.Create))
                    using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var entry in simulationXmls)
                        {
                            var zipEntry = archive.CreateEntry(entry.Key, System.IO.Compression.CompressionLevel.Optimal);
                            using (var entryStream = zipEntry.Open())
                            using (var writer = new StreamWriter(entryStream))
                            {
                                writer.Write(entry.Value);
                            }
                        }
                    }
                    status.DownloadUrl = $"/certification_files/simulacion_{jobId}.zip";
                    Console.WriteLine($"[INFO] ZIP de simulación generado: {status.DownloadUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERR] Error generando ZIP de simulación: {ex.Message}");
                }
            }

            // [NEW] Generate ZIP results for JSON payloads
            if (simulationJsons.Any())
            {
                try
                {
                    string zipDir = Path.Combine(webRootPath, "certification_files");
                    if (!Directory.Exists(zipDir)) Directory.CreateDirectory(zipDir);

                    string zipPath = Path.Combine(zipDir, $"simulacion_json_{jobId}.zip");
                    using (var zipStream = new FileStream(zipPath, FileMode.Create))
                    using (var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Create, true))
                    {
                        foreach (var entry in simulationJsons)
                        {
                            var zipEntry = archive.CreateEntry(entry.Key, System.IO.Compression.CompressionLevel.Optimal);
                            using (var entryStream = zipEntry.Open())
                            using (var writer = new StreamWriter(entryStream))
                            {
                                writer.Write(entry.Value);
                            }
                        }
                    }
                    status.JsonDownloadUrl = $"/certification_files/simulacion_json_{jobId}.zip";
                    Console.WriteLine($"[INFO] ZIP de JSONs de simulación generado: {status.JsonDownloadUrl}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERR] Error generando ZIP de JSONs de simulación: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;

            try
            {
                var client = await _clientService.GetByAsync(c => c.Rnc == dto.ECF.Encabezado.Emisor.RNCEmisor);
                if (client != null)
                {
                    var process = await _context.CertificationProcesses
                        .OrderByDescending(p => p.RegisteredAt)
                        .FirstOrDefaultAsync(p => p.ClientId == client.ClientId && p.Status == CertificationStatus.InProgress);
                    if (process != null)
                    {
                        process.Status = CertificationStatus.Rejected;
                        process.EndDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }
        }
    }

    public async Task<string> ProcessSimulacionUnoAUnoAsync(EcfInvoiceRequestDto dto, string webRootPath)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto), "No se proporcionó el comprobante.");

        try
        {
            // 1. Validate Client
            var client = await _clientService.GetByAsync(c => c.Rnc == dto.ECF.Encabezado.Emisor.RNCEmisor)
                ?? throw new Exception($"Cliente con RNC {dto.ECF.Encabezado.Emisor.RNCEmisor} no encontrado.");

            // 2. Prepare Credentials
            var apiKey = await _apiKeyService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new Exception("ApiKey no encontrada.");
            var decryptedSecretKey = _encryptedService.DecryptString(apiKey.SecretKey);
            var certificate = await _clientCertificateService.GetByAsync(x => x.ClientId == client.ClientId)
                ?? throw new Exception("Certificado no encontrado.");
            var certificateBytes = _encryptedService.DecryptWithSecret(certificate.Certificate, decryptedSecretKey);
            var passwordBytes = _encryptedService.DecryptWithSecret(certificate.Password, decryptedSecretKey);
            var certBase64 = Convert.ToBase64String(certificateBytes);
            var certPass = Encoding.UTF8.GetString(passwordBytes);

            string token = await _authService.GetTokenAsync(dto.ECF.Encabezado.Emisor.RNCEmisor, DgiiEnvironment.CerteCF, certBase64, certPass);

            var signatureDate = DateTime.Now;
            var currentDto = dto;
            currentDto.SignatureDateOverride = signatureDate;
            
            int ecfType = int.TryParse(currentDto.ECF.Encabezado.IdDoc.TipoeCF, out var t) ? t : (string.IsNullOrEmpty(currentDto.ECF.Encabezado.IdDoc.eNCF) ? 31 : int.Parse(currentDto.ECF.Encabezado.IdDoc.eNCF.Substring(1, 2)));

            if (ecfType == 32) // RFCE Summary
            {
                // Validate totals from the current items before processing
                decimal calculatedGravado = currentDto.ECF.DetallesItems.Item.Where(it => it.IndicadorFacturacion == "1").Sum(it => it.CantidadItem * it.PrecioUnitarioItem);
                decimal calculatedExento = currentDto.ECF.DetallesItems.Item.Where(it => it.IndicadorFacturacion == "4").Sum(it => it.CantidadItem * it.PrecioUnitarioItem);
                decimal calculatedITBIS = currentDto.ECF.DetallesItems.Item.Sum(it => 0m);
                decimal calculatedTotal = currentDto.ECF.DetallesItems.Item.Sum(it => (it.CantidadItem * it.PrecioUnitarioItem) );

                if (Math.Abs((currentDto.ECF.Encabezado.Totales.MontoGravadoTotal ?? 0) - calculatedGravado) > 0.01m)
                {
                    throw new Exception($"Discrepancia en Monto Gravado. Enviado: {currentDto.ECF.Encabezado.Totales.MontoGravadoTotal}, Calculado: {calculatedGravado}");
                }
                if (Math.Abs((currentDto.ECF.Encabezado.Totales.MontoExento ?? 0) - calculatedExento) > 0.01m)
                {
                    throw new Exception($"Discrepancia en Monto Exento. Enviado: {currentDto.ECF.Encabezado.Totales.MontoExento}, Calculado: {calculatedExento}");
                }
                if (Math.Abs((currentDto.ECF.Encabezado.Totales.TotalITBIS ?? 0) - calculatedITBIS) > 0.01m)
                {
                    throw new Exception($"Discrepancia en Total ITBIS. Enviado: {currentDto.ECF.Encabezado.Totales.TotalITBIS}, Calculado: {calculatedITBIS}");
                }
                if (Math.Abs((currentDto.ECF.Encabezado.Totales.MontoTotal ?? 0) - calculatedTotal) > 0.01m)
                {
                    throw new Exception($"Discrepancia en Monto Total. Enviado: {currentDto.ECF.Encabezado.Totales.MontoTotal}, Calculado: {calculatedTotal}");
                }

                // For RFCE B2C simulation, clear customer to ensure anonymous
                currentDto.ECF.Encabezado.Comprador.RNCComprador = null;
                currentDto.ECF.Encabezado.Comprador.RazonSocialComprador = "CONSUMIDOR FINAL";

                // 1. Dry-run Individual signature to get Security Code
                var indDto = CloneDto(currentDto)!;
                indDto.SignatureDateOverride = signatureDate;
                
                string indUnsigned = _generatorService.GenerateUnsignedXml(indDto, false);
                string indSigned = _signerService.SignXml(indUnsigned, certBase64, certPass);

                string tag = "<SignatureValue>";
                var start = indSigned.IndexOf(tag);
                if (start != -1)
                {
                    var content = indSigned.Substring(start + tag.Length).TrimStart();
                    var realCode = content.Substring(0, 6);
                    currentDto.SecurityCodeOverride = realCode;
                    indDto.SecurityCodeOverride = realCode;
                }

                // 2. Generate Summary XML
                string unsignedSummaryXml = _generatorService.GenerateUnsignedXml(currentDto, true);
                string signedSummaryXml = _signerService.SignXml(unsignedSummaryXml, certBase64, certPass);

                // 3. Save Manual individual document
                string indUnsignedFinal = _generatorService.GenerateUnsignedXml(indDto, false);
                string indSignedFinal = _signerService.SignXml(indUnsignedFinal, certBase64, certPass);

                string fileName = $"cert_test_manual_{indDto.ECF.Encabezado.IdDoc.eNCF}.xml";
                string fullPath = Path.Combine(webRootPath, "certification_files", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, indSignedFinal);

                // 4. Send Summary via API
                decimal summaryTotal = currentDto.ECF.Encabezado.Totales.MontoTotal ?? currentDto.ECF.DetallesItems.Item.Sum(itm => (itm.CantidadItem * itm.PrecioUnitarioItem) + 0m);
                var result = await _transmissionService.SendEcfAsync(
                    DgiiEnvironment.CerteCF,
                    token,
                    signedSummaryXml,
                    ecfType,
                    summaryTotal,
                    currentDto.ECF.Encabezado.Emisor.RNCEmisor,
                    currentDto.ECF.Encabezado.IdDoc.eNCF,
                    true);
                
                if (result.Success && !string.IsNullOrEmpty(result.TrackId))
                {
                    await Task.Delay(2000);
                    var finalStatus = await PollDgiiStatusAsync(result.TrackId, currentDto.ECF.Encabezado.Emisor.RNCEmisor);
                    result.Estado = finalStatus.Estado;
                    result.Mensaje = string.Join(" | ", finalStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "Sin mensaje" });
                }
                
                return indSignedFinal;
            }
            else // Non-RFCE document
            {
                string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, false);
                string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                decimal actualTransmissionTotal = currentDto.ECF.Encabezado.Totales.MontoTotal ?? 0;
                if (actualTransmissionTotal == 0 && currentDto.ECF.DetallesItems.Item.Any())
                {
                    actualTransmissionTotal = currentDto.ECF.DetallesItems.Item.Sum(itm => 
                        (itm.CantidadItem * itm.PrecioUnitarioItem) 
                        - (itm.DescuentoMonto ?? 0) 
                        + 0m 
                        + (itm.RecargoMonto ?? 0)
                        + ((itm.IscSpecificAmount ?? 0) + (itm.IscAdvaloremAmount ?? 0) + (itm.OtherAdditionalTaxAmount ?? 0)));
                }

                var result = await _transmissionService.SendEcfAsync(
                    DgiiEnvironment.CerteCF,
                    token,
                    signedXml,
                    ecfType,
                    actualTransmissionTotal,
                    currentDto.ECF.Encabezado.Emisor.RNCEmisor,
                    currentDto.ECF.Encabezado.IdDoc.eNCF,
                    false);
                
                if (result.Success && !string.IsNullOrEmpty(result.TrackId))
                {
                    await Task.Delay(2000);
                    var finalStatus = await PollDgiiStatusAsync(result.TrackId, currentDto.ECF.Encabezado.Emisor.RNCEmisor);
                    result.Estado = finalStatus.Estado;
                    result.Mensaje = string.Join(" | ", finalStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "Sin mensaje" });
                }

                return System.Text.Json.JsonSerializer.Serialize(result);
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    private EcfInvoiceRequestDto? CloneDto(EcfInvoiceRequestDto source)
    {
        return Tools.DeepClone(source);
    }

    private string GenerateRandomCode(int length)
    {
        return Tools.GenerateRandomCode(length);
    }
}
