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
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Dtos;

using System.IO.Compression;
using ZynstormECFPlatform.Core.Entities;
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

            var requestDto = MapRowToRequest(row, test.Step);

            string issuerRnc = requestDto.IssuerRnc;
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
                            requestDto.IssuerWebSite = $"[DEBUG-PRECALC] RNC={individualDto.IssuerRnc}, NCF={individualDto.Ncf}, Date={individualDto.IssueDate:yyyy-MM-dd}, Total={individualDto.ManualMontoTotal}";
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
            EcfType = int.TryParse(GetStr(row, "TipoeCF"), out int t) ? t : 0,
            Ncf = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "CasoPrueba") ?? "") ?? "",
            IssuerRnc = GetStr(row, "RNCEmisor") ?? "",
            IssuerName = GetStr(row, "RazonSocialEmisor") ?? "",
            IssuerAddress = GetStr(row, "DireccionEmisor") ?? "",
            IssuerCommercialName = GetStr(row, "NombreComercial"),
            IssuerBranchCode = GetStr(row, "Sucursal"),
            IssuerActivityCode = GetStr(row, "ActividadEconomica"),
            IssuerMunicipality = GetStr(row, "Municipio"),
            IssuerProvince = GetStr(row, "Provincia"),
            IssuerPhone = GetStr(row, "TelefonoEmisor[1]"),
            IssuerEmail = GetStr(row, "CorreoEmisor"),
            IssuerWebSite = GetStr(row, "WebSite"),
            IssuerSellerCode = GetStr(row, "CodigoVendedor"),
            CustomerRnc = GetStr(row, "RNCComprador") ?? "",
            CustomerForeignId = GetStr(row, "IdentificadorExtranjero"),
            CustomerName = GetStr(row, "RazonSocialComprador") ?? "",
            CustomerCountry = GetStr(row, "PaisComprador"),
            CustomerAddress = GetStr(row, "DireccionComprador"),
            CustomerContact = GetStr(row, "ContactoComprador"),
            CustomerTelephone = GetStr(row, "TelefonoAdicional"),
            CustomerEmail = GetStr(row, "CorreoComprador"),
            CustomerMunicipality = GetStr(row, "MunicipioComprador"),
            CustomerProvince = GetStr(row, "ProvinciaComprador"),
            BuyerInternalCode = GetStr(row, "CodigoInternoComprador"),
            InternalInvoiceNumber = GetStr(row, "NumeroFacturaInterna"),
            InternalOrderNumber = GetStr(row, "NumeroPedidoInterno"),
            SalesZone = GetStr(row, "ZonaVenta"),
            OrderNumber = GetStr(row, "NumeroOrdenCompra"),
            PaymentType = int.TryParse(GetStr(row, "TipoPago"), out int p) ? p : null,
            PaymentDeadline = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaLimitePago"))),
            IncomeType = GetStr(row, "TipoIngresos"),
            ManualMontoGravadoTotal = GetDec(row, "MontoGravadoTotal"),
            ManualMontoExento = GetDec(row, "MontoExento"),
            ManualMontoTotal = GetDec(row, "MontoTotal"),
            ManualTotalITBIS = GetDec(row, "TotalITBIS"),
            ManualTotalITBIS1 = GetDec(row, "TotalITBIS1"),
            ManualTotalITBIS2 = GetDec(row, "TotalITBIS2"),
            ManualTotalITBIS3 = GetDec(row, "TotalITBIS3"),
            ManualMontoPeriodo = GetDec(row, "MontoPeriodo"),
            ManualValorPagar = GetDec(row, "ValorPagar"),
            ManualIndicadorMontoGravado = int.TryParse(GetStr(row, "IndicadorMontoGravado"), out int img) ? img : null,
            ManualTotalITBISRetenido = GetDec(row, "TotalITBISRetenido"),
            ManualTotalISRRetencion = GetDec(row, "TotalISRRetencion"),
            ManualMontoGravadoI1 = GetDec(row, "MontoGravadoI1"),
            ManualIndicadorNotaCredito = int.TryParse(GetStr(row, "IndicadorNotaCredito"), out int inc) ? inc : null,
            ManualMontoNoFacturable = GetDec(row, "MontoNoFacturable"),
            ManualMontoGravadoI2 = GetDec(row, "MontoGravadoI2"),
            ManualMontoGravadoI3 = GetDec(row, "MontoGravadoI3"),
            ReferenceNcf = CleanNcf(GetStr(row, "NCFModificado")),
            ReferenceCustomerRnc = GetStr(row, "RNCOtroContribuyente"),
            ReferenceReasonCode = int.TryParse(GetStr(row, "CodigoModificacion"), out int rc) ? rc : null,
            ReferenceReasonDescription = GetStr(row, "RazonModificacion"),
            Items = new List<EcfItemRequestDto>()
        };

        dto.IssueDate = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaEmision")) ?? fallbackDate ?? DateTime.Now);
        // dto.SignatureDateOverride = dto.IssueDate.Date.AddHours(12); // Removed as requested

        var expDate = ParseDgiiDate(GetStr(row, "FechaVencimientoSecuencia"));
        string typeStr = GetStr(row, "TipoeCF") ?? "";
        if (!expDate.HasValue && _typeExpirationDates != null && _typeExpirationDates.TryGetValue(typeStr, out DateTime fallback))
        {
            expDate = fallback;
        }
        dto.SequenceExpirationDate = ApplyDateOffset(expDate);

        dto.DeliveryDate = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaEntrega")));
        dto.OrderDate = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaOrdenCompra")));
        dto.ReferenceIssueDate = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaNCFModificado")));

        var montoNoFacturableStr = GetStr(row, "MontoNoFacturable");
        if (decimal.TryParse(montoNoFacturableStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal mnf) && mnf > 0)
            dto.MontoNoFacturable = mnf;

        for (int i = 1; i <= 50; i++)
        {
            var nombreKey = $"NombreItem[{i}]";
            var nombre = GetStr(row, nombreKey);
            if (nombre == null) continue;

            var indicadorStr = GetStr(row, $"IndicadorFacturacion[{i}]");
            var indicadorFact = int.TryParse(indicadorStr, out int indF) ? (int?)indF : null;

            decimal taxPct = indicadorFact switch
            {
                1 => 18m,
                2 => 16m,
                3 => 0m,
                4 => 0m,
                0 => 0m,
                _ => 18m
            };

            var cantStr = GetStr(row, $"CantidadItem[{i}]");
            var precioStr = GetStr(row, $"PrecioUnitarioItem[{i}]");
            var bienServStr = GetStr(row, $"IndicadorBienoServicio[{i}]");
            var unidadStr = GetStr(row, $"UnidadMedida[{i}]");

            decimal cantidad = decimal.TryParse(cantStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal c) ? c : 1;
            decimal precio = decimal.TryParse(precioStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal pr) ? pr : 0;
            int? itemType = int.TryParse(bienServStr, out int bs) ? (int?)bs : null;
            int? unitOfMeasure = int.TryParse(unidadStr, out int um) ? (int?)um : null;

            var item = new EcfItemRequestDto
            {
                Name = nombre,
                Description = GetStr(row, $"DescripcionItem[{i}]"),
                Quantity = cantidad,
                UnitPrice = precio,
                ItemType = itemType,
                UnitOfMeasure = unitOfMeasure,
                TaxPercentage = taxPct,
                BillingIndicator = indicadorFact,
                ManualMontoItem = GetDec(row, $"MontoItem[{i}]"),
                ManualDescuentoMonto = GetDec(row, $"DescuentoMonto[{i}]"),
                ManualRecargoMonto = GetDec(row, $"RecargoMonto[{i}]"),
                ManualMontoITBISRetenido = GetDec(row, $"MontoITBISRetenido[{i}]"),
                ManualMontoISRRetenido = GetDec(row, $"MontoISRRetenido[{i}]"),
                FechaElaboracion = GetStr(row, $"FechaElaboracion[{i}]"),
                FechaVencimientoItem = GetStr(row, $"FechaVencimientoItem[{i}]")
            };

            for (int k = 1; k <= 5; k++)
            {
                var subTipo = GetStr(row, $"TipoSubRecargo[{i}][{k}]");
                var subMonto = GetDec(row, $"MontosubRecargo[{i}][{k}]");
                if (subTipo != null || subMonto != null)
                {
                    item.ManualSubRecargos.Add(new EcfSubRecargoDto
                    {
                        TipoSubRecargo = subTipo ?? "$",
                        MontoSubRecargo = subMonto ?? 0,
                        SubRecargoPorcentaje = GetDec(row, $"SubRecargoPorcentaje[{i}][{k}]")
                    });
                }
            }

            dto.Items.Add(item);

            // [NEW] For Credit (33) and Debit (34) Notes, only affect one row as requested
            if ((dto.EcfType == 33 || dto.EcfType == 34) && dto.Items.Count >= 1)
                break;
        }

        // [NEW] Default ReferenceReasonCode to 3 (Correction of amounts) for notes if not provided
        if ((dto.EcfType == 33 || dto.EcfType == 34) && !dto.ReferenceReasonCode.HasValue)
        {
            dto.ReferenceReasonCode = 3;
            dto.ReferenceReasonDescription ??= "Ajuste parcial de montos";
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

        if (!_jobStatuses.ContainsKey(jobId))
        {
            _jobStatuses[jobId] = new CertificationJobStatusDto { JobId = jobId, Status = "Processing" };
        }

        var status = _jobStatuses[jobId];
        status.Status = "Processing";

        try
        {
            // 1. Validate Client
            var client = await _clientService.GetByAsync(c => c.Rnc == dto.IssuerRnc)
                ?? throw new Exception($"Cliente con RNC {dto.IssuerRnc} no encontrado.");

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
                // When the simulation restarts, mark ALL previously sent documents as Rejected
                // to reflect the DGII's "reinicio" state. The send history is still kept in DB
                // so the next run can skip re-sending them.
                // Exception: if the process is already Approved/Completed, leave everything as-is.
                var docsToInvalidate = await _context.CertificationDocuments
                    .Where(d => d.CertificationProcessId == process.CertificationProcessId
                             && d.Status != DocumentStatus.Rejected) // only update non-rejected ones
                    .ToListAsync();

                if (docsToInvalidate.Any())
                {
                    foreach (var doc in docsToInvalidate)
                        doc.Status = DocumentStatus.Rejected;

                    await _context.SaveChangesAsync();
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

            string token = await _authService.GetTokenAsync(dto.IssuerRnc, DgiiEnvironment.CerteCF, certBase64, certPass);

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
                (32, 4, true, false, 10, 249999),
                // Paso 5: Consumo Individual B2C < 250k (Manual upload - 4 requeridos)
                (32, 4, false, true, 10, 249999)
            };

            status.TotalSteps = matrix.Sum(m => m.Count);
            status.CurrentStep = 0;

            // In-memory list to track documents sent in THIS run (for DB persistence)
            var sentDocsThisRun = new List<CertificationDocument>();

            foreach (var item in matrix)
            {
                for (int i = 0; i < item.Count; i++)
                {
                    status.CurrentStep++;

                    var currentDto = CloneDto(dto);
                    currentDto.EcfType = item.Type;
                    currentDto.SequenceExpirationDate = new DateTime(2028, 12, 31);

                    bool isNote = item.Type == 33 || item.Type == 34;

                    // ── Step 1: Force exempt, clear retentions for all Type 31s
                    foreach (var itm in currentDto.Items)
                    {
                        itm.BillingIndicator = 4; // Exento
                        itm.TaxPercentage = 0;
                        itm.ItbisAmount = 0;
                        itm.ManualMontoISRRetenido = null;
                        itm.IsrRetentionAmount = null;
                        itm.ManualMontoITBISRetenido = null;
                        itm.ManualMontoItem = null; // Let generator recalculate
                    }

                    // ── Step 2: Clear manual totals so the generator calculates from items
                    currentDto.ManualMontoGravadoTotal = null;
                    currentDto.ManualTotalITBIS = null;
                    currentDto.ManualMontoExento = null;
                    currentDto.ManualTotalISRRetencion = null;
                    currentDto.ManualTotalITBISRetenido = null;
                    currentDto.ManualMontoTotal = null;

                    // ── Step 3-6: For Notes (33/34), use EXACT clone of the referenced Type 31
                    //             For all other types, apply the standard adjustments

                    if (isNote)
                    {
                        // Use references from the pool
                        // Type 33 -> uses 1st accepted 31 (index 0)
                        // Type 34 -> uses 2nd and 3rd accepted 31 (index 1, 2, ...)
                        int poolIndex = (item.Type == 33) ? i : (1 + i);

                        if (accepted31Pool.Count <= poolIndex)
                        {
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
                        currentDto.Items = firstItem != null ? new List<EcfItemRequestDto> { firstItem } : new List<EcfItemRequestDto>();

                        currentDto.CustomerRnc = reference.Dto.CustomerRnc;
                        currentDto.CustomerName = reference.Dto.CustomerName;
                        currentDto.CustomerAddress = reference.Dto.CustomerAddress;

                        // Set reference fields
                        currentDto.ReferenceNcf = reference.Ncf;
                        currentDto.ReferenceIssueDate = reference.IssueDate;
                        currentDto.ReferenceReasonCode = 3; // [MODIFIED] 3 = Correction of amounts (Partial)
                        currentDto.ReferenceCustomerRnc = reference.CustomerRnc == currentDto.CustomerRnc ? null : reference.CustomerRnc;
                        currentDto.ReferenceReasonDescription = "Ajuste parcial de montos";
                        currentDto.IncomeType = currentDto.IncomeType ?? "01";

                        if (item.Type == 34)
                            currentDto.ManualIndicadorNotaCredito = 1;

                        // Cash payment for notes
                        currentDto.PaymentType = 1;
                        currentDto.PaymentDeadline = null;
                        currentDto.PaymentTerms = null;
                    }
                    else
                    {
                        // ── Step 3: Calculate current total from items (base for adjustments)
                        decimal itemsTotal = currentDto.Items.Sum(itm => (itm.Quantity * itm.UnitPrice) - (itm.ManualDescuentoMonto ?? itm.Discount));

                        // ── Step 4: Scale item prices proportionally if min/max constraints apply
                        if (item.MinAmount.HasValue && itemsTotal < item.MinAmount.Value)
                        {
                            decimal scaleFactor = item.MinAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                            foreach (var itm in currentDto.Items)
                                itm.UnitPrice = Math.Round(itm.UnitPrice * scaleFactor, 2);
                        }
                        else if (item.MaxAmount.HasValue && itemsTotal > item.MaxAmount.Value)
                        {
                            decimal scaleFactor = item.MaxAmount.Value / (itemsTotal > 0 ? itemsTotal : 1);
                            foreach (var itm in currentDto.Items)
                                itm.UnitPrice = Math.Round(itm.UnitPrice * scaleFactor, 2);
                        }

                        // ── Step 5: Add variety (small offset per step to avoid duplicate rejection)
                        if (currentDto.Items.Any())
                            currentDto.Items[0].UnitPrice += status.CurrentStep;

                        // ── Step 6: Type-specific header field adjustments only
                        switch (item.Type)
                        {
                            case 31: break;

                            case 32:
                                currentDto.PaymentType = 1;
                                currentDto.PaymentDeadline = null;
                                currentDto.PaymentTerms = null;
                                if (item.MinAmount.HasValue && item.MinAmount.Value >= 250000)
                                    currentDto.IncomeType = currentDto.IncomeType ?? "01";
                                break;

                            case 41:
                                // Type 41 (Compras): RNCComprador is REQUIRED (minOccurs=1)
                                // Must be 9 digits (company RNC) or 11 digits (cédula)
                                // The supplier/seller we're buying from — use a valid company RNC
                                currentDto.CustomerRnc = "131793916"; // Valid 9-digit test RNC
                                currentDto.CustomerName = "PROVEEDOR DE SERVICIOS SRL";
                                currentDto.IncomeType = null; // Not in type 41 XSD
                                currentDto.PaymentType = 1;
                                currentDto.PaymentDeadline = null;
                                currentDto.PaymentTerms = null;
                                break;

                            case 43:
                                foreach (var itm in currentDto.Items)
                                    itm.UnitPrice = Math.Round(500m / currentDto.Items.Count, 2) + i;
                                currentDto.IncomeType = null;
                                currentDto.PaymentType = 1;
                                currentDto.PaymentDeadline = null;
                                currentDto.PaymentTerms = null;
                                break;

                            case 44: break;
                            case 45: break;

                            case 46:
                                // Type 46 (Regímenes Especiales): TipoPago is REQUIRED (minOccurs=1)
                                // Keep payment type from JSON but ensure IncomeType is set
                                currentDto.IncomeType = currentDto.IncomeType ?? "01";
                                // Keep paymentType/deadline/terms from the JSON as-is
                                break;

                            case 47:
                                // Type 47 (Pagos al Exterior): Comprador is optional (minOccurs=0)
                                // No RNC, no Dominican fields — just foreign identifier
                                currentDto.CustomerRnc = null;
                                currentDto.CustomerName = "FOREIGN SERVICES PROVIDER";
                                currentDto.CustomerForeignId = $"FOREIGN{i + 1:D6}";
                                currentDto.CustomerCountry = null; // PaisComprador excluded for 47 in serializer
                                currentDto.CustomerAddress = null;
                                currentDto.IncomeType = null; // Not in type 47 XSD
                                currentDto.PaymentType = 1;
                                currentDto.PaymentDeadline = null;
                                currentDto.PaymentTerms = null;
                                break;
                        }
                    }

                    // D. Generate with temp NCF for XSD validation BEFORE consuming sequence
                    currentDto.Ncf = $"E{item.Type}0000000000"; // Temp NCF for validation only
                    string unsignedXmlTemp = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);

                    // XSD Validation BEFORE sequence management (to avoid burning NCFs)
                    var xsdErrors = _generatorService.ValidateXmlAgainstSchema(unsignedXmlTemp, item.Type);
                    if (xsdErrors.Any())
                    {
                        status.CompletedSteps.Add(new CertificationStepResultDto
                        {
                            Index = status.CurrentStep,
                            Ncf = currentDto.Ncf,
                            Status = "Error XSD",
                            Message = string.Join(" | ", xsdErrors.Take(3))
                        });
                        // DO NOT consume sequence - skip without incrementing
                        continue;
                    }

                    // B. Sequence Management (AFTER XSD validation passes)
                    var ecfTypeRecord = await _context.Set<Core.Entities.EcfType>().FirstOrDefaultAsync(t => t.Code == item.Type.ToString());
                    if (ecfTypeRecord == null) throw new Exception($"Tipo de e-CF {item.Type} no soportado en la base de datos.");

                    var encfRecord = await _context.ENcfs.FirstOrDefaultAsync(e => e.NcfTypeId == ecfTypeRecord.EcfTypeId && e.ClientId == client.ClientId);
                    if (encfRecord == null)
                    {
                        encfRecord = new ENcf { NcfTypeId = ecfTypeRecord.EcfTypeId, ClientId = client.ClientId, Sequence = 1, RegisteredAt = DateTime.Now };
                        _context.ENcfs.Add(encfRecord);
                        await _context.SaveChangesAsync();
                    }

                    int seq = encfRecord.Sequence++;
                    currentDto.Ncf = $"E{item.Type}{seq:D10}";
                    _context.Entry(encfRecord).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    // Re-generate with the real NCF
                    string unsignedXml = _generatorService.GenerateUnsignedXml(currentDto, item.IsSummary);

                    string signedXml = _signerService.SignXml(unsignedXml, certBase64, certPass);

                    bool isAccepted = false;
                    string? trackId = null;
                    string? error = null;
                    string? downloadUrl = null;

                    if (item.IsManual)
                    {
                        // Manual Upload Rule: Save to downloads folder
                        string simulationFolder = Path.Combine(webRootPath, "downloads", "simulation", client.Rnc);
                        if (!Directory.Exists(simulationFolder)) Directory.CreateDirectory(simulationFolder);
                        string fileName = $"{currentDto.Ncf}.xml";
                        string filePath = Path.Combine(simulationFolder, fileName);
                        await File.WriteAllTextAsync(filePath, signedXml);

                        downloadUrl = $"/downloads/simulation/{client.Rnc}/{fileName}";
                        isAccepted = true; // Manual documents are "accepted" once generated for upload
                    }
                    else
                    {
                        // [NEW] Console Logging [TX]
                        Console.WriteLine($"[TX] Enviando e-CF {currentDto.Ncf} tipo {item.Type} (Paso {status.CurrentStep}/{status.TotalSteps})...");

                        // Calculate the real total from items for correct routing
                        decimal actualTransmissionTotal = currentDto.Items.Sum(itm => (itm.Quantity * itm.UnitPrice) - itm.Discount + (itm.ManualRecargoMonto ?? 0));

                        // API Transmission
                        var result = await _transmissionService.SendEcfAsync(
                            DgiiEnvironment.CerteCF,
                            token,
                            signedXml,
                            item.Type,
                            actualTransmissionTotal,
                            dto.IssuerRnc,
                            currentDto.Ncf,
                            item.IsSummary);

                        if (result.Success && !string.IsNullOrEmpty(result.TrackId))
                        {
                            // [NEW] Stability wait of 2 seconds as requested before polling
                            await Task.Delay(2000);

                            // Poll DGII to get actual acceptance/rejection
                            var finalStatus = await PollDgiiStatusAsync(result.TrackId, dto.IssuerRnc);
                            isAccepted = finalStatus.Estado == "Aceptado" || (item.IsSummary && finalStatus.Estado == "Generado");
                            trackId = result.TrackId;

                            // [NEW] Console Logging [RX]
                            Console.WriteLine($"[RX] Resultado e-CF {currentDto.Ncf}: {finalStatus.Estado}");

                            if (!isAccepted)
                                error = $"DGII: {finalStatus.Estado} - {string.Join(" | ", finalStatus.Mensajes?.Select(m => m.Valor) ?? new[] { "Sin mensaje" })}";
                        }
                        else
                        {
                            isAccepted = false;
                            error = result.Error;
                            Console.WriteLine($"[RX] Error en envío e-CF {currentDto.Ncf}: {error}");
                        }
                    }

                    // Save ALL sent documents (accepted or rejected) to allow audit trail
                    var doc = new CertificationDocument
                    {
                        CertificationProcessId = process.CertificationProcessId,
                        ENcfSecuence = currentDto.Ncf,
                        ENcfId = encfRecord.ENcfId,
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
                            accepted31Pool.Add((currentDto.Ncf, currentDto.IssueDate, currentDto.CustomerRnc, cloned));
                        }
                    }

                    status.CompletedSteps.Add(new CertificationStepResultDto
                    {
                        Index = status.CurrentStep,
                        Ncf = currentDto.Ncf,
                        Status = isAccepted ? "Aceptado" : "Rechazado",
                        Message = isAccepted ? (item.IsManual ? $"Manual: {downloadUrl}" : $"TrackId: {trackId}") : error
                    });

                    // [NEW] Stop simulation on first error as requested (mirror EnqueueCertificationJobAsync behavior)
                    if (!isAccepted)
                    {
                        status.Status = "Failed";
                        status.ErrorMessage = $"Error en NCF {currentDto.Ncf}: {error}";
                        goto EndOfJob; // Use goto or another way to exit nested loops
                    }
                }
            }

            EndOfJob:
            if (status.Status != "Failed")
            {
                process.Status = CertificationStatus.Approved;
                process.EndDate = DateTime.Now;
                status.Status = "Completed";
                Console.WriteLine($"[INFO] Simulación finalizada exitosamente para {dto.IssuerRnc}.");
            }
            else
            {
                process.Status = CertificationStatus.Rejected;
                process.EndDate = DateTime.Now;
                Console.WriteLine($"[ERROR] Simulación detenida por error en {dto.IssuerRnc}.");
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            status.Status = "Failed";
            status.ErrorMessage = ex.Message;

            try
            {
                var client = await _clientService.GetByAsync(c => c.Rnc == dto.IssuerRnc);
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

    private EcfInvoiceRequestDto? CloneDto(EcfInvoiceRequestDto source)
    {
        if (source == null) return null;
        var json = System.Text.Json.JsonSerializer.Serialize(source);
        return System.Text.Json.JsonSerializer.Deserialize<EcfInvoiceRequestDto>(json);
    }

    private string GenerateRandomCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}