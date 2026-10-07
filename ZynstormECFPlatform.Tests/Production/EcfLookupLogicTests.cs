using ZynstormECFPlatform.Core.Entities;
using ZynstormECFPlatform.Core.Enums;
using ZynstormECFPlatform.Services.Production;

namespace ZynstormECFPlatform.Tests.Production;

public class EcfLookupLogicTests
{
    // ─── NormalizeNcf ──────────────────────────────────────────────

    [Theory]
    [InlineData("E320000000098", "E320000000098")]
    [InlineData("  e320000000098 ", "E320000000098")]
    public void NormalizeNcf_AcceptsValidAndNormalizes(string raw, string expected)
    {
        Assert.Equal(expected, EcfLookupLogic.NormalizeNcf(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("B0100000001")]          // NCF tradicional
    [InlineData("E32000000009")]         // 12 caracteres
    [InlineData("E3200000000988")]       // 14 caracteres
    [InlineData("E32000000009A")]        // letra en el consecutivo
    [InlineData("E990000000001")]        // tipo de e-CF que no existe
    public void NormalizeNcf_RejectsInvalid(string? raw)
    {
        Assert.Null(EcfLookupLogic.NormalizeNcf(raw));
    }

    // ─── ResolveState ──────────────────────────────────────────────

    private const string ConditionalPayload =
        "{\"transmission\":{\"TrackId\":\"T1\"},\"status\":{\"TrackId\":\"T1\",\"Codigo\":\"4\",\"Estado\":\"Aceptado Condicional\"}}";

    private const string RejectedPayload =
        "{\"transmission\":{},\"status\":{\"Codigo\":\"2\",\"Estado\":\"Rechazado\"}}";

    [Theory]
    [InlineData(10, EcfLookupState.Accepted)]
    [InlineData(7, EcfLookupState.Pending)]
    [InlineData(8, EcfLookupState.Pending)]
    [InlineData(9, EcfLookupState.Pending)]
    [InlineData(3, EcfLookupState.Rejected)]
    [InlineData(12, EcfLookupState.Error)]
    [InlineData(1, EcfLookupState.NotSent)]
    [InlineData(2, EcfLookupState.NotSent)]
    [InlineData(4, EcfLookupState.NotSent)]
    [InlineData(5, EcfLookupState.NotSent)]
    [InlineData(6, EcfLookupState.NotSent)]
    [InlineData(13, EcfLookupState.NotSent)]
    public void ResolveState_MapsStatusIds(int statusId, EcfLookupState expected)
    {
        Assert.Equal(expected, EcfLookupLogic.ResolveState(statusId, null));
    }

    [Fact]
    public void ResolveState_Id11WithConditionalPayload_IsAcceptedConditional()
    {
        Assert.Equal(EcfLookupState.AcceptedConditional, EcfLookupLogic.ResolveState(11, ConditionalPayload));
    }

    [Theory]
    [InlineData(RejectedPayload)]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("esto no es json")]
    [InlineData("{\"transmission\":{},\"status\":null}")]
    public void ResolveState_Id11WithoutConditionalPayload_IsRejected(string? payload)
    {
        Assert.Equal(EcfLookupState.Rejected, EcfLookupLogic.ResolveState(11, payload));
    }

    [Theory]
    [InlineData(EcfLookupState.Accepted, true)]
    [InlineData(EcfLookupState.AcceptedConditional, true)]
    [InlineData(EcfLookupState.Pending, true)]
    [InlineData(EcfLookupState.Rejected, false)]
    [InlineData(EcfLookupState.Error, false)]
    [InlineData(EcfLookupState.NotSent, false)]
    public void IsUsable_OnlyForAcceptedAcceptedConditionalAndPending(EcfLookupState state, bool expected)
    {
        Assert.Equal(expected, EcfLookupLogic.IsUsable(state));
    }

    // ─── PickBest ──────────────────────────────────────────────────

    private static EcfLookupCandidate Candidate(int id, EcfLookupState state, int minutes) =>
        new(id, state, new DateTime(2026, 10, 7, 9, 0, 0, DateTimeKind.Utc).AddMinutes(minutes));

    [Fact]
    public void PickBest_PrefersAcceptedOverALaterRejection()
    {
        // La factura 118: el primer envío fue aceptado y el reenvío fue rechazado con 75.
        var accepted = Candidate(1, EcfLookupState.Accepted, 0);
        var rejected = Candidate(2, EcfLookupState.Rejected, 1);

        Assert.Equal(1, EcfLookupLogic.PickBest([rejected, accepted])!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_FollowsThePriorityOrder()
    {
        var all = new[]
        {
            Candidate(1, EcfLookupState.NotSent, 5),
            Candidate(2, EcfLookupState.Error, 4),
            Candidate(3, EcfLookupState.Rejected, 3),
            Candidate(4, EcfLookupState.Pending, 2),
            Candidate(5, EcfLookupState.AcceptedConditional, 1),
        };

        Assert.Equal(5, EcfLookupLogic.PickBest(all)!.EcfDocumentId);
        Assert.Equal(4, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId != 5))!.EcfDocumentId);
        Assert.Equal(3, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId < 4))!.EcfDocumentId);
        Assert.Equal(2, EcfLookupLogic.PickBest(all.Where(c => c.EcfDocumentId < 3))!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_BreaksTiesWithTheMostRecent()
    {
        var older = Candidate(1, EcfLookupState.Rejected, 0);
        var newer = Candidate(2, EcfLookupState.Rejected, 10);

        Assert.Equal(2, EcfLookupLogic.PickBest([older, newer])!.EcfDocumentId);
    }

    [Fact]
    public void PickBest_WithNoCandidates_ReturnsNull()
    {
        Assert.Null(EcfLookupLogic.PickBest([]));
    }

    // ─── ForClient ─────────────────────────────────────────────────

    [Fact]
    public void ForClient_ReturnsOnlyTheClientsOwnNonDeletedDocuments()
    {
        var documents = new[]
        {
            new EcfDocument { EcfDocumentId = 1, ClientId = 7, Ncf = "E320000000098" },
            new EcfDocument { EcfDocumentId = 2, ClientId = 8, Ncf = "E320000000098" },   // otro cliente
            new EcfDocument { EcfDocumentId = 3, ClientId = 7, Ncf = "E320000000099" },   // otro eNCF
            new EcfDocument { EcfDocumentId = 4, ClientId = 7, Ncf = "E320000000098", IsDeleted = true },
        }.AsQueryable();

        var ids = documents
            .Where(EcfLookupLogic.ForClient(7, "E320000000098"))
            .Select(document => document.EcfDocumentId)
            .ToList();

        Assert.Equal([1], ids);
    }

    // ─── ResolveEnvironment ────────────────────────────────────────

    [Theory]
    [InlineData("Production", DgiiEnvironment.Production)]
    [InlineData("test", DgiiEnvironment.Test)]
    [InlineData("CerteCF", DgiiEnvironment.CerteCF)]
    [InlineData(null, DgiiEnvironment.Production)]
    [InlineData("", DgiiEnvironment.Production)]
    [InlineData("algo raro", DgiiEnvironment.Production)]
    public void ResolveEnvironment_ParsesOrDefaultsToProduction(string? configured, DgiiEnvironment expected)
    {
        Assert.Equal(expected, EcfLookupLogic.ResolveEnvironment(configured));
    }

    // ─── BuildResponse ─────────────────────────────────────────────

    private const string SignedXml =
        "<ECF><FechaHoraFirma>07-10-2026 09:01:23</FechaHoraFirma>" +
        "<Signature><SignatureValue>ABCDEF123456</SignatureValue></Signature></ECF>";

    private static EcfLookupSource Source(string? signedXml, DateTime? signatureDateTime = null) => new(
        EcfDocumentId: 123,
        ENcf: "E320000000098",
        Total: 140m,
        IssueDateUtc: new DateTime(2026, 10, 7),
        SignatureDateTime: signatureDateTime,
        IssuerRnc: "132293894",
        CustomerRnc: "",
        TrackId: "TRACK-1",
        SignedXml: signedXml);

    [Fact]
    public void BuildResponse_Accepted_CarriesTheDataNeededToPrintTheInvoice()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), EcfLookupState.Accepted, attempts: 2, DgiiEnvironment.Production);

        Assert.True(response.Found);
        Assert.Equal(EcfLookupState.Accepted, response.State);
        Assert.True(response.IsUsable);
        Assert.Equal("TRACK-1", response.TrackId);
        Assert.Equal("ABCDEF", response.SecurityCode);
        Assert.Equal("07-10-2026 09:01:23", response.SignatureDate);
        Assert.Equal(
            "https://fc.dgii.gov.do/ecf/ConsultaTimbreFC?RncEmisor=132293894&ENCF=E320000000098&MontoTotal=140&CodigoSeguridad=ABCDEF",
            response.QrUrl);
        Assert.Equal(123, response.EcfDocumentId);
        Assert.Equal(2, response.Attempts);
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void BuildResponse_UsableWithoutSignedXml_ReturnsStateWithoutSecurityData()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(signedXml: null), EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.True(response.IsUsable);
        Assert.Null(response.SecurityCode);
        Assert.Null(response.SignatureDate);
        Assert.Null(response.QrUrl);
        Assert.Contains("XML firmado", response.Message);
    }

    [Fact]
    public void BuildResponse_UnparseableSignedXml_IsTreatedAsMissing()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source("esto no es xml"), EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.Null(response.SecurityCode);
        Assert.Null(response.QrUrl);
        Assert.Contains("XML firmado", response.Message);
    }

    [Fact]
    public void BuildResponse_SignatureDateFallsBackToTheStoredOne()
    {
        const string xmlWithoutDate = "<ECF><Signature><SignatureValue>ABCDEF123456</SignatureValue></Signature></ECF>";

        var response = EcfLookupLogic.BuildResponse(
            Source(xmlWithoutDate, new DateTime(2026, 10, 7, 9, 1, 23)),
            EcfLookupState.Accepted, attempts: 1, DgiiEnvironment.Production);

        Assert.Equal("07-10-2026 09:01:23", response.SignatureDate);
        Assert.NotNull(response.QrUrl);
    }

    [Theory]
    [InlineData(EcfLookupState.Rejected)]
    [InlineData(EcfLookupState.Error)]
    [InlineData(EcfLookupState.NotSent)]
    public void BuildResponse_NotUsable_DoesNotExposeSecurityData(EcfLookupState state)
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), state, attempts: 1, DgiiEnvironment.Production);

        Assert.False(response.IsUsable);
        Assert.Null(response.SecurityCode);
        Assert.Null(response.QrUrl);
        Assert.False(string.IsNullOrWhiteSpace(response.Message));
    }

    [Fact]
    public void BuildResponse_Pending_IsUsableAndKeepsTheTrackId()
    {
        var response = EcfLookupLogic.BuildResponse(
            Source(SignedXml), EcfLookupState.Pending, attempts: 1, DgiiEnvironment.Production);

        Assert.True(response.IsUsable);
        Assert.Equal("TRACK-1", response.TrackId);
    }
}
