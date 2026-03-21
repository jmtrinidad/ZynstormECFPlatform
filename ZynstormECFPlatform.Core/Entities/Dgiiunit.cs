using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class DGIIUnit
{
    public int DGIIUnitId { get; set; }

    public int DGIICode { get; set; }

    public string Name { get; set; } = null!;
}
