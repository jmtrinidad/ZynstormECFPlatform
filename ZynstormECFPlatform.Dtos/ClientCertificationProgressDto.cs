namespace ZynstormECFPlatform.Dtos;

public class ClientCertificationProgressDto
{
    public string ClientGuidId { get; set; } = string.Empty;
    public int CurrentStep { get; set; }
    public List<int> CompletedSteps { get; set; } = [];
    public bool IsCertified { get; set; }
}