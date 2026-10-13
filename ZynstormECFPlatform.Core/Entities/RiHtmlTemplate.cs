namespace ZynstormECFPlatform.Core.Entities;

/// <summary>Formato HTML de Representación Impresa (biblioteca reutilizable). Html ya saneado.</summary>
public class RiHtmlTemplate : BaseEntity
{
    public int RiHtmlTemplateId { get; set; }

    public string Name { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string Html { get; set; } = null!;

    public virtual ICollection<ClientType> ClientTypes { get; set; } = [];

    public virtual ICollection<ClientTypeTemplate> ClientTypeLinks { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}
