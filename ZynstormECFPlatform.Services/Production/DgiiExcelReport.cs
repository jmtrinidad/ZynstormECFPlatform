using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;
using ZynstormECFPlatform.Dtos;

namespace ZynstormECFPlatform.Services.Production;

public sealed record DgiiExcelReportCandidate(
    int EcfDocumentId,
    int ClientId,
    string ENcf,
    EcfLookupState State,
    DateTime RegisteredAt);

public sealed record DgiiExcelReportSource(
    int EcfDocumentId,
    int ClientId,
    string ENcf,
    string? CustomerRnc,
    string? ModifiedNcf,
    DateTime IssueDateUtc,
    DateTime RegisteredAt,
    DateTime? SentAtUtc,
    string? ResponsePayload,
    decimal SubTotal,
    decimal ItbisTotal,
    EcfLookupState State,
    string? Xml);

public sealed record DgiiExcelReportRow(
    string? RncReceptor,
    string ENcf,
    string? ENcfModificado,
    string FechaComprobante,
    string FechaRecepcion,
    string AprobacionComercial,
    string? FechaAprobacionComercial,
    string Estado,
    decimal? ItbisFacturado,
    decimal? MontoTotalGravado,
    decimal? MontoExento,
    decimal? MontoNoFacturable);

public static class DgiiExcelReportLogic
{
    public const int MaxItems = 5000;
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public static readonly string[] Headers =
    [
        "RNC Receptor",
        "ENCF",
        "ENCF Modificado",
        "Fecha Comprobante",
        "Fecha Recepción",
        "Aprobación Comercial",
        "Fecha Aprobación Comercial",
        "Estado",
        "ITBIS Facturado",
        "Monto Total Gravado",
        "Monto Exento",
        "Monto No Facturable"
    ];

    private static readonly string[] IssueDateFormats =
    [
        "dd-MM-yyyy",
        "dd/MM/yyyy",
        "yyyy-MM-dd",
        "d-M-yyyy",
        "d/M/yyyy"
    ];

    private static readonly string[] ReceptionDateFormats =
    [
        "dd/MM/yyyy hh:mm:ss tt",
        "dd/MM/yyyy h:mm:ss tt",
        "d/M/yyyy hh:mm:ss tt",
        "d/M/yyyy h:mm:ss tt",
        "dd-MM-yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "dd-MM-yyyy hh:mm:ss tt"
    ];

    /// <summary>
    /// Filtra únicamente comprobantes aceptados (incluyendo Aceptado Condicional),
    /// eligiendo el mejor intento por (ClientId, eNCF).
    /// </summary>
    public static IReadOnlyList<DgiiExcelReportCandidate> SelectAccepted(IEnumerable<DgiiExcelReportCandidate> candidates) =>
        candidates
            .Where(c => c.State is EcfLookupState.Accepted or EcfLookupState.AcceptedConditional)
            .GroupBy(c => (c.ClientId, c.ENcf))
            .Select(group =>
            {
                var best = EcfLookupLogic.PickBest(
                    group.Select(c => new EcfLookupCandidate(c.EcfDocumentId, c.State, c.RegisteredAt)))!;

                return group.First(c => c.EcfDocumentId == best.EcfDocumentId);
            })
            .OrderBy(c => c.RegisteredAt)
            .ThenBy(c => c.ENcf, StringComparer.Ordinal)
            .ToList();

    public static EcfLookupState ResolveReportState(int ecfStatusId, string? lastResponsePayload, string? lastResponseMessage, string? lastResponseCode)
    {
        var resolved = EcfLookupLogic.ResolveState(ecfStatusId, lastResponsePayload);
        if (resolved != EcfLookupState.Rejected || ecfStatusId != 11)
        {
            return resolved;
        }

        if (string.Equals(lastResponseCode?.Trim(), "4", StringComparison.OrdinalIgnoreCase))
        {
            return EcfLookupState.AcceptedConditional;
        }

        if (!string.IsNullOrWhiteSpace(lastResponseMessage)
            && (lastResponseMessage.Contains("condicional", StringComparison.OrdinalIgnoreCase)
                || lastResponseMessage.Contains("conditional", StringComparison.OrdinalIgnoreCase)))
        {
            return EcfLookupState.AcceptedConditional;
        }

        return EcfLookupState.Rejected;
    }

