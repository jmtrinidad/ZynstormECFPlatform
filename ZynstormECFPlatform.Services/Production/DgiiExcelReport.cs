using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ZynstormECFPlatform.Abstractions.DataServices;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Services.Production;

/// <summary>
/// Estado del comprobante dentro del reporte Excel DGII.
/// </summary>
public enum DgiiExcelReportState
{
    Excluded = 0,
    Accepted = 1,
    ConditionallyAccepted = 2
}

/// <summary>
/// Candidato liviano para filtrar y deduplicar comprobantes aceptados antes de cargar los XML.
/// </summary>
public sealed record DgiiExcelReportCandidate(
    int EcfDocumentId,
    int ClientId,
    string ENcf,
    DgiiExcelReportState State,
    DateTime RegisteredAt);

/// <summary>
/// Datos de entrada de un comprobante aceptado para construir su fila en el Excel DGII.
/// </summary>
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
    DgiiExcelReportState State,
    string? Xml);

/// <summary>
/// Fila ya resuelta del reporte Excel DGII (12 columnas A–L).
/// </summary>
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

/// <summary>
/// Lógica pura para extraer los datos del XML de cada e-CF y generar el archivo .xlsx
/// con el formato del reporte DGII de comprobantes exentos y gravados.
/// </summary>
public static class DgiiExcelReportLogic
{
    public const int MaxItems = 5000;
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ReportTitle = "REPORTE DE COMPROBANTES FISCALES ELECTRÓNICOS (e-CF) — EXENTOS Y GRAVADOS DGII";
    public const int HeaderRowIndex = 5;
    public const int FirstDataRowIndex = 6;

