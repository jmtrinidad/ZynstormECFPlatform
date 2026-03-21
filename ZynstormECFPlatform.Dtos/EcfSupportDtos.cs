using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Dtos;

public class EcfStatusHistoryDto
{
    public int EcfStatusHistoryId { get; set; }
    public int EcfDocumentId { get; set; }
    public int EcfStatusId { get; set; }
    public string? Observation { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public EcfStatusDto? Status { get; set; }
}

public class EcfTransmissionDto
{
    public int EcfTransmissionId { get; set; }
    public int EcfDocumentId { get; set; }
    public int EcfStatusId { get; set; }
    public string? TrackId { get; set; }
    public string? ResponseXml { get; set; }
    public DateTime SentAt { get; set; }
}

public class EcfXmlDocumentDto
{
    public int EcfXmlDocumentId { get; set; }
    public int EcfDocumentId { get; set; }
    public string XmlContent { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
