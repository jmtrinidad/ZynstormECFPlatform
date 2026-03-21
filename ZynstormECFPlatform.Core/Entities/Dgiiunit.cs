using System;
using System.Collections.Generic;

namespace ZynstormECFPlatform.Core.Entities;

public partial class Dgiiunit
{
    public int DgiiunitId { get; set; }

    public int Dgiicode { get; set; }

    public string Name { get; set; } = null!;
}
