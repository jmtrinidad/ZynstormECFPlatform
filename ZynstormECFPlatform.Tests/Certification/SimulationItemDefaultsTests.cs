using ZynstormECFPlatform.Services.Certification.OldSimulation;

namespace ZynstormECFPlatform.Tests.Certification;

public class SimulationItemDefaultsTests
{
    [Fact]
    public void ApplyItemDefaults_FillsMissingFields()
    {
        var items = new List<OldEcfItemRequestDto> { new() { Name = "BLOCK" } };

        OldCertificationSimulationService.ApplyItemDefaults(items);

        Assert.Equal(2, items[0].ItemType);
        Assert.Equal("BLOCK", items[0].Description);
        Assert.Equal(43, items[0].UnitOfMeasure);
    }

    [Fact]
    public void ApplyItemDefaults_KeepsExistingValues()
    {
        var items = new List<OldEcfItemRequestDto> { new() { Name = "BLOCK", Description = "Block 6", ItemType = 1, UnitOfMeasure = 23 } };

        OldCertificationSimulationService.ApplyItemDefaults(items);

        Assert.Equal(1, items[0].ItemType);
        Assert.Equal("Block 6", items[0].Description);
        Assert.Equal(23, items[0].UnitOfMeasure);
    }
}
