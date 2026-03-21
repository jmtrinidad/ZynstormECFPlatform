namespace ZynstormECFPlatform.Dtos;

public class ApiKeyViewDto
{
    public int ApiKeyId { get; set; }

    public int ClientId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int StatusId { get; set; }

    public DateTime CreatedAt { get; set; }
}