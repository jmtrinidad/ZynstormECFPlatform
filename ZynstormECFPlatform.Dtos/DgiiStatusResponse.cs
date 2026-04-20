using System.Collections.Generic;

namespace ZynstormECFPlatform.Dtos;

public class DgiiStatusResponse
{
    public string TrackId { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Rnc { get; set; } = string.Empty;
    public string ENcf { get; set; } = string.Empty;
    public bool SecuenciaUtilizada { get; set; }
    public string FechaRecepcion { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public List<DgiiStatusMensaje> Mensajes { get; set; } = new();
}

public class DgiiStatusMensaje
{
    public object Codigo { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
}
