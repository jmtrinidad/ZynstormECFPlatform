namespace ZynstormECFPlatform.Core.Entities;

public partial class Status : BaseEntity
{
    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ApiKey> ApiKeys { get; set; } = [];

    public virtual ICollection<ClientBranche> ClientBranches { get; set; } = [];

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = [];

    public virtual ICollection<Client> Clients { get; set; } = [];
}