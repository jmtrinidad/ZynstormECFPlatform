using System.IO.Compression;
using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Tests.Certification;

public class SimulationDatabaseZipTests
{
    private static List<CertificationDocument> Docs() => new()
    {
        new() { ENcfSecuence = "E310000000007", TrackId = "t1", Status = DocumentStatus.Accepted, XmlSent = "<ECF>31</ECF>" },
        new() { ENcfSecuence = "E340000000009", TrackId = "t2", Status = DocumentStatus.Rejected, XmlSent = "<ECF>34</ECF>" },
        new() { ENcfSecuence = "E320000000018", TrackId = "t3", Status = DocumentStatus.Accepted, XmlSent = "<RFCE>32</RFCE>" },
        new() { ENcfSecuence = "E320000000018", TrackId = "MANUAL", Status = DocumentStatus.Accepted, XmlSent = "<ECF>32 manual</ECF>" },
    };

    [Theory]
    [InlineData("DB_2", 2)]
    [InlineData("DB_15", 15)]
    public void TryParseDatabaseJobId_ReadsProcessId(string jobId, int expected)
    {
        Assert.True(OldCertificationSimulationService.TryParseDatabaseJobId(jobId, out var processId));
        Assert.Equal(expected, processId);
    }

    [Theory]
    [InlineData("a1b2c3d4")]
    [InlineData("DB_")]
    [InlineData("DB_x")]
    [InlineData(null)]
    public void TryParseDatabaseJobId_RejectsInMemoryOrInvalidIds(string? jobId)
    {
        Assert.False(OldCertificationSimulationService.TryParseDatabaseJobId(jobId, out _));
    }

    [Fact]
    public void ZipEntries_Manual_OnlyManualDocsWithUploadName()
    {
        var entries = OldCertificationSimulationService.GetDatabaseZipEntries(Docs(), manual: true);

        var entry = Assert.Single(entries);
        Assert.Equal("SUBIR_DGII_E320000000018.xml", entry.Name);
        Assert.Equal("<ECF>32 manual</ECF>", entry.Content);
    }

    [Fact]
    public void ZipEntries_Approved_AcceptedNonManualDocs()
    {
        var entries = OldCertificationSimulationService.GetDatabaseZipEntries(Docs(), manual: false);

        Assert.Equal(new[] { "E310000000007.xml", "E320000000018.xml" }, entries.Select(e => e.Name).ToArray());
    }

    [Fact]
    public void BuildZip_ContainsEveryEntry()
    {
        var bytes = OldCertificationSimulationService.BuildZip(new[] { ("a.xml", "<a/>"), ("b.xml", "<b/>") });

        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        Assert.Equal(new[] { "a.xml", "b.xml" }, archive.Entries.Select(e => e.FullName).ToArray());
        using var reader = new StreamReader(archive.GetEntry("b.xml")!.Open());
        Assert.Equal("<b/>", reader.ReadToEnd());
    }
}
