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

    // In-memory store for certification state
    private static List<CertificationTestDto>? _cachedTests;
    private static Dictionary<string, DateTime>? _typeExpirationDates;
    private static TimeSpan? _dateOffset;

    public CertificationService(
        IConfiguration configuration,
        IEcfGeneratorService generatorService,
        IXmlSignatureService signerService,
        IDgiiAuthService authService,
        IDgiiTransmissionService transmissionService,
        IClientService clientService,
        IApiKeyService apiKeyService,
        IClientCertificateService clientCertificateService,
        IEncryptedService encryptedService)
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
    }

    public async Task<List<CertificationTestDto>> GetTestsAsync()
    {
        if (_cachedTests != null) return _cachedTests;

        string excelPath = Path.Combine(AppContext.BaseDirectory, "133009889-16042026193727.xlsx");
        if (!File.Exists(excelPath))
        {
            excelPath = "c:\\Projects\\ZynstormECFPlatform\\133009889-16042026193727.xlsx";
        }

        if (!File.Exists(excelPath))
            throw new FileNotFoundException("El archivo de excel de certificación no fue encontrado.");

        var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        // ── 1. Identify all referenced NCFs first to handle dependencies ──
        var referencedNcfs = rows
            .Select(r => GetStr(r, "NCFModificado"))
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // ── 1.5 Calculate Global Date Offset if not already done ──────────
        // DISABLED: Raw Excel dates used as requested by USER
        _dateOffset = TimeSpan.Zero;

        // ── 2. Identify primary expiration dates by type ──────────────────
        _typeExpirationDates = new Dictionary<string, DateTime>();
        foreach (var r in rows)
        {
            var type = r.ContainsKey("TipoeCF") ? r["TipoeCF"]?.ToString() ?? "" : "";
            var date = ParseDgiiDate(GetStr(r, "FechaVencimientoSecuencia"));
            if (!string.IsNullOrEmpty(type) && date.HasValue && !_typeExpirationDates.ContainsKey(type))
            {
                _typeExpirationDates[type] = date.Value;
            }
        }

        var tests = new List<CertificationTestDto>();

        int index = 0;
        foreach (var row in rows)
        {
            var ecfTypeStr = row.ContainsKey("TipoeCF") ? row["TipoeCF"]?.ToString() ?? "" : "";
            if (string.IsNullOrEmpty(ecfTypeStr)) continue;

            var encf = GetStr(row, "ENCF") ?? "";
            var test = new CertificationTestDto
            {
                Index = index++,
                CaseNumber = row.ContainsKey("CasoPrueba") ? row["CasoPrueba"]?.ToString() ?? "" : "",
                EcfType = ecfTypeStr,
                ENcf = encf,
                TotalAmount = row.ContainsKey("MontoTotal") ? Convert.ToDecimal(row["MontoTotal"] ?? 0) : 0,
                Description = $"Caso: {row["CasoPrueba"]}"
            };

            test.Step = DetermineStep(test, referencedNcfs);
            
            if (test.Step > 0)
            {
                tests.Add(test);
                
                // Special: If it's Step 3 (Summary), we also need the SAME case as Step 4 (Individual)
                if (test.Step == 3)
                {
                    var step4Test = new CertificationTestDto
                    {
                        Index = index++,
                        CaseNumber = test.CaseNumber,
                        EcfType = test.EcfType,
                        ENcf = test.ENcf, 
                        TotalAmount = test.TotalAmount,
                        Description = $"Caso (Individual): {test.CaseNumber}",
                        Step = 4
                    };
                    tests.Add(step4Test);
                }
            }
        }

        _cachedTests = tests;
        return _cachedTests;
    }

    private int DetermineStep(CertificationTestDto test, HashSet<string> referencedNcfs)
    {
        if (!int.TryParse(test.EcfType, out int type)) return 0;
        
        // Promotion: If this document is referenced by another, it MUST be Step 1 (Individual)
        // so the DGII system has it recorded before the reference document is sent.
        if (referencedNcfs.Contains(test.ENcf?.Trim()))
            return 1;

        // Group 1: 31, 41, 43, 44, 45, 46, 47, and 32 with amount >= 250,000
        if (type == 31 || type == 41 || (type >= 43 && type <= 47) || (type == 32 && test.TotalAmount >= 250000))
            return 1;

        // Group 2: 33, 34 (References)
        if (type == 33 || type == 34)
            return 2;

        // Group 3: 32 with amount < 250,000 (Summary)
        if (type == 32 && test.TotalAmount < 250000)
            return 3;

        return 0;
    }

    public async Task<DgiiTransmissionResult> RunTestAsync(int index)
    {
        var allTests = await GetTestsAsync();
        var test = allTests.FirstOrDefault(t => t.Index == index) 
            ?? throw new ArgumentException("Test no encontrado.");

        test.Status = TestStatus.Running;
        
        try
        {
            string excelPath = Path.Combine(AppContext.BaseDirectory, "133009889-16042026193727.xlsx");
            if (!File.Exists(excelPath))
            {
                excelPath = "c:\\Projects\\ZynstormECFPlatform\\133009889-16042026193727.xlsx";
            }
            var rows = MiniExcel.Query(excelPath, useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();
            // Use specific row for Type 31 to ensure matching reference for subsequent tests
            var row = (test.EcfType == "31")
                ? rows.First(r => r["ENCF"]?.ToString() == "E310000000034" || r["ENCF"]?.ToString() == "133009889E310000000034")
                : rows.First(r => (r["CasoPrueba"]?.ToString() ?? "") == test.CaseNumber);
            // Synchronize the test ENCF so the filename matches the XML content
            test.ENcf = row["ENCF"]?.ToString();

            var requestDto = MapRowToRequest(row, test.Step);

            // ── 1. Resolve issuer RNC from DTO or config ───────────────────────
            string issuerRnc = requestDto.IssuerRnc;
            if (string.IsNullOrEmpty(issuerRnc))
                issuerRnc = _configuration["Certification:IssuerRnc"] ?? "133009889";

            // ── 2. Load certificate from database by issuer RNC ────────────────
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
            var passwordBytes   = _encryptedService.DecryptWithSecret(certificate.Password,    decryptedSecretKey);

            if (certificateBytes.Length == 0 || passwordBytes.Length == 0)
                throw new InvalidOperationException("No se pudo desencriptar el certificado del cliente.");

            var certBase64 = Convert.ToBase64String(certificateBytes);
            var certPass   = Encoding.UTF8.GetString(passwordBytes);

            // ── 3. Generate & sign XML ─────────────────────────────────────────
            string unsignedXml = _generatorService.GenerateUnsignedXml(requestDto);
            string signedXml   = _signerService.SignXml(unsignedXml, certBase64, certPass);
            
            // ── DEBUG: Save the exact XML being sent for inspection ───────────
            try { File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "temp_last_request.xml"), signedXml); } catch { }

            // ── 4. Verify XML Schema (XSD) BEFORE transmitting ────────────────
            var validationErrors = _generatorService.ValidateXmlAgainstSchema(signedXml, int.Parse(test.EcfType));
            if (validationErrors.Any(e => e.StartsWith("[ERROR]")))
            {
                var errorMsg = "Error de esquema (XSD): " + string.Join(" | ", validationErrors);
                test.Status = TestStatus.Failed;
                test.Error = errorMsg;
                return new DgiiTransmissionResult { Error = errorMsg };
            }

            // ── 5. Authenticate with DGII ──────────────────────────────────────
            string token = await _authService.GetTokenAsync(issuerRnc, DgiiEnvironment.CerteCF, certBase64, certPass);

            // ── 6. Transmit ────────────────────────────────────────────────────
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

                // ── 7. Enqueue background status tracking job ──────────────────
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

    // ── Helper: read a string from the row, returning null if empty or "#e" ──────
    private static string? GetStr(IDictionary<string, object> row, string key)
    {
        if (!row.ContainsKey(key)) return null;
        var val = row[key]?.ToString();
        return string.IsNullOrWhiteSpace(val) || val == "#e" ? null : val.Trim();
    }

    private static decimal? GetDec(IDictionary<string, object> row, string key)
    {
        var val = GetStr(row, key);
        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal d))
            return d;
        return null;
    }

    // ── Helper: parse DD-MM-YYYY dates (DGII format) ──────────────────────────
    // IMPORTANT: Excel dates are already in Dominican Republic local time.
    // Use DateTimeKind.Unspecified to avoid unwanted conversions in EcfGenerator.
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

    private static DateTime ApplyDateOffset(DateTime date)
    {
        return _dateOffset.HasValue ? date.Add(_dateOffset.Value) : date;
    }

    private static DateTime? ApplyDateOffset(DateTime? date)
    {
        if (!date.HasValue) return null;
        return _dateOffset.HasValue ? date.Value.Add(_dateOffset.Value) : date;
    }

    private EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int currentStep)
    {
        var dto = new EcfInvoiceRequestDto
        {
            // ── Identification ─────────────────────────────────────────────
            Ncf = GetStr(row, "ENCF") ?? "",

            // ── Issuer ─────────────────────────────────────────────────────
            IssuerRnc            = GetStr(row, "RNCEmisor")          ?? "",
            IssuerName           = GetStr(row, "RazonSocialEmisor")  ?? "",
            IssuerAddress        = GetStr(row, "DireccionEmisor")     ?? "",
            IssuerCommercialName = GetStr(row, "NombreComercial"),
            IssuerBranchCode     = GetStr(row, "Sucursal"),          // null if "#e" or empty
            IssuerActivityCode   = GetStr(row, "ActividadEconomica"),// null if "#e" or empty
            IssuerMunicipality   = GetStr(row, "Municipio"),
            IssuerProvince       = GetStr(row, "Provincia"),
            IssuerEmail          = GetStr(row, "CorreoEmisor"),
            IssuerWebSite        = GetStr(row, "WebSite"),
            IssuerSellerCode     = GetStr(row, "CodigoVendedor"),

            // ── Buyer ──────────────────────────────────────────────────────
            CustomerRnc          = GetStr(row, "RNCComprador")           ?? "",
            CustomerForeignId    = GetStr(row, "IdentificadorExtranjero"),
            CustomerName         = GetStr(row, "RazonSocialComprador")   ?? "",
            CustomerCountry      = GetStr(row, "PaisComprador"),
            CustomerAddress      = GetStr(row, "DireccionComprador"),
            CustomerContact      = GetStr(row, "ContactoComprador"),

            CustomerEmail        = GetStr(row, "CorreoComprador"),
            CustomerMunicipality = GetStr(row, "MunicipioComprador"),
            CustomerProvince     = GetStr(row, "ProvinciaComprador"),
            BuyerInternalCode    = GetStr(row, "CodigoInternoComprador"),

            // ── Commercial fields ──────────────────────────────────────────
            InternalInvoiceNumber = GetStr(row, "NumeroFacturaInterna"),
            InternalOrderNumber   = GetStr(row, "NumeroPedidoInterno"),
            SalesZone             = GetStr(row, "ZonaVenta"),
            OrderNumber           = GetStr(row, "NumeroOrdenCompra"),

            // ── Payment ────────────────────────────────────────────────────
            PaymentType = int.TryParse(GetStr(row, "TipoPago"), out int p) ? p : null,
            PaymentDeadline = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaLimitePago"))),
            IncomeType  = GetStr(row, "TipoIngresos"),


            // ── Manual Overrides for Certification (Raw Excel Data) ────────
            ManualMontoGravadoTotal = GetDec(row, "MontoGravadoTotal"),
            ManualMontoExento       = GetDec(row, "MontoExento"),
            ManualMontoTotal        = GetDec(row, "MontoTotal"),
            ManualTotalITBIS        = GetDec(row, "TotalITBIS"),
            ManualTotalITBIS1       = GetDec(row, "TotalITBIS1"),
            ManualTotalITBIS2       = GetDec(row, "TotalITBIS2"),
            ManualTotalITBIS3       = GetDec(row, "TotalITBIS3"),
            ManualMontoPeriodo      = GetDec(row, "MontoPeriodo"),
            ManualValorPagar        = GetDec(row, "ValorPagar"),
            ManualIndicadorMontoGravado = int.TryParse(GetStr(row, "IndicadorMontoGravado"), out int img) ? img : null,
            ManualTotalITBISRetenido = GetDec(row, "TotalITBISRetenido"),
            ManualTotalISRRetencion  = GetDec(row, "TotalISRRetencion"),
            ManualMontoGravadoI1     = GetDec(row, "MontoGravadoI1"),
            ManualIndicadorNotaCredito = int.TryParse(GetStr(row, "IndicadorNotaCredito"), out int inc) ? inc : null,
            ManualMontoNoFacturable = GetDec(row, "MontoNoFacturable"),

            ManualMontoGravadoI2     = GetDec(row, "MontoGravadoI2"),
            ManualMontoGravadoI3     = GetDec(row, "MontoGravadoI3"),

            // ── Reference (NC/ND types 33/34) ──────────────────────────────
            ReferenceNcf         = GetStr(row, "NCFModificado"),
            ReferenceCustomerRnc = GetStr(row, "RNCOtroContribuyente"),
            ReferenceReasonCode  = int.TryParse(GetStr(row, "CodigoModificacion"), out int rc) ? rc : null,
            ReferenceReasonDescription = GetStr(row, "RazonModificacion"),


            Items = new List<EcfItemRequestDto>()
        };

        // ── Dates ──────────────────────────────────────────────────────────
        dto.IssueDate = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaEmision")) ?? DateTime.Now);

        var expDate = ParseDgiiDate(GetStr(row, "FechaVencimientoSecuencia"));
        string typeStr = GetStr(row, "TipoeCF") ?? "";
        if (!expDate.HasValue && _typeExpirationDates != null && _typeExpirationDates.TryGetValue(typeStr, out DateTime fallback))
        {
            expDate = fallback;
        }
        dto.SequenceExpirationDate = ApplyDateOffset(expDate);

        dto.DeliveryDate           = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaEntrega")));
        dto.OrderDate              = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaOrdenCompra")));
        dto.ReferenceIssueDate     = ApplyDateOffset(ParseDgiiDate(GetStr(row, "FechaNCFModificado")));

        // ── MontoNoFacturable (type 33 exento) ────────────────────────────
        var montoNoFacturableStr = GetStr(row, "MontoNoFacturable");
        if (decimal.TryParse(montoNoFacturableStr,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal mnf) && mnf > 0)
            dto.MontoNoFacturable = mnf;

        // ── Items ──────────────────────────────────────────────────────────
        // IndicadorFacturacion from Excel:
        //   1 = Gravado ITBIS 18%
        //   2 = Gravado ITBIS 16%
        //   3 = Gravado ITBIS 0%
        //   4 = Exento
        //   0 = No facturable (informativo)
        for (int i = 1; i <= 50; i++)
        {
            var nombreKey = $"NombreItem[{i}]";
            var nombre = GetStr(row, nombreKey);
            if (nombre == null) continue;

            var indicadorStr  = GetStr(row, $"IndicadorFacturacion[{i}]");
            var indicadorFact = int.TryParse(indicadorStr, out int indF) ? indF : 1;

            // Map IndicadorFacturacion → TaxPercentage
            decimal taxPct = indicadorFact switch
            {
                1 => 18m,
                2 => 16m,
                3 =>  0m,
                4 =>  0m, // exento — no ITBIS
                0 =>  0m, // no facturable — no ITBIS
                _ => 18m
            };

            var cantStr  = GetStr(row, $"CantidadItem[{i}]");
            var precioStr = GetStr(row, $"PrecioUnitarioItem[{i}]");
            var bienServStr = GetStr(row, $"IndicadorBienoServicio[{i}]");
            var unidadStr   = GetStr(row, $"UnidadMedida[{i}]");

            decimal cantidad = decimal.TryParse(cantStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal c) ? c : 1;
            decimal precio = decimal.TryParse(precioStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal pr) ? pr : 0;
            int itemType  = int.TryParse(bienServStr, out int bs) ? bs : 1;
            int? unitOfMeasure = int.TryParse(unidadStr, out int um) ? um : null;

            var item = new EcfItemRequestDto
            {
                Name             = nombre,
                Description      = GetStr(row, $"DescripcionItem[{i}]"),
                Quantity         = cantidad,
                UnitPrice        = precio,
                ItemType         = itemType,
                UnitOfMeasure    = unitOfMeasure,
                TaxPercentage    = taxPct,
                BillingIndicator = indicadorFact,  // Pass exact Excel indicator (4=exento, 0=no facturable)
                ManualMontoItem  = GetDec(row, $"MontoItem[{i}]"),
                ManualDescuentoMonto = GetDec(row, $"DescuentoMonto[{i}]"),
                ManualRecargoMonto   = GetDec(row, $"RecargoMonto[{i}]"),
                ManualMontoITBISRetenido = GetDec(row, $"MontoITBISRetenido[{i}]"),
                ManualMontoISRRetenido = GetDec(row, $"MontoISRRetenido[{i}]")

            };

            // Extract SubRecargos (up to 5 per item) from Excel
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
        }

        return dto;
    }

    public async Task<CertificationSummaryDto> GetSummaryAsync()
    {
        var tests = await GetTestsAsync();
        return new CertificationSummaryDto
        {
            TotalTests = tests.Count,
            PassedTests = tests.Count(t => t.Status == TestStatus.Passed),
            FailedTests = tests.Count(t => t.Status == TestStatus.Failed),
            Tests = tests
        };
    }
}
