using System.IO.Compression;
using System.Xml.Linq;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiExcelReportLogicTests
{
    private static DgiiExcelReportCandidate Candidate(int id, int clientId, string encf, DgiiExcelReportState state, int day = 1) =>
        new(id, clientId, encf, state, new DateTime(2026, 9, day, 12, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void SelectAccepted_KeepsAcceptedAndAcceptedConditional_AndOnePerClientAndEncf()
    {
        var picked = DgiiExcelReportLogic.SelectAccepted(
        [
            Candidate(1, 10, "E310000002232", DgiiExcelReportState.Excluded, day: 1),
            Candidate(2, 10, "E310000002232", DgiiExcelReportState.Accepted, day: 2),
            Candidate(3, 10, "E410000000217", DgiiExcelReportState.ConditionallyAccepted, day: 1),
            Candidate(4, 10, "E310000002233", DgiiExcelReportState.Excluded, day: 1)
        ]);

        Assert.Equal(["E410000000217", "E310000002232"], picked.Select(c => c.ENcf).ToArray());
        Assert.Equal([3, 2], picked.Select(c => c.EcfDocumentId).ToArray());
    }

    [Fact]
    public void ResolveReportState_HandlesAcceptedAndConditionalVariants()
    {
        Assert.Equal(
            DgiiExcelReportState.Accepted,
            DgiiExcelReportLogic.ResolveReportState(10, null, null, null));

        Assert.Equal(
            DgiiExcelReportState.ConditionallyAccepted,
            DgiiExcelReportLogic.ResolveReportState(
                11,
                "{\"status\":{\"Codigo\":\"4\",\"Estado\":\"Aceptado Condicional\"}}",
                null,
                null));

        Assert.Equal(
            DgiiExcelReportState.ConditionallyAccepted,
            DgiiExcelReportLogic.ResolveReportState(
                11,
                "{\"statusResponse\":{\"Codigo\":\"4\",\"Estado\":\"Aceptado Condicionalmente\"}}",
                null,
                null));

        Assert.Equal(
            DgiiExcelReportState.ConditionallyAccepted,
            DgiiExcelReportLogic.ResolveReportState(
                11,
                null,
                "Comprobante aceptado condicionalmente por la DGII",
                "2"));

        Assert.Equal(
            DgiiExcelReportState.Excluded,
            DgiiExcelReportLogic.ResolveReportState(
                11,
                "{\"status\":{\"Codigo\":\"2\",\"Estado\":\"Rechazado\"}}",
                "Rechazado por la DGII",
                "2"));
    }

    [Fact]
    public void BuildRow_ExtractsExemptAndTaxableFieldsFromXml()
    {
        const string exemptXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ECF>
              <Encabezado>
                <IdDoc>
                  <TipoeCF>31</TipoeCF>
                  <eNCF>E310000002232</eNCF>
                </IdDoc>
                <Emisor>
                  <RNCEmisor>132293894</RNCEmisor>
                  <FechaEmision>01-09-2026</FechaEmision>
                </Emisor>
                <Comprador>
                  <RNCComprador>101000002</RNCComprador>
                  <RazonSocialComprador>CLIENTE EJEMPLO</RazonSocialComprador>
                </Comprador>
                <Totales>
                  <MontoExento>94014.00</MontoExento>
                  <MontoTotal>94014.00</MontoTotal>
                </Totales>
              </Encabezado>
            </ECF>
            """;

        var row = DgiiExcelReportLogic.BuildRow(new DgiiExcelReportSource(
            EcfDocumentId: 1,
            ClientId: 10,
            ENcf: "E310000002232",
            CustomerRnc: "101000002",
            ModifiedNcf: null,
            IssueDateUtc: new DateTime(2026, 9, 1),
            RegisteredAt: new DateTime(2026, 9, 1, 12, 26, 2, DateTimeKind.Utc),
            SentAtUtc: new DateTime(2026, 9, 1, 12, 26, 2, DateTimeKind.Utc), // 08:26:02 A.M. en RD (UTC-4)
            ResponsePayload: null,
            SubTotal: 94014m,
            ItbisTotal: 0m,
            State: DgiiExcelReportState.Accepted,
            Xml: exemptXml));

        Assert.Equal("101000002", row.RncReceptor);
        Assert.Equal("E310000002232", row.ENcf);
        Assert.Null(row.ENcfModificado);
        Assert.Equal("01/09/2026 12:00:00 A.M.", row.FechaComprobante);
        Assert.Equal("01/09/2026 08:26:02 A.M.", row.FechaRecepcion);
        Assert.Equal(string.Empty, row.AprobacionComercial);
        Assert.Null(row.FechaAprobacionComercial);
        Assert.Equal("Aceptado", row.Estado);
        Assert.Null(row.ItbisFacturado);
        Assert.Null(row.MontoTotalGravado);
        Assert.Equal(94014m, row.MontoExento);
        Assert.Null(row.MontoNoFacturable);
    }

    [Fact]
    public void BuildRow_ExtractsTaxableCreditNoteAndConditionalState()
    {
        const string creditNoteXml = """
            <?xml version="1.0" encoding="utf-8"?>
            <ECF>
              <Encabezado>
                <IdDoc>
                  <TipoeCF>34</TipoeCF>
                  <eNCF>E340000000772</eNCF>
                </IdDoc>
                <Emisor>
                  <RNCEmisor>132293894</RNCEmisor>
                  <FechaEmision>02-09-2026</FechaEmision>
                </Emisor>
                <Comprador>
                  <RNCComprador>131880600</RNCComprador>
                </Comprador>
                <Totales>
                  <MontoGravadoTotal>1906.78</MontoGravadoTotal>
                  <MontoExento>0.00</MontoExento>
                  <TotalITBIS>343.22</TotalITBIS>
                  <MontoTotal>2250.00</MontoTotal>
                  <MontoNoFacturable>50.00</MontoNoFacturable>
                </Totales>
              </Encabezado>
              <InformacionReferencia>
                <NCFModificado>E320000011885</NCFModificado>
              </InformacionReferencia>
            </ECF>
            """;

        var row = DgiiExcelReportLogic.BuildRow(new DgiiExcelReportSource(
            EcfDocumentId: 2,
            ClientId: 10,
            ENcf: "E340000000772",
            CustomerRnc: "131880600",
            ModifiedNcf: "E320000011885",
            IssueDateUtc: new DateTime(2026, 9, 2),
            RegisteredAt: new DateTime(2026, 9, 2, 16, 4, 58, DateTimeKind.Utc),
            SentAtUtc: new DateTime(2026, 9, 2, 16, 4, 58, DateTimeKind.Utc), // 12:04:58 P.M. en RD
            ResponsePayload: "{\"status\":{\"FechaRecepcion\":\"02/09/2026 12:04:58 P.M.\"}}",
            SubTotal: 1906.78m,
            ItbisTotal: 343.22m,
            State: DgiiExcelReportState.ConditionallyAccepted,
            Xml: creditNoteXml));

        Assert.Equal("131880600", row.RncReceptor);
        Assert.Equal("E340000000772", row.ENcf);
        Assert.Equal("E320000011885", row.ENcfModificado);
        Assert.Equal("02/09/2026 12:00:00 A.M.", row.FechaComprobante);
        Assert.Equal("02/09/2026 12:04:58 P.M.", row.FechaRecepcion);
        Assert.Equal("Aceptado Condicional", row.Estado);
        Assert.Equal(343.22m, row.ItbisFacturado);
        Assert.Equal(1906.78m, row.MontoTotalGravado);
        Assert.Equal(0m, row.MontoExento);
        Assert.Equal(50m, row.MontoNoFacturable);
    }

    [Fact]
    public void BuildExcelFile_GeneratesValidXlsxPackageWithTitleClientHeadersAndCells()
    {
        var rows = new[]
        {
            new DgiiExcelReportRow(
                RncReceptor: "101000002",
                ENcf: "E310000002232",
                ENcfModificado: null,
                FechaComprobante: "01/09/2026 12:00:00 A.M.",
                FechaRecepcion: "01/09/2026 08:26:02 A.M.",
                AprobacionComercial: string.Empty,
                FechaAprobacionComercial: null,
                Estado: "Aceptado",
                ItbisFacturado: null,
                MontoTotalGravado: null,
                MontoExento: 94014m,
                MontoNoFacturable: null),
            new DgiiExcelReportRow(
                RncReceptor: "131880600",
                ENcf: "E410000000219",
                ENcfModificado: null,
                FechaComprobante: "01/09/2026 12:00:00 A.M.",
                FechaRecepcion: "01/09/2026 11:33:22 A.M.",
                AprobacionComercial: string.Empty,
                FechaAprobacionComercial: null,
                Estado: "Aceptado Condicional",
                ItbisFacturado: 742.27m,
                MontoTotalGravado: 4123.71m,
                MontoExento: 0m,
                MontoNoFacturable: null)
        };

        var file = DgiiExcelReportLogic.BuildExcelFile(
            rows,
            "FARMACIA CARIBE SRL",
            "1-32-29389-4",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 3));

        Assert.Equal("Reporte_DGII_132293894_20260901_20260903.xlsx", file.FileName);
        Assert.Equal(DgiiExcelReportLogic.ExcelContentType, file.ContentType);
        Assert.Equal(2, file.Count);

        using var archive = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);
        Assert.NotNull(archive.GetEntry("xl/worksheets/sheet1.xml"));
        Assert.NotNull(archive.GetEntry("xl/sharedStrings.xml"));

        using var sstStream = archive.GetEntry("xl/sharedStrings.xml")!.Open();
        var sstDoc = XDocument.Load(sstStream);
        var sharedStrings = sstDoc.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value).ToList();

        Assert.Contains(DgiiExcelReportLogic.ReportTitle, sharedStrings);
        Assert.Contains(sharedStrings, s => s.Contains("FARMACIA CARIBE SRL") && s.Contains("1-32-29389-4"));

        foreach (var header in DgiiExcelReportLogic.Headers)
        {
            Assert.Contains(header, sharedStrings);
        }

        Assert.Contains("101000002", sharedStrings);
        Assert.Contains("E310000002232", sharedStrings);
        Assert.Contains("Aceptado Condicional", sharedStrings);

        using var sheetStream = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
        var sheetDoc = XDocument.Load(sheetStream);
        var cells = sheetDoc.Descendants().Where(e => e.Name.LocalName == "c").ToDictionary(
            c => c.Attribute("r")!.Value,
            c => c);

        // Fila 6 (primera fila de datos): K6 = Monto Exento 94014
        Assert.Equal("94014", cells["K6"].Elements().First(e => e.Name.LocalName == "v").Value);
        // Fila 7 (segunda fila de datos): I7 = ITBIS Facturado 742.27, J7 = 4123.71, K7 = 0
        Assert.Equal("742.27", cells["I7"].Elements().First(e => e.Name.LocalName == "v").Value);
        Assert.Equal("4123.71", cells["J7"].Elements().First(e => e.Name.LocalName == "v").Value);
        Assert.Equal("0", cells["K7"].Elements().First(e => e.Name.LocalName == "v").Value);
    }
}
