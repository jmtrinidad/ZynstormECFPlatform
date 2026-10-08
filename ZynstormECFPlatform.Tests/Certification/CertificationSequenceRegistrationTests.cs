using ZynstormECFPlatform.Services.Certification;

namespace ZynstormECFPlatform.Tests.Certification;

public class CertificationSequenceRegistrationTests
{
    [Fact]
    public void NextSequence_IsAboveHighestEncfPerType()
    {
        var next = CertificationExcelService.GetNextSequencesByType(new[]
        {
            "E310000000001", "E310000000034", "E310000000009",
            "E340000000002", "E320000000006", null, "", "N/A"
        });

        Assert.Equal(35, next["31"]);
        Assert.Equal(3, next["34"]);
        Assert.Equal(7, next["32"]);
        Assert.Equal(3, next.Count);
    }
}
