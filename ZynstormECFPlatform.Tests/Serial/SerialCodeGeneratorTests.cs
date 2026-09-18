using System.Text.RegularExpressions;
using ZynstormECFPlatform.Services;

namespace ZynstormECFPlatform.Tests.Serial;

public sealed class SerialCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsExpectedUnambiguousFormat()
    {
        var serial = SerialCodeGenerator.Generate();

        Assert.Matches(new Regex("^[A-HJ-NP-Z2-9]{5}(-[A-HJ-NP-Z2-9]{5}){3}$"), serial);
    }

    [Fact]
    public void Normalize_AcceptsLowerCaseSpacesAndHyphens()
    {
        var normalized = SerialCodeGenerator.Normalize(" abcde - fghjk - 23456 - npqrs ");

        Assert.Equal("ABCDEFGHJK23456NPQRS", normalized);
    }

    [Fact]
    public void ComputeHash_UsesNormalizedValue()
    {
        var formattedHash = SerialCodeGenerator.ComputeHash("ABCDE-FGHJK-23456-NPQRS");
        var compactHash = SerialCodeGenerator.ComputeHash("abcdefghjk23456npqrs");

        Assert.Equal(compactHash, formattedHash);
        Assert.Equal(64, formattedHash.Length);
    }
}
