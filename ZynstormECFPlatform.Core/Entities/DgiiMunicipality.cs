namespace ZynstormECFPlatform.Core.Entities;

public partial class DgiiMunicipality : BaseEntity
{
    public int DgiiMunicipalityId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsProvince { get; set; }
}
