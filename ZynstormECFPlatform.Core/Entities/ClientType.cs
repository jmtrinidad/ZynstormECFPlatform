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

    /// <summary>Formato predeterminado del tipo (también aparece en <see cref="TemplateLinks"/> o se añade al listar).</summary>
    public virtual RiHtmlTemplate? RiHtmlTemplate { get; set; }

    /// <summary>Formatos disponibles para los clientes de este tipo.</summary>
    public virtual ICollection<ClientTypeTemplate> TemplateLinks { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}
