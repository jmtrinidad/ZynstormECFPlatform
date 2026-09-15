using System;
using System.Collections.Generic;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Client : BaseEntity
{
    public int ClientId { get; set; }

    public string Name { get; set; } = null!;

    public string Rnc { get; set; } = null!;

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? DailyReportEmails { get; set; }

    public string? WeeklyReportEmails { get; set; }

    public int StatusId { get; set; }

    public bool IsDgiiProduction { get; set; }

    public bool IsCertified { get; set; }

    public int? PlanId { get; set; }

    public bool ClientInactive { get; set; }

    /// <summary>Fecha calendario del último pago de renta.</summary>
    public DateTime? LastRentPaymentDate { get; set; }

    /// <summary>Fecha calendario del próximo pago de renta.</summary>
    public DateTime? NextRentPaymentDate { get; set; }

    /// <summary>El cliente pagó el año completo de renta.</summary>
    public bool RentPaidFullYear { get; set; }

    /// <summary>Descuento por pago adelantado, en porcentaje (0–100). Por defecto 0.</summary>
    public decimal RentDiscountPercent { get; set; }

    public virtual Plan? Plan { get; set; }

    public virtual ICollection<ClientMonthlyUsage> MonthlyUsages { get; set; } = [];

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<ReceivedB2BMessage> ReceivedB2BMessages { get; set; } = [];

    public virtual ICollection<CertificationInvoicePrintTemplate> CertificationInvoicePrintTemplates { get; set; } = [];

    public virtual ICollection<CertificationProcess> CertificationProcesses { get; set; } = [];

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = [];

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = [];

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = [];

    public virtual ICollection<ClientCertificate> ClientCertificates { get; set; } = [];

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = [];

    public virtual ICollection<UserClient> UserClients { get; set; } = [];

    public virtual ICollection<ENcf> ENcfs { get; set; } = [];
}