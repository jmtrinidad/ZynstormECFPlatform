using System.ComponentModel.DataAnnotations;

namespace ZynstormECFPlatform.Core.Entities;

public class UserAuditLog : BaseEntity
{
    [Key]
    public int UserAuditLogId { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [MaxLength(50)]
    public string Action { get; set; } = null!; // Create, Update, Delete

    [MaxLength(100)]
    public string EntityName { get; set; } = null!;

    [MaxLength(450)]
    public string EntityId { get; set; } = null!;

    public string? PreviousState { get; set; } // JSON

    public string? NewState { get; set; } // JSON

    public DateTime TimestampUtc { get; set; }

    public virtual User User { get; set; } = null!;
}
