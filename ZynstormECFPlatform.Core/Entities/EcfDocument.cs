using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class EcfDocument
{
    public int EcfDocumentId { get; set; }

    public int ClientId { get; set; }

    public int ClientBrancheId { get; set; }

    public int ApiKeyId { get; set; }

    public int EcfTypeId { get; set; }

    public string ExternalReference { get; set; } = null!;

    public string Ncf { get; set; } = null!;

    public string CustomerRnc { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string? CustomerEmail { get; set; }

    public string? CustomerAddress { get; set; }

    public DateTime IssueDate { get; set; }

    public int CurrencyId { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Itbistotal { get; set; }

    public decimal Total { get; set; }

    public int EcfStatusId { get; set; }

    public string? HangfireJobId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public virtual ApiKey ApiKey { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual ClientBranche ClientBranche { get; set; } = null!;

    public virtual Currency Currency { get; set; } = null!;

    public virtual ICollection<EcfDocumentDetail> EcfDocumentDetails { get; set; } = new List<EcfDocumentDetail>();

    public virtual ICollection<EcfDocumentTotal> EcfDocumentTotals { get; set; } = new List<EcfDocumentTotal>();

    public virtual EcfStatus EcfStatus { get; set; } = null!;

    public virtual ICollection<EcfStatusHistory> EcfStatusHistories { get; set; } = new List<EcfStatusHistory>();

    public virtual ICollection<EcfTransmission> EcfTransmissions { get; set; } = new List<EcfTransmission>();

    public virtual EcfType EcfType { get; set; } = null!;

    public virtual ICollection<EcfXmlDocument> EcfXmlDocuments { get; set; } = new List<EcfXmlDocument>();
}
