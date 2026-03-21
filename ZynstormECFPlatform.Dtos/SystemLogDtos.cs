namespace ZynstormECFPlatform.Dtos;

public class SystemLogViewDto
{
    public int SystemLogId { get; set; }

    public int ClientId { get; set; }

    public int EcfDocumentId { get; set; }

    public string LogLevel { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreateAtUtc { get; set; }

    public string? Exception { get; set; }
}