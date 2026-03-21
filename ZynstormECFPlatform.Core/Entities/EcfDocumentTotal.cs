using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfDocumentTotal : IEntityMarker
{
    public int EcfDocumentTotalId { get; set; }

    public int EcfDocumentId { get; set; }

    public decimal TaxableTotal { get; set; }

    public decimal ExemptTotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal ITBISTotal { get; set; }

    public decimal Total { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}