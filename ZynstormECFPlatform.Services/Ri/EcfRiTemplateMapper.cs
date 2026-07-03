using System.Globalization;
using ZynstormECFPlatform.Core.Enums;

namespace ZynstormECFPlatform.Services.Ri;

/// <summary>
/// Maps a signed e-CF XML document into the <see cref="RiInvoiceModel"/> consumed by
/// <see cref="RiInvoicePdf"/> (the QuestPDF template ported from EasyInvoice's
/// InvoicePdf). Reuses <see cref="EcfRiDataMapper"/> for the emisor/comprador/items/
/// totals/security-code extraction and QR URL construction, so both renderers stay
/// consistent about how those values are derived from the XML.
/// </summary>
public static class EcfRiTemplateMapper
{
    public static RiInvoiceModel MapInvoice(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);
        var ecfType = int.TryParse(data.TipoeCF, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

        var received = data.Totals.Total == decimal.Truncate(data.Totals.Total)
            ? data.Totals.Total
            : Math.Ceiling(data.Totals.Total);

        return new RiInvoiceModel
        {
            Company = new RiInvoiceCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            Client = new RiInvoiceClient
            {
                Name = data.Buyer.Name,
                Rnc = data.Buyer.Document,
                Address = string.IsNullOrWhiteSpace(data.Buyer.Address) ? null : data.Buyer.Address
            },
            NcfNumber = data.ENcf,
            NcfTypeName = NcfTypeName(ecfType),
            EcfType = ecfType,
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            ValidUntil = FormatDate(data.FechaVencimientoSecuencia),
            InternalInvoiceNumber = data.NumeroFacturaInterna,
            PaymentType = PaymentTypeLabel(data.TipoPago),
            PaymentCondition = PaymentCondition(data),
            IsCredit = data.TipoPago == 2,
            Cashier = CertificationCashier,
            ReceivedAmount = received,
            ChangeAmount = received - data.Totals.Total,
            AffectedNcf = data.NcfModificado,
            ModificationCode = ModificationCodeLabel(data.CodigoModificacion),
            ModificationReason = data.RazonModificacion,
            Items = data.Items.ConvertAll(item => new RiInvoiceItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                Price = item.Price,
                Itbis = item.Itbis,
                Amount = item.Amount,
                Unit = UnitAbbreviation(item.UnidadMedida),
                Discount = item.Discount
            }),
            SubTotal = data.Totals.SubTotal,
            Discount = data.Items.Sum(item => item.Discount),
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>
    /// Maps a signed e-CF type 41 (Comprobante de Compras) XML into the
    /// <see cref="RiPurchaseModel"/> consumed by <see cref="RiPurchasePdf"/>. En el 41 el
    /// EMISOR del XML es la empresa que registra la compra (ella emite el comprobante) y el
    /// nodo COMPRADOR contiene al suplidor informal al que se le compró
    /// (dgii_ecf_requirements.md §41): Company ← Emisor, Supplier ← Comprador.
    /// </summary>
    public static RiPurchaseModel MapPurchase(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        return new RiPurchaseModel
        {
            Company = new RiPurchaseCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            Supplier = new RiPurchaseSupplier
            {
                Name = data.Buyer.Name,
                Rnc = data.Buyer.Document,
                Address = string.IsNullOrWhiteSpace(data.Buyer.Address) ? null : data.Buyer.Address
            },
            NcfNumber = data.ENcf,
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            Items = data.Items.ConvertAll(item =>
            {
                var rate = ItbisRateFor(item.IndicadorFacturacion, data.Totals);
                return new RiPurchaseItem
                {
                    Description = item.Description,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    ItbisRate = rate,
                    Itbis = item.Itbis > 0 ? item.Itbis : Math.Round(item.Amount * rate / 100m, 2),
                    Amount = item.Amount
                };
            }),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            IsrRetentionAmount = data.Totals.IsrRetencion,
            IsrRetentionRate = data.Totals.SubTotal > 0
                ? data.Totals.IsrRetencion / data.Totals.SubTotal * 100m
                : 0m,
            ItbisRetentionAmount = data.Totals.ItbisRetenido,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>
    /// Maps a signed e-CF type 43 (Gastos Menores) XML into the <see cref="RiExpenseModel"/>
    /// consumed by <see cref="RiExpensePdf"/> (plantilla portada del ExpensePdf informal de
    /// EasyInvoice). El 43 no lleva comprador: solo emisor, totales e ítems.
    /// </summary>
    public static RiExpenseModel MapExpense(string signedXml, DgiiEnvironment environment)
    {
        var data = EcfRiDataMapper.Map(signedXml, environment);

        var itbisLabel = data.Totals.Itbis <= 0
            ? "EXENTO:"
            : (data.Totals.Itbis1Rate == 16m ? "ITBIS 16%:" : "ITBIS 18%:");

        return new RiExpenseModel
        {
            Company = new RiInvoiceCompany
            {
                Name = data.Issuer.Name,
                Rnc = data.Issuer.Document,
                Address = data.Issuer.Address,
                Phone = data.Issuer.Phone
            },
            NcfNumber = data.ENcf,
            ValidUntil = FormatDate(data.FechaVencimientoSecuencia),
            PaymentMethod = PaymentTypeLabel(data.TipoPago),
            FechaEmision = data.FechaEmision,
            FechaFirma = data.FechaFirma,
            UserName = CertificationCashier,
            Concept = string.Join("; ", data.Items
                .Select(item => item.Description)
                .Where(description => !string.IsNullOrWhiteSpace(description))),
            SubTotal = data.Totals.SubTotal,
            Itbis = data.Totals.Itbis,
            Total = data.Totals.Total,
            ItbisLabel = itbisLabel,
            Qr = data.QrUrl,
            SecurityCode = data.SecurityCode
        };
    }

    /// <summary>IndicadorFacturacion del ítem → tasa ITBIS de Totales (1→ITBIS1, 2→ITBIS2, 3→ITBIS3; 4/0→exento).</summary>
    private static decimal ItbisRateFor(int indicadorFacturacion, RiTotals totals) => indicadorFacturacion switch
    {
        1 => totals.Itbis1Rate,
        2 => totals.Itbis2Rate,
        3 => totals.Itbis3Rate,
        _ => 0m
    };

    /// <summary>DGII e-CF type code to display title, mirroring EasyInvoice's InvoicePdf label logic.</summary>
    private static string NcfTypeName(int ecfType) => ecfType switch
    {
        31 => "FACTURA DE CRÉDITO FISCAL ELECTRÓNICA",
        32 => "FACTURA DE CONSUMO ELECTRÓNICA",
        // DGII: 33 = Nota de Débito, 34 = Nota de Crédito (igual que E33/E34 en EasyInvoice).
        33 => "NOTA DE DÉBITO ELECTRÓNICA",
        34 => "NOTA DE CRÉDITO ELECTRÓNICA",
        41 => "COMPRAS ELECTRÓNICO",
        43 => "GASTO MENOR ELECTRÓNICO",
        44 => "REGÍMENES ESPECIALES ELECTRÓNICO",
        45 => "GUBERNAMENTAL ELECTRÓNICO",
        46 => "EXPORTACIONES ELECTRÓNICO",
        47 => "PAGOS AL EXTERIOR ELECTRÓNICO",
        _ => "COMPROBANTE FISCAL ELECTRÓNICO"
    };

    /// <summary>Nombre fijo usado en las RI de certificación (decisión de producto).</summary>
    internal const string CertificationCashier = "PEDRO";

    internal static string PaymentTypeLabel(int tipoPago) => tipoPago switch
    {
        1 => "CONTADO",
        2 => "CRÉDITO",
        _ => string.Empty
    };

    /// <summary>"dd-MM-yyyy" del XML -> "dd/MM/yyyy" para mostrar; vacío se preserva.</summary>
    internal static string FormatDate(string xmlDate) => xmlDate.Replace('-', '/');

    private static string PaymentCondition(RiData data)
    {
        if (!string.IsNullOrEmpty(data.TerminoPago))
        {
            return data.TerminoPago;
        }

        if (data.TipoPago == 1)
        {
            return "CONTADO";
        }

        if (data.TipoPago == 2)
        {
            var days = DaysBetween(data.FechaEmision, data.FechaLimitePago);
            if (days <= 0)
            {
                days = 30;
            }
            return $"{days} DÍAS";
        }

        return string.Empty;
    }

    private static int DaysBetween(string fromDdMmYyyy, string toDdMmYyyy) =>
        DateTime.TryParseExact(fromDdMmYyyy, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var from)
        && DateTime.TryParseExact(toDdMmYyyy, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var to)
            ? (int)(to.Date - from.Date).TotalDays
            : 0;

    /// <summary>Códigos DGII de unidad de medida (enum UnitOfMeasure) -> abreviatura corta del recibo.</summary>
    internal static string UnitAbbreviation(int code) => code switch
    {
        0 => string.Empty,
        2 => "Bolsa",
        5 => "Bot",
        6 => "Caja",
        12 => "Día",
        13 => "Doc",
        15 => "Gal",
        17 => "g",
        19 => "Hora",
        21 => "Kg",
        23 => "Lb",
        24 => "L",
        26 => "m",
        27 => "m²",
        28 => "m³",
        30 => "Min",
        31 => "Paq",
        32 => "Par",
        34 => "Pza",
        39 => "Ton",
        45 => "Millar",
        46 => "Saco",
        47 => "Lata",
        59 => "ml",
        60 => "mg",
        61 => "Oz",
        _ => "Und"
    };

    /// <summary>Catálogo DGII de códigos de modificación (InformacionReferencia).</summary>
    private static string ModificationCodeLabel(string code) => code switch
    {
        "1" => "1 - Anula el NCF modificado",
        "2" => "2 - Corrige texto del NCF modificado",
        "3" => "3 - Corrige montos del NCF modificado",
        "4" => "4 - Reemplazo NCF emitido en contingencia",
        "5" => "5 - Referencia Factura de Consumo Electrónica",
        "" => string.Empty,
        _ => code
    };
}
