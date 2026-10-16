namespace ZynstormECFPlatform.Core.Entities;

/// <summary>
/// Página del frontend a la que un usuario restringido (no SA ni Admin) tiene acceso.
/// PagePath usa la ruta del frontend, ej. "/facturas".
/// </summary>
public class UserPageAccess : BaseEntity
{
    public int UserPageAccessId { get; set; }

    public string UserId { get; set; } = default!;

    public string PagePath { get; set; } = default!;

    public virtual User User { get; set; } = default!;
}
