using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class DashboardSummaryDto
{
    public int ActiveClientsCount { get; set; }
    public string ActiveClientsTrend { get; set; } = null!;
    public int SentInvoicesCount { get; set; }
    public string SentInvoicesTrend { get; set; } = null!;
    public int CertifiedClientsCount { get; set; }
    public string CertifiedClientsTrend { get; set; } = null!;
    public int InProcessClientsCount { get; set; }
    public string InProcessClientsTrend { get; set; } = null!;
    public List<DashboardActivityDto> RecentActivities { get; set; } = [];
}

public class DashboardActivityDto
{
    public string ClientName { get; set; } = null!;
    public string Action { get; set; } = null!; // "Factura enviada", "Certificación completada", "Certificación en proceso", "Nuevo cliente"
    public string Status { get; set; } = null!; // "Aceptada", "Paso X", "Pendiente", "Registrado", "Rechazada"
    public string Time { get; set; } = null!; // Ej: "Hace 5 min", "Hace 2 horas", "Ayer"
}
