namespace ZynstormECFPlatform.Dtos;

public class BusinessTypeDto
{
    public string GuidId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BusinessSimulationSampleDto
{
    public string GuidId { get; set; } = string.Empty;
    public string EcfType { get; set; } = string.Empty;
    public string JsonData { get; set; } = string.Empty;
}

public class StartSimulationRequestDto
{
    public string BusinessTypeGuidId { get; set; } = string.Empty;
    public string ClientGuidId { get; set; } = string.Empty;
}