    public static DgiiExcelReportRow BuildRow(DgiiExcelReportSource source)
    {
        XDocument? doc = null;
        if (!string.IsNullOrWhiteSpace(source.Xml))
        {
            try
            {
                doc = XDocument.Parse(source.Xml);
            }
            catch (XmlException)
            {
                doc = null;
            }
        }

        var idDoc = FirstElement(doc, "IdDoc");
        var emisor = FirstElement(doc, "Emisor");
        var comprador = FirstElement(doc, "Comprador");
        var totales = FirstElement(doc, "Totales");
        var infoRef = FirstElement(doc, "InformacionReferencia");

        var rncReceptor = FirstNonEmptyOrNull(
            ElementValue(comprador, "RNCComprador"),
            source.CustomerRnc);

        var encf = FirstNonEmptyOrNull(
            ElementValue(idDoc, "eNCF"),
            source.ENcf) ?? source.ENcf;

        var encfModificado = FirstNonEmptyOrNull(
            ElementValue(infoRef, "NCFModificado"),
            source.ModifiedNcf);

        var issueDate = TryParseIssueDate(ElementValue(emisor, "FechaEmision")) ?? source.IssueDateUtc.Date;
        var fechaComprobante = FormatDgiiDateTime(issueDate.Date);

        var receptionDate = TryExtractDgiiReceptionDate(source.ResponsePayload)
            ?? (source.SentAtUtc ?? source.RegisteredAt).ToDrTime();
        var fechaRecepcion = FormatDgiiDateTime(receptionDate);

        var estado = source.State == EcfLookupState.AcceptedConditional
            ? "Aceptado Condicional"
            : "Aceptado";

        decimal? itbisFacturado;
        decimal? montoTotalGravado;
        decimal? montoExento;
        decimal? montoNoFacturable;

        if (totales is not null)
        {
            itbisFacturado = DecimalElementOrNull(totales, "TotalITBIS");
            montoTotalGravado = DecimalElementOrNull(totales, "MontoGravadoTotal");
            montoExento = DecimalElementOrNull(totales, "MontoExento");
            montoNoFacturable = DecimalElementOrNull(totales, "MontoNoFacturable");
        }
        else
        {
            itbisFacturado = source.ItbisTotal > 0m ? source.ItbisTotal : null;
            montoTotalGravado = source.ItbisTotal > 0m ? source.SubTotal : null;
            montoExento = source.ItbisTotal == 0m && source.SubTotal > 0m ? source.SubTotal : null;
            montoNoFacturable = null;
        }

        return new DgiiExcelReportRow(
            RncReceptor: rncReceptor,
            ENcf: encf,
            ENcfModificado: encfModificado,
            FechaComprobante: fechaComprobante,
            FechaRecepcion: fechaRecepcion,
            AprobacionComercial: string.Empty,
            FechaAprobacionComercial: null,
            Estado: estado,
            ItbisFacturado: itbisFacturado,
            MontoTotalGravado: montoTotalGravado,
            MontoExento: montoExento,
            MontoNoFacturable: montoNoFacturable);
    }

    public static string FormatDgiiDateTime(DateTime dateTime)
    {
        var period = dateTime.Hour < 12 ? "A.M." : "P.M.";
        return $"{dateTime.ToString("dd/MM/yyyy hh:mm:ss", CultureInfo.InvariantCulture)} {period}";
    }

    public static DateTime? TryParseIssueDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (DateTime.TryParseExact(
                raw.Trim(),
                IssueDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    public static DateTime? TryExtractDgiiReceptionDate(string? responsePayload)
    {
        if (string.IsNullOrWhiteSpace(responsePayload))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responsePayload);
            var root = doc.RootElement;

            if (TryGetFechaRecepcion(root, "status", out var raw)
                || TryGetFechaRecepcion(root, "statusResponse", out raw)
                || TryGetFechaRecepcion(root, "transmission", out raw))
            {
                return TryParseReceptionDate(raw);
            }
        }
        catch (JsonException)
        {
            // Ignorar si no es JSON válido
        }

        return null;
    }

