namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Logo del emisor usado por los formatos HTML de RI. Vive en su propia tabla (1 a 1 con
/// <see cref="Client"/>) para que las consultas de clientes no carguen la imagen.
/// </summary>
public class ClientLogo : BaseEntity
{
    public int ClientLogoId { get; set; }

    public int ClientId { get; set; }

    /// <summary>data:image/png|jpeg|webp;base64,... (validado por RiLogoValidator, máx. 500 KB).</summary>
    public string DataUri { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;
}
