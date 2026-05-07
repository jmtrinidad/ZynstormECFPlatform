using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public class BusinessType : BaseEntity
{
    public int BusinessTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public virtual ICollection<BusinessSimulationSample> Samples { get; set; } = new List<BusinessSimulationSample>();
}
