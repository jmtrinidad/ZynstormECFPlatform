namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Formato HTML disponible para un tipo de cliente. Un tipo puede tener varios; el predeterminado
/// sigue siendo <see cref="ClientType.RiHtmlTemplateId"/> y siempre cuenta como disponible.
/// </summary>
public class ClientTypeTemplate : BaseEntity
{
    public int ClientTypeTemplateId { get; set; }

    public int ClientTypeId { get; set; }

    public int RiHtmlTemplateId { get; set; }

    public virtual ClientType ClientType { get; set; } = null!;

    public virtual RiHtmlTemplate RiHtmlTemplate { get; set; } = null!;
}