    private const int AcceptedStatusId = 10;
    private const int RejectedOrConditionalStatusId = 11;

    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

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
        "dd-MM-yyyy hh:mm:ss tt",
        "dd-MM-yyyy h:mm:ss tt",
        "dd/MM/yyyy HH:mm:ss",
        "dd-MM-yyyy HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ssZ"
    ];

    /// <summary>
    /// Determina si un comprobante debe incluirse en el reporte como "Aceptado" o "Aceptado Condicional".
    /// </summary>
    public static DgiiExcelReportState ResolveReportState(
        int ecfStatusId,
        string? responsePayload,
        string? responseMessage,
        string? responseCode = null)
    {
        var state = EcfLookupLogic.ResolveState(ecfStatusId, responsePayload);
        if (state == EcfLookupState.AcceptedConditional)
        {
            return DgiiExcelReportState.ConditionallyAccepted;
        }

        var messageIndicatesConditional = !string.IsNullOrWhiteSpace(responseMessage)
            && (responseMessage.Contains("condicional", StringComparison.OrdinalIgnoreCase)
                || responseMessage.Contains("conditional", StringComparison.OrdinalIgnoreCase));

        if (ecfStatusId == AcceptedStatusId)
        {
            return messageIndicatesConditional
                ? DgiiExcelReportState.ConditionallyAccepted
                : DgiiExcelReportState.Accepted;
        }

        if (ecfStatusId == RejectedOrConditionalStatusId && messageIndicatesConditional)
        {
            return DgiiExcelReportState.ConditionallyAccepted;
        }

        return DgiiExcelReportState.Excluded;
    }

    /// <summary>
    /// Filtra los comprobantes aceptados (incluyendo Aceptado Condicional) y deduplica
    /// reintentos por (ClientId, ENcf) priorizando el intento aceptado más reciente.
    /// </summary>
    public static IReadOnlyList<DgiiExcelReportCandidate> SelectAccepted(
        IEnumerable<DgiiExcelReportCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return candidates
            .Where(c => c.State != DgiiExcelReportState.Excluded && !string.IsNullOrWhiteSpace(c.ENcf))
            .GroupBy(c => (c.ClientId, ENcf: c.ENcf.Trim().ToUpperInvariant()))
            .Select(group => group
                .OrderBy(c => c.State == DgiiExcelReportState.Accepted ? 0 : 1)
                .ThenByDescending(c => c.RegisteredAt)
                .ThenByDescending(c => c.EcfDocumentId)
                .First())
            .OrderBy(c => c.RegisteredAt)
            .ThenBy(c => c.EcfDocumentId)
            .ToList();
    }

    /// <summary>
    /// Construye la fila del reporte combinando los datos del XML firmado con los metadatos de recepción.
    /// </summary>
    public static DgiiExcelReportRow BuildRow(DgiiExcelReportSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var xmlDoc = TryParseXml(source.Xml);

        var idDoc = FirstElement(xmlDoc, "IdDoc");
        var emisor = FirstElement(xmlDoc, "Emisor");
        var comprador = FirstElement(xmlDoc, "Comprador");
        var totales = FirstElement(xmlDoc, "Totales");
        var infoRef = FirstElement(xmlDoc, "InformacionReferencia");

        var rncReceptor = FirstNonEmptyOrNull(
            ElementValue(comprador, "RNCComprador"),
            source.CustomerRnc);

        var eNcf = FirstNonEmptyOrNull(
            ElementValue(idDoc, "eNCF"),
            source.ENcf) ?? string.Empty;

        var eNcfModificado = FirstNonEmptyOrNull(
            ElementValue(infoRef, "NCFModificado"),
            source.ModifiedNcf);

        var issueDate = TryParseIssueDate(ElementValue(emisor, "FechaEmision"))
                        ?? source.IssueDateUtc.Date;

        var receptionDate = TryExtractDgiiReceptionDate(source.ResponsePayload)
                            ?? (source.SentAtUtc ?? source.RegisteredAt).ToDrTime();

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
            itbisFacturado = source.ItbisTotal;
            montoTotalGravado = source.SubTotal > 0 ? source.SubTotal : null;
            montoExento = null;
            montoNoFacturable = null;
        }

        var estado = source.State == DgiiExcelReportState.ConditionallyAccepted
            ? "Aceptado Condicional"
            : "Aceptado";

        return new DgiiExcelReportRow(
            RncReceptor: rncReceptor,
            ENcf: eNcf,
            ENcfModificado: eNcfModificado,
            FechaComprobante: FormatDrDateTime(issueDate.Date),
            FechaRecepcion: FormatDrDateTime(receptionDate),
            AprobacionComercial: string.Empty,
            FechaAprobacionComercial: null,
            Estado: estado,
            ItbisFacturado: itbisFacturado,
            MontoTotalGravado: montoTotalGravado,
            MontoExento: montoExento,
            MontoNoFacturable: montoNoFacturable);
    }

    /// <summary>
    /// Formatea una fecha/hora con el formato exacto del reporte DGII: "dd/MM/yyyy hh:mm:ss A.M./P.M.".
    /// </summary>
    public static string FormatDrDateTime(DateTime dt)
    {
        var datePart = dt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        var timePart = dt.ToString("hh:mm:ss", CultureInfo.InvariantCulture);
        var amPm = dt.Hour < 12 ? "A.M." : "P.M.";
        return $"{datePart} {timePart} {amPm}";
    }

    public static XDocument? TryParseXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        try
        {
            return XDocument.Parse(xml, LoadOptions.None);
        }
        catch (XmlException)
        {
            return null;
        }
    }

    public static DateTime? TryParseIssueDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        if (DateTime.TryParseExact(
                trimmed,
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
        string? clientName,
        string? issuerRnc,
        DateTime fromDate,
        DateTime toDate)
    {
        var digits = SignedXmlExportLogic.Digits(issuerRnc);
        var fileName = string.IsNullOrEmpty(digits)
            ? $"Reporte_DGII_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx"
            : $"Reporte_DGII_{digits}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

        var bytes = RenderWorkbookBytes(rows, clientName, issuerRnc, fromDate, toDate);
        return new SignedXmlFile(bytes, fileName, ExcelContentType, rows.Count);
    }

    public static byte[] RenderWorkbookBytes(
        IReadOnlyList<DgiiExcelReportRow> rows,
        string? clientName = null,
        string? issuerRnc = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
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
            WriteXmlEntry(
                archive,
                "xl/worksheets/sheet1.xml",
                BuildWorksheetXml(rows, clientName, issuerRnc, fromDate, toDate, InternString));
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
        """<fonts count="5">""" +
        """<font><sz val="11"/><color rgb="FF1F2937"/><name val="Calibri"/><family val="2"/></font>""" +
        """<font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/><family val="2"/></font>""" +
        """<font><b/><sz val="13"/><color rgb="FFFFFFFF"/><name val="Calibri"/><family val="2"/></font>""" +
        """<font><b/><sz val="11"/><color rgb="FF1F4E78"/><name val="Calibri"/><family val="2"/></font>""" +
        """<font><sz val="10"/><color rgb="FF374151"/><name val="Calibri"/><family val="2"/></font>""" +
        """</fonts>""" +
        """<fills count="6">""" +
        """<fill><patternFill patternType="none"/></fill>""" +
        """<fill><patternFill patternType="gray125"/></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FF1F4E78"/><bgColor indexed="64"/></patternFill></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FFD9E1F2"/><bgColor indexed="64"/></patternFill></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FFEDF2F8"/><bgColor indexed="64"/></patternFill></fill>""" +
        """<fill><patternFill patternType="solid"><fgColor rgb="FF2F5597"/><bgColor indexed="64"/></patternFill></fill>""" +
        """</fills>""" +
        """<borders count="3">""" +
        """<border><left/><right/><top/><bottom/><diagonal/></border>""" +
        """<border>""" +
        """<left style="thin"><color rgb="FF1F4E78"/></left>""" +
        """<right style="thin"><color rgb="FF1F4E78"/></right>""" +
        """<top style="thin"><color rgb="FF1F4E78"/></top>""" +
        """<bottom style="medium"><color rgb="FF1F4E78"/></bottom>""" +
        """<diagonal/>""" +
        """</border>""" +
        """<border>""" +
        """<left style="thin"><color rgb="FFD9D9D9"/></left>""" +
        """<right style="thin"><color rgb="FFD9D9D9"/></right>""" +
        """<top style="thin"><color rgb="FFD9D9D9"/></top>""" +
        """<bottom style="thin"><color rgb="FFD9D9D9"/></bottom>""" +
        """<diagonal/>""" +
        """</border>""" +
        """</borders>""" +
        """<cellStyleXfs count="2">""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0"/>""" +
        """<xf numFmtId="43" fontId="0" fillId="0" borderId="0" applyFont="0" applyFill="0" applyBorder="0" applyAlignment="0" applyProtection="0"/>""" +
        """</cellStyleXfs>""" +
        """<cellXfs count="8">""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>""" +
        """<xf numFmtId="0" fontId="1" fillId="5" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>""" +
        """<xf numFmtId="43" fontId="1" fillId="5" borderId="1" xfId="1" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf>""" +
        """<xf numFmtId="43" fontId="0" fillId="0" borderId="2" xfId="1" applyNumberFormat="1" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf>""" +
        """<xf numFmtId="0" fontId="0" fillId="0" borderId="2" xfId="0" applyFont="1" applyBorder="1" applyAlignment="1"><alignment vertical="center"/></xf>""" +
        """<xf numFmtId="0" fontId="2" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>""" +
        """<xf numFmtId="0" fontId="3" fillId="3" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>""" +
        """<xf numFmtId="0" fontId="4" fillId="4" borderId="2" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="left" vertical="center"/></xf>""" +
        """</cellXfs>""" +
        """<cellStyles count="2">""" +
        """<cellStyle name="Millares" xfId="1" builtinId="3"/>""" +
        """<cellStyle name="Normal" xfId="0" builtinId="0"/>""" +
        """</cellStyles>""" +
        """</styleSheet>""";

    private static string BuildWorksheetXml(
        IReadOnlyList<DgiiExcelReportRow> rows,
        string? clientName,
        string? issuerRnc,
        DateTime? fromDate,
        DateTime? toDate,
        Func<string, int> internString)
    {
        var lastRow = Math.Max(HeaderRowIndex, HeaderRowIndex + rows.Count);
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
            WriteCol(writer, 1, 1, "16");
            WriteCol(writer, 2, 2, "16");
            WriteCol(writer, 3, 3, "18");
            WriteCol(writer, 4, 5, "24");
            WriteCol(writer, 6, 6, "22");
            WriteCol(writer, 7, 7, "27");
            WriteCol(writer, 8, 8, "21");
            WriteCol(writer, 9, 9, "17");
            WriteCol(writer, 10, 10, "22");
            WriteCol(writer, 11, 11, "17");
            WriteCol(writer, 12, 12, "21");
            writer.WriteEndElement();

            writer.WriteStartElement("sheetData");

            // Fila 1: Título del reporte (A1:L1 combinadas, estilo s="5")
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", "1");
            writer.WriteAttributeString("spans", "1:12");
            writer.WriteAttributeString("ht", "28");
            writer.WriteAttributeString("customHeight", "1");
            WriteBannerRowCells(writer, rowNumber: 1, textIdx: internString(ReportTitle), styleIndex: "5");
            writer.WriteEndElement();

            // Fila 2: Cliente y RNC (A2:L2 combinadas, estilo s="6")
            var resolvedClientName = string.IsNullOrWhiteSpace(clientName) ? "No especificado" : clientName.Trim();
            var clientLine = string.IsNullOrWhiteSpace(issuerRnc)
                ? $"Cliente: {resolvedClientName}"
                : $"Cliente: {resolvedClientName}   |   RNC: {issuerRnc.Trim()}";

            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", "2");
            writer.WriteAttributeString("spans", "1:12");
            writer.WriteAttributeString("ht", "22");
            writer.WriteAttributeString("customHeight", "1");
            WriteBannerRowCells(writer, rowNumber: 2, textIdx: internString(clientLine), styleIndex: "6");
            writer.WriteEndElement();

            // Fila 3: Período y total de comprobantes (A3:L3 combinadas, estilo s="7")
            var periodPart = fromDate.HasValue && toDate.HasValue
                ? $"Período: {fromDate.Value:dd/MM/yyyy} al {toDate.Value:dd/MM/yyyy}   |   "
                : string.Empty;
            var metaLine = $"{periodPart}Total de comprobantes aceptados: {rows.Count}";

            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", "3");
            writer.WriteAttributeString("spans", "1:12");
            writer.WriteAttributeString("ht", "20");
            writer.WriteAttributeString("customHeight", "1");
            WriteBannerRowCells(writer, rowNumber: 3, textIdx: internString(metaLine), styleIndex: "7");
            writer.WriteEndElement();

            // Fila 4: Separador visual vacío
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", "4");
            writer.WriteAttributeString("ht", "10");
            writer.WriteAttributeString("customHeight", "1");
            writer.WriteEndElement();

            // Fila 5: Encabezados de la tabla (A5:L5, estilo s="1" texto / s="2" montos)
            writer.WriteStartElement("row");
            writer.WriteAttributeString("r", HeaderRowIndex.ToString(CultureInfo.InvariantCulture));
            writer.WriteAttributeString("spans", "1:12");
            writer.WriteAttributeString("ht", "24");
            writer.WriteAttributeString("customHeight", "1");
            for (var col = 0; col < Headers.Length; col++)
            {
                var cellRef = $"{(char)('A' + col)}{HeaderRowIndex}";
                var styleIndex = col >= 8 ? "2" : "1";
                WriteStringCell(writer, cellRef, internString(Headers[col]), styleIndex);
            }
            writer.WriteEndElement();

            // Filas 6+: Datos de comprobantes
            for (var i = 0; i < rows.Count; i++)
            {
                var r = FirstDataRowIndex + i;
                var row = rows[i];

                writer.WriteStartElement("row");
                writer.WriteAttributeString("r", r.ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("spans", "1:12");
                writer.WriteAttributeString("ht", "19");
                writer.WriteAttributeString("customHeight", "1");

                WriteOptionalStringCell(writer, $"A{r}", row.RncReceptor, internString, styleIndex: "4");
                WriteStringCell(writer, $"B{r}", internString(row.ENcf), styleIndex: "4");
                WriteOptionalStringCell(writer, $"C{r}", row.ENcfModificado, internString, styleIndex: "4");
                WriteStringCell(writer, $"D{r}", internString(row.FechaComprobante), styleIndex: "4");
                WriteStringCell(writer, $"E{r}", internString(row.FechaRecepcion), styleIndex: "4");
                WriteStringCell(writer, $"F{r}", internString(row.AprobacionComercial), styleIndex: "4");
                WriteOptionalStringCell(writer, $"G{r}", row.FechaAprobacionComercial, internString, styleIndex: "4");
                WriteStringCell(writer, $"H{r}", internString(row.Estado), styleIndex: "4");

                WriteDecimalCell(writer, $"I{r}", row.ItbisFacturado, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"J{r}", row.MontoTotalGravado, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"K{r}", row.MontoExento, includeEmptyStyle: true);
                WriteDecimalCell(writer, $"L{r}", row.MontoNoFacturable, includeEmptyStyle: true);

                writer.WriteEndElement();
            }

            writer.WriteEndElement(); // sheetData

            // Celdas combinadas para el encabezado del reporte
            writer.WriteStartElement("mergeCells");
            writer.WriteAttributeString("count", "3");
            WriteMergeCell(writer, "A1:L1");
            WriteMergeCell(writer, "A2:L2");
            WriteMergeCell(writer, "A3:L3");
            writer.WriteEndElement();

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

    private static void WriteMergeCell(XmlWriter writer, string reference)
    {
        writer.WriteStartElement("mergeCell");
        writer.WriteAttributeString("ref", reference);
        writer.WriteEndElement();
    }

    private static void WriteBannerRowCells(XmlWriter writer, int rowNumber, int textIdx, string styleIndex)
    {
        WriteStringCell(writer, $"A{rowNumber}", textIdx, styleIndex);
        for (var col = 1; col < Headers.Length; col++)
        {
            var cellRef = $"{(char)('A' + col)}{rowNumber}";
            writer.WriteStartElement("c");
            writer.WriteAttributeString("r", cellRef);
            writer.WriteAttributeString("s", styleIndex);
            writer.WriteEndElement();
        }
    }

    private static void WriteOptionalStringCell(
        XmlWriter writer,
        string cellRef,
        string? value,
        Func<string, int> internString,
        string styleIndex)
    {
        if (value is not null)
        {
            WriteStringCell(writer, cellRef, internString(value), styleIndex);
            return;
        }

        writer.WriteStartElement("c");
        writer.WriteAttributeString("r", cellRef);
        writer.WriteAttributeString("s", styleIndex);
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
        int clientId,
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
        int clientId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var clientInfo = await clients.Table
            .AsNoTracking()
            .Where(c => c.ClientId == clientId)
            .Select(c => new { c.Name, c.Rnc })
            .FirstOrDefaultAsync(cancellationToken);

        if (clientInfo is null)
        {
            return SignedXmlExportResult.Missing("No se encontró el cliente especificado.");
        }

        var (fromUtc, toUtcExclusive) = SignedXmlExportLogic.ToUtcRange(fromDate, toDate);

        var rows = await documents.Table
            .AsNoTracking()
            .Where(d => !d.IsDeleted
                        && d.ClientId == clientId
                        && d.RegisteredAt >= fromUtc
                        && d.RegisteredAt < toUtcExclusive)
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
            return SignedXmlExportResult.Missing("No hay comprobantes aceptados para este cliente en ese rango de fechas.");
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
            return SignedXmlExportResult.Missing("No hay comprobantes aceptados para este cliente en ese rango de fechas.");
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

        return SignedXmlExportResult.Success(
            DgiiExcelReportLogic.BuildExcelFile(reportRows, clientInfo.Name, clientInfo.Rnc, fromDate, toDate));
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
