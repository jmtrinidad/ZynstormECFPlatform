using System.IO.Compression;
using System.Text;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class SignedXmlExportLogicTests
{
    private static SignedXmlCandidate Candidate(int id, string encf, EcfLookupState state, int day = 1) =>
        new(id, encf, state, new DateTime(2026, 10, day, 12, 0, 0, DateTimeKind.Utc));

    [Theory]
    [InlineData("2026-10-14", true)]
    [InlineData("2026-1-4", false)]
    [InlineData("14/10/2026", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseDate_IsStrict(string? raw, bool expected)
    {
        Assert.Equal(expected, SignedXmlExportLogic.TryParseDate(raw, out _));
    }

    [Fact]
    public void ToUtcRange_UsesDominicanDays_InclusiveOfTheLastDay()
    {
        var (from, toExclusive) = SignedXmlExportLogic.ToUtcRange(new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        // RD es UTC-4 todo el año: el día local empieza a las 04:00 UTC.
        Assert.Equal(new DateTime(2026, 10, 1, 4, 0, 0), from);
        Assert.Equal(new DateTime(2026, 10, 3, 4, 0, 0), toExclusive);
    }

    [Fact]
    public void SelectBest_KeepsOnlyUsableOnes_AndOnePerEncf()
    {
        var picked = SignedXmlExportLogic.SelectBest(
        [
            Candidate(1, "E310000000001", EcfLookupState.Rejected),
            Candidate(2, "E310000000001", EcfLookupState.Accepted, day: 2),
            Candidate(3, "E310000000002", EcfLookupState.Rejected),
            Candidate(4, "E310000000003", EcfLookupState.Pending),
            Candidate(5, "E310000000003", EcfLookupState.Accepted, day: 3),
            Candidate(6, "E310000000004", EcfLookupState.Error),
            Candidate(7, "E310000000005", EcfLookupState.NotSent)
        ]);

        Assert.Equal(["E310000000001", "E310000000003"], picked.Select(c => c.ENcf).ToArray());
        Assert.Equal([2, 5], picked.Select(c => c.EcfDocumentId).ToArray());
    }

    [Fact]
    public void SelectBest_OrdersByEncf()
    {
        var picked = SignedXmlExportLogic.SelectBest(
        [
            Candidate(1, "E310000000009", EcfLookupState.Accepted),
            Candidate(2, "E310000000003", EcfLookupState.Accepted)
        ]);

        Assert.Equal(["E310000000003", "E310000000009"], picked.Select(c => c.ENcf).ToArray());
    }

    [Fact]
    public void BuildFile_OneItem_IsThePlainXml_WithExactBytes()
    {
        var xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><ECF><eNCF>E310000000001</eNCF></ECF>";

        var file = SignedXmlExportLogic.BuildFile(
            [new SignedXmlItem("E310000000001", xml)], "132293894", new DateTime(2026, 10, 1), new DateTime(2026, 10, 2));

        Assert.Equal("E310000000001.xml", file.FileName);
        Assert.Equal("application/xml", file.ContentType);
        Assert.Equal(1, file.Count);
        Assert.Equal(Encoding.UTF8.GetBytes(xml), file.Content);
        Assert.NotEqual(0xEF, file.Content[0]);
    }

    [Fact]
    public void BuildFile_SeveralItems_IsAZipWithOneEntryEach()
    {
        var items = new[]
        {
            new SignedXmlItem("E310000000001", "<ECF>uno ñ</ECF>"),
            new SignedXmlItem("E310000000002", "<ECF>dos</ECF>")
        };

        var file = SignedXmlExportLogic.BuildFile(items, "1-32-29389-4", new DateTime(2026, 10, 1), new DateTime(2026, 10, 14));

        Assert.Equal("XML_firmados_132293894_20261001_20261014.zip", file.FileName);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal(2, file.Count);

        using var archive = new ZipArchive(new MemoryStream(file.Content), ZipArchiveMode.Read);
        Assert.Equal(["E310000000001.xml", "E310000000002.xml"], archive.Entries.Select(e => e.FullName).ToArray());

        using var reader = new StreamReader(archive.GetEntry("E310000000001.xml")!.Open(), new UTF8Encoding(false));
        Assert.Equal("<ECF>uno ñ</ECF>", reader.ReadToEnd());
    }

    [Fact]
    public void Digits_KeepsOnlyDigits()
    {
        Assert.Equal("132293894", SignedXmlExportLogic.Digits("1-32-29389-4"));
        Assert.Equal(string.Empty, SignedXmlExportLogic.Digits(null));
    }
}
