using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Certification;

public class CertificationExcelMappingService : ICertificationExcelMappingService
{
    private sealed record CertificationTypeRule(
        bool AllowDescuentoMonto = true,
        bool AllowTablaSubDescuento = true,
        bool AllowRecargoMonto = true,
        bool AllowTablaSubRecargo = true
    );

    private static readonly Dictionary<int, CertificationTypeRule> CertificationTypeMatrix = new()
    {
        // Tipo 41 (Compras): según rechazo DGII, no enviar descuento/subdescuento ni recargo/subrecargo.
        [43] = new CertificationTypeRule(
            AllowDescuentoMonto: false,
            AllowTablaSubDescuento: false,
            AllowRecargoMonto: false,
            AllowTablaSubRecargo: false
        ),
        [47] = new CertificationTypeRule(
            AllowDescuentoMonto: false,
            AllowTablaSubDescuento: false,
            AllowRecargoMonto: false,
            AllowTablaSubRecargo: false
        ),
    };

    public EcfInvoiceRequestDto MapRowToRequest(IDictionary<string, object> row, int step, DateTime? fallbackDate = null)
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
                        TerminoPago = GetStr(row, "TerminoPago"),
                        TipoCuentaPago = GetStr(row, "TipoCuentaPago"),
                        NumeroCuentaPago = GetStr(row, "NumeroCuentaPago"),
                        BancoPago = GetStr(row, "BancoPago"),
                        FechaDesde = GetStr(row, "FechaDesde"),
                        FechaHasta = GetStr(row, "FechaHasta")
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
                        ContactoEntrega = GetStr(row, "ContactoEntrega"),
                        DireccionEntrega = GetStr(row, "DireccionEntrega"),
                        FechaOrdenCompra = GetStr(row, "FechaOrdenCompra"),
                        NumeroOrdenCompra = GetStr(row, "NumeroOrdenCompra"),
                        CodigoInternoComprador = GetStr(row, "CodigoInternoComprador"),
                        ResponsablePago = GetStr(row, "ResponsablePago"),
                        InformacionAdicionalComprador = GetStr(row, "InformacionAdicionalComprador")
                    },
                    // Los totales se pasan tal cual están en el Excel (sin Math.Round): este
                    // flujo es un orquestador 100%. El formato exacto (sin forzar decimales) se
                    // aplica al serializar en Xml.Excel.EcfXmlTotales / RfceXmlExtra.
                    Totales = new EcfTotalesRequest
                    {
                        MontoGravadoTotal = GetDec(row, "MontoGravadoTotal"),
                        MontoGravadoI1 = GetDec(row, "MontoGravadoI1"),
                        MontoGravadoI2 = GetDec(row, "MontoGravadoI2"),
                        MontoGravadoI3 = GetDec(row, "MontoGravadoI3"),
                        MontoExento = GetDec(row, "MontoExento"),
                        ITBIS1 = GetNullableInt(row, "ITBIS1"),
                        ITBIS2 = GetNullableInt(row, "ITBIS2"),
                        ITBIS3 = GetNullableInt(row, "ITBIS3"),
                        TotalITBIS = GetDec(row, "TotalITBIS"),
                        TotalITBIS1 = GetDec(row, "TotalITBIS1"),
                        TotalITBIS2 = GetDec(row, "TotalITBIS2"),
                        TotalITBIS3 = GetDec(row, "TotalITBIS3"),
                        MontoTotal = GetDec(row, "MontoTotal") ?? 0,
                        MontoNoFacturable = GetDec(row, "MontoNoFacturable"),
                        MontoPeriodo = GetDec(row, "MontoPeriodo"),
                        ValorPagar = GetDec(row, "ValorPagar"),
                        TotalITBISRetenido = GetDec(row, "TotalITBISRetenido"),
                        TotalISRRetencion = GetDec(row, "TotalISRRetencion"),
                        MontoImpuestoAdicional = GetDecAny(row, "MontoImpuestoAdicional", "MontoImpuestosAdicionales", "Monto Impuesto Adicional"),
                        ImpuestosAdicionales = GetImpuestosAdicionales(row)
                    }
                },
                InformacionReferencia = (GetStr(row, "TipoeCF") == "33" || GetStr(row, "TipoeCF") == "34") ? new EcfInformacionReferenciaRequest
                {
                    NCFModificado = CleanNcf(GetStr(row, "NCFModificado")),
                    RNCOtroContribuyente = GetStr(row, "RNCOtroContribuyente"),
                    FechaNCFModificado = GetStr(row, "FechaNCFModificado"),
                    CodigoModificacion = GetStr(row, "CodigoModificacion"),
                    RazonModificacion = GetStr(row, "RazonModificacion")
                } : null
            }
        };

        for (int i = 1; i <= 50; i++)
        {
            var nombre = GetItemStr(row, "NombreItem", i);
            if (nombre == null) continue;

            var cantidadItem = GetItemDec(row, "CantidadItem", i);
            var precioUnitarioItem = GetItemDec(row, "PrecioUnitarioItem", i);
            var montoItem = GetItemDec(row, "MontoItem", i);
            if (!cantidadItem.HasValue && !precioUnitarioItem.HasValue && !montoItem.HasValue) continue;

            if (!cantidadItem.HasValue || !precioUnitarioItem.HasValue || !montoItem.HasValue)
            {
                throw new InvalidOperationException(
                    $"La línea {i} del Excel no tiene todos los campos requeridos del item. " +
                    $"CantidadItem={cantidadItem?.ToString() ?? "null"}, " +
                    $"PrecioUnitarioItem={precioUnitarioItem?.ToString() ?? "null"}, " +
                    $"MontoItem={montoItem?.ToString() ?? "null"}.");
            }

            var tablaSubDescuento = GetItemTablaSubDescuento(row, i);
            var tablaSubRecargo = GetItemTablaSubRecargo(row, i);

            var descuentoMonto = GetItemDec(row, "DescuentoMonto", i);
            if (!descuentoMonto.HasValue &&
                tablaSubDescuento?.SubDescuento?.Any(s => s.MontoSubDescuento.HasValue) == true)
            {
                descuentoMonto = tablaSubDescuento.SubDescuento
                    .Where(s => s.MontoSubDescuento.HasValue)
                    .Sum(s => s.MontoSubDescuento!.Value);
            }

            var recargoMonto = GetItemDec(row, "RecargoMonto", i);
            if (!recargoMonto.HasValue &&
                tablaSubRecargo?.SubRecargo?.Any(s => s.MontoSubRecargo.HasValue) == true)
            {
                recargoMonto = tablaSubRecargo.SubRecargo
                    .Where(s => s.MontoSubRecargo.HasValue)
                    .Sum(s => s.MontoSubRecargo!.Value);
            }

            var billingIndicator = GetItemStr(row, "IndicadorFacturacion", i);
            
            // Derive billing indicator if missing
            if (string.IsNullOrEmpty(billingIndicator))
            {
                // In certification Excel, we often have to infer this. 
                // Defaulting to 1 (18%) for items that have amounts, or using other logic.
                // In the old project, it was derived in the generator.
                billingIndicator = "1";
            }

            // Tipos de impuesto adicional (ISC) que aplican a la línea: TipoImpuesto[i][j] (máx 2 por XSD)
            var impuestoTipos = new List<string>();
            for (int j = 1; j <= 2; j++)
            {
                var tipo = GetItemNestedStr(row, "TipoImpuesto", i, j);
                if (!string.IsNullOrWhiteSpace(tipo)) impuestoTipos.Add(tipo);
            }

            // Tabla de subcantidades: Subcantidad[i][j] / CodigoSubcantidad[i][j] (máx 5 por XSD). Verbatim.
            var subcantidades = new List<EcfSubcantidadRequest>();
            for (int j = 1; j <= 5; j++)
            {
                var sub = GetItemNestedDec(row, "Subcantidad", i, j);
                var cod = GetItemNestedStr(row, "CodigoSubcantidad", i, j);
                if (!sub.HasValue && string.IsNullOrWhiteSpace(cod)) continue;
                subcantidades.Add(new EcfSubcantidadRequest { Subcantidad = sub, CodigoSubcantidad = cod });
            }

            var item = new EcfItemRequestDto
            {
                NumeroLinea = i.ToString(),
                IndicadorFacturacion = billingIndicator,
                NombreItem = nombre,
                DescripcionItem = GetItemStr(row, "DescripcionItem", i),
                IndicadorBienoServicio = GetItemStr(row, "IndicadorBienoServicio", i),
                CantidadItem = cantidadItem.Value,
                UnidadMedida = GetItemStr(row, "UnidadMedida", i),
                PrecioUnitarioItem = precioUnitarioItem.Value,
                PrecioUnitarioItemDecimals = GetItemDecScale(row, "PrecioUnitarioItem", i),
                DescuentoMonto = descuentoMonto,
                TablaSubDescuento = tablaSubDescuento,
                MontoItem = montoItem.Value,
                RecargoMonto = recargoMonto,
                TablaSubRecargo = tablaSubRecargo,
                MontoITBISRetenido = GetItemDec(row, "MontoITBISRetenido", i),
                MontoISRRetenido = GetItemDec(row, "MontoISRRetenido", i),
                FechaElaboracion = GetItemStr(row, "FechaElaboracion", i),
                FechaVencimientoItem = GetItemStr(row, "FechaVencimientoItem", i),
                // Campos de referencia (bienes regulados / alcohol) — tal cual el Excel
                CantidadReferencia = GetItemDec(row, "CantidadReferencia", i),
                UnidadReferencia = GetItemStr(row, "UnidadReferencia", i),
                GradosAlcohol = GetItemDec(row, "GradosAlcohol", i),
                PrecioUnitarioReferencia = GetItemDec(row, "PrecioUnitarioReferencia", i),
                ImpuestoAdicionalTipos = impuestoTipos.Count > 0 ? impuestoTipos : null,
                TablaSubcantidad = subcantidades.Count > 0 ? subcantidades : null
            };
            dto.ECF.DetallesItems.Item.Add(item);
        }



        return dto;
    }

    public AcecfRequestDto MapRowToAcecfRequest(IDictionary<string, object> row, DateTime? fallbackDate = null)
    {
        return new AcecfRequestDto
        {
            Version = GetStr(row, "Version") ?? "1.0",
            RNCEmisor = GetStr(row, "RNCEmisor") ?? "",
            ENcf = CleanNcf(GetStr(row, "ENCF") ?? GetStr(row, "eNCF") ?? "") ?? "",
            FechaEmision = GetStr(row, "FechaEmision") ?? fallbackDate?.ToString("dd-MM-yyyy") ?? DateTime.Now.ToString("dd-MM-yyyy"),
            MontoTotal = GetDec(row, "MontoTotal") ?? 0,
            RNCComprador = GetStr(row, "RNCComprador") ?? "",
            Estado = GetNullableInt(row, "Estado") ?? 0,
            DetalleMotivoRechazo = GetStr(row, "DetalleMotivoRechazo") ?? "",
            FechaHoraAprobacionComercial = GetStr(row, "FechaHoraAprobacionComercial") ?? fallbackDate?.ToString("dd-MM-yyyy HH:mm:ss") ?? DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")
        };
    }



    private static string? CleanNcf(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"[A-Za-z].*");
        return match.Success ? match.Value : raw;
    }

    private static object? GetVal(IDictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var val) && val != null) return val;
        return row.Keys.FirstOrDefault(k => NormalizeKey(k) == NormalizeKey(key)) is { } hit ? row[hit] : null;
    }

    private static string NormalizeKey(string key) =>
        key.Replace(" ", "").Replace(".", "").Replace("/", "").Replace("-", "").Replace("_", "").ToLowerInvariant()
           .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u");

    private static string? GetStr(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return null;
        var clean = valObj.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(clean)) return null;
        var lowered = clean.ToLowerInvariant();
        return lowered is "#e" or "#n/a" ? null : clean;
    }

    private static decimal? GetDec(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return null;
        if (valObj is decimal d) return d;
        if (valObj is double db) return (decimal)db;
        if (valObj is int i) return i;
        if (valObj is long l) return l;
        if (valObj is float f) return (decimal)f;
        var raw = valObj.ToString();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var val = raw.Trim();
        if (val == "#e" || val.Equals("#n/a", StringComparison.OrdinalIgnoreCase)) return null;

        // 1) Invariant (1234.56)
        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        // 2) es-DO/es-ES style (1234,56)
        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("es-DO"), out parsed))
            return parsed;
        if (decimal.TryParse(val, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("es-ES"), out parsed))
            return parsed;

        // 3) Normalize thousand/decimal separators
        var normalized = val.Replace(" ", "");
        if (normalized.Contains(',') && normalized.Contains('.'))
        {
            // assume last separator is decimal point
            if (normalized.LastIndexOf(',') > normalized.LastIndexOf('.'))
                normalized = normalized.Replace(".", "").Replace(',', '.');
            else
                normalized = normalized.Replace(",", "");
        }
        else if (normalized.Contains(','))
        {
            normalized = normalized.Replace(',', '.');
        }

        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private static int? GetNullableInt(IDictionary<string, object> row, string key)
    {
        var val = GetStr(row, key);
        if (val == null) return null;
        return int.TryParse(val, out var parsed) ? parsed : null;
    }

    private static decimal? GetDecAny(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = GetDec(row, key);
            if (value.HasValue)
                return value;
        }

        return null;
    }

    private static string? GetItemStr(IDictionary<string, object> row, string baseKey, int line)
    {
        return GetStr(row, $"{baseKey}[{line}]") ?? (line == 1 ? GetStr(row, baseKey) : null);
    }

    private static decimal? GetItemDec(IDictionary<string, object> row, string baseKey, int line)
    {
        return GetDec(row, $"{baseKey}[{line}]") ?? (line == 1 ? GetDec(row, baseKey) : null);
    }

    private static int? GetItemDecScale(IDictionary<string, object> row, string baseKey, int line)
    {
        return GetDecScale(row, $"{baseKey}[{line}]") ?? (line == 1 ? GetDecScale(row, baseKey) : null);
    }

    private static string? GetItemNestedStr(IDictionary<string, object> row, string baseKey, int line, int index)
    {
        return GetStr(row, $"{baseKey}[{line}][{index}]");
    }

    private static decimal? GetItemNestedDec(IDictionary<string, object> row, string baseKey, int line, int index)
    {
        return GetDec(row, $"{baseKey}[{line}][{index}]");
    }

    private static int? GetDecScale(IDictionary<string, object> row, string key)
    {
        var valObj = GetVal(row, key);
        if (valObj == null) return null;

        var raw = valObj.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var lowered = raw.ToLowerInvariant();
        if (lowered is "#e" or "#n/a") return null;

        var normalized = raw.Replace(" ", "");
        var lastDot = normalized.LastIndexOf('.');
        var lastComma = normalized.LastIndexOf(',');
        var sepIndex = Math.Max(lastDot, lastComma);

        if (sepIndex < 0 || sepIndex >= normalized.Length - 1)
            return 0;

        var decimals = normalized.Length - sepIndex - 1;
        if (decimals < 0) return null;
        if (decimals > 4) decimals = 4;
        return decimals;
    }

    // Desglose de impuestos adicionales (ISC) a nivel Totales: TipoImpuesto[k] + montos/tasa. Verbatim.
    private static List<EcfImpuestoAdicionalRequest>? GetImpuestosAdicionales(IDictionary<string, object> row)
    {
        var list = new List<EcfImpuestoAdicionalRequest>();
        for (int k = 1; k <= 20; k++)
        {
            var tipo = GetStr(row, $"TipoImpuesto[{k}]");
            if (string.IsNullOrWhiteSpace(tipo)) continue;

            list.Add(new EcfImpuestoAdicionalRequest
            {
                TipoImpuesto = tipo,
                TasaImpuestoAdicional = GetDec(row, $"TasaImpuestoAdicional[{k}]"),
                MontoImpuestoSelectivoConsumoEspecifico = GetDec(row, $"MontoImpuestoSelectivoConsumoEspecifico[{k}]"),
                MontoImpuestoSelectivoConsumoAdvalorem = GetDec(row, $"MontoImpuestoSelectivoConsumoAdvalorem[{k}]"),
                OtrosImpuestosAdicionales = GetDec(row, $"OtrosImpuestosAdicionales[{k}]")
            });
        }
        return list.Count > 0 ? list : null;
    }

    private static EcfTablaSubDescuentoRequest? GetItemTablaSubDescuento(IDictionary<string, object> row, int line)
    {
        var subDescuentos = new List<EcfSubDescuentoRequest>();
        for (int i = 1; i <= 5; i++)
        {
            var tipo = GetStr(row, $"TipoSubDescuento[{line}][{i}]");
            var porcentaje = GetDec(row, $"SubDescuentoPorcentaje[{line}][{i}]");
            var monto = GetDec(row, $"MontoSubDescuento[{line}][{i}]");

            if (string.IsNullOrWhiteSpace(tipo))
                continue;

            if (!porcentaje.HasValue && !monto.HasValue)
                continue;

            subDescuentos.Add(new EcfSubDescuentoRequest
            {
                TipoSubDescuento = tipo,
                SubDescuentoPorcentaje = porcentaje,
                MontoSubDescuento = monto
            });
        }

        if (subDescuentos.Count == 0)
            return null;

        return new EcfTablaSubDescuentoRequest { SubDescuento = subDescuentos };
    }

    private static EcfTablaSubRecargoRequest? GetItemTablaSubRecargo(IDictionary<string, object> row, int line)
    {
        var subRecargos = new List<EcfSubRecargoRequest>();
        for (int i = 1; i <= 5; i++)
        {
            var tipo = GetStr(row, $"TipoSubRecargo[{line}][{i}]");
            var porcentaje = GetDec(row, $"SubRecargoPorcentaje[{line}][{i}]");
            var monto = GetDec(row, $"MontoSubRecargo[{line}][{i}]") ?? GetDec(row, $"MontosubRecargo[{line}][{i}]");

            if (string.IsNullOrWhiteSpace(tipo))
                continue;

            if (!porcentaje.HasValue && !monto.HasValue)
                continue;

            subRecargos.Add(new EcfSubRecargoRequest
            {
                TipoSubRecargo = tipo,
                SubRecargoPorcentaje = porcentaje,
                MontoSubRecargo = monto
            });
        }

        if (subRecargos.Count == 0)
            return null;

        return new EcfTablaSubRecargoRequest { SubRecargo = subRecargos };
    }
}
