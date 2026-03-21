using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfTransmission
{
    public int EcfTransmissionId { get; set; }

    public int EcfDocumentId { get; set; }

    public string TrackId { get; set; } = null!;

    public int? AttemptNumber { get; set; }

    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    public int EcfStatusId { get; set; }

    public DateTime SentAt { get; set; }

    public string ResponseCode { get; set; } = null!;

    public string ResponseMessage { get; set; } = null!;

    public bool Success { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;

    public virtual EcfStatus EcfStatus { get; set; } = null!;
}
