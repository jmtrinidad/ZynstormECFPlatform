using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Client : BaseEntity
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string Rnc { get; set; } = null!;

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int StatusId { get; set; }

    public bool IsDgiiProduction { get; set; }

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<CertificationInvoicePrintTemplate> CertificationInvoicePrintTemplates { get; set; } = [];
    public virtual ICollection<CertificationProcess> CertificationProcesses { get; set; } = [];
    public virtual ICollection<ApiKey> ApiKeys { get; set; } = [];

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = [];

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = [];

    public virtual ICollection<ClientCertificate> ClientCertificates { get; set; } = [];

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = [];

    public virtual ICollection<UseClient> UseClients { get; set; } = [];
}