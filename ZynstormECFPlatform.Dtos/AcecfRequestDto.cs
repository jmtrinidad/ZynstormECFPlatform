using System;

namespace ZynstormECFPlatform.Dtos;

public class AcecfRequestDto
{
    public string? Version { get; set; } = "1.0";
    public string RNCEmisor { get; set; }
    public string ENcf { get; set; }
    public string FechaEmision { get; set; }
    public decimal MontoTotal { get; set; }
    public string RNCComprador { get; set; }
    public int Estado { get; set; }
    public string DetalleMotivoRechazo { get; set; }
    public string FechaHoraAprobacionComercial { get; set; }
}
