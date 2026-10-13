namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Tipo/grupo de cliente (ej. "Royaltrix Software"). Define el formato HTML de RI que usan
/// todos sus clientes y los valores de muestra de los campos que el XML no trae.
/// </summary>
public class ClientType : BaseEntity
{
    public int ClientTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? RiHtmlTemplateId { get; set; }

    /// <summary>JSON plano {"Cedula":"...","Medico":"...","EmisorLogo":"data:image/..."}.</summary>
    public string? SampleDataJson { get; set; }

    public virtual RiHtmlTemplate? RiHtmlTemplate { get; set; }

    public virtual ICollection<Client> Clients { get; set; } = [];
}
