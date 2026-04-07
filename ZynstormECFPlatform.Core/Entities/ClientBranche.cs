using ZynstormECFPlatform.Common;

namespace ZynstormECFPlatform.Core.Entities;

public partial class ClientBranche : BaseEntity
{
    public int ClientBrancheId { get; set; }

    public int ClientId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsMain { get; set; }

    public int StatusId { get; set; }


    public virtual Client Client { get; set; } = null!;

    public virtual Status Status { get; set; } = null!;

    public virtual ICollection<ClientCallBack> ClientCallBacks { get; set; } = [];

    public virtual ICollection<EcfDocument> EcfDocuments { get; set; } = [];
}