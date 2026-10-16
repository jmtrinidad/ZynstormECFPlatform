using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public class User : IdentityUser, IEntityMarker
{
    [MaxLength(50)]
    public string FirstName { get; set; } = default!;

    [MaxLength(50)]
    public string LastName { get; set; } = default!;

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedTimeUtc { get; set; }

    public string GuidId { get; set; } = Guid.NewGuid().ToString();

    [ConcurrencyCheck]
    public DateTime? LastUpdateUtc { get; set; }

    public DateTime RegisteredAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? LastAccessUtc { get; set; }

    /// <summary>Ruta del frontend a la que entra al iniciar sesión. Null = Dashboard.</summary>
    [MaxLength(150)]
    public string? HomePagePath { get; set; }

    /// <summary>
    /// False mientras nunca se le hayan guardado permisos por página: en ese estado el usuario
    /// ve todo lo de su rol. Una vez guardados, la lista de UserPageAccess manda aunque esté vacía.
    /// </summary>
    public bool PageAccessConfigured { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    public virtual ICollection<UserClient> UserClients { get; set; } = [];

    public virtual ICollection<UserAccessLog> UserAccessLogs { get; set; } = [];

    public virtual ICollection<UserAuditLog> UserAuditLogs { get; set; } = [];

    public virtual ICollection<UserNotificationConfiguration> UserNotificationConfigurations { get; set; } = [];

    public virtual ICollection<UserPageAccess> PageAccesses { get; set; } = [];
}