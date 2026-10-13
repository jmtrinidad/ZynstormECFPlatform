using ZynstormECFPlatform.Abstractions.Services;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class DgiiTrackIdPickerTests
{
    private static DgiiTrackIdEntry Entry(string trackId, string? estado, string? fecha = null) =>
        new() { TrackId = trackId, Estado = estado, FechaRecepcion = fecha };

    [Fact]
    public void Pick_Empty_ReturnsNull() => Assert.Null(DgiiTrackIdPicker.Pick([]));

    [Fact]
    public void Pick_IgnoresNotFoundAndEntriesWithoutTrackId()
    {
        var picked = DgiiTrackIdPicker.Pick([Entry("", "Aceptado"), Entry("T1", "No encontrado")]);

        Assert.Null(picked);
    }

    [Fact]
    public void Pick_PrefersAcceptedOverInProcessOverRejected()
    {
        var picked = DgiiTrackIdPicker.Pick([
            Entry("R", "Rechazado", "2026-10-13 12:00:00"),
            Entry("P", "En proceso", "2026-10-13 11:00:00"),
            Entry("A", "Aceptado", "2026-10-13 09:00:00")]);

        Assert.Equal("A", picked!.TrackId);
    }

    [Fact]
    public void Pick_AcceptedConditionalCountsAsAccepted()
    {
        var picked = DgiiTrackIdPicker.Pick([Entry("P", "En proceso"), Entry("C", "Aceptado Condicional")]);

        Assert.Equal("C", picked!.TrackId);
    }

    [Fact]
    public void Pick_SameRank_PicksTheMostRecentReception()
    {
        var picked = DgiiTrackIdPicker.Pick([
            Entry("OLD", "En proceso", "2026-10-13 09:00:00"),
            Entry("NEW", "En proceso", "2026-10-13 10:00:00")]);

        Assert.Equal("NEW", picked!.TrackId);
    }

    [Fact]
    public void Pick_ExcludesTheGivenTrackId()
    {
        var picked = DgiiTrackIdPicker.Pick(
            [Entry("DUP", "Rechazado"), Entry("ORIG", "En proceso")],
            excludeTrackId: "DUP");

        Assert.Equal("ORIG", picked!.TrackId);

        Assert.Null(DgiiTrackIdPicker.Pick([Entry("DUP", "Rechazado")], excludeTrackId: "dup"));
    }
}
