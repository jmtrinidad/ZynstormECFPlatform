namespace ZynstormECFPlatform.Core.Entities;

public partial class UseClient : BaseEntity
{
    public int ClientId { get; set; }

    public string UserId { get; set; } = null!;

    public virtual Client Client { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}