    public static DateTime? TryParseReceptionDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var normalized = raw.Trim()
            .Replace("A.M.", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("P.M.", "PM", StringComparison.OrdinalIgnoreCase)
            .Replace("a. m.", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("p. m.", "PM", StringComparison.OrdinalIgnoreCase);

        if (DateTime.TryParseExact(
                normalized,
                ReceptionDateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var exact))
        {
            return exact;
        }

        return null;
    }

    public static SignedXmlFile BuildExcelFile(
        IReadOnlyList<DgiiExcelReportRow> rows,
        string? issuerRnc,
        DateTime fromDate,
        DateTime toDate)
    {
        var digits = SignedXmlExportLogic.Digits(issuerRnc);
        var fileName = string.IsNullOrEmpty(digits)
            ? $"Reporte_DGII_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx"
            : $"Reporte_DGII_{digits}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

        var bytes = RenderWorkbookBytes(rows);
        return new SignedXmlFile(bytes, fileName, ExcelContentType, rows.Count);
    }

    public static byte[] RenderWorkbookBytes(IReadOnlyList<DgiiExcelReportRow> rows)
    {
        var sharedStrings = new List<string>();
        var sharedStringIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var totalStringOccurrences = 0;

        int InternString(string value)
        {
            totalStringOccurrences++;
            if (!sharedStringIndex.TryGetValue(value, out var idx))
            {
                idx = sharedStrings.Count;
                sharedStrings.Add(value);
                sharedStringIndex[value] = idx;
            }
            return idx;
        }

        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteXmlEntry(archive, "[Content_Types].xml", BuildContentTypesXml());
            WriteXmlEntry(archive, "_rels/.rels", BuildRootRelsXml());
            WriteXmlEntry(archive, "xl/workbook.xml", BuildWorkbookXml());
            WriteXmlEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml());
            WriteXmlEntry(archive, "xl/styles.xml", BuildStylesXml());
            WriteXmlEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(rows, InternString));
            WriteXmlEntry(archive, "xl/sharedStrings.xml", BuildSharedStringsXml(sharedStrings, totalStringOccurrences));
        }

        return stream.ToArray();
    }

    private static void WriteXmlEntry(ZipArchive archive, string entryPath, string xmlContent)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        var bytes = Utf8NoBom.GetBytes(xmlContent);
        entryStream.Write(bytes, 0, bytes.Length);
    }

    private static string BuildContentTypesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""" +
        """<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""" +
        """<Default Extension="xml" ContentType="application/xml"/>""" +
        """<Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>""" +
        """<Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>""" +
        """<Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>""" +
        """<Override PartName="/xl/sharedStrings.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml"/>""" +
        """</Types>""";

    private static string BuildRootRelsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>""" +
        """</Relationships>""";

    private static string BuildWorkbookXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">""" +
        """<sheets><sheet name="Hoja1" sheetId="1" r:id="rId1"/></sheets>""" +
        """</workbook>""";

    private static string BuildWorkbookRelsXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""" +
        """<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>""" +
        """<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""" +
        """<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/>""" +
        """</Relationships>""";

    private static string BuildStylesXml() =>
        """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""" +
        """<styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""" +
        """<numFmts count="1"><numFmt numFmtId="43" formatCode="_(* #,##0.00_);_(* \(#,##0.00\);_(* &quot;-&quot;??_);_(@_)"/></numFmts>""" +
        """<fonts count="3">""" +
        """<font><sz val="11"/><name val="Aptos Narrow"/><family val="2"/></font>""" +
        """<font><sz val="11"/><name val="Aptos Narrow"/><family val="2"/></font>""" +
        """<font><b/><sz val="11"/><name val="Calibri"/></font>""" +
        """</fonts>""" +
        """<fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills>""" +
        """<borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>""" +
        """<cellStyleXfs count="2">""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>""" +
        """<xf numFmtId="43" fontId="1" fillId="0" borderId="0" applyFont="0" applyFill="0" applyBorder="0" applyAlignment="0" applyProtection="0"/>""" +
        """</cellStyleXfs>""" +
        """<cellXfs count="4">""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>""" +
        """<xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>""" +
        """<xf numFmtId="43" fontId="2" fillId="0" borderId="0" xfId="1" applyFont="1"/>""" +
        """<xf numFmtId="43" fontId="0" fillId="0" borderId="0" xfId="1" applyFont="1"/>""" +
        """</cellXfs>""" +
        """<cellStyles count="2">""" +
        """<cellStyle name="Millares" xfId="1" builtinId="3"/>""" +
        """<cellStyle name="Normal" xfId="0" builtinId="0"/>""" +
        """</cellStyles>""" +
        """</styleSheet>""";

    private static string BuildWorksheetXml(IReadOnlyList<DgiiExcelReportRow> rows, Func<string, int> internString)
    {
        var lastRow = rows.Count + 1;
        var sb = new StringBuilder(4096);
        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false }))
        {
            writer.WriteRaw("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
            writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

            writer.WriteStartElement("dimension");
            writer.WriteAttributeString("ref", $"A1:L{lastRow}");
            writer.WriteEndElement();

            writer.WriteStartElement("sheetViews");
            writer.WriteStartElement("sheetView");
            writer.WriteAttributeString("tabSelected", "1");
            writer.WriteAttributeString("workbookViewId", "0");
            writer.WriteEndElement();
            writer.WriteEndElement();

            writer.WriteStartElement("sheetFormatPr");
            writer.WriteAttributeString("baseColWidth", "10");
            writer.WriteAttributeString("defaultRowHeight", "15");
            writer.WriteEndElement();

            writer.WriteStartElement("cols");
            WriteCol(writer, 1, 1, "15");
            WriteCol(writer, 2, 2, "14");
            WriteCol(writer, 3, 3, "16.28515625");
            WriteCol(writer, 4, 5, "22.5703125");
            WriteCol(writer, 6, 6, "20.5703125");
            WriteCol(writer, 7, 7, "26.42578125");
            WriteCol(writer, 8, 8, "20.5703125");
            WriteCol(writer, 9, 9, "15.85546875");
            WriteCol(writer, 10, 10, "21.42578125");
            WriteCol(writer, 11, 11, "15");
            WriteCol(writer, 12, 12, "19.85546875");
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData");

            // Row 1: Headers
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", "1");
            writer.WriteAttributeString("spans", "1:12");
            for (var col = 0; col < Headers.Length; col++)
            {
                var cellRef = $"{ (char)('A' + col) }1";
                var styleIndex = col is 8 or 9 or 10 ? "2" : "1";
                WriteStringCell(writer, cellRef, internString(Headers[col]), styleIndex);
            }
            writer.WriteEndElement();

            // Data rows
            for (var i = 0; i < rows.Count; i++)
            {
                var r = i + 2;
                var row = rows[i];

                writer.WriteStartElement("row");
                writer.WriteAttributeString("r", r.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("spans", "1:12");

                if (row.RncReceptor is not null)
                    WriteStringCell(writer, $"A{r}", internString(row.RncReceptor));

                WriteStringCell(writer, $"B{r}", internString(row.ENcf));

                if (row.ENcfModificado is not null)
                    WriteStringCell(writer, $"C{r}", internString(row.ENcfModificado));

                WriteStringCell(writer, $"D{r}", internString(row.FechaComprobante));
                WriteStringCell(writer, $"E{r}", internString(row.FechaRecepcion));
                WriteStringCell(writer, $"F{r}", internString(row.AprobacionComercial));

                if (row.FechaAprobacionComercial is not null)
                    WriteStringCell(writer, $"G{r}", internString(row.FechaAprobacionComercial));

                WriteStringCell(writer, $"H{r}", internString(row.Estado));

                WriteDecimalCell(writer, $"I{r}", row.ItbisFacturado, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"J{r}", row.MontoTotalGravado, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"K{r}", row.MontoExento, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"L{r}", row.MontoNoFacturable, includeEmptyStyle: false);

                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // sheetData
            writer.WriteEndElement(); // worksheet
        }

        return sb.ToString();
    }

    private static string BuildSharedStringsXml(IReadOnlyList<string> strings, int count)
    {
        var sb = new StringBuilder(2048);
        using (var writer = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false }))
        {
            writer.WriteRaw("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
            writer.WriteStartElement("sst", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
            writer.WriteAttributeString("count", count.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("uniqueCount", strings.Count.ToString(CultureInfo.InvariantCulture));

            foreach (var s in strings)
            {
                writer.WriteStartElement("si");
                writer.WriteElementString("t", s);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        return sb.ToString();
    }

    private static void WriteCol(XmlWriter writer, int min, int max, string width)
    {
        writer.WriteStartElement("col");
        writer.WriteAttributeString("min", min.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("max", max.ToString(CultureInfo.InvariantCulture));
        writer.WriteAttributeString("width", width);
        writer.WriteAttributeString("bestFit", "1");
        writer.WriteAttributeString("customWidth", "1");
        writer.WriteEndElement();
    }

    private static void WriteStringCell(XmlWriter writer, string cellRef, int sharedStringIdx, string? styleIndex = null)
    {
        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", cellRef);
        if (styleIndex is not null)
        {
            writer.WriteAttributeString("s", styleIndex);
        }
        writer.WriteAttributeString("t", "s");
        writer.WriteElementString("v", sharedStringIdx.ToString(CultureInfo.InvariantCulture));
        writer.WriteEndElement();
    }

    private static void WriteDecimalCell(XmlWriter writer, string cellRef, decimal? value, bool includeEmptyStyle)
    {
        if (value is null && !includeEmptyStyle)
            return;

        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", cellRef);
        writer.WriteAttributeString("s", "3");
        if (value.HasValue)
        {
            // Evitar ".00" innecesario en el nodo <v> cuando es entero (el formato s="3" lo muestra con 2 decimales)
            var formatted = value.Value == Math.Truncate(value.Value)
                ? Math.Truncate(value.Value).ToString("0", CultureInfo.InvariantCulture)
                : value.Value.ToString("0.##", CultureInfo.InvariantCulture);
            writer.WriteElementString("v", formatted);
        }
        writer.WriteEndElement();
    }

    private static bool TryGetFechaRecepcion(JsonElement root, string propName, out string? fechaRecepcion)
    {
        fechaRecepcion = null;
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(propName, out var child)
            || child.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var prop in child.EnumerateObject())
        {
            if (prop.Name.Equals("FechaRecepcion", StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.String)
            {
                var val = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    fechaRecepcion = val;
                    return true;
                }
            }
        }

        return false;
    }

    private static XElement? FirstElement(XDocument? doc, string localName) =>
        doc?.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

    private static string? ElementValue(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase))?.Value;

    private static decimal? DecimalElementOrNull(XElement parent, string localName)
    {
        var raw = ElementValue(parent, localName);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        return decimal.TryParse(raw.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? FirstNonEmptyOrNull(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }
}

public interface IDgiiExcelReportService
{
    Task<SignedXmlExportResult> GetRangeExcelAsync(
        int? clientId,
        string? userId,
        bool isSuperAdmin,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);
}

public class DgiiExcelReportService(
    IEcfDocumentService documents,
    IEcfTransmissionService transmissions,
    IEcfXmlDocumentService xmlDocuments,
    IClientService clients) : IDgiiExcelReportService
{
    private const int RejectedOrConditionalStatusId = 11;

    private sealed record DocRow(
        int Id,
        int ClientId,
        string ENcf,
        string? CustomerRnc,
        string? ModifiedNcf,
        DateTime IssueDateUtc,
        DateTime RegisteredAt,
        decimal SubTotal,
        decimal ItbisTotal,
        int EcfStatusId);

    private sealed record TransmissionSummary(
        DateTime SentAtUtc,
        string? ResponsePayload,
        string? ResponseMessage,
        string? ResponseCode);

    public async Task<SignedXmlExportResult> GetRangeExcelAsync(
        int? clientId,
        string? userId,
        bool isSuperAdmin,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var (fromUtc, toUtcExclusive) = SignedXmlExportLogic.ToUtcRange(fromDate, toDate);

        var query = documents.Table
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                        && d.RegisteredAt >= fromUtc
                        && d.RegisteredAt < toUtcExclusive);

        if (!isSuperAdmin)
        {
            query = query.Where(d => d.Client.UserClients.Any(uc => uc.UserId == userId));
        }

        if (clientId is > 0)
        {
            query = query.Where(d => d.ClientId == clientId.Value);
        }

        var rows = await query
            .Select(d => new DocRow(
                d.EcfDocumentId,
                d.ClientId,
                d.Ncf,
                d.CustomerRnc,
                d.ModifiedNcf,
                d.IssueDateUtc,
                d.RegisteredAt,
                d.SubTotal,
                d.Itbistotal,
                d.EcfStatusId))
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return SignedXmlExportResult.Missing("No hay comprobantes aceptados en ese rango de fechas.");
        }

        // Para distinguir Rechazado de Aceptado Condicional en EcfStatusId == 11, leemos la última transmisión
        var ambiguousIds = rows
            .Where(r => r.EcfStatusId == RejectedOrConditionalStatusId)
            .Select(r => r.Id)
            .ToList();

        var ambiguousTransmissions = await LoadLatestTransmissionsAsync(ambiguousIds, cancellationToken);

        var candidates = rows
            .Select(r =>
            {
                var trans = ambiguousTransmissions.GetValueOrDefault(r.Id);
                var state = DgiiExcelReportLogic.ResolveReportState(
                    r.EcfStatusId,
                    trans?.ResponsePayload,
                    trans?.ResponseMessage,
                    trans?.ResponseCode);

                return new DgiiExcelReportCandidate(r.Id, r.ClientId, r.ENcf, state, r.RegisteredAt);
            })
            .ToList();

        var selected = DgiiExcelReportLogic.SelectAccepted(candidates);

        if (selected.Count == 0)
        {
            return SignedXmlExportResult.Missing("No hay comprobantes aceptados en ese rango de fechas.");
        }

        if (selected.Count > DgiiExcelReportLogic.MaxItems)
        {
            return SignedXmlExportResult.Excess(
                selected.Count,
                $"El rango tiene {selected.Count} comprobantes aceptados y el máximo por exportación es {DgiiExcelReportLogic.MaxItems}. Reduzca el rango de fechas.");
        }

        var selectedIds = selected.Select(c => c.EcfDocumentId).ToList();
        var allSelectedTransmissions = await LoadLatestTransmissionsAsync(selectedIds, cancellationToken);
        var xmlByDocId = await LoadXmlAsync(selectedIds, cancellationToken);
        var docRowById = rows.ToDictionary(r => r.Id);

        var reportRows = selected
            .Select(candidate =>
            {
                var doc = docRowById[candidate.EcfDocumentId];
                var trans = allSelectedTransmissions.GetValueOrDefault(candidate.EcfDocumentId);
                var xml = xmlByDocId.GetValueOrDefault(candidate.EcfDocumentId);

                return DgiiExcelReportLogic.BuildRow(new DgiiExcelReportSource(
                    EcfDocumentId: doc.Id,
                    ClientId: doc.ClientId,
                    ENcf: doc.ENcf,
                    CustomerRnc: doc.CustomerRnc,
                    ModifiedNcf: doc.ModifiedNcf,
                    IssueDateUtc: doc.IssueDateUtc,
                    RegisteredAt: doc.RegisteredAt,
                    SentAtUtc: trans?.SentAtUtc,
                    ResponsePayload: trans?.ResponsePayload,
                    SubTotal: doc.SubTotal,
                    ItbisTotal: doc.ItbisTotal,
                    State: candidate.State,
                    Xml: xml));
            })
            .ToList();

        string? issuerRnc = null;
        if (clientId is > 0)
        {
            issuerRnc = await clients.Table
                .AsNoTracking()
                .Where(c => c.ClientId == clientId.Value)
                .Select(c => c.Rnc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return SignedXmlExportResult.Success(
            DgiiExcelReportLogic.BuildExcelFile(reportRows, issuerRnc, fromDate, toDate));
    }

    private async Task<Dictionary<int, TransmissionSummary>> LoadLatestTransmissionsAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
        {
            return [];
        }

        var rows = await transmissions.Table
            .AsNoTracking()
            .Where(t => documentIds.Contains(t.EcfDocumentId))
            .Select(t => new
            {
                t.EcfDocumentId,
                t.SentAtUtc,
                t.ResponsePayload,
                t.ResponseMessage,
                t.ResponseCode
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.EcfDocumentId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(x => x.SentAtUtc).First();
                    return new TransmissionSummary(
                        latest.SentAtUtc,
                        latest.ResponsePayload,
                        latest.ResponseMessage,
                        latest.ResponseCode);
                });
    }

    private async Task<Dictionary<int, string>> LoadXmlAsync(
        IReadOnlyCollection<int> documentIds,
        CancellationToken cancellationToken)
    {
        if (documentIds.Count == 0)
        {
            return [];
        }

        var rows = await xmlDocuments.Table
            .AsNoTracking()
            .Where(x => documentIds.Contains(x.EcfDocumentId))
            .Select(x => new
            {
                x.EcfDocumentId,
                x.EcfXmlDocumentId,
                Xml = !string.IsNullOrEmpty(x.XmlSigned) ? x.XmlSigned : x.XmlUnsigned
            })
            .Where(x => x.Xml != null && x.Xml != "")
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.EcfDocumentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.EcfXmlDocumentId).First().Xml);
    }
}
