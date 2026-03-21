using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfDocumentTotal
{
    public int EcfDocumentTotalId { get; set; }

    public int EcfDocumentId { get; set; }

    public decimal TaxableTotal { get; set; }

    public decimal ExemptTotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal Itbistotal { get; set; }

    public decimal Total { get; set; }

    public virtual EcfDocument EcfDocument { get; set; } = null!;
}
