namespace ZynstormECFPlatform.Dtos;

public class EcfStatusHistoryViewDto
{
    public int EcfStatusHistoryId { get; set; }

    public int EcfDocumentId { get; set; }

    public int EcfStatusId { get; set; }

    public string Message { get; set; } = null!;

    public DateTime RegisteredAt { get; set; }
}