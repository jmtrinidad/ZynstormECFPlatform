using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdParserTests
{
    [Fact]
    public void Parse_List_ReadsEveryEntry()
    {
        var entries = DgiiTrackIdParser.Parse("""
            [{"trackId":"A","estado":"Aceptado","fechaRecepcion":"13/10/2026 10:00:00"},
             {"trackId":"B","estado":"Rechazado","fechaRecepcion":"13/10/2026 09:00:00"}]
            """);

        Assert.Equal(2, entries.Count);
        Assert.Equal("A", entries[0].TrackId);
        Assert.Equal("Aceptado", entries[0].Estado);
        Assert.Equal("13/10/2026 10:00:00", entries[0].FechaRecepcion);
    }

    [Fact]
    public void Parse_SingleObject_ReadsOneEntry()
    {
        var entries = DgiiTrackIdParser.Parse("""{"trackId":"A","estado":"En proceso","fechaRecepcion":"x"}""");

        Assert.Single(entries);
        Assert.Equal("A", entries[0].TrackId);
    }

    [Fact]
    public void Parse_PropertyNamesAreCaseInsensitive()
    {
        var entries = DgiiTrackIdParser.Parse("""{"TrackId":"A","Estado":"Aceptado"}""");

        Assert.Equal("A", Assert.Single(entries).TrackId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("[]")]
    [InlineData("""{"estado":"No encontrado"}""")]
    public void Parse_EmptyOrWithoutTrackId_ReturnsNoEntries(string? body) =>
        Assert.Empty(DgiiTrackIdParser.Parse(body));
}
