using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfDocumentDetail : IEntityMarker
{
    public int EcfDocumentDetailId { get; set; }

    public int EcfDocumentId { get; set; }

    public int LineNumber { get; set; }

    public string Description { get; set; } = null!;

    public decimal Quantity { get; set; }

    public int UnitCode { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal SubTotal { get; set; }

    public decimal ItbisPercentage { get; set; }

    public decimal ItbisAmount { get; set; }

    public decimal Total { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = null!;

    public DateTime? LastUpdateUtc { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}