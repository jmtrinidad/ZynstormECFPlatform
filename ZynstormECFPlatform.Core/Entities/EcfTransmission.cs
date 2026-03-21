using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfTransmission : IEntityMarker
{
    public int EcfTransmissionId { get; set; }

    public int EcfDocumentId { get; set; }

    public string TrackId { get; set; } = null!;

    public int? AttemptNumber { get; set; }

    public string? RequestPayload { get; set; }

    public string? ResponsePayload { get; set; }

    public int EcfStatusId { get; set; }

    public DateTime SentAtUtc { get; set; }

    public string ResponseCode { get; set; } = null!;

    public string ResponseMessage { get; set; } = null!;

    public bool Success { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;

    public virtual EcfStatus EcfStatus { get; set; } = null!;
}